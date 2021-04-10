using Newtonsoft.Json;
using System.Collections.Generic;
using TaskData.TaskStatus;
using Triangle;

namespace TaskerAgent.Domain.RepetitiveTasks
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MonthlyRepetitiveMeasureableTask : GeneralRepetitiveMeasureableTask
    {
        [JsonProperty]
        public List<int> DaysOfMonth { get; } = new List<int>();

        internal MonthlyRepetitiveMeasureableTask(string id, string description,
            Frequency frequency,
            MeasureType measureType,
            List<int> daysOfMonth,
            int expected,
            int score) : base(id, description, frequency, measureType, expected, score)
        {
            if (daysOfMonth == null)
                return;

            foreach (int dayOfMonth in daysOfMonth)
            {
                if (dayOfMonth <= 0 || dayOfMonth >= 31)
                    continue;

                DaysOfMonth.Add(dayOfMonth);
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