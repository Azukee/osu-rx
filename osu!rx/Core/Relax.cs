using osu_rx.Configuration;
using osu_rx.Dependencies;
using osu_rx.Helpers;
using osu_rx.osu;
using OsuParsers.Beatmaps;
using OsuParsers.Beatmaps.Objects;
using OsuParsers.Enums;
using OsuParsers.Enums.Beatmaps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;

namespace osu_rx.Core
{
    public class Relax
    {
        private OsuManager osuManager;
        private ConfigManager configManager;
        private InputSimulator inputSimulator;

        private Beatmap currentBeatmap;
        private bool shouldStop;

        private int hitWindow50;
        private int hitWindow100;
        private int hitWindow300;

        private Random random = new Random();

        private Timewarp timewarp;
        public Relax()
        {
            osuManager = DependencyContainer.Get<OsuManager>();
            configManager = DependencyContainer.Get<ConfigManager>();
            inputSimulator = new InputSimulator();
            timewarp = new Timewarp();
        }

        public void Start()
        {
            shouldStop = false;
            currentBeatmap = osuManager.CurrentBeatmap;

            hitWindow50 = osuManager.HitWindow50(currentBeatmap.DifficultySection.OverallDifficulty);
            hitWindow100 = osuManager.HitWindow100(currentBeatmap.DifficultySection.OverallDifficulty);
            hitWindow300 = osuManager.HitWindow300(currentBeatmap.DifficultySection.OverallDifficulty);

            float audioRate = (osuManager.CurrentMods.HasFlag(Mods.DoubleTime) || osuManager.CurrentMods.HasFlag(Mods.Nightcore)) ? 1.5f : osuManager.CurrentMods.HasFlag(Mods.HalfTime) ? 0.75f : 1f;
            float maxBPM = configManager.MaxSingletapBPM / (audioRate / 2);

            int index, lastTime, hitTime = 0;
            bool isHit, shouldStartAlternating, shouldAlternate;
            VirtualKeyCode currentKey;
            HitObject currentHitObject;
            (int StartOffset, int HoldTime) currentHitTimings;

            reset();

            while (osuManager.CanPlay && index < currentBeatmap.HitObjects.Count && !shouldStop)
            {
                Thread.Sleep(1);

                if (configManager.EnableTimewarp)
                    timewarp.Update(configManager.TimewarpRate, audioRate);

                if (osuManager.IsPaused)
                {
                    if (isHit)
                    {
                        isHit = false;
                        releaseAllKeys();
                    }

                    continue;
                }

                if (lastTime > osuManager.CurrentTime)
                {
                    reset(true);
                    releaseAllKeys();
                    continue;
                }
                else
                    lastTime = osuManager.CurrentTime;

                int currentTime = osuManager.CurrentTime + configManager.AudioOffset;
                if (currentTime >= currentHitObject.StartTime - hitWindow50)
                {
                    if (!isHit)
                    {
                        var hitScanResult = getHitScanResult(index);
                        switch (hitScanResult)
                        {
                            case HitScanResult.CanHit when currentTime >= currentHitObject.StartTime + currentHitTimings.StartOffset:
                            case HitScanResult.ShouldHit:
                                {
                                    isHit = true;
                                    hitTime = currentTime;

                                    switch (configManager.PlayStyle)
                                    {
                                        case PlayStyles.MouseOnly when currentKey == configManager.PrimaryKey:
                                            inputSimulator.Mouse.LeftButtonDown();
                                            break;
                                        case PlayStyles.MouseOnly:
                                            inputSimulator.Mouse.RightButtonDown();
                                            break;
                                        case PlayStyles.TapX when !shouldAlternate && !shouldStartAlternating:
                                            inputSimulator.Mouse.LeftButtonDown();
                                            currentKey = configManager.PrimaryKey;
                                            break;
                                        default:
                                            inputSimulator.Keyboard.KeyDown(currentKey);
                                            break;
                                    }
                                }
                                break;
                            case HitScanResult.MoveToNextObject:
                                moveToNextObject();
                                break;
                        }
                    }
                    else if (currentTime >= (currentHitObject is HitCircle ? hitTime : currentHitObject.EndTime) + currentHitTimings.HoldTime)
                    {
                        moveToNextObject();

                        if (currentHitObject is Spinner && currentHitObject.StartTime - currentBeatmap.HitObjects[index - 1].EndTime <= configManager.HoldBeforeSpinnerTime)
                            continue;

                        isHit = false;
                        releaseAllKeys();
                    }
                }
            }

            releaseAllKeys();

            if (configManager.EnableTimewarp)
                timewarp.Reset();

            while (osuManager.CanPlay && index >= currentBeatmap.HitObjects.Count && !shouldStop)
                Thread.Sleep(5);

            void reset(bool retry = false)
            {
                index = retry ? 0 : closestHitObjectIndex;
                isHit = false;
                currentKey = configManager.PrimaryKey;
                currentHitObject = currentBeatmap.HitObjects[index];
                updateAlternate();
                currentHitTimings = randomizeHitObjectTimings(index, shouldAlternate, false);
                lastTime = int.MinValue;

                if (configManager.EnableTimewarp)
                    timewarp.Refresh();
            }

            void updateAlternate()
            {
                var lastHitObject = index > 0 ? currentBeatmap.HitObjects[index - 1] : null;
                var nextHitObject = index + 1 < currentBeatmap.HitObjects.Count ? currentBeatmap.HitObjects[index + 1] : null;

                shouldStartAlternating = nextHitObject != null ? 60000 / (nextHitObject.StartTime - currentHitObject.EndTime) >= maxBPM : false;
                shouldAlternate = lastHitObject != null ? 60000 / (currentHitObject.StartTime - lastHitObject.EndTime) >= maxBPM : false;
                if (shouldAlternate || configManager.PlayStyle == PlayStyles.Alternate)
                    currentKey = (currentKey == configManager.PrimaryKey) ? configManager.SecondaryKey : configManager.PrimaryKey;
                else
                    currentKey = configManager.PrimaryKey;
            }

            void moveToNextObject()
            {
                index++;
                if (index < currentBeatmap.HitObjects.Count)
                {
                    currentHitObject = currentBeatmap.HitObjects[index];

                    updateAlternate();
                    currentHitTimings = randomizeHitObjectTimings(index, shouldAlternate, inputSimulator.InputDeviceState.IsKeyDown(configManager.HitWindow100Key));
                }
            }
        }

