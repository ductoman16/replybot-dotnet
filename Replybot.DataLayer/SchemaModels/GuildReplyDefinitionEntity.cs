﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Replybot.Models;

namespace Replybot.DataLayer.SchemaModels;

public class GuildReplyDefinitionEntity
{
    public GuildReplyDefinitionEntity(ulong guildId, string[] triggers, decimal priority, string[]? replies = null,
        bool mentionAuthor = false, bool requiresBotName = false, string[]? reactions = null)
    {
        Triggers = triggers;
        Priority = priority;
        GuildId = guildId;
        Replies = replies;
        MentionAuthor = mentionAuthor;
        RequiresBotName = requiresBotName;
        Reactions = reactions;
    }

    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    [BsonElement("guildId")]
    public ulong GuildId { get; set; }
    [BsonElement("triggers")]
    public string[] Triggers { get; set; }
    [BsonElement("replies")]
    public string[]? Replies { get; set; }
    [BsonElement("mentionAuthor")]
    public bool MentionAuthor { get; set; }
    [BsonElement("requiresBotName")]
    public bool RequiresBotName { get; set; }
    [BsonElement("reactions")]
    public string[]? Reactions { get; set; }
    [BsonElement("channelId")]
    public string? ChannelId { get; set; }
    [BsonElement("createdBy")]
    public string? CreatedBy { get; set; }
    [BsonElement("lastEditedBy")]
    public string? LastEditedBy { get; set; }
    [BsonElement("userReplies")]
    public UserReplyEntity[]? UserReplies { get; set; }
    [BsonElement("priority")]
    public decimal Priority { get; set; }

    public GuildReplyDefinition ToDomain()
    {
        var userReplies = UserReplies?.Select(p => p.ToDomain()).ToArray();
        return new GuildReplyDefinition(GuildId, Triggers, Replies, userReplies, MentionAuthor, RequiresBotName, Reactions, Priority)
        {
            Id = Id
        };
    }
}