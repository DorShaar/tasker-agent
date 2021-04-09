using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TaskerAgent.Infra.Options.Configurations;
using TaskerAgent.Infra.Services.AgentTiming;
using Triangle.Time;
using Xunit;

namespace TaskerAgantTests.Infra.Services.AgentTiming
{
    public class AgentTimingServiceTests
    {
        private const string LastDateUserReportedFeedbackFileName = "last_date_user_reported_feedback";

        [Fact]
        public async Task SignalDatesGivenFeedbackByUser_DateIsSavedAsExpected()
        {
            string tempDirectory = Directory.CreateDirectory(Path.GetRandomFileName()).FullName;

            try
            {
                IOptionsMonitor<TaskerAgentConfiguration> configuration = A.Fake<IOptionsMonitor<TaskerAgentConfiguration>>();
                configuration.CurrentValue.DatabaseDirectoryPath = tempDirectory;

                string databasePath = Path.Combine(
                    configuration.CurrentValue.DatabaseDirectoryPath, LastDateUserReportedFeedbackFileName);

                string lastDateUserReportedAFeedback = DateTime.Now.AddDays(-6).ToString(TimeConsts.TimeFormat);
                await File.WriteAllTextAsync(databasePath, lastDateUserReportedAFeedback).ConfigureAwait(false);

                AgentTimingService agentTimingService =
                    new AgentTimingService(configuration, NullLogger<AgentTimingService>.Instance);

                DateTime expectedDateTimeToBeSaved = DateTime.Now.AddDays(-5);
                List<DateTime> dateTimes = new List<DateTime>
                {
                    DateTime.Now.AddDays(-7),
                    DateTime.Now.AddDays(1),
                    DateTime.Now.AddDays(3),
                    DateTime.Now.AddDays(2),
                    expectedDateTimeToBeSaved,
                    DateTime.Now.AddDays(-6),
                };

                agentTimingService.SignalDatesGivenFeedbackByUser(dateTimes);

                agentTimingService.Dispose();

                string dateTimeText = await File.ReadAllTextAsync(databasePath).ConfigureAwait(false);
                Assert.Equal(expectedDateTimeToBeSaved.ToString(TimeConsts.TimeFormat), dateTimeText);
            }
            finally
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }
}