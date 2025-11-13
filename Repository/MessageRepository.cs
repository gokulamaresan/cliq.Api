using System.Text.Json;
using cliq.Api.Models.Messages;
using Cliq.Api.Interface;
using Cliq.Api.Services;
using FluentResults;
using RestSharp;

namespace Cliq.Api.Repository
{
    public class MessageRepository : IMessageInterface
    {
        private readonly IConfiguration _configuration;
        string _botName;
        string _baseUrl;
        private readonly CliqAuthService _authService;
        private readonly string _UsersFilePath;

        public MessageRepository(CliqAuthService authService, IConfiguration configuration)
        {
            _configuration = configuration;
            _authService = authService;
            _configuration = configuration;
            _botName = _configuration["Message:BotIdentity"];
            _baseUrl = _configuration["ApiCall:BaseUrl"];
            _UsersFilePath = _configuration["Datas:UserData"];
        }

        //get users
        public async Task<Result<List<UsersList>>> GetUsersAsync()
        {
            try
            {
                var result = await File.ReadAllTextAsync(_UsersFilePath);

                var usersList = JsonSerializer.Deserialize<List<UsersList>>(result);

                if (usersList == null)
                    return Result.Fail<List<UsersList>>("Failed to deserialize users");

                return Result.Ok(usersList);
            }
            catch (Exception ex)
            {
                return Result.Fail<List<UsersList>>(ex.Message);
            }
        }

        public async Task<Result<bool>> SendMessageAsync(SendMessageRequest request)
        {
            try
            {
                var accessTokenResult = await _authService.GetAccessTokenAsync();
                if (accessTokenResult.IsFailed)
                    return Result.Fail<bool>(accessTokenResult.Errors[0].Message ?? "Error in getting access/refresh token");
                var accessToken = accessTokenResult.Value;


                var client = new RestClient($"{_baseUrl}bots/{_botName}/message");
                var restRequest = new RestRequest("", Method.Post);
                restRequest.AddHeader("Authorization", $"Zoho-oauthtoken {accessToken}");
                restRequest.AddHeader("Content-Type", "application/json");

                var body = new
                {
                    text = request.Message,
                    userids = request.Zuid,
                    sync_message = true
                };

                restRequest.AddJsonBody(body);

                var response = await client.ExecuteAsync(restRequest);

                if (!response.IsSuccessful)
                    return Result.Fail<bool>($"Failed to send message: {response.Content}");

                return Result.Ok(true);
            }
            catch (Exception ex)
            {
                return Result.Fail<bool>(ex.Message);
            }
        }


        public async Task<Result<string>> SendFileToUserByZuidAsync(IFormFile file, string zuid, string comments)
        {
            if (file == null || file.Length == 0)
                return Result.Fail<string>("File is null or empty.");

            try
            {
                var tokenResult = await _authService.GetAccessTokenAsync();
                if (tokenResult.IsFailed)
                    return Result.Fail<string>(tokenResult.Errors[0].Message ?? "Error getting access token");

                var accessToken = tokenResult.Value;

                var client = new RestClient($"{_baseUrl}buddies/{zuid}/files");
                var request = new RestRequest("", Method.Post);
                request.AddHeader("Authorization", $"Zoho-oauthtoken {accessToken}");
                request.AlwaysMultipartFormData = true;

                // Add the file
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    var fileBytes = ms.ToArray();
                    request.AddFile("files", fileBytes, file.FileName, file.ContentType);
                }


                if (!string.IsNullOrEmpty(comments))
                {
                    // Add comments as proper JSON array in multipart
                    var commentsArray = new string[] { comments };
                    var commentsJson = JsonSerializer.Serialize(commentsArray);
                    request.AddParameter("comments", commentsJson); // Works as multipart field
                }

                // Execute request
                var response = await client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                    return Result.Fail<string>($"File send failed: {response.StatusCode} - {response.Content}");

                return Result.Ok("File successfully sent to user via ZUID!");
            }
            catch (Exception ex)
            {
                return Result.Fail<string>($"Exception during file send: {ex.Message}");
            }
        }


