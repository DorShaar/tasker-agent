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

        /// <summary>
        /// Geting a synchronization token for all the events in the range of
        /// previous 31 days and next 31 days from today.
        /// </summary>
        Task InitialFullSynchronization();
    }
}