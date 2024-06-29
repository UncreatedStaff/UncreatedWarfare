using SDG.Unturned;

namespace Uncreated.Warfare.Events.Players;
public sealed class PlayerAided : CancellablePlayerEvent
{
    public UCPlayer Medic { get; }
    public ulong MedicId { get; }
    public ItemConsumeableAsset AidItem { get; }
    public bool IsRevive { get; }
    public PlayerAided(UCPlayer medic, UCPlayer player, ItemConsumeableAsset asset, bool isRevive, bool shouldAllow) : base(player)
    {
        Medic = medic;
        MedicId = medic.Steam64;
        AidItem = asset;
        IsRevive = isRevive;

        if (!shouldAllow)
            Break();
    }
}
