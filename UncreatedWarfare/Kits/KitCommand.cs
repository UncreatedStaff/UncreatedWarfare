﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SDG.Unturned;
using Uncreated.Framework;
using Uncreated.Players;
using Uncreated.SQL;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Commands;
public sealed class KitCommand : AsyncCommand
{
    private const string Syntax = "/kit <search|skills|create|delete|give|set|giveaccess|removeacces|copyfrom|createloadout>";
    private const string Help = "Admin command to manage kits; creating, deleting, editing, and giving/removing access is done through this command.";

    public KitCommand() : base("kit", EAdminType.STAFF)
    {
        Structure = new CommandStructure
        {
            Description = Help,
            Parameters = new CommandParameter[]
            {
                new CommandParameter("Search")
                {
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
                    Description = "Delete a kit.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Kit", typeof(Kit))
                    }
                },
                new CommandParameter("Give")
                {
                    Description = "Gives the caller a kit.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Kit", typeof(Kit))
                    }
                },
                new CommandParameter("Set")
                {
                    Description = "Sets a property of a kit.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Sign")
                        {
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
                    Description = "Give a player access to a non-public kit.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Player", typeof(IPlayer))
                        {
                            ChainDisplayCount = 3,
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
                    Description = "Create a loadout with some default parameters.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Player", typeof(IPlayer))
                        {
                            ChainDisplayCount = 4,
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("Faction", typeof(FactionInfo))
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
                        }
                    }
                },
                new CommandParameter("Skills")
                {
                    Description = "Modify the skillset overrides on a kit.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Add")
                        {
                            ChainDisplayCount = 3,
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
                            ChainDisplayCount = 2,
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("Skill", typeof(string))
                            }
                        }
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

        ctx.AssertOnDuty();

        ctx.AssertArgs(1, Syntax + " - " + Help);

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
                        kit.Items = UCInventoryManager.ItemsFromInventory(ctx.Caller, findAssetRedirects: true);
                        kit.ItemListCache = null;
                        kit.UpdateLastEdited(ctx.CallerID);
                        ctx.LogAction(ActionLogType.EditKit, "OVERRIDE ITEMS " + kit.Id + ".");
                        await proxy.SaveItem(token).ConfigureAwait(false);
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
                    Items = UCInventoryManager.ItemsFromInventory(ctx.Caller, findAssetRedirects: true),
                };
                kit.Creator = kit.LastEditor = ctx.CallerID;
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
                }
                else
                    ctx.Reply(T.KitNotFound, kitName);
            }
            else
                ctx.SendCorrectUsage("/kit <delete|d|remove> <kit name>");
        }
        else if (ctx.MatchParameter(0, "give", "g"))
        {
            ctx.AssertHelpCheck(1, "/kit <give|g> <id> - Equips you with the kit with the id provided.");

            ctx.AssertRanByPlayer();

            if (ctx.TryGet(1, out string kitName))
            {
                SqlItem<Kit>? proxy = await manager.FindKit(kitName, token, true);
                if (proxy?.Item != null)
                {
                    Class @class = proxy.Item.Class;
                    await manager.GiveKit(ctx.Caller, proxy, token).ConfigureAwait(false);
                    await UCWarfare.ToUpdate(token);
                    ctx.LogAction(ActionLogType.GiveKit, kitName);
                    ctx.Reply(T.RequestSignGiven, @class);
                }
                else
                    throw ctx.Reply(T.KitNotFound, kitName);
            }
            else
                ctx.SendCorrectUsage("/kit <give|g> <kitName>");
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
                                ctx.Reply(T.KitPropertySet, "faction", proxy.Item, faction?.GetName(L.Default)!);
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
                            bool isDefLim = Mathf.Abs(kit.TeamLimit - KitManager.GetDefaultTeamLimit(oldclass)) < 0.005f;
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
                                    if (kit.Class != oldclass && isDefLim)
                                        kit.TeamLimit = KitManager.GetDefaultTeamLimit(kit.Class);
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
                    await proxy.Enter(token).ConfigureAwait(false);
                    try
                    {
                        if (proxy.Item == null)
                            throw ctx.Reply(T.KitNotFound, kitName);
                        
                        if (!ctx.TryGet(3, out KitAccessType type) || type == KitAccessType.Unknown)
                            type = KitAccessType.Purchase;

                        bool hasAccess = await KitManager.HasAccess(proxy.Item, playerId, token).ConfigureAwait(false);
                        PlayerNames names = await F.GetPlayerOriginalNamesAsync(playerId, token).ConfigureAwait(false);
                        if (hasAccess)
                        {
                            await UCWarfare.ToUpdate(token);
                            ctx.Reply(T.KitAlreadyHasAccess, onlinePlayer as IPlayer ?? names, proxy.Item);
                            return;
                        }
                        await KitManager.GiveAccess(proxy, playerId, KitAccessType.Purchase, token).ConfigureAwait(false);
                        ctx.LogAction(ActionLogType.ChangeKitAccess, playerId.ToString(Data.AdminLocale) + " GIVEN ACCESS TO " + kitName + ", REASON: " + type);

                        await UCWarfare.ToUpdate();
                        ctx.Reply(T.KitAccessGiven, onlinePlayer as IPlayer ?? names, playerId, proxy.Item);
                        if (onlinePlayer is not null)
                        {
                            onlinePlayer.SendChat(T.KitAccessGivenDm, proxy.Item);
                            KitManager.UpdateSigns(proxy.Item, onlinePlayer);
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
                    await proxy.Enter(token).ConfigureAwait(false);
                    try
                    {
                        if (proxy.Item == null)
                            throw ctx.Reply(T.KitNotFound, kitName);

                        bool hasAccess = await KitManager.HasAccess(proxy.Item, playerId, token).ConfigureAwait(false);
                        PlayerNames names = await F.GetPlayerOriginalNamesAsync(playerId, token).ConfigureAwait(false);
                        if (!hasAccess)
                        {
                            await UCWarfare.ToUpdate(token);
                            ctx.Reply(T.KitAlreadyMissingAccess, onlinePlayer as IPlayer ?? names, proxy.Item);
                            return;
                        }
                        await KitManager.RemoveAccess(proxy, playerId, token).ConfigureAwait(false);
                        ctx.LogAction(ActionLogType.ChangeKitAccess, playerId.ToString(Data.AdminLocale) + " DENIED ACCESS TO " + kitName);

                        await UCWarfare.ToUpdate();
                        ctx.Reply(T.KitAccessRevoked, onlinePlayer as IPlayer ?? names, playerId, proxy.Item);
                        if (onlinePlayer is not null)
                        {
                            onlinePlayer.SendChat(T.KitAccessRevokedDm, proxy.Item);
                            KitManager.UpdateSigns(proxy.Item, onlinePlayer);
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
                ctx.SendCorrectUsage("/kit <removeaccess|removea|ra> <player> <kitname>");
        }
        else if (ctx.MatchParameter(0, "copyfrom", "cf"))
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
                    kit = new Kit(kitName, existing.Item)
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
            ctx.AssertHelpCheck(1, "/kit <createloadout|cloadout|cl> <player> <faction> <class> [sign text...] - Creates and prepares a loadout for the provided player with optional sign text.");

            ctx.AssertRanByPlayer();
            if (ctx.TryGet(3, out Class @class) && ctx.TryGet(2, out string factionStr) && ctx.TryGet(1, out ulong playerId, out UCPlayer? onlinePlayer))
            {
                if (onlinePlayer is null && !PlayerSave.HasPlayerSave(playerId))
                    throw ctx.Reply(T.PlayerNotFound);
                FactionInfo? faction = TeamManager.FindFactionInfo(factionStr);
                if (faction == null)
                    throw ctx.Reply(T.FactionNotFoundCreateKit, factionStr);

                PlayerNames names = await F.GetPlayerOriginalNamesAsync(playerId, token).ConfigureAwait(false);
                char let = await KitManager.GetLoadoutCharacter(playerId);
                if (!ctx.TryGetRange(4, out string? signText) || string.IsNullOrWhiteSpace(signText))
                    signText = null;
                await UCWarfare.ToUpdate(token);
                Kit loadout = new Kit(playerId, let, @class, signText, faction)
                {
                    Items = UCInventoryManager.ItemsFromInventory(ctx.Caller, findAssetRedirects: true),
                    Creator = ctx.CallerID,
                    LastEditor = ctx.CallerID
                };
                SqlItem<Kit>? oldKit = await manager.FindKit(loadout.Id, token).ConfigureAwait(false);
                if (let <= 'z' && oldKit?.Item == null)
                {
                    await UCWarfare.ToUpdate();
                    SqlItem<Kit> kit = await manager.AddOrUpdate(loadout, token).ConfigureAwait(false);
                    await KitManager.GiveAccess(kit, playerId, KitAccessType.Purchase, token).ConfigureAwait(false);
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
                throw ctx.SendCorrectUsage("/kit <createloadout|cloadout|cl> <player> <faction> <class> [sign text...]");
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
                        EPlayerSpeciality.DEFENSE => Localization.TranslateEnum((EPlayerDefense)skillset, ctx.CallerID),
                        EPlayerSpeciality.OFFENSE => Localization.TranslateEnum((EPlayerOffense)skillset, ctx.CallerID),
                        EPlayerSpeciality.SUPPORT => Localization.TranslateEnum((EPlayerSupport)skillset, ctx.CallerID),
                        _ => skillset.ToString()
                    }, level);
                }
            }
            throw ctx.SendCorrectUsage("/kit skills <kit> <add|remove> <skill> [level]");
        }
        else ctx.SendCorrectUsage(Syntax);
    }
}
