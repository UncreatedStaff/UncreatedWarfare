using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uncreated.Players;
using Uncreated.Warfare.Gamemodes.Interfaces;

namespace Uncreated.Warfare.Kits
{
    public class Command_kit : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "kit";
        public string Help => "creates, renames or deletes a kit";
        public string Syntax => "/kit";
        public List<string> Aliases => new List<string>(0);
        public List<string> Permissions => new List<string>(1) { "uc.kit" };
        static void Reply(UCPlayer player, string key, params string[] formatting)
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
            UCPlayer ucplayer;
            if (!(caller is UnturnedPlayer player))
            {
                player = null;
                ucplayer = null;
            }
            else
            {
                ucplayer = UCPlayer.FromIRocketPlayer(caller);
                player = (UnturnedPlayer)caller;
            }
            if (!Data.Is<IKitRequests>(out _))
            {
                if (ucplayer == null)
                    L.LogWarning(Translation.Translate("command_e_gamemode", 0));
                else
                    player.SendChat("command_e_gamemode");
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

                if (KitManager.KitExists(kitName, out Kit kit)) // create kit
                {
                    if (kit.AllowedUsers.Contains(player.CSteamID.m_SteamID))
                    {
                        if (player.GetTeam() == kit.Team)
                        {
                            if (!CooldownManager.HasCooldown(ucplayer, ECooldownType.PREMIUM_KIT, out var cooldown, kit.Name))
                            {
                                bool branchChanged = false;
                                if (KitManager.HasKit(player.CSteamID, out var oldkit) && kit.Branch != EBranch.DEFAULT && oldkit.Branch != kit.Branch)
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

            if (player != null && !player.OnDuty())
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
                        foreach (Kit kit in KitManager.ActiveObjects)
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
                        KitManager.CreateKit(kitName, KitManager.ItemsFromInventory(player), KitManager.ClothesFromInventory(player));
                        RequestSigns.InvokeLangUpdateForSignsOfKit(kitName);
                        Reply(ucplayer, "kit_created", kitName);
                        return;
                    }
                    else // overwrite kit
                    {
                        KitManager.OverwriteKitItems(kit.Name, KitManager.ItemsFromInventory(player), KitManager.ClothesFromInventory(player));
                        RequestSigns.InvokeLangUpdateForSignsOfKit(kitName);
                        Reply(ucplayer, "kit_overwritten", kitName);
                        return;
                    }
                }
                // delete kit
                else if (op == "delete" || op == "d")
                {
                    if (KitManager.KitExists(kitName, out _))
                    {
                        KitManager.DeleteKit(kitName);

                        RequestSigns.InvokeLangUpdateForSignsOfKit(kitName);
                        RequestSigns.RemoveRequestSigns(kitName);
                        Reply(ucplayer, "kit_deleted", kitName);
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
                        if (KitManager.HasKit(player.CSteamID, out var oldkit) && kit.Branch != EBranch.DEFAULT && oldkit.Branch != kit.Branch)
                            branchChanged = true;

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
                        KitManager.SetProperty(kit, property, newValue, out bool set, out bool parsed, out bool foundproperty, out bool allowedToChange);
                        if (!allowedToChange) // error - invalid argument value
                        {
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
                        // success
                        Reply(ucplayer, "kit_setprop", property, kitName, newValue);
                        RequestSigns.InvokeLangUpdateForSignsOfKit(kitName);
                        if (wasloadout && !kit.IsLoadout)
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
                    UCPlayer target;
                    if (targetPlayer.Length != 17 || !ulong.TryParse(targetPlayer, System.Globalization.NumberStyles.Any, Data.Locale, out ulong steam64))
                        target = UCPlayer.FromName(targetPlayer);
                    else 
                        target = UCPlayer.FromID(steam64);

                    // error - no player found
                    if (target == null)
                    {
                        Reply(ucplayer, "kit_e_noplayer", targetPlayer);
                        return;
                    }
                    // error - player already has access
                    if (KitManager.HasAccess(target.CSteamID.m_SteamID, kit.Name))
                    {
                        Reply(ucplayer, "kit_e_alreadyaccess", targetPlayer, kitName);
                        return;
                    }

                    //success
                    FPlayerName name = F.GetPlayerOriginalNames(target.Player);
                    Reply(ucplayer, "kit_accessgiven", name.CharacterName, kitName);
                    KitManager.GiveAccess(target.Steam64, kit.Name);
                    RequestSigns.InvokeLangUpdateForSignsOfKit(target.Player.channel.owner, kitName);
                    return;
                }
                // remove player access to kit
                if (op == "removeaccess" || op == "removea")
                {
                    if (!KitManager.KitExists(kitName, out var kit))
                    {
                        Reply(ucplayer, "kit_e_noexist", kitName);
                        return;
                    }

                    UCPlayer target = UCPlayer.FromName(targetPlayer);

                    // error - no player found
                    if (target == null)
                    {
                        if (targetPlayer.Length == 17 && ulong.TryParse(targetPlayer, System.Globalization.NumberStyles.Any, Data.Locale, out ulong steamid))
                        {
                            if (!KitManager.HasAccess(steamid, kit.Name))
                            {
                                Reply(ucplayer, "kit_e_alreadyaccess", targetPlayer, kitName);
                                return;
                            }

                            //success
                            FPlayerName names = Data.DatabaseManager.GetUsernames(steamid);
                            Reply(ucplayer, "kit_accessremoved", names.CharacterName, kitName);
                            KitManager.RemoveAccess(steamid, kit.Name);

                            target = UCPlayer.FromID(steamid);
                            if (target == null)
                            {
                                RequestSigns.InvokeLangUpdateForSignsOfKit(target.Player.channel.owner, kitName);
                            }
                            return;
                        }
                        else
                        {
                            Reply(ucplayer, "kit_e_noplayer", targetPlayer);
                            return;
                        }
                    }
                    // error - player already has no access
                    if (!KitManager.HasAccess(target.CSteamID.m_SteamID, kit.Name))
                    {
                        Reply(ucplayer, "kit_e_noaccess", target.CharacterName, kitName);
                        return;
                    }

                    //success
                    FPlayerName name = F.GetPlayerOriginalNames(target.Player);
                    Reply(ucplayer, "kit_accessremoved", name.CharacterName, kitName);
                    KitManager.RemoveAccess(target.Steam64, kit.Name);
                    RequestSigns.InvokeLangUpdateForSignsOfKit(target.Player.channel.owner, kitName);
                    return;
                }
                // copy new kit from existing kit
                if (op == "copyfrom" || op == "cf")
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
                                UnlockBranch = existing.UnlockBranch,
                                UnlockLevel = existing.UnlockLevel,
                                TicketCost = existing.TicketCost,
                                IsPremium = existing.IsPremium,
                                PremiumCost = existing.PremiumCost,
                                IsLoadout = existing.IsLoadout,
                                TeamLimit = existing.TeamLimit,
                                Cooldown = existing.Cooldown,
                                AllowedUsers = new List<ulong>(),
                                SignTexts = existing.SignTexts,
                                Weapons = existing.Weapons
                            };

                            KitManager.CreateKit(newKit);

                            Reply(ucplayer, "kit_copied", existing.Name, newKit.Name);
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
                    if (!Data.DatabaseManager.HasPlayerJoined(steamid))
                    {
                        Reply(ucplayer, "kit_l_e_playernotfound", steamid.ToString());
                        return;
                    }
                    if (!ulong.TryParse(team_s, out ulong team))
                    {
                        Reply(ucplayer, "kit_l_e_invalid_team", team_s);
                        return;
                    }
                    if (!Enum.TryParse(class_s, out EClass kitClass))
                    {
                        Reply(ucplayer, "kit_l_e_invalid_class", class_s);
                        return;
                    }

                    char[] chars = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't' };

                    var loadoutsCount = KitManager.GetKitsWhere(k => k.IsLoadout && k.AllowedUsers.Contains(steamid)).Count();

                    char letter = chars[loadoutsCount];
                    string loadoutName = steamid.ToString() + "_" + letter;

                    if (!KitManager.KitExists(loadoutName, out _))
                    {

                        Kit loadout = KitManager.CreateKit(loadoutName, KitManager.ItemsFromInventory(player), KitManager.ClothesFromInventory(player));

                        KitManager.UpdateObjectsWhere(k => k.Name == loadoutName, k =>
                        {
                            k.IsLoadout = true;
                            k.Team = team;
                            k.Class = kitClass;
                            if (kitClass == EClass.PILOT)
                                k.Branch = EBranch.AIRFORCE;
                            else if (kitClass == EClass.CREWMAN)
                                k.Branch = EBranch.ARMOR;
                            else
                                k.Branch = EBranch.INFANTRY;

                            if (kitClass == EClass.HAT)
                                k.TeamLimit = 0.1F;

                            k.AllowedUsers.Add(steamid);

                                StringBuilder sb = new StringBuilder();
                            for (int i = 4; i < command.Length; i++)
                            {
                                if (i > 4) sb.Append(' ');
                                sb.Append(command[i]);
                            }
                            string displayName = sb.ToString();
                            KitManager.UpdateText(loadoutName, displayName, "en-us");

                        });

                        Reply(ucplayer, "kit_l_created", kitClass.ToString().ToUpper(), Data.DatabaseManager.GetUsernames(steamid).CharacterName, steamid.ToString(), loadoutName);
                    }
                    else
                    {
                        Reply(ucplayer, "kit_l_e_kitexists", class_s);
                        return;
                    }
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
                            if (KitManager.UpdateText(command[2], text, command[3]))
                                Reply(ucplayer, "kit_setprop", "sign text", command[2], command[3] + " : " + text);
                            else
                                Reply(ucplayer, "kit_e_noexist", command[2]);
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
