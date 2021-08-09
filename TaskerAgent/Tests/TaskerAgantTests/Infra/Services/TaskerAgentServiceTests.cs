using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading.Tasks;
using TaskerAgent.App.Services.Email;
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

            mServiceCollection.BuildServiceProvider().GetRequiredService<TaskerAgentService>();
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

            await service.SendTodaysFutureTasksReport().ConfigureAwait(false);

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

            await service.SendThisWeekFutureTasksReport().ConfigureAwait(false);

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

            Assert.Contains("Occurrences", report, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Liters", report, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Hours", report, StringComparison.OrdinalIgnoreCase);
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
    }
}