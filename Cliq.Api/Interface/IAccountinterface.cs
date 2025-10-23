using FluentResults;
using Models.Account;

namespace Cliq.Api.Interface
{
    public interface IAccountInterface
    {
        Task<Result<List<User>>> GetUsersAsync();
    
    }
}