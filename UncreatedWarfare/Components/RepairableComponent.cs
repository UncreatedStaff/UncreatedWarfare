using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Events.Barricades;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
using UnityEngine;

namespace Uncreated.Warfare.Components;

internal class RepairableComponent : MonoBehaviour
{
    public BarricadeDrop Structure { get; private set; }

    private Dictionary<ulong, float> PlayerHits;

    public void Awake()
    {
        Structure = BarricadeManager.FindBarricadeByRootTransform(transform);
        PlayerHits = new Dictionary<ulong, float>();
    }

    public void Repair(UCPlayer builder)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Structure.GetServersideData().barricade.health >= Structure.asset.health)
            return;

        float amount = 30;

        if (builder.KitClass == Class.CombatEngineer)
            amount *= 2;

        BarricadeManager.repair(transform, amount, 1, builder.CSteamID);
        if (Gamemode.Config.EffectDig.ValidReference(out EffectAsset effect))
            F.TriggerEffectReliable(effect, EffectManager.MEDIUM, builder.Position);

        if (PlayerHits.ContainsKey(builder.Steam64))
            PlayerHits[builder.Steam64] += amount;
        else
            PlayerHits.Add(builder.Steam64, amount);

        if (Structure.GetServersideData().barricade.health >= Structure.asset.health)
        {
            // give XP?
        }
    }
    public void Destroy(BarricadeDestroyed e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        BuildableData buildable = FOBManager.Config.Buildables.Find(b => b.BuildableBarricade.MatchGuid(Structure.asset.GUID) && b.Type != EBuildableType.EMPLACEMENT);

        if (buildable != null && buildable.Foundation.ValidReference(out ItemBarricadeAsset asset))
        {
            string structureName = asset.itemName;
            string message = structureName + " DESTROYED";

            UCPlayer? player = e.Instigator;
            if (player != null)
            {
                ulong bteam = Structure.GetServersideData().group.GetTeam();
                bool teamkilled = bteam != 0 && bteam == player.GetTeam();

                int amount = 0;
                float vehicleQuota = 0;
                if (buildable.Type == EBuildableType.FOB_BUNKER)
                {
                    if (teamkilled)
                    {
                        amount = Points.XPConfig.FOBTeamkilledXP;
                    }
                    else
                    {
                        amount = Points.XPConfig.FOBKilledXP;
                        vehicleQuota = 5;
                    }
                }
                if (buildable.Type == EBuildableType.FORTIFICATION)
                {
                    amount = (int)Math.Round(buildable.RequiredHits * 0.1F);


                    if (teamkilled) amount *= -1;
                    else vehicleQuota = 0.1F;
                }
                else
                {
                    amount = (int)Math.Round(buildable.RequiredHits * 0.75F);
                    if (teamkilled) amount *= -1;
                    else vehicleQuota = amount * 0.02F;
                }

                if (teamkilled)
                {
                    message = "FRIENDLY " + message;
                }

                if (amount != 0)
                {
                    Points.AwardXP(player, amount, message.ToUpper());
                    Points.TryAwardDriverAssist(player.Player, amount, vehicleQuota);
                }
            }
        }

        Destroy(this);
    }
}