using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskerAgent.Domain;
using TaskerAgent.Domain.Calendar;

namespace TaskerAgent.App.Services.Calendar
{
    public interface ICalendarService : IDisposable, IAsyncDisposable
    {
        Task<IEnumerable<EventInfo>> PullEvents(DateTime lowerTimeBoundary, DateTime upperTimeBoundary);
        Task PushEvent(string summary, DateTime start, DateTime end, Frequency frequency);
    }
}