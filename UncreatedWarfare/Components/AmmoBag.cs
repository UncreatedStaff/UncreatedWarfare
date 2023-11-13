using SDG.Unturned;
using System;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Models.Kits;
using UnityEngine;

namespace Uncreated.Warfare.FOBs;

public class AmmoBagComponent : MonoBehaviour
{
    public BarricadeDrop Drop;
    //public Dictionary<ulong, int> ResuppliedPlayers;
    public int Ammo;
    public void Initialize(BarricadeDrop drop)
    {
        Drop = drop;
        Ammo = FOBManager.Config.AmmoBagMaxUses;
        
        if (Gamemode.Config.EffectMarkerAmmo.ValidReference(out Guid guid))
        {
            IconManager.AttachIcon(guid, drop.model, drop.GetServersideData().group.GetTeam(), 1f);
        }
        //ResuppliedPlayers = new Dictionary<ulong, int>();
    }
    public async Task ResupplyPlayer(UCPlayer player, Kit kit, int ammoCost, CancellationToken token = default)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Ammo -= ammoCost;
        if (Data.Is(out IKitRequests req))
        {
            await req.KitManager.ResupplyKit(player, kit, true, token).ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
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