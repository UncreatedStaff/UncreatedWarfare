using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Vehicles;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;
using VehicleSpawn = Uncreated.Warfare.Vehicles.VehicleSpawn;

namespace Uncreated.Warfare.Commands;
public class RequestCommand : AsyncCommand
{
    private const string SYNTAX = "/request [save|remove]";
    private const string HELP = "Request a kit by targeting a sign or request a vehicle by targeting the vehicle or it's sign while doing /request.";

    public RequestCommand() : base("request", EAdminType.MEMBER)
    {
        AddAlias("req");
    }

    public override async Task Execute(CommandInteraction ctx, CancellationToken token)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertRanByPlayer();

        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        ulong team = ctx.Caller!.GetTeam();
        if (ctx.HasArg(0))
        {
            if (ctx.MatchParameter(0, "save"))
            {
                ctx.AssertGamemode<IKitRequests>();
                ctx.AssertPermissions(EAdminType.STAFF);

                if (ctx.TryGetTarget(out BarricadeDrop drop) && drop.interactable is InteractableSign sign)
                {
                    if (RequestSigns.AddRequestSign(sign, out RequestSign signadded))
                    {
                        if (KitManager.KitExists(signadded.KitName, out KitOld kit))
                            ctx.Reply(T.RequestSignSaved, kit);
                        else
                            ctx.SendUnknownError();
                        ctx.LogAction(EActionLogType.SAVE_REQUEST_SIGN, signadded.KitName);
                    }
                    else throw ctx.Reply(T.RequestSignAlreadySaved); // sign already registered
                }
                else throw ctx.Reply(T.RequestNoTarget);
            }
            else if (ctx.MatchParameter(0, "remove", "delete"))
            {
                ctx.AssertGamemode<IKitRequests>();
                ctx.AssertPermissions(EAdminType.STAFF);

                if (ctx.TryGetTarget(out BarricadeDrop drop) && drop.interactable is InteractableSign sign)
                {
                    if (RequestSigns.SignExists(sign, out RequestSign requestsign))
                    {
                        if (KitManager.KitExists(requestsign.KitName, out KitOld kit))
                            ctx.Reply(T.RequestSignRemoved, kit);
                        else
                            ctx.SendUnknownError();
                        RequestSigns.RemoveRequestSign(requestsign);
                        ctx.LogAction(EActionLogType.UNSAVE_REQUEST_SIGN, requestsign.KitName);
                    }
                    else throw ctx.Reply(T.RequestSignNotSaved);
                }
                else throw ctx.Reply(T.RequestNoTarget);
            }
            else throw ctx.SendCorrectUsage(SYNTAX + " - " + HELP);
        }
        else
        {
            if (ctx.TryGetTarget(out BarricadeDrop drop) && drop.interactable is InteractableSign sign)
            {
                if (RequestSigns.Loaded && RequestSigns.SignExists(sign, out RequestSign kitsign))
                {
                    ctx.AssertGamemode<IKitRequests>();

                    UCPlayer caller2 = ctx.Caller!;
                    if (kitsign.KitName.StartsWith("loadout_", StringComparison.OrdinalIgnoreCase))
                    {
                        if (byte.TryParse(kitsign.KitName.Substring(8), NumberStyles.Number, Data.Locale, out byte loadoutId))
                        {
                            byte bteam = ctx.Caller!.Player.GetTeamByte();
                            List<KitOld> loadouts = KitManager.GetKitsWhere(k => k.IsLoadout && k.Team == team && KitManager.HasAccessFast(k, caller2));
                            if (loadoutId > 0 && loadoutId <= loadouts.Count)
                            {
                                KitOld loadout = loadouts[loadoutId - 1];

                                if (loadout.IsClassLimited(out int currentPlayers, out int allowedPlayers, bteam))
                                {
                                    ctx.Reply(T.RequestKitLimited, allowedPlayers);
                                    return;
                                }

                                ctx.LogAction(EActionLogType.REQUEST_KIT, $"Loadout #{loadoutId}: {loadout.Name}, Team {loadout.Team}, Class: {Localization.TranslateEnum(loadout.Class, 0)}");
                                GiveKit(caller2, loadout);
                                ctx.Defer();
                            }
                            else throw ctx.Reply(T.RequestLoadoutNotOwned);
                        }
                        else throw ctx.Reply(T.RequestLoadoutNotOwned);
                    }
                    else
                    {
                        if (!KitManager.KitExists(kitsign.KitName, out KitOld kit) || kit.IsLoadout)
                            throw ctx.Reply(T.KitNotFound, kitsign.KitName);
                        if (caller2.KitName == kit.Name)
                            throw ctx.Reply(T.RequestKitAlreadyOwned);
                        if (kit.IsPremium && !KitManager.HasAccessFast(kit, caller2) && !UCWarfare.Config.OverrideKitRequirements)
                            throw ctx.Reply(T.RequestKitMissingAccess);
                        if (kit.Team != 0 && kit.Team != team)
                            throw ctx.Reply(T.RequestKitWrongTeam, TeamManager.GetFactionSafe(team)!);
                        if (caller2.Rank.Level < kit.UnlockLevel)
                            throw ctx.Reply(T.RequestKitLowLevel, RankData.GetRankName(kit.UnlockLevel));
                        if (!kit.IsPremium && kit.CreditCost > 0 && !KitManager.HasAccessFast(kit, caller2) && !UCWarfare.Config.OverrideKitRequirements)
                        {
                            if (caller2.CachedCredits >= kit.CreditCost)
                                throw ctx.Reply(T.RequestKitNotBought, kit.CreditCost);
                            else
                                throw ctx.Reply(T.RequestKitCantAfford, kit.CreditCost - caller2.CachedCredits, kit.CreditCost);
                        }
                        if (kit.IsLimited(out _, out int allowedPlayers, caller2.GetTeam()))
                            throw ctx.Reply(T.RequestKitLimited, allowedPlayers);
                        if (kit.Class == Class.Squadleader && caller2.Squad is not null && !caller2.IsSquadLeader())
                            throw ctx.Reply(T.RequestKitNotSquadleader);
                        if (
                            Data.Gamemode.State == EState.ACTIVE &&
                            CooldownManager.HasCooldown(caller2, ECooldownType.REQUEST_KIT, out Cooldown requestCooldown) &&
                            !caller2.OnDutyOrAdmin() &&
                            !UCWarfare.Config.OverrideKitRequirements &&
                            !(kit.Class == Class.Crewman || kit.Class == Class.Pilot))
                            throw ctx.Reply(T.KitOnGlobalCooldown, requestCooldown);
                        if (kit.IsPremium &&
                            CooldownManager.HasCooldown(caller2, ECooldownType.PREMIUM_KIT, out Cooldown premiumCooldown, kit.Name) &&
                            !caller2.OnDutyOrAdmin() &&
                            !UCWarfare.Config.OverrideKitRequirements)
                            throw ctx.Reply(T.KitOnCooldown, premiumCooldown);

                        for (int i = 0; i < kit.UnlockRequirements.Length; i++)
                        {
                            UnlockRequirement req = kit.UnlockRequirements[i];
                            if (req.CanAccess(caller2))
                                continue;
                            if (req is LevelUnlockRequirement level)
                                throw ctx.Reply(T.RequestKitLowLevel, level.UnlockLevel.ToString(Data.Locale));
                            if (req is RankUnlockRequirement rank)
                            {
                                Ranks.RankData data = Ranks.RankManager.GetRank(rank.UnlockRank);
                                if (data.Order == -1)
                                    L.LogWarning("Invalid rank order in kit requirement: " + kit.Name + " :: " + rank.UnlockRank + ".");
                                throw ctx.Reply(T.RequestKitLowRank, data);
                            }
                            if (req is QuestUnlockRequirement quest)
                            {
                                if (Assets.find(quest.QuestID) is QuestAsset asset)
                                {
                                    ctx.Caller.Player.quests.sendAddQuest(asset.id);
                                    throw ctx.Reply(T.RequestKitQuestIncomplete, asset);
                                }
                                throw ctx.Reply(T.RequestKitQuestIncomplete, null!);
                            }
                            L.LogWarning("Unhandled kit requirement type: " + req.GetType().Name);
                            throw ctx.SendUnknownError();
                        }
                        
                        bool hasKit = kit.CreditCost == 0 && !kit.IsPremium;
                        if (!hasKit)
                        {
                            hasKit = await KitManager.HasAccess(kit, caller2.Steam64).ConfigureAwait(false);
                            await UCWarfare.ToUpdate();
                            if (!hasKit)
                            {
                                if (kit.IsPremium)
                                    ctx.Reply(T.RequestKitMissingAccess);
                                else if (caller2.CachedCredits >= kit.CreditCost)
                                    ctx.Reply(T.RequestKitNotBought, kit.CreditCost);
                                else
                                    ctx.Reply(T.RequestKitCantAfford, kit.CreditCost - caller2.CachedCredits, kit.CreditCost);
                                return;
                            }
                        }
                        // recheck limits to make sure people can't request at the same time to avoid limits.
                        if (kit.IsLimited(out _, out allowedPlayers, caller2.GetTeam()))
                        {
                            ctx.Reply(T.RequestKitLimited, allowedPlayers);
                            return;
                        }
                        if (kit.Class == Class.Squadleader && caller2.Squad is not null && !caller2.IsSquadLeader())
                        {
                            ctx.Reply(T.RequestKitNotSquadleader);
                            return;
                        }
                        if (kit.Class == Class.Squadleader && caller2.Squad == null)
                        {
                            if (SquadManager.Squads.Count(x => x.Team == team) < 8)
                            {
                                // create a squad automatically if someone requests a squad leader kit.
                                Squad squad = SquadManager.CreateSquad(caller2, caller2.GetTeam());
                                ctx.Reply(T.SquadCreated, squad);
                            }
                            else
                            {
                                ctx.Reply(T.SquadsTooMany, SquadManager.ListUI.Squads.Length);
                                return;
                            }
                        }
                        ctx.LogAction(EActionLogType.REQUEST_KIT, $"Kit {kit.Name}, Team {kit.Team}, Class: {Localization.TranslateEnum(kit.Class, 0)}");
                        GiveKit(caller2, kit);
                        ctx.Defer();
                    }
                }
                else if (VehicleSigns.Loaded && VehicleSigns.SignExists(sign, out VehicleSign vbsign))
                {
                    ctx.AssertGamemode<IVehicles>();

                    if (vbsign.VehicleBay.HasLinkedVehicle(out InteractableVehicle vehicle))
                    {
                        VehicleBay? bay = Data.Singletons.GetSingleton<VehicleBay>();
                        if (bay != null && bay.IsLoaded)
                        {
                            SqlItem<VehicleData>? data = await bay.GetDataProxy(vehicle.asset.GUID, token).ConfigureAwait(false);
                            await UCWarfare.ToUpdate();
                            if (data?.Item != null)
                            {
                                await data.Enter(token).ConfigureAwait(false);
                                try
                                {
                                    await RequestVehicle(ctx.Caller!, vehicle, data.Item, token);
                                    ctx.Defer();
                                }
                                finally
                                {
                                    data.Release();
                                }
                                return;
                            }
                        }
                        else throw ctx.SendGamemodeError();
                    }
                    throw ctx.Reply(T.RequestNoTarget);
                }
                else if (TraitManager.Loaded && sign.text.StartsWith(TraitSigns.TRAIT_SIGN_PREFIX, StringComparison.OrdinalIgnoreCase))
                {
                    ctx.AssertGamemode<ITraits>();

                    if (!TraitManager.Loaded)
                        throw ctx.SendGamemodeError();

                    TraitData? d = TraitManager.GetData(sign.text.Substring(TraitSigns.TRAIT_SIGN_PREFIX.Length));
                    if (d == null)
                        throw ctx.Reply(T.RequestNoTarget);

                    TraitManager.RequestTrait(d, ctx);
                }
                else throw ctx.Reply(T.RequestNoTarget);
            }
            else if (ctx.TryGetTarget(out InteractableVehicle vehicle))
            {
                VehicleBay? bay = Data.Singletons.GetSingleton<VehicleBay>();
                if (bay != null && bay.IsLoaded)
                {
                    SqlItem<VehicleData>? data = await bay.GetDataProxy(vehicle.asset.GUID, token).ConfigureAwait(false);
                    await UCWarfare.ToUpdate();
                    if (data?.Item != null)
                    {
                        await data.Enter(token).ConfigureAwait(false);
                        try
                        {
                            await RequestVehicle(ctx.Caller, vehicle, data.Item, token).ConfigureAwait(false);
                            ctx.Defer();
                        }
                        finally
                        {
                            data.Release();
                        }
                    }
                    else throw ctx.Reply(T.RequestNoTarget);
                }
                else throw ctx.SendGamemodeError();
            }
            else throw ctx.Reply(T.RequestNoTarget);
        }
    }
    private void GiveKit(UCPlayer ucplayer, KitOld kit)
    {
        AmmoCommand.WipeDroppedItems(ucplayer.Steam64);
        KitManager.GiveKit(ucplayer, kit);
        Stats.StatsManager.ModifyKit(kit.Name, k => k.TimesRequested++);
        Stats.StatsManager.ModifyStats(ucplayer.Steam64, s =>
        {
            Stats.WarfareStats.KitData kitData = s.Kits.Find(k => k.KitID == kit.Name && k.Team == kit.Team);
            if (kitData == default)
            {
                kitData = new Stats.WarfareStats.KitData() { KitID = kit.Name, Team = (byte)kit.Team, TimesRequested = 1 };
                s.Kits.Add(kitData);
            }
            else
                kitData.TimesRequested++;
        }, false);

        ucplayer.SendChat(T.RequestSignGiven, kit.Class);

        if (kit.IsPremium && kit.Cooldown > 0)
            CooldownManager.StartCooldown(ucplayer, ECooldownType.PREMIUM_KIT, kit.Cooldown, kit.Name);
        CooldownManager.StartCooldown(ucplayer, ECooldownType.REQUEST_KIT, CooldownManager.Config.RequestKitCooldown);

        //PlayerManager.ApplyTo(ucplayer);
    }
    /// <remarks>Thread Safe</remarks>
    internal Task RequestVehicle(UCPlayer ucplayer, InteractableVehicle vehicle, VehicleData data, CancellationToken token = default) => RequestVehicle(ucplayer, vehicle, data, ucplayer.GetTeam(), token);
    /// <remarks>Thread Safe</remarks>
    internal async Task RequestVehicle(UCPlayer ucplayer, InteractableVehicle vehicle, VehicleData data, ulong team, CancellationToken token = default)
    {
        if (!UCWarfare.IsMainThread)
            await UCWarfare.ToUpdate();
        if (vehicle.lockedOwner != CSteamID.Nil || vehicle.lockedGroup != CSteamID.Nil)
        {
            ucplayer.SendChat(T.RequestVehicleAlreadyRequested, await F.GetPlayerOriginalNamesAsync(vehicle.lockedOwner.m_SteamID, token).ThenToUpdate(token));
            return;
        }
        if (data.Team != 0 && data.Team != team)
        {
            ucplayer.SendChat(T.RequestVehicleOtherTeam, TeamManager.GetFactionSafe(data.Team)!);
            return;
        }
        if (data.RequiresSL && ucplayer.Squad == null)
        {
            ucplayer.SendChat(T.RequestVehicleNotSquadLeader);
            return;
        }
        if (!KitManager.HasKit(ucplayer.CSteamID, out KitOld kit))
        {
            ucplayer.SendChat(T.RequestVehicleNoKit);
            return;
        }
        if (data.RequiredClass != Class.None && kit.Class != data.RequiredClass)
        {
            ucplayer.SendChat(T.RequestVehicleWrongClass, data.RequiredClass);
            return;
        }
        if (ucplayer.Rank.Level < data.UnlockLevel)
        {
            ucplayer.SendChat(T.RequestVehicleMissingLevels, RankData.GetRankName(data.UnlockLevel));
            return;
        }
        if (ucplayer.CachedCredits < data.CreditCost)
        {
            ucplayer.SendChat(T.RequestVehicleCantAfford, data.CreditCost - ucplayer.CachedCredits, data.CreditCost);
            return;
        }
        if (CooldownManager.HasCooldown(ucplayer, ECooldownType.REQUEST_VEHICLE, out Cooldown cooldown, vehicle.id))
        {
            ucplayer.SendChat(T.RequestVehicleCooldown, cooldown);
            return;
        }

        if (VehicleSpawner.Loaded) // check if an owned vehicle is nearby
        {
            foreach (VehicleSpawn spawn in VehicleSpawner.Spawners)
            {
                if (spawn is not null && spawn.HasLinkedVehicle(out InteractableVehicle veh))
                {
                    if (veh == null || veh.isDead) continue;
                    if (veh.lockedOwner.m_SteamID == ucplayer.Steam64 &&
                        (veh.transform.position - vehicle.transform.position).sqrMagnitude < UCWarfare.Config.MaxVehicleAbandonmentDistance * UCWarfare.Config.MaxVehicleAbandonmentDistance)
                    {
                        ucplayer.SendChat(T.RequestVehicleAlreadyOwned, data);
                        return;
                    }
                }
            }
        }
        if (data.IsDelayed(out Delay delay) && delay.Type != DelayType.None)
        {
            Localization.SendDelayRequestText(in delay, ucplayer, team, Localization.EDelayMode.VEHICLE_BAYS);
            return;
        }

        for (int i = 0; i < data.UnlockRequirements.Length; i++)
        {
            UnlockRequirement req = data.UnlockRequirements[i];
            if (req.CanAccess(ucplayer))
                continue;
            if (req is LevelUnlockRequirement level)
            {
                ucplayer.SendChat(T.RequestVehicleMissingLevels, RankData.GetRankName(level.UnlockLevel));
            }
            else if (req is RankUnlockRequirement rank)
            {
                Ranks.RankData rankData = Ranks.RankManager.GetRank(rank.UnlockRank);
                if (rankData.Order == -1)
                    L.LogWarning("Invalid rank order in vehicle requirement: " + data.VehicleID + " :: " + rank.UnlockRank + ".");
                ucplayer.SendChat(T.RequestVehicleRankIncomplete, rankData);
            }
            else if (req is QuestUnlockRequirement quest)
            {
                if (Assets.find(quest.QuestID) is QuestAsset asset)
                {
                    ucplayer.Player.quests.sendAddQuest(asset.id);
                    ucplayer.SendChat(T.RequestVehicleQuestIncomplete, asset);
                }
                else
                {
                    ucplayer.SendChat(T.RequestVehicleQuestIncomplete, null!);
                }
            }
            else
            {
                L.LogWarning("Unhandled vehicle requirement type: " + req.GetType().Name);
            }
            return;
        }

        if (vehicle.asset.canBeLocked)
        {
            if (data.CreditCost > 0)
            {
                await ucplayer.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    await Points.UpdatePointsAsync(ucplayer, false, token).ConfigureAwait(false);
                    if (ucplayer.CachedCredits >= data.CreditCost)
                    {
                        await Points.AwardCreditsAsync(ucplayer, -data.CreditCost, isPurchase: true, @lock: false, token: token).ConfigureAwait(false);
                    }
                    else
                    {
                        await UCWarfare.ToUpdate(token);
                        ucplayer.SendChat(T.RequestVehicleCantAfford, data.CreditCost - ucplayer.CachedCredits, data.CreditCost);
                        return;
                    }
                }
                finally
                {
                    ucplayer.PurchaseSync.Release();
                }
            }

            await UCWarfare.ToUpdate(token);
            GiveVehicle(ucplayer, vehicle, data);
            Stats.StatsManager.ModifyStats(ucplayer.Steam64, x => x.VehiclesRequested++, false);
            Stats.StatsManager.ModifyTeam(team, t => t.VehiclesRequested++, false);
            Stats.StatsManager.ModifyVehicle(vehicle.id, v => v.TimesRequested++);
            CooldownManager.StartCooldown(ucplayer, ECooldownType.REQUEST_VEHICLE, CooldownManager.Config.RequestVehicleCooldown, vehicle.id);
        }
        else
        {
            ucplayer.SendChat(T.RequestVehicleAlreadyRequested);
        }
    }

    internal static void GiveVehicle(UCPlayer ucplayer, InteractableVehicle vehicle, VehicleData data)
    {
        vehicle.tellLocked(ucplayer.CSteamID, ucplayer.Player.quests.groupID, true);

        VehicleManager.ServerSetVehicleLock(vehicle, ucplayer.CSteamID, ucplayer.Player.quests.groupID, true);

        if (VehicleSpawner.HasLinkedSpawn(vehicle.instanceID, out VehicleSpawn spawn))
        {
            VehicleBayComponent? comp = spawn.Component;
            if (comp != null)
            {
                comp.OnRequest();
                ActionLogger.Add(EActionLogType.REQUEST_VEHICLE, $"{vehicle.asset.vehicleName} / {vehicle.id} / {vehicle.asset.GUID:N} at spawn {comp.gameObject.transform.position.ToString("N2", Data.Locale)}", ucplayer);
            }
            else
                ActionLogger.Add(EActionLogType.REQUEST_VEHICLE, $"{vehicle.asset.vehicleName} / {vehicle.id} / {vehicle.asset.GUID:N}", ucplayer);
            Data.Reporter?.OnVehicleRequest(ucplayer.Steam64, vehicle.asset.GUID, spawn.InstanceId);
        }
        else
            ActionLogger.Add(EActionLogType.REQUEST_VEHICLE, $"{vehicle.asset.vehicleName} / {vehicle.id} / {vehicle.asset.GUID:N}", ucplayer);

        vehicle.updateVehicle();
        vehicle.updatePhysics();

        if (Gamemode.Config.EffectUnlockVehicle.ValidReference(out EffectAsset effect))
            F.TriggerEffectReliable(effect, EffectManager.SMALL, vehicle.transform.position);

        ucplayer.SendChat(T.RequestVehicleSuccess, data);

        if (!FOBManager.Config.Buildables.Exists(e => e.Type == EBuildableType.EMPLACEMENT && e.Emplacement != null && e.Emplacement.EmplacementVehicle.MatchGuid(vehicle.asset.GUID)))
        {
            ItemManager.dropItem(new Item(28, true), ucplayer.Position, true, true, true); // gas can
            ItemManager.dropItem(new Item(277, true), ucplayer.Position, true, true, true); // car jack
        }
        foreach (Guid item in data.Items)
        {
            if (Assets.find(item) is ItemAsset a)
                ItemManager.dropItem(new Item(a.id, true), ucplayer.Position, true, true, true);
        }
    }
}
