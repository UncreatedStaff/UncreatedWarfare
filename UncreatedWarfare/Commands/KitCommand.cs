using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management.Legacy;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Sync;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("blank")]
[MetadataFile(nameof(GetHelpMetadata))]
public sealed class KitCommand : IExecutableCommand
{
    private const string Syntax = "/kit <keybind|search|skills|create|delete|give|set|giveaccess|removeacces|copyfrom|createloadout>";
    private const string Help = "Admin command to manage kits; creating, deleting, editing, and giving/removing access is done through this command.";

    private static readonly PermissionLeaf PermissionKeybind    = new PermissionLeaf("commands.kit.keybind",      unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionLayout     = new PermissionLeaf("commands.kit.layout",       unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionRename     = new PermissionLeaf("commands.kit.edit.rename",  unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionCreate     = new PermissionLeaf("commands.kit.edit.create",  unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionDelete     = new PermissionLeaf("commands.kit.edit.delete",  unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionSet        = new PermissionLeaf("commands.kit.edit.set",     unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionUpgrade    = new PermissionLeaf("commands.kit.edit.upgrade", unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionLock       = new PermissionLeaf("commands.kit.edit.lock",    unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionGive       = new PermissionLeaf("commands.kit.give",         unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionAccess     = new PermissionLeaf("commands.kit.access",       unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionFavorite   = new PermissionLeaf("commands.kit.favorite",     unturned: false, warfare: true);

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = Help,
            Parameters =
            [
                new CommandParameter("Keybind")
                {
                    Aliases = [ "hotkey", "bind" ],
                    Permission = PermissionKeybind,
                    Description = "Add or remove default keybinds for this kit.",
                    Parameters =
                    [
                        new CommandParameter("Add")
                        {
                            Aliases = [ "create", "new" ],
                            Description = "Add held item as a default keybind at [slot].",
                            Parameters =
                            [
                                new CommandParameter("Slot", typeof(byte))
                            ]
                        },
                        new CommandParameter("Remove")
                        {
                            Aliases = [ "delete", "cancel" ],
                            Description = "Remove held item as a default keybind at [slot].",
                            Parameters =
                            [
                                new CommandParameter("Slot", typeof(byte))
                            ]
                        },
                        new CommandParameter("Slot", typeof(byte))
                    ]
                },
                new CommandParameter("Layout")
                {
                    Aliases = [ "loadout", "customize" ],
                    Permission = PermissionLayout,
                    Description = "Set how you want your items organized when you receive your kit.",
                    Parameters =
                    [
                        new CommandParameter("Save")
                        {
                            IsOptional = true,
                            Aliases = [ "confirm", "keep" ],
                            Description = "Saves your current inventory as your layout for this kit."
                        },
                        new CommandParameter("Reset")
                        {
                            IsOptional = true,
                            Aliases = [ "delete", "cancel" ],
                            Description = "Resets your layout for this kit."
                        }
                    ]
                },
                new CommandParameter("Rename")
                {
                    Aliases = [ "name", "setname" ],
                    Permission = PermissionRename,
                    Description = "Rename the loadout on the sign you are looking at.",
                    Parameters =
                    [
                        new CommandParameter("New Name", typeof(string))
                        {
                            Description = "The new name to give your loadout.",
                            IsRemainder = true
                        }
                    ]
                },
                new CommandParameter("Create")
                {
                    Aliases = [ "c", "override" ],
                    Permission = PermissionCreate,
                    Description = "Creates or overwrites a kit. Class is not required to overwrite a kit.",
                    Parameters =
                    [
                        new CommandParameter("Id", typeof(Kit))
                        {
                            Parameters =
                            [
                                new CommandParameter("Class", typeof(Class))
                                {
                                    Parameters =
                                    [
                                        new CommandParameter("Type", "Public", "Elite", "Loadout", "Special")
                                        {
                                            IsOptional = true,
                                            Parameters =
                                            [
                                                new CommandParameter("Faction", typeof(FactionInfo))
                                                {
                                                    IsOptional = true
                                                }
                                            ]
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                },
                new CommandParameter("Delete")
                {
                    Aliases = [ "d", "remove" ],
                    Permission = PermissionDelete,
                    Description = "Delete a kit.",
                    Parameters =
                    [
                        new CommandParameter("Kit", typeof(Kit))
                    ]
                },
                new CommandParameter("Give")
                {
                    Aliases = [ "g" ],
                    Permission = PermissionGive,
                    Description = "Gives the caller a kit.",
                    Parameters =
                    [
                        new CommandParameter("Kit", typeof(Kit))
                        {
                            IsOptional = true
                        }
                    ]
                },
                new CommandParameter("Set")
                {
                    Aliases = [ "s" ],
                    Permission = PermissionSet,
                    Description = "Sets a property of a kit.",
                    Parameters =
                    [
                        new CommandParameter("Sign")
                        {
                            Aliases = [ "text" ],
                            Description = "Sets the sign text of a kit for a language. Default language is " + L.Default + ".",
                            Parameters =
                            [
                                new CommandParameter("Kit", typeof(Kit))
                                {
                                    Parameters =
                                    [
                                        new CommandParameter("Language", typeof(string))
                                        {
                                            Parameters =
                                            [
                                                new CommandParameter("Text", typeof(string))
                                                {
                                                    IsRemainder = true
                                                }
                                            ]
                                        }
                                    ]
                                }
                            ]
                        },
                        new CommandParameter("Property", typeof(string))
                        {
                            Aliases = [ "level", "lvl", "faction", "team", "group" ],
                            Parameters =
                            [
                                new CommandParameter("Kit", typeof(Kit))
                                {
                                    Parameters =
                                    [
                                        new CommandParameter("Value", typeof(object))
                                        {
                                            IsRemainder = true
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                },
                new CommandParameter("GiveAccess")
                {
                    Aliases = [ "givea", "ga" ],
                    Permission = PermissionAccess,
                    Description = "Give a player access to a non-public kit.",
                    Parameters =
                    [
                        new CommandParameter("Player", typeof(IPlayer))
                        {
                            Parameters =
                            [
                                new CommandParameter("Kit", typeof(Kit))
                                {
                                    Parameters =
                                    [
                                        new CommandParameter("AccessType", typeof(KitAccessType))
                                        {
                                            IsOptional = true
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                },
                new CommandParameter("RemoveAccess")
                {
                    Aliases = [ "remvoea", "ra" ],
                    Permission = PermissionAccess,
                    Description = "Remove a player's access to a non-public kit.",
                    Parameters =
                    [
                        new CommandParameter("Player", typeof(IPlayer))
                        {
                            ChainDisplayCount = 2,
                            Parameters =
                            [
                                new CommandParameter("Kit", typeof(Kit))
                            ]
                        }
                    ]
                },
                new CommandParameter("CopyFrom")
                {
                    Aliases = [ "cf", "copy" ],
                    Permission = PermissionCreate,
                    Description = "Create a copy of a kit with a different id.",
                    Parameters =
                    [
                        new CommandParameter("Kit", typeof(Kit))
                        {
                            Parameters =
                            [
                                new CommandParameter("Id", typeof(string))
                            ]
                        }
                    ]
                },
                new CommandParameter("CreateLoadout")
                {
                    Aliases = [ "cloadout", "cl" ],
                    Permission = PermissionCreate,
                    Description = "Create a loadout with some default parameters.",
                    Parameters =
                    [
                        new CommandParameter("Player", typeof(IPlayer))
                        {
                            Parameters =
                            [
                                new CommandParameter("Class", typeof(Class))
                                {
                                    Parameters =
                                    [
                                        new CommandParameter("SignText", typeof(string))
                                        {
                                            IsOptional = false,
                                            IsRemainder = true
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                },
                new CommandParameter("Skills")
                {
                    Aliases = [ "skillset", "skillsets" ],
                    Permission = PermissionSet,
                    Description = "Modify the skillset overrides on a kit.",
                    Parameters =
                    [
                        new CommandParameter("Add")
                        {
                            Aliases = [ "set" ],
                            Parameters =
                            [
                                new CommandParameter("Skill", typeof(string))
                                {
                                    Parameters =
                                    [
                                        new CommandParameter("Level", typeof(byte))
                                    ]
                                }
                            ]
                        },
                        new CommandParameter("Remove")
                        {
                            Aliases = [ "delete", "clear" ],
                            Parameters =
                            [
                                new CommandParameter("Skill", typeof(string))
                            ]
                        }
                    ]
                },
                new CommandParameter("Upgrade")
                {
                    Aliases = [ "update", "upg" ],
                    Permission = PermissionUpgrade,
                    Description = "Upgrade an old loadout.",
                    Parameters =
                    [
                        new CommandParameter("Kit", typeof(Kit))
                        {
                            Parameters =
                            [
                                new CommandParameter("Class", typeof(Class))
                            ]
                        }
                    ]
                },
                new CommandParameter("Unlock")
                {
                    Aliases = [ "unl", "ul" ],
                    Permission = PermissionLock,
                    Description = "Unlock a completed loadout.",
                    Parameters =
                    [
                        new CommandParameter("Kit", typeof(Kit))
                    ]
                },
                new CommandParameter("Lock")
                {
                    Description = "Lock a setup loadout.",
                    Permission = PermissionLock,
                    Parameters =
                    [
                        new CommandParameter("Kit", typeof(Kit))
                    ]
                },
                new CommandParameter("Favorite")
                {
                    Aliases = [ "favourite", "favour", "favor", "fav", "star" ],
                    Description = "Favorite your kit or loadout.",
                    Permission = PermissionFavorite,
                    Parameters =
                    [
                        new CommandParameter("Kit", typeof(Kit))
                        {
                            IsOptional = true
                        }
                    ]
                },
                new CommandParameter("Unfavorite")
                {
                    Aliases = [ "unfavourite", "unfavour", "unfavor", "unfav", "unstar" ],
                    Description = "Unfavorite your kit or loadout.",
                    Permission = PermissionFavorite,
                    Parameters =
                    [
                        new CommandParameter("Kit", typeof(Kit))
                        {
                            IsOptional = true
                        }
                    ]
                }
            ]
        };
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertGamemode<IKitRequests>();
        KitManager? manager = KitManager.GetSingletonQuick();
        if (manager == null)
            throw Context.SendGamemodeError();

        Context.AssertArgs(1, "/kit <bind|rename|layout|favorite|unfavorite> - Customize your experience with kits.");
        Context.AssertHelpCheck(0, "/kit <bind|rename|layout|favorite|unfavorite> - Customize your experience with kits.");

        if (Context.MatchParameter(0, "hotkey", "keybind", "bind"))
        {
            Context.AssertRanByPlayer();

            await Context.AssertPermissions(PermissionKeybind, token);

            bool add = Context.MatchParameter(1, "add", "create", "new") || Context.HasArgsExact(2);
            if ((add || Context.MatchParameter(1, "remove", "delete", "cancel")) && Context.TryGet(Context.ArgumentCount - 1, out byte slot) && KitEx.ValidSlot(slot))
            {
                await Context.Player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    PlayerEquipment equipment = Context.Player.UnturnedPlayer.equipment;
                    Kit? kit = await Context.Player.GetActiveKit(token).ConfigureAwait(false);
                    if (kit is null)
                        throw Context.Reply(T.AmmoNoKit);
                    
                    if (add)
                    {
                        IPageKitItem? item = await manager.GetHeldItemFromKit(Context.Player, token).ConfigureAwait(false);
                        await UniTask.SwitchToMainThread(token);

                        if (item == null)
                            throw Context.Reply(T.KitHotkeyNotHoldingItem);

                        ItemAsset? asset = item is ISpecificKitItem i2
                            ? i2.Item.GetAsset<ItemAsset>()
                            : item.GetItem(kit, TeamManager.GetFactionSafe(Context.Player.GetTeam()), out _, out _);
                        if (asset == null)
                            throw Context.Reply(T.KitHotkeyNotHoldingItem);

                        if (!KitEx.CanBindHotkeyTo(asset, item.Page))
                            throw Context.Reply(T.KitHotkeyNotHoldingValidItem, asset);

                        await manager.AddHotkey(kit.PrimaryKey, Context.CallerId.m_SteamID, slot, item, token).ConfigureAwait(false);
                        await UniTask.SwitchToMainThread(token);
                        if (Context.Player.HotkeyBindings != null)
                        {
                            // remove duplicates / conflicts
                            Context.Player.HotkeyBindings.RemoveAll(x =>
                                x.Kit == kit.PrimaryKey && (x.Slot == slot ||
                                                                          x.Item.X == item.X &&
                                                                          x.Item.Y == item.Y &&
                                                                          x.Item.Page == item.Page));
                        }
                        else Context.Player.HotkeyBindings = new List<HotkeyBinding>(32);

                        Context.Player.HotkeyBindings.Add(new HotkeyBinding(kit.PrimaryKey, slot, item, new KitHotkey
                        {
                            Steam64 = Context.CallerId.m_SteamID,
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
                        throw Context.Reply(T.KitHotkeyBinded, asset, slot, kit);
                    }
                    else
                    {
                        bool removed = await manager.RemoveHotkey(kit.PrimaryKey, Context.CallerId.m_SteamID, slot, token).ConfigureAwait(false);
                        await UniTask.SwitchToMainThread(token);
                        if (!removed)
                            throw Context.Reply(T.KitHotkeyNotFound, slot, kit);

                        byte index = KitEx.GetHotkeyIndex(slot);
                        equipment.ServerClearItemHotkey(index);
                        throw Context.Reply(T.KitHotkeyUnbinded, slot, kit);
                    }
                }
                finally
                {
                    Context.Player.PurchaseSync.Release();
                }
            }
            throw Context.SendCorrectUsage("/kit keybind [add (default)|remove] <key (3-9 or 0)>");
        }
        if (Context.MatchParameter(0, "layout", "loadout", "customize"))
        {
            Context.AssertRanByPlayer();

            await Context.AssertPermissions(PermissionLayout, token);

            Context.AssertHelpCheck(1, "/kit layout <save|reset> - Cutomize your kit's item layout.");
            if (Context.MatchParameter(1, "save", "confirm", "keep"))
            {
                await Context.Player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    Kit? kit = await Context.Player.GetActiveKit(token).ConfigureAwait(false);

                    if (kit == null)
                        throw Context.Reply(T.AmmoNoKit);
                    
                    await manager.SaveLayout(Context.Player, kit, false, token).ConfigureAwait(false);
                    await UniTask.SwitchToMainThread(token);
                    throw Context.Reply(T.KitLayoutSaved, kit);
                }
                finally
                {
                    Context.Player.PurchaseSync.Release();
                }
            }

            if (Context.MatchParameter(1, "reset", "delete", "cancel"))
            {
                await Context.Player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    Kit? kit = await Context.Player.GetActiveKit(token).ConfigureAwait(false);
                    if (kit == null)
                        throw Context.Reply(T.AmmoNoKit);
                    
                    if (kit.Items != null)
                    {
                        await UniTask.SwitchToMainThread(token);
                        manager.Layouts.TryReverseLayoutTransformations(Context.Player, kit.Items, kit.PrimaryKey);
                    }

                    await manager.ResetLayout(Context.Player, kit.PrimaryKey, false, token);
                    await UniTask.SwitchToMainThread(token);
                    throw Context.Reply(T.KitLayoutReset, kit);
                }
                finally
                {
                    Context.Player.PurchaseSync.Release();
                }
            }
            throw Context.SendCorrectUsage("/kit layout <save|reset>");
        }
        if (Context.MatchParameter(0, "rename", "name", "setname"))
        {
            Context.AssertRanByPlayer();

            await Context.AssertPermissions(PermissionRename, token);
            await UniTask.SwitchToMainThread(token);

            Context.AssertHelpCheck(1, "/kit <rename> <new name ...> - Rename the loadout on the sign you are looking at.");
            Kit? kit = null;
            UCPlayer player = Context.Player;
            UCPlayer.TryApplyViewLens(ref player);
            if (Context.TryGetBarricadeTarget(out BarricadeDrop? drop))
            {
                string kitName = drop.interactable is InteractableSign sign ? sign.text : null!;
                kit = Signs.GetKitFromSign(drop, out int loadoutId);
                if (loadoutId > 0)
                    kit = await manager.Loadouts.GetLoadout(player, loadoutId, token).ConfigureAwait(false);

                if (kit == null)
                    throw Context.Reply(T.KitNotFound, kitName.Replace(Signs.Prefix, string.Empty));
            }
            
            if (kit == null || !Context.TryGetRange(0, out string? newName))
            {
                throw Context.SendCorrectUsage("/kit rename <new name ...> - Rename the loadout on the sign you are looking at.");
            }

            if (kit.Type != KitType.Loadout)
                throw Context.Reply(T.KitRenameNotLoadout, kit);

            if (Data.GetChatFilterViolation(newName) is { } chatFilterViolation)
                throw Context.Reply(T.KitRenameFilterVoilation, chatFilterViolation);

            await using IKitsDbContext dbContext = new WarfareDbContext();

            LanguageInfo defaultLanguage = Localization.GetDefaultLanguage();

            string oldName = kit.GetDisplayName(defaultLanguage, removeNewLine: false);

            newName = KitEx.ReplaceNewLineSubstrings(newName);

            kit.SetSignText(dbContext, 0ul, kit, newName, defaultLanguage);
            if (kit.Translations.Count > 1)
            {
                for (int i = kit.Translations.Count - 1; i >= 0; i--)
                {
                    KitTranslation t = kit.Translations[i];
                    if (t.LanguageId == defaultLanguage.Key)
                        continue;

                    dbContext.Remove(t);
                    kit.Translations.RemoveAt(i);
                }
            }

            oldName = oldName.Replace("\n", "<br>");
            newName = newName.Replace("\n", "<br>");

            await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
            int ldId = KitEx.ParseStandardLoadoutId(kit.InternalName);
            string ldIdStr = ldId == -1 ? "???" : KitEx.GetLoadoutLetter(ldId).ToUpperInvariant();
            Context.LogAction(ActionLogType.SetKitProperty, kit.FactionId + ": SIGN TEXT >> \"" + newName + "\" (using /kit rename)");
            manager.Signs.UpdateSigns(kit);
            throw Context.Reply(T.KitRenamed, ldIdStr, oldName, newName);
        }

        bool fav = Context.MatchParameter(0, "favorite", "favourite", "favour", "favor", "fav", "star");
        if (fav || Context.MatchParameter(0, "unfavorite", "unfavourite", "unfavour", "unfavor", "unfav", "unstar"))
        {
            Context.AssertRanByPlayer();

            await Context.AssertPermissions(PermissionFavorite, token);
            await UniTask.SwitchToMainThread(token);

            Context.AssertHelpCheck(1, "/kit <fav|unfav> (look at kit sign <b>or</b> [kit id]) - Favorite or unfavorite your kit or loadout.");
            Kit? kit;
            UCPlayer player = Context.Player;
            UCPlayer.TryApplyViewLens(ref player);
            if (Context.TryGetRange(1, out string? kitName))
            {
                kit = await manager.FindKit(kitName, token).ConfigureAwait(false);
            }
            else if (Context.TryGetBarricadeTarget(out BarricadeDrop? drop))
            {
                kitName = drop.interactable is InteractableSign sign ? sign.text : null!;
                kit = Signs.GetKitFromSign(drop, out int loadoutId);
                if (loadoutId > 0)
                    kit = await manager.Loadouts.GetLoadout(player, loadoutId, token).ConfigureAwait(false);
            }
            else
                throw Context.SendCorrectUsage("/kit <fav|unfav> (look at kit sign <b>or</b> [kit id]) - Favorite or unfavorite your kit or loadout.");
            
            if (kit == null)
            {
                await UniTask.SwitchToMainThread(token);
                throw Context.Reply(T.KitNotFound, kitName.Replace(Signs.Prefix, string.Empty));
            }
            await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (fav && manager.IsFavoritedQuick(kit.PrimaryKey, player))
                {
                    await UniTask.SwitchToMainThread(token);
                    throw Context.Reply(T.KitFavoriteAlreadyFavorited, kit);
                }
                else if (!fav && !manager.IsFavoritedQuick(kit.PrimaryKey, player))
                {
                    await UniTask.SwitchToMainThread(token);
                    throw Context.Reply(T.KitFavoriteAlreadyUnfavorited, kit);
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
                await UniTask.SwitchToMainThread(token);

                Context.Reply(fav ? T.KitFavorited : T.KitUnfavorited, kit);
            }
            finally
            {
                player.PurchaseSync.Release();
            }

            await UniTask.SwitchToMainThread(token);
            Signs.UpdateKitSigns(player, null);
            return;
        }

        Context.AssertOnDuty();

        if (Context.MatchParameter(0, "create", "c", "override"))
        {
            Context.AssertRanByPlayer();

            await Context.AssertPermissions(PermissionCreate, token);

            Context.AssertHelpCheck(1, "/kit <create|c|override> <id> <class> [type] [faction] - Creates (or overrides if it already exits) a kit with default values based on the items in your inventory and your clothes.");

            if (Context.TryGet(1, out string kitName))
            {
                kitName = kitName.ToLowerInvariant();
                Kit? kit = await manager.FindKit(kitName, token, true);
                if (kit != null) // overwrite
                {
                    await UniTask.SwitchToMainThread(token);
                    Context.Reply(T.KitConfirmOverride, kit, kit);
                    bool didConfirm = await CommandWaiter.WaitAsync(Context.Player, typeof(ConfirmCommand), 10000);
                    if (!didConfirm)
                    {
                        await UniTask.SwitchToMainThread(token);
                        throw Context.Reply(T.KitCancelOverride);
                    }

                    await using IKitsDbContext dbContext = new WarfareDbContext();

                    IKitItem[] oldItems = kit.Items;
                    kit.SetItemArray(ItemUtility.ItemsFromInventory(Context.Player, findAssetRedirects: true), dbContext);
                    kit.WeaponText = manager.GetWeaponText(kit);
                    kit.UpdateLastEdited(Context.CallerId.m_SteamID);
                    Context.LogAction(ActionLogType.EditKit, "OVERRIDE ITEMS " + kit.InternalName + ".");
                    dbContext.Update(kit);
                    await dbContext.SaveChangesAsync(token).ConfigureAwait(false);

                    _ = UCWarfare.RunTask(manager.OnItemsChangedLayoutHandler, oldItems, kit, token, ctx: "Update layouts after changing items.");
                    manager.Signs.UpdateSigns(kit);
                    Context.Reply(T.KitOverwrote, kit);
                    return;
                }
                
                FactionInfo? faction = null;
                Class @class = Context.Player.KitClass;
                if (@class == Class.None) @class = Class.Unarmed;
                KitType type = KitType.Public;
                bool def = Context.MatchParameter(2, "default") || Context.MatchParameter(2, Default);
                if (def || Context.MatchParameter(2, "me") || Context.TryGet(2, out @class))
                {
                    if (def || @class == Class.None) @class = Class.Unarmed;
                    if ((Context.MatchParameter(3, "default") || Context.MatchParameter(3, Default)) || Context.TryGet(3, out type))
                    {
                        if (Context.TryGet(4, out string factionId))
                        {
                            faction = TeamManager.FindFactionInfo(factionId);
                            if (faction == null)
                                throw Context.Reply(T.FactionNotFoundCreateKit, factionId);
                        }
                    }
                    else if (Context.HasArgs(4))
                        throw Context.Reply(T.TypeNotFoundCreateKit, Context.Get(2)!);
                }
                else if (Context.HasArgs(3))
                    throw Context.Reply(T.ClassNotFoundCreateKit, Context.Get(2)!);

                await using IKitsDbContext dbContext2 = new WarfareDbContext();

                if (@class == Class.None) @class = Class.Unarmed;
                kit = new Kit(kitName, @class, KitDefaults<WarfareDbContext>.GetDefaultBranch(@class), type, SquadLevel.Member, faction);

                await dbContext2.AddAsync(kit, token).ConfigureAwait(false);
                await dbContext2.SaveChangesAsync(token).ConfigureAwait(false);

                await UniTask.SwitchToMainThread(token);

                kit.SetItemArray(ItemUtility.ItemsFromInventory(Context.Player, findAssetRedirects: true), dbContext2);

                kit.Creator = kit.LastEditor = Context.CallerId.m_SteamID;
                kit.WeaponText = manager.GetWeaponText(kit);
                dbContext2.Update(kit);
                await dbContext2.SaveChangesAsync(token).ConfigureAwait(false);
                Context.LogAction(ActionLogType.CreateKit, kitName);

                await UniTask.SwitchToMainThread(token);
                manager.Signs.UpdateSigns(kit);
                Context.Reply(T.KitCreated, kit);
            }
            else
                Context.SendCorrectUsage("/kit <create|c|override> <kit name>");
        }
        else if (Context.MatchParameter(0, "delete", "d", "remove"))
        {
            await Context.AssertPermissions(PermissionDelete, token);

            Context.AssertHelpCheck(1, "/kit <delete|d|remove> <id> - Deletes the kit with the provided id.");

            if (Context.TryGet(1, out string kitName))
            {
                Kit? kit = await manager.FindKit(kitName, token, true);
                if (kit != null)
                {
                    bool ld = kit.Type == KitType.Loadout;
                    await UniTask.SwitchToMainThread(token);
                    Context.Reply(T.KitConfirmDelete, kit, kit);
                    bool didConfirm = await CommandWaiter.WaitAsync(Context.Player, typeof(ConfirmCommand), 10000);
                    if (!didConfirm)
                    {
                        await UniTask.SwitchToMainThread(token);
                        throw Context.Reply(T.KitCancelDelete);
                    }
                    
                    kit.UpdateLastEdited(Context.CallerId.m_SteamID);

                    await using IKitsDbContext dbContext = new WarfareDbContext();
                    dbContext.Remove(kit);
                    await dbContext.SaveChangesAsync(token).ConfigureAwait(false);

                    Context.LogAction(ActionLogType.DeleteKit, kitName);

                    await UniTask.SwitchToMainThread(token);
                    Context.Reply(T.KitDeleted, kit);

                    if (!ld)
                        Signs.UpdateKitSigns(null, kitName);
                    else
                        Signs.UpdateLoadoutSigns(null);
                }
                else
                    Context.Reply(T.KitNotFound, kitName);
            }
            else
                Context.SendCorrectUsage("/kit <delete|d|remove> <kit name>");
        }
        else if (Context.MatchParameter(0, "upgrade", "update", "upg"))
        {
            await Context.AssertPermissions(PermissionUpgrade, token);

            Context.AssertHelpCheck(1, "/kit <upgrade|update|upg> <id> <new class> - Upgrades a loadout and prepares it for unlocking.");

            if (Context.TryGet(2, out Class @class) && Context.TryGet(1, out string kitName))
            {
                if (KitEx.ParseStandardLoadoutId(kitName) < 1 || kitName.Length < 18 || !ulong.TryParse(kitName.Substring(0, 17), NumberStyles.Number, Data.AdminLocale, out ulong playerId))
                    throw Context.Reply(T.KitLoadoutIdBadFormat);
                
                Kit? kit = await manager.FindKit(kitName, token, true, KitManager.FullSet);
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
                            throw Context.Reply(T.KitUpgraded, kit);
                        }
                        await UniTask.SwitchToMainThread(token);
                        throw Context.Reply(T.DoesNotNeedUpgrade, kit);
                    }

                    (_, StandardErrorCode err) = await manager.Loadouts.UpgradeLoadout(Context.CallerId.m_SteamID, playerId, @class, kitName, token).ConfigureAwait(false);
                    await UniTask.SwitchToMainThread(token);
                    if (err != StandardErrorCode.Success)
                        throw Context.SendUnknownError();
                    Context.Reply(T.LoadoutUpgraded, kit, @class);
                    await manager.Requests.GiveKit(Context.Player, kit, true, false, token).ConfigureAwait(false);
                }
                else
                    Context.Reply(T.KitNotFound, kitName);
            }
            else
                Context.SendCorrectUsage("/kit <upgrade|update|upg> <id> <new class>");
        }
        else if (Context.MatchParameter(0, "unlock", "unl", "ul"))
        {
            await Context.AssertPermissions(PermissionLock, token);

            Context.AssertHelpCheck(1, "/kit <unlock|unl|ul> <id> - Unlocks a loadout so it's owner can use it.");

            if (Context.TryGet(1, out string kitName))
            {
                Kit? kit = await manager.FindKit(kitName, token, true);
                if (kit is null)
                    throw Context.Reply(T.KitNotFound);

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
                            throw Context.Reply(T.KitUnlocked, kit);
                        }
                        await UniTask.SwitchToMainThread(token);
                        throw Context.Reply(T.DoesNotNeedUnlock, kit);
                    }
                }
                else
                    throw Context.Reply(T.KitNotFound, kitName);

                (_, StandardErrorCode err) = await manager.Loadouts.UnlockLoadout(Context.CallerId.m_SteamID, kitName, token).ConfigureAwait(false);
                if (err != StandardErrorCode.Success)
                    throw Context.SendUnknownError();
                Context.Reply(T.KitUnlocked, kit);
            }
            else
                Context.SendCorrectUsage("/kit <unlock|unl|ul> <id>");
        }
        else if (Context.MatchParameter(0, "lock"))
        {
            await Context.AssertPermissions(PermissionLock, token);

            Context.AssertHelpCheck(1, "/kit <lock> <id> - Locks a loadout for staff review.");

            if (Context.TryGet(1, out string kitName))
            {
                Kit? kit = await manager.FindKit(kitName, token, true);
                if (kit is null)
                    throw Context.Reply(T.KitNotFound);

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
                            throw Context.Reply(T.KitLocked, kit);
                        }

                        await UniTask.SwitchToMainThread(token);
                        throw Context.Reply(T.DoesNotNeedLock, kit);
                    }
                }
                else
                    throw Context.Reply(T.KitNotFound, kitName);

                (_, StandardErrorCode err) = await manager.Loadouts.LockLoadout(Context.CallerId.m_SteamID, kitName, token).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(token);
                if (err != StandardErrorCode.Success)
                    throw Context.SendUnknownError();
                Context.Reply(T.KitLocked, kit);
            }
            else
                Context.SendCorrectUsage("/kit <lock> <id>");
        }
        else if (Context.MatchParameter(0, "give", "g"))
        {
            await Context.AssertPermissions(PermissionGive, token);

            Context.AssertHelpCheck(1, "/kit <give|g> [id] (or look at a sign) - Equips you with the kit with the id provided.");

            Context.AssertRanByPlayer();
            BarricadeDrop? drop = null;
            if (Context.TryGet(1, out string kitName) || Context.TryGetBarricadeTarget(out drop))
            {
                Kit? kit = kitName == null ? null : await manager.FindKit(kitName, token, true, x => KitManager.RequestableSet(x, false));
                if (kit == null && drop != null)
                {
                    kit = Signs.GetKitFromSign(drop, out int loadout);
                    if (loadout > 0)
                    {
                        UCPlayer pl = Context.Player;
                        UCPlayer.TryApplyViewLens(ref pl);
                        kit = await manager.Loadouts.GetLoadout(pl, loadout, token).ConfigureAwait(false);
                    }

                    if (kit != null)
                        kit = await manager.GetKit(kit.PrimaryKey, token, x => KitManager.RequestableSet(x, false));
                }
                
                if (kit != null)
                {
                    Class @class = kit.Class;
                    await manager.Requests.GiveKit(Context.Player, kit, true, true, token).ConfigureAwait(false);
                    await UniTask.SwitchToMainThread(token);
                    Context.LogAction(ActionLogType.GiveKit, kitName);
                    Context.Reply(T.RequestSignGiven, @class);
                }
                else
                    throw Context.Reply(T.KitNotFound, kitName);
            }
            else
                Context.SendCorrectUsage("/kit <give|g> [id]");
        }
        else if (Context.MatchParameter(0, "set", "s"))
        {
            await Context.AssertPermissions(PermissionSet, token);

            Context.AssertHelpCheck(1, "/kit <set|s> <level|sign|property> <value> - Sets the level requirement, sign text, or other properties to value. To set default sign text use: /kit set sign <kit id> en-us <text>.");

            if (Context.TryGet(3, out string newValue) && Context.TryGet(2, out string kitName) && Context.TryGet(1, out string property))
            {
                Kit? kit = await manager.FindKit(kitName, token, true, x => x.Kits
                    .Include(y => y.UnlockRequirementsModels)
                    .Include(y => y.Translations)
                );
                if (kit != null)
                {
                    if (kit == null)
                        throw Context.Reply(T.KitNotFound, kitName);

                    await using IKitsDbContext dbContext = new WarfareDbContext();

                    if (Context.MatchParameter(1, "level", "lvl"))
                    {
                        if (Context.TryGet(3, out int level))
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

                            kit.UpdateLastEdited(Context.CallerId.m_SteamID);
                            dbContext.Update(kit);
                            await dbContext.SaveChangesAsync(token).ConfigureAwait(false);

                            await UniTask.SwitchToMainThread(token);

                            Context.Reply(T.KitPropertySet, property, kit, newValue);
                            Context.LogAction(ActionLogType.SetKitProperty, kitName + ": LEVEL >> " + newValue.ToUpper());
                            manager.Signs.UpdateSigns(kit);
                            Context.Defer();
                        }
                        else
                            Context.SendCorrectUsage("/kit <set|s> <level|lvl> <kitname> <value: integer>");
                    }
                    else if (Context.MatchParameter(1, "sign", "text"))
                    {
                        Context.AssertHelpCheck(2, "/kit <set|s> <sign> <language (default: " + L.Default + "> <text> - Sets the display text for the kit's kit sign.");

                        LanguageInfo? language = Data.LanguageDataStore.GetInfoCached(newValue, false);
                        if (language == null)
                            throw Context.Reply(T.KitLanguageNotFound, newValue);
                        if (Context.TryGetRange(4, out newValue))
                        {
                            newValue = KitEx.ReplaceNewLineSubstrings(newValue);
                            kit.SetSignText(dbContext, Context.CallerId.m_SteamID, kit, newValue, language);
                            await dbContext.SaveChangesAsync(token);
                            await UniTask.SwitchToMainThread(token);
                            newValue = newValue.Replace("\n", "<br>");
                            Context.Reply(T.KitPropertySet, "sign text", kit, language + " : " + newValue);
                            Context.LogAction(ActionLogType.SetKitProperty, kitName + ": SIGN TEXT >> \"" + newValue + "\"");
                            manager.Signs.UpdateSigns(kit);
                        }
                        else
                            Context.SendCorrectUsage("/kit set sign <kitname> <language> <sign text>");
                    }
                    else if (Context.MatchParameter(1, "faction", "team", "group"))
                    {
                        bool isNull = Context.MatchParameter(3, "null", "none", "blank");
                        FactionInfo? faction = isNull ? null : TeamManager.FindFactionInfo(newValue);
                        if (faction != null || isNull)
                        {
                            kit.Faction = faction?.CreateModel();
                            kit.FactionId = faction?.PrimaryKey;
                            kit.UpdateLastEdited(Context.CallerId.m_SteamID);
                            dbContext.Update(kit);
                            await dbContext.SaveChangesAsync(token);
                            await UniTask.SwitchToMainThread(token);
                            Context.Reply(T.KitPropertySet, "faction", kit, faction?.GetName(Localization.GetDefaultLanguage())!);
                            Context.LogAction(ActionLogType.SetKitProperty, kitName + ": FACTION >> " +
                                                                           (faction?.Name.ToUpper() ?? Translation.Null(TranslationFlags.NoRichText)));
                            manager.Signs.UpdateSigns(kit);
                        }
                        else
                        {
                            Context.SendCorrectUsage("/kit set faction <faction id or search>");
                            Context.ReplyString("Factions: <#aaa>" + string.Join(", ", TeamManager.Factions
                                .OrderBy(x => x.PrimaryKey)
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
                                Context.Reply(T.KitInvalidPropertyValue, newValue,
                                    info switch { FieldInfo i => i.FieldType, PropertyInfo i => i.PropertyType, _ => null! },
                                        property);
                                return;
                            case SetPropertyResult.PropertyProtected:
                                Context.Reply(T.KitPropertyProtected, property);
                                return;
                            case SetPropertyResult.PropertyNotFound:
                            case SetPropertyResult.TypeNotSettable:
                                Context.Reply(T.KitPropertyNotFound, property);
                                return;
                            case SetPropertyResult.ObjectNotFound:
                                Context.Reply(T.KitNotFound, kitName);
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

                                kit.UpdateLastEdited(Context.CallerId.m_SteamID);
                                dbContext.Update(kit);
                                await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
                                await UniTask.SwitchToMainThread(token);
                                manager.Signs.UpdateSigns(kit);
                                Context.Reply(T.KitPropertySet, property, kit, newValue);
                                Context.LogAction(ActionLogType.SetKitProperty, kitName + ": " + property.ToUpper() + " >> " + newValue.ToUpper());
                                if (oldbranch != kit.Branch || oldclass != kit.Class || prevType != kit.Type)
                                {
                                    kit = await manager.GetKit(kit.PrimaryKey, token, x => KitManager.RequestableSet(x, false));
                                    await UniTask.SwitchToMainThread(token);
                                    manager.InvokeAfterMajorKitUpdate(kit, true);
                                }
                                return;
                        }
                    }
                }
                else
                    Context.Reply(T.KitNotFound, kitName);
            }
            else
                Context.SendCorrectUsage("/kit <set|s> <parameter> <kitname> <value>");
        }
        else if (Context.MatchParameter(0, "giveaccess", "givea", "ga"))
        {
            await Context.AssertPermissions(PermissionAccess, token);

            Context.AssertHelpCheck(1, "/kit <giveaccess|givea|ga> <player> <kit id> [access type] - Give the provided player access to the kit with the provided id. Optionally supply an access type: [credits | event | default: purchase]");

            if (Context.TryGet(2, out string kitName) && Context.TryGet(1, out ulong playerId, out UCPlayer? onlinePlayer))
            {
                Kit? kit = await manager.FindKit(kitName, token, true);
                if (kit != null)
                {
                    if (!Context.TryGet(3, out KitAccessType type) || type == KitAccessType.Unknown)
                        type = KitAccessType.Purchase;

                    bool hasAccess = await manager.HasAccess(kit, playerId, token).ConfigureAwait(false);
                    PlayerNames names = await F.GetPlayerOriginalNamesAsync(playerId, token).ConfigureAwait(false);
                    if (hasAccess)
                    {
                        await UniTask.SwitchToMainThread(token);
                        Context.Reply(T.KitAlreadyHasAccess, onlinePlayer as IPlayer ?? names, kit);
                        return;
                    }
                    await manager.GiveAccess(kit, playerId, KitAccessType.Purchase, token).ConfigureAwait(false);
                    KitSync.OnAccessChanged(playerId);
                    Context.LogAction(ActionLogType.ChangeKitAccess, playerId.ToString(Data.AdminLocale) + " GIVEN ACCESS TO " + kitName + ", REASON: " + type);

                    await UniTask.SwitchToMainThread(token);
                    Context.Reply(T.KitAccessGiven, onlinePlayer as IPlayer ?? names, playerId, kit);
                    if (onlinePlayer is not null)
                    {
                        onlinePlayer.SendChat(T.KitAccessGivenDm, kit);
                        manager.Signs.UpdateSigns(kit, onlinePlayer);
                    }
                }
                else
                    Context.Reply(T.KitNotFound, kitName);
            }
            else
                Context.SendCorrectUsage("/kit <giveaccess|givea|ga> <player> <kitname> [credits|purchase|event]");
        }
        else if (Context.MatchParameter(0, "removeaccess", "removea", "ra"))
        {
            await Context.AssertPermissions(PermissionAccess, token);

            Context.AssertHelpCheck(1, "/kit <removeaccess|removea|ra> <player> <kit id> - Revoke access to the kit with the provided id from the provided player.");

            if (Context.TryGet(2, out string kitName) && Context.TryGet(1, out ulong playerId, out UCPlayer? onlinePlayer))
            {
                Kit? kit = await manager.FindKit(kitName, token, true);
                if (kit != null)
                {
                    bool hasAccess = await manager.HasAccess(kit, playerId, token).ConfigureAwait(false);
                    PlayerNames names = await F.GetPlayerOriginalNamesAsync(playerId, token).ConfigureAwait(false);
                    if (!hasAccess)
                    {
                        await UniTask.SwitchToMainThread(token);
                        Context.Reply(T.KitAlreadyMissingAccess, onlinePlayer as IPlayer ?? names, kit);
                        return;
                    }
                    await manager.RemoveAccess(kit, playerId, token).ConfigureAwait(false);
                    Context.LogAction(ActionLogType.ChangeKitAccess, playerId.ToString(Data.AdminLocale) + " DENIED ACCESS TO " + kitName);
                    KitSync.OnAccessChanged(playerId);

                    await UniTask.SwitchToMainThread(token);
                    Context.Reply(T.KitAccessRevoked, onlinePlayer as IPlayer ?? names, playerId, kit);
                    if (onlinePlayer is not null)
                    {
                        onlinePlayer.SendChat(T.KitAccessRevokedDm, kit);
                        manager.Signs.UpdateSigns(kit, onlinePlayer);
                    }
                }
                else
                    Context.Reply(T.KitNotFound, kitName);
            }
            else
                Context.SendCorrectUsage("/kit <removeaccess|removea|ra> <player> <kitname>");
        }
        else if (Context.MatchParameter(0, "copyfrom", "copy", "cf"))
        {
            await Context.AssertPermissions(PermissionCreate, token);

            Context.AssertHelpCheck(1, "/kit <copyfrom|cf> <source kit id> <new kit id> - Creates an exact copy of the source kit renamed to the new kit id.");

            if (Context.TryGet(2, out string kitName) && Context.TryGet(1, out string existingName))
            {
                Kit? existing = await manager.FindKit(existingName, token, set: KitManager.FullSet).ConfigureAwait(false);
                if (existing == null)
                    throw Context.Reply(T.KitNotFound, existingName);

                Kit? kit = await manager.FindKit(kitName, token, set: x => x.Kits).ConfigureAwait(false);
                if (kit != null)
                    throw Context.Reply(T.KitNameTaken, kitName);

                kit = new Kit(kitName.ToLowerInvariant().Replace(' ', '_'), existing)
                {
                    Season = UCWarfare.Season,
                    Disabled = false,
                    Creator = Context.CallerId.m_SteamID
                };

                await using (IKitsDbContext dbContext = new WarfareDbContext())
                {
                    await dbContext.AddAsync(kit, token).ConfigureAwait(false);
                    await dbContext.SaveChangesAsync(token).ConfigureAwait(false);

                    kit.ReapplyPrimaryKey();

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

                    await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
                }
                
                Context.LogAction(ActionLogType.CreateKit, kitName + " COPIED FROM " + existingName);
                await UniTask.SwitchToMainThread(token);
                manager.Signs.UpdateSigns(kit);
                Context.Reply(T.KitCopied, existing, kit);
            }
            else
                Context.SendCorrectUsage("/kit <copyfrom|cf> <kitname> <newkitname>");
        }
        else if (Context.MatchParameter(0, "createloadout", "cloadout", "cl"))
        {
            await Context.AssertPermissions(PermissionCreate, token);

            Context.AssertHelpCheck(1, "/kit <createloadout|cloadout|cl> <player> <class> [sign text...] - Creates and prepares a loadout for the provided player with optional sign text.");

            Context.AssertRanByPlayer();
            if (Context.TryGet(2, out Class @class) && Context.TryGet(1, out ulong playerId, out UCPlayer? onlinePlayer))
            {
                if (onlinePlayer is null && !PlayerSave.HasPlayerSave(playerId))
                    throw Context.Reply(T.PlayerNotFound);

                PlayerNames names = await F.GetPlayerOriginalNamesAsync(playerId, token).ConfigureAwait(false);
                string loadoutId = await manager.Loadouts.GetFreeLoadoutName(playerId).ConfigureAwait(false);
                if (!Context.TryGetRange(3, out string? signText) || string.IsNullOrWhiteSpace(signText))
                    signText = null;
                await UniTask.SwitchToMainThread(token);
                Kit loadout = new Kit(loadoutId, @class, signText)
                {
                    Creator = Context.CallerId.m_SteamID,
                    LastEditor = Context.CallerId.m_SteamID
                };

                Kit? oldKit = await manager.FindKit(loadout.InternalName, token, set: x => x.Kits).ConfigureAwait(false);
                if (oldKit != null)
                    throw Context.Reply(T.KitNameTaken, loadout.InternalName);

                await using (IKitsDbContext dbContext = new WarfareDbContext())
                {
                    await dbContext.AddAsync(loadout, token).ConfigureAwait(false);

                    await dbContext.SaveChangesAsync(token).ConfigureAwait(false);

                    await UniTask.SwitchToMainThread(token);
                    loadout.SetItemArray(ItemUtility.ItemsFromInventory(Context.Player, findAssetRedirects: true), dbContext);

                    await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
                }

                await manager.GiveAccess(loadout, playerId, KitAccessType.Purchase, token).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(token);

                KitSync.OnAccessChanged(playerId);

                Context.LogAction(ActionLogType.CreateKit, loadout.InternalName);
                manager.Signs.UpdateSigns(loadout);
                Context.Reply(T.LoadoutCreated, @class, onlinePlayer as IPlayer ?? names, playerId, loadout);
            }
            else
                throw Context.SendCorrectUsage("/kit <createloadout|cloadout|cl> <player> <class> [sign text...]");
        }
        else if (Context.MatchParameter(0, "skills", "skillset", "skillsets"))
        {
            await Context.AssertPermissions(PermissionSet, token);

            Context.AssertHelpCheck(1, "/kit skills <kit> <add|remove> <skill> [level]");

            bool add = Context.MatchParameter(2, "add", "set");

            if (!add && !Context.MatchParameter(2, "delete", "remove", "clear"))
                throw Context.SendCorrectUsage("/kit skills <kit> <add|remove> <skill> [level]");

            if (!Context.TryGet(3, out string skillsetStr))
                throw Context.SendCorrectUsage("/kit skills <kit> <add|remove> <skill> [level]");

            int skillset = Skillset.GetSkillsetFromEnglishName(skillsetStr, out EPlayerSpeciality specialty);
            if (skillset < 0)
                throw Context.Reply(T.KitInvalidSkillset, skillsetStr);

            byte level = 0;
            if (add && !Context.TryGet(4, out level))
            {
                throw Context.Reply(T.KitInvalidSkillsetLevel, specialty switch
                {
                    EPlayerSpeciality.DEFENSE => Localization.TranslateEnum((EPlayerDefense)skillset, Context.Language),
                    EPlayerSpeciality.OFFENSE => Localization.TranslateEnum((EPlayerOffense)skillset, Context.Language),
                    EPlayerSpeciality.SUPPORT => Localization.TranslateEnum((EPlayerSupport)skillset, Context.Language),
                    _ => skillset.ToString()
                }, level);
            }

            Skill skill = Context.Player.UnturnedPlayer.skills.skills[(int)specialty][skillset];
            int max = skill.GetClampedMaxUnlockableLevel();
            if (!add || max >= level)
            {
                Skillset set = new Skillset(specialty, (byte)skillset, level);
                string kitName = Context.Get(1)!;
                Kit? kit = await manager.FindKit(kitName, token, true, set => set.Kits.Include(x => x.Skillsets));

                if (kit is null)
                    throw Context.Reply(T.KitNotFound, kitName);

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
                    throw Context.Reply(T.KitSkillsetNotFound, set, kit);

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
                Context.LogAction(add ? ActionLogType.AddSkillset : ActionLogType.RemoveSkillset, set + " ON " + kit.InternalName);
                await UniTask.SwitchToMainThread(token);
                Context.Reply(add ? T.KitSkillsetAdded : T.KitSkillsetRemoved, set, kit);
                for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                {
                    UCPlayer player = PlayerManager.OnlinePlayers[i];
                    uint? activeKit = player.ActiveKit;
                    if (!activeKit.HasValue || activeKit.Value != kit.PrimaryKey)
                        continue;

                    if (add)
                        player.EnsureSkillset(set);
                    else
                        player.RemoveSkillset(set.Speciality, set.SkillIndex);
                }

                return;
            }
            throw Context.Reply(T.KitInvalidSkillsetLevel, specialty switch
            {
                EPlayerSpeciality.DEFENSE => Localization.TranslateEnum((EPlayerDefense)skillset, Context.Language),
                EPlayerSpeciality.OFFENSE => Localization.TranslateEnum((EPlayerOffense)skillset, Context.Language),
                EPlayerSpeciality.SUPPORT => Localization.TranslateEnum((EPlayerSupport)skillset, Context.Language),
                _ => skillset.ToString()
            }, level);
        }
        else Context.SendCorrectUsage(Syntax);
    }
}
