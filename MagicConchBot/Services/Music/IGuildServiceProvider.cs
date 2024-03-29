﻿namespace MagicConchBot.Services.Music {
    public interface IGuildServiceProvider<TT>
    {
        TT AddService<TInterface, TImplementation>(ulong guildId) 
            where TInterface : class
            where TImplementation : class, TInterface;
        T GetService<T>(ulong guildId) where T : class;
    }
}