using TaskData.WorkTasks;
using TaskerAgent.Domain;

namespace TaskerAgent.App.RepetitiveTasks
{
    public interface IRepetitiveMeasureableTask : IRepetitiveTask, IMeasureableTask
    {
        void InitializeRepetitiveMeasureableTask(Frequency frequency, IMeasureableTask measureableTask);
    }
}