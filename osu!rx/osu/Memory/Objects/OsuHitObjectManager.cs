using System;
using System.Collections.Generic;
using OsuParsers.Beatmaps.Objects;
using OsuParsers.Enums;

namespace osu_rx.osu.Memory.Objects
{
    public class OsuHitObjectManager : OsuObject
    {
        public UIntPtr PointerToBaseAddress { get; private set; }

        public override UIntPtr BaseAddress
        {
            get => (UIntPtr)OsuProcess.ReadInt32(PointerToBaseAddress);
            protected set { }
        }
        
        public OsuHitObjectManager(UIntPtr pointerToBaseAddress) => PointerToBaseAddress = pointerToBaseAddress;

        public Mods CurrentMods
        {
            get {
                UIntPtr modsObjectPointer = (UIntPtr) OsuProcess.ReadInt32(PointerToBaseAddress + 0x34);
                int encryptedValue = OsuProcess.ReadInt32(modsObjectPointer + 0x08);
                int decryptionKey = OsuProcess.ReadInt32(modsObjectPointer + 0x0C);
                
                return (Mods) (encryptedValue ^ decryptionKey);
            }
        }

        public IEnumerable<HitObject> HitObjects 
        {
            get {
                UIntPtr hitObjectsPointer = (UIntPtr) OsuProcess.ReadInt32(PointerToBaseAddress + 0x48);
                UIntPtr hitObjectsListPointer = (UIntPtr) OsuProcess.ReadInt32(hitObjectsPointer + 0x04);

                for (int i = 0; i < OsuProcess.ReadInt32(hitObjectsListPointer + 0x04); i++) {
                    UIntPtr hitObjectPointer = (UIntPtr) OsuProcess.ReadInt32(hitObjectsListPointer + 0x08 + 0x04 * i);
                    
                    // Parse HitObjects
                    
                    HitObject ho = null;
                    yield return ho;
                }
            }
        }
    }
}