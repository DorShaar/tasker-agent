using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading.Tasks;
using TaskerAgent.Infra.Options.Configurations;
using TaskerAgent.Infra.Services.AgentTiming;
using Triangle.Time;
using Xunit;

namespace TaskerAgantTests.Infra.Services.AgentTiming
{
    public class DailySummaryTimingHandlerTests
    {
        private const string MissingDatesUserReportedFeedbackFileName = "missing_dates_user_reported_feedback";

        [Theory]
        [InlineData("13/04/2021", true)]
        [InlineData("13/05/2020", true)]
        [InlineData("19/04/2020", false)]
        public async Task IsContainMissingDate_AsExpected(string dateString, bool shouldBeContained)
        {
            string databaseDirectoryPath = Directory.CreateDirectory(Path.GetRandomFileName()).FullName;

            try
            {
                IOptionsMonitor<TaskerAgentConfiguration> configuration = A.Fake<IOptionsMonitor<TaskerAgentConfiguration>>();
                configuration.CurrentValue.DatabaseDirectoryPath = databaseDirectoryPath;

                await File.WriteAllTextAsync(Path.Combine(databaseDirectoryPath, MissingDatesUserReportedFeedbackFileName),
                    $"13/04/2021{Environment.NewLine}13/05/2020{Environment.NewLine}").ConfigureAwait(false);

                DailySummaryTimingHandler dailySummaryTimingHandler = new DailySummaryTimingHandler(configuration, NullLogger<DailySummaryTimingHandler>.Instance);

                Assert.Equal(shouldBeContained, dailySummaryTimingHandler.IsContainMissingDate(DateTime.Parse(dateString)));
            }
            finally
            {
                Directory.Delete(databaseDirectoryPath, recursive: true);
            }
        }

        [Fact]
        public async Task IsContainMissingDate_TimeIsNow_ReturnsTrue()
        {
            string databaseDirectoryPath = Directory.CreateDirectory(Path.GetRandomFileName()).FullName;

            try
            {
                IOptionsMonitor<TaskerAgentConfiguration> configuration = A.Fake<IOptionsMonitor<TaskerAgentConfiguration>>();
                configuration.CurrentValue.DatabaseDirectoryPath = databaseDirectoryPath;

                await File.WriteAllTextAsync(Path.Combine(databaseDirectoryPath, MissingDatesUserReportedFeedbackFileName),
                    DateTime.Now.ToString(TimeConsts.TimeFormat) + Environment.NewLine).ConfigureAwait(false);

                DailySummaryTimingHandler dailySummaryTimingHandler = new DailySummaryTimingHandler(configuration, NullLogger<DailySummaryTimingHandler>.Instance);

                Assert.True(dailySummaryTimingHandler.IsContainMissingDate(DateTime.Now));
            }
            finally
            {
                Directory.Delete(databaseDirectoryPath, recursive: true);
            }
        }
    }
}