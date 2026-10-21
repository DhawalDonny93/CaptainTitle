using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Repositories;

namespace AppBackendBasic.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly IAppleAuthService _appleAuthService;
    private readonly IUserTeamMappingRepository _userTeamMappingRepository;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(
        IAuthService authService,
        IEmailVerificationService emailVerificationService,
        IAppleAuthService appleAuthService,
        IUserTeamMappingRepository userTeamMappingRepository,
        ILogger<AuthController> logger,
        IConfiguration configuration)
    {
        _authService = authService;
        _emailVerificationService = emailVerificationService;
        _appleAuthService = appleAuthService;
        _userTeamMappingRepository = userTeamMappingRepository;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] EmailRegisterRequestDto request)
    {
        _logger.LogInformation("Registration attempt for user: {Email}", request.Email);

        var result = await _authService.EmailRegisterAsync(request);

        if (result.User == null || result.User.Id == 0)
        {
            _logger.LogWarning("Registration failed for user: {Email}", request.Email);
            return BadRequest(new { message = "Registration failed. Email may already be in use." });
        }

        // Get user's primary team for response
        var teamId = await _userTeamMappingRepository.GetPrimaryTeamIdAsync(result.User.Id);

        if (result.RequiresEmailVerification)
        {
            _logger.LogInformation("Registration successful for user: {Email}, email verification required", request.Email);
            return Ok(new {
                success = true,
                requiresEmailVerification = true,
                sessionId = result.EmailVerificationSessionId,
                user = result.User,
                teamId = teamId
            });
        }

        _logger.LogInformation("Registration successful for user: {Email}", request.Email);
        return Ok(new { result=true, userId = result.User?.Id, user = result.User, teamId = teamId });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] EmailLoginRequestDto request)
    {
        _logger.LogInformation("Login attempt for user: {Email}", request.Email);

        var result = await _authService.EmailLoginAsync(request);

        if (string.IsNullOrEmpty(result.AccessToken))
        {
            _logger.LogWarning("Login failed for user: {Email}", request.Email);
            return Unauthorized(new { message = "Invalid email or password." });
        }

        // Get user's primary team
        var teamId = await _userTeamMappingRepository.GetPrimaryTeamIdAsync(result.User.Id);

        _logger.LogInformation("Login successful for user: {Email}", request.Email);
        return Ok(new {
            success = true,
            accessToken = result.AccessToken,
            refreshToken = result.RefreshToken,
            requiresSubscription = result.RequiresSubscription,
            isFirstLogin = result.User?.IsFirstLogin ?? false,
            user = result.User,
            teamId = teamId
        });
    }

    [HttpPost("google")]
    public async Task<IActionResult> GoogleSignIn([FromBody] GoogleSignInRequestDto request)
    {
        _logger.LogInformation("Google sign-in attempt");

        var result = await _authService.GoogleSignInAsync(request);

        if (string.IsNullOrEmpty(result.AccessToken))
        {
            _logger.LogWarning("Google sign-in failed");
            return BadRequest(new { message = "Google sign-in failed." });
        }

        // Get user's primary team
        var teamId = await _userTeamMappingRepository.GetPrimaryTeamIdAsync(result.User.Id);

        _logger.LogInformation("Google sign-in successful");
        return Ok(new
        {
            success = true,
            accessToken = result.AccessToken,
            refreshToken = result.RefreshToken,
            user = result.User,
            teamId = teamId
        });
    }

    /// <summary>
    /// Authenticate with Apple Sign-In using identity token
    /// </summary>
    /// <param name="request">Apple sign-in request containing identity token</param>
    /// <returns>Authentication response with tokens and user profile</returns>
    [HttpPost("apple")]
    public async Task<IActionResult> AppleSignIn([FromBody] AppleSignInRequestDto request)
    {
        _logger.LogInformation("Apple Sign-In attempt");
        
        if (string.IsNullOrEmpty(request.IdentityToken))
        {
            _logger.LogWarning("Apple Sign-In failed: Missing identity token");
            return BadRequest(new { 
                success = false, 
                message = "Identity token is required" 
            });
        }
        
        var result = await _appleAuthService.AuthenticateWithAppleAsync(request);

        if (string.IsNullOrEmpty(result.AccessToken))
        {
            _logger.LogWarning("Apple Sign-In failed: {Message}", result.Message);
            return Unauthorized(new {
                success = false,
                message = result.Message ?? "Apple sign-in failed"
            });
        }

        _logger.LogInformation("Apple Sign-In successful for user: {UserId}", result.User?.Id);

        // Get user's primary team
        var teamId = await _userTeamMappingRepository.GetPrimaryTeamIdAsync(result.User.Id);

        // Return consistent response format
        return Ok(new
        {
            success = true,
            accessToken = result.AccessToken,
            refreshToken = result.RefreshToken,
            expiresIn = result.ExpiresIn,
            teamId = teamId,
            user = new
            {
                id = result.User.Id,
                email = result.User.Email,
                name = $"{result.User.FirstName} {result.User.LastName}".Trim(),
                firstName = result.User.FirstName,
                lastName = result.User.LastName,
                isSubscribed = result.User.IsSubscribed
            }
        });
    }

    // [HttpPost("github")]
    // public async Task<IActionResult> GitHubSignIn([FromBody] GitHubSignInRequestDto request)
    // {
    //     _logger.LogInformation("GitHub sign-in attempt");
        
    //     var result = await _authService.GitHubSignInAsync(request);
        
    //     if (string.IsNullOrEmpty(result.AccessToken))
    //     {
    //         _logger.LogWarning("GitHub sign-in failed");
    //         return BadRequest(new { message = "GitHub sign-in failed." });
    //     }
        
    //     _logger.LogInformation("GitHub sign-in successful");
    //     return Ok(result);
    // }

    [HttpPost("mobile-login/send-otp")]
    public async Task<IActionResult> MobileSendOtp([FromBody] SendOtpRequestDto request)
    {
        _logger.LogInformation("OTP request for phone: {PhoneNumber}", request.PhoneNumber);
        
        var sessionId = await _authService.SendOtpAsync(request);
        
        return Ok(new { success= true,mobileSessionID = sessionId });
    }

    [HttpPost("mobile-login/verify-otp")]
    public async Task<IActionResult> MobileVerifyOtp([FromBody] VerifyOtpRequestDto request)
    {
        _logger.LogInformation("OTP verification attempt for phone: {PhoneNumber}", request.PhoneNumber);
        
        var result = await _authService.VerifyOtpAsync(request);
        
        if (string.IsNullOrEmpty(result.AccessToken))
        {
            _logger.LogWarning("OTP verification failed for phone: {PhoneNumber}", request.PhoneNumber);
            return BadRequest(new { message = "Invalid OTP or session." });
        }
        
        _logger.LogInformation("OTP verification successful for phone: {PhoneNumber}", request.PhoneNumber);
        return Ok(new { success = true, result });
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        _logger.LogInformation("Token refresh attempt");
        
        var result = await _authService.RefreshTokenAsync(request);
        
        if (string.IsNullOrEmpty(result.AccessToken))
        {
            _logger.LogWarning("Token refresh failed");
            return BadRequest(new { message = "Invalid or expired refresh token." });
        }
        
        _logger.LogInformation("Token refresh successful");
        return Ok(result);
    }

    [Authorize] // Keep this attribute as this endpoint requires authentication
    [HttpPost("revoke-token")]
    public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequestDto request)
    {
        _logger.LogInformation("Token revocation attempt");
        
        var result = await _authService.RevokeTokenAsync(request.RefreshToken);
        
        if (!result)
        {
            _logger.LogWarning("Token revocation failed");
            return BadRequest(new { message = "Invalid token." });
        }
        
        _logger.LogInformation("Token revocation successful");
        return Ok(new { message = "Token revoked successfully." });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto request)
    {
        _logger.LogInformation("Logout attempt");
        
        var result = await _authService.LogoutAsync(request.RefreshToken);
        
        if (!result)
        {
            _logger.LogWarning("Logout failed - refresh token not found or already deleted");
            // Still return success to avoid information leakage
            return Ok(new { success = true, message = "Logged out successfully." });
        }
        
        _logger.LogInformation("Logout successful - refresh token deleted from database");
        return Ok(new { success = true, message = "Logged out successfully." });
    }

    [HttpGet("google/callback")]
