using Newtonsoft.Json;
using System;
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
        private IMeasureableTask mMeasureableTask;

        [JsonProperty]
        public Frequency Frequency { get; private set; }

        public MeasureType MeasureType => mMeasureableTask.MeasureType;

        public int Expected => mMeasureableTask.Expected;

        public int Actual => mMeasureableTask.Actual;

        public int Score => mMeasureableTask.Score;

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
            IMeasureableTask measureableTask) : base(id, groupName, description, taskStatusHistory, taskTriangle)
        {
            Frequency = frequency;
            mMeasureableTask = measureableTask ?? throw new ArgumentNullException(nameof(measureableTask));
        }

        public void InitializeRepetitiveMeasureableTask(Frequency frequency, IMeasureableTask measureableTask)
        {
            Frequency = frequency;
            mMeasureableTask = measureableTask ?? throw new ArgumentNullException(nameof(measureableTask));
        }
    }
}