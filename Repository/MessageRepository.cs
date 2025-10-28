using System.Net.Http.Headers;
using System.Text.Json;
using Cliq.Api.Interface;
using Cliq.Api.Services;
using FluentResults;
using Models.Account;
using RestSharp;

namespace Cliq.Api.Repository
{
    public class MessageRepository : IMessageInterface
    {
        private readonly IConfiguration _configuration;
        string _botName;
        string _baseUrl;
        private readonly CliqAuthService _authService;

        public MessageRepository(CliqAuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
            _botName = _configuration["Message:BotIdentity"];
            _baseUrl = _configuration["ApiCall:BaseUrl"];
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




        private bool ValidateFile(IFormFile file, bool isImage = false)
        {
            if (file == null) return false;

            // Max file size 10 MB (adjust as needed)
            const long maxFileSize = 10 * 1024 * 1024;
            if (file.Length > maxFileSize) return false;

            if (isImage)
            {
                // Allowed image types
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/bmp" };
                if (!allowedTypes.Contains(file.ContentType)) return false;
            }
            else
            {
                // For general files, allow these types (you can expand)
                var allowedTypes = new[] {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "text/plain",
            "application/zip",
            "application/octet-stream" // generic
        };
                if (!allowedTypes.Contains(file.ContentType)) return false;
            }

            return true;
        }

        private bool IsTransientError(RestResponse response)
        {
            // Retry only for server errors or network issues
            if (response == null) return true; // network exception
            if ((int)response.StatusCode >= 500) return true; // 5xx errors
            if (response.ResponseStatus == ResponseStatus.TimedOut ||
                response.ResponseStatus == ResponseStatus.Error ||
                response.ResponseStatus == ResponseStatus.Aborted) return true;

            return false;
        }

        public async Task<Result<bool>> SendImageAsync(string zuid, IFormFile file)
        {
            if (!ValidateFile(file, isImage: true))
                return Result.Fail<bool>("Invalid image file. Only JPEG, PNG, GIF, BMP allowed, max 10 MB.");

            try
            {
                var tokenResult = await _authService.GetAccessTokenAsync();
                if (tokenResult.IsFailed)
                    return Result.Fail<bool>(tokenResult.Errors[0].Message ?? "Error getting access token");

                var accessToken = tokenResult.Value;
                var client = new RestClient($"{_baseUrl}bots/{_botName}/message");

                for (int attempt = 1; attempt <= 2; attempt++)
                {
                    var request = new RestRequest("", Method.Post);
                    request.AddHeader("Authorization", $"Zoho-oauthtoken {accessToken}");
                    request.AlwaysMultipartFormData = true;

                    byte[] fileBytes;
                    using (var ms = new MemoryStream())
                    {
                        await file.CopyToAsync(ms);
                        fileBytes = ms.ToArray();
                    }

                    request.AddFile("attachments", fileBytes, file.FileName, file.ContentType);
                    request.AddParameter("text", "Image sent via bot");
                    request.AddParameter("userids", zuid);
                    request.AddParameter("sync_message", true);

                    var response = await client.ExecuteAsync(request);

                    if (response.IsSuccessful) return Result.Ok(true);

                    // Only retry if it's a transient error
                    if (IsTransientError(response))
                        return Result.Fail<bool>($"Permanent error: {response.Content}");

                    // Last attempt failed
                    if (attempt == 2)
                        return Result.Fail<bool>($"Failed after 2 attempts: {response.Content}");
                }

                return Result.Fail<bool>("Unexpected error while sending image.");
            }
            catch (Exception ex)
            {
                return Result.Fail<bool>($"Exception: {ex.Message}");
            }
        }

        public async Task<Result<bool>> SendFileAsync(string zuid, IFormFile file)
        {
            if (!ValidateFile(file))
                return Result.Fail<bool>("Invalid file type or size. Max 10 MB.");

            try
            {
                var tokenResult = await _authService.GetAccessTokenAsync();
                if (tokenResult.IsFailed)
                    return Result.Fail<bool>(tokenResult.Errors[0].Message ?? "Error getting access token");

                var accessToken = tokenResult.Value;
                var client = new RestClient($"{_baseUrl}bots/{_botName}/message");

                for (int attempt = 1; attempt <= 2; attempt++)
                {
                    var request = new RestRequest("", Method.Post);
                    request.AddHeader("Authorization", $"Zoho-oauthtoken {accessToken}");
                    request.AlwaysMultipartFormData = true;

                    byte[] fileBytes;
                    using (var ms = new MemoryStream())
                    {
                        await file.CopyToAsync(ms);
                        fileBytes = ms.ToArray();
                    }

                    request.AddFile("attachments", fileBytes, file.FileName, file.ContentType);
                    request.AddParameter("text", "File sent via bot");
                    request.AddParameter("userids", zuid);
                    request.AddParameter("sync_message", true);

                    var response = await client.ExecuteAsync(request);

                    if (response.IsSuccessful) return Result.Ok(true);

                    if (IsTransientError(response))
                        return Result.Fail<bool>($"Permanent error: {response.Content}");

                    if (attempt == 2)
                        return Result.Fail<bool>($"Failed after 2 attempts: {response.Content}");
                }

                return Result.Fail<bool>("Unexpected error while sending file.");
            }
            catch (Exception ex)
            {
                return Result.Fail<bool>($"Exception: {ex.Message}");
            }
        }


        public async Task<Result<string>> SendFileToUserByZuidAsync(IFormFile file, string zuid , string comments)
        {
            if (file == null || file.Length == 0)
                return Result.Fail<string>("File is null or empty.");

            try
            {
                var tokenResult = await _authService.GetAccessTokenAsync();
                if (tokenResult.IsFailed)
                    return Result.Fail<string>(tokenResult.Errors[0].Message ?? "Error getting access token");

                var accessToken = tokenResult.Value;

                var client = new RestClient($"https://cliq.zoho.in/api/v2/buddies/{zuid}/files");
                var request = new RestRequest("",Method.Post);
                request.AddHeader("Authorization", $"Zoho-oauthtoken {accessToken}");
                request.AlwaysMultipartFormData = true;

                // Add the file
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    var fileBytes = ms.ToArray();
                    request.AddFile("files", fileBytes, file.FileName, file.ContentType);
                }

                // Add comments as proper JSON array in multipart
                // var commentsArray = new string[] { "Shared via API" };
                var commentsJson = JsonSerializer.Serialize(comments);
                request.AddParameter("comments", commentsJson); // Works as multipart field

                // Execute request
                var response = await client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                    return Result.Fail<string>($"File send failed: {response.StatusCode} - {response.Content}");

                return Result.Ok("✅ File successfully sent to user via ZUID!");
            }
            catch (Exception ex)
            {
                return Result.Fail<string>($"Exception during file send: {ex.Message}");
            }
        }





    }
}