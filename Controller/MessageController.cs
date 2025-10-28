using Cliq.Api.Interface;
using Microsoft.AspNetCore.Mvc;
using Models.Account;

namespace Cliq.Api.Controller
{
    [Route("[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly IMessageInterface _IMessageInterface;

        public MessageController(IMessageInterface IMessageInterface)
        {
            _IMessageInterface = IMessageInterface;
        }

        [HttpPost("send-text-message")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                var result = await _IMessageInterface.SendMessageAsync(request);
                if (result.IsFailed)
                    return BadRequest(new { Error = result.Errors[0].Message });

                return Ok(new { Message = "Message sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Unexpected error: {ex.Message}" });
            }
        }

        [HttpPost("send-image")]
        public async Task<IActionResult> SendImageAsync([FromForm] string zuid, IFormFile image)
        {
            try
            {
                if (image == null || string.IsNullOrEmpty(zuid))
                    return BadRequest(new { Error = "ZUID and Image file are required" });

                var result = await _IMessageInterface.SendImageAsync(zuid, image);
                if (result.IsFailed)
                    return BadRequest(new { Error = result.Errors[0].Message });

                return Ok(new { Message = "Image sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Unexpected error: {ex.Message}" });
            }
        }

        [HttpPost("send-file")]
        public async Task<IActionResult> SendFileAsync([FromForm] string zuid, IFormFile file)
        {
            try
            {
                if (file == null || string.IsNullOrEmpty(zuid))
                    return BadRequest(new { Error = "ZUID and File are required" });

                var result = await _IMessageInterface.SendFileAsync(zuid, file);
                if (result.IsFailed)
                    return BadRequest(new { Error = result.Errors[0].Message });

                return Ok(new { Message = "File sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Unexpected error: {ex.Message}" });
            }
        }

        [HttpPost("upload-file")]
        public async Task<IActionResult> UploadFile([FromForm] string zuid, IFormFile file , [FromForm] string comments)
        {
            try
            {
                if (string.IsNullOrEmpty(zuid))
                    return BadRequest(new { Error = "ZUID is required." });

                if (file == null || file.Length == 0)
                    return BadRequest(new { Error = "File is required." });

                var result = await _IMessageInterface.SendFileToUserByZuidAsync(file , zuid , comments);

                if (result.IsFailed)
                    return BadRequest(new { Error = result.Errors[0].Message });

                return Ok(new
                {
                    Message = "File uploaded successfully",
                    FileId = result.Value
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Unexpected error: {ex.Message}" });
            }
        }

    }
}
