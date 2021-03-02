using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using TaskData.TasksGroups;
using TaskerAgent.App.RepetitiveTasks;
using TaskerAgent.Infra;
using TaskerAgent.Infra.Extensions;
using TaskerAgent.Infra.Options.Configurations;
using Xunit;

namespace TaskerAgantTests
{
    public class RepetitiveTasksParserTests
    {
        private readonly ServiceProvider mServiceProvider;

        public RepetitiveTasksParserTests()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
                //.AddLogging();

            serviceCollection.UseDI();

            mServiceProvider = serviceCollection.BuildServiceProvider();
        }

        [Fact]
        public void Parse_AsExpected()
        {
            ITasksGroupFactory groupsFactory = mServiceProvider.GetRequiredService<ITasksGroupFactory>();
            ITasksGroup group = groupsFactory.CreateGroup("test");
            IOptionsMonitor<TaskerAgentConfiguration> configuration =
                mServiceProvider.GetRequiredService<IOptionsMonitor<TaskerAgentConfiguration>>();

            RepetitiveTasksParser parser = new RepetitiveTasksParser(groupsFactory);
            List<IRepetitiveTask> repetitiveTasks = parser.Parse(group, configuration).ToList();

            Assert.Equal("Drink Water", repetitiveTasks[0].WorkTask.Description);
            Assert.Equal("Exercise", repetitiveTasks[1].WorkTask.Description);
            Assert.Equal("Floss", repetitiveTasks[2].WorkTask.Description);
            Assert.Equal("Eat bamba", repetitiveTasks[3].WorkTask.Description);
            Assert.Equal("Sleep hours", repetitiveTasks[4].WorkTask.Description);
        }
    }
}