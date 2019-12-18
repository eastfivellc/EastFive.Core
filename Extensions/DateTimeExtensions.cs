using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive
{
    public static class DateTimeExtensions
    {
        #region Equality

        #region Seconds

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

        #region GreaterThanEqualToSecond

        public static bool GreaterThanEqualToSecond(this DateTime time1, DateTime time2)
        {
            var delta = time1 - time2;
            return (delta.TotalSeconds > -1.0);
        }

        #endregion

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

        #region Misc
        public static int BirthDateToAge(this DateTime dateOfBirth)
        {
            int years = DateTime.Now.Year - dateOfBirth.Year;
            if ((dateOfBirth.Month > DateTime.Now.Month) || (dateOfBirth.Month == DateTime.Now.Month && dateOfBirth.Day > DateTime.Now.Day))
                years--;
            return years;
        }

        public static DateTime AsKindUnspecified(this DateTime date)
        {
            // Printing Kind=Unspecified for ISO formatting looks like this (without any Z or -0:00 formatting):
            // 2019-12-18T00:00:00.0000000
            // A javascript client will assume this time is in whatever local time it has and not attempt any conversion
            // which is especially helpful for a date-only DateTime where we want what is sent to be displayed the same.
            return new DateTime(date.Ticks, DateTimeKind.Unspecified);
        }
        #endregion Misc

        #endregion

        #region Comparison

        public static DateTime Min(this DateTime time1, DateTime time2)
        {
            if (time1 < time2)
                return time1;
            return time2;
        }

        public static DateTime Max(this DateTime time1, DateTime time2)
        {
            if (time1 > time2)
                return time1;
            return time2;
        }

        #endregion
    }
}
