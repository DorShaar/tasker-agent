using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using TaskData.TasksGroups;
using TaskData.WorkTasks;
using TaskerAgent.Domain;
using TaskerAgent.Domain.RepetitiveTasks;
using TaskerAgent.Domain.RepetitiveTasks.TasksProducers;
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

            RepetitiveTasksParser parser = new RepetitiveTasksParser(
                groupsFactory, new TasksProducerFactory(), configuration, NullLogger<RepetitiveTasksParser>.Instance);
            parser.ParseIntoGroup(group);
            List<IWorkTask> repetitiveTasks = group.GetAllTasks().ToList();

            if (!(repetitiveTasks[0] is DailyRepetitiveMeasureableTask repetitiveMeasureableTask0)     ||
                !(repetitiveTasks[1] is DailyRepetitiveMeasureableTask repetitiveMeasureableTask1)     ||
                !(repetitiveTasks[2] is WeeklyRepetitiveMeasureableTask repetitiveMeasureableTask2)    ||
                !(repetitiveTasks[3] is WeeklyRepetitiveMeasureableTask repetitiveMeasureableTask3)    ||
                !(repetitiveTasks[4] is DailyRepetitiveMeasureableTask repetitiveMeasureableTask4)     ||
                !(repetitiveTasks[5] is MonthlyRepetitiveMeasureableTask repetitiveMeasureableTask5)   ||
                !(repetitiveTasks[6] is WeeklyRepetitiveMeasureableTask repetitiveMeasureableTask6))
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

            Assert.Equal(22, repetitiveMeasureableTask5.DaysOfMonth[0]);

            Assert.Equal(Days.Wednesday | Days.Friday, repetitiveMeasureableTask6.OccurrenceDays);
        }
    }
}