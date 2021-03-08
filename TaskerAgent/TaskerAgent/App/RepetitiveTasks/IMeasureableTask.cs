using TaskData.WorkTasks;
using TaskerAgent.Domain;

namespace TaskerAgent.App.RepetitiveTasks
{
    public interface IMeasureableTask : IWorkTask
    {
        MeasureType MeasureType { get; set; }
        int Expected { get; set; }
        int Actual { get; set; }
        int Score { get; set; }
    }
}