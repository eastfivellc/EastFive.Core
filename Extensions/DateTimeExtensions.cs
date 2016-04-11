using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackBarLabs.Core
{
    public static class DateTimeExtensions
    {
        #region Equality

        #region EqualToMinute

        public static bool EqualToMinute(this DateTime time1, DateTime time2)
        {
            return
                time1.DayOfYear == time2.DayOfYear &&
                time1.Hour == time2.Hour &&
                time1.Minute == time2.Minute;
        }

        public static bool EqualToMinute(this DateTime? time1, DateTime? time2)
        {
            if (time1.HasValue != time2.HasValue)
                return false;
            if (!time1.HasValue)
                return false;
            return time1.Value.EqualToMinute(time2);
        }
        
        public static bool EqualToMinute(this DateTime time1, DateTime? time2)
        {
            if (!time2.HasValue)
                return false;
            return time1.EqualToMinute(time2.Value);
        }

        public static bool EqualToMinute(this DateTime? time1, DateTime time2)
        {
            return time2.EqualToMinute(time1);
        }

        #endregion

        #endregion
    }
}
