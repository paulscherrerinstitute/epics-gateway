using System;
using System.Runtime.InteropServices;
using FileTime = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace GatewayLogic
{
    public class DiagnosticInfo
    {
        public struct MemoryStatusEx
        {
            public UInt32 Length;
            public UInt32 MemoryLoad;
            public UInt64 TotalPhysical;
            public UInt64 AvailablePhysical;
            public UInt64 TotalPageFile;
            public UInt64 AvailablePageFile;
            public UInt64 TotalVirtualMemory;
            public UInt64 AvailableVirtual;
            public UInt64 AvailableExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(
            out FileTime lpIdleTime,
            out FileTime lpKernelTime,
            out FileTime lpUserTime
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(
            [In, Out] IntPtr lpBuffer
        );

        private static MemoryStatusEx GlobalMemoryStatusEx()
        {
            MemoryStatusEx buffer = new MemoryStatusEx();
            var size = Marshal.SizeOf(buffer);
            buffer.Length = (uint)size;
            IntPtr bufPtr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(buffer, bufPtr, false);
                if (!GlobalMemoryStatusEx(bufPtr))
                    throw new Exception("Native call to GlobalMemoryStatusEx() failed");
                buffer = (MemoryStatusEx)Marshal.PtrToStructure(bufPtr, typeof(MemoryStatusEx));
            }
            finally
            {
                Marshal.FreeHGlobal(bufPtr);
            }
            return buffer;
        }

        private static ulong lastIdleTime;
        private static ulong lastKernelTime;
        private static ulong lastUserTime;

        public static double GetCPUUsage()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                FileTime f_idleTime, f_kernelTime, f_userTime;
                if (!GetSystemTimes(out f_idleTime, out f_kernelTime, out f_userTime))
                    throw new Exception("Native call to GetSystemTimes() failed");
                var idleTime = ULong(f_idleTime);
                var kernelTime = ULong(f_kernelTime);
                var userTime = ULong(f_userTime);

                var usr = userTime - lastUserTime;
                var ker = kernelTime - lastKernelTime;
                var idl = idleTime - lastIdleTime;

                var sys = ker + usr;
                double cpu = (double)((sys - idl) * 100) / sys;

                lastIdleTime = idleTime;
                lastKernelTime = kernelTime;
                lastUserTime = userTime;
                return cpu;
            }
            else
            {
                return 0.0;
            }
        }

        public static double GetMemoryUsage()
        {
            UInt64 a, b;
            return GetMemoryUsage(out a, out b);
        }

        public static double GetMemoryUsage(out UInt64 total, out UInt64 available)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var res = GlobalMemoryStatusEx();
                var a = res.AvailablePhysical;
                var t = res.TotalPhysical;
                total = t / 1000000;
                available = a / 1000000;
                var used = t - a;
                return 100 * used / (double)t;
            }
            else
            {
                total = 0;
                available = 0;
                return 0.0;
            }
        }

        private static ulong ULong(FileTime filetime)
        {
            return ((ulong)filetime.dwHighDateTime << 32) + (uint)filetime.dwLowDateTime;
        }

    }
}
