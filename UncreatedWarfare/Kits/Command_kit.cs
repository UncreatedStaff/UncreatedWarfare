using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Players;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;

namespace Uncreated.Warfare.Kits
{
    public class Command_kit : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "kit";
        public string Help => "creates, renames or deletes a kit";
        public string Syntax => "/kit";
        public List<string> Aliases => new List<string>() { "kit" };
        public List<string> Permissions => new List<string>() { "uc.kit" };
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
            UnturnedPlayer player;
            UCPlayer ucplayer;
            if (caller.DisplayName == "Console")
            {
                player = null;
                ucplayer = null;
            }
            else
            {
                player = caller as UnturnedPlayer;
                ucplayer = UCPlayer.FromIRocketPlayer(caller);
            }
            if (!Data.Is(out IKitRequests ctf))
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

            if (command.Length != 1 && !player.OnDuty())
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
                    if (!KitManager.KitExists(kitName, out var kit)) // create kit
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
                            Reply(ucplayer, "branch_changed", Translation.TranslateBranch(kit.Branch, ucplayer).ToUpper());
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
            else if (command.Length == 5)
            {
                op = command[0].ToLower();
                if (op == "set" || op == "s")
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
                            target = UCPlayer.FromID(steamid);
                            if (target == null)
                            {
                                if (KitManager.HasAccess(steamid, kit.Name))
                                {
                                    Reply(ucplayer, "kit_e_alreadyaccess", targetPlayer, kitName);
                                    return;
                                }
                                //success
                                FPlayerName names = Data.DatabaseManager.GetUsernames(steamid);
                                Reply(ucplayer, "kit_accessgiven", names.CharacterName, kitName);
                                KitManager.GiveAccess(steamid, kit.Name);
                                return;
                            }
                        }
                        else
                        {
                            Reply(ucplayer, "kit_e_noplayer", targetPlayer);
                            return;
                        }
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
                            target = UCPlayer.FromID(steamid);
                            if (target == null)
                            {
                                if (KitManager.HasAccess(steamid, kit.Name))
                                {
                                    Reply(ucplayer, "kit_e_alreadyaccess", targetPlayer, kitName);
                                    return;
                                }
                                //success
                                FPlayerName names = Data.DatabaseManager.GetUsernames(steamid);
                                Reply(ucplayer, "kit_accessremoved", names.CharacterName, kitName);
                                KitManager.RemoveAccess(steamid, kit.Name);
                                return;
                            }
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
            }
            else
            {
                Reply(ucplayer, "correct_usage", "/kit <create|delete|set>");
            }
        }
    }
}
