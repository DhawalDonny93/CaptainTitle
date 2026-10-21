using Hangfire.Dashboard;

namespace AppBackendBasic.API.Middleware;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // In development, allow all access
        // In production, you should implement proper authentication
        var httpContext = context.GetHttpContext();
        
        // For production, check if user is authenticated and is admin
        // return httpContext.User.Identity?.IsAuthenticated == true && 
        //        httpContext.User.IsInRole("Admin");
        
        // For now, allow in development
        return true;
    }
}
