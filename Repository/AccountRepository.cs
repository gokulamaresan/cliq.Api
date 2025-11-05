using System.Collections.Generic;
using System.Text.Json;
using Cliq.Api.Interface;
using Cliq.Api.Services;
using FluentResults;
using Models.Account;
using Models.ChannelDto;
using RestSharp;
using Channel = Models.ChannelDto.Channel;

namespace Cliq.Api.Repository
{
    public class AccountRepository : IAccountInterface
    {
        private readonly IConfiguration _configuration;
        private readonly CliqAuthService _authService;
        private readonly string _baseUrl;
        private readonly string _botName;

        // ✅ Paths to your JSON files (Datas folder)
        private readonly string _userFilePath ;
        private readonly string _channelFilePath;
        public AccountRepository(CliqAuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
            _botName = _configuration["Message:BotIdentity"];
            _baseUrl = _configuration["ApiCall:BaseUrl"];
            _userFilePath = _configuration["Datas:UserData"];
            _channelFilePath = _configuration["Datas:ChannelsData"];
        }

        // ✅ Fetch all users and store in Datas/userdetails.json
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

                // ✅ Store user list in Datas/userdetails.json
                var json = JsonSerializer.Serialize(allUsers, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_userFilePath, json);

                return Result.Ok(allUsers);
            }
            catch (Exception ex)
            {
                return Result.Fail<List<User>>(ex.Message);
            }
        }

        // ✅ Fetch all channels and store in Datas/channeldetails.json
        public async Task<Result<List<Channel>>> GetAllChannelsAsync()
        {
            try
            {
                var accessTokenResult = await _authService.GetAccessTokenAsync();
                if (accessTokenResult.IsFailed)
                    return Result.Fail<List<Channel>>(accessTokenResult.Errors[0].Message ?? "Error in getting access/refresh token");

                var accessToken = accessTokenResult.Value;
                var allChannels = new List<Channel>();

                var client = new RestClient(_baseUrl + "channels");
                var request = new RestRequest("", Method.Get);
                request.AddHeader("Authorization", $"Zoho-oauthtoken {accessToken}");

                var response = await client.ExecuteAsync(request);
                if (!response.IsSuccessful)
                    return Result.Fail<List<Channel>>($"Failed to fetch channels: {response.Content}");

                var channelsResponse = JsonSerializer.Deserialize<ChannelsResponse>(response.Content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (channelsResponse?.channels != null)
                    allChannels.AddRange(channelsResponse.channels);

                // ✅ Store channel list in Datas/channeldetails.json
                var json = JsonSerializer.Serialize(allChannels, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_channelFilePath, json);

                return Result.Ok(allChannels);
            }
            catch (Exception ex)
            {
                return Result.Fail<List<Channel>>(ex.Message);
            }
        }
    }
}
