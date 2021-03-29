using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskData.TasksGroups;
using TaskerAgent.App.Persistence.Repositories;
using TaskerAgent.Infra.Persistence.Context;

namespace TaskerAgent.Infra.Persistence.Repositories
{
    public class TasksGroupRepository : IDbRepository<ITasksGroup>
    {
        private readonly ILogger<TasksGroupRepository> mLogger;
        private readonly AppDbContext mDatabase;

        public TasksGroupRepository(AppDbContext database, ILogger<TasksGroupRepository> logger)
        {
            mDatabase = database ?? throw new ArgumentNullException(nameof(database));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> AddAsync(ITasksGroup newGroup)
        {
            if (await mDatabase.FindAsync(newGroup.Name).ConfigureAwait(false) != null)
            {
                mLogger.LogError($"Group name: {newGroup.Name} is already found in database");
                return false;
            }

            await mDatabase.SaveCurrentDatabase(newGroup).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// List all tasks group names only
        /// </summary>
        public Task<IEnumerable<string>> ListAsync()
        {
            return Task.FromResult(mDatabase.ListGroupsNames());
        }

        public async Task<ITasksGroup> FindAsync(string entityToFind)
        {
            return await mDatabase.FindAsync(entityToFind).ConfigureAwait(false);
        }

        public async Task<bool> AddOrUpdateAsync(ITasksGroup newGroup)
        {
            await mDatabase.SaveCurrentDatabase(newGroup).ConfigureAwait(false);
            return true;
        }

        public Task RemoveAsync(ITasksGroup group)
        {
            mLogger.LogError("Not impemented yet");
            return Task.CompletedTask;

            // TODO
            //if (await mDatabase.FindAsync(group.Name).ConfigureAwait(false) == null)
            //{
            //    mLogger.LogError($"Group ID: {group.ID} Group name: {group.Name} - No such entity was found in database");
            //    return;
            //}

            //foreach (IWorkTask task in group.GetAllTasks())
            //{
            //    mLogger.LogDebug($"Removing inner task id {task.ID} description {task.Description}");
            //}
        }
    }
}