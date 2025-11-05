using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using cliq.Api.Models.Channels;
using FluentResults;

namespace cliq.Api.Interface
{
    public interface IChannelInterface
    {
        Task<Result<List<ChannelDetailsDto>>> GetChannelDetails();
        Task<Result<string>> PostMessageInChannel(string message, string channelId);
        Task<Result<string>> UploadFileToChannelAsync(string channelId, IFormFile file , string? comments);
    }
}