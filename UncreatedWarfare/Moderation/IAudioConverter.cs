using System;
using System.Collections.Generic;
using System.IO;

namespace Uncreated.Warfare.Moderation;

/// <summary>
/// The implementation for this is private code given to me by Senior-S (https://github.com/Senior-S) which exists as a plugin.
/// </summary>
/// <remarks>He maintains a web API for https://github.com/Senior-S/SVD-Example-Use.</remarks>
public interface IAudioConverter
{
    /// <summary>
    /// If this audio converter has the info it needs to work.
    /// </summary>
    bool Enabled { get; }

    /// <summary>
    /// Convert voice packets to WAV and write it to <paramref name="output"/>.
    /// </summary>
    Task<AudioConvertResult> ConvertAsync(Stream output, bool leaveOpen, IEnumerable<ArraySegment<byte>> packets, float volume, CancellationToken token = default);
}

public enum AudioConvertResult
{
    Success,
    InvalidFormat,
    ConnectionError,
    Unauthorized,
    UnknownError,
    NoData,
    Disabled
}