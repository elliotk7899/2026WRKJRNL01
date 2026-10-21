using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using WRKJRNL01.Data.Repository;
using WRKJRNL01.Data.Services;

namespace WRKJRNL01.UI.Controllers;

[Route("GoogleAuth")]
public class GoogleAuthController : Controller
{
    private readonly GoogleConnectRepo googleConnectRepo;
    private readonly TokenEncryptionService tokenEncryptionService;

    public GoogleAuthController(
        GoogleConnectRepo googleConnectRepo,
        TokenEncryptionService tokenEncryptionService)
    {
        this.googleConnectRepo = googleConnectRepo;
        this.tokenEncryptionService = tokenEncryptionService;
    }

    // OFFLINE: Google OAuth is disabled for local/off-network use. The endpoints remain
    // so links/controls don't 404, but they no longer challenge Google.
    private const bool GoogleDisabledLocally = true;

    [HttpGet("Connect")]
    public IActionResult Connect()
    {
        if (GoogleDisabledLocally)
        {
            return Content(
                "Google Calendar integration is disabled in the local build.",
                "text/plain");
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = $"{Request.PathBase}/GoogleAuth/Callback"
        };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("Callback")]
    public async Task<IActionResult> Callback()
    {
        if (GoogleDisabledLocally)
        {
            return Redirect($"{Request.PathBase}/");
        }

        var result = await HttpContext.AuthenticateAsync("GoogleCalendarCookie");

        if (!result.Succeeded)
        {
            return BadRequest("Google authentication was not successful.");
        }

        var email = User.Claims
            .FirstOrDefault(c => c.Type == "mail")
            ?.Value;

        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("Unable to determine the user's email address.");
        }

        var refreshToken = result.Properties?.GetTokenValue("refresh_token");

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return BadRequest("Google did not return a refresh token.");
        }

        try
        {
            var encryptedRefreshToken = tokenEncryptionService.Encrypt(refreshToken);

            await googleConnectRepo.SaveConnectionAsync(email, encryptedRefreshToken);

            return Redirect($"{Request.PathBase}/");
        }
        catch (Exception ex)
        {
            // Replace with your logging framework
            Console.WriteLine(ex);

            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while saving the Google connection: {ex}");
        }
    }
}