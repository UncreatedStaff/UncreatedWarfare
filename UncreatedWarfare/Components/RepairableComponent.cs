using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Events.Barricades;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Levels;
using UnityEngine;

namespace Uncreated.Warfare.Components;

internal class RepairableComponent : MonoBehaviour, IShovelable
{
    public BarricadeDrop Structure { get; private set; }
    public TickResponsibilityCollection Builders { get; } = new TickResponsibilityCollection();

    public void Awake()
    {
        Structure = BarricadeManager.FindBarricadeByRootTransform(transform);
    }

    public bool Shovel(UCPlayer shoveler)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Structure.GetServersideData().barricade.health >= Structure.asset.health)
            return false;

        float amount = FOBManager.GetBuildIncrementMultiplier(shoveler);

        BarricadeManager.repair(transform, amount, 1, shoveler.CSteamID);
        FOBManager.TriggerBuildEffect(shoveler.Position);

        Builders.Increment(shoveler.Steam64, amount);
        return true;
    }
    public void Destroy(BarricadeDestroyed e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        BuildableData buildable = FOBManager.Config.Buildables.Find(b => b.FullBuildable.MatchGuid(Structure.asset.GUID) && b.Type != BuildableType.Emplacement);

        if (buildable != null && buildable.Foundation.ValidReference(out ItemBarricadeAsset asset))
        {
            string structureName = asset.itemName;
            // todo translation
            string message = structureName + " DESTROYED";

            UCPlayer? player = e.Instigator;
            if (player != null)
            {
                ulong bteam = Structure.GetServersideData().group.GetTeam();
                bool teamkilled = bteam != 0 && bteam == player.GetTeam();

                if (buildable.Type == BuildableType.Bunker)
                {
                    if (teamkilled)
                    {
                        // TODO: find out why random barricade teamkills are still happening, if they are at all
                        //Points.AwardXP(player, XPReward.FriendlyBunkerDestroyed);
                    }
                    else
                    {
                        Points.AwardXP(player, XPReward.BunkerDestroyed);
                        Points.TryAwardDriverAssist(player.Player, XPReward.BunkerDestroyed, quota: 5);
                    }
                }
                else if (buildable.Type == BuildableType.Fortification)
                {
                    int amount = Mathf.RoundToInt(buildable.RequiredHits * 0.1f);


                    if (teamkilled)
                    {
                        // TODO: find out why random barricade teamkills are still happening, if they are at all
                        //Points.AwardXP(player, XPReward.FriendlyBuildableDestroyed, "FRIENDLY " + message, -amount);
                    }
                    else
                    {
                        Points.AwardXP(player, XPReward.FortificationDestroyed, message, amount);
                        Points.TryAwardDriverAssist(player.Player, XPReward.FortificationDestroyed, quota: 0.1f);
                    }
                }
                else
                {
                    int amount = Mathf.RoundToInt(buildable.RequiredHits * 0.75f);
                    if (teamkilled)
                    {
                        Points.AwardXP(player, XPReward.FriendlyBuildableDestroyed, "FRIENDLY " + message, -amount);
                    }
                    else
                    {
                        Points.AwardXP(player, XPReward.BuildableDestroyed, message, amount);
                        Points.TryAwardDriverAssist(player.Player, XPReward.BuildableDestroyed, quota: amount * 0.02f);
                    }
                }
            }
        }

        Destroy(this);
    }
}