﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ObjectSerializer.JsonService;
using System.IO;
using TaskData;
using TaskData.TasksGroups;
using TaskerAgent.App.Persistence.Repositories;
using TaskerAgent.App.TasksProducers;
using TaskerAgent.Domain.RepetitiveTasks.TasksProducers;
using TaskerAgent.Infra.HostedServices;
using TaskerAgent.Infra.Options.Configurations;
using TaskerAgent.Infra.Persistence.Context;
using TaskerAgent.Infra.Persistence.Repositories;
using TaskerAgent.Infra.Services;
using TaskerAgent.Infra.Services.RepetitiveTasksUpdaters;
using TaskerAgent.Infra.Services.SummaryReporters;
using TaskerAgent.Infra.Services.TasksParser;

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

            services.AddHostedService<TaskerAgentHostedService>();
        }

        private static void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton<TaskerAgentService>();
            services.AddSingleton<RepetitiveTasksUpdater>();
            services.AddSingleton<RepetitiveTasksParser>();
            services.AddSingleton<SummaryReporter>();
        }

        private static void RegisterRepositories(IServiceCollection services)
        {
            services.AddSingleton<IDbRepository<ITasksGroup>, TasksGroupRepository>();
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
            services.Configure<TaskerAgentConfiguration>(configuration);
            services.AddOptions();
        }
    }
}