namespace TaskerAgent.Domain.TaskGroup
{
    public enum ComparisonResult
    {
        NoResult = 0,
        Equal = 1,
        TasksContentChanged = 2,
        TasksAddedOrRemoved
    }
}