using System;

namespace TaskerAgent.Domain
{
    [Flags]
    public enum Days
    {
        EveryDay = 0,
        Sunday = 1,
        Monday = 2,
        Tuedsay = 4,
        Wednesday = 8,
        Thursday = 16,
        Friday = 32,
        Saturday = 64,
    }
}