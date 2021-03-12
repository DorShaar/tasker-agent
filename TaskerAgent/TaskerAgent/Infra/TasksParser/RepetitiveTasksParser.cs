﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using TaskData.OperationResults;
using TaskData.TasksGroups;
using TaskData.WorkTasks;
using TaskerAgent.App.TasksProducers;
using TaskerAgent.Domain;
using TaskerAgent.Infra.Options.Configurations;

namespace TaskerAgent.Infra.TasksParser
{
    public class RepetitiveTasksParser
    {
        private readonly ITasksGroupFactory mTaskGroupFactory;
        private readonly ITasksProducerFactory mTasksProducerFactory;
        private readonly IOptionsMonitor<TaskerAgentConfiguration> mTaskerAgentOptions;
        private readonly ILogger<RepetitiveTasksParser> mLogger;

        public RepetitiveTasksParser(ITasksGroupFactory taskGroupFactory,
            ITasksProducerFactory tasksProducerFactory,
            IOptionsMonitor<TaskerAgentConfiguration> taskerAgentOptions,
            ILogger<RepetitiveTasksParser> logger)
        {
            mTaskGroupFactory = taskGroupFactory ?? throw new ArgumentNullException(nameof(taskGroupFactory));
            mTasksProducerFactory = tasksProducerFactory ?? throw new ArgumentNullException(nameof(tasksProducerFactory));
            mTaskerAgentOptions = taskerAgentOptions ?? throw new ArgumentNullException(nameof(taskerAgentOptions));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void ParseIntoGroup(ITasksGroup taskGroup)
        {
            IEnumerable<string> lines = File.ReadLines(mTaskerAgentOptions.CurrentValue.InputFilePath);

            foreach (string line in lines)
            {
                string[] parameters = line.Split(',');

                CreateRepetitiveTaskFromParameters(taskGroup, parameters);
            }
        }

        private void CreateRepetitiveTaskFromParameters(ITasksGroup taskGroup, string[] parameters)
        {
            try
            {
                string taskDescription = parameters[0];
                string frequencyString = parameters[1];
                string expectedString = parameters[2];
                string measureTypeString = parameters[3];

                if (!Enum.TryParse(frequencyString, ignoreCase: true, out Frequency frequency))
                    return;

                if (!Enum.TryParse(measureTypeString, ignoreCase: true, out MeasureType measureType))
                    return;

                if (!int.TryParse(expectedString, out int expected))
                    return;

                IWorkTaskProducer taskProducer =
                    mTasksProducerFactory.CreateProducer(frequency, measureType, expected, score: 1);

                OperationResult<IWorkTask> createTaskResult =
                    mTaskGroupFactory.CreateTask(taskGroup, taskDescription, taskProducer);

                if (!createTaskResult.Success)
                    mLogger.LogError($"Could not create task {taskDescription}");
            }
            catch (IndexOutOfRangeException ex)
            {
                mLogger.LogError(ex, "Failed to Parse");
            }
        }
    }
}