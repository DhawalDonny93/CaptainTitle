using System.Globalization;
using AppBackendBasic.AdminUI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json.Linq;

namespace AppBackendBasic.AdminUI.Pages.Captions;

public class CatalogModel : PageModel
{
    private readonly ApiClient _api;
    private readonly AuthService _auth;
    private readonly ILogger<CatalogModel> _logger;

    public CatalogModel(ApiClient api, AuthService auth, ILogger<CatalogModel> logger) { _api = api; _auth = auth; _logger = logger; }

    public CatalogKind Kind { get; private set; } = null!;
    public JArray Rows { get; private set; } = new();
    [TempData] public string? SuccessMessage { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string kind)
    {
        if (!_auth.IsAuthenticated()) return RedirectToPage("/Login");
        var k = CaptionCatalogSchema.Find(kind);
        if (k == null) return NotFound();
        Kind = k;
        try { Rows = await _api.GetAsync<JArray>(k.ApiPath) ?? new JArray(); }
        catch (UnauthorizedAccessException) { return RedirectToPage("/Login"); }
        catch (Exception ex) { _logger.LogError(ex, "Loading {Kind}", kind); ErrorMessage = "Could not load data from the API."; }
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(string kind, int? id)
    {
        if (!_auth.IsAuthenticated()) return RedirectToPage("/Login");
        var k = CaptionCatalogSchema.Find(kind);
        if (k == null) return NotFound();

        var body = new Dictionary<string, object?>();
        foreach (var f in k.Fields)
        {
            var raw = Request.Form[$"f_{f.Name}"].ToString().Trim();
            switch (f.Type)
            {
                case FieldType.Bool: body[f.Name] = Request.Form.ContainsKey($"f_{f.Name}") && raw != "false"; break;
                case FieldType.Number:
                    if (raw.Length == 0) body[f.Name] = f.Required ? 0 : null;
                    else if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var n)) body[f.Name] = n;
                    else { ErrorMessage = $"'{f.Label}' must be a number."; return RedirectToPage(new { kind }); }
                    break;
                default: body[f.Name] = raw.Length == 0 ? (f.Required ? "" : null) : raw; break;
            }
        }
        try
        {
            if (id is > 0) await _api.PutAsync<Dictionary<string, object?>, JObject>($"{k.ApiPath}/{id}", body);
            else await _api.PostAsync<Dictionary<string, object?>, JObject>(k.ApiPath, body);
            SuccessMessage = $"{k.Title}: saved.";
        }
        catch (UnauthorizedAccessException) { return RedirectToPage("/Login"); }
        catch (Exception ex) { _logger.LogWarning(ex, "Saving {Kind}", kind); ErrorMessage = "Save failed - check required fields, unique keys, and JSON validity."; }
        return RedirectToPage(new { kind });
    }

    public async Task<IActionResult> OnPostDeleteAsync(string kind, int id)
    {
        if (!_auth.IsAuthenticated()) return RedirectToPage("/Login");
        var k = CaptionCatalogSchema.Find(kind);
        if (k == null) return NotFound();
        try
        {
            if (await _api.DeleteAsync($"{k.ApiPath}/{id}")) SuccessMessage = "Deleted.";
            else ErrorMessage = "Delete refused - the item is in use. Deactivate it instead.";
        }
        catch (UnauthorizedAccessException) { return RedirectToPage("/Login"); }
        catch (Exception ex) { _logger.LogWarning(ex, "Deleting {Kind}", kind); ErrorMessage = "Delete failed."; }
        return RedirectToPage(new { kind });
    }
}
