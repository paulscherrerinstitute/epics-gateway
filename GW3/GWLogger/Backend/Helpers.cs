using System;
using System.Collections.Generic;
using System.Linq;

namespace GWLogger.Backend
{
    public static class Helpers
    {
        // 
        private static DateTime _jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Converts a DateTime into a (JavaScript parsable) Int64.
        /// </summary>
        /// <param name="from">The DateTime to convert from</param>
        /// <returns>An integer value representing the number of milliseconds since 1 January 1970 00:00:00 UTC.</returns>
        public static long ToJsDate(this DateTime from)
        {
            return System.Convert.ToInt64((from - _jan1st1970).TotalMilliseconds);
        }

        /// <summary>
        /// Converts a (JavaScript parsable) Int64 into a DateTime.
        /// </summary>
        /// <param name="from">An integer value representing the number of milliseconds since 1 January 1970 00:00:00 UTC.</param>
        /// <returns>The date as a DateTime</returns>
        public static DateTime ToNetDate(this long from)
        {
            return _jan1st1970.AddMilliseconds(from).AddHours(TimeZoneInfo.Local.BaseUtcOffset.TotalHours + (TimeZoneInfo.Local.IsDaylightSavingTime(DateTime.UtcNow) ? 1 : 0));
        }

        public static DateTime Trim(this DateTime dateTime, TimeSpan interval = default(TimeSpan))
        {
            if (interval == default(TimeSpan))
                interval = TimeSpan.FromMinutes(10);
            return dateTime.AddTicks(-(dateTime.Ticks % interval.Ticks));
        }

        public static DateTime Round(this DateTime dateTime, TimeSpan interval = default(TimeSpan))
        {
            if (interval == default(TimeSpan))
                interval = TimeSpan.FromMinutes(10);
            var halfIntervalTicks = (interval.Ticks + 1) >> 1;
            return dateTime.AddTicks(halfIntervalTicks - ((dateTime.Ticks + halfIntervalTicks) % interval.Ticks));
        }

        public static DateTime TrimToSeconds(this DateTime date)
        {
            var ticks = TimeSpan.TicksPerSecond;
            return new DateTime(date.Ticks - (date.Ticks % ticks), date.Kind);
        }

        public static string JsEscape(this string source)
        {
            return source.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n","\\n").Replace("\r","\\r");
        }

        public static string Hostname(this string ip)
        {
            if (ip.Contains(':'))
                ip = ip.Split(':')[0];

            lock (knownHosts)
            {
                knownHosts.RemoveAll(row => (DateTime.UtcNow - row.LastChecked).TotalHours > 1);
                var known = knownHosts.FirstOrDefault(row => row.Ip == ip);
                if (known == null)
                {
                    known = new KnownHost { Ip = ip, Hostname = System.Net.Dns.GetHostEntry(ip).HostName };
                    knownHosts.Add(known);
                }
                return known.Hostname;
            }
        }

        private static List<KnownHost> knownHosts = new List<KnownHost>();
        private class KnownHost
        {
            public string Ip { get; set; }
            public string Hostname { get; set; }
            public DateTime LastChecked { get; } = DateTime.UtcNow;
        }

        public static IEnumerable<TType> Last<TType>(this IEnumerable<TType> src,int nbElements)
        {
            return src.Skip(Math.Max(0, src.Count() - nbElements));
        }
    }
}