using osu_rx.Configuration;
using osu_rx.Dependencies;
using osu_rx.Helpers;
using osu_rx.osu;
using OsuParsers.Beatmaps;
using OsuParsers.Beatmaps.Objects;
using OsuParsers.Enums;
using System;
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

            var playStyle = configManager.PlayStyle;
            var hit100Key = configManager.HitWindow100Key;
            float audioRate = (osuManager.CurrentMods.HasFlag(Mods.DoubleTime) || osuManager.CurrentMods.HasFlag(Mods.Nightcore)) ? 1.5f : osuManager.CurrentMods.HasFlag(Mods.HalfTime) ? 0.75f : 1f;
            float maxBPM = configManager.MaxSingletapBPM / (audioRate / 2);
            int audioOffset = configManager.AudioOffset;

            float hitWindow50 = osuManager.HitWindow50(currentBeatmap.DifficultySection.OverallDifficulty);

            int index, lastTime;
            bool isHit, shouldStartAlternating, shouldAlternate;
            VirtualKeyCode currentKey;
            HitObject currentHitObject;

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

                if (!isHit && currentTime >= currentHitObject.StartTime)
                {
                    isHit = true;

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
                else if (isHit && currentTime >= currentHitObject.EndTime)
                {
                    isHit = false;
                    moveToNextObject();
                    releaseAllKeys();
                }
                //else if (currentTime >= currentBeatmap.HitObjects[index].EndTime + hitWindow50) //hitscan leftovers
                //    moveToNextObject();

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
                updateAlternate();
                currentHitObject = randomizeHitObjectTimings(currentBeatmap.HitObjects[index], shouldAlternate, false);
                lastTime = -currentBeatmap.GeneralSection.AudioLeadIn;
            }

            void updateAlternate()
            {
                shouldStartAlternating = index + 1 < currentBeatmap.HitObjects.Count ? 60000 / (currentBeatmap.HitObjects[index + 1].StartTime - currentBeatmap.HitObjects[index].EndTime) >= maxBPM : false;
                shouldAlternate = index > 0 ? 60000 / (currentBeatmap.HitObjects[index].StartTime - currentBeatmap.HitObjects[index - 1].EndTime) >= maxBPM : false;
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
                    updateAlternate();
                    currentHitObject = randomizeHitObjectTimings(currentBeatmap.HitObjects[index], shouldAlternate, inputSimulator.InputDeviceState.IsKeyDown(hit100Key));
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

        private HitObject randomizeHitObjectTimings(HitObject hitObject, bool alternating, bool allowHit100)
        {
            var result = new HitObject(hitObject.Position, hitObject.StartTime, hitObject.EndTime, hitObject.HitSound, null, false, 0);

            int hitWindow300 = osuManager.HitWindow300(currentBeatmap.DifficultySection.OverallDifficulty);
            int hitWindow100 = osuManager.HitWindow100(currentBeatmap.DifficultySection.OverallDifficulty);

            var random = new Random();

            float acc = alternating ? random.NextFloat(1.2f, 1.7f) : 2;

            if (allowHit100)
                result.StartTime += random.Next(-hitWindow100 / 2, hitWindow100 / 2);
            else
                result.StartTime += random.Next((int)(-hitWindow300 / acc), (int)(hitWindow300 / acc));

            int circleHoldTime = random.Next(hitWindow300, hitWindow300 * 2);
            int sliderHoldTime = random.Next(-hitWindow300 / 2, hitWindow300 * 2);

            if (hitObject is HitCircle)
                result.EndTime = result.StartTime + circleHoldTime;
            else if (hitObject is Slider)
                result.EndTime += sliderHoldTime;

            return result;
        }
    }
}
