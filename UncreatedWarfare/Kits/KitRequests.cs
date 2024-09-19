using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Kits.Translations;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.ItemTracking;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Skillsets;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Kits;
public class KitRequests
{
    private readonly DroppedItemTracker _droppedItemTracker;
    private readonly RequestTranslations _translations;
    private readonly ITranslationValueFormatter _valueFormatter;
    private readonly LanguageService _languageService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IPlayerService _playerService;
    private readonly CooldownManager _cooldownManager;
    public KitManager Manager { get; }
    public KitRequests(KitManager manager, IServiceProvider serviceProvider)
    {
        _droppedItemTracker = serviceProvider.GetRequiredService<DroppedItemTracker>();
        _valueFormatter = serviceProvider.GetRequiredService<ITranslationValueFormatter>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<RequestTranslations>>().Value;
        _languageService = serviceProvider.GetRequiredService<LanguageService>();
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
        _cooldownManager = serviceProvider.GetRequiredService<CooldownManager>();
        _serviceProvider = serviceProvider;
        Manager = manager;
    }

    /// <exception cref="CommandContext"/>
    public async Task RequestLoadout(int loadoutId, CommandContext ctx, CancellationToken token = default)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        await using IKitsDbContext dbContext = scope.ServiceProvider.GetRequiredService<IKitsDbContext>();

        Kit? loadout = await Manager.Loadouts.GetLoadout(dbContext, ctx.CallerId, loadoutId, token).ConfigureAwait(false);

        if (loadout != null)
            loadout = await Manager.GetKit(dbContext, loadout.PrimaryKey, token, x => KitManager.RequestableSet(x, true)).ConfigureAwait(false);

