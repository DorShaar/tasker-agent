using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskData.TasksGroups;
using TaskerAgent.App.Persistence.Repositories;
using TaskerAgent.Domain.TaskGroup;
using TaskerAgent.Infra.Extensions;
using TaskerAgent.Infra.Persistence.Context;

namespace TaskerAgent.Infra.Persistence.Repositories
{
    // TODO think if needed.
    public class TasksGroupRepository : IDbRepository<DailyTasksGroup>
    {
        private readonly ILogger<TasksGroupRepository> mLogger;
        private readonly AppDbContext mDatabase;

        public TasksGroupRepository(AppDbContext database, ILogger<TasksGroupRepository> logger)
        {
            mDatabase = database ?? throw new ArgumentNullException(nameof(database));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));

            mDatabase.LoadDatabase().Wait();
        }

        public async Task<bool> AddAsync(DailyTasksGroup newGroup)
        {
            if (await mDatabase.FindAsync(newGroup.Name).ConfigureAwait(false) != null)
            {
                mLogger.LogError($"Group name: {newGroup.Name} is already found in database");
                return false;
            }

            await mDatabase.AddToDatabase(newGroup).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// List all tasks group names only.
        /// </summary>
        public Task<IEnumerable<string>> ListAsync()
        {
            return Task.FromResult(mDatabase.ListGroupsNames());
        }

        public async Task<DailyTasksGroup> FindAsync(string entityToFind)
        {
            return await mDatabase.FindAsync(entityToFind).ConfigureAwait(false);
        }

        public async Task<bool> AddOrUpdateAsync(DailyTasksGroup newGroup)
        {
            ITasksGroup currentTaskGroup = await mDatabase.FindAsync(newGroup.Name).ConfigureAwait(false);

            if (currentTaskGroup == null)
            {
                mLogger.LogInformation($"Could not find group {newGroup.Name} in database. Adding it");
                await mDatabase.AddToDatabase(newGroup).ConfigureAwait(false);
                return true;
            }

            mLogger.LogDebug($"Found group {newGroup.Name} in database. Checking if should be updated");

            ComparisonResult comparisonResult = currentTaskGroup.Compare(newGroup);
            if (comparisonResult == ComparisonResult.Equal)
            {
                mLogger.LogDebug($"Group {newGroup.Name} was not changed. Nothing to update");
                return false;
            }

            if (comparisonResult == ComparisonResult.TasksContentChanged)
            {
                mLogger.LogInformation($"Updating group {newGroup.Name}. Change is in tasks values");
                await mDatabase.UpdateGroupWithoutNewTasks(newGroup).ConfigureAwait(false);
            }
            else if (comparisonResult == ComparisonResult.TasksAddedOrRemoved)
            {
                mLogger.LogInformation($"Updating group {newGroup.Name}. Tasks were deleted or added");
                await mDatabase.UpdateGroupWithNewTasks(newGroup).ConfigureAwait(false);
            }

            return true;
        }

        public Task RemoveAsync(DailyTasksGroup group)
        {
            mLogger.LogError("Not impemented yet");
            return Task.CompletedTask;
        }
    }
}