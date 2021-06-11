﻿using System;
using System.Linq;

using EastFive;
using EastFive.Linq;
using EastFive.Collections.Generic;

namespace EastFive.Extensions
{
    public static class TimeZoneInformationExtensions
    {
        public static TimeZoneInfo FindSystemTimeZone(this string timeZoneId)
        {
            if (System.Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                }
                catch (TimeZoneNotFoundException)
                {
                    return TimeZoneInfo.Local;
                }
            }

            return TimeZoneInfo.GetSystemTimeZones()
                .Where(tz => tz.DisplayName.Equals(timeZoneId))
                .First(
                    (tzi, next) => tzi,
                    () => TimeZoneInfo.Local);
        }
    }
}