        await RequestLoadoutIntl(loadout, ctx, token).ConfigureAwait(false);
    }

    private async Task RequestLoadoutIntl(Kit? loadout, CommandContext ctx, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);
        if (loadout == null)
            throw ctx.Reply(_translations.RequestLoadoutNotOwned);
        if (loadout.NeedsUpgrade)
            throw ctx.Reply(_translations.RequestKitNeedsUpgrade);
        if (loadout.NeedsSetup)
            throw ctx.Reply(_translations.RequestKitNeedsSetup);
        if (loadout.Disabled || loadout.Season != WarfareModule.Season && loadout.Season > 0)
            throw ctx.Reply(_translations.RequestKitDisabled);
        if (!loadout.IsCurrentMapAllowed())
            throw ctx.Reply(_translations.RequestKitMapBlacklisted);

        Team team = ctx.Player.Team;

        if (!loadout.IsFactionAllowed(team.Faction))
        {
            throw ctx.Reply(_translations.RequestKitFactionBlacklisted);
        }

        if (loadout.IsClassLimited(_playerService, out _, out int allowedPlayers, team))
        {
            ctx.Reply(_translations.RequestKitLimited, allowedPlayers);
            return;
        }

        ctx.LogAction(ActionLogType.RequestKit, $"Loadout {LoadoutIdHelper.GetLoadoutLetter(LoadoutIdHelper.Parse(loadout.InternalName))}: {loadout.InternalName}, Team {team}, Class: {_valueFormatter.FormatEnum(loadout.Class, ctx.Language)}");

        if (!await GrantKitRequest(ctx, loadout, token).ConfigureAwait(false))
        {
            await UniTask.SwitchToMainThread(token);
            throw ctx.SendUnknownError();
        }

        // todo
        //if (loadout.Class == Class.Squadleader)
        //{
        //    if (SquadManager.MaxSquadsReached(team))
        //        ctx.Reply(T.SquadsTooMany, SquadManager.ListUI.Squads.Length);
        //    else if (SquadManager.AreSquadLimited(team, out int requiredTeammatesForMoreSquads))
        //        ctx.Reply(T.SquadsTooManyPlayerCount, requiredTeammatesForMoreSquads);
        //    else
        //        Manager.TryCreateSquadOnRequestSquadleaderKit(ctx);
        //}
    }

    /// <exception cref="CommandContext"/>
    public async Task RequestKit(Kit kit, CommandContext ctx, CancellationToken token = default)
    {
        if (kit.Type == KitType.Loadout)
        {
            await RequestLoadoutIntl(kit, ctx, token).ConfigureAwait(false);
            return;
        }

        await UniTask.SwitchToMainThread(token);

        // already requested
        uint? activeKit = ctx.Player.Component<KitPlayerComponent>().ActiveKitKey;
        if (activeKit.HasValue && activeKit.Value == kit.PrimaryKey)
            throw ctx.Reply(_translations.RequestKitAlreadyOwned);

        // outdated kit
        if (kit.Disabled || kit.Season != WarfareModule.Season && kit.Season > 0)
            throw ctx.Reply(_translations.RequestKitDisabled);

        // map filter
        if (!kit.IsCurrentMapAllowed())
            throw ctx.Reply(_translations.RequestKitMapBlacklisted);

        // faction filter
        if (!kit.IsFactionAllowed(ctx.Player.Team.Faction.NullIfDefault()))
            throw ctx.Reply(_translations.RequestKitFactionBlacklisted);

        // // check credits bought
        // if (kit is { IsPublicKit: true, CreditCost: > 0 } && !Manager.HasAccessQuick(kit, ctx.Player) && !UCWarfare.Config.OverrideKitRequirements)
        // {
        //     if (ctx.Caller.CachedCredits >= kit.CreditCost)
        //         throw ctx.Reply(_translations.RequestKitNotBought, kit.CreditCost);
        //     
        //     throw ctx.Reply(_translations.RequestKitCantAfford, ctx.Caller.CachedCredits, kit.CreditCost);
        // }
        // 
        // // elite access
        // if (kit is { IsPublicKit: false, RequiresNitro: false } && !Manager.HasAccessQuick(kit, ctx.Caller) && !UCWarfare.Config.OverrideKitRequirements)
        //     throw ctx.Reply(_translations.RequestKitMissingAccess);
        // 
        // // team limits
        // if (kit.IsLimited(out _, out int allowedPlayers, team) || kit.Type == KitType.Loadout && kit.IsClassLimited(out _, out allowedPlayers, team))
        //     throw ctx.Reply(_translations.RequestKitLimited, allowedPlayers);
        // 
        // // squad leader limit
        // if (kit.Class == Class.Squadleader && ctx.Caller.Squad is not null && !ctx.Caller.IsSquadLeader())
        //     throw ctx.Reply(_translations.RequestKitNotSquadleader);
        // 
        // // cooldowns
        // if (
        //     _cooldownManager.HasCooldown(ctx.Player, CooldownType.RequestKit, out Cooldown requestCooldown) &&
        //     !ctx.Caller.OnDutyOrAdmin() &&
        //     !UCWarfare.Config.OverrideKitRequirements &&
        //     kit.Class is not Class.Crewman and not Class.Pilot)
        //     throw ctx.Reply(T.KitOnGlobalCooldown, requestCooldown);
        // 
        // if (kit is { IsPaid: true, RequestCooldown: > 0f } &&
        //     _cooldownManager.HasCooldown(ctx.Player, CooldownType.PremiumKit, out Cooldown premiumCooldown, kit.InternalName) &&
        //     !ctx.Caller.OnDutyOrAdmin() &&
        //     !UCWarfare.Config.OverrideKitRequirements)
        //     throw ctx.Reply(T.KitOnCooldown, premiumCooldown);
        // 
        // // unlock requirements
        // if (kit.UnlockRequirements != null)
        // {
        //     for (int i = 0; i < kit.UnlockRequirements.Length; i++)
        //     {
        //         UnlockRequirement req = kit.UnlockRequirements[i];
        //         if (req == null || req.CanAccess(ctx.Caller))
        //             continue;
        //         throw req.RequestKitFailureToMeet(ctx, kit);
        //     }
        // }
        // 
        // bool hasAccess = kit is { CreditCost: 0, IsPublicKit: true } || UCWarfare.Config.OverrideKitRequirements;
        // if (!hasAccess)
        // {
        //     // double check access against database
        //     hasAccess = await Manager.HasAccess(kit, ctx.CallerId, token).ConfigureAwait(false);
        //     await UniTask.SwitchToMainThread(token);
        //     if (!hasAccess)
        //     {
        //         // check nitro boost status
        //         if (kit.RequiresNitro)
        //         {
        //             try
        //             {
        //                 bool nitroBoosting = await Manager.Boosting.IsNitroBoosting(ctx.CallerId.m_SteamID, token).ConfigureAwait(false) ?? ctx.Caller.Save.WasNitroBoosting;
        //                 await UniTask.SwitchToMainThread(token);
        //                 if (!nitroBoosting)
        //                     throw ctx.Reply(_translations.RequestKitMissingNitro);
        //             }
        //             catch (TimeoutException)
        //             {
        //                 throw ctx.Reply(T.UnknownError);
        //             }
        //         }
        //         else if (kit.IsPaid)
        //             throw ctx.Reply(_translations.RequestKitMissingAccess);
        //         else if (ctx.Caller.CachedCredits >= kit.CreditCost)
        //             throw ctx.Reply(_translations.RequestKitNotBought, kit.CreditCost);
        //         else
        //             throw ctx.Reply(_translations.RequestKitCantAfford, ctx.Caller.CachedCredits, kit.CreditCost);
        //     }
        // }
        // // recheck limits to make sure people can't request at the same time to avoid limits.
        // if (kit.IsLimited(out _, out allowedPlayers, team) || kit.Type == KitType.Loadout && kit.IsClassLimited(out _, out allowedPlayers, team))
        //     throw ctx.Reply(_translations.RequestKitLimited, allowedPlayers);
        // 
        // ctx.LogAction(ActionLogType.RequestKit, $"Kit {kit.InternalName}, Team {team}, Class: {_valueFormatter.FormatEnum(kit.Class, ctx.Language)}");
        // 
        // if (!await GrantKitRequest(ctx, kit, token).ConfigureAwait(false))
        // {
        //     await UniTask.SwitchToMainThread(token);
        //     throw ctx.SendUnknownError();
        // }
        // 
        // if (kit.Class == Class.Squadleader)
        // {
        //     if (SquadManager.MaxSquadsReached(team))
        //         ctx.Reply(T.SquadsTooMany, SquadManager.ListUI.Squads.Length);
        //     else if (SquadManager.AreSquadLimited(team, out int requiredTeammatesForMoreSquads))
        //         ctx.Reply(T.SquadsTooManyPlayerCount, requiredTeammatesForMoreSquads);
        //     else
        //     {
        //         await UniTask.SwitchToMainThread(token);
        //         if (Manager.IsLoaded)
        //             Manager.TryCreateSquadOnRequestSquadleaderKit(ctx);
        //     }
        // }
    }
    internal async Task GiveKit(WarfarePlayer player, Kit? kit, bool manual, bool tip, CancellationToken token = default, bool psLock = true)
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
            oldKit = await player.Component<KitPlayerComponent>().GetActiveKitAsync(token).ConfigureAwait(false);
            if (!player.IsOnline)
                return;
            _ = kit.Items; // run off main thread preferrably, not that it's usually that expensive
            await UniTask.SwitchToMainThread(token);
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

        Manager.InvokeOnKitChanged(player, kit, oldKit);
        if (manual)
            Manager.InvokeOnManualKitChanged(player, kit, oldKit);
    }
    private async Task<bool> GrantKitRequest(CommandContext ctx, Kit kit, CancellationToken token = default)
    {
        await _droppedItemTracker.DestroyItemsDroppedByPlayerAsync(ctx.CallerId, false, token);
        await GiveKit(ctx.Player, kit, true, true, token).ConfigureAwait(false);
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

        ctx.Reply(_translations.RequestSignGiven, kit.Class);

        if (kit is { IsPaid: true, RequestCooldown: > 0 })
            _cooldownManager.StartCooldown(ctx.Player, CooldownType.PremiumKit, kit.RequestCooldown, kit.InternalName);
        _cooldownManager.StartCooldown(ctx.Player, CooldownType.RequestKit, _cooldownManager.Config.RequestKitCooldown);

        return true;
    }
    private void GrantKit(WarfarePlayer player, Kit? kit, bool tip = true)
    {
        GameThread.AssertCurrent();

        if (!player.IsOnline)
            return;

        player.Component<SkillsetPlayerComponent>().EnsureSkillsets(kit?.Skillsets.Select(x => x.Skillset) ?? Array.Empty<Skillset>());
        player.Component<KitPlayerComponent>().UpdateKit(kit);

        player.Save.KitId = kit?.PrimaryKey ?? 0;
        player.Save.Save();

        if (kit == null)
        {
            ItemUtility.ClearInventoryAndSlots(player, true);
            return;
        }

        Manager.Distribution.DistributeKitItems(player, kit, true, tip, false);

        // bind hotkeys
        ItemTrackingPlayerComponent itemTracking = player.Component<ItemTrackingPlayerComponent>();

        HotkeyPlayerComponent hotkeys = player.Component<HotkeyPlayerComponent>();
        if (hotkeys.HotkeyBindings == null)
            return;

        foreach (HotkeyBinding binding in hotkeys.HotkeyBindings)
        {
            if (binding.Kit != kit.PrimaryKey)
                continue;

            byte index = KitEx.GetHotkeyIndex(binding.Slot);
            if (index == byte.MaxValue)
                continue;
            
            byte x = binding.Item.X, y = binding.Item.Y;
            Page page = binding.Item.Page;
            foreach (ItemTransformation transformation in itemTracking.ItemTransformations)
            {
                if (transformation.OldPage != page || transformation.OldX != x || transformation.OldY != y)
                    continue;

                x = transformation.NewX;
                y = transformation.NewY;
                page = transformation.NewPage;
                break;
            }

            ItemAsset? asset = binding.GetAsset(kit, 0/* todo player.Team */);
            if (asset != null && KitEx.CanBindHotkeyTo(asset, page))
                player.UnturnedPlayer.equipment.ServerBindItemHotkey(index, asset, (byte)page, x, y);
        }
    }
    internal async Task RemoveKit(WarfarePlayer player, bool manual, CancellationToken token = default, bool psLock = true)
    {
        if (psLock)
            await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        Kit? oldKit;
        try
        {
            oldKit = await player.Component<KitPlayerComponent>().GetActiveKitAsync(token).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(token);
            GrantKit(player, null, false);
            if (oldKit != null)
                Manager.Signs.UpdateSigns(oldKit);
        }
        finally
        {
            if (psLock)
                player.PurchaseSync.Release();
        }

        Manager.InvokeOnKitChanged(player, null, oldKit);
        if (manual)
            Manager.InvokeOnManualKitChanged(player, null, oldKit);
    }
    /// <exception cref="BaseCommandContext"/>
    public async Task BuyKit(CommandContext ctx, Kit kit, Vector3? effectPos = null, CancellationToken token = default)
    {
        throw ctx.Reply(_translations.RequestKitCantAfford, 0, kit.CreditCost);
        //if (!ctx.Player.HasDownloadedKitData)
        //    await Manager.DownloadPlayerKitData(ctx.Player, false, token).ConfigureAwait(false);
        //
        //Team team = ctx.Player.Team;
        //if (!kit.IsPublicKit)
        //{
        //    if (UCWarfare.Config.WebsiteUri != null && kit.EliteKitInfo != null)
        //    {
        //        await UniTask.SwitchToMainThread(token);
        //        ctx.Player.UnturnedPlayer.sendBrowserRequest("Purchase " + kit.GetDisplayName(_languageService, ctx.Language) + " on our website.",
        //            new Uri(UCWarfare.Config.WebsiteUri, "checkout/addtocart?productkeys=" + Uri.EscapeDataString(kit.InternalName)).OriginalString);
        //
        //        throw ctx.Defer();
        //    }
        //
        //    throw ctx.Reply(_translations.RequestNotBuyable);
        //}
        //if (kit.CreditCost == 0 || Manager.HasAccessQuick(kit, ctx.Player))
        //    throw ctx.Reply(_translations.RequestKitAlreadyOwned);
        //if (ctx.Player.CachedCredits < kit.CreditCost)
        //    throw ctx.Reply(_translations.RequestKitCantAfford, ctx.Player.CachedCredits, kit.CreditCost);
        //
        //await ctx.Player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        //try
        //{
        //    await Points.UpdatePointsAsync(ctx.Player, false, token).ConfigureAwait(false);
        //    if (ctx.Player.CachedCredits < kit.CreditCost)
        //    {
        //        await UniTask.SwitchToMainThread(token);
        //        throw ctx.Reply(_translations.RequestKitCantAfford, ctx.Player.CachedCredits, kit.CreditCost);
        //    }
        //
        //    CreditsParameters parameters = new CreditsParameters(ctx.Player.Steam64, team, -kit.CreditCost)
        //    {
        //        IsPurchase = true,
        //        IsPunishment = false
        //    };
        //    await Points.AwardCreditsAsync(parameters, token, false).ConfigureAwait(false);
        //}
        //finally
        //{
        //    ctx.Player.PurchaseSync.Release();
        //}
        //
        //if (!await Manager.AddAccessRow(kit.PrimaryKey, ctx.CallerId, KitAccessType.Credits, token).ConfigureAwait(false))
        //    L.LogWarning($"Already had access to bought kit: {ctx.CallerId.m_SteamID}, {kit.PrimaryKey}, {kit.InternalName}.");
        //
        //await UniTask.SwitchToMainThread(token);
        //
        //(ctx.Player.Component<KitPlayerComponent>().AccessibleKits ??= new List<uint>(4)).Add(kit.PrimaryKey);
        //Manager.InvokeOnKitAccessChanged(kit, ctx.CallerId, true, KitAccessType.Credits);
        //
        //ctx.LogAction(ActionLogType.BuyKit, "BOUGHT KIT " + kit.InternalName + " FOR " + kit.CreditCost + " CREDITS");
        //L.Log(ctx.Player.Names.PlayerName + " (" + ctx.Player.Steam64 + ") bought " + kit.InternalName);
        //
        //Manager.Signs.UpdateSigns(kit, ctx.Player);
        //if (Gamemode.Config.EffectPurchase.TryGetAsset(out EffectAsset? effect))
        //{
        //    EffectUtility.TriggerEffect(effect, EffectManager.SMALL, effectPos ?? (ctx.Player.UnturnedPlayer.look.aim.position + ctx.Player.UnturnedPlayer.look.aim.forward * 0.25f), true);
        //}
        //
        //ctx.Reply(_translations.RequestKitBought, kit.CreditCost);
    }

    public async Task ResupplyKit(WarfarePlayer player, bool ignoreAmmoBags = false, CancellationToken token = default)
    {
        KitPlayerComponent comp = player.Component<KitPlayerComponent>();
        if (!comp.HasKit)
            return;

        Kit? kit = await comp.GetActiveKitAsync(token).ConfigureAwait(false);

        if (kit != null)
            await ResupplyKit(player, kit, ignoreAmmoBags, token).ConfigureAwait(false);
    }
    public async Task ResupplyKit(WarfarePlayer player, Kit kit, bool ignoreAmmoBags = false, CancellationToken token = default)
    {
        if (kit is null) throw new ArgumentNullException(nameof(kit));
        await UniTask.SwitchToMainThread(token);
        List<KeyValuePair<ItemJar, Page>> nonKitItems = new List<KeyValuePair<ItemJar, Page>>(16);
        for (byte page = 0; page < PlayerInventory.PAGES - 2; ++page)
        {
            byte count = player.UnturnedPlayer.inventory.getItemCount(page);
            for (byte i = 0; i < count; ++i)
            {
                ItemJar jar = player.UnturnedPlayer.inventory.items[page].getItem(i);
                ItemAsset? asset = jar.item.GetAsset();
                if (asset is null) continue;
                if (kit != null && kit.ContainsItem(asset.GUID, player.Team))
                    continue;

                ItemWhitelist? item = null;
                // todo if (Whitelister.Loaded && !Whitelister.IsWhitelisted(asset.GUID, out item))
                // todo     continue;

                if (item is { Amount: < byte.MaxValue } && item.Amount != 0)
                {
                    int amt = 0;
                    for (int w = 0; w < nonKitItems.Count; ++w)
                    {
                        if (nonKitItems[w].Key.GetAsset() is not { } ia2 || !item.Item.Equals(ia2))
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
            if (player.UnturnedPlayer.inventory.tryAddItem(jar.Key.item, jar.Key.x, jar.Key.y, (byte)jar.Value, jar.Key.rot))
                continue;

            if (!player.UnturnedPlayer.inventory.tryAddItem(jar.Key.item, false, playEffectEquip))
            {
                ItemManager.dropItem(jar.Key.item, player.Position, playEffectDrop, true, true);
                playEffectDrop = false;
            }
            else playEffectEquip = false;
        }
    }
}
