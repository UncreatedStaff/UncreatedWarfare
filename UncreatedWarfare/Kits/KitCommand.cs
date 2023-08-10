using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SDG.Unturned;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Players;
using Uncreated.SQL;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Sync;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Commands;
public sealed class KitCommand : AsyncCommand
{
    private const string Syntax = "/kit <keybind|search|skills|create|delete|give|set|giveaccess|removeacces|copyfrom|createloadout>";
    private const string Help = "Admin command to manage kits; creating, deleting, editing, and giving/removing access is done through this command.";

    public KitCommand() : base("kit", EAdminType.MEMBER)
    {
        Structure = new CommandStructure
        {
            Description = Help,
            Parameters = new CommandParameter[]
            {
                new CommandParameter("Keybind")
                {
                    Aliases = new string[] { "hotkey", "bind" },
                    Description = "Add or remove default keybinds for this kit.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Add")
                        {
                            Aliases = new string[] { "create", "new" },
                            Description = "Add held item as a default keybind at [slot].",
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("Slot", typeof(byte))
                            }
                        },
                        new CommandParameter("Remove")
                        {
                            Aliases = new string[] { "delete", "cancel" },
                            Description = "Remove held item as a default keybind at [slot].",
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("Slot", typeof(byte))
                            }
                        },
                        new CommandParameter("Slot", typeof(byte))
                    }
                },
                new CommandParameter("Layout")
                {
                    Aliases = new string[] { "loadout", "customize" },
                    Description = "Set how you want your items organized when you receive your kit.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Save")
                        {
                            IsOptional = true,
                            Aliases = new string[] { "confirm", "keep" },
                            Description = "Saves your current inventory as your layout for this kit."
                        },
                        new CommandParameter("Reset")
                        {
                            IsOptional = true,
                            Aliases = new string[] { "delete", "cancel" },
                            Description = "Resets your layout for this kit."
                        }
                    }
                },
                new CommandParameter("Search")
                {
                    Aliases = new string[] { "find" },
                    Permission = EAdminType.STAFF,
                    Description = "Finds a kit Id from a localized name.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Term", typeof(string))
                        {
                            IsRemainder = true
                        }
                    }
                },
                new CommandParameter("Create")
                {
                    Aliases = new string[] { "c", "override" },
                    Permission = EAdminType.STAFF,
                    Description = "Creates or overwrites a kit. Class is not required to overwrite a kit.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Id", typeof(Kit))
                        {
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("Class", typeof(Class))
                                {
                                    Parameters = new CommandParameter[]
                                    {
                                        new CommandParameter("Type", "Public", "Elite", "Loadout", "Special")
                                        {
                                            IsOptional = true,
                                            Parameters = new CommandParameter[]
                                            {
                                                new CommandParameter("Faction", typeof(FactionInfo))
                                                {
                                                    IsOptional = true
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                new CommandParameter("Delete")
                {
                    Aliases = new string[] { "d", "remove" },
                    Permission = EAdminType.STAFF,
                    Description = "Delete a kit.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Kit", typeof(Kit))
                    }
                },
                new CommandParameter("Give")
                {
                    Aliases = new string[] { "g" },
                    Permission = EAdminType.STAFF,
                    Description = "Gives the caller a kit.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Kit", typeof(Kit))
                        {
                            IsOptional = true
                        }
                    }
                },
                new CommandParameter("Set")
                {
                    Aliases = new string[] { "s" },
                    Permission = EAdminType.STAFF,
                    Description = "Sets a property of a kit.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Sign")
                        {
                            Aliases = new string[] { "text" },
                            Description = "Sets the sign text of a kit for a language. Default language is " + L.Default + ".",
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("Kit", typeof(Kit))
                                {
                                    Parameters = new CommandParameter[]
                                    {
                                        new CommandParameter("Language", typeof(string))
                                        {
                                            Parameters = new CommandParameter[]
                                            {
                                                new CommandParameter("Text", typeof(string))
                                                {
                                                    IsRemainder = true
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        new CommandParameter("Property", typeof(string))
                        {
                            Aliases = new string[] { "level", "lvl", "faction", "team", "group" },
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("Kit", typeof(Kit))
                                {
                                    Parameters = new CommandParameter[]
                                    {
                                        new CommandParameter("Value", typeof(object))
                                        {
                                            IsRemainder = true
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                new CommandParameter("GiveAccess")
                {
                    Aliases = new string[] { "givea", "ga" },
                    Permission = EAdminType.STAFF,
                    Description = "Give a player access to a non-public kit.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Player", typeof(IPlayer))
                        {
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("Kit", typeof(Kit))
                                {
                                    Parameters = new CommandParameter[]
                                    {
                                        new CommandParameter("AccessType", typeof(KitAccessType))
                                        {
                                            IsOptional = true
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                new CommandParameter("RemoveAccess")
                {
                    Aliases = new string[] { "remvoea", "ra" },
                    Permission = EAdminType.STAFF,
                    Description = "Remove a player's access to a non-public kit.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Player", typeof(IPlayer))
                        {
                            ChainDisplayCount = 2,
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("Kit", typeof(Kit))
                            }
                        }
                    }
                },
                new CommandParameter("CopyFrom")
                {
                    Aliases = new string[] { "cf", "copy" },
                    Permission = EAdminType.STAFF,
                    Description = "Create a copy of a kit with a different id.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Kit", typeof(Kit))
                        {
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("Id", typeof(string))
                            }
                        }
                    }
                },
                new CommandParameter("CreateLoadout")
                {
                    Aliases = new string[] { "cloadout", "cl" },
                    Permission = EAdminType.STAFF,
                    Description = "Create a loadout with some default parameters.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Player", typeof(IPlayer))
                        {
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("Class", typeof(Class))
                                {
                                    Parameters = new CommandParameter[]
                                    {
                                        new CommandParameter("SignText", typeof(string))
                                        {
                                            IsOptional = false,
                                            IsRemainder = true
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                new CommandParameter("Skills")
                {
                    Aliases = new string[] { "skillset", "skillsets" },
                    Permission = EAdminType.STAFF,
                    Description = "Modify the skillset overrides on a kit.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Add")
                        {
                            Aliases = new string[] { "set" },
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("Skill", typeof(string))
                                {
                                    Parameters = new CommandParameter[]
                                    {
                                        new CommandParameter("Level", typeof(byte))
                                    }
                                }
                            }
                        },
                        new CommandParameter("Remove")
                        {
                            Aliases = new string[] { "delete", "clear" },
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("Skill", typeof(string))
                            }
                        }
                    }
                },
                new CommandParameter("Upgrade")
                {
                    Aliases = new string[] { "update", "upg" },
                    Permission = EAdminType.STAFF,
                    Description = "Upgrade an old loadout.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Kit", typeof(Kit))
                        {
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("Class", typeof(Class))
                            }
                        }
                    }
                },
                new CommandParameter("Unlock")
                {
                    Aliases = new string[] { "unl", "unlk" },
                    Permission = EAdminType.STAFF,
                    Description = "Unlock a completed loadout.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Kit", typeof(Kit))
                    }
                },
                new CommandParameter("Lock")
                {
                    Permission = EAdminType.STAFF,
                    Description = "Lock a setup loadout.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Kit", typeof(Kit))
                    }
                }
            }
        };
    }

    public override async Task Execute(CommandInteraction ctx, CancellationToken token)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertGamemode<IKitRequests>();
        KitManager? manager = KitManager.GetSingletonQuick();
        if (manager == null)
            throw ctx.SendGamemodeError();

        ctx.AssertArgs(1, ctx.HasPermission(EAdminType.MEMBER, PermissionComparison.AtMost)
                ? "/kit <bind|layout> - Customize your experience with kits."
                : (Syntax + " - " + Help));
        ctx.AssertHelpCheck(0, ctx.HasPermission(EAdminType.MEMBER, PermissionComparison.AtMost)
                ? "/kit <bind|layout> - Customize your experience with kits."
                : (Syntax + " - " + Help));

        if (ctx.MatchParameter(0, "hotkey", "keybind", "bind"))
        {
            ctx.AssertRanByPlayer();

            bool add = ctx.MatchParameter(1, "add", "create", "new") || ctx.HasArgsExact(2);
            if ((add || ctx.MatchParameter(1, "remove", "delete", "cancel")) && ctx.TryGet(ctx.ArgumentCount - 1, out byte slot) && KitEx.ValidSlot(slot))
            {
                await ctx.Caller.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    PlayerEquipment equipment = ctx.Caller.Player.equipment;
                    if (add)
                    {
                        SqlItem<Kit>? proxy = ctx.Caller.ActiveKit;
                        IItemJar? item = await manager.GetHeldItemFromKit(ctx.Caller, token).ConfigureAwait(false);
                        if (proxy is null)
                        {
                            await UCWarfare.ToUpdate(token);
                            throw ctx.Reply(T.AmmoNoKit);
                        }
                        await proxy.Enter(token).ConfigureAwait(false);
                        try
                        {
                            await UCWarfare.ToUpdate(token);
                            Kit? kit = proxy.Item;
                            if (kit is null)
                                throw ctx.Reply(T.AmmoNoKit);

                            if (item == null)
                                throw ctx.Reply(T.KitHotkeyNotHoldingItem);

                            ItemAsset? asset = item is IItem i2
                                ? Assets.find<ItemAsset>(i2.Item)
                                : ((IKitItem)item).GetItem(kit, TeamManager.GetFactionSafe(ctx.Caller.GetTeam()), out _, out _);
                            if (asset == null)
                                throw ctx.Reply(T.KitHotkeyNotHoldingItem);

                            if (!KitEx.CanBindHotkeyTo(asset, item.Page))
                                throw ctx.Reply(T.KitHotkeyNotHoldingValidItem, asset);

                            bool added = await KitManager.AddHotkey(proxy.LastPrimaryKey, ctx.CallerID, slot, item, token).ConfigureAwait(false);
                            await UCWarfare.ToUpdate(token);
                            if (!added)
                                throw ctx.SendUnknownError();
                            if (ctx.Caller.HotkeyBindings != null)
                            {
                                // remove duplicates / conflicts
                                ctx.Caller.HotkeyBindings.RemoveAll(x =>
                                    x.Kit.Key == proxy.LastPrimaryKey.Key && (x.Slot == slot ||
                                                                              x.Item.X == item.X &&
                                                                              x.Item.Y == item.Y &&
                                                                              x.Item.Page == item.Page));
                            }
                            (ctx.Caller.HotkeyBindings ??= new List<HotkeyBinding>(32)).Add(new HotkeyBinding(proxy.LastPrimaryKey, slot, item));

                            byte index = KitEx.GetHotkeyIndex(slot);

                            if (KitEx.CanBindHotkeyTo(asset, (Page)equipment.equippedPage))
                            {
                                equipment.ServerBindItemHotkey(index, asset,
                                    equipment.equippedPage, equipment.equipped_x,
                                    equipment.equipped_y);
                            }
                            throw ctx.Reply(T.KitHotkeyBinded, asset, slot, kit);
                        }
                        finally
                        {
                            proxy.Release();
                        }
                    }
                    else
                    {
                        SqlItem<Kit>? proxy = ctx.Caller.ActiveKit;
                        if (proxy is null)
                        {
                            await UCWarfare.ToUpdate(token);
                            throw ctx.Reply(T.AmmoNoKit);
                        }

                        await proxy.Enter(token).ConfigureAwait(false);
                        try
                        {
                            Kit? kit = proxy.Item;
                            if (kit == null)
                            {
                                await UCWarfare.ToUpdate(token);
                                throw ctx.Reply(T.AmmoNoKit);
                            }
                            bool removed = await KitManager.RemoveHotkey(proxy.LastPrimaryKey, ctx.CallerID, slot, token).ConfigureAwait(false);
                            await UCWarfare.ToUpdate(token);
                            if (!removed)
                                throw ctx.Reply(T.KitHotkeyNotFound, slot, kit);

                            byte index = KitEx.GetHotkeyIndex(slot);
                            equipment.ServerClearItemHotkey(index);
                            throw ctx.Reply(T.KitHotkeyUnbinded, slot, kit);
                        }
                        finally
                        {
                            proxy.Release();
                        }
                    }
                }
                finally
                {
                    ctx.Caller.PurchaseSync.Release();
                }
            }
            throw ctx.SendCorrectUsage("/kit keybind [add (default)|remove] <key (3-9 or 0)>");
        }
        if (ctx.MatchParameter(0, "layout", "loadout", "customize"))
        {
            ctx.AssertRanByPlayer();

            ctx.AssertHelpCheck(1, "/kit layout <save|reset> - Cutomize your kit's item layout.");
            if (ctx.MatchParameter(1, "save", "confirm", "keep"))
            {
                await ctx.Caller.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    SqlItem<Kit>? proxy = ctx.Caller.ActiveKit;
                    if (proxy?.Item is not { } kit)
                    {
                        throw ctx.Reply(T.AmmoNoKit);
                    }
                    if (!KitManager.ShouldAllowLayouts(kit))
                        throw ctx.Reply(T.KitLayoutsNotSupported, kit);
                    await KitManager.SaveLayout(ctx.Caller, proxy, false, true, token).ConfigureAwait(false);
                    await UCWarfare.ToUpdate(token);
                    throw ctx.Reply(T.KitLayoutSaved, kit);
                }
                finally
                {
                    ctx.Caller.PurchaseSync.Release();
                }
            }

            if (ctx.MatchParameter(1, "reset", "delete", "cancel"))
            {
                await ctx.Caller.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    SqlItem<Kit>? proxy = ctx.Caller.ActiveKit;
                    if (proxy?.Item is not { } kit)
                    {
                        throw ctx.Reply(T.AmmoNoKit);
                    }
                    if (!KitManager.ShouldAllowLayouts(kit))
                        throw ctx.Reply(T.KitLayoutsNotSupported, kit);
                    if (kit.Items != null)
                    {
                        await UCWarfare.ToUpdate(token);
                        KitManager.TryReverseLayoutTransformations(ctx.Caller, kit.Items, kit.PrimaryKey);
                    }
                    await KitManager.ResetLayout(ctx.Caller, kit.PrimaryKey, false, token);
                    await UCWarfare.ToUpdate(token);
                    throw ctx.Reply(T.KitLayoutReset, kit);
                }
                finally
                {
                    ctx.Caller.PurchaseSync.Release();
                }
            }
            throw ctx.SendCorrectUsage("/kit layout <save|reset>");
        }

        bool fav = ctx.MatchParameter(0, "favorite", "favourite", "favour", "favor", "fav", "star");
        if (fav || ctx.MatchParameter(0, "unfavorite", "unfavourite", "unfavour", "unfavor", "unfav", "unstar"))
        {
            ctx.AssertRanByPlayer();
            
            ctx.AssertHelpCheck(1, "/kit <fav|unfav> (look at kit sign <b>or</b> [kit id]) - Favorite or unfavorite your kit or loadout.");
            SqlItem<Kit>? proxy;
            UCPlayer player = ctx.Caller;
            UCPlayer.TryApplyViewLens(ref player);
            if (ctx.TryGetRange(1, out string kitName))
            {
                proxy = await manager.FindKit(kitName, token).ConfigureAwait(false);
            }
            else if (ctx.TryGetTarget(out BarricadeDrop drop))
            {
                kitName = drop.interactable is InteractableSign sign ? sign.text : null!;
                proxy = Signs.GetKitFromSign(drop, out int loadoutId);
                if (loadoutId > 0)
                    proxy = await KitManager.GetLoadout(player, loadoutId, token).ConfigureAwait(false);
            }
            else
                throw ctx.SendCorrectUsage("/kit favorite (look at kit sign <b>or</b> [kit id]) - Favorite your kit or loadout.");
            
            if (proxy?.Item is not { } kit)
            {
                await UCWarfare.ToUpdate(token);
                throw ctx.Reply(T.KitNotFound, kitName.Replace(Signs.Prefix, string.Empty));
            }
            await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (fav && KitManager.IsFavoritedQuick(kit.PrimaryKey, player))
                {
                    await UCWarfare.ToUpdate(token);
                    throw ctx.Reply(T.KitFavoriteAlreadyFavorited, kit);
                }
                else if (!fav && !KitManager.IsFavoritedQuick(kit.PrimaryKey, player))
                {
                    await UCWarfare.ToUpdate(token);
                    throw ctx.Reply(T.KitFavoriteAlreadyUnfavorited, kit);
                }
                else
                {
                    if (fav)
                        (player.KitMenuData.FavoriteKits ??= new List<PrimaryKey>(8)).Add(kit.PrimaryKey);
                    else if (player.KitMenuData.FavoriteKits != null)
                        player.KitMenuData.FavoriteKits.RemoveAll(x => x.Key == kit.PrimaryKey.Key);
                    player.KitMenuData.FavoritesDirty = true;
                    if (player.KitMenuData.FavoriteKits != null)
                        await manager.SaveFavorites(player, player.KitMenuData.FavoriteKits, token).ConfigureAwait(false);
                }
                await UCWarfare.ToUpdate(token);

                ctx.Reply(fav ? T.KitFavorited : T.KitUnfavorited, kit);
            }
            finally
            {
                player.PurchaseSync.Release();
            }


            await UCWarfare.ToUpdate(token);
            Signs.UpdateKitSigns(player, null);
            return;
        }
        ctx.AssertOnDuty();
        ctx.AssertPermissions(EAdminType.STAFF);

        if (ctx.MatchParameter(0, "search", "find"))
        {
            ctx.AssertHelpCheck(1, "/kit search <term> - Searches for the search term within display names of all kits and tells you the id of all results.");

            if (ctx.TryGetRange(1, out string searchTerm))
            {
                List<SqlItem<Kit>> res = await manager.FindKits(searchTerm, token, false);
                if (res.Count <= 0)
                    throw ctx.Reply(T.KitSearchResults, "--");
                ctx.Reply(T.KitSearchResults, string.Join(", ", res.Select(x => x.Item?.Id ?? x.LastPrimaryKey.ToString())));
            }
            else
                ctx.SendCorrectUsage("/kit <search|find> <term>");
        }
        else if (ctx.MatchParameter(0, "create", "c", "override"))
        {
            ctx.AssertRanByPlayer();

            ctx.AssertHelpCheck(1, "/kit <create|c|override> <id> <class> [type] [faction] - Creates (or overrides if it already exits) a kit with default values based on the items in your inventory and your clothes.");

            if (ctx.TryGet(1, out string kitName))
            {
                kitName = kitName.ToLowerInvariant();
                SqlItem<Kit>? proxy = await manager.FindKit(kitName, token, true);
                Kit kit;
                if (proxy?.Item != null) // overwrite
                {
                    await UCWarfare.ToUpdate(token);
                    ctx.Reply(T.KitConfirmOverride, proxy.Item, proxy.Item);
                    bool didConfirm = await CommandWaiter.WaitAsync(ctx.Caller, typeof(ConfirmCommand), 10000);
                    if (!didConfirm)
                    {
                        await UCWarfare.ToUpdate(token);
                        throw ctx.Reply(T.KitCancelOverride);
                    }
                    await proxy.Enter(token).ConfigureAwait(false);
                    try
                    {
                        kit = proxy.Item;
                        if (kit == null)
                            goto @new;
                        IKitItem[] oldItems = kit.Items;
                        kit.Items = UCInventoryManager.ItemsFromInventory(ctx.Caller, findAssetRedirects: true);
                        kit.ItemListCache = null;
                        kit.ClothingSetCache = null;
                        kit.WeaponText = KitManager.DetectWeaponText(kit);
                        kit.UpdateLastEdited(ctx.CallerID);
                        ctx.LogAction(ActionLogType.EditKit, "OVERRIDE ITEMS " + kit.Id + ".");
                        await proxy.SaveItem(token).ConfigureAwait(false);
                        UCWarfare.RunTask(KitManager.OnItemsChangedLayoutHandler, oldItems, kit, token, ctx: "Update layouts after changing items.");
                        await UCWarfare.ToUpdate();
                        KitManager.UpdateSigns(kit);
                        ctx.Reply(T.KitOverwrote, kit);
                    }
                    finally
                    {
                        proxy.Release();
                    }
                    return;
                }

                @new:
                FactionInfo? faction = null;
                Class @class = ctx.Caller.KitClass;
                if (@class == Class.None) @class = Class.Unarmed;
                KitType type = KitType.Public;
                bool def = ctx.MatchParameter(2, "default") || ctx.MatchParameter(2, Default);
                if (def || ctx.MatchParameter(2, "me") || ctx.TryGet(2, out @class))
                {
                    if (def || @class == Class.None) @class = Class.Unarmed;
                    if ((ctx.MatchParameter(3, "default") || ctx.MatchParameter(3, Default)) || ctx.TryGet(3, out type))
                    {
                        if (ctx.TryGet(4, out string factionId))
                        {
                            faction = TeamManager.FindFactionInfo(factionId);
                            if (faction == null)
                                throw ctx.Reply(T.FactionNotFoundCreateKit, factionId);
                        }
                    }
                    else if (ctx.HasArg(3))
                        throw ctx.Reply(T.TypeNotFoundCreateKit, ctx.Get(2)!);
                }
                else if (ctx.HasArg(2))
                    throw ctx.Reply(T.ClassNotFoundCreateKit, ctx.Get(2)!);

                if (@class == Class.None) @class = Class.Unarmed;
                kit = new Kit(kitName, @class, KitManager.GetDefaultBranch(@class), type, SquadLevel.Member, faction)
                {
                    Items = UCInventoryManager.ItemsFromInventory(ctx.Caller, findAssetRedirects: true)
                };
                kit.Creator = kit.LastEditor = ctx.CallerID;
                kit.WeaponText = KitManager.DetectWeaponText(kit);
                await manager.AddOrUpdate(kit, token).ConfigureAwait(false);
                ctx.LogAction(ActionLogType.CreateKit, kitName);
                await UCWarfare.ToUpdate(token);
                KitManager.UpdateSigns(kit);
                ctx.Reply(T.KitCreated, kit);
            }
            else
                ctx.SendCorrectUsage("/kit <create|c|override> <kit name>");
        }
        else if (ctx.MatchParameter(0, "delete", "d", "remove"))
        {
            ctx.AssertHelpCheck(1, "/kit <delete|d|remove> <id> - Deletes the kit with the provided id.");

            if (ctx.TryGet(1, out string kitName))
            {
                SqlItem<Kit>? proxy = await manager.FindKit(kitName, token, true);
                if (proxy?.Item != null)
                {
                    bool ld = proxy.Item.Type == KitType.Loadout;
                    await UCWarfare.ToUpdate(token);
                    ctx.Reply(T.KitConfirmDelete, proxy.Item, proxy.Item);
                    bool didConfirm = await CommandWaiter.WaitAsync(ctx.Caller, typeof(ConfirmCommand), 10000);
                    if (!didConfirm)
                    {
                        await UCWarfare.ToUpdate(token);
                        throw ctx.Reply(T.KitCancelDelete);
                    }

                    Kit? item = proxy.Item;
                    item.UpdateLastEdited(ctx.CallerID);
                    await proxy.Delete(token).ConfigureAwait(false);
                    ctx.LogAction(ActionLogType.DeleteKit, kitName);
                    await UCWarfare.ToUpdate();
                    ctx.Reply(T.KitDeleted, item);
                    if (!ld)
                        Signs.UpdateKitSigns(null, kitName);
                    else
                        Signs.UpdateLoadoutSigns(null);
                }
                else
                    ctx.Reply(T.KitNotFound, kitName);
            }
            else
                ctx.SendCorrectUsage("/kit <delete|d|remove> <kit name>");
        }
        else if (ctx.MatchParameter(0, "upgrade", "update", "upg"))
        {
            ctx.AssertHelpCheck(1, "/kit <upgrade|update|upg> <id> <new class> - Upgrades a loadout and prepares it for unlocking.");

            if (ctx.TryGet(2, out Class @class) && ctx.TryGet(1, out string kitName))
            {
                if (KitEx.ParseStandardLoadoutId(kitName) < 1 || kitName.Length < 18 || !ulong.TryParse(kitName.Substring(0, 17), NumberStyles.Number, Data.AdminLocale, out ulong playerId))
                    throw ctx.Reply(T.KitLoadoutIdBadFormat);
                
                SqlItem<Kit>? proxy = await manager.FindKit(kitName, token, true);
                if (proxy is { Item: { } kit })
                {
                    if (!kit.NeedsUpgrade)
                    {
                        if (kit.Season != UCWarfare.Season)
                        {
                            kit.Season = UCWarfare.Season;
                            await proxy.SaveItem(token).ConfigureAwait(false);
                            await UCWarfare.ToUpdate(token);
                            throw ctx.Reply(T.KitUpgraded, kit);
                        }
                        await UCWarfare.ToUpdate(token);
                        throw ctx.Reply(T.DoesNotNeedUpgrade, kit);
                    }

                    (_, StandardErrorCode err) = await manager.UpgradeLoadout(ctx.CallerID, playerId, @class, kitName, token).ConfigureAwait(false);
                    await UCWarfare.ToUpdate();
                    if (err != StandardErrorCode.Success)
                        throw ctx.SendUnknownError();
                    ctx.Reply(T.LoadoutUpgraded, kit, @class);
                    await manager.GiveKit(ctx.Caller, proxy, false, token).ConfigureAwait(false);
                }
                else
                    ctx.Reply(T.KitNotFound, kitName);
            }
            else
                ctx.SendCorrectUsage("/kit <upgrade|update|upg> <id> <new class>");
        }
        else if (ctx.MatchParameter(0, "unlock", "unl"))
        {
            ctx.AssertHelpCheck(1, "/kit <unlock|unl> <id> - Unlocks a loadout so it's owner can use it.");

            if (ctx.TryGet(1, out string kitName))
            {
                SqlItem<Kit>? proxy = await manager.FindKit(kitName, token, true);
                if (proxy is null)
                    throw ctx.Reply(T.KitNotFound);
                await proxy.Enter(token).ConfigureAwait(false);
                try
                {
                    if (proxy is { Item: { } kit })
                    {
                        if (!kit.NeedsSetup)
                        {
                            if (kit.Disabled)
                            {
                                kit.Disabled = false;
                                await proxy.SaveItem(token).ConfigureAwait(false);
                                await UCWarfare.ToUpdate(token);
                                throw ctx.Reply(T.KitUnlocked, kit);
                            }
                            await UCWarfare.ToUpdate(token);
                            throw ctx.Reply(T.DoesNotNeedUnlock, kit);
                        }
                    }
                    else
                        throw ctx.Reply(T.KitNotFound, kitName);
                }
                finally
                {
                    proxy.Release();
                }

                (_, StandardErrorCode err) = await manager.UnlockLoadout(ctx.CallerID, kitName, token).ConfigureAwait(false);
                if (err != StandardErrorCode.Success)
                    throw ctx.SendUnknownError();
                await proxy.Enter(token).ConfigureAwait(false);
                try
                {
                    if (proxy is { Item: { } kit })
                        ctx.Reply(T.KitUnlocked, kit);
                }
                finally
                {
                    proxy.Release();
                }
            }
            else
                ctx.SendCorrectUsage("/kit <unlock|unl> <id>");
        }
        else if (ctx.MatchParameter(0, "lock"))
        {
            ctx.AssertHelpCheck(1, "/kit <lock> <id> - Locks a loadout for staff review.");

            if (ctx.TryGet(1, out string kitName))
            {
                SqlItem<Kit>? proxy = await manager.FindKit(kitName, token, true);
                if (proxy is null)
                    throw ctx.Reply(T.KitNotFound);
                await proxy.Enter(token).ConfigureAwait(false);
                try
                {
                    if (proxy is { Item: { } kit })
                    {
                        if (kit.Type != KitType.Loadout && !kit.Disabled)
                        {
                            if (!kit.Disabled)
                            {
                                kit.Disabled = true;
                                await proxy.SaveItem(token).ConfigureAwait(false);
                                await UCWarfare.ToUpdate(token);
                                throw ctx.Reply(T.KitLocked, kit);
                            }
                            await UCWarfare.ToUpdate(token);
                            throw ctx.Reply(T.DoesNotNeedUnlock, kit);
                        }
                    }
                    else
                        throw ctx.Reply(T.KitNotFound, kitName);
                }
                finally
                {
                    proxy.Release();
                }

                (_, StandardErrorCode err) = await manager.LockLoadout(ctx.CallerID, kitName, token).ConfigureAwait(false);
                await UCWarfare.ToUpdate();
                if (err != StandardErrorCode.Success)
                    throw ctx.SendUnknownError();
                try
                {
                    if (proxy is { Item: { } kit })
                        ctx.Reply(T.KitLocked, kit);
                }
                finally
                {
                    proxy.Release();
                }
            }
            else
                ctx.SendCorrectUsage("/kit <unlock|unl> <id>");
        }
        else if (ctx.MatchParameter(0, "give", "g"))
        {
            ctx.AssertHelpCheck(1, "/kit <give|g> [id] (or look at a sign) - Equips you with the kit with the id provided.");

            ctx.AssertRanByPlayer();
            BarricadeDrop? drop = null;
            if (ctx.TryGet(1, out string kitName) || ctx.TryGetTarget(out drop))
            {
                SqlItem<Kit>? proxy = await manager.FindKit(kitName, token, true);
                if (proxy?.Item == null && drop != null)
                {
                    proxy = Signs.GetKitFromSign(drop, out int loadout);
                    if (loadout > 0)
                    {
                        UCPlayer pl = ctx.Caller;
                        UCPlayer.TryApplyViewLens(ref pl);
                        proxy = await KitManager.GetLoadout(pl, loadout, token).ConfigureAwait(false);
                    }
                }
                
                if (proxy?.Item != null)
                {
                    Class @class = proxy.Item.Class;
                    await manager.GiveKit(ctx.Caller, proxy, true, token).ConfigureAwait(false);
                    await UCWarfare.ToUpdate(token);
                    ctx.LogAction(ActionLogType.GiveKit, kitName);
                    ctx.Reply(T.RequestSignGiven, @class);
                }
                else
                    throw ctx.Reply(T.KitNotFound, kitName);
            }
            else
                ctx.SendCorrectUsage("/kit <give|g> [id]");
        }
        else if (ctx.MatchParameter(0, "set", "s"))
        {
            ctx.AssertHelpCheck(1, "/kit <set|s> <level|sign|property> <value> - Sets the level requirement, sign text, or other properties to value. To set default sign text use: /kit set sign <kit id> en-us <text>.");

            if (ctx.TryGet(3, out string newValue) && ctx.TryGet(2, out string kitName) && ctx.TryGet(1, out string property))
            {
                SqlItem<Kit>? proxy = await manager.FindKit(kitName, token, true);
                if (proxy?.Item != null)
                {
                    await proxy.Enter(token).ConfigureAwait(false);
                    try
                    {
                        if (proxy.Item == null)
                            throw ctx.Reply(T.KitNotFound, kitName);
                        if (ctx.MatchParameter(1, "level", "lvl"))
                        {
                            if (ctx.TryGet(3, out int level))
                            {
                                if (level == 0)
                                {
                                    UnlockRequirement[] ulr = proxy.Item.UnlockRequirements;
                                    do
                                    {
                                        int index = Array.FindIndex(ulr, x => x is LevelUnlockRequirement);
                                        if (index == -1)
                                            break;
                                        Util.RemoveFromArray(ref ulr, index);
                                        proxy.Item.UnlockRequirements = ulr;
                                    } while (true);
                                }
                                else
                                {
                                    UnlockRequirement[] ulr = proxy.Item.UnlockRequirements;
                                    int index = Array.FindIndex(ulr, x => x is LevelUnlockRequirement);
                                    UnlockRequirement req = new LevelUnlockRequirement { UnlockLevel = level };
                                    if (index == -1)
                                    {
                                        Util.AddToArray(ref ulr!, req);
                                        proxy.Item.UnlockRequirements = ulr;
                                    }
                                    else ((LevelUnlockRequirement)ulr[index]).UnlockLevel = level;
                                }
                                proxy.Item.UpdateLastEdited(ctx.CallerID);
                                await proxy.SaveItem(token).ConfigureAwait(false);
                                await UCWarfare.ToUpdate(token);
                                ctx.Reply(T.KitPropertySet, property, proxy.Item, newValue);
                                ctx.LogAction(ActionLogType.SetKitProperty, kitName + ": LEVEL >> " + newValue.ToUpper());
                                KitManager.UpdateSigns(proxy.Item);
                                ctx.Defer();
                            }
                            else
                                ctx.SendCorrectUsage("/kit <set|s> <level|lvl> <kitname> <value: integer>");
                        }
                        else if (ctx.MatchParameter(1, "sign", "text"))
                        {
                            ctx.AssertHelpCheck(2, "/kit <set|s> <sign> <language (default: " + L.Default + "> <text> - Sets the display text for the kit's kit sign.");

                            string language = newValue;
                            if (ctx.TryGetRange(4, out newValue))
                            {
                                newValue = newValue.Replace("\\n", "\n");
                                KitManager.SetTextNoLock(ctx.CallerID, proxy.Item, newValue, language);
                                await proxy.SaveItem(token).ConfigureAwait(false);
                                await UCWarfare.ToUpdate(token);
                                newValue = newValue.Replace('\n', '\\');
                                ctx.Reply(T.KitPropertySet, "sign text", proxy.Item, language + " : " + newValue);
                                ctx.LogAction(ActionLogType.SetKitProperty, kitName + ": SIGN TEXT >> \"" + newValue + "\"");
                                KitManager.UpdateSigns(proxy.Item);
                            }
                            else
                                ctx.SendCorrectUsage("/kit set sign <kitname> <language> <sign text>");
                        }
                        else if (ctx.MatchParameter(1, "faction", "team", "group"))
                        {
                            bool isNull = ctx.MatchParameter(3, "null", "none", "blank");
                            FactionInfo? faction = isNull ? null : TeamManager.FindFactionInfo(newValue);
                            if (faction != null || isNull)
                            {
                                proxy.Item.Faction = faction;
                                proxy.Item.UpdateLastEdited(ctx.CallerID);
                                await proxy.SaveItem(token).ConfigureAwait(false);
                                await UCWarfare.ToUpdate(token);
                                ctx.Reply(T.KitPropertySet, "faction", proxy.Item, faction?.GetName(Localization.GetDefaultLanguage())!);
                                ctx.LogAction(ActionLogType.SetKitProperty, kitName + ": FACTION >> " +
                                                                               (faction?.Name.ToUpper() ?? Translation.Null(TranslationFlags.NoRichText)));
                                KitManager.UpdateSigns(proxy.Item);
                            }
                            else
                            {
                                ctx.SendCorrectUsage("/kit set faction <faction id or search>");
                                ctx.ReplyString("Factions: <#aaa>" + string.Join(", ", TeamManager.Factions
                                    .OrderBy(x => x.PrimaryKey.Key)
                                    .Select(x => x.FactionId)) + "</color>.");
                            }
                        }
                        else
                        {
                            Kit kit = proxy.Item;
                            KitType prevType = kit.Type;
                            Class oldclass = kit.Class;
                            Branch oldbranch = kit.Branch;
                            float oldReqCooldown = kit.RequestCooldown;
                            float oldTeamLimit = kit.TeamLimit;
                            (SetPropertyResult result, MemberInfo? info) = await manager.SetProperty(proxy, property, newValue, false, token).ConfigureAwait(false);
                            if (info != null)
                                property = info.Name;
                            switch (result)
                            {
                                default:
                                    ctx.Reply(T.KitInvalidPropertyValue, newValue,
                                        info switch { FieldInfo i => i.FieldType, PropertyInfo i => i.PropertyType, _ => null! },
                                            property);
                                    return;
                                case SetPropertyResult.PropertyProtected:
                                    ctx.Reply(T.KitPropertyProtected, property);
                                    return;
                                case SetPropertyResult.PropertyNotFound:
                                case SetPropertyResult.TypeNotSettable:
                                    ctx.Reply(T.KitPropertyNotFound, property);
                                    return;
                                case SetPropertyResult.ObjectNotFound:
                                    ctx.Reply(T.KitNotFound, kitName);
                                    return;
                                case SetPropertyResult.Success:
                                    if (kit.Class != oldclass)
                                    {
                                        if (Mathf.Abs(KitManager.GetDefaultTeamLimit(oldclass) - oldTeamLimit) < 0.005f)
                                            kit.TeamLimit = KitManager.GetDefaultTeamLimit(kit.Class);
                                        if (KitManager.GetDefaultBranch(oldclass) == oldbranch)
                                            kit.Branch = KitManager.GetDefaultBranch(kit.Class);
                                        if (Mathf.Abs(KitManager.GetDefaultRequestCooldown(oldclass) - oldReqCooldown) < 0.25f)
                                            kit.RequestCooldown = KitManager.GetDefaultRequestCooldown(kit.Class);
                                    }
                                    proxy.Item.UpdateLastEdited(ctx.CallerID);
                                    await proxy.SaveItem(token).ConfigureAwait(false);
                                    await UCWarfare.ToUpdate(token);
                                    KitManager.UpdateSigns(kit);
                                    ctx.Reply(T.KitPropertySet, property, kit, newValue);
                                    ctx.LogAction(ActionLogType.SetKitProperty, kitName + ": " + property.ToUpper() + " >> " + newValue.ToUpper());
                                    if (oldbranch != kit.Branch || oldclass != kit.Class || prevType != kit.Type)
                                        manager.InvokeAfterMajorKitUpdate(proxy);
                                    return;
                            }
                        }
                    }
                    finally
                    {
                        proxy.Release();
                    }
                }
                else
                    ctx.Reply(T.KitNotFound, kitName);
            }
            else
                ctx.SendCorrectUsage("/kit <set|s> <parameter> <kitname> <value>");
        }
        else if (ctx.MatchParameter(0, "giveaccess", "givea", "ga"))
        {
            ctx.AssertHelpCheck(1, "/kit <giveaccess|givea|ga> <player> <kit id> [access type] - Give the provided player access to the kit with the provided id. Optionally supply an access type: [credits | event | default: purchase]");

            if (ctx.TryGet(2, out string kitName) && ctx.TryGet(1, out ulong playerId, out UCPlayer? onlinePlayer))
            {
                SqlItem<Kit>? proxy = await manager.FindKit(kitName, token, true);
                if (proxy?.Item != null)
                {
                    Kit? item;
                    await proxy.Enter(token).ConfigureAwait(false);
                    try
                    {
                        item = proxy.Item;
                    }
                    finally
                    {
                        proxy.Release();
                    }
                    if (item == null)
                        throw ctx.Reply(T.KitNotFound, kitName);
                    
                    if (!ctx.TryGet(3, out KitAccessType type) || type == KitAccessType.Unknown)
                        type = KitAccessType.Purchase;

                    bool hasAccess = await KitManager.HasAccess(item, playerId, token).ConfigureAwait(false);
                    PlayerNames names = await F.GetPlayerOriginalNamesAsync(playerId, token).ConfigureAwait(false);
                    if (hasAccess)
                    {
                        await UCWarfare.ToUpdate(token);
                        ctx.Reply(T.KitAlreadyHasAccess, onlinePlayer as IPlayer ?? names, item);
                        return;
                    }
                    await KitManager.GiveAccess(proxy, playerId, KitAccessType.Purchase, token).ConfigureAwait(false);
                    KitSync.OnAccessChanged(playerId);
                    ctx.LogAction(ActionLogType.ChangeKitAccess, playerId.ToString(Data.AdminLocale) + " GIVEN ACCESS TO " + kitName + ", REASON: " + type);

                    await UCWarfare.ToUpdate();
                    ctx.Reply(T.KitAccessGiven, onlinePlayer as IPlayer ?? names, playerId, item);
                    if (onlinePlayer is not null)
                    {
                        onlinePlayer.SendChat(T.KitAccessGivenDm, item);
                        KitManager.UpdateSigns(item, onlinePlayer);
                    }
                }
                else
                    ctx.Reply(T.KitNotFound, kitName);
            }
            else
                ctx.SendCorrectUsage("/kit <giveaccess|givea|ga> <player> <kitname> [credits|purchase|event]");
        }
        else if (ctx.MatchParameter(0, "removeaccess", "removea", "ra"))
        {
            ctx.AssertHelpCheck(1, "/kit <removeaccess|removea|ra> <player> <kit id> - Revoke access to the kit with the provided id from the provided player.");

            if (ctx.TryGet(2, out string kitName) && ctx.TryGet(1, out ulong playerId, out UCPlayer? onlinePlayer))
            {
                SqlItem<Kit>? proxy = await manager.FindKit(kitName, token, true);
                if (proxy?.Item != null)
                {
                    Kit? item;
                    await proxy.Enter(token).ConfigureAwait(false);
                    try
                    {
                        item = proxy.Item;
                    }
                    finally
                    {
                        proxy.Release();
                    }
                    if (item == null)
                        throw ctx.Reply(T.KitNotFound, kitName);
                    bool hasAccess = await KitManager.HasAccess(item, playerId, token).ConfigureAwait(false);
                    PlayerNames names = await F.GetPlayerOriginalNamesAsync(playerId, token).ConfigureAwait(false);
                    if (!hasAccess)
                    {
                        await UCWarfare.ToUpdate(token);
                        ctx.Reply(T.KitAlreadyMissingAccess, onlinePlayer as IPlayer ?? names, item);
                        return;
                    }
                    await KitManager.RemoveAccess(proxy, playerId, token).ConfigureAwait(false);
                    ctx.LogAction(ActionLogType.ChangeKitAccess, playerId.ToString(Data.AdminLocale) + " DENIED ACCESS TO " + kitName);
                    KitSync.OnAccessChanged(playerId);

                    await UCWarfare.ToUpdate();
                    ctx.Reply(T.KitAccessRevoked, onlinePlayer as IPlayer ?? names, playerId, item);
                    if (onlinePlayer is not null)
                    {
                        onlinePlayer.SendChat(T.KitAccessRevokedDm, item);
                        KitManager.UpdateSigns(item, onlinePlayer);
                    }
                }
                else
                    ctx.Reply(T.KitNotFound, kitName);
            }
            else
                ctx.SendCorrectUsage("/kit <removeaccess|removea|ra> <player> <kitname>");
        }
        else if (ctx.MatchParameter(0, "copyfrom", "copy", "cf"))
        {
            ctx.AssertHelpCheck(1, "/kit <copyfrom|cf> <source kit id> <new kit id> - Creates an exact copy of the source kit renamed to the new kit id.");

            if (ctx.TryGet(2, out string kitName) && ctx.TryGet(1, out string existingName))
            {
                SqlItem<Kit>? existing = await manager.FindKit(existingName, token).ConfigureAwait(false);
                if (existing?.Item == null)
                    throw ctx.Reply(T.KitNotFound, existingName);
                await existing.Enter(token).ConfigureAwait(false);
                Kit kit;
                try
                {
                    if (existing.Item == null)
                        throw ctx.Reply(T.KitNotFound, existingName);
                    SqlItem<Kit>? newKitProxy = await manager.FindKit(kitName, token).ConfigureAwait(false);
                    if (newKitProxy?.Item != null)
                        throw ctx.Reply(T.KitNameTaken, kitName);
                    kit = new Kit(kitName.ToLowerInvariant(), existing.Item)
                    {
                        Season = UCWarfare.Season,
                        Disabled = false,
                        Creator = ctx.CallerID
                    };
                }
                finally
                {
                    existing.Release();
                }

                await manager.AddOrUpdate(kit, token).ConfigureAwait(false);
                ctx.LogAction(ActionLogType.CreateKit, kitName + " COPIED FROM " + existingName);
                await UCWarfare.ToUpdate();
                KitManager.UpdateSigns(kit);
                ctx.Reply(T.KitCopied, existing.Item, kit);
            }
            else
                ctx.SendCorrectUsage("/kit <copyfrom|cf> <kitname> <newkitname>");
        }
        else if (ctx.MatchParameter(0, "createloadout", "cloadout", "cl"))
        {
            ctx.AssertHelpCheck(1, "/kit <createloadout|cloadout|cl> <player> <class> [sign text...] - Creates and prepares a loadout for the provided player with optional sign text.");

            ctx.AssertRanByPlayer();
            if (ctx.TryGet(2, out Class @class) && ctx.TryGet(1, out ulong playerId, out UCPlayer? onlinePlayer))
            {
                if (onlinePlayer is null && !PlayerSave.HasPlayerSave(playerId))
                    throw ctx.Reply(T.PlayerNotFound);

                PlayerNames names = await F.GetPlayerOriginalNamesAsync(playerId, token).ConfigureAwait(false);
                string loadoutId = await KitManager.GetFreeLoadoutName(playerId).ConfigureAwait(false);
                if (!ctx.TryGetRange(3, out string? signText) || string.IsNullOrWhiteSpace(signText))
                    signText = null;
                await UCWarfare.ToUpdate(token);
                Kit loadout = new Kit(loadoutId, @class, signText, null)
                {
                    Items = UCInventoryManager.ItemsFromInventory(ctx.Caller, findAssetRedirects: true),
                    Creator = ctx.CallerID,
                    LastEditor = ctx.CallerID
                };
                SqlItem<Kit>? oldKit = await manager.FindKit(loadout.Id, token).ConfigureAwait(false);
                if (oldKit?.Item == null)
                {
                    await UCWarfare.ToUpdate();
                    SqlItem<Kit> kit = await manager.AddOrUpdate(loadout, token).ConfigureAwait(false);
                    await KitManager.GiveAccess(kit, playerId, KitAccessType.Purchase, token).ConfigureAwait(false);
                    KitSync.OnAccessChanged(playerId);
                    ctx.LogAction(ActionLogType.CreateKit, loadout.Id);
                    await UCWarfare.ToUpdate();
                    KitManager.UpdateSigns(loadout);
                    ctx.Reply(T.LoadoutCreated, @class, onlinePlayer as IPlayer ?? names, playerId, loadout);
                }
                else
                {
                    await UCWarfare.ToUpdate();
                    ctx.Reply(T.KitNameTaken, loadout.Id);
                }
            }
            else
                throw ctx.SendCorrectUsage("/kit <createloadout|cloadout|cl> <player> <class> [sign text...]");
        }
        else if (ctx.MatchParameter(0, "skills", "skillset", "skillsets"))
        {
            ctx.AssertHelpCheck(1, "/kit skills <kit> <add|remove> <skill> [level]");
            bool add = ctx.MatchParameter(2, "add", "set");
            if (add || ctx.MatchParameter(2, "delete", "remove", "clear"))
            {
                if (ctx.TryGet(3, out string skillsetStr))
                {
                    int skillset = Skillset.GetSkillsetFromEnglishName(skillsetStr, out EPlayerSpeciality specialty);
                    if (skillset < 0)
                        throw ctx.Reply(T.KitInvalidSkillset, skillsetStr);
                    byte level = 0;
                    if (!add || ctx.TryGet(4, out level))
                    {
                        Skill skill = ctx.Caller.Player.skills.skills[(int)specialty][skillset];
                        int max = skill.GetClampedMaxUnlockableLevel();
                        if (!add || max >= level)
                        {
                            Skillset set = new Skillset(specialty, (byte)skillset, level);
                            string kitName = ctx.Get(1)!;
                            SqlItem<Kit>? proxy = await manager.FindKit(kitName, token, true);
                            if (proxy is null)
                                throw ctx.Reply(T.KitNotFound, kitName);
                            await proxy.Enter(token).ConfigureAwait(false);
                            try
                            {
                                if (proxy.Item is not { } kit)
                                    throw ctx.Reply(T.KitNotFound, kitName);
                                Skillset[] skillsets = kit.Skillsets;
                                for (int i = 0; i < skillsets.Length; ++i)
                                {
                                    if (skillsets[i].SkillIndex == set.SkillIndex && skillsets[i].SpecialityIndex == set.SpecialityIndex)
                                    {
                                        if (add)
                                            skillsets[i] = set;
                                        else
                                        {
                                            set = skillsets[i];
                                            Util.RemoveFromArray(ref skillsets, i);
                                            kit.Skillsets = skillsets;
                                        }
                                        
                                        goto reply;
                                    }
                                }

                                if (!add)
                                    throw ctx.Reply(T.KitSkillsetNotFound, set, kit);
                                Util.AddToArray(ref skillsets!, set);
                                kit.Skillsets = skillsets;
                                reply:
                                await proxy.SaveItem(token);
                                ctx.LogAction(add ? ActionLogType.AddSkillset : ActionLogType.RemoveSkillset, set + " ON " + kit.Id);
                                await UCWarfare.ToUpdate(token);
                                ctx.Reply(add ? T.KitSkillsetAdded : T.KitSkillsetRemoved, set, kit);
                                for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                                {
                                    UCPlayer player = PlayerManager.OnlinePlayers[i];
                                    if (player.ActiveKit is { Item: { } } && player.ActiveKit.LastPrimaryKey.Key == proxy.LastPrimaryKey.Key)
                                    {
                                        if (add)
                                            player.EnsureSkillset(set);
                                        else
                                            player.RemoveSkillset(set.Speciality, set.SkillIndex);
                                    }
                                }

                                return;
                            }
                            finally
                            {
                                proxy.Release();
                            }
                        }
                    }
                    throw ctx.Reply(T.KitInvalidSkillsetLevel, specialty switch
                    {
                        EPlayerSpeciality.DEFENSE => Localization.TranslateEnum((EPlayerDefense)skillset, ctx.LanguageInfo),
                        EPlayerSpeciality.OFFENSE => Localization.TranslateEnum((EPlayerOffense)skillset, ctx.LanguageInfo),
                        EPlayerSpeciality.SUPPORT => Localization.TranslateEnum((EPlayerSupport)skillset, ctx.LanguageInfo),
                        _ => skillset.ToString()
                    }, level);
                }
            }
            throw ctx.SendCorrectUsage("/kit skills <kit> <add|remove> <skill> [level]");
        }
        else ctx.SendCorrectUsage(Syntax);
    }
}
