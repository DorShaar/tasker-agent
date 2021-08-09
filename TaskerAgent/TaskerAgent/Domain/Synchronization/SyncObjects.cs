using System.Collections.Generic;

namespace TaskerAgent.Domain.Synchronization
{
    // TODO thinks if needed.
    public class SyncObjects
    {
        public List<SyncRemoteObject> ChangedRemoteObjects { get; } = new List<SyncRemoteObject>();
        public List<SyncLocalObject> ChangedLocalObjects { get; } = new List<SyncLocalObject>();
    }

    public class SyncRemoteObject
    {
        public string EventId { get; set; }
        public string EventName { get; set; }
    }

    public class SyncLocalObject
    {
        public string EventName { get; set; }
        public string EventOtherName { get; set; }
    }
}