public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string state, [FromQuery] string? error)
{
    _logger.LogInformation("Google OAuth callback received");

    if (!string.IsNullOrEmpty(error))
    {
        _logger.LogWarning("Google OAuth error: {Error}", error);
        return Redirect($"/auth/error?error={error}");
    }

    if (string.IsNullOrEmpty(code))
    {
        _logger.LogWarning("Google OAuth callback missing authorization code");
        return Redirect("/auth/error?error=missing_code");
    }

    try
    {
        // Redirect to frontend with the authorization code
        // Frontend will exchange code for ID token and call /api/auth/google
        var redirectUrl = $"/auth/success?provider=google&code={code}&state={state}";
        _logger.LogInformation("Redirecting to frontend for Google OAuth completion");
        
        return Redirect(redirectUrl);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing Google OAuth callback");
        return Redirect("/auth/error?error=processing_error");
    }
}

[HttpGet("apple/callback")]
public async Task<IActionResult> AppleCallback([FromQuery] string code, [FromQuery] string state, [FromQuery] string? error)
{
    _logger.LogInformation("Apple OAuth callback received");

    if (!string.IsNullOrEmpty(error))
    {
        _logger.LogWarning("Apple OAuth error: {Error}", error);
        return Redirect($"/auth/error?error={error}");
    }

    if (string.IsNullOrEmpty(code))
    {
        _logger.LogWarning("Apple OAuth callback missing authorization code");
        return Redirect("/auth/error?error=missing_code");
    }

    try
    {
        // Redirect to frontend with the authorization code
        // Frontend will exchange code for ID token and call /api/auth/apple
        var redirectUrl = $"/auth/success?provider=apple&code={code}&state={state}";
        _logger.LogInformation("Redirecting to frontend for Apple OAuth completion");
        
        return Redirect(redirectUrl);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing Apple OAuth callback");
        return Redirect("/auth/error?error=processing_error");
    }
}

