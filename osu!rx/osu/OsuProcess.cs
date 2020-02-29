using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace osu_rx.osu
{
    public class OsuProcess
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, IntPtr dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        public Process Process { get; private set; }

        public OsuProcess(Process process) => Process = process;

        public IntPtr FindPattern(string pattern)
        {
            byte?[] patternBytes = parsePattern(pattern);

            long startAddress = (long)Process.MainModule.EntryPointAddress;
            long endAddress = startAddress + 0x100000000;

            long currentAddress = startAddress;

            byte[] buffer = new byte[8192];

            while (currentAddress < endAddress)
            {
                ReadMemory((IntPtr)currentAddress, buffer, 8192);
                IntPtr index = findMatch(patternBytes, buffer, buffer.Length);
                if (index != IntPtr.Zero)
                    return new IntPtr(currentAddress + (long)index);
                currentAddress += 8192;
            }

            return IntPtr.Zero;
        }

        public byte[] ReadMemory(IntPtr address, long size)
        {
            byte[] result = new byte[size];
            ReadProcessMemory(Process.Handle, address, result, (IntPtr)size, out IntPtr bytesRead);
            return result;
        }

        public IntPtr ReadMemory(IntPtr address, byte[] buffer, long size)
        {
            IntPtr bytesRead;
            ReadProcessMemory(Process.Handle, address, buffer, (IntPtr)size, out bytesRead);
            return bytesRead;
        }

        public void WriteMemory(IntPtr address, byte[] data, uint length)
        {
            WriteProcessMemory(Process.Handle, address, data, length, out UIntPtr bytesWritten);
        }

        public int ReadInt32(IntPtr address) => BitConverter.ToInt32(ReadMemory(address, sizeof(int)), 0);

        public uint ReadUInt32(IntPtr address) => BitConverter.ToUInt32(ReadMemory(address, sizeof(uint)), 0);

        public long ReadInt64(IntPtr address) => BitConverter.ToInt64(ReadMemory(address, sizeof(long)), 0);

        public ulong ReadUInt64(IntPtr address) => BitConverter.ToUInt64(ReadMemory(address, sizeof(ulong)), 0);

        public float ReadFloat(IntPtr address) => BitConverter.ToSingle(ReadMemory(address, sizeof(float)), 0);

        public double ReadDouble(IntPtr address) => BitConverter.ToDouble(ReadMemory(address, sizeof(double)), 0);

        public bool ReadBool(IntPtr address) => ReadInt32(address) == 1;

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

        private IntPtr findMatch(byte?[] pattern, byte[] source, long length)
        {
            bool found;
            for (int i = 0; i + pattern.Length <= length; i++)
            {
                found = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (pattern[j] == null)
                        continue;

                    if (source[i + j] != pattern[j])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                    return (IntPtr)i;
            }

            return IntPtr.Zero;
        }
    }
}
