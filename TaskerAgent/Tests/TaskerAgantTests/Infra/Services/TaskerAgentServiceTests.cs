using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskData.WorkTasks;
using TaskerAgent.Infra.Extensions;
using TaskerAgent.Infra.Services;
using Xunit;

namespace TaskerAgantTests.Infra.Services
{
    public class TaskerAgentServiceTests
    {
        private readonly ServiceProvider mServiceProvider;

        public TaskerAgentServiceTests()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.UseDI();
            mServiceProvider = serviceCollection.BuildServiceProvider();

            TaskerAgentService service = mServiceProvider.GetRequiredService<TaskerAgentService>();

            service.UpdateRepetitiveTasks().ConfigureAwait(false);
        }

        [Fact]
        public async Task GetTodaysTasks_AsExpected()
        {
            TaskerAgentService service = mServiceProvider.GetRequiredService<TaskerAgentService>();

            IEnumerable<IWorkTask> todaysTasks = (await service.GetTodaysTasks().ConfigureAwait(false));

            Assert.Contains(todaysTasks, task => task.Description.Equals("Drink Water", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(todaysTasks, task => task.Description.Equals("Exercise", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(todaysTasks, task => task.Description.Equals("Sleep hours", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task GetThisWeekTasks_AsExpected()
        {
            TaskerAgentService service = mServiceProvider.GetRequiredService<TaskerAgentService>();

            IEnumerable<IWorkTask> thisWeekTasks = await service.GetThisWeekTasks().ConfigureAwait(false);

            IEnumerable<IWorkTask> drinkWaterTasks = thisWeekTasks.Where(task => task.Description.Equals("Drink Water", StringComparison.OrdinalIgnoreCase));
            Assert.True(drinkWaterTasks.Count() == 7);

            IEnumerable<IWorkTask> exerciseTasks = thisWeekTasks.Where(task => task.Description.Equals("Exercise", StringComparison.OrdinalIgnoreCase));
            Assert.True(exerciseTasks.Count() == 7);

            IEnumerable<IWorkTask> SleepTasks = thisWeekTasks.Where(task => task.Description.Equals("Sleep hours", StringComparison.OrdinalIgnoreCase));
            Assert.True(SleepTasks.Count() == 7);

            IEnumerable<IWorkTask> flossTasks = thisWeekTasks.Where(task => task.Description.Equals("Floss", StringComparison.OrdinalIgnoreCase));
            Assert.True(flossTasks.Count() == 3);

            IEnumerable<IWorkTask> eatBambaTasks = thisWeekTasks.Where(task => task.Description.Equals("Eat bamba", StringComparison.OrdinalIgnoreCase));
            Assert.True(eatBambaTasks.Count() == 1);

            IEnumerable<IWorkTask> runTasks = thisWeekTasks.Where(task => task.Description.Equals("Run", StringComparison.OrdinalIgnoreCase));
            Assert.True(runTasks.Count() == 2);
        }
    }
}