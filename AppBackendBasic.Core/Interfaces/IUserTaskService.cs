using System;
using System.Threading.Tasks;

namespace AppBackendBasic.Core.Interfaces
{
    public interface IUserTaskService
    {
        Task GenerateTasksFromAuditsAsync(int projectId);
        Task CreateTaskAsync(int projectId, string title, string description, string priority);
    }
}
