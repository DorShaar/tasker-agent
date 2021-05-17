using System;
using System.Collections.Generic;

namespace TaskerAgent.Infra.TaskerDateTime
{
    public static class DateTimeUtilities
    {
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

        private static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek = DayOfWeek.Sunday)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }
    }
}