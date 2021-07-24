using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TaskerAgent.App.Services.Calendar;
using TaskerAgent.App.Services.TasksUpdaters;
using TaskerAgent.Domain.Synchronization;

namespace TaskerAgent.Infra.Services.TasksUpdaters
{
    public class TasksSynchronizer : ITasksSynchronizer
    {
        // TODO
        //private readonly IDbRepository<DailyTasksGroup> mTasksGroupRepository;
        //private readonly ITasksGroupFactory mTaskGroupFactory;
        //private readonly ITasksProducerFactory mTasksProducerFactory;
        //private readonly ITasksGroupProducer mTasksGroupProducer;
        //private readonly IOptionsMonitor<TaskerAgentConfiguration> mTaskerAgentOptions;
        private readonly ICalendarService mCalendarService;
        private readonly ILogger<TasksSynchronizer> mLogger;

        public TasksSynchronizer(ICalendarService calendarService,
            ILogger<TasksSynchronizer> logger)
        {
            mCalendarService = calendarService ?? throw new ArgumentNullException(nameof(calendarService));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async  Task<SyncObjects> GetUnsynchronizeObjects(DateTime lastTimeSynchronized,
            DateTime lowerTimeBoundary,
            DateTime upperTimeBoundary)
        {
            // TOOD use https://developers.google.com/calendar/api/guides/sync.

            await mCalendarService.PullEvents(lowerTimeBoundary, upperTimeBoundary).ConfigureAwait(false);


        }

        public Task Synchronize()
        {
            // TODO
            throw new NotImplementedException();
        }
    }
}