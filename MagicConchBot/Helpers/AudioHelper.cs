using System;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using NLog;

namespace MagicConchBot.Helpers
{
    public static class AudioHelper
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static unsafe byte[] ChangeVol(byte[] audioSamples, float volume)
        {
            if (Math.Abs(volume - 1f) < 0.0001f)
                return audioSamples;

            // 16-bit precision for the multiplication
            var volumeFixed = (int) Math.Round(volume * 65536d);

            var count = audioSamples.Length / 2;

            fixed (byte* srcBytes = audioSamples)
            {
                var src = (short*) srcBytes;

                for (var i = count; i != 0; i--, src++)
                    *src = (short) ((*src * volumeFixed) >> 16);
            }

            return audioSamples;
        }

        public static async Task LeaveChannelAsync(IAudioClient audio)
        {
            if (audio != null && audio.ConnectionState == ConnectionState.Connected)
            {
                await audio.StopAsync();
            }
        }

        public static async Task<IAudioClient> JoinChannelAsync(IMessage msg)
        {
            try
            {
                var channel = (msg.Author as IGuildUser)?.VoiceChannel;
                return channel == null ? null : await channel.ConnectAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to join channel.");
            }

            return null;
        }
    }
}