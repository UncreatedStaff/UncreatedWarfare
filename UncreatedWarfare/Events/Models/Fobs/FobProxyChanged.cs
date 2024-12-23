using Uncreated.Warfare.Fobs;

namespace Uncreated.Warfare.Events.Models.Fobs;

/// <summary>
/// Event listener args which fires after <see cref="ResourceFob"/> becomes proxied by enemies or spawnable again.
/// </summary>
public class FobProxyChanged
{
    /// <summary>
    /// The <see cref="ResourceFob"/> that was proxied or unproxied by enemies.
    /// </summary>
    public required ResourceFob Fob { get; init; }
    /// <summary>
    /// The new proxy state of the Fob.
    /// </summary>
    public required bool IsProxied { get; init; }
}
