using System;
using System.Threading.Tasks;

namespace AppBackendBasic.Core.Interfaces
{
    public interface ISiteAuditService
    {
        Task RunFullAuditAsync(int projectId);
        Task RunAllProjectAuditsAsync();
    }
}
