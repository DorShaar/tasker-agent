using System;
using System.Collections.Generic;
using TaskerAgent.Domain;

namespace TaskerAgent.Infra.Services.TasksParser
{
    public class ParseComponents
    {
        public Frequency Frequency { get; private set; }
        public MeasureType MeasureType { get; private set; }
        public int Expected { get; private set; }
        public Days OccurrenceDays { get; private set; }
        public List<int> DaysOfMonth { get; private set; }

        public bool SetFrequency(string frequencyString)
        {
            if (!Enum.TryParse(frequencyString, ignoreCase: true, out Frequency frequency))
                return false;

            Frequency = frequency;
            return true;
        }

        public bool SetMeasureType(string measureTypeString)
        {
            if (!Enum.TryParse(measureTypeString, ignoreCase: true, out MeasureType measureType))
                return false;

            MeasureType = measureType;
            return true;
        }

        public bool SetExpected(string expectedString)
        {
            if (!int.TryParse(expectedString, out int expected))
                return false;

            Expected = expected;
            return true;
        }

        public bool SetDaysOfMonth(string[] daysStrings)
        {
            DaysOfMonth = new List<int>();

            foreach (string dayString in daysStrings)
            {
                if (!int.TryParse(dayString, out int day))
                    return false;

                DaysOfMonth.Add(day);
            }

            return true;
        }

        public bool SetOccurrenceDays(string[] daysStrings)
        {
            foreach (string dayString in daysStrings)
            {
                if (!Enum.TryParse(dayString, ignoreCase: true, out Days day))
                    return false;

                OccurrenceDays |= day;
            }

            return true;
        }
    }
}