﻿using CorpinatorBot.ConfigModels;
using Discord.Commands;
using Discord.WebSocket;

namespace CorpinatorBot.Discord
{
    public class GuildConfigSocketCommandContext : SocketCommandContext
    {
        public GuildConfigSocketCommandContext(DiscordSocketClient client, SocketUserMessage msg, GuildConfiguration config) : base(client, msg)
        {
            Configuration = config;
        }

        public GuildConfiguration Configuration { get; set; }
    }
}
