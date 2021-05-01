using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskData.TasksGroups;
using TaskData.WorkTasks;
using TaskerAgent.App.TasksProducers;
using TaskerAgent.Domain;
using TaskerAgent.Domain.RepetitiveTasks;
using TaskerAgent.Infra.Extensions;
using TaskerAgent.Infra.Persistence.Context;
using Xunit;

namespace TaskerAgantTests.Infra.Context
{
    public class AppDbContextTests
    {
        private readonly ServiceProvider mServiceProvider;

        public AppDbContextTests()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.UseDI();

            mServiceProvider = serviceCollection.BuildServiceProvider();
        }

        [Fact]
        public async Task LoadAndSaveCurrentDatabase_AsExpected()
        {
            AppDbContext appDbContext = mServiceProvider.GetRequiredService<AppDbContext>();

            ITasksProducerFactory tasksProducerFactory = mServiceProvider.GetRequiredService<ITasksProducerFactory>();
            IWorkTaskProducer dailyTaskProducer = tasksProducerFactory.CreateDailyProducer(
                MeasureType.Liters, 3, 1);
            IWorkTaskProducer weeklyTaskProducer = tasksProducerFactory.CreateWeeklyProducer(
                MeasureType.Liters, Days.Friday, 3, 1);
            IWorkTaskProducer monthlyTaskProducer = tasksProducerFactory.CreateMonthlyProducer(
                MeasureType.Liters, new List<int> { 1 }, 3, 1);

            ITasksGroupFactory taskGroupFactory = mServiceProvider.GetRequiredService<ITasksGroupFactory>();

            ITasksGroup group = taskGroupFactory.CreateGroup("test group").Value;

            string dailyTaskID = taskGroupFactory.CreateTask(group, "task1", dailyTaskProducer).Value.ID;
            string weeklyTaskID = taskGroupFactory.CreateTask(group, "task2", weeklyTaskProducer).Value.ID;
            string monthlyTaskID = taskGroupFactory.CreateTask(group, "task3", monthlyTaskProducer).Value.ID;

            await appDbContext.AddToDatabase(group).ConfigureAwait(false);

            ITasksGroup groupFromDatabase = await appDbContext.FindAsync(group.Name).ConfigureAwait(false);

            Assert.Equal("task1", groupFromDatabase.GetTask(dailyTaskID).Value.Description);
            Assert.True(groupFromDatabase.GetTask(dailyTaskID).Value is DailyRepetitiveMeasureableTask);
            Assert.Equal("task2", groupFromDatabase.GetTask(weeklyTaskID).Value.Description);
            Assert.True(groupFromDatabase.GetTask(weeklyTaskID).Value is WeeklyRepetitiveMeasureableTask);
            Assert.Equal("task3", groupFromDatabase.GetTask(monthlyTaskID).Value.Description);
            Assert.True(groupFromDatabase.GetTask(monthlyTaskID).Value is MonthlyRepetitiveMeasureableTask);
        }
    }
}