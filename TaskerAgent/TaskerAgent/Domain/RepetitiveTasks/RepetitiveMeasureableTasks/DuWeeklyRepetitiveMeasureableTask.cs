using Newtonsoft.Json;
using System;
using TaskData.TaskStatus;
using Triangle;

namespace TaskerAgent.Domain.RepetitiveTasks.RepetitiveMeasureableTasks
{
    [JsonObject(MemberSerialization.OptIn)]
    public class DuWeeklyRepetitiveMeasureableTask : BaseRepetitiveMeasureableTask
    {
        [JsonProperty]
        public new Days OccurrenceDays { get; }

        internal DuWeeklyRepetitiveMeasureableTask(string id, string description,
            Frequency frequency,
            MeasureType measureType,
            Days occurrenceDays,
            int expected,
            int score) : base(id, description, frequency, measureType, expected, score)
        {
            OccurrenceDays = occurrenceDays == Days.EveryDay ? Days.Saturday : occurrenceDays;
        }

        [JsonConstructor]
        internal DuWeeklyRepetitiveMeasureableTask(string id,
            string groupName,
            string description,
            ITaskStatusHistory taskStatusHistory,
            TaskTriangle taskTriangle,
            Frequency frequency,
            MeasureType measureType,
            Days occurrenceDays,
            int expected,
            int actual,
            int score) : base(id, groupName, description, taskStatusHistory, taskTriangle, frequency, measureType, expected, actual, score)
        {
            OccurrenceDays = occurrenceDays;
        }

        public bool IsDayIsOneOfWeeklyOccurrence(DayOfWeek dayOfWeek)
        {
            Days day = FromDayOfWeek(dayOfWeek);
            return (day & OccurrenceDays) != 0;
        }
    }
}