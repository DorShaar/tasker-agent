using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskerAgent.Infra.Persistence.Context;

namespace TaskerAgent.Domain.Synchronization
{
    public class EventsServerToLocalMapper
    {
        private readonly AppDbContext mAppDbContext;

        /// <summary>
        /// Mapps between local event id to a list of server events ids.
        /// </summary>
        private readonly Dictionary<string, List<string>> mMapper = new Dictionary<string, List<string>>();

        public EventsServerToLocalMapper(AppDbContext appDbContext)
        {
            mAppDbContext = appDbContext ?? throw new ArgumentNullException(nameof(appDbContext));

            mMapper = mAppDbContext.LoadEventsMapper().Result;
        }

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
    }
}