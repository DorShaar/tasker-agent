﻿using System;
using System.Collections.Generic;
using TaskData.WorkTasks.Producers;
using TaskerAgent.App.TasksProducers;

namespace TaskerAgent.Domain.RepetitiveTasks.TasksProducers
{
    public class TasksProducerFactory : ITasksProducerFactory
    {
        private readonly WorkTaskProducer mWorkTaskProducer = new WorkTaskProducer();

        public IWorkTaskProducer CreateDailyProducer(MeasureType measureType, int expected, int score)
        {
            return new DailyRepetetiveTaskProducer(Frequency.Daily, measureType, expected, score);
        }

        public IWorkTaskProducer CreateWeeklyProducer(MeasureType measureType, Days occurrenceDays, int expected, int score)
        {
            return new WeeklyRepetetiveTaskProducer(Frequency.Weekly, measureType, occurrenceDays, expected, score);
        }

        public IWorkTaskProducer CreateDuWeeklyProducer(MeasureType measureType, Days occurrenceDays, int expected, int score)
        {
            return new DuWeeklyRepetetiveTaskProducer(Frequency.DuWeekly, measureType, occurrenceDays, expected, score);
        }

        public IWorkTaskProducer CreateMonthlyProducer(MeasureType measureType, List<int> daysOfMonth, int expected, int score)
        {
            return new MonthlyRepetetiveTaskProducer(Frequency.Monthly, measureType, daysOfMonth, expected, score);
        }

        public IWorkTaskProducer CreateWhyTasksProducer(Frequency frequency)
        {
            return new WhyTaskProducer(frequency);
        }

        public IWorkTaskProducer CreateRegularTaskProducer(DateTime dateTime)
        {
            return new RegularWorkTaskProducer(mWorkTaskProducer, dateTime);
        }
    }
}