using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TaskData.TasksGroups;
using TaskData.WorkTasks;
using TaskerAgent.App.Persistence.Repositories;
using TaskerAgent.App.Services.Email;
using TaskerAgent.Domain.Email;
using TaskerAgent.Domain.RepetitiveTasks;
using TaskerAgent.Infra.Extensions;
using TaskerAgent.Infra.Options.Configurations;
using TaskerAgent.Infra.Services;
using Triangle.Time;
using Xunit;

namespace TaskerAgantTests.Infra.Services
{
    public class TaskerAgentServiceTests
    {
        private const string TestFilesDirectory = "TestFiles";
        private const string DatabaseTestFilesPath = "TaskerAgentDB";

        private readonly string mInputFileName = Path.Combine(TestFilesDirectory, "repetitive_tasks.txt");
        private readonly IServiceCollection mServiceCollection;

        public TaskerAgentServiceTests()
        {
            mServiceCollection = new ServiceCollection();
            mServiceCollection.UseDI();

            IOptionsMonitor<TaskerAgentConfiguration> configuration = A.Fake<IOptionsMonitor<TaskerAgentConfiguration>>();
            configuration.CurrentValue.DatabaseDirectoryPath = DatabaseTestFilesPath;
            configuration.CurrentValue.InputFilePath = mInputFileName;
            mServiceCollection.AddSingleton(configuration);

            TaskerAgentService service = mServiceCollection.BuildServiceProvider().GetRequiredService<TaskerAgentService>();

            service.UpdateRepetitiveTasksFromInputFile().Wait();
        }

        [Fact]
        public async Task UpdateRepetitiveTasks_ShouldModifyExpected_ExpectedValueIsModified()
        {
            const int dayOfMonthWithDesiredExpectedValue = 17;

            DateTime specificDateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, dayOfMonthWithDesiredExpectedValue);
            string databaseFileName = specificDateTime.ToString(TimeConsts.TimeFormat);
            databaseFileName = databaseFileName.Replace('\\', '-');
            databaseFileName = databaseFileName.Replace('/', '-');

            IOptionsMonitor<TaskerAgentConfiguration> options =
                mServiceCollection.BuildServiceProvider().GetRequiredService<IOptionsMonitor<TaskerAgentConfiguration>>();

            string specificDayDatabase = Path.Combine(options.CurrentValue.DatabaseDirectoryPath, databaseFileName);

            string contentBeforeChange = await File.ReadAllTextAsync(specificDayDatabase).ConfigureAwait(false);
            const string expectedStringToBeFound = "\"Expected\": 5";
            Assert.Contains(expectedStringToBeFound, contentBeforeChange, StringComparison.OrdinalIgnoreCase);

            const string stringToReplaceWith = "\"Expected\": 90";
            string contentAfterChange = contentBeforeChange.Replace(expectedStringToBeFound, stringToReplaceWith);
            await File.WriteAllTextAsync(specificDayDatabase, contentAfterChange).ConfigureAwait(false);

            contentAfterChange = await File.ReadAllTextAsync(specificDayDatabase).ConfigureAwait(false);
            Assert.DoesNotContain(expectedStringToBeFound, contentAfterChange, StringComparison.OrdinalIgnoreCase);

            TaskerAgentService service = mServiceCollection.BuildServiceProvider().GetRequiredService<TaskerAgentService>();
            await service.UpdateRepetitiveTasksFromInputFile().ConfigureAwait(false);

