using TaskerAgent.Domain;

namespace TaskerAgent.App.RepetitiveTasks
{
    public interface IMeasureableTask
    {
        MeasureType MeasureType { get; }
        int Expected { get; }
        int Actual { get; }
        int Score { get; }
    }
}