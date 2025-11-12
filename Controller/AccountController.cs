using Microsoft.AspNetCore.Mvc;
using Cliq.Api.Interface;
using Cliq.Api.AdminApiAttribute;

namespace Cliq.Api.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [AdminApiKey]

    public class AccountController : ControllerBase
    {
        // private readonly string _logFilePath = Path.Combine(AppContext.BaseDirectory, "Logs", $"log-{DateTime.Now:yyyyMMdd}.txt");
        private readonly string _logFilePath = Path.Combine(
     Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName,
     "Logs",
     $"log-{DateTime.Now:yyyyMMdd}.txt");



        private readonly IAccountInterface _IAccountInterface;

        public AccountController(IAccountInterface IAccountInterface)
        {
            _IAccountInterface = IAccountInterface;
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
        public IActionResult GetSuccessLogs()
        {
            if (!System.IO.File.Exists(_logFilePath))
                return NotFound(new { message = "Log file not found" });

            var logs = System.IO.File.ReadAllLines(_logFilePath)
                .Where(line => line.Contains("✅ Success"))
                .ToList();

            return Ok(new
            {
                success = true,
                count = logs.Count,
                logs
            });
        }

        [HttpGet("error")]
        public IActionResult GetErrorLogs()
        {
            if (!System.IO.File.Exists(_logFilePath))
                return NotFound(new { message = "Log file not found" });

            var logs = System.IO.File.ReadAllLines(_logFilePath)
                .Where(line => line.Contains("❌ Error"))
                .ToList();

            return Ok(new
            {
                success = true,
                count = logs.Count,
                logs
            });
        }

        [HttpGet("all")]
        public IActionResult GetAllLogs()
        {
            if (!System.IO.File.Exists(_logFilePath))
                return NotFound(new { message = "Log file not found" });

            var logs = System.IO.File.ReadAllLines(_logFilePath).ToList();

            return Ok(new
            {
                success = true,
                count = logs.Count,
                logs
            });
        }

    }
}
