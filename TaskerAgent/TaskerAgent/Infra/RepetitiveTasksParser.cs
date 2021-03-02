using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using TaskData.TasksGroups;
using TaskData.WorkTasks;
using TaskerAgent.App.RepetitiveTasks;
using TaskerAgent.Domain;
using TaskerAgent.Domain.RepetitiveTasks;
using TaskerAgent.Infra.Options.Configurations;

namespace TaskerAgent.Infra
{
    public class RepetitiveTasksParser
    {
        private readonly ITasksGroupFactory mTaskGroupFactory;

        public RepetitiveTasksParser(ITasksGroupFactory taskGroupFactory)
        {
            mTaskGroupFactory = taskGroupFactory ?? throw new ArgumentNullException(nameof(taskGroupFactory));
        }

        public IEnumerable<IRepetitiveTask> Parse(ITasksGroup taskGroup, IOptionsMonitor<TaskerAgentConfiguration> options)
        {
            IEnumerable<string> lines = File.ReadLines(options.CurrentValue.InputFilePath);

            foreach (string line in lines)
            {
                string[] parameters = line.Split(',');

                IRepetitiveTask repetitiveTask = CreateRepetitiveTaskFromParameters(taskGroup, parameters);
                if (repetitiveTask != null)
                    yield return repetitiveTask;
            }
        }

        private IRepetitiveTask CreateRepetitiveTaskFromParameters(ITasksGroup taskGroup, string[] parameters)
        {
            try
            {
                string taskDescription = parameters[0];
                string frequencyString = parameters[1];
                string expectedString = parameters[2];
                string measureTypeString = parameters[3];

                IWorkTask workTask = mTaskGroupFactory.CreateTask(taskGroup, taskDescription);

                if (!Enum.TryParse(frequencyString, ignoreCase: true, out Frequency frequency))
                    return null;

                if (!Enum.TryParse(measureTypeString, ignoreCase: true, out MeasureType measureType))
                    return null;

                if (!int.TryParse(expectedString, out int expected))
                    return null;

                IMeasureableTask measureableTask = new MeasureableTask(measureType, expected, score: 1);

                return new RepetitiveMeasureableTask(workTask, frequency, measureableTask);
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
        }
    }
}