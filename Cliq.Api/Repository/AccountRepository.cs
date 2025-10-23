using System.Text.Json;
using Cliq.Api.Interface;
using Cliq.Api.Services;
using FluentResults;
using Models.Account;
using RestSharp;

namespace Cliq.Api.Repository
{
    public class AccountRepository : IAccountInterface
    {
        private readonly IConfiguration _configuration;
        string _baseUrl;
        string _botName;
        private readonly CliqAuthService _authService;

        public AccountRepository(CliqAuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
            _botName = _configuration["Message:BotIdentity"];
            _baseUrl = _configuration["ApiCall:BaseUrl"];
        }


        public async Task<Result<List<User>>> GetUsersAsync()
        {
            try
            {
                var accessTokenResult = await _authService.GetAccessTokenAsync();
                if (accessTokenResult.IsFailed)
                    return Result.Fail<List<User>>(accessTokenResult.Errors[0].Message ?? "Error in getting access/refresh token");

                var accessToken = accessTokenResult.Value;
                var allUsers = new List<User>();
                string nextToken = null;

                do
                {
                    var client = new RestClient(_baseUrl + "users");
                    var request = new RestRequest("", Method.Get);
                    request.AddHeader("Authorization", $"Zoho-oauthtoken {accessToken}");

                    if (!string.IsNullOrEmpty(nextToken))
                        request.AddQueryParameter("next_token", nextToken);

                    var response = await client.ExecuteAsync(request);

                    if (!response.IsSuccessful)
                        return Result.Fail<List<User>>($"Failed to fetch users: {response.Content}");

                    var usersResponse = JsonSerializer.Deserialize<UsersResponse>(response.Content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (usersResponse?.data != null)
                        allUsers.AddRange(usersResponse.data);

                    nextToken = usersResponse?.has_more == true ? usersResponse.next_token : null;

                } while (!string.IsNullOrEmpty(nextToken));

                return Result.Ok(allUsers);
            }
            catch (Exception ex)
            {
                return Result.Fail<List<User>>(ex.Message);
            }
        }

    }
}