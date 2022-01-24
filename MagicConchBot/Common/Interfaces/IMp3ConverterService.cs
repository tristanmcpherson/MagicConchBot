using Discord;

namespace MagicConchBot.Services
{
    public interface IMp3ConverterService
    {
        void GetMp3(Mp3Request song, IUser user);
    }
}