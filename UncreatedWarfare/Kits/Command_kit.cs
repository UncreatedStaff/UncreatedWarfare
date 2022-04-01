using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.Warfare.Gamemodes.Interfaces;

namespace Uncreated.Warfare.Kits
{
    public class Command_kit : IRocketCommand
    {
        private static readonly char[] LOADOUT_CHARACTERS = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't' };
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "kit";
        public string Help => "creates, renames or deletes a kit";
        public string Syntax => "/kit";
        public List<string> Aliases => new List<string>(0);
        public List<string> Permissions => new List<string>(1) { "uc.kit" };
        static void Reply(UCPlayer? player, string key, params string[] formatting)
        {
            if (player == null)
            {
                L.Log(Translation.Translate(key, 0, out _, formatting), ConsoleColor.Yellow);
            }
            else
            {
                player.SendChat(Translation.Translate(key, player.Steam64, formatting));
            }
        }
        public void Execute(IRocketPlayer caller, string[] command)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            UCPlayer? ucplayer = UCPlayer.FromIRocketPlayer(caller);
            if (!Data.Is<IKitRequests>(out _))
            {
                if (ucplayer == null)
                    L.LogWarning(Translation.Translate("command_e_gamemode", 0));
                else
                    ucplayer.SendChat("command_e_gamemode");
                return;
            }
            string property;
            string kitName;
            string newValue;
            string targetPlayer;

            if (command.Length == 1)
            {
                if (ucplayer == null)
                {
                    L.Log("This command can not be called from console.", ConsoleColor.Red);
                    return;
                }
                kitName = command[0];

                if (KitManager.KitExists(kitName, out Kit kit))
                {
                    if (KitManager.HasAccessFast(kit, ucplayer))
                    {
                        if (ucplayer.GetTeam() == kit.Team)
                        {
                            if (!CooldownManager.HasCooldown(ucplayer, ECooldownType.PREMIUM_KIT, out Cooldown? cooldown, kit.Name))
                            {
                                bool branchChanged = false;
                                if (KitManager.HasKit(ucplayer, out Kit oldkit) && kit.Branch != EBranch.DEFAULT && oldkit.Branch != kit.Branch)
                                    branchChanged = true;

                                KitManager.GiveKit(ucplayer, kit);
                                Reply(ucplayer, "request_kit_given", kit.DisplayName.ToUpper());

                                if (branchChanged)
                                {
                                    ucplayer.Branch = kit.Branch;
                                    Reply(ucplayer, "branch_changed", Translation.TranslateBranch(kit.Branch, ucplayer).ToUpper());
                                }
                                PlayerManager.ApplyToOnline();
                                return;
                            }
                            else
                            {
                                Reply(ucplayer, "kit_e_cooldown", cooldown.ToString());
                                return;
                            }
                        }
                        else
                        {
                            Reply(ucplayer, "kit_e_wrongteam", kitName);
                            return;
                        }
                    }
                    else
                    {
                        Reply(ucplayer, "kit_e_notallowed", kitName);
                        return;
                    }
                }
                else
                {
                    Reply(ucplayer, "kit_e_noexist", kitName);
                    return;
                }
            }

