namespace MagicConchBot.Common.Interfaces
{
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using MagicConchBot.Common.Types;

    public interface IMusicInfoService
    {
        Regex Regex { get; }

        Task<Song> GetSongInfoAsync(string url);
    }
}
