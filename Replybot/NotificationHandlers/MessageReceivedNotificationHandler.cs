﻿using MediatR;
using Replybot.BusinessLayer;
using Replybot.Models;
using Replybot.Notifications;
using Replybot.TextCommands;
using System.Text.RegularExpressions;

namespace Replybot.NotificationHandlers;
public class MessageReceivedNotificationHandler : INotificationHandler<MessageReceivedNotification>
{
    private readonly IReplyBusinessLayer _replyBusinessLayer;
    private readonly KeywordHandler _keywordHandler;
    private readonly HowLongToBeatCommand _howLongToBeatCommand;
    private readonly DefineWordCommand _defineWordCommand;
    private readonly GetFortniteShopInformationCommand _fortniteShopInformationCommand;
    private readonly PollCommand _pollCommand;
    private readonly FixTwitterCommand _fixTwitterCommand;
    private readonly VersionSettings _versionSettings;
    private readonly DiscordSocketClient _client;
    private readonly LogMessageBuilder _logMessageBuilder;
    private readonly ILogger<DiscordBot> _logger;

    public MessageReceivedNotificationHandler(IReplyBusinessLayer replyBusinessLayer,
        KeywordHandler keywordHandler,
        HowLongToBeatCommand howLongToBeatCommand,
        DefineWordCommand defineWordCommand,
        GetFortniteShopInformationCommand fortniteShopInformationCommand,
        PollCommand pollCommand,
        FixTwitterCommand fixTwitterCommand,
        VersionSettings versionSettings,
        DiscordSocketClient client,
        LogMessageBuilder logMessageBuilder,
        ILogger<DiscordBot> logger)
    {
        _replyBusinessLayer = replyBusinessLayer;
        _keywordHandler = keywordHandler;
        _howLongToBeatCommand = howLongToBeatCommand;
        _defineWordCommand = defineWordCommand;
        _fortniteShopInformationCommand = fortniteShopInformationCommand;
        _pollCommand = pollCommand;
        _fixTwitterCommand = fixTwitterCommand;
        _versionSettings = versionSettings;
        _client = client;
        _logMessageBuilder = logMessageBuilder;
        _logger = logger;
    }

    public Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            var message = notification.Message;
            if (message.Author.IsBot)
            {
                return Task.CompletedTask;
            }

            var messageChannel = message.Channel;
            var guildChannel = messageChannel as SocketGuildChannel;

            if (guildChannel != null)
            {
                await HandleDiscordMessageLink(guildChannel, notification.Message);
            }

            var replyDefinition = await _replyBusinessLayer.GetReplyDefinition(message.Content, guildChannel?.Guild.Id);
            if (replyDefinition == null)
            {
                return Task.CompletedTask;
            }

            var isBotMentioned = await IsBotMentioned(message, guildChannel);
            if (replyDefinition.RequiresBotName && !isBotMentioned)
            {
                return Task.CompletedTask;
            }

            await HandleReactions(message, replyDefinition);

            if ((replyDefinition.Replies == null || !replyDefinition.Replies.Any()) &&
                (replyDefinition.UserReplies == null || !replyDefinition.UserReplies.Any()))
            {
                return Task.CompletedTask;
            }
            var reply = ChooseReply(replyDefinition, message.Author);

            if (string.IsNullOrEmpty(reply))
            {
                return Task.CompletedTask;
            }
            var wasDeleted = await HandleDelete(message, reply);
            var messageReference = wasDeleted ? null : new MessageReference(message.Id);

            // handle commands
            if (reply == _keywordHandler.BuildKeyword(TriggerKeyword.HowLongToBeat))
            {
                var howLongToBeatEmbed = await _howLongToBeatCommand.GetHowLongToBeatEmbed(message);
                if (howLongToBeatEmbed != null)
                {
                    await messageChannel.SendMessageAsync(embed: howLongToBeatEmbed,
                        messageReference: messageReference);
                }

                return Task.CompletedTask;
            }

            if (reply == _keywordHandler.BuildKeyword(TriggerKeyword.DefineWord))
            {
                var defineWordEmbed = await _defineWordCommand.GetWordDefinitionEmbed(message);
                if (defineWordEmbed != null)
                {
                    await messageChannel.SendMessageAsync(embed: defineWordEmbed,
                        messageReference: messageReference);
                }

                return Task.CompletedTask;
            }

            if (reply == _keywordHandler.BuildKeyword(TriggerKeyword.FortniteShopInfo))
            {
                var fortniteShopInfoEmbed =
                    await _fortniteShopInformationCommand.GetFortniteShopInformationEmbed(message);
                if (fortniteShopInfoEmbed != null)
                {
                    await messageChannel.SendMessageAsync(embed: fortniteShopInfoEmbed,
                        messageReference: messageReference);
                }

                return Task.CompletedTask;
            }

            if (reply == _keywordHandler.BuildKeyword(TriggerKeyword.Poll))
            {
                var (pollEmbed, reactionEmotes) = _pollCommand.BuildPollEmbed(message);
                if (pollEmbed == null)
                {
                    return Task.CompletedTask;
                }
                var messageSent = await messageChannel.SendMessageAsync(embed: pollEmbed,
                    messageReference: messageReference);
                if (messageSent != null && reactionEmotes != null)
                {
                    await messageSent.AddReactionsAsync(reactionEmotes);
                }

                return Task.CompletedTask;
            }

