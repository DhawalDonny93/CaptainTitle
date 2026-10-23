using AppBackendBasic.AdminUI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json.Linq;

namespace AppBackendBasic.AdminUI.Pages.Captions;

public class IndexModel : PageModel
{
    private readonly ApiClient _api;
    private readonly AuthService _auth;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ApiClient api, AuthService auth, ILogger<IndexModel> logger) { _api = api; _auth = auth; _logger = logger; }

    public JObject Overview { get; private set; } = new();
    public JArray Settings { get; private set; } = new();
    public JObject Health { get; private set; } = new();
    public List<JObject> Jobs { get; private set; } = new();
    [TempData] public string? SuccessMessage { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!_auth.IsAuthenticated()) return RedirectToPage("/Login");
        try
        {
            Overview = await _api.GetAsync<JObject>("/admin/captions/overview") ?? new JObject();
            Settings = await _api.GetAsync<JArray>("/admin/captions/settings") ?? new JArray();
            Health = await _api.GetAsync<JObject>("/admin/captions/health") ?? new JObject();
            var jobs = await _api.GetAsync<JArray>("/admin/llm-job-configs") ?? new JArray();
            Jobs = jobs.OfType<JObject>().Where(j => ((string?)j["jobKey"] ?? "").StartsWith("Caption")).ToList();
        }
        catch (UnauthorizedAccessException) { return RedirectToPage("/Login"); }
        catch (Exception ex) { _logger.LogError(ex, "Loading Captain Title overview"); ErrorMessage = "Could not load data from the API."; }
        return Page();
    }

    public async Task<IActionResult> OnPostSettingAsync(string key, string value)
    {
        if (!_auth.IsAuthenticated()) return RedirectToPage("/Login");
        try
        {
            await _api.PutAsync<object, JObject>($"/admin/captions/settings/{key}", new { value });
            SuccessMessage = $"{key} updated.";
        }
        catch (UnauthorizedAccessException) { return RedirectToPage("/Login"); }
        catch (Exception ex) { _logger.LogWarning(ex, "Updating {Key}", key); ErrorMessage = "Update failed - check the value type."; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostJobAsync(string jobKey, string jobName, string? description, string provider, string model,
        int maxTokens, decimal temperature, bool isActive)
    {
        if (!_auth.IsAuthenticated()) return RedirectToPage("/Login");
        try
        {
            await _api.PutAsync<object, JObject>($"/admin/llm-job-configs/{jobKey}",
                new { jobKey, jobName, description, provider, model, maxTokens, temperature, isActive, category = "Captions" });
            SuccessMessage = $"{jobName}: model settings saved.";
        }
        catch (UnauthorizedAccessException) { return RedirectToPage("/Login"); }
        catch (Exception ex) { _logger.LogWarning(ex, "Saving job {JobKey}", jobKey); ErrorMessage = "Saving the model settings failed."; }
        return RedirectToPage();
    }

}
