using Newtonsoft.Json;
using TaskData.TaskStatus;
using Triangle;

namespace TaskerAgent.Domain.RepetitiveTasks
{
    [JsonObject(MemberSerialization.OptIn)]
    public class WeeklyRepetitiveMeasureableTask : GeneralRepetitiveMeasureableTask
    {
        [JsonProperty]
        new public Days OccurrenceDays { get; }

        internal WeeklyRepetitiveMeasureableTask(string id, string description,
            Frequency frequency,
            MeasureType measureType,
            int expected,
            int score) : base(id, description, frequency, measureType, expected, score)
        {
        }

        [JsonConstructor]
        internal WeeklyRepetitiveMeasureableTask(string id,
            string groupName,
            string description,
            ITaskStatusHistory taskStatusHistory,
            TaskTriangle taskTriangle,
            Frequency frequency,
            MeasureType measureType,
            int expected,
            int actual,
            int score) : base(id, groupName, description, taskStatusHistory, taskTriangle, frequency, measureType, expected, actual, score)
        {
        }
    }
}