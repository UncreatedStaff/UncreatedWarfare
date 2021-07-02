using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public async void Execute(IRocketPlayer caller, string[] command)
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
                kitName = command[1];

                if (!KitManager.KitExists(kitName, out var kit)) // create kit
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

                                KitManager.GiveKit(player, kit);
                                ucplayer.Message("request_kit_given", kit.Name);

                                if (branchChanged)
                                {
                                    ucplayer.Branch = kit.Branch;
                                    ucplayer.Message("branch_changed", F.TranslateBranch(kit.Branch, ucplayer).ToUpper());
                                }

                                PlayerManager.Save();
                                return;
                            }
                            else
                            {
                                player.Message("kit_e_cooldown", cooldown.ToString());
                                return;
                            }
                        }
                        else
                        {
                            player.Message("kit_e_wrongteam", kitName);
                            return;
                        }
                    }
                    else
                    {
                        player.Message("kit_e_notallowed", kitName);
                        return;
                    }
                }
                else
                {
                    player.Message("kit_e_noexist", kitName);
                    return;
                }
            }

            if (command.Length != 1 && !player.OnDuty())
            {
                player.Message("kits_notonduty", kitName);
                return;
            }

            if (command.Length == 2)
            {
                op = command[0];
                kitName = command[1];
                // create kit
                if (op.ToLower() == "create" || op.ToLower() == "c")
                {
                    if (!KitManager.KitExists(kitName, out var kit)) // create kit
                    {
                        KitManager.CreateKit(kitName, KitManager.ItemsFromInventory(player), KitManager.ClothesFromInventory(player));
                        await RequestSigns.InvokeLangUpdateForSignsOfKit(kitName);
                        player.Message("kit_created", kitName);
                        return;
                    }
                    else // overwrite kit
                    {
                        KitManager.OverwriteKitItems(kit.Name, KitManager.ItemsFromInventory(player), KitManager.ClothesFromInventory(player));
                        await RequestSigns.InvokeLangUpdateForSignsOfKit(kitName);
                        player.Message("kit_overwritten", kitName);
                        return;
                    }
                }
                // delete kit
                if (op.ToLower() == "delete" || op.ToLower() == "d")
                {
                    if (KitManager.KitExists(kitName, out _))
                    {
                        KitManager.DeleteKit(kitName);
                        
                        RequestSigns.RemoveRequestSigns(kitName);
                        player.Message("kit_deleted", kitName);
                        return;
                    }
                    else // error
                    {
                        player.Message("kit_e_noexist", kitName);
                        return;
                    }
                }
                if(op.ToLower() == "give")
                {
                    if(KitManager.KitExists(kitName, out Kit kit))
                    {
                        bool branchChanged = false;
                        if (KitManager.HasKit(player.CSteamID, out var oldkit) && kit.Branch != EBranch.DEFAULT && oldkit.Branch != kit.Branch)
                            branchChanged = true;

                        KitManager.GiveKit(player, kit);
                        ucplayer.Message("request_kit_given", kit.Name);

                        if (branchChanged)
                        {
                            ucplayer.Branch = kit.Branch;
                            ucplayer.Message("branch_changed", F.TranslateBranch(kit.Branch, ucplayer).ToUpper());
                        }

                        PlayerManager.Save();
                    }
                    else // error
                    {
                        player.Message("kit_e_noexist", kitName);
                        return;
                    }
                }
                return;
            }
            else if (command.Length == 5)
            {
                if (command[0].ToLower() == "set" || command[0].ToLower() == "s")
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
                            F.Log(text);
                            if (await KitManager.UpdateText(command[2], text, command[3]))
                                player.Message("kit_setprop", "sign text", command[2], command[3] + " : " + text);
                            else
                                player.Message("kit_e_noexist", command[2]);
                            return;
                        }
                        else
                        {
                            player.Message("kit_e_set_sign_syntax", command[2]);
                            return;
                        }
                    }
                }
            }
            // change kit property
            else if (command.Length == 4)
            {
                op = command[0];
                property = command[1];
                kitName = command[2];
                newValue = command[3];

                if (command[0].ToLower() == "set" || command[0].ToLower() == "s")
                {
                    if (!KitManager.SetProperty(x => x.Name == kitName, property, newValue, out bool founditem, out bool set, out bool parsed, out bool foundproperty, out bool allowedToChange))
                    {
                        if (!founditem) // error - kit does not exist
                        {
                            player.Message("kit_e_noexist", kitName);
                            return;
                        }
                        if (!allowedToChange) // error - invalid argument value
                        {
                            player.Message("kit_e_invalidarg_not_allowed", property);
                            return;
                        }
                        if (!parsed) // error - invalid argument value
                        {
                            player.Message("kit_e_invalidarg", newValue, property);
                            return;
                        }
                        if (!foundproperty || !set) // error - invalid property name
                        {
                            player.Message("kit_e_invalidprop", property);
                            return;
                        }
                        return;
                    }
                    else
                    {
                        // success
                        player.Message("kit_setprop", property, kitName, newValue);
                        await RequestSigns.InvokeLangUpdateForSignsOfKit(kitName);
                        return;
                    }
                }
            }
            else if (command.Length == 3)
            {
                op = command[0];
                targetPlayer = command[1];
                kitName = command[2];

                // give player access to kit
                if (op.ToLower() == "giveaccess" || op.ToLower() == "givea")
                {
                    if (!KitManager.KitExists(kitName, out var kit))
                    {
                        player.Message("kit_e_noexist", kitName);
                        return;
                    }

                    UnturnedPlayer target = UnturnedPlayer.FromName(targetPlayer);

                    // error - no player found
                    if (target == null)
                    {
                        player.Message("kit_e_noplayer", targetPlayer);
                        return;
                    }
                    // error - player already has access
                    if (KitManager.HasAccess(player.CSteamID.m_SteamID, kit.Name))
                    {
                        player.Message("kit_e_alreadyaccess", targetPlayer, kitName);
                        return;
                    }

                    //success
                    player.Message("kit_accessgiven", targetPlayer, kitName);
                    KitManager.GiveAccess(player.CSteamID.m_SteamID, kit.Name);
                    await RequestSigns.InvokeLangUpdateForSignsOfKit(target.Player.channel.owner, kitName);
                    return;
                }
                // remove player access to kit
                if (op.ToLower() == "removeaccess" || op.ToLower() == "removea")
                {
                    if (!KitManager.KitExists(kitName, out var kit))
                    {
                        player.Message("kit_e_noexist", kitName);
                        return;
                    }

                    UnturnedPlayer target = UnturnedPlayer.FromName(targetPlayer);

                    // error - no player found
                    if (target == null)
                    {
                        player.Message("kit_e_noplayer", targetPlayer);
                        return;
                    }
                    // error - player already has no access
                    if (!KitManager.HasAccess(player.CSteamID.m_SteamID, kit.Name))
                    {
                        player.Message("kit_e_noaccess", targetPlayer, kitName);
                        return; 
                    }

                    //success
                    player.Message("kit_accessremoved", targetPlayer, kitName);
                    KitManager.RemoveAccess(player.CSteamID.m_SteamID, kit.Name);
                    await RequestSigns.InvokeLangUpdateForSignsOfKit(target.Player.channel.owner, kitName);
                    return;
                }
            }
            else
            {
                player.Message("correct_usage", "/kit <create|delete|set>");
            }
        }
    }
}