            string contentAfterUpdate = await File.ReadAllTextAsync(specificDayDatabase).ConfigureAwait(false);
            Assert.Contains(expectedStringToBeFound, contentAfterUpdate, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SendTodaysTasksReport_AsExpected()
        {
            IEmailService emailService = A.Fake<IEmailService>();
            mServiceCollection.AddSingleton(emailService);

            ServiceProvider serviceProvider = mServiceCollection.BuildServiceProvider();
            TaskerAgentService service = serviceProvider.GetRequiredService<TaskerAgentService>();

            await service.SendTodaysTasksReport().ConfigureAwait(false);

            A.CallTo(() => emailService.SendMessage(
                A<string>.Ignored,
                A<string>.That.Matches(report =>
                    report.Contains("Today's Tasks:", StringComparison.OrdinalIgnoreCase) &&
                    report.Contains("Drink Water.", StringComparison.OrdinalIgnoreCase) &&
                    report.Contains("Exercise.", StringComparison.OrdinalIgnoreCase) &&
                    report.Contains("Sleep hours.", StringComparison.OrdinalIgnoreCase),
                    "as expected")))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task SendThisWeekTasksReport_AsExpected()
        {
            IEmailService emailService = A.Fake<IEmailService>();
            mServiceCollection.AddSingleton(emailService);

            ServiceProvider serviceProvider = mServiceCollection.BuildServiceProvider();
            TaskerAgentService service = serviceProvider.GetRequiredService<TaskerAgentService>();

            string thisWeekReport = string.Empty;
            A.CallTo(() => emailService.SendMessage(A<string>.Ignored, A<string>.Ignored))
                .Invokes(fakeObjectCall => thisWeekReport = fakeObjectCall.Arguments.Get<string>(1));

            await service.SendThisWeekTasksReport().ConfigureAwait(false);

            string[] splittedReport = thisWeekReport.Split("Drink Water");
            Assert.True(splittedReport.Length == 8);

            splittedReport = thisWeekReport.Split("Exercise");
            Assert.True(splittedReport.Length == 8);

            splittedReport = thisWeekReport.Split("Sleep hours");
            Assert.True(splittedReport.Length == 8);

            splittedReport = thisWeekReport.Split("Floss");
            Assert.True(splittedReport.Length == 4);

            splittedReport = thisWeekReport.Split("Eat bamba");
            Assert.True(splittedReport.Length == 2);

            splittedReport = thisWeekReport.Split("Run");
            Assert.True(splittedReport.Length == 3);
        }

        [Fact]
        public async Task SendDailySummary_AsExpected()
        {
            IEmailService emailService = A.Fake<IEmailService>();
            mServiceCollection.AddSingleton(emailService);

            ServiceProvider serviceProvider = mServiceCollection.BuildServiceProvider();
            TaskerAgentService service = serviceProvider.GetRequiredService<TaskerAgentService>();

            string report = string.Empty;
            A.CallTo(() => emailService.SendMessage(A<string>.Ignored, A<string>.Ignored))
                .Invokes(fakeObjectCall => report = fakeObjectCall.Arguments.Get<string>(1));

            await service.SendDailySummary(DateTime.Now).ConfigureAwait(false);

            Assert.Contains("Daily Summary Report:", report);
            Assert.Contains("Tasks Status - ", report);
            Assert.Contains("Drink Water:", report);
            Assert.Contains("Exercise:", report);
            Assert.Contains("Sleep hours:", report);
            Assert.Contains("Total score:", report);
        }

        [Fact]
        public async Task SendWeeklySummary_AsExpected()
        {
            IEmailService emailService = A.Fake<IEmailService>();
            mServiceCollection.AddSingleton(emailService);

            ServiceProvider serviceProvider = mServiceCollection.BuildServiceProvider();
            TaskerAgentService service = serviceProvider.GetRequiredService<TaskerAgentService>();

            string report = string.Empty;
            A.CallTo(() => emailService.SendMessage(A<string>.Ignored, A<string>.Ignored))
                .Invokes(fakeObjectCall => report = fakeObjectCall.Arguments.Get<string>(1));

            await service.SendWeeklySummary(DateTime.Now).ConfigureAwait(false);

            string[] splittedReport = report.Split("Tasks Status");

            Assert.True(splittedReport.Length == 8);
        }

        [Fact]
        public async Task FulfillReport_AsExpected()
        {
            const string message = @"Today's Tasks:
Saturday - 03/04/2021:
Drink Water. Expected: 2. Actual: 1.
Exercise. Expected: 3. actual: 1.
Sleep hours. Expected: 7. Actual:    5.
Eat bamba. Expected: 2.Actual: 6
";

            IOptionsMonitor<TaskerAgentConfiguration> configuration = A.Fake<IOptionsMonitor<TaskerAgentConfiguration>>();
            configuration.CurrentValue.DatabaseDirectoryPath = DatabaseTestFilesPath;
            mServiceCollection.AddSingleton(configuration);

            IEmailService emailService = A.Fake<IEmailService>();
            A.CallTo(() => emailService.ReadMessages(false)).Returns(new MessageInfo[] { new MessageInfo("id", message, DateTime.Now) });
            mServiceCollection.AddSingleton(emailService);

            ServiceProvider serviceProvider = mServiceCollection.BuildServiceProvider();
            TaskerAgentService service = serviceProvider.GetRequiredService<TaskerAgentService>();
            IDbRepository<ITasksGroup> realTasksGroupRepository = serviceProvider.GetRequiredService<IDbRepository<ITasksGroup>>();

            ITasksGroup returnedTasksGroup = await realTasksGroupRepository.FindAsync("03-04-2021").ConfigureAwait(false);

            List<IWorkTask> tasks = returnedTasksGroup.GetAllTasks().ToList();

            GeneralRepetitiveMeasureableTask task1 = tasks[0] as GeneralRepetitiveMeasureableTask;
            Assert.True(task1.Description == "Drink Water" && task1.Actual == 0);

            GeneralRepetitiveMeasureableTask task2 = tasks[1] as GeneralRepetitiveMeasureableTask;
            Assert.True(task2.Description == "Exercise" && task2.Actual == 0);

            GeneralRepetitiveMeasureableTask task3 = tasks[2] as GeneralRepetitiveMeasureableTask;
            Assert.True(task3.Description == "Sleep hours" && task3.Actual == 0);

            GeneralRepetitiveMeasureableTask task4 = tasks[3] as GeneralRepetitiveMeasureableTask;
            Assert.True(task4.Description == "Eat bamba" && task4.Actual == 0);

            await service.CheckForUpdates().ConfigureAwait(false);

            returnedTasksGroup = await realTasksGroupRepository.FindAsync("03-04-2021").ConfigureAwait(false);

            tasks = returnedTasksGroup.GetAllTasks().ToList();

            task1 = tasks[0] as GeneralRepetitiveMeasureableTask;
            Assert.True(task1.Description == "Drink Water" && task1.Actual == 1);

            task2 = tasks[1] as GeneralRepetitiveMeasureableTask;
            Assert.True(task2.Description == "Exercise" && task2.Actual == 1);

            task3 = tasks[2] as GeneralRepetitiveMeasureableTask;
            Assert.True(task3.Description == "Sleep hours" && task3.Actual == 5);

            task4 = tasks[3] as GeneralRepetitiveMeasureableTask;
            Assert.True(task4.Description == "Eat bamba" && task4.Actual == 6);
        }
    }
}