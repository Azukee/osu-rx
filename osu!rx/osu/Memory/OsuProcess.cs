using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace osu_rx.osu.Memory
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

        public bool ReadBool(IntPtr address) => BitConverter.ToBoolean(ReadMemory(address, sizeof(bool)), 0);

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

        #region ThreadStack0 stuff
        //https://gist.github.com/baratgabor/06aef4815226eedc8ef5052e595b3ca4
        public IntPtr GetThreadStack0Address()
        {
            MODULEINFO moduleInfo = getModuleInfo();
            NT_TIB tib = getTIB();

            byte[] StackBaseSample = ReadMemory((IntPtr)(tib.StackBase - 4096), 4096);

            int i = 0;
            for (i = (StackBaseSample.Length / 4) - 1; i >= 0; --i)
            {
                uint valueAtPosition = BitConverter.ToUInt32(StackBaseSample, i * 4);
                if (valueAtPosition >= moduleInfo.lpBaseOfDll &&
                    valueAtPosition <= moduleInfo.lpBaseOfDll + moduleInfo.SizeOfImage)
                    break;
            }

            if (i == 0)
                throw new Exception("ThreadStack0 not found!");

            return (IntPtr)(tib.StackBase - 4096 + i * 4);
        }

        private MODULEINFO getModuleInfo()
        {
            IntPtr moduleHandle = GetModuleHandle("kernel32.dll");

            if (moduleHandle == null)
                throw new Exception();

            MODULEINFO moduleInfo = new MODULEINFO();

            var result = GetModuleInformation(Process.Handle, moduleHandle, out moduleInfo, (uint)Marshal.SizeOf(moduleInfo));

            if (!result)
                throw new Exception();

            return moduleInfo;
        }

        private NT_TIB getTIB()
        {
            THREAD_BASIC_INFORMATION tbi = getTBI();

            GCHandle pinned = GCHandle.Alloc(ReadMemory((IntPtr)tbi.TebBaseAddress, Marshal.SizeOf(typeof(NT_TIB))), GCHandleType.Pinned);
            NT_TIB tib = (NT_TIB)Marshal.PtrToStructure(pinned.AddrOfPinnedObject(), typeof(NT_TIB));
            pinned.Free();

            return tib;
        }

        private THREAD_BASIC_INFORMATION getTBI()
        {
            IntPtr hThread = OpenThread(ThreadAccess.QueryInformation, false, (uint)Process.Threads.OfType<ProcessThread>().First().Id);

            if (hThread == null)
                throw new Exception();

            try
            {
                THREAD_BASIC_INFORMATION tbi = new THREAD_BASIC_INFORMATION();
                int result = NtQueryInformationThread(hThread, ThreadInfoClass.ThreadBasicInformation, out tbi, (uint)Marshal.SizeOf(tbi), IntPtr.Zero);

                if (result != 0)
                    throw new Exception();

                return tbi;
            }
            finally
            {
                CloseHandle(hThread);
            }
        }

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool GetModuleInformation(IntPtr hProcess, IntPtr hModule, out MODULEINFO lpmodinfo, uint cb);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtQueryInformationThread(
            IntPtr threadHandle,
            ThreadInfoClass threadInformationClass,
            out THREAD_BASIC_INFORMATION threadInformation,
            ulong threadInformationLength,
            IntPtr returnLengthPtr);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private enum ThreadInfoClass : int
        {
            ThreadBasicInformation = 0,
            ThreadQuerySetWin32StartAddress = 9
        }

        [Flags]
        private enum ThreadAccess : int
        {
            Terminate = 0x0001,
            SuspendResume = 0x0002,
            GetContext = 0x0008,
            SetContext = 0x0010,
            SetInformation = 0x0020,
            QueryInformation = 0x0040,
            SetThreadToken = 0x0080,
            Impersonate = 0x0100,
            DirectImpersonation = 0x0200
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct THREAD_BASIC_INFORMATION
        {
            public uint ExitStatus;
            public uint TebBaseAddress;
            public CLIENT_ID ClientId;
            public uint AffinityMask;
            public uint Priority;
            public uint BasePriority;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CLIENT_ID
        {
            public uint UniqueProcess;
            public uint UniqueThread;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NT_TIB
        {
            public uint ExceptionListPointer;
            public uint StackBase;
            public uint StackLimit;
            public uint SubSystemTib;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MODULEINFO
        {
            public uint lpBaseOfDll;
            public uint SizeOfImage;
            public uint EntryPoint;
        }
        #endregion
    }
}
