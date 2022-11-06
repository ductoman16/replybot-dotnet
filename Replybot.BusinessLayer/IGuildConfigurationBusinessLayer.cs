﻿using Discord;
using Replybot.Models;

namespace Replybot.BusinessLayer;

public interface IGuildConfigurationBusinessLayer
{
    Task<GuildConfiguration> GetGuildConfiguration(IGuild guild);
    Task<bool> UpdateGuildConfiguration(IGuild guild);
    Task<bool> SetAvatarAnnouncementEnabled(IGuild guild, bool isEnabled);
    Task<bool> SetAvatarMentionEnabled(IGuild guild, bool isEnabled);
}