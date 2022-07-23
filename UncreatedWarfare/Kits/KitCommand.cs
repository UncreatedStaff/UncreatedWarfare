using SDG.Unturned;
using System;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Teams;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class KitCommand : Command
{
    private const string SYNTAX = "/kit <search|create|delete|give|set|giveaccess|removeacces|copyfrom|createloadout>";
    private const string HELP = "Admin command to manage kits; creating, deleting, editing, and giving/removing access is done through this command.";

    public KitCommand() : base("kit", EAdminType.STAFF) { }

    public override void Execute(CommandInteraction ctx)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertGamemode<IKitRequests>();

        ctx.AssertOnDuty();

        ctx.AssertArgs(1, SYNTAX + " - " + HELP);

        KitManager singleton = KitManager.GetSingleton() ?? throw ctx.SendGamemodeError();

        if (ctx.MatchParameter(0, "search", "find"))
        {
            ctx.AssertHelpCheck(1, "/kit search <term> - Searches for the search term within display names of all kits and tells you the id of all results.");

            if (ctx.TryGetRange(1, out string searchTerm))
            {
                string res = KitManager.Search(searchTerm);
                if (res.Length < 0)
                    res += "--";
                ctx.Reply("kit_search_results", res);
            }
            else
                ctx.SendCorrectUsage("/kit <search|find> <term>");
        }
        else if (ctx.MatchParameter(0, "create", "c", "override"))
        {
            ctx.AssertRanByPlayer();

            ctx.AssertHelpCheck(1, "/kit <create|c|override> <id> - Creates (or overrides if it already exits) a kit with default values based on the items in your inventory and your clothes.");

            if (ctx.TryGet(1, out string kitName))
            {
                if (!KitManager.KitExists(kitName, out Kit kit)) // create kit
                {
                    Task.Run(async () =>
                    {
                        Kit? kit = await KitManager.AddKit(new Kit(kitName, KitManager.ItemsFromInventory(ctx.Caller!), KitManager.ClothesFromInventory(ctx.Caller!)));
                        if (kit is not null)
                        {
                            ctx.LogAction(EActionLogType.CREATE_KIT, kitName);
                            await UCWarfare.ToUpdate();
                            KitManager.UpdateSigns(kit);
                            ctx.Reply("kit_created", kitName);
                        }
                        else
                        {
                            await UCWarfare.ToUpdate();
                            ctx.SendUnknownError();
                        }
                    }).ConfigureAwait(false);
                }
                else // overwrite kit
                {
                    Task.Run(async () =>
                    {
                        kit.Items = KitManager.ItemsFromInventory(ctx.Caller!);
                        kit.Clothes = KitManager.ClothesFromInventory(ctx.Caller!);
                        kit = (await KitManager.AddKit(kit))!;
                        if (kit != null)
                        {
                            ctx.LogAction(EActionLogType.EDIT_KIT, kitName);
                            await UCWarfare.ToUpdate();
                            KitManager.UpdateSigns(kit);
                            ctx.Reply("kit_overwritten", kitName);
                        }
                        else
                        {
                            await UCWarfare.ToUpdate();
                            ctx.Reply("kit_overwritten", kitName);
                        }
                    }).ConfigureAwait(false);
                }

                ctx.Defer();
            }
            else
                ctx.SendCorrectUsage("/kit <create|c|override> <kit name>");
        }
        else if (ctx.MatchParameter(0, "delete", "d", "remove"))
        {
            ctx.AssertHelpCheck(1, "/kit <delete|d|remove> <id> - Deletes the kit with the provided id.");

            if (ctx.TryGet(1, out string kitName))
            {
                if (KitManager.KitExists(kitName, out Kit kit))
                {
                    Task.Run(async () =>
                    {
                        if (await KitManager.DeleteKit(kit))
                        {
                            ctx.LogAction(EActionLogType.DELETE_KIT, kitName);

                            await UCWarfare.ToUpdate();
                            RequestSigns.RemoveRequestSigns(kitName);
                            ctx.Reply("kit_deleted", kitName);
                        }
                        else
                        {
                            await UCWarfare.ToUpdate();
                            ctx.SendUnknownError();
                        }
                    }).ConfigureAwait(false);
                    ctx.Defer();
                }
                else
                    ctx.Reply("kit_e_noexist", kitName);
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
                if (KitManager.KitExists(kitName, out Kit kit))
                {
                    bool branchChanged = false;
                    if (KitManager.HasKit(ctx.Caller!, out Kit oldkit) && kit.Branch != EBranch.DEFAULT && oldkit.Branch != kit.Branch)
                        branchChanged = true;

                    ctx.LogAction(EActionLogType.GIVE_KIT, kitName);

                    KitManager.GiveKit(ctx.Caller!, kit);
                    ctx.Reply("request_kit_given", kit.DisplayName.ToUpper());

                    if (branchChanged)
                    {
                        ctx.Caller!.Branch = kit.Branch;
                    }

                    PlayerManager.ApplyTo(ctx.Caller!);
                }
                else
                    ctx.Reply("kit_e_noexist", kitName);
            }
            else
                ctx.SendCorrectUsage("/kit <give|g> <kitName>");
        }
        else if (ctx.MatchParameter(0, "set", "s"))
        {
            ctx.AssertHelpCheck(1, "/kit <set|s> <level|sign|property> <value> - Sets the level requirement, sign text, or other properties to value. To set default sign text use: /kit set sign <kit id> en-us <text>.");

            if (ctx.TryGet(3, out string newValue) && ctx.TryGet(2, out string kitName) && ctx.TryGet(1, out string property))
            {
                if (KitManager.KitExists(kitName, out Kit kit))
                {
                    if (ctx.MatchParameter(1, "level", "lvl"))
                    {
                        if (ctx.TryGet(3, out int level))
                        {
                            if (level == 0)
                                kit.RemoveLevelUnlock();
                            else
                                kit.AddSimpleLevelUnlock(level);
                            Task.Run(async () =>
                            {
                                Kit _kit = await KitManager.AddKit(kit);
                                await UCWarfare.ToUpdate();
                                ctx.Reply("kit_setprop", property, kitName, newValue);
                                ctx.LogAction(EActionLogType.SET_KIT_PROPERTY, kitName + ": " + property.ToUpper() + " >> " + newValue.ToUpper());
                                KitManager.UpdateSigns(_kit);
                            }).ConfigureAwait(false);
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
                            KitManager.UpdateText(kit, newValue, language);
                            Task.Run(async () =>
                            {
                                Kit _kit = await KitManager.AddKit(kit);
                                newValue = newValue.Replace('\n', '\\');
                                await UCWarfare.ToUpdate();
                                ctx.Reply("kit_setprop", "sign text", kitName, language + " : " + newValue);
                                ctx.LogAction(EActionLogType.SET_KIT_PROPERTY, kitName + ": SIGN TEXT >> \"" + newValue + "\"");
                                KitManager.UpdateSigns(_kit);
                            }).ConfigureAwait(false);
                            ctx.Defer();
                        }
                        else
                            ctx.SendCorrectUsage("/kit set sign <kitname> <language> <sign text>");
                    }
                    else
                    {
                        if (property.Equals("team", StringComparison.OrdinalIgnoreCase))
                        {
                            if (newValue.Equals(TeamManager.Team1Code, StringComparison.OrdinalIgnoreCase) || newValue.Equals(TeamManager.Team1Name, StringComparison.OrdinalIgnoreCase) || newValue == "t1")
                                newValue = "1";
                            else if (newValue.Equals(TeamManager.Team2Code, StringComparison.OrdinalIgnoreCase) || newValue.Equals(TeamManager.Team2Name, StringComparison.OrdinalIgnoreCase) || newValue == "t2")
                                newValue = "2";
                            else if (newValue.Equals(TeamManager.AdminCode, StringComparison.OrdinalIgnoreCase) || newValue.Equals(TeamManager.AdminName, StringComparison.OrdinalIgnoreCase) || newValue == "t3")
                                newValue = "3";
                        }
                        bool wasLoadout = kit.IsLoadout;
                        bool wasPremium = kit.IsPremium;
                        ESetFieldResult result = KitEx.SetProperty(kit, property, newValue);
                        switch (result)
                        {
                            default:
                            case ESetFieldResult.INVALID_INPUT:
                                ctx.Reply("kit_e_invalidarg", newValue, property);
                                return;
                            case ESetFieldResult.FIELD_PROTECTED:
                                ctx.Reply("kit_e_invalidarg_not_allowed", property);
                                return;
                            case ESetFieldResult.FIELD_NOT_SERIALIZABLE:
                            case ESetFieldResult.FIELD_NOT_FOUND:
                                ctx.Reply("kit_e_invalidprop", property);
                                return;
                            case ESetFieldResult.OBJECT_NOT_FOUND:
                                ctx.Reply("kit_e_noexist", kitName);
                                return;
                            case ESetFieldResult.SUCCESS:
                                Task.Run(async () =>
                                {
                                    KitManager.UpdateSigns(await KitManager.AddKit(kit));
                                    await UCWarfare.ToUpdate();
                                    ctx.Reply("kit_setprop", property, kitName, newValue);
                                    ctx.LogAction(EActionLogType.SET_KIT_PROPERTY, kitName + ": " + property.ToUpper() + " >> " + newValue.ToUpper());
                                }).ConfigureAwait(false);
                                ctx.Defer();
                                return;
                        }
                    }
                }
                else
                    ctx.Reply("kit_e_noexist", kitName);
            }
            else
                ctx.SendCorrectUsage("/kit <set|s> <parameter> <kitname> <value>");
        }
        else if (ctx.MatchParameter(0, "giveaccess", "givea", "ga"))
        {
            ctx.AssertHelpCheck(1, "/kit <giveaccess|givea|ga> <player> <kit id> [access type] - Give the provided player access to the kit with the provided id. Optionally supply an access type: [credits | event | default: purchase]");

            if (ctx.TryGet(2, out string kitName) && ctx.TryGet(1, out ulong playerId, out UCPlayer? onlinePlayer))
            {
                if (KitManager.KitExists(kitName, out Kit kit))
                {
                    if (onlinePlayer is null && !PlayerSave.HasPlayerSave(playerId))
                    {
                        ctx.Reply("kit_e_noplayer", playerId.ToString(Data.Locale));
                        return;
                    }
                    if (!ctx.TryGet(3, out EKitAccessType type) || type == EKitAccessType.UNKNOWN)
                        type = EKitAccessType.PURCHASE;
                    Task.Run(async () =>
                    {
                        bool hasAccess;
                        string username;
                        if (onlinePlayer is not null)
                        {
                            username = onlinePlayer.CharacterName;
                            hasAccess = KitManager.HasAccessFast(kit, onlinePlayer);
                        }
                        else
                        {
                            username = (await Data.DatabaseManager.GetUsernamesAsync(playerId)).CharacterName;
                            hasAccess = await KitManager.HasAccess(kit, playerId);
                        }
                        if (hasAccess)
                        {
                            await UCWarfare.ToUpdate();
                            ctx.Reply("kit_e_alreadyaccess", username, kitName);
                            return;
                        }
                        if (onlinePlayer is not null)
                            await KitManager.GiveAccess(kit, onlinePlayer, EKitAccessType.PURCHASE);
                        else
                            await KitManager.GiveAccess(kit, playerId, EKitAccessType.PURCHASE);
                        ctx.LogAction(EActionLogType.CHANGE_KIT_ACCESS, playerId.ToString(Data.Locale) + " GIVEN ACCESS TO " + kitName + ", REASON: " + type.ToString());

                        await UCWarfare.ToUpdate();
                        ctx.Reply("kit_accessgiven", username, kitName);
                        if (onlinePlayer is not null)
                        {
                            onlinePlayer.SendChat("kit_accessgiven_dm", kit.GetDisplayName());
                            KitManager.UpdateSigns(kit, onlinePlayer);
                        }
                    });
                    ctx.Defer();
                }
                else
                    ctx.Reply("kit_e_noexist", kitName);
            }
            else
                ctx.SendCorrectUsage("/kit <giveaccess|givea|ga> <player> <kitname> [credits|purchase|event]");
        }
        else if (ctx.MatchParameter(0, "removeaccess", "removea", "ra"))
        {
            ctx.AssertHelpCheck(1, "/kit <removeaccess|removea|ra> <player> <kit id> - Revoke access to the kit with the provided id from the provided player.");

            if (ctx.TryGet(2, out string kitName) && ctx.TryGet(1, out ulong playerId, out UCPlayer? onlinePlayer))
            {
                if (KitManager.KitExists(kitName, out Kit kit))
                {
                    if (onlinePlayer is null && !PlayerSave.HasPlayerSave(playerId))
                    {
                        ctx.Reply("kit_e_noplayer", playerId.ToString(Data.Locale));
                        return;
                    }
                    Task.Run(async () =>
                    {
                        bool hasAccess;
                        string username;
                        if (onlinePlayer is not null)
                        {
                            username = onlinePlayer.CharacterName;
                            hasAccess = KitManager.HasAccessFast(kit, onlinePlayer);
                        }
                        else
                        {
                            username = (await Data.DatabaseManager.GetUsernamesAsync(playerId)).CharacterName;
                            hasAccess = await KitManager.HasAccess(kit, playerId);
                        }
                        if (!hasAccess)
                        {
                            await UCWarfare.ToUpdate();
                            ctx.Reply("kit_e_noaccess", username, kitName);
                            return;
                        }
                        if (onlinePlayer is not null)
                            await KitManager.RemoveAccess(kit, onlinePlayer);
                        else
                            await KitManager.RemoveAccess(kit, playerId);
                        ctx.LogAction(EActionLogType.CHANGE_KIT_ACCESS, playerId.ToString(Data.Locale) + " DENIED ACCESS TO " + kitName);

                        await UCWarfare.ToUpdate();
                        ctx.Reply("kit_accessremoved", username, kitName);
                        if (onlinePlayer is not null)
                        {
                            onlinePlayer.SendChat("kit_accessremoved_dm", kit.GetDisplayName());
                            KitManager.UpdateSigns(kit, onlinePlayer);
                        }
                    });
                    ctx.Defer();
                }
                else
                    ctx.Reply("kit_e_noexist", kitName);
            }
            else
                ctx.SendCorrectUsage("/kit <removeaccess|removea|ra> <player> <kitname>");
        }
        else if (ctx.MatchParameter(0, "copyfrom", "cf"))
        {
            ctx.AssertHelpCheck(1, "/kit <copyfrom|cf> <source kit id> <new kit id> - Creates an exact copy of the source kit renamed to the new kit id.");

            if (ctx.TryGet(2, out string kitName) && ctx.TryGet(1, out string existingName))
            {
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
                            Skillsets = existing.Skillsets,
                            UnlockRequirements = existing.UnlockRequirements,
                            CreditCost = existing.CreditCost,
                            IsPremium = existing.IsPremium,
                            PremiumCost = existing.PremiumCost,
                            IsLoadout = existing.IsLoadout,
                            TeamLimit = existing.TeamLimit,
                            Cooldown = existing.Cooldown,
                            SignTexts = existing.SignTexts,
                            Weapons = existing.Weapons
                        };

                        Task.Run(async () =>
                        {
                            await KitManager.AddKit(newKit);
                            ctx.LogAction(EActionLogType.CREATE_KIT, kitName + " COPIED FROM " + existingName);
                            await UCWarfare.ToUpdate();
                            KitManager.UpdateSigns(newKit);
                            ctx.Reply("kit_copied", existing.Name, newKit.Name);
                        });
                        ctx.Defer();
                    }
                    else
                        ctx.Reply("kit_e_exist", kitName);
                }
                else
                    ctx.Reply("kit_e_noexist", existingName);
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
            if (ctx.TryGet(3, out EClass @class) && ctx.TryGet(2, out ulong team) && ctx.TryGet(1, out ulong playerId, out UCPlayer? onlinePlayer))
            {
                if (onlinePlayer is null && !PlayerSave.HasPlayerSave(playerId))
                {
                    ctx.Reply("kit_l_e_playernotfound", playerId.ToString());
                    return;
                }
                Task.Run(async () =>
                {
                    string username = onlinePlayer is not null ? onlinePlayer.CharacterName : (await Data.DatabaseManager.GetUsernamesAsync(playerId)).CharacterName;
                    char let = await KitManager.GetLoadoutCharacter(playerId);
                    string loadoutName = playerId.ToString() + "_" + let;
                    if (!ctx.TryGetRange(4, out string signText) || string.IsNullOrEmpty(signText))
                        signText = loadoutName;
                    await UCWarfare.ToUpdate();
                    if (!KitManager.KitExists(loadoutName, out _))
                    {
                        Kit loadout = new Kit(loadoutName, KitManager.ItemsFromInventory(ctx.Caller!), KitManager.ClothesFromInventory(ctx.Caller!));

                        loadout.IsLoadout = true;
                        loadout.Team = team;
                        loadout.Class = @class;
                        if (@class == EClass.PILOT)
                            loadout.Branch = EBranch.AIRFORCE;
                        else if (@class == EClass.CREWMAN)
                            loadout.Branch = EBranch.ARMOR;
                        else
                            loadout.Branch = EBranch.INFANTRY;

                        if (@class == EClass.HAT)
                            loadout.TeamLimit = 0.1F;

                        KitManager.UpdateText(loadout, signText);

                        loadout = await KitManager.AddKit(loadout);
                        await KitManager.GiveAccess(loadout, playerId, EKitAccessType.PURCHASE);
                        ctx.LogAction(EActionLogType.CREATE_KIT, loadoutName);
                        await UCWarfare.ToUpdate();
                        KitManager.UpdateSigns(loadout);
                        ctx.Reply("kit_l_created", Localization.TranslateEnum(@class, ctx.Caller!), username, playerId.ToString(), loadoutName);
                    }
                    else
                    {
                        ctx.Reply("kit_l_e_kitexists", loadoutName);
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
