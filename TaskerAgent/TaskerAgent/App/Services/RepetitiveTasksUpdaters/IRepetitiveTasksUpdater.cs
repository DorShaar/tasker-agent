using System.Threading.Tasks;
using TaskData.TasksGroups;
using TaskerAgent.Infra.Services.Email;

namespace TaskerAgent.App.Services.RepetitiveTasksUpdaters
{
    public interface IRepetitiveTasksUpdater
    {
        Task UpdateGroupByMessage(MessageInfo message);
        Task Update(ITasksGroup tasksGroup);
    }
}