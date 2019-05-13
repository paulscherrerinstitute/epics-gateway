using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
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

        private static string ProcessOutput(string command, string arguments = null)
        {
            using (var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            })
            {
                proc.Start();
                return proc.StandardOutput.ReadToEnd();
            }
        }

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
                var vmstat = ProcessOutput("/bin/iostat").Split(new char[] { '\n' })[3].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(row => double.Parse(row)).ToArray();
                return 100 - vmstat[5];
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
                var freeinfo = ProcessOutput("/bin/free").Split(new char[] { '\n' })[1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(row => ulong.Parse(row)).ToArray();
                total = freeinfo[0];
                available = freeinfo[2];
                var used = total - available;
                return 100.0 * used / total;
            }
        }

        static long? lastBytesIn = null;
        public static long TotalNetworkIn()
        {
            long result = 0;
            long v;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                v = NetworkInterface.GetAllNetworkInterfaces().Sum(row => row.GetIPv4Statistics().BytesReceived);
                if (lastBytesIn.HasValue)
                    result = (int)(v - lastBytesIn.Value);
                lastBytesIn = v;
                return result;
            }
            else
            {
                var ifstat = ProcessOutput("/sbin/ifstat").Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Skip(3).ToArray();
                v = 0;
                for (var i = 0; i < ifstat.Length; i += 2)
                    v += ifstat[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(row => row.EndsWith("K") ? long.Parse(row.Replace("K", "")) * 1000 : long.Parse(row)).ToArray()[4];
                return v;
            }
        }

        static long? lastBytesOut = null;
        public static long TotalNetworkOut()
        {
            long result = 0;
            long v;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                v = NetworkInterface.GetAllNetworkInterfaces().Sum(row => row.GetIPv4Statistics().BytesSent);
                if (lastBytesOut.HasValue)
                    result = (int)(v - lastBytesIn.Value);
                lastBytesOut = v;
                return result;
            }
            else
            {
                var ifstat = ProcessOutput("/sbin/ifstat").Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Skip(3).ToArray();
                v = 0;
                for (var i = 0; i < ifstat.Length; i += 2)
                    v += ifstat[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(row => row.EndsWith("K") ? long.Parse(row.Replace("K", "")) * 1000 : long.Parse(row)).ToArray()[6];
                return v;
            }
        }

        private static ulong ULong(FileTime filetime)
        {
            return ((ulong)filetime.dwHighDateTime << 32) + (uint)filetime.dwLowDateTime;
        }
    }
}
