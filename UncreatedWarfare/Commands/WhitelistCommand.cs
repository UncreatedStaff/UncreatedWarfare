﻿using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;

namespace Uncreated.Warfare.Commands
{
    public class WhitelistCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "whitelist";
        public string Help => "Whitelists items";
        public string Syntax => "/whitelist";
        private readonly List<string> _aliases = new List<string>(2) { "wl", "wh" };
        public List<string> Aliases => _aliases;
        private readonly List<string> _permissions = new List<string>(1) { "uc.whitelist" };
		public List<string> Permissions => _permissions;
        public void Execute(IRocketPlayer caller, string[] arguments)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            UnturnedPlayer player = (UnturnedPlayer)caller;
            if (!Data.Gamemode.UseWhitelist)
            {
                player.SendChat("command_e_gamemode");
                return;
            }
            if (arguments.Length == 2)
            {
                if (arguments[0].ToLower() == "add")
                {
                    if (UInt16.TryParse(arguments[1], System.Globalization.NumberStyles.Any, Data.Locale, out ushort itemID))
                    {
                        if (Assets.find(EAssetType.ITEM, itemID) is ItemAsset asset)
                        {
                            if (!Whitelister.IsWhitelisted(asset.GUID, out _))
                            {
                                Whitelister.AddItem(asset.GUID);
                                ActionLog.Add(EActionLogType.ADD_WHITELIST, $"{asset.itemName} / {asset.id} / {asset.GUID:N}", player.CSteamID.m_SteamID);
                                player.SendChat("whitelist_added", arguments[1]);
                            }
                            else
                                player.SendChat("whitelist_e_exist", arguments[1]);
                        }
                        else
                            player.SendChat("whitelist_e_invalidid", arguments[1]);
                    }
                    else
                        player.SendChat("whitelist_e_invalidid", arguments[1]);
                }
                else if (arguments[0].ToLower() == "remove")
                {
                    if (UInt16.TryParse(arguments[1], System.Globalization.NumberStyles.Any, Data.Locale, out ushort itemID))
                    {
                        if (Assets.find(EAssetType.ITEM, itemID) is ItemAsset asset)
                        {
                            if (Whitelister.IsWhitelisted(asset.GUID, out _))
                            {
                                Whitelister.RemoveItem(asset.GUID);
                                ActionLog.Add(EActionLogType.REMOVE_WHITELIST, $"{asset.itemName} / {asset.id} / {asset.GUID:N}", player.CSteamID.m_SteamID);
                                player.SendChat("whitelist_removed", arguments[1]);
                            }
                            else
                                player.SendChat("whitelist_e_noexist", arguments[1]);
                        }
                        else
                            player.SendChat("whitelist_e_invalidid", arguments[1]);
                    }
                    else
                        player.SendChat("whitelist_e_invalidid", arguments[1]);
                }
                else
                    player.SendChat("correct_usage", "/whitelist <add|remove|set>");
            }
            else if (arguments.Length == 4)
            {
                if (arguments[0].ToLower() == "set")
                {
                    if (arguments[1].ToLower() == "maxamount" || arguments[1].ToLower() == "a")
                    {
                        if (UInt16.TryParse(arguments[2], System.Globalization.NumberStyles.Any, Data.Locale, out ushort itemID))
                        {
                            if (Assets.find(EAssetType.ITEM, itemID) is ItemAsset asset)
                            {
                                if (UInt16.TryParse(arguments[3], System.Globalization.NumberStyles.Any, Data.Locale, out ushort amount))
                                {
                                    if (Whitelister.IsWhitelisted(asset.GUID, out _))
                                    {
                                        Whitelister.SetAmount(asset.GUID, amount);
                                        ActionLog.Add(EActionLogType.SET_WHITELIST_MAX_AMOUNT, $"{asset.itemName} / {asset.id} / {asset.GUID:N} set to {amount}", player.CSteamID.m_SteamID);
                                        player.SendChat("whitelist_removed", arguments[2]);
                                    }
                                    else
                                        player.SendChat("whitelist_e_noexist", arguments[2]);
                                }
                                else
                                    player.SendChat("whitelist_e_invalidamount", arguments[3]);
                            }
                            else
                                player.SendChat("whitelist_e_invalidid", arguments[2]);
                        }
                        else
                            player.SendChat("whitelist_e_invalidid", arguments[2]);
                    }
                    else
                        player.SendChat("correct_usage", "/whitelist set <amount|salvagable> <value>");
                }
                else
                    player.SendChat("correct_usage", "/whitelist <add|remove|set>");
            }
            else
                player.SendChat("correct_usage", "/whitelist <add|remove|set>");
        }
    }
}