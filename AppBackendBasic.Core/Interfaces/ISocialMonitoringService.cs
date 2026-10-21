using System;
using System.Threading.Tasks;

namespace AppBackendBasic.Core.Interfaces
{
    public interface ISocialMonitoringService
    {
        Task ScanOpportunitiesAsync();
    }
}
