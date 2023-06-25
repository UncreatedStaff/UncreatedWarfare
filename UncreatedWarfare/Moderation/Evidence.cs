using System;

namespace Uncreated.Warfare.Moderation;
public readonly struct Evidence
{
    public string URL { get; }
    public string? Message { get; }
    public bool IsImage { get; }
    public IModerationActor Actor { get; }
    public DateTimeOffset Timestamp { get; }
    public Evidence(string url, string? message, bool isImage, IModerationActor actor, DateTimeOffset timestamp)
    {
        URL = url;
        Message = message;
        IsImage = isImage;
        Actor = actor;
        Timestamp = timestamp;
    }
}
