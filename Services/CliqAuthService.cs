using FluentResults;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Threading.Tasks;

namespace Cliq.Api.Services
{
    public class CliqAuthService
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _refreshToken;
        private readonly string _redirectUri;
        private readonly RestClient _client;

        private string _accessToken;
        private DateTime _expiryTime;

        public static string AccessToken { get; private set; }
        public static string RefreshToken { get; private set; }
        public CliqAuthService(IConfiguration config)
        {
            _clientId = config["ZohoCliq:ClientId"];
            _clientSecret = config["ZohoCliq:ClientSecret"];
            _refreshToken = config["ZohoCliq:RefreshToken"];
            _redirectUri = config["ZohoCliq:RedirectUri"];
            _client = new RestClient("https://accounts.zoho.in");
        }


        public async Task<Result<string>> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken) && _expiryTime > DateTime.UtcNow.AddMinutes(1))
                return Result.Ok(_accessToken); // return cached token

            return await RefreshAccessTokenAsync();
        }


        public async Task<Result<string>> RefreshAccessTokenAsync()
        {
            if (string.IsNullOrEmpty(_refreshToken))
                return Result.Fail<string>("Refresh token not configured.");

            try
            {
                var request = new RestRequest("/oauth/v2/token", Method.Post);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddParameter("grant_type", "refresh_token");
                request.AddParameter("client_id", _clientId);
                request.AddParameter("client_secret", _clientSecret);
                request.AddParameter("refresh_token", _refreshToken);

                var response = await _client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                    return Result.Fail<string>($"Zoho OAuth failed: {response.Content}");

                var json = JObject.Parse(response.Content ?? "{}");
                _accessToken = json["access_token"]?.ToString();
                int expiresIn = json["expires_in"]?.ToObject<int>() ?? 3600; // Zoho token expiry
                _expiryTime = DateTime.UtcNow.AddSeconds(expiresIn);

                if (string.IsNullOrEmpty(_accessToken))
                    return Result.Fail<string>("Zoho OAuth returned empty access token");

                return Result.Ok(_accessToken);
            }
            catch (Exception ex)
            {
                return Result.Fail<string>(ex.Message);
            }
        }


        public async Task<Result<(string AccessToken, string RefreshToken)>> ExchangeCodeForTokensAsync(string code)
        {
            if (string.IsNullOrEmpty(code))
                return Result.Fail<(string, string)>("Authorization code is missing.");

            try
            {
                var request = new RestRequest("/oauth/v2/token", Method.Post);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddParameter("grant_type", "authorization_code");
                request.AddParameter("client_id", _clientId);
                request.AddParameter("client_secret", _clientSecret);
                request.AddParameter("redirect_uri", _redirectUri);
                request.AddParameter("code", code);

                var response = await _client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                    return Result.Fail<(string, string)>($"Zoho OAuth request failed: {response.StatusCode} - {response.Content}");

                var json = JObject.Parse(response.Content ?? "{}");
                string accessToken = json["access_token"]?.ToString();
                string refreshToken = json["refresh_token"]?.ToString();

                if (string.IsNullOrEmpty(accessToken))
                    return Result.Fail<(string, string)>("Zoho OAuth returned empty access token.");
                if (string.IsNullOrEmpty(refreshToken))
                    return Result.Fail<(string, string)>("Zoho OAuth returned empty refresh token.");

                AccessToken = accessToken;
                RefreshToken = refreshToken;

                return Result.Ok((accessToken, refreshToken));
            }
            catch (Exception ex)
            {
                return Result.Fail<(string, string)>($"Exception occurred: {ex.Message}");
            }
        }

    }
}
