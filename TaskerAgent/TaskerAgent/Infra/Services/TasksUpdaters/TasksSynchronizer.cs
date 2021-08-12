using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TaskData.TasksGroups;
using TaskData.WorkTasks;
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
        private readonly EventsServerAndLocalMapper mEventsServerAndLocalMapper;
        private readonly IOptionsMonitor<TaskerAgentConfiguration> mTaskerAgentOptions;
        private readonly ILogger<TasksSynchronizer> mLogger;

        public TasksSynchronizer(FileTasksParser tasksParser,
            ICalendarService calendarService,
            EventsServerAndLocalMapper eventsServerToLocalMapper,
            IOptionsMonitor<TaskerAgentConfiguration> taskerAgentOptions,
            ILogger<TasksSynchronizer> logger)
        {
            mTasksParser = tasksParser ?? throw new ArgumentNullException(nameof(tasksParser));
            mCalendarService = calendarService ?? throw new ArgumentNullException(nameof(calendarService));
            mEventsServerAndLocalMapper = eventsServerToLocalMapper ?? throw new ArgumentNullException(nameof(eventsServerToLocalMapper));
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

            await LogLocalUnsynchornized(tasksFromConfigGroup, eventsInfo).ConfigureAwait(false);
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

        // TODO rename log method
        /// <summary>
        /// Logs all the local events which are not found in the server events.
        /// Logs all the server events which are not found in the local events.
        /// </summary>
        private async Task LogUnsynchornized(IEnumerable<ITasksGroup> localEventsGroups,
            IEnumerable<EventInfo> serverEvents)
        {
            //List<string> localTaskIds = new List<string>(); // TODO

            foreach (ITasksGroup tasksGroup in localEventsGroups)
            {
                foreach (IWorkTask workTask in tasksGroup.GetAllTasks())
                {
                    mLogger.LogDebug($"Task {workTask.Description} of ID {workTask.ID} is registered");
                    if (mEventsServerAndLocalMapper.TryGetValue(workTask.ID, out List<string> serverRegisteredEventsId))
                    {
                        mLogger.LogDebug($"Task {workTask.Description} of ID {workTask.ID} is registered. " +
                            "Checking if any of the server events is registered as well");

                        EventInfo eventInfo = serverEvents.FirstOrDefault(eventInfo => eventInfo.LocalId == workTask.ID);

                        if (eventInfo == null)
                        {

                            // TODO think how to check unsynchronized - from serverEvents and / or from serverRegisteredEventsId.
                            mLogger.LogInformation($"Task {workTask.Description} of ID {workTask.ID} is not ");
                            mEventsServerAndLocalMapper.Add
                        }


                        foreach (string serverEventId in serverRegisteredEventsId)
                        {
                        }
                        continue;
                    }

                    mLogger.LogInformation($"Task {workTask.Description} of ID {workTask.ID} is registered");
                    mEventsServerAndLocalMapper.AddLocalUnregisteredEventId(workTask.ID);
                }
            }

            await mEventsServerAndLocalMapper.Add().ConfigureAwait(false);
        }
    }
}