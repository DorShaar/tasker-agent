using System;
using System.Threading.Tasks;
using TaskerAgent.Domain.Synchronization;

namespace TaskerAgent.App.Services.TasksUpdaters
{
    public interface ITasksSynchronizer
    {
        Task<SyncObjects> GetUnsynchronizeObjects(DateTime lastTimeSynchronized, DateTime lowerTimeBoundary, DateTime upperTimeBoundary);
        Task Synchronize();
    }
}