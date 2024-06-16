using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Layouts;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Kits;
public class KitRequests(KitManager manager)
{
    public KitManager Manager { get; } = manager;

    /// <exception cref="CommandInteraction"/>
    public async Task RequestLoadout(int loadoutId, CommandInteraction ctx, CancellationToken token = default)
    {
        Kit? loadout = await Manager.Loadouts.GetLoadout(ctx.Caller, loadoutId, token).ConfigureAwait(false);

        if (loadout != null)
            loadout = await Manager.GetKit(loadout.PrimaryKey, token, x => KitManager.RequestableSet(x, true)).ConfigureAwait(false);

        await RequestLoadoutIntl(loadout, ctx, token).ConfigureAwait(false);
    }
    private async Task RequestLoadoutIntl(Kit? loadout, CommandInteraction ctx, CancellationToken token = default)
    {
        await UCWarfare.ToUpdate(token);
        if (loadout == null)
            throw ctx.Reply(T.RequestLoadoutNotOwned);
        if (loadout.NeedsUpgrade)
            throw ctx.Reply(T.RequestKitNeedsUpgrade);
        if (loadout.NeedsSetup)
            throw ctx.Reply(T.RequestKitNeedsSetup);
        if (loadout.Disabled || loadout.Season != UCWarfare.Season && loadout.Season > 0)
            throw ctx.Reply(T.RequestKitDisabled);
        if (!loadout.IsCurrentMapAllowed())
            throw ctx.Reply(T.RequestKitMapBlacklisted);

        ulong team = ctx.Caller.GetTeam();

        if (!loadout.IsFactionAllowed(TeamManager.GetFactionSafe(team)))
            throw ctx.Reply(T.RequestKitFactionBlacklisted);
        if (loadout.IsClassLimited(out _, out int allowedPlayers, team))
        {
            ctx.Reply(T.RequestKitLimited, allowedPlayers);
            return;
        }
        ctx.LogAction(ActionLogType.RequestKit, $"Loadout {KitEx.GetLoadoutLetter(KitEx.ParseStandardLoadoutId(loadout.InternalName))}: {loadout.InternalName}, Team {team}, Class: {Localization.TranslateEnum(loadout.Class)}");

        if (!await GrantKitRequest(ctx, loadout, token).ConfigureAwait(false))
        {
            await UCWarfare.ToUpdate(token);
            throw ctx.SendUnknownError();
        }

        if (loadout.Class == Class.Squadleader)
        {
            if (SquadManager.MaxSquadsReached(team))
                ctx.Reply(T.SquadsTooMany, SquadManager.ListUI.Squads.Length);
            else if (SquadManager.AreSquadLimited(team, out int requiredTeammatesForMoreSquads))
                ctx.Reply(T.SquadsTooManyPlayerCount, requiredTeammatesForMoreSquads);
            else
                Manager.TryCreateSquadOnRequestSquadleaderKit(ctx);
        }
    }

