using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Steamworks;
using Uncreated.Framework;
using Uncreated.Players;
using Uncreated.SQL;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Sync;
using Uncreated.Warfare.Teams;
using UnityEngine;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public sealed class KitCommand : AsyncCommand
{
    private const string SYNTAX = "/kit <search|create|delete|give|set|giveaccess|removeacces|copyfrom|createloadout>";
    private const string HELP = "Admin command to manage kits; creating, deleting, editing, and giving/removing access is done through this command.";

    public KitCommand() : base("kit", EAdminType.STAFF) { }

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

        ctx.AssertArgs(1, SYNTAX + " - " + HELP);

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

            ctx.AssertHelpCheck(1, "/kit <create|c|override> <id> [class] [type] [faction] - Creates (or overrides if it already exits) a kit with default values based on the items in your inventory and your clothes.");

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
                        ctx.LogAction(EActionLogType.EDIT_KIT, "OVERRIDE ITEMS " + kit.Id + ".");
                        await proxy.SaveItem(token).ConfigureAwait(false);
                        await UCWarfare.ToUpdate();
                        KitManager.UpdateSigns(kit);
                        ctx.Reply(T.KitOverwrote, kit!);
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
                KitType type = KitType.Public;
                bool def = ctx.MatchParameter(2, "default") || ctx.MatchParameter(2, Default);
                if (def || ctx.MatchParameter(2, "me") || ctx.TryGet(2, out @class))
                {
                    if (def) @class = Class.None;
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

                kit = new Kit(kitName, @class, KitManager.GetDefaultBranch(@class), type, SquadLevel.Member, faction);
                await manager.AddOrUpdate(kit, token).ThenToUpdate(token);
                ctx.LogAction(EActionLogType.CREATE_KIT, kitName);
                await UCWarfare.ToUpdate();
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
                    await proxy.Delete(token).ConfigureAwait(false);
                    ctx.LogAction(EActionLogType.DELETE_KIT, kitName);
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
                    await manager.GiveKit(ctx.Caller, proxy, token).ThenToUpdate(token);
                    ctx.LogAction(EActionLogType.GIVE_KIT, kitName);
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
                                await proxy.SaveItem(token).ThenToUpdate(token);
                                ctx.Reply(T.KitPropertySet, property, proxy.Item, newValue);
                                ctx.LogAction(EActionLogType.SET_KIT_PROPERTY, kitName + ": LEVEL >> " + newValue.ToUpper());
                                KitManager.UpdateSigns(proxy.Item);
                                ctx.Defer();
                            }
                            else
                                ctx.SendCorrectUsage("/kit <set|s> <level|lvl> <kitname> <value: integer>");
                        }
                        else if (ctx.MatchParameter(1, "sign", "text"))
                        {
                            ctx.AssertHelpCheck(2, "/kit <set|s> <sign> <language (default: en-us> <text> - Sets the display text for the kit's kit sign.");

                            string language = newValue;
                            if (ctx.TryGetRange(4, out newValue))
                            {
                                newValue = newValue.Replace("\\n", "\n");
                                KitManager.SetTextNoLock(proxy.Item, newValue, language);
                                await proxy.SaveItem(token).ThenToUpdate(token);
                                newValue = newValue.Replace('\n', '\\');
                                ctx.Reply(T.KitPropertySet, "sign text", proxy.Item, language + " : " + newValue);
                                ctx.LogAction(EActionLogType.SET_KIT_PROPERTY, kitName + ": SIGN TEXT >> \"" + newValue + "\"");
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
                                await proxy.SaveItem(token).ThenToUpdate(token);
                                ctx.Reply(T.KitPropertySet, "faction", proxy.Item, faction?.GetName(L.DEFAULT)!);
                                ctx.LogAction(EActionLogType.SET_KIT_PROPERTY, kitName + ": FACTION >> " +
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
                                    await proxy.SaveItem(token).ThenToUpdate(token);
                                    await UCWarfare.ToUpdate();
                                    KitManager.UpdateSigns(kit);
                                    ctx.Reply(T.KitPropertySet, property, kit, newValue);
                                    ctx.LogAction(EActionLogType.SET_KIT_PROPERTY, kitName + ": " + property.ToUpper() + " >> " + newValue.ToUpper());
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
                        ctx.LogAction(EActionLogType.CHANGE_KIT_ACCESS, playerId.ToString(Data.AdminLocale) + " GIVEN ACCESS TO " + kitName + ", REASON: " + type);

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
                        ctx.LogAction(EActionLogType.CHANGE_KIT_ACCESS, playerId.ToString(Data.AdminLocale) + " DENIED ACCESS TO " + kitName);

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
                        Disabled = false
                    };
                }
                finally
                {
                    existing.Release();
                }

                await manager.AddOrUpdate(kit, token).ConfigureAwait(false);
                ctx.LogAction(EActionLogType.CREATE_KIT, kitName + " COPIED FROM " + existingName);
                await UCWarfare.ToUpdate();
                KitManager.UpdateSigns(kit);
                ctx.Reply(T.KitCopied, existing.Item, kit);
            }
            else
                ctx.SendCorrectUsage("/kit <copyfrom|cf> <kitname> <newkitname>");
        }
        else if (ctx.MatchParameter(0, "createloadout", "cloadout", "cl"))
        {
            ctx.AssertHelpCheck(1, "/kit <createloadout|cloadout|cl> <player> <team (1 = " +
                                   TeamManager.TranslateShortName(1, ctx.CallerID, false) + ", 2 = " + TeamManager.TranslateShortName(2, ctx.CallerID, false) +
                                   ")> <class> [sign text...] - Creates and prepares a loadout for the provided player with optional sign text.");

            ctx.AssertRanByPlayer();
            if (ctx.TryGet(3, out Class @class) && ctx.TryGet(2, out ulong team) && ctx.TryGet(1, out ulong playerId, out UCPlayer? onlinePlayer))
            {
                if (onlinePlayer is null && !PlayerSave.HasPlayerSave(playerId))
                {
                    ctx.Reply(T.PlayerNotFound);
                    return;
                }
                Task.Run(async () =>
                {
                    PlayerNames names = onlinePlayer is not null ? onlinePlayer.Name : await Data.DatabaseManager.GetUsernamesAsync(playerId);
                    char let = await KitManager.GetLoadoutCharacter(playerId);
                    string loadoutName = playerId.ToString() + "_" + let;
                    if (!ctx.TryGetRange(4, out string signText) || string.IsNullOrEmpty(signText))
                        signText = loadoutName;
                    if (let <= 'z' && !KitManager.KitExists(loadoutName, out _))
                    {
                        await UCWarfare.ToUpdate();
                        KitOld loadout = new KitOld(loadoutName, KitManager.ItemsFromInventory(ctx.Caller!), KitManager.ClothesFromInventory(ctx.Caller!));

                        loadout.IsLoadout = true;
                        loadout.Team = team;
                        loadout.Class = @class;
                        if (@class == Class.Pilot)
                            loadout.Branch = Branch.Airforce;
                        else if (@class == Class.Crewman)
                            loadout.Branch = Branch.Armor;
                        else
                            loadout.Branch = Branch.Infantry;

                        loadout.TeamLimit = KitManager.GetDefaultTeamLimit(@class);

                        KitManager.SetText(loadout, signText, L.DEFAULT, false);

                        loadout = await KitManager.AddKit(loadout);
                        await KitManager.GiveAccess(loadout, playerId, KitAccessType.PURCHASE);
                        ctx.LogAction(EActionLogType.CREATE_KIT, loadoutName);
                        await UCWarfare.ToUpdate();
                        KitManager.UpdateSigns(loadout);
                        ctx.Reply(T.LoadoutCreated, @class, onlinePlayer as IPlayer ?? names, playerId, loadout);
                        KitManager.InvokeKitCreated(loadout);
                    }
                    else
                    {
                        await UCWarfare.ToUpdate();
                        ctx.Reply(T.KitNameTaken, loadoutName);
                    }
                }).ConfigureAwait(false);
                ctx.Defer();
            }
            else
                ctx.SendCorrectUsage("/kit <createloadout|cloadout|cl> <player> <team (1 = " + TeamManager.Team1Code + ", 2 = " + TeamManager.Team2Code + "> <class> <sign text>");
        }
        else ctx.SendCorrectUsage(SYNTAX);
    }
}
