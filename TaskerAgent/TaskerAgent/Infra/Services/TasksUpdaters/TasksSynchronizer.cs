using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TaskData.TasksGroups;
using TaskerAgent.App.Services.Calendar;
using TaskerAgent.App.Services.TasksUpdaters;
using TaskerAgent.Domain.Calendar;
using TaskerAgent.Domain.Synchronization;
using TaskerAgent.Domain.TaskerDateTime;
using TaskerAgent.Infra.Options.Configurations;
using TaskerAgent.Infra.Services.TasksParser;

namespace TaskerAgent.Infra.Services.TasksUpdaters
{
    // https://developers.google.com/calendar/api/guides/sync.
    public class TasksSynchronizer : ITasksSynchronizer
    {
        private const string SyncTokensDirectory = "sync_tokens";

        private readonly FileTasksParser mTasksParser;
        private readonly ICalendarService mCalendarService;
        private readonly IOptionsMonitor<TaskerAgentConfiguration> mTaskerAgentOptions;
        private readonly ILogger<TasksSynchronizer> mLogger;

        public TasksSynchronizer(FileTasksParser tasksParser,
            ICalendarService calendarService,
            IOptionsMonitor<TaskerAgentConfiguration> taskerAgentOptions,
            ILogger<TasksSynchronizer> logger)
        {
            mTasksParser = tasksParser ?? throw new ArgumentNullException(nameof(tasksParser));
            mCalendarService = calendarService ?? throw new ArgumentNullException(nameof(calendarService));
            mTaskerAgentOptions = taskerAgentOptions ?? throw new ArgumentNullException(nameof(taskerAgentOptions));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));

            mCalendarService.Connect().Wait();
        }

        public async Task Synchronize()
        {
            mLogger.LogDebug("Updating repetitive tasks");

            IEnumerable<ITasksGroup> tasksFromConfigGroup =
                await mTasksParser.ParseTasksToWhyGroups().ConfigureAwait(false);

            if (tasksFromConfigGroup == null)
            {
                mLogger.LogError("Could not update repetitive tasks");
                return;
            }

            IEnumerable<EventInfo> eventsInfo = await GetServerEventsAndUpdateSyncToken().ConfigureAwait(false);

            LogLocalUnsynchornized(tasksFromConfigGroup, eventsInfo);
            LogServerUnsynchornized(tasksFromConfigGroup, eventsInfo);
        }

        private async Task<IEnumerable<EventInfo>> GetServerEventsAndUpdateSyncToken()
        {
            (DateTime startOfTheMonth, DateTime endOfTheMonth) = DateTimeUtilities.GetThisMonthRange();
            string syncToken = await GetSyncToken(startOfTheMonth, endOfTheMonth).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(syncToken))
            {
                await SaveNewToken(startOfTheMonth, endOfTheMonth).ConfigureAwait(false);
                return await mCalendarService.PullEvents(startOfTheMonth, endOfTheMonth).ConfigureAwait(false);
            }
            else
            {
                // TODO
                //event2.Status == "confirmed";
                //event2.Status == "cancelled";
                return await mCalendarService.PullUpdatedEvents(syncToken).ConfigureAwait(false);
            }
        }

        private async Task SaveNewToken(DateTime startOfTheMonth, DateTime endOfTheMonth)
        {
            string syncTokensDirectory = Directory.CreateDirectory(Path.Combine(
                    mTaskerAgentOptions.CurrentValue.DatabaseDirectoryPath, SyncTokensDirectory)).FullName;

            string tokenFileName = $"{startOfTheMonth.ToDateName()}_{endOfTheMonth.ToDateName()}";

            string tokenPath = Path.Combine(syncTokensDirectory, tokenFileName);

            string tokenContent = await mCalendarService.InitialFullSynchronization(
                startOfTheMonth, endOfTheMonth).ConfigureAwait(false);

            await File.WriteAllTextAsync(tokenPath, tokenContent).ConfigureAwait(false);
        }

        private async Task<string> GetSyncToken(DateTime startOfTheMonth, DateTime endOfTheMonth)
        {
            string syncTokensDirectory = Path.Combine(
                   mTaskerAgentOptions.CurrentValue.DatabaseDirectoryPath, SyncTokensDirectory);

            if (!Directory.Exists(syncTokensDirectory))
            {
                mLogger.LogInformation($"Sync tokens directory '{syncTokensDirectory}' does not exist");
                return string.Empty;
            }

            string tokenFileName = $"{startOfTheMonth.ToDateName()}_{endOfTheMonth.ToDateName()}";

            string tokenPath = Path.Combine(syncTokensDirectory, tokenFileName);

            if (!File.Exists(tokenPath))
            {
                mLogger.LogInformation($"There is no such token '{tokenPath}'");
                return string.Empty;
            }

            return await File.ReadAllTextAsync(tokenPath).ConfigureAwait(false);
        }

        private void LogLocalUnsynchornized(IEnumerable<ITasksGroup> localEvents, IEnumerable<EventInfo> serverEvents)
        {
            // TODO
        }

        private void LogServerUnsynchornized(IEnumerable<ITasksGroup> localEvents, IEnumerable<EventInfo> serverEvents)
        {
            // TODO
        }
    }
}