using System;
using System.Text.Json.Serialization;
using Uncreated.Encoding;
using Uncreated.SQL;

namespace Uncreated.Warfare.Moderation;
public struct Evidence : IEquatable<Evidence>
{
    [JsonPropertyName("id")]
    public PrimaryKey Id { get; set; }

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
        : this(PrimaryKey.NotAssigned, url, message, savedLocation, image, actor, timestamp) { }
    
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
        writer.Write(Id.Key);
        writer.Write(URL);
        writer.WriteNullable(SavedLocation);
        writer.WriteNullable(Message);
        writer.Write(Image);
        writer.Write(Actor.Id);
        writer.Write(Timestamp);
    }

    public bool Equals(Evidence other)
    {
        return Id.Key == other.Id.Key && string.Equals(URL, other.URL, StringComparison.Ordinal) &&
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

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = Id.GetHashCode();
            hashCode = (hashCode * 397) ^ URL.GetHashCode();
            hashCode = (hashCode * 397) ^ (SavedLocation != null ? SavedLocation.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Message != null ? Message.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ Image.GetHashCode();
            hashCode = (hashCode * 397) ^ Actor.GetHashCode();
            hashCode = (hashCode * 397) ^ Timestamp.GetHashCode();
            return hashCode;
        }
    }

    public static bool operator ==(Evidence left, Evidence right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Evidence left, Evidence right)
    {
        return !left.Equals(right);
    }
}
