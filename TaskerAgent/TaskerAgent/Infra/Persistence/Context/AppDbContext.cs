using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ObjectSerializer.JsonService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TaskData.IDsProducer;
using TaskData.TasksGroups;
using TaskerAgent.Infra.Consts;
using TaskerAgent.Infra.Options.Configurations;
using TaskerAgent.Infra.Persistence.Context.Serialization;
using static ObjectSerializer.JsonService.JsonSerializerWrapper;

namespace TaskerAgent.Infra.Persistence.Context
{
    public class AppDbContext
    {
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

        public async Task<ITasksGroup> FindAsync(string entityToFind)
        {
            string databasePath = GetDatabasePath(entityToFind);

            if (!File.Exists(databasePath))
            {
                mLogger.LogError($"Could not find database file {databasePath}");
                return null;
            }

            mLogger.LogDebug($"Going to load database from {databasePath}");
            return await mSerializer.Deserialize<ITasksGroup>(databasePath)
                .ConfigureAwait(false);
        }

        private async Task LoadNextIdToProduce()
        {
            if (!File.Exists(NextIdPath))
            {
                mLogger.LogError($"Database file {NextIdPath} does not exists");
                throw new FileNotFoundException("Database does not exists", NextIdPath);
            }

            mLogger.LogDebug("Going to load next id");
            mIdProducer.SetNextID(await mSerializer.Deserialize<int>(NextIdPath).ConfigureAwait(false));
        }

        public async Task SaveCurrentDatabase(ITasksGroup newGroup)
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

        private async Task SaveNextId()
        {
            await mSerializer.Serialize(mIdProducer.PeekForNextId(), NextIdPath).ConfigureAwait(false);
        }
    }
}