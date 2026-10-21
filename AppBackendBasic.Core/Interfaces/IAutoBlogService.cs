using System.Threading.Tasks;

namespace AppBackendBasic.Core.Interfaces
{
    public interface IAutoBlogService
    {
        Task PublishScheduledArticlesAsync();
    }
}
