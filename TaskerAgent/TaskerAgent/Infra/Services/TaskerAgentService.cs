using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskData.TasksGroups;
using TaskData.WorkTasks;
using TaskerAgent.App.Persistence.Repositories;
using TaskerAgent.Infra.Extensions;
using TaskerAgent.Infra.Options.Configurations;
using Triangle.Time;

namespace TaskerAgent.Infra.Services
{
    public class TaskerAgentService //: ITaskerAgentService // TODO 
    {
        private readonly IDbRepository<ITasksGroup> mTasksGroupRepository;
        private readonly ITasksGroupFactory mTaskGroupFactory;
        private readonly IOptionsMonitor<TaskerAgentConfigurtaion> mTaskerAgentOptions; // tODO
        private readonly ILogger<TaskerAgentService> mLogger;

        // TODO every morning writes all the todays tasks.
        // TODO every Sunday writes all this week tasks.

        // TODO calendar tasks + reminders.
        public TaskerAgentService(IDbRepository<ITasksGroup> TaskGroupRepository,
            ITasksGroupFactory tasksGroupBuilder,
            ILogger<TaskerAgentService> logger)
        {
            mTasksGroupRepository = TaskGroupRepository ?? throw new ArgumentNullException(nameof(TaskGroupRepository));
            mTaskGroupFactory = tasksGroupBuilder ?? throw new ArgumentNullException(nameof(tasksGroupBuilder));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<IWorkTask>> GetTodaysTasks()
        {
            string todayDate = DateTime.Now.ToString(TimeConsts.TimeFormat);

            ITasksGroup todaysTasksGroup = await mTasksGroupRepository.FindAsync(todayDate).ConfigureAwait(false);

            return todaysTasksGroup.GetAllTasks();
        }

        public async Task<IEnumerable<IWorkTask>> GetThisWeekTasks()
        {
            List<IWorkTask> thisWeekTasks = new List<IWorkTask>();

            foreach (DateTime date in GetThisWeekDates())
            {
                ITasksGroup todaysTasksGroup = await mTasksGroupRepository.FindAsync(date.ToString(TimeConsts.TimeFormat)).ConfigureAwait(false);

                thisWeekTasks.AddRange(todaysTasksGroup.GetAllTasks());
            }

            return thisWeekTasks;
        }

        private IEnumerable<DateTime> GetThisWeekDates()
        {
            DateTime startOfWeekDate = DateTime.Now.StartOfWeek();

            for (int i = 0; i < 7; ++i)
            {
                yield return startOfWeekDate.AddDays(i);
            }
        }

        // TODO read from config the tasks.
        public async Task UpdateRepetitiveTasks() 
        {
            ReadRepetetiveTasksFromInputFile();

            //if (WasUpdatePerformed()) // TODO
            //    UpdateDatabase();
        }

        private async Task ReadRepetetiveTasksFromInputFile()
        {

        }

        // TODO Create summary with score on Saturday.
        //public async Task<string> SendDailySummary() // tODO
        //{

        //}

        //public async Task<string> SendWeeklySummary() // tODO
        //{

        //}

        //public async Task<string> SendMonthlySummary() // tODO
        //{

        //}
    }
}