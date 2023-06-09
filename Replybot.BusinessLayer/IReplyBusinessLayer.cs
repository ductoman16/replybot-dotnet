﻿using Discord;
using Discord.WebSocket;
using Replybot.Models;

namespace Replybot.BusinessLayer;

public interface IReplyBusinessLayer
{
    Task<GuildReplyDefinition?> GetReplyDefinition(string message, string? guildId);
    Task<bool> IsBotNameMentioned(SocketMessage message, IGuild? guild, ulong botUserId);
}