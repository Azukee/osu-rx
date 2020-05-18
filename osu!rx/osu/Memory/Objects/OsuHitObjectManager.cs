using OsuParsers.Beatmaps.Objects;
using OsuParsers.Enums;
using OsuParsers.Enums.Beatmaps;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace osu_rx.osu.Memory.Objects
{
    public class OsuHitObjectManager : OsuObject
    {
        public OsuHitObjectManager(UIntPtr pointerToBaseAddress) => BaseAddress = pointerToBaseAddress;

        public Mods CurrentMods
        {
            get
            {
                UIntPtr modsObjectPointer = (UIntPtr)OsuProcess.ReadInt32(BaseAddress + 0x34);
                int encryptedValue = OsuProcess.ReadInt32(modsObjectPointer + 0x08);
                int decryptionKey = OsuProcess.ReadInt32(modsObjectPointer + 0x0C);

                return (Mods)(encryptedValue ^ decryptionKey);
            }
        }

        public List<HitObject> HitObjects
        {
            get
            {
                List<HitObject> hitObjects = new List<HitObject>();

                UIntPtr hitObjectsListPointer = (UIntPtr)OsuProcess.ReadInt32(BaseAddress + 0x48);
                UIntPtr hitObjectsList = (UIntPtr)OsuProcess.ReadInt32(hitObjectsListPointer + 0x4);
                int hitObjectsCount = OsuProcess.ReadInt32(hitObjectsListPointer + 0xC);

                for (int i = 0; i < hitObjectsCount; i++)
                {
                    UIntPtr hitObjectPointer = (UIntPtr)OsuProcess.ReadInt32(hitObjectsList + 0x8 + 0x4 * i);

                    HitObject hitObject = null;

                    //TODO: expose this enum in osuparsers
                    HitObjectType type = (HitObjectType)OsuProcess.ReadInt32(hitObjectPointer + 0x18);
                    type &= ~HitObjectType.ComboOffset;
                    type &= ~HitObjectType.NewCombo;

                    int startTime = OsuProcess.ReadInt32(hitObjectPointer + 0x10);
                    int endTime = OsuProcess.ReadInt32(hitObjectPointer + 0x14);
                    HitSoundType hitSoundType = (HitSoundType)OsuProcess.ReadInt32(hitObjectPointer + 0x1C);
                    Vector2 position = new Vector2(OsuProcess.ReadFloat(hitObjectPointer + 0x38), OsuProcess.ReadFloat(hitObjectPointer + 0x3C));

                    switch (type)
                    {
                        case HitObjectType.Circle:
                            hitObject = new HitCircle(position, startTime, endTime, hitSoundType, null, false, 0);
                            break;
                        case HitObjectType.Slider:
                            int repeats = OsuProcess.ReadInt32(hitObjectPointer + 0x20);
                            double pixelLength = OsuProcess.ReadDouble(hitObjectPointer + 0x8);
                            CurveType curveType = (CurveType)OsuProcess.ReadInt32(hitObjectPointer + 0xE8);
                            UIntPtr sliderPointsPointer = (UIntPtr)OsuProcess.ReadInt32(hitObjectPointer + 0xC0);
                            UIntPtr sliderPointsList = (UIntPtr)OsuProcess.ReadInt32(sliderPointsPointer + 0x4);
                            int sliderPointsCount = OsuProcess.ReadInt32(sliderPointsPointer + 0xC);
                            List<Vector2> sliderPoints = new List<Vector2>();
                            for (int j = 0; j < sliderPointsCount; j++)
                            {
                                UIntPtr sliderPoint = sliderPointsList + 0x8 + 0x8 * j;

                                sliderPoints.Add(new Vector2(OsuProcess.ReadFloat(sliderPoint), OsuProcess.ReadFloat(sliderPoint + 0x4)));
                            }
                            hitObject = new Slider(position, startTime, endTime, hitSoundType, curveType, sliderPoints, repeats, pixelLength, false, 0);
                            break;
                        case HitObjectType.Spinner:
                            hitObject = new Spinner(position, startTime, endTime, hitSoundType, null, false, 0);
                            break;
                    }

                    hitObjects.Add(hitObject);
                }

                return hitObjects;
            }
        }
    }

    enum HitObjectType
    {
        Circle = 1 << 0,
        Slider = 1 << 1,
        NewCombo = 1 << 2,
        Spinner = 1 << 3,
        ComboOffset = 1 << 4 | 1 << 5 | 1 << 6,
        Hold = 1 << 7
    }
}