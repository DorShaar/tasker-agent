using Newtonsoft.Json;
using TaskData.TaskStatus;
using Triangle;

namespace TaskerAgent.Domain.RepetitiveTasks
{
    [JsonObject(MemberSerialization.OptIn)]
    public class DailyRepetitiveMeasureableTask : GeneralRepetitiveMeasureableTask
    {
        internal DailyRepetitiveMeasureableTask(string id, string description,
            Frequency frequency,
            MeasureType measureType,
            int expected,
            int score) : base(id, description, frequency, measureType, expected, score)
        {
        }

        [JsonConstructor]
        internal DailyRepetitiveMeasureableTask(string id,
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