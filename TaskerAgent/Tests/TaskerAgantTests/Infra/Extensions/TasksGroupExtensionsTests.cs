using Microsoft.Extensions.DependencyInjection;
using TaskData.TasksGroups;
using TaskData.WorkTasks;
using TaskerAgent.App.TasksProducers;
using TaskerAgent.Domain;
using TaskerAgent.Domain.TaskGroup;
using TaskerAgent.Infra.Extensions;
using Xunit;

namespace TaskerAgantTests.Infra
{
    public class TasksGroupExtensionsTests
    {
        private readonly ServiceProvider mServiceProvider;

        public TasksGroupExtensionsTests()
        {
            IServiceCollection serviceCollection = new ServiceCollection();

            serviceCollection.UseDI();
            serviceCollection.AddLogging();

            mServiceProvider = serviceCollection.BuildServiceProvider();
        }

        [Fact]
        public void Compare_ShouldBeDifferent_ResultIsTasksAddedOrRemoved()
        {
            ITasksGroupFactory taskGroupFactory = mServiceProvider.GetRequiredService<ITasksGroupFactory>();
            ITasksProducerFactory tasksProducerFactory = mServiceProvider.GetRequiredService<ITasksProducerFactory>();

            ITasksGroup groupA = taskGroupFactory.CreateGroup("GroupA").Value;
            ITasksGroup groupB = taskGroupFactory.CreateGroup("GroupB").Value;

            IWorkTaskProducer workTaskProducer = tasksProducerFactory.CreateDailyProducer(MeasureType.Liters, 3, 2);
            taskGroupFactory.CreateTask(groupA, "descriptionA", workTaskProducer);
            taskGroupFactory.CreateTask(groupB, "descriptionA", workTaskProducer);

            Assert.Equal(ComparisonResult.TasksAddedOrRemoved, groupA.Compare(groupB));
        }

        [Fact]
        public void Compare_ShouldBeEqual_ResultIsEqual()
        {
            ITasksGroupFactory taskGroupFactory = mServiceProvider.GetRequiredService<ITasksGroupFactory>();
            ITasksProducerFactory tasksProducerFactory = mServiceProvider.GetRequiredService<ITasksProducerFactory>();

            ITasksGroup groupA = taskGroupFactory.CreateGroup("GroupA").Value;

            IWorkTaskProducer workTaskProducer = tasksProducerFactory.CreateDailyProducer(MeasureType.Liters, 3, 2);
            taskGroupFactory.CreateTask(groupA, "descriptionA", workTaskProducer);

            Assert.Equal(ComparisonResult.Equal, groupA.Compare(groupA));
        }
    }
}