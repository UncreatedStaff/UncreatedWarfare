using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Icons;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Components;

public class AmmoBagComponent : MonoBehaviour
{
    private IServiceProvider _serviceProvider;

    public BarricadeDrop Drop;
    //public Dictionary<ulong, int> ResuppliedPlayers;
    public int Ammo;

    public void Initialize(BarricadeDrop drop, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        Drop = drop;
        Ammo = 3;//todo FobManager.Config.AmmoBagMaxUses;

        ITeamManager<Team> teamManager = serviceProvider.GetRequiredService<ITeamManager<Team>>();

        AssetConfiguration assetConfig = serviceProvider.GetRequiredService<AssetConfiguration>();
        if (assetConfig.GetAssetLink<EffectAsset>("Effects:Ammo") is { } ammoAsset && ammoAsset.TryGetAsset(out _))
        {
            Team team = teamManager.GetTeam(new CSteamID(drop.GetServersideData().group));
            if (!team.IsValid)
                return;

            WorldIconManager iconManager = serviceProvider.GetRequiredService<WorldIconManager>();
            iconManager.CreateIcon(new WorldIconInfo(transform, ammoAsset, team) { TickSpeed = 1f });
        }

        //ResuppliedPlayers = new Dictionary<ulong, int>();
    }
    public async Task ResupplyPlayer(WarfarePlayer player, Kit kit, int ammoCost, CancellationToken token = default)
    {
        Ammo -= ammoCost;
        await _serviceProvider.GetRequiredService<KitManager>().Requests.ResupplyKit(player, kit, true, token).ConfigureAwait(false);
        await UniTask.SwitchToMainThread(token);

        WarfarePlayer? owner = _serviceProvider.GetRequiredService<IPlayerService>().GetOnlinePlayerOrNull(Drop.GetServersideData().owner);

        if (owner != null && owner.Steam64 != player.Steam64)
        {
            // todo Points.AwardXP(owner, XPReward.Resupply);
        }

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