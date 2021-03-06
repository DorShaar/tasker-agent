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

namespace TaskerAgent.Infra.Context
{
    public class AppDbContext
    {
        private readonly IObjectSerializer mSerializer;
        private readonly IOptionsMonitor<TaskerAgentConfiguration> mConfiguration;
        private readonly IIDProducer mIdProducer;
        private readonly ILogger<AppDbContext> mLogger;

        private readonly string NextIdPath;
        private readonly string mDatabaseFilePath;

        public List<ITasksGroup> Entities { get; private set; } = new List<ITasksGroup>();

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

            mDatabaseFilePath = Path.Combine(mConfiguration.CurrentValue.DatabaseDirectoryPath, AppConsts.DatabaseName);
            NextIdPath = Path.Combine(mConfiguration.CurrentValue.DatabaseDirectoryPath, AppConsts.NextIdHolderName);
        }

        public async Task LoadDatabase()
        {
            try
            {
                await LoadTasksGroups().ConfigureAwait(false);
                await LoadNextIdToProduce().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                mLogger.LogError(ex, "Unable to deserialize whole information");
            }
        }

        private async Task LoadTasksGroups()
        {
            if (!File.Exists(mDatabaseFilePath))
            {
                mLogger.LogError($"Database file {mDatabaseFilePath} does not exists");
                throw new FileNotFoundException("Database does not exists", mDatabaseFilePath);
            }

            mLogger.LogDebug($"Going to load database from {mDatabaseFilePath}");
            Entities = await mSerializer.Deserialize<List<ITasksGroup>>(mDatabaseFilePath)
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

        public async Task SaveCurrentDatabase()
        {
            if (string.IsNullOrEmpty(mDatabaseFilePath))
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
                await SaveTasksGroups().ConfigureAwait(false);
                await SaveNextId().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                mLogger.LogError(ex, $"Unable to serialize database in {mConfiguration.CurrentValue.DatabaseDirectoryPath}");
            }
        }

        private async Task SaveTasksGroups()
        {
            await mSerializer.Serialize(Entities, mDatabaseFilePath).ConfigureAwait(false);
        }

        private async Task SaveNextId()
        {
            await mSerializer.Serialize(mIdProducer.PeekForNextId(), NextIdPath).ConfigureAwait(false);
        }
    }
}