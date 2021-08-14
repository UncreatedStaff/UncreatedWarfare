using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Players;

namespace Uncreated.Warfare.Kits
{
    public class Command_kit : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "kit";
        public string Help => "creates, renames or deletes a kit";
        public string Syntax => "/kit";
        public List<string> Aliases => new List<string>() { "kit" };
        public List<string> Permissions => new List<string>() { "uc.kit" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
            UCPlayer ucplayer = UCPlayer.FromIRocketPlayer(caller);

            string op = "";
            string property = "";
            string kitName = "";
            string newValue = "";
            string targetPlayer = "";

            if (command.Length == 1)
            {
                kitName = command[0];

                if (!KitManager.KitExists(kitName, out Kit kit)) // create kit
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
                                ucplayer.SendChat("request_kit_given", kit.DisplayName.ToUpper());

                                if (branchChanged)
                                {
                                    ucplayer.Branch = kit.Branch;
                                    ucplayer.SendChat("branch_changed", F.TranslateBranch(kit.Branch, ucplayer).ToUpper());
                                }
                                PlayerManager.Save();
                                return;
                            }
                            else
                            {
                                player.SendChat("kit_e_cooldown", cooldown.ToString());
                                return;
                            }
                        }
                        else
                        {
                            player.SendChat("kit_e_wrongteam", kitName);
                            return;
                        }
                    }
                    else
                    {
                        player.SendChat("kit_e_notallowed", kitName);
                        return;
                    }
                }
                else
                {
                    player.SendChat("kit_e_noexist", kitName);
                    return;
                }
            }

            if (command.Length != 1 && !player.OnDuty())
            {
                player.SendChat("kits_notonduty", kitName);
                return;
            }
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
                        for (int i = 0; i < KitManager.ActiveObjects.Count; i++)
                        {
                            if (KitManager.ActiveObjects[i].SignTexts == null || KitManager.ActiveObjects[i].SignTexts.Count == 0) continue;
                            for (int n = 0; n < KitManager.ActiveObjects[i].SignTexts.Values.Count; n++)
                            {
                                if (KitManager.ActiveObjects[i].SignTexts.Values.ElementAt(n).ToLower().Contains(kitName))
                                {
                                    if (counter > 0) sb.Append(", ");
                                    sb.Append(KitManager.ActiveObjects[i].Name);
                                    counter++;
                                    break;
                                }
                            }
                            if (counter > 8) break;
                        }
                    }
                    catch (Exception ex)
                    {
                        F.LogError("Error searching for kit names.");
                        F.LogError(ex);
                        sb.Append("<color=#dd1111>ERROR</color>");
                    }
                    if (sb.Length == 0)
                    {
                        sb.Append("--");
                    }
                    player.SendChat("kit_search_results", sb.ToString());
                }
                // create kit
                else if (op == "create" || op == "c")
                {
                    if (!KitManager.KitExists(kitName, out var kit)) // create kit
                    {
                        KitManager.CreateKit(kitName, KitManager.ItemsFromInventory(player), KitManager.ClothesFromInventory(player));
                        RequestSigns.InvokeLangUpdateForSignsOfKit(kitName);
                        player.SendChat("kit_created", kitName);
                        return;
                    }
                    else // overwrite kit
                    {
                        KitManager.OverwriteKitItems(kit.Name, KitManager.ItemsFromInventory(player), KitManager.ClothesFromInventory(player));
                        RequestSigns.InvokeLangUpdateForSignsOfKit(kitName);
                        player.SendChat("kit_overwritten", kitName);
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
                        player.SendChat("kit_deleted", kitName);
                        return;
                    }
                    else // error
                    {
                        player.SendChat("kit_e_noexist", kitName);
                        return;
                    }
                }
                else if(op == "give")
                {
                    if(KitManager.KitExists(kitName, out Kit kit))
                    {
                        bool branchChanged = false;
                        if (KitManager.HasKit(player.CSteamID, out var oldkit) && kit.Branch != EBranch.DEFAULT && oldkit.Branch != kit.Branch)
                            branchChanged = true;

                        KitManager.GiveKit(ucplayer, kit);
                        ucplayer.SendChat("request_kit_given", kit.DisplayName.ToUpper());

                        if (branchChanged)
                        {
                            ucplayer.Branch = kit.Branch;
                            ucplayer.SendChat("branch_changed", F.TranslateBranch(kit.Branch, ucplayer).ToUpper());
                        }

                        PlayerManager.Save();
                    }
                    else // error
                    {
                        player.SendChat("kit_e_noexist", kitName);
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
                            F.Log(text);
                            if (KitManager.UpdateText(command[2], text, command[3]))
                                player.SendChat("kit_setprop", "sign text", command[2], command[3] + " : " + text);
                            else
                                player.SendChat("kit_e_noexist", command[2]);
                            return;
                        }
                        else
                        {
                            player.SendChat("kit_e_set_sign_syntax", command[2]);
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
                            player.SendChat("kit_e_invalidarg_not_allowed", property);
                            return;
                        }
                        if (!parsed) // error - invalid argument value
                        {
                            player.SendChat("kit_e_invalidarg", newValue, property);
                            return;
                        }
                        if (!foundproperty || !set) // error - invalid property name
                        {
                            player.SendChat("kit_e_invalidprop", property);
                            return;
                        }
                        // success
                        player.SendChat("kit_setprop", property, kitName, newValue);
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
                    } else
                    {
                        player.SendChat("kit_e_noexist", kitName);
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
                        player.SendChat("kit_e_noexist", kitName);
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
                                    player.SendChat("kit_e_alreadyaccess", targetPlayer, kitName);
                                    return;
                                }
                                //success
                                FPlayerName names = Data.DatabaseManager.GetUsernames(steamid);
                                player.SendChat("kit_accessgiven", names.CharacterName, kitName);
                                KitManager.GiveAccess(steamid, kit.Name);
                                return;
                            }
                        } else
                        {
                            player.SendChat("kit_e_noplayer", targetPlayer);
                            return;
                        }
                    }
                    // error - player already has access
                    if (KitManager.HasAccess(target.CSteamID.m_SteamID, kit.Name))
                    {
                        player.SendChat("kit_e_alreadyaccess", targetPlayer, kitName);
                        return;
                    }

                    //success
                    FPlayerName name = F.GetPlayerOriginalNames(target.Player);
                    player.SendChat("kit_accessgiven", name.CharacterName, kitName);
                    KitManager.GiveAccess(target.Steam64, kit.Name);
                    RequestSigns.InvokeLangUpdateForSignsOfKit(target.Player.channel.owner, kitName);
                    return;
                }
                // remove player access to kit
                if (op == "removeaccess" || op == "removea")
                {
                    if (!KitManager.KitExists(kitName, out var kit))
                    {
                        player.SendChat("kit_e_noexist", kitName);
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
                                    player.SendChat("kit_e_alreadyaccess", targetPlayer, kitName);
                                    return;
                                }
                                //success
                                FPlayerName names = Data.DatabaseManager.GetUsernames(steamid);
                                player.SendChat("kit_accessremoved", names.CharacterName, kitName);
                                KitManager.RemoveAccess(steamid, kit.Name);
                                return;
                            }
                        }
                        else
                        {
                            player.SendChat("kit_e_noplayer", targetPlayer);
                            return;
                        }
                    }
                    // error - player already has no access
                    if (!KitManager.HasAccess(target.CSteamID.m_SteamID, kit.Name))
                    {
                        player.SendChat("kit_e_noaccess", target.CharacterName, kitName);
                        return; 
                    }

                    //success
                    FPlayerName name = F.GetPlayerOriginalNames(target.Player);
                    player.SendChat("kit_accessremoved", name.CharacterName, kitName);
                    KitManager.RemoveAccess(target.Steam64, kit.Name);
                    RequestSigns.InvokeLangUpdateForSignsOfKit(target.Player.channel.owner, kitName);
                    return;
                }
            }
            else
            {
                player.SendChat("correct_usage", "/kit <create|delete|set>");
            }
        }
    }
}
