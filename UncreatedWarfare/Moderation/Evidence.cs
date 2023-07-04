using System;
using Uncreated.SQL;

namespace Uncreated.Warfare.Moderation;
public readonly struct Evidence
{
    public PrimaryKey Id { get; }
    public string URL { get; }
    public string? SavedLocation { get; }
    public string? Message { get; }
    public bool IsImage { get; }
    public IModerationActor Actor { get; }
    public DateTimeOffset Timestamp { get; }
    public Evidence(PrimaryKey id, string url, string? message, string? savedLocation, bool isImage, IModerationActor actor, DateTimeOffset timestamp)
    {
        Id = id;
        URL = url;
        Message = message;
        SavedLocation = savedLocation;
        IsImage = isImage;
        Actor = actor;
        Timestamp = timestamp;
    }
}
