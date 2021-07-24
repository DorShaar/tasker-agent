using System.Threading.Tasks;
using TaskData.TasksGroups;
using TaskerAgent.Domain.Email;

namespace TaskerAgent.App.Services.RepetitiveTasksUpdaters
{
    // TODO delete
    public interface IRepetitiveTasksUpdater
    {
        Task<bool> UpdateGroupByMessage(MessageInfo message);
        Task Update(ITasksGroup tasksGroup);
    }
}