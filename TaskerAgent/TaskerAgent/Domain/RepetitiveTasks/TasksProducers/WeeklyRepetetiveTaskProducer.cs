using TaskData.WorkTasks;

namespace TaskerAgent.Domain.RepetitiveTasks.TasksProducers
{
    public class WeeklyRepetetiveTaskProducer : IWorkTaskProducer
    {
        public Frequency Frequency { get; }
        public MeasureType MeasureType { get; }
        public int Expected { get; }
        public int Score { get; }

        public WeeklyRepetetiveTaskProducer(Frequency frequency, MeasureType measureType, int expected, int score)
        {
            Frequency = frequency;
            MeasureType = measureType;
            Expected = expected;
            Score = score;
        }

        public IWorkTask ProduceTask(string id, string description)
        {
            return new WeeklyRepetitiveMeasureableTask(id, description, Frequency, MeasureType, Expected, Score);
        }
    }
}