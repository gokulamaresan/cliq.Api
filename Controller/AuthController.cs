using Cliq.Api.AdminApiAttribute;
using Cliq.Api.Interface;
using Cliq.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Cliq.Api.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [AdminApiKey]
    public class AuthController : ControllerBase
    {
        private readonly IAuthtInterface _IAuthtInterface;
        private readonly string? _clientId;
        private readonly string? _redirectUri;
        private readonly string? _scope;

        public AuthController(CliqAuthService authService, IConfiguration config , IAuthtInterface IAuthtInterface)
        {
            _IAuthtInterface = IAuthtInterface;
            _clientId = config["ZohoCliq:ClientId"];
            _redirectUri = config["ZohoCliq:RedirectUri"];
            _scope = config["ZohoCliq:Scope"];
        }


        // Step 1: Get Zoho authorization URL
        [HttpGet("authorize")]
        public IActionResult AuthorizeUser()
        {
            string authUrl =
                $"https://accounts.zoho.in/oauth/v2/auth?scope={_scope}&client_id={_clientId}&response_type=code&access_type=offline&redirect_uri={_redirectUri}";

            return Ok(new { authUrl });
        }


        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string code)
        {
            if (string.IsNullOrEmpty(code))
                return BadRequest("Missing authorization code");

            var result = await _IAuthtInterface.ExchangeCodeForTokensAsync(code);
            if (result.IsFailed)
                return BadRequest(new { errors = result.Errors });

            return Ok(new
            {
                message = "Authentication successful!",
                accessToken = result.Value.AccessToken,
                refreshToken = result.Value.RefreshToken
            });
        }


        // Step 3: Refresh access token manually
        // Use this anytime the access token expires
        [HttpGet("refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            var result = await _IAuthtInterface.RefreshAccessTokenAsync();
            if (result.IsFailed)
                return BadRequest(new { errors = result.Errors });

            return Ok(new { accessToken = result.Value });
        }

        // Step 4: Get current access token (automatically refresh if expired)
        [HttpGet("token")]
        public async Task<IActionResult> GetAccessToken()
        {
            var result = await _IAuthtInterface.GetAccessTokenAsync();
            if (result.IsFailed)
                return BadRequest(new { errors = result.Errors });

            return Ok(new { accessToken = result.Value });
        }
    }
}
