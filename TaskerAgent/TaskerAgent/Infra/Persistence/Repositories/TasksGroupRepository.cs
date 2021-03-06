using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskData.TasksGroups;
using TaskData.WorkTasks;
using TaskerAgent.App.Persistence.Repositories;
using TaskerAgent.Infra.Context;

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
            await mDatabase.LoadDatabase().ConfigureAwait(false);

            if (mDatabase.Entities.Contains(newGroup) ||
               (mDatabase.Entities.Find(entity => entity.ID == newGroup.ID) != null))
            {
                mLogger.LogError($"Group ID: {newGroup.ID} is already found in database");
                return false;
            }

            if (mDatabase.Entities.Find(entity => entity.Name == newGroup.Name) != null)
            {
                mLogger.LogError($"Group name: {newGroup.Name} is already found in database");
                return false;
            }

            mDatabase.Entities.Add(newGroup);

            await mDatabase.SaveCurrentDatabase().ConfigureAwait(false);
            return true;
        }

        public async Task<IEnumerable<ITasksGroup>> ListAsync()
        {
            await mDatabase.LoadDatabase().ConfigureAwait(false);

            return mDatabase.Entities.AsEnumerable();
        }

        public async Task<ITasksGroup> FindAsync(string entityToFind)
        {
            await mDatabase.LoadDatabase().ConfigureAwait(false);

            ITasksGroup entityFound =
                mDatabase.Entities.Find(entity => entity.ID == entityToFind) ??
                mDatabase.Entities.Find(entity => entity.Name == entityToFind);

            return entityFound;
        }

        public async Task UpdateAsync(ITasksGroup newGroup)
        {
            int tasksGroupToUpdateIndex = mDatabase.Entities.FindIndex(entity => entity.ID == newGroup.ID);

            if (tasksGroupToUpdateIndex < 0)
            {
                mLogger.LogError($"Group ID: {newGroup.ID} Group name: {newGroup.Name} - No such entity was found in database");
                return;
            }

            mDatabase.Entities[tasksGroupToUpdateIndex] = newGroup;

            await mDatabase.SaveCurrentDatabase().ConfigureAwait(false);
            return;
        }

        public async Task RemoveAsync(ITasksGroup group)
        {
            if (!mDatabase.Entities.Contains(group))
            {
                mLogger.LogError($"Group ID: {group.ID} Group name: {group.Name} - No such entity was found in database");
                return;
            }

            foreach (IWorkTask task in group.GetAllTasks())
            {
                mLogger.LogDebug($"Removing inner task id {task.ID} description {task.Description}");
            }

            mDatabase.Entities.Remove(group);
            mLogger.LogDebug($"Task group id {group.ID} group name {group.Name} removed");

            await mDatabase.SaveCurrentDatabase().ConfigureAwait(false);
            return;
        }
    }
}