            if (reply == _keywordHandler.BuildKeyword(TriggerKeyword.FixTwitter) || reply == _keywordHandler.BuildKeyword(TriggerKeyword.BreakTwitter))
            {
                var keywordToPass = TriggerKeyword.FixTwitter;
                if (reply == _keywordHandler.BuildKeyword(TriggerKeyword.BreakTwitter))
                {
                    keywordToPass = TriggerKeyword.BreakTwitter;
                }
                var fixedTwitterMessage = await _fixTwitterCommand.GetFixedTwitterMessage(notification.Message, keywordToPass);
                if (fixedTwitterMessage != null)
                {
                    await messageChannel.SendMessageAsync(fixedTwitterMessage.Value.fixedTwitterMessage,
                        messageReference: fixedTwitterMessage.Value.messageToReplyTo);
                    return Task.CompletedTask;
                }
            }

            var messageText = _keywordHandler.ReplaceKeywords(reply,
                message.Author.Username,
                message.Author.Id,
                _versionSettings.VersionNumber,
                message.Content,
                replyDefinition,
                message.MentionedUsers.ToList(),
                guildChannel?.Guild,
                guildChannel);

            await messageChannel.SendMessageAsync(
                messageText,
                messageReference: messageReference
            );

            return Task.CompletedTask;
        }, cancellationToken);
        return Task.CompletedTask;
    }

    private async Task HandleDiscordMessageLink(SocketGuildChannel channel, SocketMessage messageWithLink)
    {
        var discordLinkRegex =
            new Regex(
                @"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        var discordLinkMatches = discordLinkRegex.Matches(messageWithLink.Content);
        if (discordLinkMatches.Any())
        {
            foreach (Match match in discordLinkMatches)
            {
                var fullUrl = new Uri(match.Value);
                var urlSplitOnDot = fullUrl.Host.Split('.');
                if (!urlSplitOnDot.Contains("discord"))
                {
                    continue;
                }

                var segmentsJoined = string.Join("", fullUrl.Segments).Split("/");
                if (!segmentsJoined.Contains("channels"))
                {
                    continue;
                }

                if (segmentsJoined.Length < 5)
                {
                    continue;
                }

                SocketGuild guild;
                var guildIdFromUrl = Convert.ToUInt64(segmentsJoined[2]);
                if (channel.Guild.Id != guildIdFromUrl)
                {
                    guild = _client.GetGuild(guildIdFromUrl);
                    if (guild is null)
                    {
                        continue;
                    }
                }
                else
                {
                    guild = channel.Guild;
                }
                if (guild != channel.Guild)
                {
                    continue;
                }

                var channelWithMessage = await ((IGuild)guild).GetTextChannelAsync(Convert.ToUInt64(segmentsJoined[3]))
                    .ConfigureAwait(false);
                if (channelWithMessage == null)
                {
                    continue;
                }

                var messageBeingLinked = await channelWithMessage.GetMessageAsync(Convert.ToUInt64(segmentsJoined[4])).ConfigureAwait(false);
                if (messageBeingLinked is null)
                {
                    continue;
                }

                var embedBuilder = _logMessageBuilder.CreateEmbedBuilder("Message Linked", $"[Original Message]({messageBeingLinked.GetJumpUrl()}) by {messageBeingLinked.Author.Mention}:", messageBeingLinked);
                await messageWithLink.Channel.SendMessageAsync(embed: embedBuilder.Build());
            }
        }
    }

    private async Task HandleReactions(IMessage message, GuildReplyDefinition guildReplyDefinition)
    {
        if (guildReplyDefinition.Reactions != null)
        {
            foreach (var triggerResponseReaction in guildReplyDefinition.Reactions)
            {
                try
                {
                    await message.AddReactionAsync(new Emoji(triggerResponseReaction));
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, $"Failed to add reaction {triggerResponseReaction}", ex);
                }
            }
        }
    }

    private async Task<bool> IsBotMentioned(SocketMessage message, IGuildChannel? channel)
    {
        var isBotMentioned = false;

        var botUserInGuild = (message.Author as SocketGuildUser)?.Guild.CurrentUser;
        var isDm = message.Channel is SocketDMChannel;

        if (botUserInGuild != null)
        {
            isBotMentioned = await _replyBusinessLayer.IsBotNameMentioned(message, channel?.Guild, botUserInGuild.Id);
        }
        else if (isDm)
        {
            isBotMentioned = true;
        }

        return isBotMentioned;
    }

    private async Task<bool> HandleDelete(IDeletable message, string reply)
    {
        var wasDeleted = false;
        if (!reply.Contains(_keywordHandler.BuildKeyword(TriggerKeyword.DeleteMessage)))
        {
            return wasDeleted;
        }
        try
        {
            await message.DeleteAsync(new RequestOptions
            {
                AuditLogReason = "Deleted by replybot."
            });
            wasDeleted = true;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, $"Failed to delete message: {ex.Message}", ex);
        }

        return wasDeleted;
    }

    private static string? ChooseReply(GuildReplyDefinition guildReplyDefinition, SocketUser author)
    {
        var replies = guildReplyDefinition.Replies;
        if (guildReplyDefinition.UserReplies != null && guildReplyDefinition.UserReplies.Any())
        {
            if (guildReplyDefinition.UserReplies.Any(pr => pr.UserId == author.Id))
            {
                replies = guildReplyDefinition.UserReplies.First(pr => pr.UserId == author.Id).Replies;
            }
        }

        if (replies == null || !replies.Any())
        {
            return null;
        }

        var random = new Random();
        var randomNumber = random.Next(replies.Length);
        return replies[randomNumber];
    }
}