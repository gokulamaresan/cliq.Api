using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentResults;
using Models.Account;

namespace Cliq.Api.Interface
{
    public interface IMessageInterface
    {
        Task<Result<bool>> SendMessageAsync(SendMessageRequest request);
        Task<Result<bool>> SendImageAsync(string zuid, IFormFile file);
        Task<Result<bool>> SendFileAsync(string zuid, IFormFile file);
        Task<Result<string>> SendFileToUserByZuidAsync(IFormFile file, string zuid , string comments);
    }
}