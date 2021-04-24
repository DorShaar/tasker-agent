using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TaskerAgent.Infra.Options.Configurations;
using TaskerAgent.Infra.Services.AgentTiming;
using Triangle.Time;
using Xunit;

namespace TaskerAgantTests.Infra.Services.AgentTiming
{
    public class AgentTimingServiceTests
    {
        private const string MissingDatesUserReportedFeedbackFileName = "missing_dates_user_reported_feedback";

        [Fact]
        public async Task SignalDatesGivenFeedbackByUser_DateIsSavedAsExpected()
        {
            string tempDirectory = Directory.CreateDirectory(Path.GetRandomFileName()).FullName;

            try
            {
                IOptionsMonitor<TaskerAgentConfiguration> configuration = A.Fake<IOptionsMonitor<TaskerAgentConfiguration>>();
                configuration.CurrentValue.DatabaseDirectoryPath = tempDirectory;

                string databasePath = Path.Combine(
                    configuration.CurrentValue.DatabaseDirectoryPath, MissingDatesUserReportedFeedbackFileName);

                List<DateTime> dateTimesInDatabase = new List<DateTime>
                {
                    DateTime.Parse("12/05/21"),
                    DateTime.Parse("13/06/21"),
                    DateTime.Parse("24/09/21"),
                };

                await PrepareTextFileWithDates(databasePath, dateTimesInDatabase).ConfigureAwait(false);

                List<DateTime> dateTimesGivenFeedback = new List<DateTime>
                {
                    DateTime.Parse("12/05/21"),
                };

                using (AgentTimingService agentTimingService =
                    new AgentTimingService(configuration, NullLogger<AgentTimingService>.Instance))
                {
                    agentTimingService.SignalDatesGivenFeedbackByUser(dateTimesGivenFeedback);
                }

                string[] dateTimesStrings = await File.ReadAllLinesAsync(databasePath).ConfigureAwait(false);
                Assert.Equal(dateTimesStrings[0], dateTimesInDatabase[1].ToString(TimeConsts.TimeFormat));
                Assert.Equal(dateTimesStrings[1], dateTimesInDatabase[2].ToString(TimeConsts.TimeFormat));
                Assert.Equal(dateTimesStrings[2], DateTime.Today.AddDays(1).ToString(TimeConsts.TimeFormat));
            }
            finally
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }

        private async Task PrepareTextFileWithDates(string databasePath, List<DateTime> dateTimesInDatabase)
        {
            StringBuilder stringBuilder = new StringBuilder();

            foreach (DateTime dateTime in dateTimesInDatabase)
            {
                stringBuilder.AppendLine(dateTime.ToString(TimeConsts.TimeFormat));
            }

            await File.WriteAllTextAsync(databasePath, stringBuilder.ToString()).ConfigureAwait(false);
        }
    }
}