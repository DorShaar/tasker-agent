using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using TaskData.Ioc;
using TaskData.ObjectSerializer.JsonService;
using TaskData.TasksGroups.Producers;
using TaskerAgent.App.Persistence.Repositories;
using TaskerAgent.App.Services.Email;
using TaskerAgent.App.Services.TasksUpdaters;
using TaskerAgent.App.TasksProducers;
using TaskerAgent.Domain.RepetitiveTasks.TasksProducers;
using TaskerAgent.Domain.TaskGroup;
using TaskerAgent.Infra.HostedServices;
using TaskerAgent.Infra.Options.Configurations;
using TaskerAgent.Infra.Persistence.Context;
using TaskerAgent.Infra.Persistence.Repositories;
using TaskerAgent.Infra.Services;
using TaskerAgent.Infra.Services.AgentTiming;
using TaskerAgent.Infra.Services.Email;
using TaskerAgent.Infra.Services.SummaryReporters;
using TaskerAgent.Infra.Services.TasksParser;
using TaskerAgent.Infra.Services.TasksUpdaters;

namespace TaskerAgent.Infra.Extensions
{
    public static class DIExtensions
    {
        private const string LogFilesDirectory = "logs";
        private const string LogFileName = "tasker_agent.log";

        public static void UseDI(this IServiceCollection services)
        {
            RegisterServices(services);
            RegisterRepositories(services);
            RegisterTaskerCoreComponents(services);
            RegisterDadabases(services);
            RegisterLogger(services);

            AddConfiguration(services);

            services.AddHostedService<TaskerAgentHostedService>();
        }

        private static void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton<TaskerAgentService>();
            services.AddSingleton<ITasksSynchronizer, TasksSynchronizer>();
            services.AddSingleton<FileTasksParser>();
            services.AddSingleton<SummaryReporter>();
            services.AddSingleton<AgentTimingService>();
            services.AddSingleton<IEmailService, EmailService>();
        }

        private static void RegisterRepositories(IServiceCollection services)
        {
            services.AddSingleton<IDbRepository<DailyTasksGroup>, TasksGroupRepository>();
        }

        private static void RegisterDadabases(IServiceCollection services)
        {
            services.AddSingleton<AppDbContext>();
        }

        private static void RegisterTaskerCoreComponents(IServiceCollection services)
        {
            services.UseJsonObjectSerializer();
            services.UseTaskerDataEntities();
            services.AddSingleton<ITasksProducerFactory, TasksProducerFactory>();
            services.AddSingleton<ITasksGroupProducer, DailyTasksGroupProducer>();
        }

        private static void RegisterLogger(IServiceCollection services)
        {
            services.AddLogging(loggerBuilder =>
                loggerBuilder.AddConsole().SetMinimumLevel(LogLevel.Debug));

#if RELEASE
             services.AddLogging(loggerBuilder =>
                loggerBuilder.AddFile(Path.Combine(LogFilesDirectory, LogFileName), append: true));
#endif
        }

        private static void AddConfiguration(IServiceCollection services)
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();

#if DEBUG
            const string configFileName = "Config.yaml";
            configurationBuilder.AddYamlFile(Path.Combine("Infra", "Options", configFileName), optional: false);
#else
    const string configFileName = "Config.yaml";
            configurationBuilder.AddYamlFile(Path.Combine("Infra", "Options", configFileName), optional: false);
#endif

            IConfiguration configuration = configurationBuilder.Build();

            // Binds between IConfiguration to given configurtaion.
            services.Configure<TaskerAgentConfiguration>(configuration);
            services.AddOptions();
        }
    }
}