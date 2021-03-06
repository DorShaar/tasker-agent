using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using TaskData.TasksGroups;
using TaskData.WorkTasks;
using TaskerAgent.App.RepetitiveTasks;
using TaskerAgent.Domain;
using TaskerAgent.Infra.Extensions;
using TaskerAgent.Infra.Options.Configurations;
using TaskerAgent.Infra.TasksParser;
using Xunit;

namespace TaskerAgantTests.Infra.TasksParser
{
    public class RepetitiveTasksParserTests
    {
        private readonly ServiceProvider mServiceProvider;

        public RepetitiveTasksParserTests()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.UseDI();
            mServiceProvider = serviceCollection.BuildServiceProvider();
        }

        [Fact]
        public void Parse_AsExpected()
        {
            ITasksGroupFactory groupsFactory = mServiceProvider.GetRequiredService<ITasksGroupFactory>();
            ITasksGroup group = groupsFactory.CreateGroup("test").Value;
            IOptionsMonitor<TaskerAgentConfiguration> configuration =
                mServiceProvider.GetRequiredService<IOptionsMonitor<TaskerAgentConfiguration>>();

            RepetitiveTasksParser parser = new RepetitiveTasksParser(groupsFactory, configuration, NullLogger<RepetitiveTasksParser>.Instance);
            parser.ParseIntoGroup(group);
            List<IWorkTask> repetitiveTasks = group.GetAllTasks().ToList();

            if (!(repetitiveTasks[0] is IRepetitiveMeasureableTask repetitiveMeasureableTask0) ||
                !(repetitiveTasks[1] is IRepetitiveMeasureableTask repetitiveMeasureableTask1) ||
                !(repetitiveTasks[2] is IRepetitiveMeasureableTask repetitiveMeasureableTask2) ||
                !(repetitiveTasks[3] is IRepetitiveMeasureableTask repetitiveMeasureableTask3) ||
                !(repetitiveTasks[4] is IRepetitiveMeasureableTask repetitiveMeasureableTask4))
            {
                Assert.False(true);
                return;
            }

            Assert.Equal("Drink Water", repetitiveMeasureableTask0.Description);
            Assert.Equal(MeasureType.Liter, repetitiveMeasureableTask0.MeasureType);

            Assert.Equal("Exercise", repetitiveMeasureableTask1.Description);
            Assert.Equal(MeasureType.Occurrence, repetitiveMeasureableTask1.MeasureType);

            Assert.Equal("Floss", repetitiveMeasureableTask2.Description);

            Assert.Equal("Eat bamba", repetitiveMeasureableTask3.Description);

            Assert.Equal("Sleep hours", repetitiveMeasureableTask4.Description);
        }
    }
}