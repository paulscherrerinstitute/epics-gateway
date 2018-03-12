using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GWLogger.Backend
{
    public static class Helpers
    {
        public static DateTime Round(this DateTime dateTime, TimeSpan interval = default(TimeSpan))
        {
            if (interval == default(TimeSpan))
                interval = TimeSpan.FromMinutes(10);
            var halfIntervalTicks = (interval.Ticks + 1) >> 1;
            return dateTime.AddTicks(halfIntervalTicks - ((dateTime.Ticks + halfIntervalTicks) % interval.Ticks));
        }
    }
}