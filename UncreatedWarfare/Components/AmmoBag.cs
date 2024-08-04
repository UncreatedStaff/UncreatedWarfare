using System;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Kits;

namespace Uncreated.Warfare.Components;

public class AmmoBagComponent : MonoBehaviour
{
    public BarricadeDrop Drop;
    //public Dictionary<ulong, int> ResuppliedPlayers;
    public int Ammo;
    public void Initialize(BarricadeDrop drop)
    {
        Drop = drop;
        Ammo = FOBManager.Config.AmmoBagMaxUses;
        
        if (Gamemode.Config.EffectMarkerAmmo.TryGetGuid(out Guid guid))
        {
            IconManager.AttachIcon(guid, drop.model, drop.GetServersideData().group.GetTeam(), 1f);
        }

        //ResuppliedPlayers = new Dictionary<ulong, int>();
    }
    public async Task ResupplyPlayer(UCPlayer player, Kit kit, int ammoCost, CancellationToken token = default)
    {
        Ammo -= ammoCost;
        if (Data.Is(out IKitRequests req))
        {
            await req.KitManager.Requests.ResupplyKit(player, kit, true, token).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(token);
        }
        else
        {
            L.LogWarning("Failed to resupply " + player + ", KitManager is not loaded.");
            return;
        }

        UCPlayer? owner = UCPlayer.FromID(Drop.GetServersideData().owner);
        if (owner != null && owner.Steam64 != player.Steam64)
            Points.AwardXP(owner, XPReward.Resupply);

        if (Ammo <= 0 && Regions.tryGetCoordinate(Drop.model.position, out byte x, out byte y))
        {
            Destroy(this);
            BarricadeManager.destroyBarricade(Drop, x, y, ushort.MaxValue);
            return;
        }
        /*

        if (ResuppliedPlayers.ContainsKey(player.Steam64))
            ResuppliedPlayers[player.Steam64] = player.LifeCounter;
        else
            ResuppliedPlayers.Add(player.Steam64, player.LifeCounter);*/

    }
}