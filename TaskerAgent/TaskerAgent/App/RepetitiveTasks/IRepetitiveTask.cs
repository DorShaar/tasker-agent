using TaskData.WorkTasks;
using TaskerAgent.Domain;

namespace TaskerAgent.App.RepetitiveTasks
{
    public interface IRepetitiveTask
    {
        IWorkTask WorkTask { get; }
        Frequency Frequency { get; }
    }
}