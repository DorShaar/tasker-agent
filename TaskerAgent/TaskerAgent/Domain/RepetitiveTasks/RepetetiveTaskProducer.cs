using TaskData.WorkTasks;

namespace TaskerAgent.Domain.RepetitiveTasks
{
    public class RepetetiveTaskProducer : IWorkTaskProducer
    {
        public IWorkTask ProduceTask(string id, string description)
        {
            return new RepetitiveMeasureableTask(id, description);
        }
    }
}