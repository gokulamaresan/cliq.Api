using FluentResults;
using Models.Account;
using Channel = Models.ChannelDto.Channel;

namespace Cliq.Api.Interface
{
    public interface IAccountInterface
    {
        Task<Result<List<User>>> GetUsersAsync();
        Task<Result<List<Channel>>> GetAllChannelsAsync();    
    }
}