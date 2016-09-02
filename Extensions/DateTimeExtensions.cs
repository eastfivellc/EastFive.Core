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

        #region EqualToSecond

        public static bool EqualToSecond(this DateTime time1, DateTime time2)
        {
            return
                time1.EqualToMinute(time2) &&
                time1.Second == time2.Second;
        }

        public static bool EqualToSecond(this DateTime? time1, DateTime? time2)
        {
            if (time1.HasValue != time2.HasValue)
                return false;
            if (!time1.HasValue)
                return false;
            return time1.Value.EqualToSecond(time2);
        }

        public static bool EqualToSecond(this DateTime time1, DateTime? time2)
        {
            if (!time2.HasValue)
                return false;
            return time1.EqualToSecond(time2.Value);
        }

        public static bool EqualToSecond(this DateTime? time1, DateTime time2)
        {
            return time2.EqualToSecond(time1);
        }

        #endregion

        #region EqualToMinute

        public static bool EqualToMinute(this DateTime time1, DateTime time2)
        {
            return
                time1.EqualToDay(time2) &&
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
        
        #region EqualToDay

        public static bool EqualToDay(this DateTime time1, DateTime time2)
        {
            return
                time1.Year == time2.Year &&
                time1.DayOfYear == time2.DayOfYear;
        }

        public static bool EqualToDay(this DateTime? time1, DateTime? time2)
        {
            if (time1.HasValue != time2.HasValue)
                return false;
            if (!time1.HasValue)
                return false;
            return time1.Value.EqualToDay(time2);
        }

        public static bool EqualToDay(this DateTime time1, DateTime? time2)
        {
            if (!time2.HasValue)
                return false;
            return time1.EqualToDay(time2.Value);
        }

        public static bool EqualToDay(this DateTime? time1, DateTime time2)
        {
            return time2.EqualToDay(time1);
        }

        #endregion

        #endregion
    }
}
