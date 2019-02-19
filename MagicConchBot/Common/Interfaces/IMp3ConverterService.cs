using System.Threading.Tasks;
using Discord;
using MagicConchBot.Common.Types;

namespace MagicConchBot.Services
{
    public interface IMp3ConverterService
    {
        Task GetMp3(Song song, IUser user);
    }
}