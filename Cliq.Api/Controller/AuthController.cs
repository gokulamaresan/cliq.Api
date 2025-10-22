using Microsoft.AspNetCore.Mvc;
using VaultCliqMessageService.Services;
using System.Threading.Tasks;

namespace VaultCliqMessageService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly CliqAuthService _authService;

        public AuthController(CliqAuthService authService)
        {
            _authService = authService;
        }

        // STEP 1: Redirect user to Zoho for login/consent
        [HttpGet("authorize")]
        public IActionResult AuthorizeUser()
        {
            string clientId = "1000.ZMRJYEATACB36XZ7CVN40R69IVC30A";
            string redirectUri = "https://localhost:5228/api/auth/callback";
            string scope = "ZohoCliq.Users.READ,ZohoCliq.Webhooks.CREATE";

            string authUrl =
                $"https://accounts.zoho.in/oauth/v2/auth?scope={scope}&client_id={clientId}&response_type=code&access_type=offline&redirect_uri={redirectUri}";

            // ✅ Redirect to Zoho Login page
            return Ok(authUrl);
        }

        // STEP 2: Zoho redirects here with the ?code=
        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string code)
        {
            if (string.IsNullOrEmpty(code))
                return BadRequest("Missing authorization code");

            bool success = await _authService.ExchangeCodeForTokensAsync(code);
            if (!success)
                return BadRequest("Failed to exchange code for tokens.");

            return Ok(new
            {
                message = "Authentication successful!",
                accessToken = CliqAuthService.AccessToken,
                refreshToken = CliqAuthService.RefreshToken
            });
        }

        // STEP 3: Refresh token manually (optional)
        [HttpGet("refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            var newToken = await _authService.RefreshAccessTokenAsync();
            return Ok(new { accessToken = newToken });
        }
    }
}