        public void Stop() => shouldStop = true;

        private void releaseAllKeys()
        {
            inputSimulator.Keyboard.KeyUp(configManager.PrimaryKey);
            inputSimulator.Keyboard.KeyUp(configManager.SecondaryKey);
            inputSimulator.Mouse.LeftButtonUp();
            inputSimulator.Mouse.RightButtonUp();
        }

        private int closestHitObjectIndex
        {
            get
            {
                int time = osuManager.CurrentTime;
                for (int i = 0; i < currentBeatmap.HitObjects.Count; i++)
                    if (currentBeatmap.HitObjects[i].StartTime >= time)
                        return i;

                return currentBeatmap.HitObjects.Count;
            }
        }

        private int lastHitScanIndex;
        private Vector2? lastOnNotePosition = null;
        private HitScanResult getHitScanResult(int index)
        {
            var hitObject = currentBeatmap.HitObjects[index];

            if (!configManager.EnableHitScan || hitObject is Spinner)
                return HitScanResult.CanHit;

            if (lastHitScanIndex != index)
            {
                lastHitScanIndex = index;
                lastOnNotePosition = null;
            }

            //TODO: implement slider path support
            bool isSliding = hitObject is Slider && osuManager.CurrentTime > hitObject.StartTime;
            float hitObjectRadius = osuManager.HitObjectRadius(currentBeatmap.DifficultySection.CircleSize);
            hitObjectRadius *= isSliding ? 2.4f : 1;

            float distanceToObject = Vector2.Distance(osuManager.CursorPosition, hitObjectPosition(hitObject) * osuManager.OsuWindow.PlayfieldRatio);
            float distanceToLastPos = Vector2.Distance(osuManager.CursorPosition, lastOnNotePosition ?? Vector2.Zero);

            if (osuManager.CurrentTime > hitObject.EndTime + hitWindow50)
            {
                if (configManager.HitScanMissAfterHitWindow50)
                {
                    if (distanceToObject <= hitObjectRadius + configManager.HitScanRadiusAdditional && !intersectsWithOtherHitObjects(index + 1))
                        return HitScanResult.ShouldHit;
                }

                return HitScanResult.MoveToNextObject;
            }

            if (configManager.EnableHitScanPrediction)
            {
                if (distanceToObject > hitObjectRadius * configManager.HitScanRadiusMultiplier)
                {
                    if (lastOnNotePosition != null && distanceToLastPos <= configManager.HitScanMaxDistance)
                        return HitScanResult.ShouldHit;
                }
                else
                    lastOnNotePosition = osuManager.CursorPosition;
            }

            if (distanceToObject <= hitObjectRadius)
                return HitScanResult.CanHit;

            if (configManager.HitScanMissChance != 0)
                if (distanceToObject <= hitObjectRadius + configManager.HitScanRadiusAdditional && random.Next(1, 101) <= configManager.HitScanMissChance && !intersectsWithOtherHitObjects(index + 1))
                    return HitScanResult.CanHit;

            return HitScanResult.Wait;
        }

