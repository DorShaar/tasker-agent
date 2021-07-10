using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TaskData.TasksGroups;
using TaskData.WorkTasks;
using TaskData.WorkTasks.Producers;
using TaskerAgent.Domain;
using TaskerAgent.Domain.RepetitiveTasks.RepetitiveMeasureableTasks;
using TaskerAgent.Domain.RepetitiveTasks.TasksProducers;
using TaskerAgent.Infra.Extensions;
using TaskerAgent.Infra.Options.Configurations;
using TaskerAgent.Infra.Services.TasksParser;
using Xunit;

namespace TaskerAgantTests.Infra.TasksParser
{
    public class RepetitiveTasksParserTests
    {
        private const string TestFilesDirectory = "TestFiles";

        private readonly string mInputFileName = Path.Combine(TestFilesDirectory, "start_with_why.txt");
        private readonly ServiceProvider mServiceProvider;
        private readonly ITasksGroupProducer mTasksGroupProducer;

        public RepetitiveTasksParserTests()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.UseDI();

            mServiceProvider = serviceCollection.BuildServiceProvider();
            mTasksGroupProducer = mServiceProvider.GetRequiredService<ITasksGroupProducer>();
        }

        [Fact]
        public async Task ParseIntoGroup_AsExpected()
        {
            ITasksGroupFactory groupsFactory = mServiceProvider.GetRequiredService<ITasksGroupFactory>();
            ITasksGroup group = groupsFactory.CreateGroup("test", mTasksGroupProducer).Value;

            IOptionsMonitor<TaskerAgentConfiguration> configuration =
                mServiceProvider.GetRequiredService<IOptionsMonitor<TaskerAgentConfiguration>>();
            configuration.CurrentValue.InputFilePath = mInputFileName;

            FileTasksParser parser = new FileTasksParser(
                groupsFactory, new TasksProducerFactory(), configuration, NullLogger<FileTasksParser>.Instance);

            await parser.ParseIntoGroup(group).ConfigureAwait(false);

            List<IWorkTask> repetitiveTasks = group.GetAllTasks().ToList();

            if (repetitiveTasks[0] is not DailyRepetitiveMeasureableTask repetitiveMeasureableTask0     ||
                repetitiveTasks[1] is not DailyRepetitiveMeasureableTask repetitiveMeasureableTask1     ||
                repetitiveTasks[2] is not WeeklyRepetitiveMeasureableTask repetitiveMeasureableTask2    ||
                repetitiveTasks[3] is not WeeklyRepetitiveMeasureableTask repetitiveMeasureableTask3    ||
                repetitiveTasks[4] is not DailyRepetitiveMeasureableTask repetitiveMeasureableTask4     ||
                repetitiveTasks[5] is not MonthlyRepetitiveMeasureableTask repetitiveMeasureableTask5   ||
                repetitiveTasks[6] is not WeeklyRepetitiveMeasureableTask repetitiveMeasureableTask6    ||
                repetitiveTasks[8] is not MonthlyRepetitiveMeasureableTask repetitiveMeasureableTask8)
            {
                Assert.False(true);
                return;
            }

            Assert.Equal("Drink Water", repetitiveMeasureableTask0.Description);
            Assert.Equal(MeasureType.Liters, repetitiveMeasureableTask0.MeasureType);

            Assert.Equal("Exercise", repetitiveMeasureableTask1.Description);
            Assert.Equal(MeasureType.Occurrences, repetitiveMeasureableTask1.MeasureType);

            Assert.Equal("Floss", repetitiveMeasureableTask2.Description);

            Assert.Equal("Eat bamba", repetitiveMeasureableTask3.Description);
            Assert.Equal(Days.Saturday, repetitiveMeasureableTask3.OccurrenceDays);

            Assert.Equal("Sleep hours", repetitiveMeasureableTask4.Description);

            Assert.Equal("Plan holidays at work", repetitiveMeasureableTask5.Description);
            Assert.Equal(15, repetitiveMeasureableTask5.DaysOfMonth[0]);

            Assert.Equal("Run", repetitiveMeasureableTask6.Description);
            Assert.Equal(Days.Wednesday | Days.Friday, repetitiveMeasureableTask6.OccurrenceDays);

            Assert.Equal("Self study", repetitiveMeasureableTask8.Description);
            Assert.Equal(25, repetitiveMeasureableTask8.DaysOfMonth[0]);
            Assert.Equal(26, repetitiveMeasureableTask8.DaysOfMonth[1]);
            Assert.Equal(27, repetitiveMeasureableTask8.DaysOfMonth[2]);
            Assert.Equal(28, repetitiveMeasureableTask8.DaysOfMonth[3]);
        }
    }
}