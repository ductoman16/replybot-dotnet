﻿using Discord;
using Replybot.DataLayer;
using Replybot.Models;
using System;

namespace Replybot.BusinessLayer;

public class GuildConfigurationBusinessLayer : IGuildConfigurationBusinessLayer
{
    private readonly IReplyDataLayer _replyDataLayer;

    public GuildConfigurationBusinessLayer(IReplyDataLayer replyDataLayer)
    {
        _replyDataLayer = replyDataLayer;
    }

    public async Task<GuildConfiguration> GetGuildConfiguration(IGuild guild)
    {
        return await _replyDataLayer.GetConfigurationForGuild(guild.Id.ToString(), guild.Name);
    }

    public async Task<bool> UpdateGuildConfiguration(IGuild guild)
    {
        GuildConfiguration? config = await _replyDataLayer.GetConfigurationForGuild(guild.Id.ToString(), guild.Name);
        if (config != null)
        {
            return await _replyDataLayer.UpdateGuildConfiguration(guild.Id.ToString(), guild.Name);
        }

        return false;
    }

    public async Task<bool> DeleteGuildConfiguration(IGuild guild)
    {
        GuildConfiguration? config = await _replyDataLayer.GetConfigurationForGuild(guild.Id.ToString(), guild.Name);
        if (config != null)
        {
            return await _replyDataLayer.DeleteGuildConfiguration(guild.Id.ToString());
        }

        return true;
    }

    public async Task<bool> SetApprovedUsers(IGuild guild, List<string> userIds, bool setAllowed)
    {
        if (setAllowed)
        {
            return await _replyDataLayer.AddAllowedUserIds(guild.Id.ToString(), guild.Name, userIds);
        }
        return await _replyDataLayer.RemoveAllowedUserIds(guild.Id.ToString(), guild.Name, userIds);
    }

    public async Task<bool> SetAvatarAnnouncementEnabled(IGuild guild, bool isEnabled)
    {
        GuildConfiguration? config = await _replyDataLayer.GetConfigurationForGuild(guild.Id.ToString(), guild.Name);
        if (config != null)
        {
            return await _replyDataLayer.SetEnableAvatarAnnouncements(guild.Id.ToString(), isEnabled);
        }

        return false;
    }

    public async Task<bool> SetAvatarMentionEnabled(IGuild guild, bool isEnabled)
    {
        GuildConfiguration? config = await _replyDataLayer.GetConfigurationForGuild(guild.Id.ToString(), guild.Name);
        if (config != null)
        {
            return await _replyDataLayer.SetEnableAvatarMentions(guild.Id.ToString(), isEnabled);
        }

        return false;
    }

    public async Task<bool> SetLogChannel(IGuild guild, string? channelId)
    {
        GuildConfiguration? config = await _replyDataLayer.GetConfigurationForGuild(guild.Id.ToString(), guild.Name);
        if (config != null)
        {
            return await _replyDataLayer.SetLogChannel(guild.Id.ToString(), channelId);
        }

        return false;
    }

    public async Task<bool> CanUserAdmin(IGuild guild, IGuildUser user)
    {
        var config = await GetGuildConfiguration(guild);
        return config.AdminUserIds.Contains(user.Id.ToString());
    }
}