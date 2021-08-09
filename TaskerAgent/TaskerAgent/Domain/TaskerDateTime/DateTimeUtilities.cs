using System;
using System.Collections.Generic;
using Triangle.Time;

namespace TaskerAgent.Domain.TaskerDateTime
{
    public static class DateTimeUtilities
    {
        private const int DefaultHour = 6;

        public static IEnumerable<DateTime> GetNextDaysDates(int nextDays)
        {
            for (int i = 0; i < nextDays; ++i)
            {
                yield return DateTime.Now.AddDays(1 + i);
            }
        }

        public static IEnumerable<DateTime> GetPreviousDaysDates(int previousDays)
        {
            for (int i = previousDays - 1; i >= 0; --i)
            {
                yield return DateTime.Now.AddDays(-i);
            }
        }

        public static IEnumerable<DateTime> GetDatesOfWeek(DateTime date = default)
        {
            if (date == default)
                date = DateTime.Now;

            DateTime startOfWeekDate = date.StartOfWeek();

            for (int i = 0; i < 7; ++i)
            {
                yield return startOfWeekDate.AddDays(i);
            }
        }

        private static DateTime StartOfWeek(this DateTime dateTime, DayOfWeek startOfWeek = DayOfWeek.Sunday)
        {
            int diff = (7 + (dateTime.DayOfWeek - startOfWeek)) % 7;
            return dateTime.AddDays(-1 * diff).Date;
        }

        /// <summary>
        /// Returnes the DateTime of the next given day at <see cref="DefaultHour"/>.
        /// </summary>
        /// <param name="startOfWeek"></param>
        public static DateTime GetNextDay(DayOfWeek startOfWeek)
        {
            DateTime date = DateTime.Now.Date;

            int diff = 7 - (date.DayOfWeek - startOfWeek);
            return date.AddDays(diff).AddHours(DefaultHour);
        }

        public static string ToDateName(this DateTime date)
        {
            return date.ToString(TimeConsts.TimeFormat).Replace('/', '-');
        }

        public static (DateTime startOfTheMonth, DateTime endOfTheMonth) GetThisMonthRange()
        {
            DateTime nowTime = DateTime.Now;

            DateTime startOfThisMonth = new DateTime(nowTime.Year, nowTime.Month, 1);
            DateTime endOfThisMonth = new DateTime(
                nowTime.Year, nowTime.Month, DateTime.DaysInMonth(nowTime.Year, nowTime.Month));

            return (startOfThisMonth, endOfThisMonth);
        }
    }
}