using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace AppBackendBasic.Services.Captions;

public interface ICaptionMediaTokenService
{
    string Create(int projectId, int userId);
    /// <summary>Returns (projectId, userId) when the token is authentic and unexpired.</summary>
    (int ProjectId, int UserId)? Validate(string token);
}

/// <summary>
/// A browser &lt;video&gt; element cannot send an Authorization header, and the template uses JWT-in-localStorage
/// (the old app used an httpOnly cookie). Playback therefore uses a short-lived, tamper-proof, project-bound URL token.
/// </summary>
public class CaptionMediaTokenService : ICaptionMediaTokenService
{
    private readonly ITimeLimitedDataProtector _protector;
    private readonly TimeSpan _lifetime;

    public CaptionMediaTokenService(IDataProtectionProvider provider, IOptions<CaptionOptions> options)
    {
        _protector = provider.CreateProtector("CaptainTitle.Media.v1").ToTimeLimitedDataProtector();
        _lifetime = TimeSpan.FromMinutes(Math.Max(1, options.Value.MediaTokenMinutes));
    }

    public string Create(int projectId, int userId) => _protector.Protect($"{projectId}:{userId}", _lifetime);

    public (int ProjectId, int UserId)? Validate(string token)
    {
        try
        {
            var parts = _protector.Unprotect(token).Split(':');
            return parts.Length == 2 && int.TryParse(parts[0], out var p) && int.TryParse(parts[1], out var u) ? (p, u) : null;
        }
        catch { return null; }
    }
}
