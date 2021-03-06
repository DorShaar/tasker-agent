using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
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
        }

        [Fact]
        public async Task GetTodaysTasks_AsExpected()
        {
            TaskerAgentService service = mServiceProvider.GetRequiredService<TaskerAgentService>();

            IEnumerable<IWorkTask> todaysTasks = await service.GetTodaysTasks().ConfigureAwait(false);

            Assert.True(false);
        }

        [Fact]
        public async Task GetThisWeekTasks_AsExpected()
        {
            TaskerAgentService service = mServiceProvider.GetRequiredService<TaskerAgentService>();

            IEnumerable<IWorkTask> thisWeekTasks = await service.GetThisWeekTasks().ConfigureAwait(false);

            Assert.True(false);
        }

        [Fact]
        public async Task UpdateRepetitiveTasks_AsExpected()
        {
            TaskerAgentService service = mServiceProvider.GetRequiredService<TaskerAgentService>();

            await service.UpdateRepetitiveTasks().ConfigureAwait(false);

            Assert.True(false);
        }
    }
}