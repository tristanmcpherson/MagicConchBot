using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MagicConchBot.Common.Types;

namespace MagicConchBot.Common.Interfaces
{
    public interface ISongInfoService
    {
        Regex Regex { get; }

        Task<Song> GetSongInfoAsync(string url);
    }
} 