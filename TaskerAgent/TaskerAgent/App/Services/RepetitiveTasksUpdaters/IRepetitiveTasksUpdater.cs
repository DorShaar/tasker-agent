using System.Threading.Tasks;
using TaskData.TasksGroups;
using TaskerAgent.Domain.Email;

namespace TaskerAgent.App.Services.RepetitiveTasksUpdaters
{
    public interface IRepetitiveTasksUpdater
    {
        Task UpdateGroupByMessage(MessageInfo message);
        Task Update(ITasksGroup tasksGroup);
    }
}