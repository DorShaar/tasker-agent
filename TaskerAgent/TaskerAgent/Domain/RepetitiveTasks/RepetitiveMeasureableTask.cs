using Newtonsoft.Json;
using TaskData.TaskStatus;
using TaskData.WorkTasks;
using TaskerAgent.App.RepetitiveTasks;
using Triangle;

namespace TaskerAgent.Domain.RepetitiveTasks
{
    [JsonObject(MemberSerialization.OptIn)]
    public class RepetitiveMeasureableTask : WorkTask, IRepetitiveMeasureableTask
    {
        [JsonProperty]
        public Frequency Frequency { get; set; }

        [JsonProperty]
        public MeasureType MeasureType { get; set; }

        [JsonProperty]
        public int Expected { get; set; }

        [JsonProperty]
        public int Actual { get; set; }

        [JsonProperty]
        public int Score { get; set; }

        internal RepetitiveMeasureableTask(string id, string description): base(id, description)
        {
        }

        [JsonConstructor]
        internal RepetitiveMeasureableTask(string id,
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

        public void InitializeRepetitiveMeasureableTask(
            Frequency frequency,
            MeasureType measureType,
            int expected,
            int score)
        {
            Frequency = frequency;
            MeasureType = measureType;
            Expected = expected;
            Score = score;
        }
    }
}