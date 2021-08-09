using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TaskerAgent.Domain;
using TaskerAgent.Domain.Calendar;

namespace TaskerAgent.App.Services.Calendar
{
    public interface ICalendarService : IDisposable, IAsyncDisposable
    {
        Task Connect(CancellationToken cancellationToken = default);
        Task<IEnumerable<EventInfo>> PullUpdatedEvents(string syncToken);
        Task<IEnumerable<EventInfo>> PullEvents(DateTime lowerTimeBoundary, DateTime upperTimeBoundary);
        Task PushEvent(string summary, DateTime start, DateTime end, Frequency frequency);

        /// <summary>
        /// Geting a synchronization token for all the events in the range of
        /// <paramref name="lowerTimeBoundary"/> and <paramref name="upperTimeBoundary"/>.
        /// </summary>
        Task<string> InitialFullSynchronization(DateTime lowerTimeBoundary, DateTime upperTimeBoundary);
    }
}