    /// <exception cref="CommandInteraction"/>
    public async Task RequestKit(Kit kit, CommandInteraction ctx, CancellationToken token = default)
    {
        if (kit.Type == KitType.Loadout)
        {
            await RequestLoadoutIntl(kit, ctx, token).ConfigureAwait(false);
            return;
        }

        await UCWarfare.ToUpdate(token);
        ulong team = ctx.Caller.GetTeam();

        // already requested
        uint? activeKit = ctx.Caller.ActiveKit;
        if (activeKit.HasValue && activeKit.Value == kit.PrimaryKey)
            throw ctx.Reply(T.RequestKitAlreadyOwned);

        // outdated kit
        if (kit.Disabled || kit.Season != UCWarfare.Season && kit.Season > 0)
            throw ctx.Reply(T.RequestKitDisabled);

        // map filter
        if (!kit.IsCurrentMapAllowed())
            throw ctx.Reply(T.RequestKitMapBlacklisted);

        // faction filter
        if (!kit.IsFactionAllowed(TeamManager.GetFactionSafe(team)))
            throw ctx.Reply(T.RequestKitFactionBlacklisted);

        // check credits bought
        if (kit is { IsPublicKit: true, CreditCost: > 0 } && !Manager.HasAccessQuick(kit, ctx.Caller) && !UCWarfare.Config.OverrideKitRequirements)
        {
            if (ctx.Caller.CachedCredits >= kit.CreditCost)
                throw ctx.Reply(T.RequestKitNotBought, kit.CreditCost);
            
            throw ctx.Reply(T.RequestKitCantAfford, ctx.Caller.CachedCredits, kit.CreditCost);
        }
        
        // elite access
        if (kit is { IsPublicKit: false, RequiresNitro: false } && !Manager.HasAccessQuick(kit, ctx.Caller) && !UCWarfare.Config.OverrideKitRequirements)
            throw ctx.Reply(T.RequestKitMissingAccess);

        // team limits
        if (kit.IsLimited(out _, out int allowedPlayers, team) || kit.Type == KitType.Loadout && kit.IsClassLimited(out _, out allowedPlayers, team))
            throw ctx.Reply(T.RequestKitLimited, allowedPlayers);

        // squad leader limit
        if (kit.Class == Class.Squadleader && ctx.Caller.Squad is not null && !ctx.Caller.IsSquadLeader())
            throw ctx.Reply(T.RequestKitNotSquadleader);

        // cooldowns
        if (
            Data.Gamemode.State == State.Active &&
            CooldownManager.HasCooldown(ctx.Caller, CooldownType.RequestKit, out Cooldown requestCooldown) &&
            !ctx.Caller.OnDutyOrAdmin() &&
            !UCWarfare.Config.OverrideKitRequirements &&
            kit.Class is not Class.Crewman and not Class.Pilot)
            throw ctx.Reply(T.KitOnGlobalCooldown, requestCooldown);

        if (kit is { IsPaid: true, RequestCooldown: > 0f } &&
            CooldownManager.HasCooldown(ctx.Caller, CooldownType.PremiumKit, out Cooldown premiumCooldown, kit.InternalName) &&
            !ctx.Caller.OnDutyOrAdmin() &&
            !UCWarfare.Config.OverrideKitRequirements)
            throw ctx.Reply(T.KitOnCooldown, premiumCooldown);

        // unlock requirements
        if (kit.UnlockRequirements != null)
        {
            for (int i = 0; i < kit.UnlockRequirements.Length; i++)
            {
                UnlockRequirement req = kit.UnlockRequirements[i];
                if (req == null || req.CanAccess(ctx.Caller))
                    continue;
                throw req.RequestKitFailureToMeet(ctx, kit);
            }
        }

        bool hasAccess = kit is { CreditCost: 0, IsPublicKit: true } || UCWarfare.Config.OverrideKitRequirements;
        if (!hasAccess)
        {
            // double check access against database
            hasAccess = await Manager.HasAccess(kit, ctx.Caller.Steam64, token).ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
            if (!hasAccess)
            {
                // check nitro boost status
                if (kit.RequiresNitro)
                {
                    try
                    {
                        bool nitroBoosting = await Manager.Boosting.IsNitroBoosting(ctx.CallerID, token).ConfigureAwait(false) ?? ctx.Caller.Save.WasNitroBoosting;
                        await UCWarfare.ToUpdate(token);
                        if (!nitroBoosting)
                            throw ctx.Reply(T.RequestKitMissingNitro);
                    }
                    catch (TimeoutException)
                    {
                        throw ctx.Reply(T.UnknownError);
                    }
                }
                else if (kit.IsPaid)
                    throw ctx.Reply(T.RequestKitMissingAccess);
                else if (ctx.Caller.CachedCredits >= kit.CreditCost)
                    throw ctx.Reply(T.RequestKitNotBought, kit.CreditCost);
                else
                    throw ctx.Reply(T.RequestKitCantAfford, ctx.Caller.CachedCredits, kit.CreditCost);
            }
        }
        // recheck limits to make sure people can't request at the same time to avoid limits.
        if (kit.IsLimited(out _, out allowedPlayers, team) || kit.Type == KitType.Loadout && kit.IsClassLimited(out _, out allowedPlayers, team))
            throw ctx.Reply(T.RequestKitLimited, allowedPlayers);

        ctx.LogAction(ActionLogType.RequestKit, $"Kit {kit.InternalName}, Team {team}, Class: {Localization.TranslateEnum(kit.Class)}");

        if (!await GrantKitRequest(ctx, kit, token).ConfigureAwait(false))
        {
            await UCWarfare.ToUpdate(token);
            throw ctx.SendUnknownError();
        }

        if (kit.Class == Class.Squadleader)
        {
            if (SquadManager.MaxSquadsReached(team))
                ctx.Reply(T.SquadsTooMany, SquadManager.ListUI.Squads.Length);
            else if (SquadManager.AreSquadLimited(team, out int requiredTeammatesForMoreSquads))
                ctx.Reply(T.SquadsTooManyPlayerCount, requiredTeammatesForMoreSquads);
            else
            {
                await UCWarfare.ToUpdate(token);
                if (Manager.IsLoaded)
                    Manager.TryCreateSquadOnRequestSquadleaderKit(ctx);
            }
        }
    }
    internal async Task GiveKit(UCPlayer player, Kit? kit, bool manual, bool tip, CancellationToken token = default, bool psLock = true)
    {
        if (!player.IsOnline)
            return;
        if (player == null) throw new ArgumentNullException(nameof(player));
        if (kit == null)
        {
            await RemoveKit(player, manual, token, psLock).ConfigureAwait(false);
            return;
        }

        Kit? oldKit;
        if (psLock)
            await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (!player.IsOnline)
                return;
            oldKit = await player.GetActiveKit(token).ConfigureAwait(false);
            if (!player.IsOnline)
                return;
            _ = kit.Items; // run off main thread preferrably, not that it's usually that expensive
            await UCWarfare.ToUpdate(token);
            if (!player.IsOnline)
                return;
            GrantKit(player, kit, tip);
            Manager.Signs.UpdateSigns(kit);
            if (oldKit != null)
                Manager.Signs.UpdateSigns(oldKit);
        }
        finally
        {
            if (psLock)
                player.PurchaseSync.Release();
        }

