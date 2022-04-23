using System;
using System.Threading.Tasks;
using MagicConchBot.Common.Types;

namespace MagicConchBot.Services
{
    public interface ISongResolutionService
    {
        Task<Song> ResolveSong(string url, TimeSpan startTime);
    }
}