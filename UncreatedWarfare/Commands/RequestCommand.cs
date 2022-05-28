using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using VehicleSpawn = Uncreated.Warfare.Vehicles.VehicleSpawn;

namespace Uncreated.Warfare.Commands
{
    public class RequestCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "request";
        public string Help => "Request a kit by looking at a sign or request a vehicle by looking at the vehicle, then do /request.";
        public string Syntax => "/request";
        private readonly List<string> _aliases = new List<string>(1) { "req" };
        public List<string> Aliases => _aliases;
        private readonly List<string> _permissions = new List<string>(1) { "uc.request" };
		public List<string> Permissions => _permissions;
        public void Execute(IRocketPlayer caller, string[] command)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            // dont allow requesting between game end and leaderboard
            if (Data.Gamemode.State != EState.ACTIVE && Data.Gamemode.State != EState.STAGING)
            {
                return;
            }
            UnturnedPlayer player = (UnturnedPlayer)caller;
            if (!Data.Is(out IKitRequests ctf))
            {
                player.Message("command_e_gamemode");
                return;
            }
            UCPlayer? ucplayer = UCPlayer.FromIRocketPlayer(caller);
            if (player == null || ucplayer == null) return;
            if (ucplayer.Position == Vector3.zero) return;
            ulong team = ucplayer.GetTeam();
            if (command.Length > 0)
            {
                if (command[0].ToLower() == "save")
                {
                    if (player.HasPermission("uc.request.save"))
                    {
                        InteractableSign? sign = UCBarricadeManager.GetInteractableFromLook<InteractableSign>(player.Player.look);
                        if (sign == default) player.Message("request_not_looking");
                        else
                        {
                            if (RequestSigns.AddRequestSign(sign, out RequestSign signadded))
                            {
                                string teamcolor = TeamManager.NeutralColorHex;
                                if (KitManager.KitExists(signadded.kit_name, out Kit kit)) teamcolor = TeamManager.GetTeamHexColor(kit.Team);
                                player.Message("request_saved_sign", signadded.kit_name, teamcolor);
                                ActionLog.Add(EActionLogType.SAVE_REQUEST_SIGN, signadded.kit_name, ucplayer);
                            }
                            else player.Message("request_already_saved"); // sign already registered
                        }
                    }
                    else
                        player.Message("no_permissions");
                    return;
                }
                else if (command[0].ToLower() == "remove")
                {
                    if (player.HasPermission("uc.request.remove"))
                    {
                        InteractableSign? sign = UCBarricadeManager.GetInteractableFromLook<InteractableSign>(player.Player.look);
                        if (sign == default) player.Message("request_not_looking");
                        else
                        {
                            if (RequestSigns.SignExists(sign, out RequestSign requestsign))
                            {
                                string teamcolor = TeamManager.NeutralColorHex;
                                if (KitManager.KitExists(requestsign.kit_name, out Kit kit)) teamcolor = TeamManager.GetTeamHexColor(kit.Team);
                                player.Message("request_removed_sign", requestsign.kit_name, teamcolor);
                                RequestSigns.RemoveRequestSign(requestsign);
                                ActionLog.Add(EActionLogType.UNSAVE_REQUEST_SIGN, requestsign.kit_name, ucplayer);
                            }
                            else player.Message("request_already_removed");
                        }
                    }
                    else
                        player.Message("no_permissions");
                    return;
                }
            }
            InteractableVehicle? vehicle = UCBarricadeManager.GetVehicleFromLook(player.Player.look);
            InteractableSign? signlook = UCBarricadeManager.GetInteractableFromLook<InteractableSign>(player.Player.look);
            if (signlook != null)
            {
                if (!RequestSigns.SignExists(signlook, out RequestSign requestsign))
                {
                    if (!VehicleSigns.SignExists(signlook, out VehicleSign vbsign))
                    {
                        ucplayer.Message("request_kit_e_kitnoexist");
                        return;
                    }
                    if (vbsign.bay != default && vbsign.bay.HasLinkedVehicle(out InteractableVehicle veh))
                    {
                        if (Data.Is<IVehicles>(out _))
                        {
                            if (veh != default)
                                RequestVehicle(ucplayer, veh, team);
                        }
                        else
                        {
                            ucplayer.SendChat("command_e_gamemode");
                        }
                    }
                            return;
                }
                if (Data.Gamemode is not IKitRequests)
                {
                    ucplayer.SendChat("command_e_gamemode");
                    return;
                }
                if (requestsign.kit_name.StartsWith("loadout_"))
                {
                    if (ushort.TryParse(requestsign.kit_name.Substring(8), out ushort loadoutNumber))
                    {
                        byte bteam = ucplayer.Player.GetTeamByte();
                        List<Kit> loadouts = KitManager.GetKitsWhere(k => k.IsLoadout && k.Team == team && KitManager.HasAccessFast(k, ucplayer)).ToList();

                        if (loadouts.Count != 0)
                        {
                            if (loadoutNumber > 0 && loadoutNumber <= loadouts.Count)
                            {
                                Kit kit = loadouts[loadoutNumber - 1];

                                if (kit.IsClassLimited(out int currentPlayers, out int allowedPlayers, player.GetTeam()))
                                {
                                    ucplayer.Message("request_kit_e_limited", currentPlayers.ToString(Data.Locale), allowedPlayers.ToString(Data.Locale));
                                    return;
                                }

                                ActionLog.Add(EActionLogType.REQUEST_KIT, $"Loadout #{loadoutNumber}: {kit.Name}, Team {kit.Team}, Class: {Translation.TranslateEnum(kit.Class, 0)}", ucplayer);
                                GiveKit(ucplayer, kit);
                                Stats.StatsManager.ModifyKit(kit.Name, x => x.TimesRequested++, true);
                                Stats.StatsManager.ModifyStats(player.CSteamID.m_SteamID, s =>
                                {
                                    Stats.WarfareStats.KitData kitData = s.Kits.Find(k => k.KitID == kit.Name && k.Team == bteam);
                                    if (kitData == default)
                                    {
                                        kitData = new Stats.WarfareStats.KitData() { KitID = kit.Name, Team = bteam, TimesRequested = 1 };
                                        s.Kits.Add(kitData);
                                    }
                                    else
                                    {
                                        kitData.TimesRequested++;
                                    }
                                }, false);
                                return;
                            }
                        }
                    }
                    ucplayer.Message("request_loadout_e_notallowed");
                    return;
                }
                else
                {
                    if (!KitManager.KitExists(requestsign.kit_name, out Kit kit))
                    {
                        ucplayer.Message("request_kit_e_kitnoexist");
                    }
                    else if (ucplayer.KitName == kit.Name)
                    {
                        ucplayer.Message("request_kit_e_alreadyhaskit");
                    }
                    else if (kit.IsPremium && !KitManager.HasAccessFast(kit, ucplayer) && !UCWarfare.Config.OverrideKitRequirements)
                    {
                        ucplayer.Message("request_kit_e_notallowed");
                    }
                    else if (ucplayer.Rank.Level < kit.UnlockLevel)
                    {
                        ucplayer.Message("request_kit_e_wronglevel", RankData.GetRankName(kit.UnlockLevel));
                    }
                    else if (!kit.IsPremium && kit.CreditCost > 0 && !KitManager.HasAccessFast(kit, ucplayer) && !UCWarfare.Config.OverrideKitRequirements)
                    {
                        if (ucplayer.CachedCredits >= kit.CreditCost)
                            ucplayer.Message("request_kit_e_notboughtcredits", kit.CreditCost.ToString());
                        else
                            ucplayer.Message("request_kit_e_notenoughcredits", (kit.CreditCost - ucplayer.CachedCredits).ToString());
                    }
                    else if (kit.IsLimited(out int currentPlayers, out int allowedPlayers, player.GetTeam()))
                    {
                        ucplayer.Message("request_kit_e_limited", currentPlayers.ToString(Data.Locale), allowedPlayers.ToString(Data.Locale));
                    }
                    else if (kit.Class == EClass.SQUADLEADER && ucplayer.Squad is not null && !ucplayer.IsSquadLeader())
                    {
                        ucplayer.Message("request_kit_e_notsquadleader");
                    }
                    else if (Data.Gamemode.State == EState.ACTIVE && CooldownManager.HasCooldown(ucplayer, ECooldownType.REQUEST_KIT, out Cooldown requestCooldown) && !ucplayer.OnDutyOrAdmin() && !UCWarfare.Config.OverrideKitRequirements && !(kit.Class == EClass.CREWMAN || kit.Class == EClass.PILOT))
                    {
                        player.Message("kit_e_cooldownglobal", requestCooldown.ToString());
                    }
                    else if (kit.IsPremium && CooldownManager.HasCooldown(ucplayer, ECooldownType.PREMIUM_KIT, out Cooldown premiumCooldown, kit.Name) && !ucplayer.OnDutyOrAdmin() && !UCWarfare.Config.OverrideKitRequirements)
                    {
                        player.Message("kit_e_cooldown", premiumCooldown.ToString());
                    }
                    else
                    {
                        //for (int i = 0; i < kit.UnlockRequirements.Length; i++)
                        //{
                        //    BaseUnlockRequirement req = kit.UnlockRequirements[i];
                        //    if (req.CanAccess(ucplayer))
                        //        continue;
                        //    if (req is LevelUnlockRequirement level)
                        //    {
                        //        ucplayer.Message("request_kit_e_wronglevel", level.UnlockLevel.ToString(Data.Locale));
                        //    }
                        //    else if (req is RankUnlockRequirement rank)
                        //    {
                        //        ref Ranks.RankData data = ref Ranks.RankManager.GetRank(rank.UnlockRank, out bool success);
                        //        if (!success)
                        //            L.LogWarning("Invalid rank order in kit requirement: " + kit.Name + " :: " + rank.UnlockRank + ".");
                        //        ucplayer.Message("request_kit_e_wrongrank", data.GetName(ucplayer.Steam64), data.Color ?? UCWarfare.GetColorHex("default"));
                        //    }
                        //    else if (req is QuestUnlockRequirement quest)
                        //    {
                        //        if (Assets.find(quest.QuestID) is QuestAsset asset)
                        //        {
                        //            ucplayer.Message("request_kit_e_quest_incomplete", asset.questName);
                        //            ucplayer.Player.quests.sendAddQuest(asset.id);
                        //        }
                        //        else
                        //        {
                        //            ucplayer.Message("request_kit_e_quest_incomplete", kit.Name);
                        //        }
                        //    }
                        //    else
                        //    {
                        //        L.LogWarning("Unhandled kit requirement type: " + req.GetType().Name);
                        //    }
                        //    return;
                        //}
                        Task.Run(async () =>
                        {
                            bool hasKit = kit.CreditCost == 0 || await KitManager.HasAccess(kit, ucplayer);
                            await UCWarfare.ToUpdate();
                            if (!hasKit)
                            {
                                if (ucplayer.CachedCredits >= kit.CreditCost)
                                    ucplayer.Message("request_kit_e_notboughtcredits", kit.CreditCost.ToString());
                                else
                                    ucplayer.Message("request_kit_e_notenoughcredits", (kit.CreditCost - ucplayer.CachedCredits).ToString());
                                return;
                            }
                            if (kit.Class == EClass.SQUADLEADER && ucplayer.Squad == null)
                            {
                                if (SquadManager.Squads.Count(x => x.Team == team) < 8)
                                {
                                    // create a squad automatically if someone requests a squad leader kit.
                                    Squad squad = SquadManager.CreateSquad(ucplayer, ucplayer.GetTeam(), ucplayer.Branch);
                                    ucplayer.Message("squad_created", squad.Name);
                                }
                                else
                                {
                                    player.SendChat("squad_too_many");
                                    return;
                                }
                            }
                            ActionLog.Add(EActionLogType.REQUEST_KIT, $"Kit {kit.Name}, Team {kit.Team}, Class: {Translation.TranslateEnum(kit.Class, 0)}", ucplayer);
                            GiveKit(ucplayer, kit);
                        });
                        return;
                    }
                }
            }
            else if (vehicle != null)
            {
                if (Data.Gamemode is IVehicles)
                {
                    RequestVehicle(ucplayer, vehicle);
                }
                else
                {
                    ucplayer.SendChat("command_e_gamemode");
                    return;
                }
            }
            else
            {
                ucplayer.Message("request_not_looking");
            }
        }
        private void GiveKit(UCPlayer ucplayer, Kit kit)
        {
            AmmoCommand.WipeDroppedItems(ucplayer.Steam64);
            KitManager.GiveKit(ucplayer, kit);
            Stats.StatsManager.ModifyKit(kit.Name, k => k.TimesRequested++);
            ucplayer.Message("request_kit_given", kit.DisplayName.ToUpper());

            if (kit.IsPremium)
            {
                CooldownManager.StartCooldown(ucplayer, ECooldownType.PREMIUM_KIT, kit.Cooldown, kit.Name);
            }
            CooldownManager.StartCooldown(ucplayer, ECooldownType.REQUEST_KIT, CooldownManager.Config.RequestKitCooldown);

            PlayerManager.ApplyTo(ucplayer);
        }
        private void RequestVehicle(UCPlayer ucplayer, InteractableVehicle vehicle) => RequestVehicle(ucplayer, vehicle, ucplayer.GetTeam());
        private void RequestVehicle(UCPlayer ucplayer, InteractableVehicle vehicle, ulong team)
        {
            if (!VehicleBay.VehicleExists(vehicle.asset.GUID, out VehicleData data))
            {
                ucplayer.Message("request_vehicle_e_notrequestable");
                return;
            }
            else if (vehicle.lockedOwner != CSteamID.Nil || vehicle.lockedGroup != CSteamID.Nil)
            {
                ucplayer.Message("request_vehicle_e_alreadyrequested");
                return;
            }
            else if (data.Team != team)
            {
                ucplayer.Message("request_vehicle_e_notinteam");
                return;
            }
            else if (data.RequiresSL && ucplayer.Squad == null)
            {
                ucplayer.Message("request_vehicle_e_notinsquad");
                return;
            }
            else if (!KitManager.HasKit(ucplayer.CSteamID, out Kit kit))
            {
                ucplayer.Message("request_vehicle_e_nokit");
                return;
            }
            else if (data.RequiredClass != EClass.NONE && kit.Class != data.RequiredClass)
            {
                Kit requiredKit = KitManager.GetKitsWhere(k => k.Class == data.RequiredClass).FirstOrDefault();
                string @class;
                if (requiredKit != null)
                    @class = requiredKit.DisplayName.ToUpper();
                else @class = data.RequiredClass.ToString().ToUpper();
                ucplayer.Message("request_vehicle_e_wrongkit", requiredKit != null ? requiredKit.DisplayName.ToUpper() : data.RequiredClass.ToString().Replace('_', ' ').ToUpper());
                return;
            }
            else if (ucplayer.Rank.Level < data.UnlockLevel)
            {
                ucplayer.Message("request_vehicle_e_wronglevel", RankData.GetRankName(data.UnlockLevel));
                return;
            }
            else if (ucplayer.CachedCredits < data.CreditCost)
            {
                ucplayer.Message("request_vehicle_e_notenoughcredits", (data.CreditCost - ucplayer.CachedCredits).ToString());
                return;
            }
            else if (CooldownManager.HasCooldown(ucplayer, ECooldownType.REQUEST_VEHICLE, out Cooldown cooldown, vehicle.id))
            {
                ucplayer.Message("request_vehicle_e_cooldown", unchecked((uint)Math.Round(cooldown.Timeleft.TotalSeconds)).GetTimeFromSeconds(ucplayer.Steam64));
                return;
            }
            else
            {
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
                                ucplayer.Message("request_vehicle_e_already_owned");
                                return;
                            }
                        }
                    }
                }
            }
            if (data.IsDelayed(out Delay delay) && delay.type != EDelayType.NONE)
            {
                RequestVehicleIsDelayed(ucplayer, ref delay, team, data);
                return;
            }
            //for (int i = 0; i < data.UnlockRequirements.Length; i++)
            //{
            //    BaseUnlockRequirement req = data.UnlockRequirements[i];
            //    if (req.CanAccess(ucplayer))
            //        continue;
            //    if (req is LevelUnlockRequirement level)
            //    {
            //        ucplayer.Message("request_vehicle_e_wronglevel", level.UnlockLevel.ToString(Data.Locale));
            //    }
            //    else if (req is RankUnlockRequirement rank)
            //    {
            //        ref Ranks.RankData rankData = ref Ranks.RankManager.GetRank(rank.UnlockRank, out bool success);
            //        if (!success)
            //            L.LogWarning("Invalid rank order in vehicle requirement: " + data.VehicleID + " :: " + rank.UnlockRank + ".");
            //        ucplayer.Message("request_vehicle_e_wrongrank", rankData.GetName(ucplayer.Steam64), rankData.Color ?? UCWarfare.GetColorHex("default"));
            //    }
            //    else if (req is QuestUnlockRequirement quest)
            //    {
            //        if (Assets.find(quest.QuestID) is QuestAsset asset)
            //        {
            //            ucplayer.Message("request_vehicle_e_quest_incomplete", asset.questName);
            //            ucplayer.Player.quests.sendAddQuest(asset.id);
            //        }
            //        else
            //        {
            //            ucplayer.Message("request_vehicle_e_quest_incomplete", vehicle.asset.name);
            //        }
            //    }
            //    else
            //    {
            //        L.LogWarning("Unhandled vehicle requirement type: " + req.GetType().Name);
            //    }
            //    return;
            //}
            if (vehicle.asset != default && vehicle.asset.canBeLocked)
            {
                vehicle.tellLocked(ucplayer.CSteamID, ucplayer.Player.quests.groupID, true);

                VehicleManager.ServerSetVehicleLock(vehicle, ucplayer.CSteamID, ucplayer.Player.quests.groupID, true);

                if (VehicleSpawner.HasLinkedSpawn(vehicle.instanceID, out VehicleSpawn spawn))
                {
                    VehicleBayComponent? comp = spawn.Component;
                    if (comp != null)
                    {
                        comp.OnRequest();
                        ActionLog.Add(EActionLogType.REQUEST_VEHICLE, $"{vehicle.asset.vehicleName} / {vehicle.id} / {vehicle.asset.GUID:N} at spawn {comp.gameObject.transform.position.ToString("N2")}", ucplayer);
                    }
                    else
                        ActionLog.Add(EActionLogType.REQUEST_VEHICLE, $"{vehicle.asset.vehicleName} / {vehicle.id} / {vehicle.asset.GUID:N}", ucplayer);
                    Data.Reporter?.OnVehicleRequest(ucplayer.Steam64, vehicle.asset.GUID, spawn.SpawnPadInstanceID);
                }
                else
                    ActionLog.Add(EActionLogType.REQUEST_VEHICLE, $"{vehicle.asset.vehicleName} / {vehicle.id} / {vehicle.asset.GUID:N}", ucplayer);

                vehicle.updateVehicle();
                vehicle.updatePhysics();
                
                EffectManager.sendEffect(8, EffectManager.SMALL, vehicle.transform.position);
                ucplayer.Message("request_vehicle_given", vehicle.asset.vehicleName, UCWarfare.GetColorHex("request_vehicle_given_vehicle_name"));

                if (!FOBManager.Config.Buildables.Exists(e => e.Type == EBuildableType.EMPLACEMENT && e.BuildableBarricade == vehicle.asset.GUID))
                {
                    ItemManager.dropItem(new Item(28, true), ucplayer.Position, true, true, true); // gas can
                    ItemManager.dropItem(new Item(277, true), ucplayer.Position, true, true, true); // car jack
                }
                foreach (Guid item in data.Items)
                {
                    if (Assets.find(item) is ItemAsset a)
                        ItemManager.dropItem(new Item(a.id, true), ucplayer.Position, true, true, true);
                }
                Stats.StatsManager.ModifyStats(ucplayer.Steam64, x => x.VehiclesRequested++, false);
                Stats.StatsManager.ModifyTeam(team, t => t.VehiclesRequested++, false);
                Stats.StatsManager.ModifyVehicle(vehicle.id, v => v.TimesRequested++);

                Points.AwardCredits(ucplayer, -data.CreditCost);
            }
            else
            {
                ucplayer.Message("request_vehicle_e_alreadyrequested");
            }
        }
        private void RequestVehicleIsDelayed(UCPlayer ucplayer, ref Delay delay, ulong team, VehicleData data)
        {
            if (delay.type == EDelayType.OUT_OF_STAGING &&
                (delay.gamemode is null || 
                (Data.Is(out Insurgency ins1) && delay.gamemode == "Insurgency" && team == ins1.AttackingTeam) ||
                (Data.Is(out Invasion inv2) && delay.gamemode == "Invasion" && team == inv2.AttackingTeam))
                )
            {
                ucplayer.Message("request_vehicle_e_staging_delay");
                return;
            }
            else if (delay.type == EDelayType.TIME)
            {
                float timeLeft = delay.value - Data.Gamemode.SecondsSinceStart;
                ucplayer.Message("request_vehicle_e_time_delay", ((uint)Mathf.Round(timeLeft)).GetTimeFromSeconds(ucplayer.Steam64));
            }
            else if (delay.type == EDelayType.FLAG || delay.type == EDelayType.FLAG_PERCENT)
            {
                if (Data.Is(out Invasion invasion))
                {
                    int ct = delay.type == EDelayType.FLAG ? Mathf.RoundToInt(delay.value) : Mathf.FloorToInt(invasion.Rotation.Count * (delay.value / 100f));
                    int ct2;
                    if (data.Team == 1)
                    {
                        if (invasion.AttackingTeam == 1)
                            ct2 = ct - invasion.ObjectiveT1Index;
                        else
                            ct2 = ct - (invasion.Rotation.Count - invasion.ObjectiveT2Index - 1);
                    }
                    else if (data.Team == 2)
                    {
                        if (invasion.AttackingTeam == 2)
                            ct2 = ct - (invasion.Rotation.Count - invasion.ObjectiveT2Index - 1);
                        else
                            ct2 = ct - invasion.ObjectiveT1Index;
                    }
                    else ct2 = ct;
                    int ind = ct - ct2;
                    if (invasion.AttackingTeam == 2) ind = invasion.Rotation.Count - ind - 1;
                    if (ct2 == 1 && invasion.Rotation.Count > 0 && ind < invasion.Rotation.Count)
                    {
                        if (data.Team == invasion.AttackingTeam)
                            ucplayer.Message("request_vehicle_e_flag_delay_1", invasion.Rotation[ind].ShortName);
                        else if (data.Team == invasion.DefendingTeam)
                            ucplayer.Message("request_vehicle_e_flag_lose_delay_1", invasion.Rotation[ind].ShortName);
                        else
                            ucplayer.Message("request_vehicle_e_flag_delay_2+", ct2.ToString(Data.Locale));
                    }
                    else if (data.Team == invasion.DefendingTeam)
                        ucplayer.Message("request_vehicle_e_flag_lose_delay_2+", ct2.ToString(Data.Locale));
                    else
                        ucplayer.Message("request_vehicle_e_flag_delay_2+", ct2.ToString(Data.Locale));
                }
                else if (Data.Is(out IFlagTeamObjectiveGamemode flags))
                {
                    int ct = delay.type == EDelayType.FLAG ? Mathf.RoundToInt(delay.value) : Mathf.FloorToInt(flags.Rotation.Count * (delay.value / 100f));
                    int ct2;
                    if (data.Team == 1)
                        ct2 = ct - flags.ObjectiveT1Index;
                    else if (data.Team == 2)
                        ct2 = ct - (flags.Rotation.Count - flags.ObjectiveT2Index - 1);
                    else ct2 = ct;
                    int ind = ct - ct2;
                    if (data.Team == 2) ind = flags.Rotation.Count - ind - 1;
                    if (ct2 == 1 && flags.Rotation.Count > 0 && ind < flags.Rotation.Count)
                    {
                        if (data.Team == 1 || data.Team == 2)
                            ucplayer.Message("request_vehicle_e_flag_delay_1", flags.Rotation[ind].ShortName);
                        else
                            ucplayer.Message("request_vehicle_e_flag_delay_2+", ct2.ToString(Data.Locale));
                    }
                    else
                    {
                        ucplayer.Message("request_vehicle_e_flag_delay_2+", ct2.ToString(Data.Locale));
                    }
                }
                else if (Data.Is(out Insurgency ins))
                {
                    int ct = delay.type == EDelayType.FLAG ? Mathf.RoundToInt(delay.value) : Mathf.FloorToInt(ins.Caches.Count * (delay.value / 100f));
                    int ct2;
                    ct2 = ct - ins.CachesDestroyed;
                    int ind = ct - ct2;
                    if (ct2 == 1 && ins.Caches.Count > 0 && ind < ins.Caches.Count)
                    {
                        if (data.Team == ins.AttackingTeam)
                        {
                            if (ins.Caches[ind].IsDiscovered)
                                ucplayer.Message("request_vehicle_e_cache_delay_atk_1", ins.Caches[ind].Cache.ClosestLocation);
                            else
                                ucplayer.Message("request_vehicle_e_cache_delay_atk_undiscovered_1");
                        }
                        else if (data.Team == ins.DefendingTeam)
                            if (ins.Caches[ind].IsActive)
                                ucplayer.Message("request_vehicle_e_cache_delay_def_1", ins.Caches[ind].Cache.ClosestLocation);
                            else
                                ucplayer.Message("request_vehicle_e_cache_delay_def_undiscovered_1");
                        else
                            ucplayer.Message("request_vehicle_e_cache_delay_atk_2+", ct2.ToString(Data.Locale));
                    }
                    else
                    {
                        if (data.Team == ins.AttackingTeam)
                            ucplayer.Message("request_vehicle_e_cache_delay_atk_2+", ct2.ToString(Data.Locale));
                        else
                            ucplayer.Message("request_vehicle_e_cache_delay_def_2+", ct2.ToString(Data.Locale));
                    }
                }
            }
            else
            {
                ucplayer.Message("request_vehicle_e_unknown_delay", delay.ToString());
            }
        }
    }
}
