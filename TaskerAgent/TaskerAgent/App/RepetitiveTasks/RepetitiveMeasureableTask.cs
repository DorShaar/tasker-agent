using TaskerAgent.Domain;

namespace TaskerAgent.App.RepetitiveTasks
{
    public interface IRepetitiveMeasureableTask : IRepetitiveTask, IMeasureableTask
    {
        void InitializeRepetitiveMeasureableTask(Frequency frequency,
            MeasureType measureType,
            int expected,
            int score);
    }
}