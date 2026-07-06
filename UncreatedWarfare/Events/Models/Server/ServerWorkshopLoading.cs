namespace Uncreated.Warfare.Events.Models.Server;

/// <summary>
/// Invoked when the server's workshop starts loading.
/// Allows adding or removing mods and maps before loading.
/// </summary>
public class ServerWorkshopLoading
{
    /// <summary>
    /// List of workshop items to download.
    /// </summary>
    public required ICollection<PublishedFileId_t> Items { get; init; }

    /// <summary>
    /// List of child mods to ignore from mods downloaded from <see cref="Items"/> or children of <see cref="Items"/>.
    /// </summary>
    public required ICollection<PublishedFileId_t> IgnoredChildren { get; init; }
}