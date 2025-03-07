﻿using System;
using System.Collections.Concurrent;
using System.IO;
using MagicConchBot.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace MagicConchBot.Services.Music
{
    public class GuildServiceProvider : IGuildServiceProvider<GuildServiceProvider> {
        private readonly ConcurrentDictionary<ulong, IServiceCollection> _musicServices =
            new();
        private readonly ConcurrentDictionary<ulong, IServiceProvider> _musicServiceProviders = new();

        //private readonly ConcurrentDictionary<ulong, Mp3ConverterService> _mp3Services =
        //    new ConcurrentDictionary<ulong, Mp3ConverterService>();

        public GuildServiceProvider AddService<TInterface, TImplementation>(ulong guildId) where TInterface : class where TImplementation : class, TInterface
        {
            if (!_musicServices.ContainsKey(guildId))
                _musicServices.TryAdd(guildId, new ServiceCollection());
            _musicServices[guildId].AddSingleton<TInterface, TImplementation>();
            return this;
        }

        public GuildServiceProvider AddService<TInterface, TImplementation>(ulong guildId, TImplementation implementation) where TInterface : class where TImplementation : class, TInterface
        {
            if (!_musicServices.ContainsKey(guildId))
                _musicServices.TryAdd(guildId, new ServiceCollection());
            _musicServices[guildId].AddSingleton<TInterface, TImplementation>((provider) => implementation);
            return this;
        }


        public T GetService<T>(ulong guildId) where T : class 
        {
            if (!_musicServiceProviders.TryGetValue(guildId, out var provider))
            {
                _musicServiceProviders[guildId] = _musicServices[guildId].BuildServiceProvider();
                provider = _musicServiceProviders[guildId];
            }

            return provider.GetService<T>();
        }
        //public Mp3ConverterService GetMp3Service(ulong guildId)
        //{
        //    if (!_mp3Services.TryGetValue(guildId, out Mp3ConverterService service))
        //    {
        //        Log.Error("Server mp3 service was not created, creating.");
        //        service = new Mp3ConverterService();
        //        _mp3Services.TryAdd(guildId, service);
        //    }

        //    return service;
        //}

        //public void StopAll()
        //{
        //    foreach (var musicService in _musicServices)
        //        if (musicService.Value.Stop())
        //            Log.Info($"Successfully stopped music for GuildId: {musicService.Key}");
        //}
    }
}