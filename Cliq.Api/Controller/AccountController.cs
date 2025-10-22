using Microsoft.AspNetCore.Mvc;
using RestSharp;
using System.Text.Json; // For JSON deserialization

namespace Cliq.Api.Controller
{
    // Model classes to represent the API response
    public class UsersResponse
    {
        public string next_token { get; set; }
        public bool has_more { get; set; }
        public List<User> data { get; set; }
    }

    public class User
    {
        public string email_id { get; set; }
        public string zuid { get; set; }
        public string zoid { get; set; }
        public string display_name { get; set; }
        public string name { get; set; }
        public string organization_id { get; set; }
        public string id { get; set; }
    }

    // New model for sending a message
    public class SendMessageRequest
    {
        public string Zuid { get; set; } // ZUID of the recipient user
        public string Message { get; set; } // The message text
    }


    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        [HttpGet("users")]
        public async Task<IActionResult> GetUsersAsync()
        {
            try
            {
                var client = new RestClient("https://cliq.zoho.in/api/v2/users");
                var request = new RestRequest("", Method.Get);
                request.AddHeader("Authorization", $"1000.d85e6fc9699c18f54193161f65679cba.1aa059a10d03cde76eb98a56f148a2d5");

                var response = await client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                {
                    return BadRequest(new { Error = "Failed to fetch users", Details = response.Content });
                }

                var usersResponse = JsonSerializer.Deserialize<UsersResponse>(response.Content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true // To handle case sensitivity if needed
                });

                return Ok(usersResponse);
            }
            catch (JsonException jsonEx)
            {
                return StatusCode(500, new { Error = "Failed to process the response", Details = jsonEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "An internal error occurred", Details = ex.Message });
            }
        }



        [HttpPost("send-message")]
        public async Task<IActionResult> SendMessageAsync([FromBody] SendMessageRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Zuid)
                || string.IsNullOrEmpty(request.Message))
            {
                return BadRequest(new { Error = "Invalid request. Please provide zuid, message, and botUniqueName." });
            }

            try
            {

                // Zoho Bot API endpoint for sending messages
                var client = new RestClient($"https://cliq.zoho.in/api/v2/bots/itdbot/message");
                var restRequest = new RestRequest("", Method.Post);

                // Use OAuth access token here (replace with your token)
                restRequest.AddHeader("Authorization", $"Zoho-oauthtoken 1000.d85e6fc9699c18f54193161f65679cba.1aa059a10d03cde76eb98a56f148a2d5");
                restRequest.AddHeader("Content-Type", "application/json");

                // Correct request body for Zoho Bot API
                var body = new
                {
                    text = request.Message,
                    userids = request.Zuid,
                    sync_message = true
                };

                restRequest.AddJsonBody(body);

                var response = await client.ExecuteAsync(restRequest);

                if (!response.IsSuccessful)
                {
                    return BadRequest(new { Error = "Failed to send message", Details = response.Content });
                }

                return Ok(new { Message = "Message sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Error = "An internal error occurred",
                    Details = ex.Message
                });
            }
        }


    }
}
