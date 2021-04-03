using System.Threading.Tasks;
using TaskData.TasksGroups;

namespace TaskerAgent.App.Services.RepetitiveTasksUpdaters
{
    public interface IRepetitiveTasksUpdater
    {
        Task UpdateGroupByMessage(string message);
        Task Update(ITasksGroup tasksGroup);
    }
}