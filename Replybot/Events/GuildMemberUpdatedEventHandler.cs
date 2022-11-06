﻿using Replybot.BusinessLayer;

namespace Replybot.Events;

public class GuildMemberUpdatedEventHandler
{
    private readonly IGuildConfigurationBusinessLayer _guildConfigurationBusinessLayer;

    public GuildMemberUpdatedEventHandler(IGuildConfigurationBusinessLayer guildConfigurationBusinessLayer)
    {
        _guildConfigurationBusinessLayer = guildConfigurationBusinessLayer;
    }

    public async Task HandleEvent(Cacheable<SocketGuildUser, ulong> cachedOldUser, SocketGuildUser newUser)
    {
        if (!cachedOldUser.HasValue)
        {
            return;
        }

        var oldUser = cachedOldUser.Value;

        var guildConfig = await _guildConfigurationBusinessLayer.GetGuildConfiguration(newUser.Guild);
        var announceChange = guildConfig.EnableAvatarAnnouncements;
        var tagUserInChange = guildConfig.EnableAvatarMentions;

        if (!announceChange)
        {
            return;
        }
        if (newUser.GuildAvatarId != oldUser.GuildAvatarId)
        {
            var avatarUrl = newUser.GetGuildAvatarUrl(ImageFormat.Jpeg);
            if (string.IsNullOrEmpty(avatarUrl))
            {
                avatarUrl = newUser.GetDisplayAvatarUrl(ImageFormat.Jpeg);
            }
            await newUser.Guild.SystemChannel.SendMessageAsync(
                $"Heads up! {(tagUserInChange ? newUser.Mention : newUser.Username)} has a new look! Check it out: {avatarUrl}");
        }
    }
}