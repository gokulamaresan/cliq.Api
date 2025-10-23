using Cliq.Api.Interface;
using Cliq.Api.Services;
using FluentResults;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Cliq.Api.Repository
{
    public class AuthRepository : IAuthtInterface
    {
        private readonly CliqAuthService _authService;

        public AuthRepository(IConfiguration configuration)
        {
            _authService = new CliqAuthService(configuration);
        }

        public async Task<Result<string>> GetAccessTokenAsync()
        {
            try
            {
                var result = await _authService.GetAccessTokenAsync();

                if (!result.IsSuccess)
                    return Result.Fail(result.Errors[0].Message ?? "Error In Getting Value From GetAccessTokenAsync");

                return Result.Ok(result.Value);
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.Message);
            }
        }

        public async Task<Result<string>> RefreshAccessTokenAsync()
        {
            try
            {
                var result = await _authService.RefreshAccessTokenAsync();
                 if (!result.IsSuccess)
                    return Result.Fail(result.Errors[0].Message ?? "Error while Make Refresh Token RefreshAccessTokenAsync");

                return Result.Ok(result.Value);
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.Message);
            }
        }

        public async Task<Result<(string AccessToken, string RefreshToken)>> ExchangeCodeForTokensAsync(string code)
        {

            try
            {
                var result = await _authService.ExchangeCodeForTokensAsync(code);
                if (!result.IsSuccess)
                    return Result.Fail<(string, string)>(result.Errors[0].Message ?? "Error In Getting Value From ExchangeCodeForTokensAsync");

                var tuple = (result.Value.AccessToken, result.Value.RefreshToken);
                return Result.Ok(tuple);
            }
            catch (Exception ex)
            {
                return Result.Fail<(string, string)>(ex.Message);
            }
        }
    }
}