[HttpGet("oauth/initiate")]
public IActionResult InitiateOAuth([FromQuery] string provider, [FromQuery] string? returnUrl)
{
    _logger.LogInformation("OAuth initiation requested for provider: {Provider}", provider);

    var state = Guid.NewGuid().ToString();
    var baseUrl = $"{Request.Scheme}://{Request.Host}";

    switch (provider.ToLower())
    {
        case "google":
            var googleClientId = _configuration["Google:ClientId"];
            var googleRedirectUri = $"{baseUrl}/api/auth/google/callback";
            var googleAuthUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                $"client_id={googleClientId}&" +
                $"redirect_uri={Uri.EscapeDataString(googleRedirectUri)}&" +
                $"response_type=code&" +
                $"scope=openid email profile&" +
                $"state={state}";
            
            _logger.LogInformation("Redirecting to Google OAuth: {Url}", googleAuthUrl);
            return Redirect(googleAuthUrl);

        case "apple":
            var appleClientId = _configuration["Apple:ClientId"];
            var appleRedirectUri = $"{baseUrl}/api/auth/apple/callback";
            var appleAuthUrl = $"https://appleid.apple.com/auth/authorize?" +
                $"client_id={appleClientId}&" +
                $"redirect_uri={Uri.EscapeDataString(appleRedirectUri)}&" +
                $"response_type=code&" +
                $"scope=name email&" +
                $"response_mode=form_post&" +
                $"state={state}";
            
            _logger.LogInformation("Redirecting to Apple OAuth: {Url}", appleAuthUrl);
            return Redirect(appleAuthUrl);

        default:
            _logger.LogWarning("Unsupported OAuth provider: {Provider}", provider);
            return BadRequest(new { message = "Unsupported provider" });
    }
}

