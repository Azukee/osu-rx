using osu_rx.Configuration;
using osu_rx.Dependencies;
using osu_rx.Helpers;
using osu_rx.osu;
using OsuParsers.Beatmaps;
using OsuParsers.Beatmaps.Objects;
using OsuParsers.Enums;
using System;
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

        private VirtualKeyCode primaryKey;
        private VirtualKeyCode secondaryKey;

        private bool hitScanEnabled;
        private bool holdBeforeSpinnerEnabled;
        private bool hitScanPredictionEnabled;
        private float hitScanRadiusMultiplier;
        private int hitScanMaxDistance;
        private float hitScanRadiusAdditional;
        
        private int hitWindow50;
        private int hitWindow100;
        private int hitWindow300;

        private float hitObjectRadius
        {
            get
            {
                float size = (float)(osuManager.OsuWindow.PlayfieldSize.X / 8f * (1f - 0.7f * osuManager.AdjustDifficulty(currentBeatmap.DifficultySection.CircleSize)));
                return size / 2f / osuManager.OsuWindow.PlayfieldRatio * 1.00041f;
            }
        }

        public Relax()
        {
            osuManager = DependencyContainer.Get<OsuManager>();
            configManager = DependencyContainer.Get<ConfigManager>();
            inputSimulator = new InputSimulator();
        }

        public void Start()
        {
            shouldStop = false;
            currentBeatmap = osuManager.CurrentBeatmap;
            primaryKey = configManager.PrimaryKey;
            secondaryKey = configManager.SecondaryKey;

            hitScanEnabled = configManager.EnableHitScan;
            holdBeforeSpinnerEnabled = configManager.HoldBeforeSpinner;
            hitScanPredictionEnabled = configManager.EnableHitScanPrediction;
            hitScanRadiusMultiplier = configManager.HitScanRadiusMultiplier;
            hitScanMaxDistance = configManager.HitScanMaxDistance;
            hitScanRadiusAdditional = configManager.HitScanRadiusAdditional;

            var playStyle = configManager.PlayStyle;
            var hit100Key = configManager.HitWindow100Key;
            float audioRate = (osuManager.CurrentMods.HasFlag(Mods.DoubleTime) || osuManager.CurrentMods.HasFlag(Mods.Nightcore)) ? 1.5f : osuManager.CurrentMods.HasFlag(Mods.HalfTime) ? 0.75f : 1f;
            float maxBPM = configManager.MaxSingletapBPM / (audioRate / 2);
            int audioOffset = configManager.AudioOffset;

            hitWindow50 = osuManager.HitWindow50(currentBeatmap.DifficultySection.OverallDifficulty);
            hitWindow100 = osuManager.HitWindow100(currentBeatmap.DifficultySection.OverallDifficulty);
            hitWindow300 = osuManager.HitWindow300(currentBeatmap.DifficultySection.OverallDifficulty);

            int index, lastTime, hitTime = 0;
            bool isHit, shouldStartAlternating, shouldAlternate;
            VirtualKeyCode currentKey;
            HitObject currentHitObject;
            (int StartOffset, int HoldTime) currentHitTimings;

            reset();

            while (osuManager.CanPlay && index < currentBeatmap.HitObjects.Count && !shouldStop)
            {
                Thread.Sleep(1);

                if (osuManager.IsPaused)
                {
                    if (isHit)
                    {
                        isHit = false;
                        releaseAllKeys();
                    }

                    continue;
                }

                int currentTime = osuManager.CurrentTime + audioOffset;
                if (currentTime < lastTime)
                {
                    reset();
                    releaseAllKeys();
                    continue;
                }

                if (currentTime >= currentHitObject.StartTime - hitWindow50)
                {
                    var hitScanResult = getHitScanResult(index);
                    if (!isHit && ((currentTime >= currentHitObject.StartTime + currentHitTimings.StartOffset && hitScanResult == HitScanResult.CanHit) || hitScanResult == HitScanResult.ShouldHit))
                    {
                        isHit = true;
                        hitTime = currentTime;

                        if (playStyle == PlayStyles.MouseOnly)
                        {
                            if (currentKey == primaryKey)
                                inputSimulator.Mouse.LeftButtonDown();
                            else
                                inputSimulator.Mouse.RightButtonDown();
                        }
                        else if (playStyle == PlayStyles.TapX && !shouldAlternate && !shouldStartAlternating)
                        {
                            inputSimulator.Mouse.LeftButtonDown();
                            currentKey = primaryKey;
                        }
                        else
                            inputSimulator.Keyboard.KeyDown(currentKey);
                    }
                    else if (isHit && currentTime >= (currentHitObject is HitCircle ? hitTime : currentHitObject.EndTime) + currentHitTimings.HoldTime)
                    {
                        isHit = false;
                        moveToNextObject();
                        releaseAllKeys();
                    }
                    else if (!isHit && hitScanResult == HitScanResult.Wait && currentTime >= (currentHitObject is HitCircle ? currentHitObject.StartTime : currentHitObject.EndTime + hitWindow50))
                        moveToNextObject();
                }

                lastTime = currentTime;
            }

            releaseAllKeys();

            while (osuManager.CanPlay && !shouldStop)
                Thread.Sleep(5);

            void reset()
            {
                index = closestHitObjectIndex;
                isHit = false;
                currentKey = primaryKey;
                currentHitObject = currentBeatmap.HitObjects[index];
                updateAlternate();
                currentHitTimings = randomizeHitObjectTimings(index, shouldAlternate, false);
                lastTime = -currentBeatmap.GeneralSection.AudioLeadIn;
            }

            void updateAlternate()
            {
                var lastHitObject = index > 0 ? currentBeatmap.HitObjects[index - 1] : null;
                var nextHitObject = index + 1 < currentBeatmap.HitObjects.Count ? currentBeatmap.HitObjects[index + 1] : null;

                shouldStartAlternating = nextHitObject != null ? 60000 / (nextHitObject.StartTime - currentHitObject.EndTime) >= maxBPM : false;
                shouldAlternate = lastHitObject != null ? 60000 / (currentHitObject.StartTime - lastHitObject.EndTime) >= maxBPM : false;
                if (shouldAlternate || playStyle == PlayStyles.Alternate)
                    currentKey = (currentKey == primaryKey) ? secondaryKey : primaryKey;
                else
                    currentKey = primaryKey;
            }

            void moveToNextObject()
            {
                index++;
                if (index < currentBeatmap.HitObjects.Count)
                {
                    currentHitObject = currentBeatmap.HitObjects[index];
                    var nextHitObject = index + 1 < currentBeatmap.HitObjects.Count ? currentBeatmap.HitObjects[index + 1] : null;
                    
                    // Check if next object is a spinner, if so, hold!
                    if (nextHitObject is Spinner && holdBeforeSpinnerEnabled)
                    {
                        nextHitObject.StartTime = currentHitObject.StartTime;
                        currentHitObject = nextHitObject;
                        
                        // This is to prevent a DivisionByZero Exception at LOC:166
                        currentBeatmap.HitObjects.RemoveAt(index + 1);
                    }
                    
                    updateAlternate();
                    currentHitTimings = randomizeHitObjectTimings(index, shouldAlternate, inputSimulator.InputDeviceState.IsKeyDown(hit100Key));
                }
            }
        }

        public void Stop() => shouldStop = true;

        private void releaseAllKeys()
        {
            inputSimulator.Keyboard.KeyUp(primaryKey);
            inputSimulator.Keyboard.KeyUp(secondaryKey);
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
        private Vector2 lastOnNotePosition = Vector2.Zero;
        private HitScanResult getHitScanResult(int index)
        {
            var hitObject = currentBeatmap.HitObjects[index];

            if (!hitScanEnabled || hitObject is Spinner)
                return HitScanResult.CanHit;

            if (lastHitScanIndex != index)
            {
                lastHitScanIndex = index;
                lastOnNotePosition = Vector2.Zero;
            }

            Vector2 hitObjectPosition()
            {
                float y = osuManager.CurrentMods.HasFlag(Mods.HardRock) ? 384 - hitObject.Position.Y : hitObject.Position.Y;

                return new Vector2(hitObject.Position.X, y);
            }

            float distanceToObject = Vector2.Distance(osuManager.CursorPosition, hitObjectPosition() * osuManager.OsuWindow.PlayfieldRatio);
            float distanceToLastPos = Vector2.Distance(osuManager.CursorPosition, lastOnNotePosition);

            if (hitScanPredictionEnabled)
            {
                //checking if cursor is almost outside or outside of object's radius
                if (distanceToObject > hitObjectRadius * hitScanRadiusMultiplier)
                {
                    if (lastOnNotePosition != Vector2.Zero && distanceToLastPos <= hitScanMaxDistance)
                        return HitScanResult.ShouldHit; //force hit if cursor didn't traveled too much distance

                    if (hitObject is Slider && osuManager.CurrentTime > hitObject.StartTime + hitWindow50)
                        return HitScanResult.ShouldHit; //force hit if starttime has ended so we can at least sliderbreak

                    if (distanceToObject <= hitObjectRadius + hitScanRadiusAdditional)
                        return HitScanResult.CanHit; //telling relax that it can hit if cursor is somewhere near object's radius

                    return HitScanResult.Wait;
                }

                lastOnNotePosition = osuManager.CursorPosition;
                return HitScanResult.CanHit; //telling relax that it can hit if cursor is inside object's radius
            }
            else //use more simple algorithm if prediction is disabled
            {
                if (distanceToObject <= hitObjectRadius)
                    return HitScanResult.CanHit;

                return HitScanResult.Wait;
            }
        }

        private (int StartOffset, int HoldTime) randomizeHitObjectTimings(int index, bool alternating, bool allowHit100)
        {
            (int StartOffset, int HoldTime) result;

            var random = new Random();

            float acc = alternating ? random.NextFloat(1.2f, 1.7f) : 2;

            if (allowHit100)
                result.StartOffset = random.Next(-hitWindow100 / 2, hitWindow100 / 2);
            else
                result.StartOffset = random.Next((int)(-hitWindow300 / acc), (int)(hitWindow300 / acc));

            int circleHoldTime = random.Next(hitWindow300, hitWindow300 * 2);
            int sliderHoldTime = random.Next(-hitWindow300 / 2, hitWindow300 * 2);

            result.HoldTime = currentBeatmap.HitObjects[index] is HitCircle ? circleHoldTime : sliderHoldTime;

            return result;
        }
    }
}