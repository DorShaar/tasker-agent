using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TaskerAgent.Domain;
using TaskerAgent.Domain.Calendar;
using TaskerAgent.Domain.TaskerDateTime;
using TaskerAgent.Infra.Options.Configurations;
using TaskerAgent.Infra.Services.Calendar;
using Xunit;

namespace TaskerAgantTests.Infra.Services.Calendar
{
    public class CalendarTaskerServiceTests
    {
        [Fact(Skip = "Requires real calendar. To be tested manually")]
        public async Task PullEvents_AsExpected()
        {
            IOptionsMonitor<TaskerAgentConfiguration> configuration = A.Fake<IOptionsMonitor<TaskerAgentConfiguration>>();
            configuration.CurrentValue.CredentialsPath = @"C:\Dor\Projects\tasker-agent\TaskerAgent\tokens\tasker-agent-email-and-calendar.json";

            CalendarTaskerService calendarService = new CalendarTaskerService(configuration, NullLogger<CalendarTaskerService>.Instance);
            await calendarService.Connect().ConfigureAwait(false);

            IEnumerable<EventInfo> events = await calendarService.PullEvents(DateTime.Today.AddDays(-2), DateTime.Today).ConfigureAwait(false);
        }

        [Fact(Skip = "Requires real calendar. To be tested manually")]
        public async Task PushEvent_AsExpected()
        {
            IOptionsMonitor<TaskerAgentConfiguration> configuration = A.Fake<IOptionsMonitor<TaskerAgentConfiguration>>();
            configuration.CurrentValue.CredentialsPath = @"C:\Dor\Projects\tasker-agent\TaskerAgent\tokens\client_secret_email_and_calendar.json";

            CalendarTaskerService calendarService = new CalendarTaskerService(configuration, NullLogger<CalendarTaskerService>.Instance);
            await calendarService.Connect().ConfigureAwait(false);

            await calendarService.PushEvent(
                "new calendar",
                DateTime.Now.AddDays(1),
                DateTime.Now.AddDays(2),
                Frequency.Weekly).ConfigureAwait(false);
        }

        //[Fact(Skip = "Requires real calendar. To be tested manually")]
        [Fact]
        public async Task InitialFullSynchronization_AsExpected()
        {
            const string SyncTokensDirectory = "sync_tokens";
            string tempDirectory = Directory.CreateDirectory(Path.GetRandomFileName()).FullName;

            try
            {
                IOptionsMonitor<TaskerAgentConfiguration> configuration = A.Fake<IOptionsMonitor<TaskerAgentConfiguration>>();
                configuration.CurrentValue.CredentialsPath = @"C:\Dor\Projects\tasker-agent\TaskerAgent\tokens\tasker-agent-email-and-calendar.json";
                configuration.CurrentValue.DatabaseDirectoryPath = tempDirectory;

                CalendarTaskerService calendarService = new CalendarTaskerService(configuration, NullLogger<CalendarTaskerService>.Instance);
                await calendarService.Connect().ConfigureAwait(false);

                await calendarService.InitialFullSynchronization().ConfigureAwait(false);

                string tokenPath = Path.Combine(tempDirectory, SyncTokensDirectory, DateTime.Now.ToDateName());
                Assert.True(File.Exists(Path.Combine(tempDirectory, tokenPath)));
            }
            finally
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }

        //[Fact(Skip = "Requires real calendar. To be tested manually")]
        [Fact]
        public async Task Synchronize_AsExpected()
        {
            const string SyncTokensDirectory = "sync_tokens";
            string tempDirectory = Directory.CreateDirectory(Path.GetRandomFileName()).FullName;

            try
            {
                IOptionsMonitor<TaskerAgentConfiguration> configuration = A.Fake<IOptionsMonitor<TaskerAgentConfiguration>>();
                configuration.CurrentValue.CredentialsPath = @"C:\Dor\Projects\tasker-agent\TaskerAgent\tokens\tasker-agent-email-and-calendar.json";
                configuration.CurrentValue.DatabaseDirectoryPath = tempDirectory;

                CalendarTaskerService calendarService = new CalendarTaskerService(configuration, NullLogger<CalendarTaskerService>.Instance);
                await calendarService.Connect().ConfigureAwait(false);

                await calendarService.Synchronize().ConfigureAwait(false);
            }
            finally
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }
}