        public async Task<Result<string>> SendFileToUserByZuid(
            byte[] fileBytes,
            string fileName,
            string contentType,
            string zuid,
            string comments)
        {
            if (fileBytes == null || fileBytes.Length == 0)
                return Result.Fail<string>("File byte array is null or empty.");

            try
            {
                var tokenResult = await _authService.GetAccessTokenAsync();
                if (tokenResult.IsFailed)
                    return Result.Fail<string>(tokenResult.Errors[0].Message ?? "Error getting access token");

                var accessToken = tokenResult.Value;

                var client = new RestClient($"{_baseUrl}buddies/{zuid}/files");
                var request = new RestRequest("", Method.Post);
                request.AddHeader("Authorization", $"Zoho-oauthtoken {accessToken}");
                request.AlwaysMultipartFormData = true;

                // Add the byte array as file
                request.AddFile("files", fileBytes, fileName, contentType);

                // Add comments if provided
                if (!string.IsNullOrEmpty(comments))
                {
                    var commentsArray = new string[] { comments };
                    var commentsJson = JsonSerializer.Serialize(commentsArray);
                    request.AddParameter("comments", commentsJson); // multipart field
                }

                // Execute request
                var response = await client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                    return Result.Fail<string>($"File send failed: {response.StatusCode} - {response.Content}");

                return Result.Ok("File successfully sent to user via ZUID!");
            }
            catch (Exception ex)
            {
                return Result.Fail<string>($"Exception during file send: {ex.Message}");
            }
        }



        public async Task<Result<string>> SendTextMessageToUserByZuidAsync(string message, string zuid)
        {
            if (string.IsNullOrEmpty(message))
                return Result.Fail<string>("Message is null or empty.");

            if (string.IsNullOrEmpty(zuid))
                return Result.Fail<string>("ZUID is null or empty.");

            try
            {
                var tokenResult = await _authService.GetAccessTokenAsync();
                if (tokenResult.IsFailed)
                    return Result.Fail<string>(tokenResult.Errors[0].Message ?? "Error getting access token");

                var accessToken = tokenResult.Value;

                var client = new RestClient($"{_baseUrl}buddies/{zuid}/message");
                var request = new RestRequest("", Method.Post);
                request.AddHeader("Authorization", $"Zoho-oauthtoken {accessToken}");
                request.AddHeader("Content-Type", "application/json");

                // Add the message payload
                var payload = new { text = message };
                request.AddJsonBody(payload);

                // Execute request
                var response = await client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                    return Result.Fail<string>($"Message send failed: {response.StatusCode} - {response.Content}");

                return Result.Ok("✅ Text message successfully sent to user via ZUID!");
            }
            catch (Exception ex)
            {
                return Result.Fail<string>($"Exception during message send: {ex.Message}");
            }
        }

        public async Task<Result<string>> SendBotVoiceCallAsync(string message, List<string> userIds)
        {
            try
            {
                var accessTokenResult = await _authService.GetAccessTokenAsync();
                if (accessTokenResult.IsFailed)
                    return Result.Fail<string>("Failed to get access token");

                var accessToken = accessTokenResult.Value;

                var client = new RestClient($"{_baseUrl}bots/{_botName}/calls");
                var request = new RestRequest("", Method.Post);
                request.AddHeader("Authorization", $"Zoho-oauthtoken {accessToken}");
                request.AddHeader("Content-Type", "application/json");

                var body = new
                {
                    text = message,
                    user_ids = userIds,
                    retry = 1,
                    loop = 1
                };

                request.AddJsonBody(body);

                var response = await client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                    return Result.Fail<string>($"Failed to send bot call: {response.Content}");

                return Result.Ok("Voice call with confirmation option triggered successfully.");
            }
            catch (Exception ex)
            {
                return Result.Fail<string>(ex.Message);
            }
        }






        public async Task<Result<string>> SisalSendFileToUserByZuid(
            byte[] fileBytes,
            string fileName,
            string contentType,
            string zuid,
            string comments)
        {
            if (fileBytes == null || fileBytes.Length == 0)
                return Result.Fail<string>("File byte array is null or empty.");

            try
            {
                var tokenResult = await _authService.GetAccessTokenAsync();
                if (tokenResult.IsFailed)
                    return Result.Fail<string>(tokenResult.Errors[0].Message ?? "Error getting access token");

                var accessToken = tokenResult.Value;

                var client = new RestClient($"{_baseUrl}buddies/{zuid}/files");
                var request = new RestRequest("", Method.Post);
                request.AddHeader("Authorization", $"Zoho-oauthtoken {accessToken}");
                request.AlwaysMultipartFormData = true;

                request.AddFile("files", fileBytes, fileName, contentType);

                if (!string.IsNullOrEmpty(comments))
                {
                    var commentsArray = new string[] { comments };
                    var commentsJson = JsonSerializer.Serialize(commentsArray);
                    request.AddParameter("comments", commentsJson);
                }

                var response = await client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                    return Result.Fail<string>($"File send failed: {response.StatusCode} - {response.Content}");

                return Result.Ok("File successfully sent to user via ZUID!");
            }
            catch (Exception ex)
            {
                return Result.Fail<string>($"Exception during file send: {ex.Message}");
            }
        }

    }
}