using System.Collections.Generic;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Components;

#pragma warning disable IDE0051
public class ThrowableComponent : MonoBehaviour
{
    /// <summary>
    /// The player that threw this throwable.
    /// </summary>
    public WarfarePlayer? Owner { get; internal set; }

    /// <summary>
    /// The asset of this throwable.
    /// </summary>
    public ItemThrowableAsset? Throwable { get; internal set; }

    /// <summary>
    /// The team of the player that threw the throwable.
    /// </summary>
    public Team? Team { get; internal set; }

    internal List<ThrowableComponent>? ToRemoveFrom;

    [UsedImplicitly]
    private void OnDestroy()
    {
        ToRemoveFrom?.Remove(this);
    }
}

#pragma warning restore IDE0051