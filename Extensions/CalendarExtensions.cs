using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DDay.iCal;

namespace BlackBarLabs.Core
{
    public static class CalendarExtensions
    {
        public static IICalendar LoadFirstCalendar<TResult>(this Stream icsStream, Func<string, TResult> onFailure)
        {
            IICalendarCollection calendarCollection = null;
            try
            {
                calendarCollection = iCalendar.LoadFromStream(icsStream, Encoding.UTF8);
                if (calendarCollection.Count == 0)
                    onFailure("No calendar found in ICS file");
                var firstCalendar = calendarCollection[0];
                if (default(IICalendar) == firstCalendar)
                    onFailure("Calendar in ICS file is invalid.");
                if (null == firstCalendar.Events)
                    onFailure("Calendar has no events.");
                if (firstCalendar.Events.Count == 0)
                    onFailure("Calendar has no events.");
                return firstCalendar;
            }
            catch (Exception)
            { 
                onFailure("ics file does not contain any calendars");
            }
            return null;
        }
        public static IDateTime GetStartTimeOfFirstEvent<TResult>(this IICalendar calendar, Func<string, TResult> onFailure)
        {
            try
            {
                return calendar.Events[0].Start;
            }
            catch (Exception)
            {
                onFailure("Could not find start event.");
            }
            return null;
        }

        public static void ValidateIcsFile<TResult>(this Stream icsStream, Func<string, TResult> onFailure)
        {
            icsStream.LoadFirstCalendar(onFailure)
                .GetStartTimeOfFirstEvent(onFailure);
        }
    }
}
