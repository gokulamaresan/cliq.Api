using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using cliq.Api.Interface;
using cliq.Api.Models.Channels;
using Cliq.Api.Services;
using FluentResults;
using RestSharp;

namespace cliq.Api.Repository
{
    public class ChannelRepository : IChannelInterface
    {
        private readonly IConfiguration _configuration;
        private readonly string _channelFilePath;
        private readonly CliqAuthService _authService;
        private readonly string _baseUrl;

        public ChannelRepository(IConfiguration configuration, CliqAuthService authService)
        {
            _authService = authService;
            _configuration = configuration;
            _channelFilePath = _configuration["Datas:ChannelsData"];
            _baseUrl = _configuration["ApiCall:BaseUrl"];
        }

        // get chel details from the json file
        public async Task<Result<List<ChannelDetailsDto>>> GetChannelDetails()
        {
            try
            {
                var result = await File.ReadAllTextAsync(_channelFilePath);
                var channelDetails = JsonSerializer.Deserialize<List<ChannelDetailsDto>>(result);

                if (channelDetails == null)
                    return Result.Fail<List<ChannelDetailsDto>>("Failed to deserialize channel details");

                return Result.Ok(channelDetails);
            }
            catch (Exception ex)
            {
                return Result.Fail<List<ChannelDetailsDto>>(ex.Message);
            }
        }
        // post message in channel and save it to json file
        public async Task<Result<string>> PostMessageInChannel(string message, string channelsbyname)
        {
            try
            {
                var accessTokenResult = await _authService.GetAccessTokenAsync();
                if (accessTokenResult.IsFailed)
                    return Result.Fail<string>(accessTokenResult.Errors[0].Message ?? "Error in getting access/refresh token");

                var accessToken = accessTokenResult.Value;

                // Post the message to the channel using the access token
                var client = new RestClient($"{_baseUrl}channelsbyname/{channelsbyname}/message");
                var request = new RestRequest("", Method.Post);
                request.AddHeader("Authorization", $"Zoho-oauthtoken {accessToken}");
                request.AddHeader("Content-Type", "application/json");

                var body = new
                {
                    text = message
                };

                request.AddJsonBody(body);

                var response = await client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                    return Result.Fail<string>($"Failed to post message: {response.Content}");

                return Result.Ok("Message posted successfully");
            }
            catch (Exception ex)
            {
                return Result.Fail<string>(ex.Message);
            }
        }


        public async Task<Result<string>> UploadFileToChannelAsync(string channelId, IFormFile file , string? comments)
        {
            try
            {
                // 1️⃣ Get access token
                var accessTokenResult = await _authService.GetAccessTokenAsync();
                if (accessTokenResult.IsFailed)
                    return Result.Fail<string>(accessTokenResult.Errors[0].Message ?? "Failed to get access token");

                var accessToken = accessTokenResult.Value;

                // 2️⃣ Create REST client
                var client = new RestClient($"{_baseUrl}channelsbyname/{channelId}/files");
                var request = new RestRequest("", Method.Post);
                request.AddHeader("Authorization", $"Zoho-oauthtoken {accessToken}");

                // 3️⃣ Convert IFormFile to stream and attach it to request
                using (var stream = file.OpenReadStream())
                {

                    request.AddFile("file", ReadFully(stream), file.FileName, file.ContentType);
                }
                request.AlwaysMultipartFormData = true;
                // ✅ Send comments as JSON array
                var commentsJson = JsonSerializer.Serialize(new[] { comments });
                request.AddParameter("comments", commentsJson);


                // 4️⃣ Execute request
                var response = await client.ExecuteAsync(request);

                // 5️⃣ Validate response
                if (!response.IsSuccessful)
                    return Result.Fail<string>($"Failed to upload file: {response.Content}");

                return Result.Ok("File uploaded successfully.");
            }
            catch (Exception ex)
            {
                return Result.Fail<string>(ex.Message);
            }
        }

        private static byte[] ReadFully(Stream input)
        {
            using (var ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }

    }
}