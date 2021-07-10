using Newtonsoft.Json;
using System.Collections.Generic;
using TaskData.TaskStatus;
using Triangle;

namespace TaskerAgent.Domain.RepetitiveTasks.RepetitiveMeasureableTasks
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MonthlyRepetitiveMeasureableTask : BaseRepetitiveMeasureableTask
    {
        private const int DefaultDayInMonth = 28;

        [JsonProperty]
        public List<int> DaysOfMonth { get; } = new List<int>();

        internal MonthlyRepetitiveMeasureableTask(string id, string description,
            Frequency frequency,
            MeasureType measureType,
            List<int> daysOfMonth,
            int expected,
            int score) : base(id, description, frequency, measureType, expected, score)
        {
            if (daysOfMonth == null || daysOfMonth.Count == 0)
            {
                AddDefaultDaysOfMonth(expected);
                return;
            }

            foreach (int dayOfMonth in daysOfMonth)
            {
                if (dayOfMonth <= 0 || dayOfMonth >= 31)
                    continue;

                if (DaysOfMonth.Contains(dayOfMonth))
                    continue;

                DaysOfMonth.Add(dayOfMonth);
            }
        }

        private void AddDefaultDaysOfMonth(int expected)
        {
            while (expected > 0)
            {
                DaysOfMonth.Add(DefaultDayInMonth - expected + 1);
                expected--;
            }
        }

        [JsonConstructor]
        internal MonthlyRepetitiveMeasureableTask(string id,
            string groupName,
            string description,
            ITaskStatusHistory taskStatusHistory,
            TaskTriangle taskTriangle,
            Frequency frequency,
            MeasureType measureType,
            List<int> daysOfMonth,
            int expected,
            int actual,
            int score) : base(id, groupName, description, taskStatusHistory, taskTriangle, frequency, measureType, expected, actual, score)
        {
            DaysOfMonth = daysOfMonth;
        }

        public bool IsDayIsOneOfMonthlyOccurrence(int dayOfMonth)
        {
            return DaysOfMonth.Contains(dayOfMonth);
        }
    }
}