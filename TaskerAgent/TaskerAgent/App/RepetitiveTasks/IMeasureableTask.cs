using TaskData.WorkTasks;
using TaskerAgent.Domain;

namespace TaskerAgent.App.RepetitiveTasks
{
    public interface IMeasureableTask : IWorkTask
    {
        MeasureType MeasureType { get; }
        int Expected { get; }
        int Actual { get; }
        int Score { get; }
    }
}