using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using static osu_rx.osu.Memory.OsuProcess;

namespace osu_rx.osu.Memory
{
    public class OsuProcess
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
        {
            public UIntPtr BaseAddress;
            public UIntPtr AllocationBase;
            public uint AllocationProtect;
            public UIntPtr RegionSize;
            public MemoryState State;
            public MemoryProtect Protect;
            public MemoryType Type;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer, uint dwSize, out UIntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        //TODO: x64 support
        [DllImport("kernel32.dll")]
        public static extern int VirtualQueryEx(IntPtr hProcess, UIntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        public Process Process { get; private set; }

        public OsuProcess(Process process) => Process = process;

        public bool FindPattern(string pattern, out UIntPtr result)
        {
            byte?[] patternBytes = parsePattern(pattern);

            var regions = EnumerateMemoryRegions();
            foreach (var region in regions)
            {
                if ((uint)region.BaseAddress < (uint)Process.MainModule.BaseAddress)
                    continue;

                byte[] buffer = ReadMemory(region.BaseAddress, region.RegionSize.ToUInt32());
                if (findMatch(patternBytes, buffer) is var match && match != UIntPtr.Zero)
                {
                    result = (UIntPtr)(region.BaseAddress.ToUInt32() + match.ToUInt32());
                    return true;
                }
            }

            result = UIntPtr.Zero;
            return false;
        }

        public List<MemoryRegion> EnumerateMemoryRegions()
        {
            var regions = new List<MemoryRegion>();
            UIntPtr address = UIntPtr.Zero;

            while (VirtualQueryEx(Process.Handle, address, out MEMORY_BASIC_INFORMATION basicInformation, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION))) != 0)
            {
                if (basicInformation.State != MemoryState.MemFree && !basicInformation.Protect.HasFlag(MemoryProtect.PageGuard))
                    regions.Add(new MemoryRegion(basicInformation));

                address = (UIntPtr)(basicInformation.BaseAddress.ToUInt32() + basicInformation.RegionSize.ToUInt32());
            }

            return regions;
        }

        public byte[] ReadMemory(UIntPtr address, uint size)
        {
            byte[] result = new byte[size];
            ReadProcessMemory(Process.Handle, address, result, size, out UIntPtr bytesRead);
            return result;
        }

        public UIntPtr ReadMemory(UIntPtr address, byte[] buffer, uint size)
        {
            UIntPtr bytesRead;
            ReadProcessMemory(Process.Handle, address, buffer, size, out bytesRead);
            return bytesRead;
        }

        public void WriteMemory(UIntPtr address, byte[] data, uint length)
        {
            WriteProcessMemory(Process.Handle, address, data, length, out UIntPtr bytesWritten);
        }

        public int ReadInt32(UIntPtr address) => BitConverter.ToInt32(ReadMemory(address, sizeof(int)), 0);

        public uint ReadUInt32(UIntPtr address) => BitConverter.ToUInt32(ReadMemory(address, sizeof(uint)), 0);

        public long ReadInt64(UIntPtr address) => BitConverter.ToInt64(ReadMemory(address, sizeof(long)), 0);

        public ulong ReadUInt64(UIntPtr address) => BitConverter.ToUInt64(ReadMemory(address, sizeof(ulong)), 0);

        public float ReadFloat(UIntPtr address) => BitConverter.ToSingle(ReadMemory(address, sizeof(float)), 0);

        public double ReadDouble(UIntPtr address) => BitConverter.ToDouble(ReadMemory(address, sizeof(double)), 0);

        public bool ReadBool(UIntPtr address) => BitConverter.ToBoolean(ReadMemory(address, sizeof(bool)), 0);

        public string ReadString(UIntPtr address, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            UIntPtr stringAddress = (UIntPtr)ReadInt32(address);
            int length = ReadInt32(stringAddress + 0x4) * (encoding == Encoding.UTF8 ? 2 : 1);

            return encoding.GetString(ReadMemory(stringAddress + 0x8, (uint)length)).Replace("\0", string.Empty);
        }

        private byte?[] parsePattern(string pattern)
        {
            byte?[] patternBytes = new byte?[pattern.Split(' ').Length];
            for (int i = 0; i < patternBytes.Length; i++)
            {
                string currentByte = pattern.Split(' ')[i];
                if (currentByte != "??")
                    patternBytes[i] = Convert.ToByte(currentByte, 16);
                else
                    patternBytes[i] = null;
            }

            return patternBytes;
        }

        private UIntPtr findMatch(byte?[] pattern, byte[] buffer)
        {
            bool found;
            for (int i = 0; i + pattern.Length <= buffer.Length; i++)
            {
                found = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (pattern[j] == null || pattern[j] == buffer[i + j])
                        continue;

                    found = false;
                    break;
                }

                if (found)
                    return (UIntPtr)i;
            }

            return UIntPtr.Zero;
        }
    }

    public class MemoryRegion
    {
        public UIntPtr BaseAddress { get; private set; }
        public UIntPtr RegionSize { get; private set; }
        public UIntPtr Start { get; private set; }
        public UIntPtr End { get; private set; }
        public MemoryState State { get; private set; }
        public MemoryProtect Protect { get; private set; }
        public MemoryType Type { get; private set; }

        public MemoryRegion(MEMORY_BASIC_INFORMATION basicInformation)
        {
            BaseAddress = basicInformation.BaseAddress;
            RegionSize = basicInformation.RegionSize;
            State = basicInformation.State;
            Protect = basicInformation.Protect;
            Type = basicInformation.Type;
        }
    }

    public enum MemoryState
    {
        MemCommit = 0x1000,
        MemReserved = 0x2000,
        MemFree = 0x10000
    }

    public enum MemoryType
    {
        MemPrivate = 0x20000,
        MemMapped = 0x40000,
        MemImage = 0x1000000
    }

    public enum MemoryProtect
    {
        PageNoAccess = 0x00000001,
        PageReadonly = 0x00000002,
        PageReadWrite = 0x00000004,
        PageWriteCopy = 0x00000008,
        PageExecute = 0x00000010,
        PageExecuteRead = 0x00000020,
        PageExecuteReadWrite = 0x00000040,
        PageExecuteWriteCopy = 0x00000080,
        PageGuard = 0x00000100,
        PageNoCache = 0x00000200,
        PageWriteCombine = 0x00000400
    }
}
