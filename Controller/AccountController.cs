using Cliq.Api.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Models.Account;
using Cliq.Api.Interface;

namespace Cliq.Api.Controller
{
    [Route("api/[controller]")]
    [ApiController]
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

       
    }
}
