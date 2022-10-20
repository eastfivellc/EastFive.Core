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

        #region EqualToDay

        public static bool EqualToMonth(this DateTime time1, DateTime time2)
        {
            return
                time1.Year == time2.Year &&
                time1.Month == time2.Month;
        }

        public static bool EqualToMonth(this DateTime? time1, DateTime? time2)
        {
            if (time1.HasValue != time2.HasValue)
                return false;
            if (!time1.HasValue)
                return false;
            return time1.Value.EqualToMonth(time2);
        }

        public static bool EqualToMonth(this DateTime time1, DateTime? time2)
        {
            if (!time2.HasValue)
                return false;
            return time1.EqualToMonth(time2.Value);
        }

        public static bool EqualToMonth(this DateTime? time1, DateTime time2)
        {
            return time2.EqualToMonth(time1);
        }

        #endregion

        #endregion

        #region Misc

        public static int BirthDateToAge(this DateTime dateOfBirth, DateTime? asOfMaybe = default)
        {
            var asOf = asOfMaybe.HasValue ? asOfMaybe.Value : DateTime.Now;
            int years = asOf.Year - dateOfBirth.Year;
            if ((dateOfBirth.Month > asOf.Month) || (dateOfBirth.Month == asOf.Month && dateOfBirth.Day > asOf.Day))
                years--;
            return years;
        }

        public static string BirthDateUnder2Formatted(this DateTime dateOfBirth)
        {
            // under two:  1m23do  (1 month 23 days old)
            var age = dateOfBirth.BirthDateToAge();
            if (age >= 2)
                return $"{age}yo";

            var asOf = DateTime.Today;
            var months = 0;
            while (asOf >= dateOfBirth.AddMonths(months + 1))
                months++;

            var days = 0;
            while (asOf >= dateOfBirth.AddMonths(months).AddDays(days + 1))
                days++;

            return $"{months}m{days}do";
        }

        public static DateTime AsKindUnspecified(this DateTime date)
        {
            // Printing Kind=Unspecified for ISO formatting looks like this (without any Z or -0:00 formatting):
            // 2019-12-18T00:00:00.0000000
            // A javascript client will assume this time is in whatever local time it has and not attempt any conversion
            // which is especially helpful for a date-only DateTime where we want what is sent to be displayed the same.
            return new DateTime(date.Ticks, DateTimeKind.Unspecified);
        }

        public static DateTime GetDayOfWeek(this DateTime date, DayOfWeek day)
        {
            // Sunday = 0
            var sunday = date.Date.AddDays(-(int)date.DayOfWeek);
            return sunday.AddDays((int)day);
        }

        public static DateTime GetFirstOfMonth(this DateTime date)
        {
            return new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        public static DateTime GetLastOfMonth(this DateTime date)
        {
            return date.AddMonths(1).GetFirstOfMonth().AddDays(-1);
        }

        // Taken from  https://www.codeproject.com/Tips/1168428/US-Federal-Holidays-Csharp
        public static bool IsFederalHoliday(this DateTime date)
        {
            // No holidays in March, April, June, August
            if (date.Month == 3 || date.Month == 4 || date.Month == 6 || date.Month == 8)
                return false;

            int nthWeekDay = (int)(Math.Ceiling((double)date.Day / 7.0d));
            DayOfWeek dayName = date.DayOfWeek;
            bool isThursday = dayName == DayOfWeek.Thursday;
            bool isFriday = dayName == DayOfWeek.Friday;
            bool isMonday = dayName == DayOfWeek.Monday;
            bool isWeekend = dayName == DayOfWeek.Saturday || dayName == DayOfWeek.Sunday;

            // New Years Day (Jan 1, or preceding Friday/following Monday if weekend)
            if ((date.Month == 12 && date.Day == 31 && isFriday) ||
                (date.Month == 1 && date.Day == 1 && !isWeekend) ||
                (date.Month == 1 && date.Day == 2 && isMonday)) return true;

            // MLK day (3rd monday in January)
            if (date.Month == 1 && isMonday && nthWeekDay == 3) return true;

            // President’s Day (3rd Monday in February)
            if (date.Month == 2 && isMonday && nthWeekDay == 3) return true;

            // Memorial Day (Last Monday in May)
            if (date.Month == 5 && isMonday && date.AddDays(7).Month == 6) return true;

            // Independence Day (July 4, or preceding Friday/following Monday if weekend)
            if ((date.Month == 7 && date.Day == 3 && isFriday) ||
                (date.Month == 7 && date.Day == 4 && !isWeekend) ||
                (date.Month == 7 && date.Day == 5 && isMonday)) return true;

            // Labor Day (1st Monday in September)
            if (date.Month == 9 && isMonday && nthWeekDay == 1) return true;

            // Columbus Day (2nd Monday in October)
            if (date.Month == 10 && isMonday && nthWeekDay == 2) return true;

            // Veteran’s Day (November 11, or preceding Friday/following Monday if weekend))
            if ((date.Month == 11 && date.Day == 10 && isFriday) ||
                (date.Month == 11 && date.Day == 11 && !isWeekend) ||
                (date.Month == 11 && date.Day == 12 && isMonday)) return true;

            // Thanksgiving Day (4th Thursday in November)
            if (date.Month == 11 && isThursday && nthWeekDay == 4) return true;

            // Christmas Day (December 25, or preceding Friday/following Monday if weekend))
            if ((date.Month == 12 && date.Day == 24 && isFriday) ||
                (date.Month == 12 && date.Day == 25 && !isWeekend) ||
                (date.Month == 12 && date.Day == 26 && isMonday)) return true;

            return false;
        }

        #endregion Misc

        #region Hash

        public static int HashToDay(this DateTime date)
            => (date.Year * 1000) + date.DayOfYear;

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

        public static bool IsInWindow(this DateTime when, DateTime start, DateTime end)
        {
            if (start > end)
                throw new ArgumentException($"Start time is greater than end time. {start} > {end}");

            if (when < start)
                return false;

            if (when > end)
                return false;

            return true;
        }

        #endregion
    }
}
