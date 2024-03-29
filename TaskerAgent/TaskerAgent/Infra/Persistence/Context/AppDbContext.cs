﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TaskData.IDsProducer;
using TaskData.ObjectSerializer.JsonService;
using TaskData.TasksGroups;
using TaskerAgent.Domain.TaskGroup;
using TaskerAgent.Infra.Consts;
using TaskerAgent.Infra.Options.Configurations;
using TaskerAgent.Infra.Persistence.Context.Serialization;
using static TaskData.ObjectSerializer.JsonService.JsonSerializerWrapper;

namespace TaskerAgent.Infra.Persistence.Context
{
    public class AppDbContext
    {
        private const string FirstId = "1000";

        private readonly IObjectSerializer mSerializer;
        private readonly IOptionsMonitor<TaskerAgentConfiguration> mConfiguration;
        private readonly IIDProducer mIdProducer;
        private readonly ILogger<AppDbContext> mLogger;

        private readonly string NextIdPath;

        public AppDbContext(IOptionsMonitor<TaskerAgentConfiguration> configuration,
            IObjectSerializer serializer,
            IIDProducer idProducer,
            ILogger<AppDbContext> logger)
        {
            mConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            mSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            mIdProducer = idProducer ?? throw new ArgumentNullException(nameof(idProducer));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (!Directory.Exists(mConfiguration.CurrentValue.DatabaseDirectoryPath))
            {
                mLogger.LogError($"No database directory found in path {mConfiguration.CurrentValue.DatabaseDirectoryPath}");
                return;
            }

            NextIdPath = Path.Combine(mConfiguration.CurrentValue.DatabaseDirectoryPath, AppConsts.NextIdHolderName);
            InitializeSerializer();
        }

        private void InitializeSerializer()
        {
            mSerializer.RegisterConverters(new TaskGroupConverter());
            mSerializer.RegisterConverters(new TaskStatusHistoryConverter());
            mSerializer.RegisterConverters(new RepetitiveTaskConverter());
        }

        public IEnumerable<string> ListGroupsNames()
        {
            foreach (string groupName in Directory.EnumerateFiles(mConfiguration.CurrentValue.DatabaseDirectoryPath))
            {
                if (groupName == AppConsts.NextIdHolderName)
                    continue;

                yield return groupName;
            }
        }

        public async Task LoadDatabase()
        {
            try
            {
                await LoadNextIdToProduce().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                mLogger.LogError(ex, "Unable to deserialize whole information");
            }
        }

        private async Task LoadNextIdToProduce()
        {
            if (!File.Exists(NextIdPath))
            {
                mLogger.LogWarning($"Database file {NextIdPath} does not exists");
                await CreateDatabaseNextIdFile().ConfigureAwait(false);
            }

            mLogger.LogDebug("Going to load next id");
            mIdProducer.SetNextID(await mSerializer.Deserialize<int>(NextIdPath).ConfigureAwait(false));
        }

        private async Task CreateDatabaseNextIdFile()
        {
            await File.WriteAllTextAsync(NextIdPath, FirstId).ConfigureAwait(false);
        }

        public async Task<DailyTasksGroup> FindAsync(string entityToFind)
        {
            string databasePath = GetDatabasePath(entityToFind);

            if (!File.Exists(databasePath))
            {
                mLogger.LogInformation($"Could not find database file {databasePath}");
                return null;
            }

            mLogger.LogDebug($"Going to load database from {databasePath}");
            return await mSerializer.Deserialize<DailyTasksGroup>(databasePath)
                .ConfigureAwait(false);
        }

        public async Task AddToDatabase(ITasksGroup newGroup)
        {
            string databasePath = GetDatabasePath(newGroup.Name);

            if (string.IsNullOrEmpty(databasePath))
            {
                mLogger.LogError("No database path was given");
                return;
            }

            if (string.IsNullOrEmpty(NextIdPath))
            {
                mLogger.LogError("No next id path was given");
                return;
            }

            try
            {
                await SaveTasksGroups(newGroup, databasePath).ConfigureAwait(false);
                await SaveNextId().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                mLogger.LogError(ex, $"Unable to serialize database in {mConfiguration.CurrentValue.DatabaseDirectoryPath}");
            }
        }

        /// <summary>
        /// Should be called when group is updated and has no deleted or new tasks.
        /// </summary>
        public async Task UpdateGroupWithoutNewTasks(ITasksGroup newGroup)
        {
            string databasePath = GetDatabasePath(newGroup.Name);

            if (string.IsNullOrEmpty(databasePath))
            {
                mLogger.LogError("No database path was given");
                return;
            }

            try
            {
                await SaveTasksGroups(newGroup, databasePath).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                mLogger.LogError(ex, $"Unable to serialize database in {mConfiguration.CurrentValue.DatabaseDirectoryPath}");
            }
        }

        /// <summary>
        /// Should be called when group is updated and has deleted or new tasks.
        /// </summary>
        public async Task UpdateGroupWithNewTasks(ITasksGroup newGroup)
        {
            await AddToDatabase(newGroup).ConfigureAwait(false);
        }

        private string GetDatabasePath(string groupName)
        {
            string databaseName = groupName.Replace('\\', '-');
            databaseName = databaseName.Replace('/', '-');

            return Path.Combine(mConfiguration.CurrentValue.DatabaseDirectoryPath, databaseName);
        }

        private async Task SaveTasksGroups(ITasksGroup newGroup, string databasePath)
        {
            mLogger.LogDebug($"Saving group {newGroup.Name} into path {databasePath}");
            await mSerializer.Serialize(newGroup, databasePath).ConfigureAwait(false);
        }

        public async Task SaveNextId()
        {
            await mSerializer.Serialize(mIdProducer.PeekForNextId(), NextIdPath).ConfigureAwait(false);
        }
    }
}