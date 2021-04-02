using Microsoft.Extensions.Logging;
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

namespace TaskerAgent.Infra.Services.TasksParser
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
            ParseComponents parseComponents = new ParseComponents();

            try
            {
                string taskDescription = parameters[0];

                if (!parseComponents.SetFrequency(parameters[1]))
                {
                    mLogger.LogError($"Could not set frequency for task {taskDescription}");
                    return;
                }

                if (!parseComponents.SetExpected(parameters[2]))
                {
                    mLogger.LogError($"Could not set expected for task {taskDescription}");
                    return;
                }

                if (!parseComponents.SetMeasureType(parameters[3]))
                {
                    mLogger.LogError($"Could not set measure type for task {taskDescription}");
                    return;
                }

                if (parseComponents.Frequency == Frequency.Weekly)
                {
                    if (!parseComponents.SetOccurrenceDays(parameters[4..]))
                    {
                        mLogger.LogError($"Could not set OccurrenceDays for task {taskDescription}");
                        return;
                    }
                }

                if (parseComponents.Frequency == Frequency.Monthly)
                {
                    if (!parseComponents.SetDaysOfMonth(parameters[4..]))
                    {
                        mLogger.LogError($"Could not set days of month for task {taskDescription}");
                        return;
                    }
                }

                IWorkTaskProducer taskProducer = GetWorkTaskProducer(parseComponents);

                if (taskProducer == null)
                {
                    mLogger.LogError($"Could not create task producer for task {taskDescription}");
                    return;
                }

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

        private IWorkTaskProducer GetWorkTaskProducer(ParseComponents parseComponents)
        {
            if (parseComponents.Frequency == Frequency.Daily)
            {
                return mTasksProducerFactory.CreateDailyProducer(
                    parseComponents.Frequency, parseComponents.MeasureType, parseComponents.Expected, score: 1);
            }

            if (parseComponents.Frequency == Frequency.Weekly)
            {
                return mTasksProducerFactory.CreateWeeklyProducer(
                    parseComponents.Frequency, parseComponents.MeasureType, parseComponents.OccurrenceDays, parseComponents.Expected, score: 1);
            }

            if (parseComponents.Frequency == Frequency.Monthly)
            {
                return mTasksProducerFactory.CreateMonthlyProducer(
                    parseComponents.Frequency, parseComponents.MeasureType, parseComponents.DaysOfMonth, parseComponents.Expected, score: 1);
            }

            return null;
        }
    }
}