        private (int StartOffset, int HoldTime) randomizeHitObjectTimings(int index, bool alternating, bool allowHit100)
        {
            (int StartOffset, int HoldTime) result;

            float acc = alternating ? random.NextFloat(1.2f, 1.7f) : 2;

            if (allowHit100)
                result.StartOffset = random.Next(-hitWindow100 / 2, hitWindow100 / 2);
            else
                result.StartOffset = random.Next((int)(-hitWindow300 / acc), (int)(hitWindow300 / acc));

            if (currentBeatmap.HitObjects[index] is Slider)
            {
                int sliderDuration = currentBeatmap.HitObjects[index].EndTime - currentBeatmap.HitObjects[index].StartTime;
                result.HoldTime = random.Next(sliderDuration >= 72 ? -26 : sliderDuration / 2 - 10, hitWindow300 * 2);
            }
            else
                result.HoldTime = random.Next(hitWindow300, hitWindow300 * 2);

            return result;
        }

        private Vector2 hitObjectPosition(HitObject hitObject)
        {
            float y = osuManager.CurrentMods.HasFlag(Mods.HardRock) ? 384 - hitObject.Position.Y : hitObject.Position.Y;

            return new Vector2(hitObject.Position.X, y);
        }

        private bool intersectsWithOtherHitObjects(int startIndex)
        {
            int time = osuManager.CurrentTime;
            Vector2 cursorPosition = osuManager.CursorPosition;

            for (int i = startIndex; i < currentBeatmap.HitObjects.Count; i++)
            {
                var hitObject = currentBeatmap.HitObjects[i];
                double preEmpt = osuManager.DifficultyRange(currentBeatmap.DifficultySection.ApproachRate, 1800, 1200, 450);
                double startTime = hitObject.StartTime - preEmpt;
                if (startTime > time)
                    break;

                float distanceToObject = Vector2.Distance(cursorPosition, hitObject.Position * osuManager.OsuWindow.PlayfieldRatio);
                if (distanceToObject <= osuManager.HitObjectRadius(currentBeatmap.DifficultySection.CircleSize))
                    return true;
            }

            return false;
        }
    }
}