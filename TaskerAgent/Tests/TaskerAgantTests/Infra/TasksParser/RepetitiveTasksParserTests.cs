using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TaskData.TasksGroups;
using TaskData.TasksGroups.Producers;
using TaskData.WorkTasks;
using TaskerAgent.Domain;
using TaskerAgent.Domain.RepetitiveTasks.RepetitiveMeasureableTasks;
using TaskerAgent.Domain.RepetitiveTasks.TasksProducers;
using TaskerAgent.Domain.TaskerDateTime;
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
        public async Task ParseTasksToWhyGroups_AsExpected()
        {
            ITasksGroupFactory groupsFactory = mServiceProvider.GetRequiredService<ITasksGroupFactory>();
            ITasksGroup group = groupsFactory.CreateGroup("test", mTasksGroupProducer).Value;

            IOptionsMonitor<TaskerAgentConfiguration> configuration =
                mServiceProvider.GetRequiredService<IOptionsMonitor<TaskerAgentConfiguration>>();
            configuration.CurrentValue.InputFilePath = mInputFileName;

            FileTasksParser parser = new FileTasksParser(
                groupsFactory, new TasksProducerFactory(), new TasksGroupProducer(), configuration, NullLogger<FileTasksParser>.Instance);

            IEnumerable<ITasksGroup> whyGroups = await parser.ParseTasksToWhyGroups().ConfigureAwait(false);

            List<IWorkTask> tasks = new List<IWorkTask>();
            whyGroups.ToList().ForEach(group => tasks.AddRange(group.GetAllTasks()));

            if (tasks[0] is not WhyMeasureableTask healthyLeavingWhyTask ||
                tasks[1] is not DailyRepetitiveMeasureableTask drinkWaterTask ||
                tasks[2] is not DailyRepetitiveMeasureableTask exerciseTask ||
                tasks[3] is not WeeklyRepetitiveMeasureableTask flossTask ||
                tasks[4] is not WeeklyRepetitiveMeasureableTask eayHealthyTask ||
                tasks[5] is not DailyRepetitiveMeasureableTask sleepTask ||
                tasks[6] is not WeeklyRepetitiveMeasureableTask runTask ||
                tasks[7] is not WhyMeasureableTask organizedWorkWhyTask ||
                tasks[8] is not MonthlyRepetitiveMeasureableTask planHolidaysTask ||
                tasks[10] is not MonthlyRepetitiveMeasureableTask selfStudyTask ||
                tasks[12] is not WhyMeasureableTask timeWithFamilyWhyTask ||
                tasks[17] is not WhyMeasureableTask improveApplicationWhyTask ||
                tasks[18] is not WorkTask finishTriangleOrientedFeatureTask)
            {
                Assert.False(true);
                return;
            }

            Assert.Equal("Healthy Leaving", healthyLeavingWhyTask.Description);
            Assert.Equal(Frequency.Weekly, healthyLeavingWhyTask.Frequency);
            IReadOnlyDictionary<string, bool> healthyLeavingWhyTaskContent = healthyLeavingWhyTask.TaskMeasurement.Content.GetContents();
            Assert.False(healthyLeavingWhyTaskContent["1003"]);
            Assert.False(healthyLeavingWhyTaskContent["1004"]);
            Assert.False(healthyLeavingWhyTaskContent["1005"]);
            Assert.False(healthyLeavingWhyTaskContent["1006"]);
            Assert.False(healthyLeavingWhyTaskContent["1007"]);
            Assert.False(healthyLeavingWhyTaskContent["1008"]);
            Assert.Equal(DateTimeUtilities.GetNextDay(DayOfWeek.Sunday).AddMinutes(5), healthyLeavingWhyTask.TaskMeasurement.Time.GetExpectedDueDate());

            Assert.Equal("Drink Water", drinkWaterTask.Description);
            Assert.Equal(MeasureType.Liters, drinkWaterTask.MeasureType);

            Assert.Equal("Exercise", exerciseTask.Description);
            Assert.Equal(MeasureType.Occurrences, exerciseTask.MeasureType);

            Assert.Equal("Floss", flossTask.Description);
            Assert.Equal(Days.Monday | Days.Wednesday | Days.Friday, flossTask.OccurrenceDays);

            Assert.Equal("Eat healthy", eayHealthyTask.Description);
            Assert.Equal(Days.Saturday, eayHealthyTask.OccurrenceDays);

            Assert.Equal("Sleep hours", sleepTask.Description);

            Assert.Equal("Run", runTask.Description);
            Assert.Equal(Days.Wednesday | Days.Friday, runTask.OccurrenceDays);

            Assert.Equal("Be organized at work", organizedWorkWhyTask.Description);
            Assert.Equal(Frequency.Monthly, organizedWorkWhyTask.Frequency);

            Assert.Equal("Plan holidays at work", planHolidaysTask.Description);
            Assert.Equal(15, planHolidaysTask.DaysOfMonth[0]);

            Assert.Equal("Self study", selfStudyTask.Description);
            Assert.Equal(25, selfStudyTask.DaysOfMonth[0]);
            Assert.Equal(26, selfStudyTask.DaysOfMonth[1]);
            Assert.Equal(27, selfStudyTask.DaysOfMonth[2]);
            Assert.Equal(28, selfStudyTask.DaysOfMonth[3]);

            Assert.Equal("Have time with family", timeWithFamilyWhyTask.Description);
            Assert.Equal(Frequency.DuWeekly, timeWithFamilyWhyTask.Frequency);

            Assert.Equal("Improve application", improveApplicationWhyTask.Description);
            Assert.Equal(Frequency.NotDefined, improveApplicationWhyTask.Frequency);
            IReadOnlyDictionary<string, bool> improveApplicationWhyTaskContent = improveApplicationWhyTask.TaskMeasurement.Content.GetContents();
            Assert.False(improveApplicationWhyTaskContent["1025"]);
            Assert.Equal(DateTime.Parse("01/08/2021").AddMinutes(5), improveApplicationWhyTask.TaskMeasurement.Time.GetExpectedDueDate());

            Assert.Equal("Finish triangle oriented feature", finishTriangleOrientedFeatureTask.Description);
            Assert.Equal(DateTime.Parse("01/08/2021"), finishTriangleOrientedFeatureTask.TaskMeasurement.Time.GetExpectedDueDate());
        }
    }
}