﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using MagicConchBot.Resources;
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
                try
                {
                    await audio.StopAsync();
                }
                catch (Exception)
                {

                }
            }
        }

        public static async Task<IVoiceChannel> GetAudioChannel(IInteractionContext context)
        {
            var channel = (context.User as IGuildUser)?.VoiceChannel;
            if (DebugTools.Debug && channel == null)
            {
                return (await context.Guild.GetVoiceChannelsAsync()).FirstOrDefault();
            }
            return channel;
        }

        public static async Task<IAudioClient> JoinChannelAsync(IAudioChannel channel)
        {
            try
            {
                if (channel != null)
                {
                    try
                    {
                        var client = await channel.ConnectAsync();
                        return client;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to join channel.");
            }

            return null;
        }
    }
}