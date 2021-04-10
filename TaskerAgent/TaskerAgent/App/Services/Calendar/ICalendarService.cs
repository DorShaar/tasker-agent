using System;
using System.Threading.Tasks;
using TaskerAgent.Domain;

namespace TaskerAgent.App.Services.Calendar
{
    public interface ICalendarService : IDisposable, IAsyncDisposable
    {
        Task PullEvents();
        Task PushEvent(string summary, DateTime start, DateTime end, Frequency frequency);
    }
}