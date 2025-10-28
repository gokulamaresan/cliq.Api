// using FluentResults;
// using Microsoft.AspNetCore.Mvc;
// using RestSharp;
// using System.Net.Http.Headers;
// using System.Text.Json;

// namespace Cliq.Api.Controller
// {
//     [Route("api/[controller]")]
//     [ApiController]
//     public class DemoController : ControllerBase
//     {
//         private readonly IAuthService _authService;
//         private readonly string _baseUrl = "https://cliq.zoho.in/api/v2/";
//         private readonly string _botName = "itdbot";

//         public DemoController(IAuthService authService)
//         {
//             _authService = authService;
//         }

//         // 1️⃣ Send text message
//         [HttpPost("send-text")]
//         public async Task<IActionResult> SendText([FromBody] SendMessageRequest request)
//         {
//             try
//             {
//                 var tokenResult = await _authService.GetAccessTokenAsync();
//                 if (tokenResult.IsFailed) return BadRequest(new { Error = tokenResult.Errors[0].Message });

//                 var client = new RestClient($"{_baseUrl}bots/{_botName}/message");
//                 var restRequest = new RestRequest("", Method.Post);
//                 restRequest.AddHeader("Authorization", $"Zoho-oauthtoken {tokenResult.Value}");
//                 restRequest.AddHeader("Content-Type", "application/json");
//                 restRequest.AddJsonBody(new { text = request.Message, userids = request.Zuid, sync_message = true });

//                 var response = await client.ExecuteAsync(restRequest);
//                 if (!response.IsSuccessful)
//                     return BadRequest(new { Error = $"Failed to send message: {response.Content}" });

//                 return Ok(new { Message = "Text message sent successfully" });
//             }
//             catch (Exception ex)
//             {
//                 return StatusCode(500, new { Error = ex.Message });
//             }
//         }

//         // 2️⃣ Send file directly via bot
//         [HttpPost("send-file-direct")]
//         public async Task<IActionResult> SendFileDirect([FromForm] string zuid, IFormFile file)
//         {
//             if (string.IsNullOrEmpty(zuid) || file == null || file.Length == 0)
//                 return BadRequest(new { Error = "ZUID and file are required." });

//             try
//             {
//                 var tokenResult = await _authService.GetAccessTokenAsync();
//                 if (tokenResult.IsFailed) return BadRequest(new { Error = tokenResult.Errors[0].Message });

//                 using var ms = new MemoryStream();
//                 await file.CopyToAsync(ms);
//                 var fileBytes = ms.ToArray();

//                 var client = new RestClient($"{_baseUrl}bots/{_botName}/message");
//                 var restRequest = new RestRequest("", Method.Post);
//                 restRequest.AddHeader("Authorization", $"Zoho-oauthtoken {tokenResult.Value}");
//                 restRequest.AlwaysMultipartFormData = true;

//                 restRequest.AddFile("attachments", fileBytes, file.FileName, file.ContentType);
//                 restRequest.AddParameter("text", "File sent via bot");
//                 restRequest.AddParameter("userids", zuid);
//                 restRequest.AddParameter("sync_message", true);

//                 var response = await client.ExecuteAsync(restRequest);
//                 if (!response.IsSuccessful)
//                     return BadRequest(new { Error = $"Failed to send file: {response.Content}" });

//                 return Ok(new { Message = "File sent directly successfully" });
//             }
//             catch (Exception ex)
//             {
//                 return StatusCode(500, new { Error = ex.Message });
//             }
//         }

//         // 3️⃣ Upload file to Zoho Files
//         private async Task<Result<string>> UploadFileToZohoAsync(string accessToken, IFormFile file)
//         {
//             if (file == null || file.Length == 0)
//                 return Result.Fail<string>("File is null or empty.");

//             try
//             {
//                 using var ms = new MemoryStream();
//                 await file.CopyToAsync(ms);
//                 var fileBytes = ms.ToArray();

//                 var client = new RestClient($"{_baseUrl}files");
//                 var request = new RestRequest("", Method.Post);
//                 request.AddHeader("Authorization", $"Zoho-oauthtoken {accessToken}");
//                 request.AlwaysMultipartFormData = true;
//                 request.AddFile("file", fileBytes, file.FileName, file.ContentType);

//                 var response = await client.ExecuteAsync(request);
//                 if (!response.IsSuccessful)
//                     return Result.Fail<string>($"Zoho file upload failed: {response.StatusCode} - {response.Content}");

//                 var json = JsonDocument.Parse(response.Content);
//                 if (json.RootElement.TryGetProperty("data", out var dataElement) &&
//                     dataElement.TryGetProperty("file_id", out var fileIdElement))
//                 {
//                     return Result.Ok(fileIdElement.GetString() ?? "");
//                 }

//                 return Result.Fail<string>("File uploaded but file_id not found in response.");
//             }
//             catch (Exception ex)
//             {
//                 return Result.Fail<string>($"Exception uploading file: {ex.Message}");
//             }
//         }

//         // 4️⃣ Upload to Zoho then send file
//         [HttpPost("upload-file-then-send")]
//         public async Task<IActionResult> UploadFileThenSend([FromForm] string zuid, IFormFile file)
//         {
//             if (string.IsNullOrEmpty(zuid) || file == null || file.Length == 0)
//                 return BadRequest(new { Error = "ZUID and file are required." });

//             try
//             {
//                 var tokenResult = await _authService.GetAccessTokenAsync();
//                 if (tokenResult.IsFailed) return BadRequest(new { Error = tokenResult.Errors[0].Message });

//                 var uploadResult = await UploadFileToZohoAsync(tokenResult.Value, file);
//                 if (uploadResult.IsFailed) return BadRequest(new { Error = uploadResult.Errors[0].Message });

//                 // Send message with file_id
//                 var client = new RestClient($"{_baseUrl}bots/{_botName}/message");
//                 var request = new RestRequest("", Method.Post);
//                 request.AddHeader("Authorization", $"Zoho-oauthtoken {tokenResult.Value}");
//                 request.AddHeader("Content-Type", "application/json");
//                 request.AddJsonBody(new
//                 {
//                     text = "File sent via Zoho Files",
//                     userids = zuid,
//                     sync_message = true,
//                     attachments = new[]
//                     {
//                         new { file_id = uploadResult.Value }
//                     }
//                 });

//                 var response = await client.ExecuteAsync(request);
//                 if (!response.IsSuccessful)
//                     return BadRequest(new { Error = $"Failed to send file: {response.Content}" });

//                 return Ok(new { Message = "File uploaded and sent successfully", FileId = uploadResult.Value });
//             }
//             catch (Exception ex)
//             {
//                 return StatusCode(500, new { Error = ex.Message });
//             }
//         }
//     }

//     // Simple Result wrapper for success/failure
//     public class Result<T>
//     {
//         public bool IsSuccess { get; private set; }
//         public bool IsFailed => !IsSuccess;
//         public T? Value { get; private set; }
//         public string[] Errors { get; private set; } = Array.Empty<string>();

//         public static Result<T> Ok(T value) => new Result<T> { IsSuccess = true, Value = value };
//         public static Result<T> Fail(string error) => new Result<T> { IsSuccess = false, Errors = new[] { error } };
//     }

//     public class SendMessageRequest
//     {
//         public string Zuid { get; set; } = string.Empty;
//         public string Message { get; set; } = string.Empty;
//     }
// }
