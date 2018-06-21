using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MagicConchBotApp.Common.Types;

namespace MagicConchBotApp.Common.Interfaces
{
    public interface ISongInfoService
    {
        Regex Regex { get; }

        Task<Song> GetSongInfoAsync(string url);
    }
} 