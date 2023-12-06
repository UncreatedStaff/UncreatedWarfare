using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Players;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Unlocks;
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
                    Aliases = new string[] { "unl", "ul" },
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
                    Kit? kit = ctx.Caller.GetActiveKit();
                    if (kit is null)
                        throw ctx.Reply(T.AmmoNoKit);
                    
                    if (add)
                    {
                        IPageKitItem? item = await manager.GetHeldItemFromKit(ctx.Caller, token).ConfigureAwait(false);
                        await UCWarfare.ToUpdate(token);

                        if (item == null)
                            throw ctx.Reply(T.KitHotkeyNotHoldingItem);

                        ItemAsset? asset = item is ISpecificKitItem i2
                            ? i2.Item.GetAsset<ItemAsset>()
                            : item.GetItem(kit, TeamManager.GetFactionSafe(ctx.Caller.GetTeam()), out _, out _);
                        if (asset == null)
                            throw ctx.Reply(T.KitHotkeyNotHoldingItem);

                        if (!KitEx.CanBindHotkeyTo(asset, item.Page))
                            throw ctx.Reply(T.KitHotkeyNotHoldingValidItem, asset);

                        await manager.AddHotkey(kit.PrimaryKey, ctx.CallerID, slot, item, token).ConfigureAwait(false);
                        await UCWarfare.ToUpdate(token);
                        if (ctx.Caller.HotkeyBindings != null)
                        {
                            // remove duplicates / conflicts
                            ctx.Caller.HotkeyBindings.RemoveAll(x =>
                                x.Kit == kit.PrimaryKey && (x.Slot == slot ||
                                                                          x.Item.X == item.X &&
                                                                          x.Item.Y == item.Y &&
                                                                          x.Item.Page == item.Page));
                        }
                        else ctx.Caller.HotkeyBindings = new List<HotkeyBinding>(32);

                        ctx.Caller.HotkeyBindings.Add(new HotkeyBinding(kit.PrimaryKey, slot, item, new KitHotkey
                        {
                            Steam64 = ctx.CallerID,
                            KitId = kit.PrimaryKey,
                            Item = item is ISpecificKitItem item2 ? item2.Item : null,
                            Redirect = item is IAssetRedirectKitItem redir ? redir.RedirectType : null,
                            X = item.X,
                            Y = item.Y,
                            Page = item.Page,
                            Slot = slot
                        }));

                        byte index = KitEx.GetHotkeyIndex(slot);

                        if (KitEx.CanBindHotkeyTo(asset, (Page)equipment.equippedPage))
                        {
                            equipment.ServerBindItemHotkey(index, asset,
                                equipment.equippedPage, equipment.equipped_x,
                                equipment.equipped_y);
                        }
                        throw ctx.Reply(T.KitHotkeyBinded, asset, slot, kit);
                    }
                    else
                    {
                        bool removed = await manager.RemoveHotkey(kit.PrimaryKey, ctx.CallerID, slot, token).ConfigureAwait(false);
                        await UCWarfare.ToUpdate(token);
                        if (!removed)
                            throw ctx.Reply(T.KitHotkeyNotFound, slot, kit);

                        byte index = KitEx.GetHotkeyIndex(slot);
                        equipment.ServerClearItemHotkey(index);
                        throw ctx.Reply(T.KitHotkeyUnbinded, slot, kit);
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
                    Kit? kit = ctx.Caller.GetActiveKit();

                    if (kit == null)
                        throw ctx.Reply(T.AmmoNoKit);
                    
                    await manager.SaveLayout(ctx.Caller, kit, false, token).ConfigureAwait(false);
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
                    Kit? kit = ctx.Caller.GetActiveKit();
                    if (kit == null)
                        throw ctx.Reply(T.AmmoNoKit);
                    
                    if (kit.Items != null)
                    {
                        await UCWarfare.ToUpdate(token);
                        manager.Layouts.TryReverseLayoutTransformations(ctx.Caller, kit.Items, kit.PrimaryKey);
                    }

                    await manager.ResetLayout(ctx.Caller, kit.PrimaryKey, false, token);
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
            Kit? kit;
            UCPlayer player = ctx.Caller;
            UCPlayer.TryApplyViewLens(ref player);
            if (ctx.TryGetRange(1, out string kitName))
            {
                kit = await manager.FindKit(kitName, token).ConfigureAwait(false);
            }
            else if (ctx.TryGetTarget(out BarricadeDrop drop))
            {
                kitName = drop.interactable is InteractableSign sign ? sign.text : null!;
                kit = Signs.GetKitFromSign(drop, out int loadoutId);
                if (loadoutId > 0)
                    kit = await manager.Loadouts.GetLoadout(player, loadoutId, token).ConfigureAwait(false);
            }
            else
                throw ctx.SendCorrectUsage("/kit favorite (look at kit sign <b>or</b> [kit id]) - Favorite your kit or loadout.");
            
            if (kit == null)
            {
                await UCWarfare.ToUpdate(token);
                throw ctx.Reply(T.KitNotFound, kitName.Replace(Signs.Prefix, string.Empty));
            }
            await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (fav && manager.IsFavoritedQuick(kit.PrimaryKey, player))
                {
                    await UCWarfare.ToUpdate(token);
                    throw ctx.Reply(T.KitFavoriteAlreadyFavorited, kit);
                }
                else if (!fav && !manager.IsFavoritedQuick(kit.PrimaryKey, player))
                {
                    await UCWarfare.ToUpdate(token);
                    throw ctx.Reply(T.KitFavoriteAlreadyUnfavorited, kit);
                }
                else
                {
                    if (fav)
                        (player.KitMenuData.FavoriteKits ??= new List<uint>(8)).Add(kit.PrimaryKey);
                    else if (player.KitMenuData.FavoriteKits != null)
                        player.KitMenuData.FavoriteKits.RemoveAll(x => x == kit.PrimaryKey);
                    player.KitMenuData.FavoritesDirty = true;
                    
                    await manager.SaveFavorites(player, (IReadOnlyList<uint>?)player.KitMenuData.FavoriteKits ?? Array.Empty<uint>(), token).ConfigureAwait(false);
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

        if (ctx.MatchParameter(0, "create", "c", "override"))
        {
            ctx.AssertRanByPlayer();

            ctx.AssertHelpCheck(1, "/kit <create|c|override> <id> <class> [type] [faction] - Creates (or overrides if it already exits) a kit with default values based on the items in your inventory and your clothes.");

            if (ctx.TryGet(1, out string kitName))
            {
                kitName = kitName.ToLowerInvariant();
                Kit? kit = await manager.FindKit(kitName, token, true);
                if (kit != null) // overwrite
                {
                    await UCWarfare.ToUpdate(token);
                    ctx.Reply(T.KitConfirmOverride, kit, kit);
                    bool didConfirm = await CommandWaiter.WaitAsync(ctx.Caller, typeof(ConfirmCommand), 10000);
                    if (!didConfirm)
                    {
                        await UCWarfare.ToUpdate(token);
                        throw ctx.Reply(T.KitCancelOverride);
                    }

                    await using IKitsDbContext dbContext = new WarfareDbContext();

                    IKitItem[] oldItems = kit.Items;
                    kit.SetItemArray(UCInventoryManager.ItemsFromInventory(ctx.Caller, findAssetRedirects: true), dbContext);
                    kit.WeaponText = manager.GetWeaponText(kit);
                    kit.UpdateLastEdited(ctx.CallerID);
                    ctx.LogAction(ActionLogType.EditKit, "OVERRIDE ITEMS " + kit.InternalName + ".");
                    dbContext.Update(kit);
                    await dbContext.SaveChangesAsync(token).ConfigureAwait(false);

                    UCWarfare.RunTask(manager.OnItemsChangedLayoutHandler, oldItems, kit, token, ctx: "Update layouts after changing items.");
                    manager.Signs.UpdateSigns(kit);
                    ctx.Reply(T.KitOverwrote, kit);
                    return;
                }
                
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

                await using IKitsDbContext dbContext2 = new WarfareDbContext();

                if (@class == Class.None) @class = Class.Unarmed;
                kit = new Kit(kitName, @class, KitDefaults<WarfareDbContext>.GetDefaultBranch(@class), type, SquadLevel.Member, faction);
                kit.SetItemArray(UCInventoryManager.ItemsFromInventory(ctx.Caller, findAssetRedirects: true), dbContext2);

                kit.Creator = kit.LastEditor = ctx.CallerID;
                kit.WeaponText = manager.GetWeaponText(kit);
                dbContext2.Update(kit);
                await dbContext2.SaveChangesAsync(token).ConfigureAwait(false);
                ctx.LogAction(ActionLogType.CreateKit, kitName);

                await UCWarfare.ToUpdate(token);
                manager.Signs.UpdateSigns(kit);
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
                Kit? kit = await manager.FindKit(kitName, token, true);
                if (kit != null)
                {
                    bool ld = kit.Type == KitType.Loadout;
                    await UCWarfare.ToUpdate(token);
                    ctx.Reply(T.KitConfirmDelete, kit, kit);
                    bool didConfirm = await CommandWaiter.WaitAsync(ctx.Caller, typeof(ConfirmCommand), 10000);
                    if (!didConfirm)
                    {
                        await UCWarfare.ToUpdate(token);
                        throw ctx.Reply(T.KitCancelDelete);
                    }
                    
                    kit.UpdateLastEdited(ctx.CallerID);

                    await using IKitsDbContext dbContext = new WarfareDbContext();
                    dbContext.Remove(kit);
                    await dbContext.SaveChangesAsync(token).ConfigureAwait(false);

                    ctx.LogAction(ActionLogType.DeleteKit, kitName);

                    await UCWarfare.ToUpdate();
                    ctx.Reply(T.KitDeleted, kit);

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
                
                Kit? kit = await manager.FindKit(kitName, token, true);
                if (kit != null)
                {
                    if (!kit.NeedsUpgrade)
                    {
                        if (kit.Season != UCWarfare.Season)
                        {
                            await using IKitsDbContext dbContext = new WarfareDbContext();

                            kit.Season = UCWarfare.Season;
                            dbContext.Update(kit);
                            await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
                            throw ctx.Reply(T.KitUpgraded, kit);
                        }
                        await UCWarfare.ToUpdate(token);
                        throw ctx.Reply(T.DoesNotNeedUpgrade, kit);
                    }

                    (_, StandardErrorCode err) = await manager.Loadouts.UpgradeLoadout(ctx.CallerID, playerId, @class, kitName, token).ConfigureAwait(false);
                    await UCWarfare.ToUpdate();
                    if (err != StandardErrorCode.Success)
                        throw ctx.SendUnknownError();
                    ctx.Reply(T.LoadoutUpgraded, kit, @class);
                    await manager.Requests.GiveKit(ctx.Caller, kit, true, false, token).ConfigureAwait(false);
                }
                else
                    ctx.Reply(T.KitNotFound, kitName);
            }
            else
                ctx.SendCorrectUsage("/kit <upgrade|update|upg> <id> <new class>");
        }
        else if (ctx.MatchParameter(0, "unlock", "unl", "ul"))
        {
            ctx.AssertHelpCheck(1, "/kit <unlock|unl|ul> <id> - Unlocks a loadout so it's owner can use it.");

            if (ctx.TryGet(1, out string kitName))
            {
                Kit? kit = await manager.FindKit(kitName, token, true);
                if (kit is null)
                    throw ctx.Reply(T.KitNotFound);

                if (kit != null)
                {
                    if (!kit.NeedsSetup)
                    {
                        if (kit.Disabled)
                        {
                            await using IKitsDbContext dbContext = new WarfareDbContext();

                            kit.Disabled = false;
                            dbContext.Update(kit);
                            await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
                            throw ctx.Reply(T.KitUnlocked, kit);
                        }
                        await UCWarfare.ToUpdate(token);
                        throw ctx.Reply(T.DoesNotNeedUnlock, kit);
                    }
                }
                else
                    throw ctx.Reply(T.KitNotFound, kitName);

                (_, StandardErrorCode err) = await manager.Loadouts.UnlockLoadout(ctx.CallerID, kitName, token).ConfigureAwait(false);
                if (err != StandardErrorCode.Success)
                    throw ctx.SendUnknownError();
                ctx.Reply(T.KitUnlocked, kit);
            }
            else
                ctx.SendCorrectUsage("/kit <unlock|unl|ul> <id>");
        }
        else if (ctx.MatchParameter(0, "lock"))
        {
            ctx.AssertHelpCheck(1, "/kit <lock> <id> - Locks a loadout for staff review.");

            if (ctx.TryGet(1, out string kitName))
            {
                Kit? kit = await manager.FindKit(kitName, token, true);
                if (kit is null)
                    throw ctx.Reply(T.KitNotFound);

                if (kit != null)
                {
                    if (kit.Type != KitType.Loadout || !kit.Disabled)
                    {
                        if (!kit.Disabled)
                        {
                            await using IKitsDbContext dbContext = new WarfareDbContext();

                            kit.Disabled = true;
                            dbContext.Update(kit);
                            await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
                            throw ctx.Reply(T.KitLocked, kit);
                        }

                        await UCWarfare.ToUpdate(token);
                        throw ctx.Reply(T.DoesNotNeedLock, kit);
                    }
                }
                else
                    throw ctx.Reply(T.KitNotFound, kitName);

                (_, StandardErrorCode err) = await manager.Loadouts.LockLoadout(ctx.CallerID, kitName, token).ConfigureAwait(false);
                await UCWarfare.ToUpdate();
                if (err != StandardErrorCode.Success)
                    throw ctx.SendUnknownError();
                ctx.Reply(T.KitLocked, kit);
            }
            else
                ctx.SendCorrectUsage("/kit <lock> <id>");
        }
        else if (ctx.MatchParameter(0, "give", "g"))
        {
            ctx.AssertHelpCheck(1, "/kit <give|g> [id] (or look at a sign) - Equips you with the kit with the id provided.");

            ctx.AssertRanByPlayer();
            BarricadeDrop? drop = null;
            if (ctx.TryGet(1, out string kitName) || ctx.TryGetTarget(out drop))
            {
                Kit? kit = await manager.FindKit(kitName, token, true);
                if (kit == null && drop != null)
                {
                    kit = Signs.GetKitFromSign(drop, out int loadout);
                    if (loadout > 0)
                    {
                        UCPlayer pl = ctx.Caller;
                        UCPlayer.TryApplyViewLens(ref pl);
                        kit = await manager.Loadouts.GetLoadout(pl, loadout, token).ConfigureAwait(false);
                    }
                }
                
                if (kit != null)
                {
                    Class @class = kit.Class;
                    await manager.Requests.GiveKit(ctx.Caller, kit, true, true, token).ConfigureAwait(false);
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
                Kit? kit = await manager.FindKit(kitName, token, true, x => x.Kits
                    .Include(y => y.UnlockRequirementsModels)
                    .Include(y => y.Translations)
                );
                if (kit != null)
                {
                    if (kit == null)
                        throw ctx.Reply(T.KitNotFound, kitName);

                    await using IKitsDbContext dbContext = new WarfareDbContext();

                    if (ctx.MatchParameter(1, "level", "lvl"))
                    {
                        if (ctx.TryGet(3, out int level))
                        {
                            if (level == 0)
                            {
                                UnlockRequirement[] ulr = kit.UnlockRequirements;
                                while (true)
                                {
                                    int index = Array.FindIndex(ulr, x => x is LevelUnlockRequirement);
                                    if (index == -1)
                                        break;
                                    Util.RemoveFromArray(ref ulr, index);
                                }
                                kit.SetUnlockRequirementArray(ulr, dbContext);
                            }
                            else
                            {
                                UnlockRequirement[] ulr = kit.UnlockRequirements;
                                int index = Array.FindIndex(ulr, x => x is LevelUnlockRequirement);
                                UnlockRequirement req = new LevelUnlockRequirement { UnlockLevel = level };
                                if (index == -1)
                                {
                                    Util.AddToArray(ref ulr!, req);
                                    kit.SetUnlockRequirementArray(ulr, dbContext);
                                }
                                else
                                {
                                    ((LevelUnlockRequirement)ulr[index]).UnlockLevel = level;
                                    kit.MarkLocalUnlockRequirementsDirty(dbContext);
                                }
                            }

                            kit.UpdateLastEdited(ctx.CallerID);
                            dbContext.Update(kit);
                            await dbContext.SaveChangesAsync(token).ConfigureAwait(false);

                            await UCWarfare.ToUpdate(token);

                            ctx.Reply(T.KitPropertySet, property, kit, newValue);
                            ctx.LogAction(ActionLogType.SetKitProperty, kitName + ": LEVEL >> " + newValue.ToUpper());
                            manager.Signs.UpdateSigns(kit);
                            ctx.Defer();
                        }
                        else
                            ctx.SendCorrectUsage("/kit <set|s> <level|lvl> <kitname> <value: integer>");
                    }
                    else if (ctx.MatchParameter(1, "sign", "text"))
                    {
                        ctx.AssertHelpCheck(2, "/kit <set|s> <sign> <language (default: " + L.Default + "> <text> - Sets the display text for the kit's kit sign.");

                        LanguageInfo? language = Data.LanguageDataStore.GetInfoCached(newValue, false);
                        if (language == null)
                            throw ctx.Reply(T.KitLanguageNotFound, newValue);
                        if (ctx.TryGetRange(4, out newValue))
                        {
                            newValue = newValue.Replace("\\n", "\n").Replace("/n", "\n");
                            kit.SetSignText(dbContext, ctx.CallerID, kit, newValue, language);
                            await dbContext.SaveChangesAsync(token);
                            await UCWarfare.ToUpdate(token);
                            newValue = newValue.Replace('\n', '\\');
                            ctx.Reply(T.KitPropertySet, "sign text", kit, language + " : " + newValue);
                            ctx.LogAction(ActionLogType.SetKitProperty, kitName + ": SIGN TEXT >> \"" + newValue + "\"");
                            manager.Signs.UpdateSigns(kit);
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
                            kit.Faction = faction?.CreateModel();
                            kit.FactionId = faction?.PrimaryKey;
                            kit.UpdateLastEdited(ctx.CallerID);
                            dbContext.Update(kit);
                            await dbContext.SaveChangesAsync(token);
                            await UCWarfare.ToUpdate(token);
                            ctx.Reply(T.KitPropertySet, "faction", kit, faction?.GetName(Localization.GetDefaultLanguage())!);
                            ctx.LogAction(ActionLogType.SetKitProperty, kitName + ": FACTION >> " +
                                                                           (faction?.Name.ToUpper() ?? Translation.Null(TranslationFlags.NoRichText)));
                            manager.Signs.UpdateSigns(kit);
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
                        KitType prevType = kit.Type;
                        Class oldclass = kit.Class;
                        Branch oldbranch = kit.Branch;
                        float oldReqCooldown = kit.RequestCooldown;
                        float? oldTeamLimit = kit.TeamLimit;
                        SetPropertyResult result = SettableUtil<Kit>.SetProperty(kit, property, newValue, out MemberInfo? info);
                        if (info != null)
                            property = info.Name;
                        
                        switch (result)
                        {
                            case SetPropertyResult.ParseFailure:
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
                                    if (oldTeamLimit.HasValue && Mathf.Abs(KitDefaults<WarfareDbContext>.GetDefaultTeamLimit(oldclass) - oldTeamLimit.Value) < 0.005f)
                                        kit.TeamLimit = null;
                                    if (KitDefaults<WarfareDbContext>.GetDefaultBranch(oldclass) == oldbranch)
                                        kit.Branch = KitDefaults<WarfareDbContext>.GetDefaultBranch(kit.Class);
                                    if (Mathf.Abs(KitDefaults<WarfareDbContext>.GetDefaultRequestCooldown(oldclass) - oldReqCooldown) < 0.25f)
                                        kit.RequestCooldown = KitDefaults<WarfareDbContext>.GetDefaultRequestCooldown(kit.Class);
                                }

                                kit.UpdateLastEdited(ctx.CallerID);
                                dbContext.Update(kit);
                                await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
                                await UCWarfare.ToUpdate(token);
                                manager.Signs.UpdateSigns(kit);
                                ctx.Reply(T.KitPropertySet, property, kit, newValue);
                                ctx.LogAction(ActionLogType.SetKitProperty, kitName + ": " + property.ToUpper() + " >> " + newValue.ToUpper());
                                if (oldbranch != kit.Branch || oldclass != kit.Class || prevType != kit.Type)
                                    manager.InvokeAfterMajorKitUpdate(kit, true);
                                return;
                        }
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
                Kit? kit = await manager.FindKit(kitName, token, true);
                if (kit != null)
                {
                    if (!ctx.TryGet(3, out KitAccessType type) || type == KitAccessType.Unknown)
                        type = KitAccessType.Purchase;

                    bool hasAccess = await manager.HasAccess(kit, playerId, token).ConfigureAwait(false);
                    PlayerNames names = await F.GetPlayerOriginalNamesAsync(playerId, token).ConfigureAwait(false);
                    if (hasAccess)
                    {
                        await UCWarfare.ToUpdate(token);
                        ctx.Reply(T.KitAlreadyHasAccess, onlinePlayer as IPlayer ?? names, kit);
                        return;
                    }
                    await manager.GiveAccess(kit, playerId, KitAccessType.Purchase, token).ConfigureAwait(false);
                    KitSync.OnAccessChanged(playerId);
                    ctx.LogAction(ActionLogType.ChangeKitAccess, playerId.ToString(Data.AdminLocale) + " GIVEN ACCESS TO " + kitName + ", REASON: " + type);

                    await UCWarfare.ToUpdate();
                    ctx.Reply(T.KitAccessGiven, onlinePlayer as IPlayer ?? names, playerId, kit);
                    if (onlinePlayer is not null)
                    {
                        onlinePlayer.SendChat(T.KitAccessGivenDm, kit);
                        manager.Signs.UpdateSigns(kit, onlinePlayer);
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
                Kit? kit = await manager.FindKit(kitName, token, true);
                if (kit != null)
                {
                    bool hasAccess = await manager.HasAccess(kit, playerId, token).ConfigureAwait(false);
                    PlayerNames names = await F.GetPlayerOriginalNamesAsync(playerId, token).ConfigureAwait(false);
                    if (!hasAccess)
                    {
                        await UCWarfare.ToUpdate(token);
                        ctx.Reply(T.KitAlreadyMissingAccess, onlinePlayer as IPlayer ?? names, kit);
                        return;
                    }
                    await manager.RemoveAccess(kit, playerId, token).ConfigureAwait(false);
                    ctx.LogAction(ActionLogType.ChangeKitAccess, playerId.ToString(Data.AdminLocale) + " DENIED ACCESS TO " + kitName);
                    KitSync.OnAccessChanged(playerId);

                    await UCWarfare.ToUpdate();
                    ctx.Reply(T.KitAccessRevoked, onlinePlayer as IPlayer ?? names, playerId, kit);
                    if (onlinePlayer is not null)
                    {
                        onlinePlayer.SendChat(T.KitAccessRevokedDm, kit);
                        manager.Signs.UpdateSigns(kit, onlinePlayer);
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
                Kit? existing = await manager.FindKit(existingName, token, set: KitManager.FullSet).ConfigureAwait(false);
                if (existing == null)
                    throw ctx.Reply(T.KitNotFound, existingName);

                Kit? kit = await manager.FindKit(kitName, token, set: x => x.Kits).ConfigureAwait(false);
                if (kit != null)
                    throw ctx.Reply(T.KitNameTaken, kitName);

                kit = new Kit(kitName.ToLowerInvariant().Replace(' ', '_'), existing)
                {
                    Season = UCWarfare.Season,
                    Disabled = false,
                    Creator = ctx.CallerID
                };

                await using (IKitsDbContext dbContext = new WarfareDbContext())
                {
                    dbContext.Add(kit);

                    foreach (KitSkillset skillset in kit.Skillsets)
                        dbContext.Add(skillset);

                    foreach (KitFilteredFaction faction in kit.FactionFilter)
                        dbContext.Add(faction);

                    foreach (KitFilteredMap map in kit.MapFilter)
                        dbContext.Add(map);

                    foreach (KitItemModel item in kit.ItemModels)
                        dbContext.Add(item);

                    foreach (KitTranslation translation in kit.Translations)
                        dbContext.Add(translation);

                    foreach (KitUnlockRequirement unlockRequirement in kit.UnlockRequirementsModels)
                        dbContext.Add(unlockRequirement);
                }
                
                ctx.LogAction(ActionLogType.CreateKit, kitName + " COPIED FROM " + existingName);
                await UCWarfare.ToUpdate();
                manager.Signs.UpdateSigns(kit);
                ctx.Reply(T.KitCopied, existing, kit);
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
                string loadoutId = await manager.Loadouts.GetFreeLoadoutName(playerId).ConfigureAwait(false);
                if (!ctx.TryGetRange(3, out string? signText) || string.IsNullOrWhiteSpace(signText))
                    signText = null;
                await UCWarfare.ToUpdate(token);
                Kit loadout = new Kit(loadoutId, @class, signText, null)
                {
                    Creator = ctx.CallerID,
                    LastEditor = ctx.CallerID
                };

                await using IKitsDbContext dbContext = new WarfareDbContext();

                loadout.SetItemArray(UCInventoryManager.ItemsFromInventory(ctx.Caller, findAssetRedirects: true), dbContext);

                Kit? oldKit = await manager.FindKit(loadout.InternalName, token, set: x => x.Kits).ConfigureAwait(false);
                if (oldKit != null)
                    throw ctx.Reply(T.KitNameTaken, loadout.InternalName);
                
                dbContext.Add(loadout);

                await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
                await manager.GiveAccess(loadout, playerId, KitAccessType.Purchase, token).ConfigureAwait(false);

                await UCWarfare.ToUpdate();

                KitSync.OnAccessChanged(playerId);

                ctx.LogAction(ActionLogType.CreateKit, loadout.InternalName);
                manager.Signs.UpdateSigns(loadout);
                ctx.Reply(T.LoadoutCreated, @class, onlinePlayer as IPlayer ?? names, playerId, loadout);
            }
            else
                throw ctx.SendCorrectUsage("/kit <createloadout|cloadout|cl> <player> <class> [sign text...]");
        }
        else if (ctx.MatchParameter(0, "skills", "skillset", "skillsets"))
        {
            ctx.AssertHelpCheck(1, "/kit skills <kit> <add|remove> <skill> [level]");

            bool add = ctx.MatchParameter(2, "add", "set");

            if (!add && !ctx.MatchParameter(2, "delete", "remove", "clear"))
                throw ctx.SendCorrectUsage("/kit skills <kit> <add|remove> <skill> [level]");

            if (!ctx.TryGet(3, out string skillsetStr))
                throw ctx.SendCorrectUsage("/kit skills <kit> <add|remove> <skill> [level]");

            int skillset = Skillset.GetSkillsetFromEnglishName(skillsetStr, out EPlayerSpeciality specialty);
            if (skillset < 0)
                throw ctx.Reply(T.KitInvalidSkillset, skillsetStr);

            byte level = 0;
            if (add && !ctx.TryGet(4, out level))
            {
                throw ctx.Reply(T.KitInvalidSkillsetLevel, specialty switch
                {
                    EPlayerSpeciality.DEFENSE => Localization.TranslateEnum((EPlayerDefense)skillset, ctx.LanguageInfo),
                    EPlayerSpeciality.OFFENSE => Localization.TranslateEnum((EPlayerOffense)skillset, ctx.LanguageInfo),
                    EPlayerSpeciality.SUPPORT => Localization.TranslateEnum((EPlayerSupport)skillset, ctx.LanguageInfo),
                    _ => skillset.ToString()
                }, level);
            }

            Skill skill = ctx.Caller.Player.skills.skills[(int)specialty][skillset];
            int max = skill.GetClampedMaxUnlockableLevel();
            if (!add || max >= level)
            {
                Skillset set = new Skillset(specialty, (byte)skillset, level);
                string kitName = ctx.Get(1)!;
                Kit? kit = await manager.FindKit(kitName, token, true, set => set.Kits.Include(x => x.Skillsets));

                if (kit is null)
                    throw ctx.Reply(T.KitNotFound, kitName);

                await using IKitsDbContext dbContext = new WarfareDbContext();

                List<KitSkillset> skillsets = kit.Skillsets;
                for (int i = 0; i < skillsets.Count; ++i)
                {
                    if (skillsets[i].Skillset.SkillIndex != set.SkillIndex || skillsets[i].Skillset.SpecialityIndex != set.SpecialityIndex)
                        continue;

                    if (add)
                    {
                        skillsets[i].Skillset = set;
                        dbContext.Update(skillsets[i]);
                    }
                    else
                    {
                        dbContext.Remove(skillsets[i]);
                    }

                    goto reply;
                }

                if (!add)
                    throw ctx.Reply(T.KitSkillsetNotFound, set, kit);

                KitSkillset skillsetModel = new KitSkillset
                {
                    Skillset = set,
                    Kit = kit,
                    KitId = kit.PrimaryKey
                };
                skillsets.Add(skillsetModel);
                dbContext.Add(skillsetModel);

                reply:
                dbContext.Update(kit);
                await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
                ctx.LogAction(add ? ActionLogType.AddSkillset : ActionLogType.RemoveSkillset, set + " ON " + kit.InternalName);
                await UCWarfare.ToUpdate(token);
                ctx.Reply(add ? T.KitSkillsetAdded : T.KitSkillsetRemoved, set, kit);
                for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                {
                    UCPlayer player = PlayerManager.OnlinePlayers[i];
                    Kit? activeKit = player.GetActiveKit();
                    if (activeKit != null && activeKit.PrimaryKey == kit.PrimaryKey)
                    {
                        if (add)
                            player.EnsureSkillset(set);
                        else
                            player.RemoveSkillset(set.Speciality, set.SkillIndex);
                    }
                }

                return;
            }
            throw ctx.Reply(T.KitInvalidSkillsetLevel, specialty switch
            {
                EPlayerSpeciality.DEFENSE => Localization.TranslateEnum((EPlayerDefense)skillset, ctx.LanguageInfo),
                EPlayerSpeciality.OFFENSE => Localization.TranslateEnum((EPlayerOffense)skillset, ctx.LanguageInfo),
                EPlayerSpeciality.SUPPORT => Localization.TranslateEnum((EPlayerSupport)skillset, ctx.LanguageInfo),
                _ => skillset.ToString()
            }, level);
        }
        else ctx.SendCorrectUsage(Syntax);
    }
}
