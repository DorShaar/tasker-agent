using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ObjectSerializer.JsonService;
using System.IO;
using TaskData;
using TaskerAgent.Infra.Options.Configurations;
using TaskerAgent.Infra.Services;

namespace TaskerAgent.Infra.Extensions
{
    public static class DIExtensions
    {
        public static void UseDI(this IServiceCollection services)
        {
            RegisterServices(services);
            RegisterRepositories(services);
            RegisterTaskerCoreComponents(services);
            RegisterDadabases(services);
            RegisterLogger(services);

            AddConfiguration(services);

            //services.AddHostedService<TaskerAgentHostedService>(); // TODO
        }

        private static void RegisterServices(IServiceCollection services)
        {
            //services.AddSingleton<ITaskerAgentService, TaskerAgentService>();
        }

        private static void RegisterRepositories(IServiceCollection services)
        {
            //services.AddSingleton<IDbRepository<ITasksGroup>, TasksGroupRepository>();
            //services.AddSingleton<IDbRepository<IWorkTask>, WorkTaskRepository>();
        }

        private static void RegisterDadabases(IServiceCollection services)
        {
            //services.AddSingleton<IAppDbContext, AppDbContext>();
        }

        private static void RegisterTaskerCoreComponents(IServiceCollection services)
        {
            services.UseJsonObjectSerializer();
            services.UseTaskerDataEntities();
        }

        private static void RegisterLogger(IServiceCollection services)
        {
            services.AddLogging(loggerBuilder =>
                loggerBuilder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        }

        private static void AddConfiguration(IServiceCollection services)
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();

            const string configFileName = "Config.yaml";
            configurationBuilder.AddYamlFile(Path.Combine("Infra", "Options", "Configurations", configFileName), optional: false);

            IConfiguration configuration = configurationBuilder.Build();

            // Binds between IConfiguration to given configurtaion.
            services.Configure<TaskerAgentConfigurtaion>(configuration);
            services.AddOptions();
        }
    }
}