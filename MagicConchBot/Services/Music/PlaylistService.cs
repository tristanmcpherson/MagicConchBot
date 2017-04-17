using System.Collections.Generic;
using LiteDB;
using MagicConchBot.Common.Types;

namespace MagicConchBot.Services.Music
{
    public class PlaylistService
    {
        private readonly LiteDatabase _db;

        public PlaylistService()
        {
            _db = new LiteDatabase(@"Playlists.db");
        }

        public List<Playlist> GetPlaylists(ulong guildId)
        {
            return _db.GetCollection<List<Playlist>>().FindById(guildId);
        }

        public bool UpsertPlaylists(ulong guildId, List<Playlist> playlists)
        {
            return _db.GetCollection<List<Playlist>>().Upsert(guildId, playlists);
        }
    }
}
