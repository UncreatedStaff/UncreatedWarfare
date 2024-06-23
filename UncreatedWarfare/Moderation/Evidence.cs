using DanielWillett.SpeedBytes;
using System;
using System.Text.Json.Serialization;

namespace Uncreated.Warfare.Moderation;
public struct Evidence : IEquatable<Evidence>
{
    [JsonPropertyName("id")]
    public uint Id { get; set; }

    [JsonPropertyName("url")]
    public string URL { get; set; }

    [JsonPropertyName("saved_location")]
    public string? SavedLocation { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("image")]
    public bool Image { get; set; }

    [JsonPropertyName("actor")]
    [JsonConverter(typeof(ActorConverter))]
    public IModerationActor Actor { get; set; }

    [JsonPropertyName("timestamp_utc")]
    public DateTimeOffset Timestamp { get; set; }

    public Evidence() { }
    public Evidence(string url, string? message, string? savedLocation, bool image, IModerationActor actor, DateTimeOffset timestamp)
        : this(0u, url, message, savedLocation, image, actor, timestamp) { }
    
    public Evidence(uint id, string url, string? message, string? savedLocation, bool image, IModerationActor actor, DateTimeOffset timestamp)
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
        Id = reader.ReadUInt32();
        URL = reader.ReadString();
        SavedLocation = reader.ReadNullableString();
        Message = reader.ReadNullableString();
        Image = reader.ReadBool();
        Actor = Actors.GetActor(reader.ReadUInt64());
        Timestamp = reader.ReadDateTimeOffset();
    }
    public void Write(ByteWriter writer)
    {
        writer.Write(Id);
        writer.Write(URL);
        writer.WriteNullable(SavedLocation);
        writer.WriteNullable(Message);
        writer.Write(Image);
        writer.Write(Actor.Id);
        writer.Write(Timestamp);
    }

    public bool Equals(Evidence other)
    {
        return Id == other.Id && string.Equals(URL, other.URL, StringComparison.Ordinal) &&
               string.Equals(SavedLocation, other.SavedLocation, StringComparison.Ordinal) &&
               string.Equals(Message, other.Message, StringComparison.Ordinal) &&
               Image == other.Image &&
               (Actor == null && other.Actor == null || Actor != null && other.Actor != null && Actor.Id == other.Actor.Id) &&
               Timestamp == other.Timestamp;
    }

    public override bool Equals(object? obj)
    {
        return obj is Evidence other && Equals(other);
    }

    public override int GetHashCode() => HashCode.Combine(Id, URL, SavedLocation, Message, Image, Actor, Timestamp);
    public static bool operator ==(Evidence left, Evidence right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Evidence left, Evidence right)
    {
        return !left.Equals(right);
    }
}
