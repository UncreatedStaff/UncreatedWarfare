using System.Runtime.CompilerServices;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Throwables;
public class ThrowableSpawned : PlayerEvent
{
    public required UseableThrowable UseableThrowable { get; init; }
    public required GameObject Object { get; init; }
    public ItemThrowableAsset Asset => UseableThrowable.equippedThrowableAsset;
}
