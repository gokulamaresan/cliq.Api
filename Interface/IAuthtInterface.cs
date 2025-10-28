using FluentResults;
using System.Threading.Tasks;

namespace Cliq.Api.Interface
{
    public interface IAuthtInterface
    {
        Task<Result<string>> GetAccessTokenAsync();       
        Task<Result<string>> RefreshAccessTokenAsync();   
        Task<Result<(string AccessToken, string RefreshToken)>> ExchangeCodeForTokensAsync(string code);
    }
}