[HttpPost("email-login/send-otp")]
public async Task<IActionResult> SendEmailLoginOtp([FromBody] SendEmailVerificationRequestDto request)
{
    _logger.LogInformation("Email login OTP requested for: {Email}", request.Email);
    
    try
    {
        var sessionId = await _emailVerificationService.SendEmailLoginOtpAsync(request.Email);
        
        _logger.LogInformation("Email login OTP sent successfully to: {Email}", request.Email);
        return Ok(new { success = true, sessionId, message = "OTP sent to your email." });
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogWarning("Email login OTP request failed: {Message}", ex.Message);
        return BadRequest(new { success = false, message = ex.Message });
    }
}

[HttpPost("email-login/resend")]
public async Task<IActionResult> ResendEmailOtp([FromBody] ResendEmailOtpRequestDto request)
{
    _logger.LogInformation("Email OTP resend requested for: {Email}", request.Email);
    
    try
    {
        var sessionId = await _emailVerificationService.ResendEmailOtpAsync(request);
        
        _logger.LogInformation("Email OTP resent successfully to: {Email}", request.Email);
        return Ok(new { success = true, sessionId, message = "OTP resent to your email." });
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogWarning("Email OTP resend failed: {Message}", ex.Message);
        return BadRequest(new { success = false, message = ex.Message });
    }
}

[HttpPost("email-login/verify")]
public async Task<IActionResult> VerifyEmailLoginOtp([FromBody] EmailOtpLoginRequestDto request, [FromQuery] bool isRegistration = false)
{
    _logger.LogInformation("{Type} OTP verification for: {Email}", isRegistration ? "Registration" : "Email login", request.Email);
    
    AuthResponseDto result;
    if (isRegistration)
    {
        result = await _emailVerificationService.VerifyRegistrationOtpAsync(request);
    }
    else
    {
        result = await _emailVerificationService.VerifyEmailLoginOtpAsync(request);
    }
    
    if (string.IsNullOrEmpty(result.AccessToken))
    {
        _logger.LogWarning("{Type} OTP verification failed for: {Email}", isRegistration ? "Registration" : "Email login", request.Email);
        return BadRequest(new { success = false, message = "Invalid OTP or session." });
    }
    
    _logger.LogInformation("{Type} OTP verification successful for: {Email}", isRegistration ? "Registration" : "Email login", request.Email);
    return Ok(new { success = true, result });
}

[HttpPost("request-password-reset")]
public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
{
    _logger.LogInformation("Password reset requested for: {Email}", request.Email);
    
    try
    {
        var result = await _emailVerificationService.ForgotPasswordAsync(request);
        
        // Always return success to prevent user enumeration attacks
        _logger.LogInformation("Password reset request processed for: {Email}", request.Email);
        return Ok(new { message = "If your email is registered, you will receive a password reset link." });
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogWarning("Password reset request failed: {Message}", ex.Message);
        return BadRequest(new { message = ex.Message });
    }
}

[HttpPost("reset-password")]
public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
{
    _logger.LogInformation("Password reset attempt");
    
    var result = await _emailVerificationService.ResetPasswordAsync(request);
    
    if (!result)
    {
        _logger.LogWarning("Password reset failed: Invalid or expired token");
        return BadRequest(new { message = "Invalid or expired reset token." });
    }
    
    _logger.LogInformation("Password reset successful");
    return Ok(new { message = "Password reset successfully." });
}
}