        KitManager.InvokeOnKitChanged(player, kit, oldKit);
        if (manual)
            KitManager.InvokeOnManualKitChanged(player, kit, oldKit);
    }
    private async Task<bool> GrantKitRequest(CommandInteraction ctx, Kit kit, CancellationToken token = default)
    {
        await UCWarfare.ToUpdate(token);
        AmmoCommand.WipeDroppedItems(ctx.CallerID);
        await GiveKit(ctx.Caller, kit, true, true, token).ConfigureAwait(false);
        // string id = kit.InternalName;
        // StatsManager.ModifyKit(id, k => k.TimesRequested++);
        // StatsManager.ModifyStats(ctx.CallerID, s =>
        // {
        //     WarfareStats.KitData kitData = s.Kits.Find(k => k.KitID == id && k.Team == team);
        //     if (kitData == default)
        //     {
        //         kitData = new WarfareStats.KitData { KitID = id, Team = (byte)team, TimesRequested = 1 };
        //         s.Kits.Add(kitData);
        //     }
        //     else
        //         kitData.TimesRequested++;
        // }, false);

        ctx.Reply(T.RequestSignGiven, kit.Class);

        if (kit is { IsPaid: true, RequestCooldown: > 0 })
            CooldownManager.StartCooldown(ctx.Caller, CooldownType.PremiumKit, kit.RequestCooldown, kit.InternalName);
        CooldownManager.StartCooldown(ctx.Caller, CooldownType.RequestKit, CooldownManager.Config.RequestKitCooldown);

        return true;
    }
    private void GrantKit(UCPlayer player, Kit? kit, bool tip = true)
    {
        ThreadUtil.assertIsGameThread();
        if (!player.IsOnline)
            return;
        if (UCWarfare.Config.ModifySkillLevels)
            player.EnsureSkillsets(kit?.Skillsets.Select(x => x.Skillset) ?? Array.Empty<Skillset>());
        player.ChangeKit(kit);
        player.Apply();
        if (kit == null)
        {
            UCInventoryManager.ClearInventoryAndSlots(player, true);
            return;
        }

        Manager.Distribution.DistributeKitItems(player, kit, true, tip, false);

        // bind hotkeys
        if (player.HotkeyBindings == null)
            return;

        foreach (HotkeyBinding binding in player.HotkeyBindings)
        {
            if (binding.Kit != kit.PrimaryKey)
                continue;

            byte index = KitEx.GetHotkeyIndex(binding.Slot);
            if (index == byte.MaxValue)
                continue;
            
            byte x = binding.Item.X, y = binding.Item.Y;
            Page page = binding.Item.Page;
            foreach (ItemTransformation transformation in player.ItemTransformations)
            {
                if (transformation.OldPage != page || transformation.OldX != x || transformation.OldY != y)
                    continue;

                x = transformation.NewX;
                y = transformation.NewY;
                page = transformation.NewPage;
                break;
            }

            ItemAsset? asset = binding.GetAsset(kit, player.GetTeam());
            if (asset != null && KitEx.CanBindHotkeyTo(asset, page))
                player.Player.equipment.ServerBindItemHotkey(index, asset, (byte)page, x, y);
        }
    }
    internal async Task RemoveKit(UCPlayer player, bool manual, CancellationToken token = default, bool psLock = true)
    {
        if (psLock)
            await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        Kit? oldKit;
        try
        {
            oldKit = await player.GetActiveKit(token).ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
            GrantKit(player, null, false);
            if (oldKit != null)
                Manager.Signs.UpdateSigns(oldKit);
        }
        finally
        {
            if (psLock)
                player.PurchaseSync.Release();
        }

        KitManager.InvokeOnKitChanged(player, null, oldKit);
        if (manual)
            KitManager.InvokeOnManualKitChanged(player, null, oldKit);
    }
    /// <exception cref="BaseCommandInteraction"/>
    public async Task BuyKit(CommandInteraction ctx, Kit kit, Vector3? effectPos = null, CancellationToken token = default)
    {
        if (!ctx.Caller.HasDownloadedKitData)
            await Manager.DownloadPlayerKitData(ctx.Caller, false, token).ConfigureAwait(false);

        ulong team = ctx.Caller.GetTeam();
        if (!kit.IsPublicKit)
        {
            if (UCWarfare.Config.WebsiteUri != null && kit.EliteKitInfo != null)
            {
                ctx.Caller.Player.sendBrowserRequest("Purchase " + kit.GetDisplayName(ctx.LanguageInfo) + " on our website.",
                    new Uri(UCWarfare.Config.WebsiteUri, "checkout/addtocart?productkeys=" + Uri.EscapeDataString(kit.InternalName)).OriginalString);

                throw ctx.Defer();
            }

            throw ctx.Reply(T.RequestNotBuyable);
        }
        if (kit.CreditCost == 0 || Manager.HasAccessQuick(kit, ctx.Caller))
            throw ctx.Reply(T.RequestKitAlreadyOwned);
        if (ctx.Caller.CachedCredits < kit.CreditCost)
            throw ctx.Reply(T.RequestKitCantAfford, ctx.Caller.CachedCredits, kit.CreditCost);

        await ctx.Caller.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await Points.UpdatePointsAsync(ctx.Caller, false, token).ConfigureAwait(false);
            if (ctx.Caller.CachedCredits < kit.CreditCost)
            {
                await UCWarfare.ToUpdate();
                throw ctx.Reply(T.RequestKitCantAfford, ctx.Caller.CachedCredits, kit.CreditCost);
            }

            CreditsParameters parameters = new CreditsParameters(ctx.Caller, team, -kit.CreditCost)
            {
                IsPurchase = true,
                IsPunishment = false
            };
            await Points.AwardCreditsAsync(parameters, token, false).ConfigureAwait(false);
        }
        finally
        {
            ctx.Caller.PurchaseSync.Release();
        }

        if (!await Manager.AddAccessRow(kit.PrimaryKey, ctx.CallerID, KitAccessType.Credits, token).ConfigureAwait(false))
            L.LogWarning($"Already had access to bought kit: {ctx.CallerID}, {kit.PrimaryKey}, {kit.InternalName}.");
        
        await UCWarfare.ToUpdate(token);

        (ctx.Caller.AccessibleKits ??= new List<uint>(4)).Add(kit.PrimaryKey);
        KitManager.InvokeOnKitAccessChanged(kit, ctx.CallerID, true, KitAccessType.Credits);

        ctx.LogAction(ActionLogType.BuyKit, "BOUGHT KIT " + kit.InternalName + " FOR " + kit.CreditCost + " CREDITS");
        L.Log(ctx.Caller.Name.PlayerName + " (" + ctx.Caller.Steam64 + ") bought " + kit.InternalName);

        Manager.Signs.UpdateSigns(kit, ctx.Caller);
        if (Gamemode.Config.EffectPurchase.ValidReference(out EffectAsset effect))
        {
            F.TriggerEffectReliable(effect, EffectManager.SMALL, effectPos ?? (ctx.Caller.Player.look.aim.position + ctx.Caller.Player.look.aim.forward * 0.25f));
        }

        ctx.Reply(T.RequestKitBought, kit.CreditCost);
    }

    public async Task ResupplyKit(UCPlayer player, bool ignoreAmmoBags = false, CancellationToken token = default)
    {
        if (!player.HasKit)
            return;

        Kit? kit = await player.GetActiveKit(token).ConfigureAwait(false);

        if (kit != null)
            await ResupplyKit(player, kit, ignoreAmmoBags, token).ConfigureAwait(false);
    }
    public async Task ResupplyKit(UCPlayer player, Kit kit, bool ignoreAmmoBags = false, CancellationToken token = default)
    {
        if (kit is null) throw new ArgumentNullException(nameof(kit));
        await UCWarfare.ToUpdate(token);
        List<KeyValuePair<ItemJar, Page>> nonKitItems = new List<KeyValuePair<ItemJar, Page>>(16);
        for (byte page = 0; page < PlayerInventory.PAGES - 2; ++page)
        {
            byte count = player.Player.inventory.getItemCount(page);
            for (byte i = 0; i < count; ++i)
            {
                ItemJar jar = player.Player.inventory.items[page].getItem(i);
                ItemAsset? asset = jar.item.GetAsset();
                if (asset is null) continue;
                if (kit != null && kit.ContainsItem(asset.GUID, player.GetTeam()))
                    continue;

                WhitelistItem? item = null;
                if (Whitelister.Loaded && !Whitelister.IsWhitelisted(asset.GUID, out item))
                    continue;

                if (item is { Amount: < byte.MaxValue } && item.Amount != 0)
                {
                    int amt = 0;
                    for (int w = 0; w < nonKitItems.Count; ++w)
                    {
                        if (nonKitItems[w].Key.GetAsset() is not { } ia2 || ia2.GUID != item.Item)
                            continue;

                        ++amt;
                        if (amt >= item.Amount)
                            goto s;
                    }
                }
                nonKitItems.Add(new KeyValuePair<ItemJar, Page>(jar, (Page)page));
            s:;
            }
        }

        Manager.Distribution.DistributeKitItems(player, kit, true, ignoreAmmoBags);
        bool playEffectEquip = true;
        bool playEffectDrop = true;
        foreach (KeyValuePair<ItemJar, Page> jar in nonKitItems)
        {
            if (player.Player.inventory.tryAddItem(jar.Key.item, jar.Key.x, jar.Key.y, (byte)jar.Value, jar.Key.rot))
                continue;

            if (!player.Player.inventory.tryAddItem(jar.Key.item, false, playEffectEquip))
            {
                ItemManager.dropItem(jar.Key.item, player.Position, playEffectDrop, true, true);
                playEffectDrop = false;
            }
            else playEffectEquip = false;
        }
    }
}
