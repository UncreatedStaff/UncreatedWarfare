using System;
using System.Text.Json.Serialization;
using Uncreated.Encoding;
using Uncreated.SQL;

namespace Uncreated.Warfare.Moderation;
public readonly struct Evidence
{
    [JsonPropertyName("id")]
    public PrimaryKey Id { get; }

    [JsonPropertyName("url")]
    public string URL { get; }

    [JsonPropertyName("saved_location")]
    public string? SavedLocation { get; }

    [JsonPropertyName("message")]
    public string? Message { get; }

    [JsonPropertyName("image")]
    public bool Image { get; }

    [JsonPropertyName("actor")]
    [JsonConverter(typeof(ActorConverter))]
    public IModerationActor Actor { get; }

    [JsonPropertyName("timestamp_utc")]
    public DateTimeOffset Timestamp { get; }

    public Evidence(string url, string? message, string? savedLocation, bool image, IModerationActor actor, DateTimeOffset timestamp)
        : this(PrimaryKey.NotAssigned, url, message, savedLocation, image, actor, timestamp) { }

    [JsonConstructor]
    public Evidence(PrimaryKey id, string url, string? message, string? savedLocation, bool image, IModerationActor actor, DateTimeOffset timestamp)
    {
        Id = id;
        URL = url;
        Message = message;
        SavedLocation = savedLocation;
        Image = image;
        Actor = actor;
        Timestamp = timestamp;
    }
    public Evidence(ByteReader reader, ushort version)
    {
        Id = reader.ReadInt32();
        URL = reader.ReadString();
        SavedLocation = reader.ReadNullableString();
        Message = reader.ReadNullableString();
        Image = reader.ReadBool();
        Actor = Actors.GetActor(reader.ReadUInt64());
        Timestamp = reader.ReadDateTimeOffset();
    }
    public void Write(ByteWriter writer)
    {
        writer.Write(Id.Key);
        writer.Write(URL);
        writer.WriteNullable(SavedLocation);
        writer.WriteNullable(Message);
        writer.Write(Image);
        writer.Write(Actor.Id);
        writer.Write(Timestamp);
    }
}
