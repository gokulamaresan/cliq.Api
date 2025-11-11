using cliq.Api.Models.Messages;
using FluentResults;

namespace Cliq.Api.Interface
{
    public interface IMessageInterface
    {
        Task<Result<List<UsersList>>> GetUsersAsync();
        Task<Result<bool>> SendMessageAsync(SendMessageRequest request);
        Task<Result<string>> SendFileToUserByZuidAsync(IFormFile file, string zuid, string comments);
        Task<Result<string>> SendTextMessageToUserByZuidAsync(string message, string zuid);
        Task<Result<string>> SendBotVoiceCallAsync(string message, List<string> userIds);

        Task<Result<string>> SendFileToUserByZuid(
            byte[] fileBytes,
            string fileName,
            string contentType,
            string zuid,
            string comments);
    }
}