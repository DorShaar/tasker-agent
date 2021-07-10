using Newtonsoft.Json;
using System;
using TaskData.TaskStatus;
using TaskData.WorkTasks;
using TaskerAgent.App.RepetitiveTasks;
using Triangle;

namespace TaskerAgent.Domain.RepetitiveTasks.RepetitiveMeasureableTasks
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class BaseRepetitiveMeasureableTask : WorkTask, IRepetitiveMeasureableTask
    {
        [JsonProperty]
        public Frequency Frequency { get; }

        [JsonProperty]
        public Days OccurrenceDays => Days.EveryDay;

        [JsonProperty]
        public MeasureType MeasureType { get; set; }

        [JsonProperty]
        public int Expected { get; set; }

        [JsonProperty]
        public int Actual { get; set; }

        [JsonProperty]
        public int Score { get; set; }

        internal BaseRepetitiveMeasureableTask(string id, string description,
            Frequency frequency,
            MeasureType measureType,
            int expected,
            int score) : base(id, description)
        {
            Frequency = frequency;
            MeasureType = measureType;

            if (expected < 1)
                throw new ArgumentException($"{nameof(expected)} must be positive integer number");

            Expected = expected;

            if (score < 1)
                throw new ArgumentException($"{nameof(score)} must be positive integer number");
            Score = score;
        }

        [JsonConstructor]
        internal BaseRepetitiveMeasureableTask(string id,
            string groupName,
            string description,
            ITaskStatusHistory taskStatusHistory,
            TaskTriangle taskTriangle,
            Frequency frequency,
            MeasureType measureType,
            int expected,
            int actual,
            int score) : base(id, groupName, description, taskStatusHistory, taskTriangle)
        {
            Frequency = frequency;
            MeasureType = measureType;
            Expected = expected;
            Actual = actual;
            Score = score;
        }

        public Days FromDayOfWeek(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Sunday => Days.Sunday,
                DayOfWeek.Monday => Days.Monday,
                DayOfWeek.Tuesday => Days.Tuesday,
                DayOfWeek.Wednesday => Days.Wednesday,
                DayOfWeek.Thursday => Days.Thursday,
                DayOfWeek.Friday => Days.Friday,
                DayOfWeek.Saturday => Days.Saturday,
                _ => Days.EveryDay,
            };
        }
    }
}