            if (ucplayer != null && !ucplayer.OnDuty())
            {
                Reply(ucplayer, "kits_notonduty");
                return;
            }
            string op;
            if (command.Length == 2)
            {
                op = command[0].ToLower();
                kitName = command[1];
                if (op == "search")
                {
                    kitName = kitName.ToLower();
                    StringBuilder sb = new StringBuilder();
                    try
                    {
                        int counter = 0;
                        foreach (Kit kit in KitManager.Instance.Kits.Values)
                        {
                            if (kit.SignTexts == null || kit.SignTexts.Count == 0) continue;
                            foreach (KeyValuePair<string, string> lang in kit.SignTexts)
                            {
                                if (lang.Value.ToLower().Contains(kitName))
                                {
                                    if (counter > 0) sb.Append(", ");
                                    sb.Append(kit.Name);
                                    counter++;
                                    break;
                                }
                            }
                            if (counter > 8) break;
                        }
                    }
                    catch (Exception ex)
                    {
                        L.LogError("Error searching for kit names.");
                        L.LogError(ex);
                        sb.Append("<color=#dd1111>ERROR</color>");
                    }
                    if (sb.Length == 0)
                    {
                        sb.Append("--");
                    }
                    Reply(ucplayer, "kit_search_results", sb.ToString());
                }
                // create kit
                else if (op == "create" || op == "c")
                {
                    if (ucplayer == null)
                    {
                        L.Log("This command can not be called from console.", ConsoleColor.Red);
                        return;
                    }
                    if (!KitManager.KitExists(kitName, out Kit kit)) // create kit
                    {
                        Task.Run(async () =>
                        {
                            Kit? kit = await KitManager.AddKit(new Kit(kitName, KitManager.ItemsFromInventory(ucplayer), KitManager.ClothesFromInventory(ucplayer)));
                            if (kit != null)
                            {
                                ActionLog.Add(EActionLogType.CREATE_KIT, kitName, ucplayer);
                                await UCWarfare.ToUpdate();
                                RequestSigns.InvokeLangUpdateForSignsOfKit(kitName);
                                Reply(ucplayer, "kit_created", kitName);
                            }
                        });
                        
                        return;
                    }
                    else // overwrite kit
                    {
                        Task.Run(async () =>
                        {
                            kit.Items = KitManager.ItemsFromInventory(ucplayer);
                            kit.Clothes = KitManager.ClothesFromInventory(ucplayer);
                            kit = (await KitManager.AddKit(kit))!;
                            if (kit != null)
                            {
                                ActionLog.Add(EActionLogType.EDIT_KIT, kitName, ucplayer);
                                await UCWarfare.ToUpdate();
                                RequestSigns.InvokeLangUpdateForSignsOfKit(kitName);
                                Reply(ucplayer, "kit_overwritten", kitName);
                            }
                        });
                        return;
                    }
                }
                // delete kit
                else if (op == "delete" || op == "d")
                {
                    if (KitManager.KitExists(kitName, out Kit kit))
                    {
                        Task.Run(async () =>
                        {
                            if (await KitManager.DeleteKit(kit))
                            {
                                ActionLog.Add(EActionLogType.DELETE_KIT, kitName, ucplayer ?? 0ul);

                                await UCWarfare.ToUpdate();
                                RequestSigns.InvokeLangUpdateForSignsOfKit(kitName);
                                RequestSigns.RemoveRequestSigns(kitName);
                                Reply(ucplayer, "kit_deleted", kitName);
                            }
                        });
                        return;
                    }
                    else // error
                    {
                        Reply(ucplayer, "kit_e_noexist", kitName);
                        return;
                    }
                }
                else if (op == "give")
                {
                    if (ucplayer == null)
                    {
                        L.Log("This command can not be called from console.", ConsoleColor.Red);
                        return;
                    }
                    if (KitManager.KitExists(kitName, out Kit kit))
                    {
                        bool branchChanged = false;
                        if (KitManager.HasKit(ucplayer, out Kit oldkit) && kit.Branch != EBranch.DEFAULT && oldkit.Branch != kit.Branch)
                            branchChanged = true;

                        ActionLog.Add(EActionLogType.GIVE_KIT, kitName, ucplayer);

                        KitManager.GiveKit(ucplayer, kit);
                        Reply(ucplayer, "request_kit_given", kit.DisplayName.ToUpper());

                        if (branchChanged)
                        {
                            ucplayer.Branch = kit.Branch;
                        }

                        PlayerManager.ApplyToOnline();
                    }
                    else // error
                    {
                        Reply(ucplayer, "kit_e_noexist", kitName);
                        return;
                    }
                }
                return;
            }
            // change kit property
            else if (command.Length == 4)
            {
                op = command[0].ToLower();
                property = command[1];
                kitName = command[2];
                newValue = command[3];

                if (op == "set" || op == "s")
                {
                    if (KitManager.KitExists(kitName, out Kit kit))
                    {
                        bool wasloadout = kit.IsLoadout;
                        KitEx.SetProperty(kit, property, newValue, out bool set, out bool parsed, out bool foundproperty, out bool allowedToChange);
                        if (!allowedToChange) // error - invalid argument value
                        {
                            if (property == "level")
                            {
                                if (int.TryParse(newValue, out int level) && level >= 0)
                                {
                                    if (level == 0)
                                        kit.RemoveLevelUnlock();
                                    else
                                        kit.AddSimpleLevelUnlock(level);
                                    Task.Run(async() => await KitManager.AddKit(kit)).ConfigureAwait(false);
                                    goto SUCCESS;
                                }

                                Reply(ucplayer, "kit_e_invalidarg", newValue, property);
                                return;
                            }

                            Reply(ucplayer, "kit_e_invalidarg_not_allowed", property);
                            return;
                        }
                        if (!parsed) // error - invalid argument value
                        {
                            Reply(ucplayer, "kit_e_invalidarg", newValue, property);
                            return;
                        }
                        if (!foundproperty || !set) // error - invalid property name
                        {
                            Reply(ucplayer, "kit_e_invalidprop", property);
                            return;
                        }
                        SUCCESS:
                        // success
                        Reply(ucplayer, "kit_setprop", property, kitName, newValue);
                        ActionLog.Add(EActionLogType.SET_KIT_PROPERTY, kitName + ": " + property.ToUpper() + " >> " + newValue.ToUpper(), ucplayer ?? 0ul);
                        RequestSigns.InvokeLangUpdateForSignsOfKit(kitName);
                        if (wasloadout || kit.IsLoadout)
                        {
                            for (int s = 0; s < RequestSigns.ActiveObjects.Count; s++)
                            {
                                if (RequestSigns.ActiveObjects[s].kit_name.StartsWith("loadout_"))
                                    RequestSigns.ActiveObjects[s].InvokeUpdate();
                            }
                        }
                        return;
                    }
                    else
                    {
                        Reply(ucplayer, "kit_e_noexist", kitName);
                        return;
                    }
                }
            }
            else if (command.Length == 3)
            {
                op = command[0].ToLower();
                targetPlayer = command[1];
                kitName = command[2];

                // give player access to kit
                if (op == "giveaccess" || op == "givea")
                {
                    if (!KitManager.KitExists(kitName, out Kit kit))
                    {
                        Reply(ucplayer, "kit_e_noexist", kitName);
                        return;
                    }

                    ulong steam64 = 0;

                    UCPlayer? target;
                    if (ulong.TryParse(targetPlayer, System.Globalization.NumberStyles.Any, Data.Locale, out steam64) && OffenseManager.IsValidSteam64ID(steam64))
                        target = UCPlayer.FromID(steam64);
                    else
                        target = UCPlayer.FromName(targetPlayer);

                    bool hasPlayerJoined = true;
                    Task.Run(async () =>
                    {
                        hasPlayerJoined = await Data.DatabaseManager.HasPlayerJoined(steam64);
                        if (!hasPlayerJoined)
                        {
                            await UCWarfare.ToUpdate();
                            Reply(ucplayer, "kit_e_noplayer", targetPlayer);
                            return;
                        }
                        bool hasAccess;
                        if (target != null)
                        {
                            steam64 = target.Steam64;
                            targetPlayer = target.CharacterName;
                            hasAccess = KitManager.HasAccessFast(kit, target);
                        }
                        else
                        {
                            targetPlayer = (await Data.DatabaseManager.GetUsernamesAsync(steam64)).CharacterName;
                            hasAccess = await KitManager.HasAccess(kit, steam64);
                        }
                        if (hasAccess)
                        {
                            await UCWarfare.ToUpdate();
                            Reply(ucplayer, "kit_e_alreadyaccess", targetPlayer, kitName);
                            return;
                        }
                        if (target != null)
                            await KitManager.GiveAccess(kit, target, EKitAccessType.PURCHASE);
                        else
                            await KitManager.GiveAccess(kit, steam64, EKitAccessType.PURCHASE);
                        ActionLog.Add(EActionLogType.CHANGE_KIT_ACCESS, steam64.ToString(Data.Locale) + " GIVEN ACCESS TO " + kitName, ucplayer ?? 0ul);

                        await UCWarfare.ToUpdate();
                        Reply(ucplayer, "kit_accessgiven", targetPlayer, kitName);
                        if (target != null)
                            RequestSigns.InvokeLangUpdateForSignsOfKit(target.Player.channel.owner, kitName);
                    });
                    return;
                }
                // remove player access to kit
                if (op == "removeaccess" || op == "removea")
                {
                    if (!KitManager.KitExists(kitName, out Kit kit))
                    {
                        Reply(ucplayer, "kit_e_noexist", kitName);
                        return;
                    }


                    UCPlayer? target;
                    if (ulong.TryParse(targetPlayer, System.Globalization.NumberStyles.Any, Data.Locale, out ulong steam64) && OffenseManager.IsValidSteam64ID(steam64))
                        target = UCPlayer.FromID(steam64);
                    else
                        target = UCPlayer.FromName(targetPlayer);

                    bool hasPlayerJoined = true;
                    Task.Run(async () =>
                    {
                        hasPlayerJoined = await Data.DatabaseManager.HasPlayerJoined(steam64);
                        if (!hasPlayerJoined)
                        {
                            await UCWarfare.ToUpdate();
                            Reply(ucplayer, "kit_e_noplayer", targetPlayer);
                            return;
                        }
                        bool hasAccess;
                        if (target != null)
                        {
                            steam64 = target.Steam64;
                            targetPlayer = target.CharacterName;
                            hasAccess = KitManager.HasAccessFast(kit, target);
                        }
                        else
                        {
                            targetPlayer = (await Data.DatabaseManager.GetUsernamesAsync(steam64)).CharacterName;
                            hasAccess = await KitManager.HasAccess(kit, steam64);
                        }
                        if (!hasAccess)
                        {
                            await UCWarfare.ToUpdate();
                            Reply(ucplayer, "kit_e_noaccess", targetPlayer, kitName);
                            return;
                        }
                        if (target != null)
                            await KitManager.RemoveAccess(kit, target);
                        else
                            await KitManager.RemoveAccess(kit, steam64);
                        ActionLog.Add(EActionLogType.CHANGE_KIT_ACCESS, steam64.ToString(Data.Locale) + " DENIED ACCESS TO " + kitName, ucplayer ?? 0ul);

                        await UCWarfare.ToUpdate();
                        Reply(ucplayer, "kit_accessremoved", targetPlayer, kitName);
                        if (target != null)
                            RequestSigns.InvokeLangUpdateForSignsOfKit(target.Player.channel.owner, kitName);
                    });
                    return;
                }
                if (op == "copyfrom" || op == "cf") // copy new kit from existing kit
                {
                    string existingName = command[1].ToLower();

                    if (KitManager.KitExists(existingName, out Kit existing))
                    {
                        if (!KitManager.KitExists(kitName, out _))
                        {
                            Kit newKit = new Kit
                            {
                                Name = kitName.ToLower(),
                                Items = existing.Items,
                                Clothes = existing.Clothes,
                                Class = existing.Class,
                                Branch = existing.Branch,
                                Team = existing.Team,
                                Skillsets = existing.Skillsets,
                                UnlockRequirements = existing.UnlockRequirements,
                                CreditCost = existing.CreditCost,
                                IsPremium = existing.IsPremium,
                                PremiumCost = existing.PremiumCost,
                                IsLoadout = existing.IsLoadout,
                                TeamLimit = existing.TeamLimit,
                                Cooldown = existing.Cooldown,
                                AllowedUsers = new List<ulong>(),
                                SignTexts = existing.SignTexts,
                                Weapons = existing.Weapons
                            };

                            Task.Run(async () =>
                            {
                                await KitManager.AddKit(newKit);
                                ActionLog.Add(EActionLogType.CREATE_KIT, kitName + " COPIED FROM " + existingName, ucplayer ?? 0ul);
                                await UCWarfare.ToUpdate();
                                Reply(ucplayer, "kit_copied", existing.Name, newKit.Name);
                            });
                        }
                        else
                        {
                            Reply(ucplayer, "kit_e_exist", kitName);
                            return;
                        }
                    }
                    else
                    {
                        Reply(ucplayer, "kit_e_noexist", existingName);
                        return;
                    }
                }
            }
            else if (command.Length >= 5)
            {
                op = command[0].ToLower();
                string steamid_s = command[1].ToLower();
                string team_s = command[2].ToLower();
                string class_s = command[3].ToLower();

                if (op == "createloadout" || op == "cloadout" || op == "cl")
                {
                    if (ucplayer == null)
                    {
                        L.Log("This command can not be called from console.", ConsoleColor.Red);
                        return;
                    }

                    if (!ulong.TryParse(steamid_s, out ulong steamid))
                    {
                        Reply(ucplayer, "kit_l_e_invalid_steamid", steamid_s);
                        return;
                    }
                    if (!Data.DatabaseManager.HasPlayerJoined(steamid).Result) // todo
                    {
                        Reply(ucplayer, "kit_l_e_playernotfound", steamid.ToString());
                        return;
                    }
                    if (!ulong.TryParse(team_s, out ulong team))
                    {
                        Reply(ucplayer, "kit_l_e_invalid_team", team_s);
                        return;
                    }
                    if (!Enum.TryParse(class_s, true, out EClass kitClass))
                    {
                        Reply(ucplayer, "kit_l_e_invalid_class", class_s);
                        return;
                    }
                    Task.Run(async () =>
                    {
                        char let = 'a';
                        await Data.DatabaseManager.QueryAsync("SELECT `InternalName` FROM `kit_data` WHERE `InternalName` LIKE @0 ORDER BY `InternalName`;", new object[1]
                        {
                            steamid.ToString() + "_%"
                        }, R =>
                        {
                            string name = R.GetString(0);
                            if (name.Length <= 18)
                                return;
                            name = name.Substring(18);
                            char let2 = name[0];
                            if (let2 == let)
                                let++;
                        });
                        string loadoutName = steamid.ToString() + "_" + let;

                        await UCWarfare.ToUpdate();
                        if (!KitManager.KitExists(loadoutName, out _))
                        {

                            Kit? loadout = new Kit(loadoutName, KitManager.ItemsFromInventory(ucplayer), KitManager.ClothesFromInventory(ucplayer));

                            if (loadout != null)
                            {
                                loadout.IsLoadout = true;
                                loadout.Team = team;
                                loadout.Class = kitClass;
                                if (kitClass == EClass.PILOT)
                                    loadout.Branch = EBranch.AIRFORCE;
                                else if (kitClass == EClass.CREWMAN)
                                    loadout.Branch = EBranch.ARMOR;
                                else
                                    loadout.Branch = EBranch.INFANTRY;

                                if (kitClass == EClass.HAT)
                                    loadout.TeamLimit = 0.1F;


                                StringBuilder sb = new StringBuilder();
                                for (int i = 4; i < command.Length; i++)
                                {
                                    if (i > 4) sb.Append(' ');
                                    sb.Append(command[i]);
                                }
                                string displayName = sb.ToString();

                                await KitManager.AddKit(loadout);
                                await KitManager.GiveAccess(loadout, steamid, EKitAccessType.PURCHASE);
                                ActionLog.Add(EActionLogType.CREATE_KIT, loadoutName, ucplayer);
                                await UCWarfare.ToUpdate();
                                KitManager.UpdateText(loadout, displayName);
                                Reply(ucplayer, "kit_l_created", kitClass.ToString().ToUpper(), Data.DatabaseManager.GetUsernames(steamid).CharacterName, steamid.ToString(), loadoutName);
                            }
                        }
                        else
                        {
                            Reply(ucplayer, "kit_l_e_kitexists", class_s);
                        }
                    });
                    return;
                }
                else if (op == "set" || op == "s")
                {
                    if (command[1] == "sign")
                    {
                        if (command.Length > 4)
                        {
                            StringBuilder sb = new StringBuilder();
                            for (int i = 4; i < command.Length; i++)
                            {
                                if (i > 4) sb.Append(' ');
                                sb.Append(command[i]);
                            }
                            string text = sb.ToString();
                            text.Replace("\\n", "\n");
                            L.Log(text);
                            if (KitManager.KitExists(command[2], out Kit kit))
                            {
                                KitManager.UpdateText(kit, text, command[3]);
                                Reply(ucplayer, "kit_setprop", "sign text", command[2], command[3] + " : " + text);
                            }
                            else
                                Reply(ucplayer, "kit_e_noexist", command[2]);
                            ActionLog.Add(EActionLogType.SET_KIT_PROPERTY, command[2] + ": SIGN TEXT >> \"" + text + "\"", ucplayer ?? 0ul);
                            return;
                        }
                        else
                        {
                            Reply(ucplayer, "kit_e_set_sign_syntax", command[2]);
                            return;
                        }
                    }
                }
            }
            else
            {
                Reply(ucplayer, "correct_usage", "/kit <create|delete|set>");
            }
        }
    }
}
