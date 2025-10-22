using RestSharp;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace VaultCliqMessageService.Services
{
    public class CliqAuthService
    {
        private readonly string _clientId = "1000.ZMRJYEATACB36XZ7CVN40R69IVC30A";
        private readonly string _clientSecret = "46efd64c1375e34c8a85e6eda298933a824a8a8475";
        private readonly string _redirectUri = "https://localhost:5228/api/auth/callback";

        public static string AccessToken { get; private set; }
        public static string RefreshToken { get; private set; }

        private readonly RestClient _client;

        public CliqAuthService()
        {
            _client = new RestClient("https://accounts.zoho.in"); // ✅ India region
        }

        public async Task<bool> ExchangeCodeForTokensAsync(string code)
        {
            var request = new RestRequest("/oauth/v2/token", Method.Post);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("grant_type", "authorization_code");
            request.AddParameter("client_id", _clientId);
            request.AddParameter("client_secret", _clientSecret);
            request.AddParameter("redirect_uri", _redirectUri);
            request.AddParameter("code", code);

            var response = await _client.ExecuteAsync(request);
            if (!response.IsSuccessful) return false;

            var json = JObject.Parse(response.Content ?? "{}");
            AccessToken = json["access_token"]?.ToString();
            RefreshToken = json["refresh_token"]?.ToString();

            return !string.IsNullOrEmpty(AccessToken);
        }

        public async Task<string> RefreshAccessTokenAsync()
        {
            if (string.IsNullOrEmpty(RefreshToken))
                throw new InvalidOperationException("No refresh token found. Authenticate first.");

            var request = new RestRequest("/oauth/v2/token", Method.Post);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("grant_type", "refresh_token");
            request.AddParameter("client_id", _clientId);
            request.AddParameter("client_secret", _clientSecret);
            request.AddParameter("refresh_token", RefreshToken);

            var response = await _client.ExecuteAsync(request);
            if (!response.IsSuccessful)
                throw new Exception("Failed to refresh access token.");

            var json = JObject.Parse(response.Content ?? "{}");
            AccessToken = json["access_token"]?.ToString();

            return AccessToken;
        }
    }
}

