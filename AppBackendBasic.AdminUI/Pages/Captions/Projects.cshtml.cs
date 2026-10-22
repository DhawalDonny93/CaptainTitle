using AppBackendBasic.AdminUI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json.Linq;

namespace AppBackendBasic.AdminUI.Pages.Captions;

public class ProjectsModel : PageModel
{
    private readonly ApiClient _api;
    private readonly AuthService _auth;
    private readonly ILogger<ProjectsModel> _logger;

    public ProjectsModel(ApiClient api, AuthService auth, ILogger<ProjectsModel> logger) { _api = api; _auth = auth; _logger = logger; }

    [BindProperty(SupportsGet = true)] public string? Status { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public JObject Result { get; private set; } = new();
    public JObject? Usage { get; private set; }
    [BindProperty(SupportsGet = true)] public int? UserId { get; set; }
    [TempData] public string? SuccessMessage { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!_auth.IsAuthenticated()) return RedirectToPage("/Login");
        try
        {
            var qs = $"?page={Math.Max(1, PageNumber)}&pageSize=25" + (string.IsNullOrEmpty(Status) ? "" : $"&status={Uri.EscapeDataString(Status)}");
            Result = await _api.GetAsync<JObject>("/admin/captions/projects" + qs) ?? new JObject();
            if (UserId is > 0) Usage = await _api.GetAsync<JObject>($"/admin/captions/usage/{UserId}");
        }
        catch (UnauthorizedAccessException) { return RedirectToPage("/Login"); }
        catch (Exception ex) { _logger.LogError(ex, "Loading caption projects"); ErrorMessage = "Could not load data from the API."; }
        return Page();
    }

    public async Task<IActionResult> OnPostRequeueAsync(int id)
    {
        if (!_auth.IsAuthenticated()) return RedirectToPage("/Login");
        try { await _api.PostAsync<object, JObject>($"/admin/captions/projects/{id}/requeue", new { }); SuccessMessage = $"Project {id} re-queued (no credit is consumed again)."; }
        catch (UnauthorizedAccessException) { return RedirectToPage("/Login"); }
        catch (Exception ex) { _logger.LogWarning(ex, "Requeue {Id}", id); ErrorMessage = "Re-queue failed."; }
        return RedirectToPage(new { Status });
    }

    public async Task<IActionResult> OnPostGrantAsync(int userId, int free, int oneoff, int basic)
    {
        if (!_auth.IsAuthenticated()) return RedirectToPage("/Login");
        try { await _api.PostAsync<object, JObject>($"/admin/captions/usage/{userId}/grant", new { free, oneoff, basic }); SuccessMessage = $"Credits adjusted for user {userId}."; }
        catch (UnauthorizedAccessException) { return RedirectToPage("/Login"); }
        catch (Exception ex) { _logger.LogWarning(ex, "Grant {UserId}", userId); ErrorMessage = "Adjustment failed (does the user exist?)."; }
        return RedirectToPage(new { UserId = userId });
    }
}
