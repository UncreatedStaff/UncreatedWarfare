using System.Runtime.CompilerServices;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Throwables;
public class ThrowableSpawned : PlayerEvent
{
    private readonly GameObject _throwable;
    private readonly ItemThrowableAsset _asset;
    public ItemThrowableAsset Asset => _asset;
    public GameObject Object => _throwable;

    [SetsRequiredMembers]
    public ThrowableSpawned(WarfarePlayer player, ItemThrowableAsset asset, GameObject @object)
    {
        Player = player;
        _throwable = @object;
        _asset = asset;
    }
}
