using TaskData.WorkTasks;
using TaskerAgent.App.RepetitiveTasks;

namespace TaskerAgent.Domain.RepetitiveTasks
{
    public class RepetitiveMeasureableTask : IRepetitiveTask
    {
        public IWorkTask WorkTask { get; }
        public Frequency Frequency { get; }
        public IMeasureableTask MeasureableTask { get; }

        public RepetitiveMeasureableTask(IWorkTask workTask, Frequency frequency, IMeasureableTask measureableTask)
        {
            WorkTask = workTask;
            Frequency = frequency;
            MeasureableTask = measureableTask;
        }
    }
}