using System.Threading.Tasks;

namespace AppBackendBasic.Core.Interfaces
{
    public interface IAiTrackingService
    {
        Task TrackAllActivePromptsAsync();
    }
}
