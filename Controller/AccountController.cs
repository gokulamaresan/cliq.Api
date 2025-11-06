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


    }
}
