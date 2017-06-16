﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
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
                await audio.StopAsync();
            }
        }

        public static async Task<IAudioClient> JoinChannelAsync(ICommandContext msg)
        {
            try
            {
                var channel = (msg.Message.Author as IGuildUser)?.VoiceChannel;
                if (DebugTools.Debug)
                {
                    var connectAsync = (await msg.Guild.GetVoiceChannelsAsync()).FirstOrDefault()?.ConnectAsync();
                    if (connectAsync != null)
                        return await connectAsync;
                }
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