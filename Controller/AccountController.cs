using Microsoft.AspNetCore.Mvc;
using Cliq.Api.Interface;
using Cliq.Api.AdminApiAttribute;
using Cliq.Api.Attributes;  // Assuming [SkipApiKey] is defined here
using System.IO;  // Ensure this is imported for FileStream and StreamReader

namespace Cliq.Api.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    // [AdminApiKey]  // Commented out, as you're using [SkipApiKey]
    [SkipApiKey]  // Skips API key validation (custom attribute)

    public class AccountController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly string _logFilePath;
        private readonly IAccountInterface _IAccountInterface;

        public AccountController(IAccountInterface IAccountInterface, IWebHostEnvironment env)
        {
            _IAccountInterface = IAccountInterface;
            _env = env;
            // Path now matches Serilog's write location
            _logFilePath = Path.Combine(_env.ContentRootPath, "Logs");
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var result = await _IAccountInterface.GetUsersAsync();
                if (result.IsFailed)
                    return BadRequest(new { errors = result.Errors[0].Message });

                return Ok(result.Value);
            }
            catch (Exception ex)
            {
                return BadRequest(new { errors = ex.Message });
            }
        }

        [HttpGet("Get-Channels")]
        public async Task<IActionResult> GetChannels()
        {
            try
            {
                var result = await _IAccountInterface.GetAllChannelsAsync();
                if (result.IsFailed)
                    return BadRequest(new { errors = result.Errors[0].Message });

                return Ok(result.Value);
            }
            catch (Exception ex)
            {
                return BadRequest(new { errors = ex.Message });
            }
        }

        [HttpGet("success")]
        public async Task<IActionResult> GetSuccessLogs(string filename = "cliqapi-20251112.log")
        {
            try
            {
                filename = $"cliqapi-{DateTime.Now:yyyyMMdd}.log";
                if (!System.IO.File.Exists(Path.Combine(_logFilePath, filename)))
                    return NotFound(new { message = "Log file not found" });

                var logs = await ReadFilteredLogsAsync(line => line.Contains("✅ Success"), filename);
                return Ok(new
                {
                    success = true,
                    count = logs.Count,
                    logs
                });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { errors = $"Service unavailable: {ex.Message}" });  // 503 for server error
            }
        }

        [HttpGet("error")]
        public async Task<IActionResult> GetErrorLogs(string filename = "cliqapi-20251112.log")
        {
            try
            {
                filename = $"cliqapi-{DateTime.Now:yyyyMMdd}.log";
                if (!System.IO.File.Exists(Path.Combine(_logFilePath, filename)))
                    return NotFound(new { message = "Log file not found" });

                var logs = await ReadFilteredLogsAsync(line => line.Contains("❌ Error") , filename);
                return Ok(new
                {
                    success = true,
                    count = logs.Count,
                    logs
                });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { errors = $"Service unavailable: {ex.Message}" });
            }
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllLogs(string filename = "cliqapi-20251112.log")
        {
            try
            {
                filename = $"cliqapi-{DateTime.Now:yyyyMMdd}.log";
                if (!System.IO.File.Exists(Path.Combine(_logFilePath, filename)))
                    return NotFound(new { message = "Log file not found" });

                var logs = await ReadAllLogsAsync(filename);
                return Ok(new
                {
                    success = true,
                    count = logs.Count,
                    logs
                });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { errors = $"Service unavailable: {ex.Message}" });
            }
        }

        // Helper method to read and filter logs with shared access
        private async Task<List<string>> ReadFilteredLogsAsync(Func<string, bool> filter , string filename)
        {
            var logs = new List<string>();
            using (var fileStream = new FileStream(Path.Combine(_logFilePath, filename), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fileStream))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (filter(line))
                    {
                        logs.Add(line);
                    }
                }
            }
            return logs;
        }

        // Helper method to read all logs with shared access
        private async Task<List<string>> ReadAllLogsAsync(string filename)
        {
            var logs = new List<string>();
            using (var fileStream = new FileStream(Path.Combine(_logFilePath, filename), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fileStream))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    logs.Add(line);
                }
            }
            return logs;
        }
    }
}
