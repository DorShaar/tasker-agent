using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskerAgent.Infra.Persistence.Context;

namespace TaskerAgent.Domain.Synchronization
{
    public class EventsServerAndLocalMapper
    {
        private readonly AppDbContext mAppDbContext;

        /// <summary>
        /// Mapps between local event id to a list of server events ids.
        /// </summary>
        private readonly Dictionary<string, List<string>> mMapper = new Dictionary<string, List<string>>();
        private readonly List<string> mUnregisteredLocalEvents = new List<string>();
        private readonly List<string> mUnregisteredServerEvents = new List<string>();

        public EventsServerAndLocalMapper(AppDbContext appDbContext)
        {
            mAppDbContext = appDbContext ?? throw new ArgumentNullException(nameof(appDbContext));

            mMapper = mAppDbContext.LoadEventsMapper().Result;
        }

        public IEnumerable<string> LocalEventIds => mMapper.Keys;

        public async Task Add(string localEventId, string serverEventId)
        {
            if (!mMapper.TryGetValue(localEventId, out List<string> serverEventIds))
            {
                mMapper.Add(localEventId, new List<string> { serverEventId });
                return;
            }

            serverEventIds.Add(serverEventId);
            await mAppDbContext.SaveEventsMapper(mMapper).ConfigureAwait(false);
        }

        public bool TryGetValue(string localEventId, out List<string> serverEventId)
        {
            return mMapper.TryGetValue(localEventId, out serverEventId);
        }

        public void AddUnregisteredLocalEventId(string localEventId)
        {
            mUnregisteredLocalEvents.Add(localEventId);
        }

        public void AddUnregisteredServerEventId(string localEventId)
        {
            mUnregisteredServerEvents.Add(localEventId);
        }

        public void ManualAdd()
        {
            
        }
    }
}