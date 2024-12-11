using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models.Kits;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Interaction.Requests;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Kits.Translations;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.ItemTracking;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Skillsets;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Kits;
public class KitRequests : IRequestHandler<KitSignInstanceProvider, Kit>, IRequestHandler<Kit, Kit>
{
    private readonly DroppedItemTracker _droppedItemTracker;
    private readonly RequestTranslations _reqTranslations;
    private readonly RequestKitsTranslations _kitReqTranslations;
    private readonly ITranslationValueFormatter _valueFormatter;
    private readonly LanguageService _languageService;
    private readonly EventDispatcher _eventDispatcher;
    private readonly SquadManager? _squadManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly IPlayerService _playerService;
    private readonly ILogger<KitRequests> _logger;
    private readonly CooldownManager _cooldownManager;
    private readonly AssetRedirectService _assetRedirectService;
    private readonly IFactionDataStore _factionDataStore;
    public KitManager Manager { get; }
    public KitRequests(KitManager manager, IServiceProvider serviceProvider)
    {
        _droppedItemTracker = serviceProvider.GetRequiredService<DroppedItemTracker>();
        _valueFormatter = serviceProvider.GetRequiredService<ITranslationValueFormatter>();
        _reqTranslations = serviceProvider.GetRequiredService<TranslationInjection<RequestTranslations>>().Value;
        _kitReqTranslations = serviceProvider.GetRequiredService<TranslationInjection<RequestKitsTranslations>>().Value;
        _languageService = serviceProvider.GetRequiredService<LanguageService>();
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
        _cooldownManager = serviceProvider.GetRequiredService<CooldownManager>();
        _logger = serviceProvider.GetRequiredService<ILogger<KitRequests>>();
        _eventDispatcher = serviceProvider.GetRequiredService<EventDispatcher>();
        _assetRedirectService = serviceProvider.GetRequiredService<AssetRedirectService>();
        _factionDataStore = serviceProvider.GetRequiredService<IFactionDataStore>();
        _squadManager = serviceProvider.GetService<SquadManager>();
        _serviceProvider = serviceProvider;
        Manager = manager;
    }

    /// <exception cref="CommandContext"/>
    public async Task<bool> RequestLoadout(WarfarePlayer player, int loadoutId, IRequestResultHandler resultHandler, CancellationToken token = default)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        await using IKitsDbContext dbContext = scope.ServiceProvider.GetRequiredService<IKitsDbContext>();

        Kit? loadout = await Manager.Loadouts.GetLoadout(dbContext, player.Steam64, loadoutId, token).ConfigureAwait(false);

        if (loadout != null)
            loadout = await Manager.GetKit(dbContext, loadout.PrimaryKey, token, x => KitManager.RequestableSet(x, true)).ConfigureAwait(false);

        return await RequestAsync(player, loadout, resultHandler, token).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> RequestAsync(WarfarePlayer player, KitSignInstanceProvider? sign, IRequestResultHandler resultHandler, CancellationToken token = default)
    {
        if (sign == null)
        {
            resultHandler.NotFoundOrRegistered(player);
            return false;
        }

        return sign.LoadoutNumber > 0
            ? await RequestLoadout(player, sign.LoadoutNumber, resultHandler, token)
            : await RequestAsync(player, await Manager.FindKit(sign.KitId, token, set: x => KitManager.RequestableSet(x, true)), resultHandler, token);
    }

    public async Task<bool> RequestAsync(WarfarePlayer player, [NotNullWhen(true)] Kit? kit, IRequestResultHandler resultHandler, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        Team team = player.Team;

        if (kit == null)
        {
            resultHandler.NotFoundOrRegistered(player);
            return false;
        }

        // already requested
        uint? activeKit = player.Component<KitPlayerComponent>().ActiveKitKey;
        if (activeKit.HasValue && activeKit.Value == kit.PrimaryKey)
        {
            resultHandler.MissingRequirement(player, kit, _kitReqTranslations.AlreadyEquipped.Translate(player));
            return false;
        }

        if (kit.NeedsUpgrade)
        {
            resultHandler.MissingRequirement(player, kit, _kitReqTranslations.NeedsUpgrade.Translate(player));
            return false;
        }

        if (kit.NeedsSetup)
        {
            resultHandler.MissingRequirement(player, kit, _kitReqTranslations.NeedsSetup.Translate(player));
            return false;
        }

        // outdated kit
        if (kit.Disabled || kit.Season != WarfareModule.Season && kit.Season > 0)
        {
            resultHandler.MissingRequirement(player, kit, _kitReqTranslations.KitDisabled.Translate(player));
            return false;
        }

        if (!kit.IsCurrentMapAllowed())
        {
            resultHandler.MissingRequirement(player, kit, _kitReqTranslations.KitMapNotAllowed.Translate(player));
            return false;
        }

        if (!kit.IsFactionAllowed(team.Faction))
        {
            resultHandler.MissingRequirement(player, kit, _kitReqTranslations.KitTeamNotAllowed.Translate(player));
            return false;
        }

        // check credits bought
        if (kit is { IsPublicKit: true, CreditCost: > 0 } && !Manager.HasAccessQuick(kit, player))
        {
            resultHandler.MissingCreditsOwnership(player, kit, kit.CreditCost);
            return false;
        }
        
        // elite access
        if (kit is { IsPublicKit: false, RequiresNitro: false } && !Manager.HasAccessQuick(kit, player))
        {
            if (kit.PremiumCost > 0)
                resultHandler.MissingDonorOwnership(player, kit, kit.PremiumCost);
            else
                resultHandler.MissingExclusiveOwnership(player, kit);
            return false;
        }
        
        // team limits
        if (kit.Type != KitType.Loadout
                ? kit.IsLimited(_playerService, out _, out int allowedPlayers, team)
                : kit.IsClassLimited(_playerService, out _, out allowedPlayers, team))
        {
            resultHandler.MissingRequirement(player, kit,
                kit.Type != KitType.Loadout
                    ? _kitReqTranslations.RequestKitLimited.Translate(allowedPlayers, player)
                    : _kitReqTranslations.RequestKitClassLimited.Translate(allowedPlayers, kit.Class, player)
            );
            return false;
        }
        
        // cooldowns
        if (_cooldownManager.HasCooldown(player, CooldownType.RequestKit, out Cooldown requestCooldown) && kit.Class is not Class.Crewman and not Class.Pilot)
        {
            resultHandler.MissingRequirement(player, kit, _kitReqTranslations.OnGlobalCooldown.Translate(requestCooldown, player));
            return false;
        }
        
        if (kit is { IsPaid: true, RequestCooldown: > 0f } && _cooldownManager.HasCooldown(player, CooldownType.PremiumKit, out Cooldown premiumCooldown, kit.InternalName))
        {
            resultHandler.MissingRequirement(player, kit, _kitReqTranslations.OnCooldown.Translate(premiumCooldown, player));
            return false;
        }
        
        // unlock requirements
        if (kit.UnlockRequirements != null)
        {
            for (int i = 0; i < kit.UnlockRequirements.Length; i++)
            {
                UnlockRequirement req = kit.UnlockRequirements[i];
                await UniTask.SwitchToMainThread(token);
                if (req == null || await req.CanAccessAsync(player, token))
                    continue;

                await UniTask.SwitchToMainThread(token);
                resultHandler.MissingUnlockRequirement(player, kit, req);
                return false;
            }

            await UniTask.SwitchToMainThread(token);
        }

        if (kit is not { CreditCost: 0, IsPublicKit: true })
        {
            // double check access against database
            bool hasAccess = await Manager.HasAccess(kit, player.Steam64, token).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(token);
            if (!hasAccess)
            {
                // check nitro boost status
                if (kit.RequiresNitro)
                {
                    bool nitroBoosting;
                    try
                    {
                        nitroBoosting = await Manager.Boosting.IsNitroBoosting(player.Steam64.m_SteamID, token).ConfigureAwait(false) ?? player.Save.WasNitroBoosting;
                        await UniTask.SwitchToMainThread(token);
                    }
                    catch
                    {
                        nitroBoosting = player.Save.WasNitroBoosting;
                    }

                    if (!nitroBoosting)
                    {
                        resultHandler.MissingRequirement(player, kit, _kitReqTranslations.RequiresNitroBoost.Translate(requestCooldown, player));
                        return false;
                    }
                }
                else if (kit.IsPaid)
                {
                    if (kit.PremiumCost > 0)
                        resultHandler.MissingDonorOwnership(player, kit, kit.PremiumCost);
                    else
                        resultHandler.MissingExclusiveOwnership(player, kit);
                    return false;
                }
                else
                {
                    resultHandler.MissingCreditsOwnership(player, kit, kit.CreditCost);
                    return false;
                }
            }
        }

        if (!player.IsOnline)
        {
            throw new OperationCanceledException("Player disconnected before kit could be granted.");
        }

        if (team != player.Team)
        {
            throw new OperationCanceledException("Change teams before kit could be granted.");
        }

        // recheck limits to make sure people can't request at the same time to avoid limits.
        if (kit.Type != KitType.Loadout
                ? kit.IsLimited(_playerService, out _, out allowedPlayers, team)
                : kit.IsClassLimited(_playerService, out _, out allowedPlayers, team))
        {
            resultHandler.MissingRequirement(player, kit,
                kit.Type != KitType.Loadout
                    ? _kitReqTranslations.RequestKitLimited.Translate(allowedPlayers, player)
                    : _kitReqTranslations.RequestKitClassLimited.Translate(allowedPlayers, kit.Class, player)
            );
            return false;
        }

        if (kit.Type == KitType.Loadout && LoadoutIdHelper.Parse(kit.InternalName, out CSteamID intendedPlayer) > 0 && intendedPlayer.m_SteamID == player.Steam64.m_SteamID)
        {
            ActionLog.Add(ActionLogType.RequestKit, $"Loadout {LoadoutIdHelper.GetLoadoutLetter(LoadoutIdHelper.Parse(kit.InternalName))}: {kit.InternalName}, Team {team}, Class: {_valueFormatter.FormatEnum(kit.Class, null)}", player.Steam64);
        }
        else
        {
            ActionLog.Add(ActionLogType.RequestKit, $"Kit {kit.InternalName}, Team {player.Team.Faction.Name}, Class: {_valueFormatter.FormatEnum(kit.Class, null)}", player.Steam64);
        }

        if (_squadManager != null && kit.Class == Class.Squadleader && !CheckOrCreateSquadForSquadleaderKit(player, kit, resultHandler))
        {
            return false;
        }

        await GrantKitRequest(player, kit, resultHandler, token).ConfigureAwait(false);
        return true;
    }

    internal async Task GiveKit(WarfarePlayer player, Kit? kit, bool manual, bool tip, CancellationToken token = default) // todo: remove manual and tip
    {
        if (!player.IsOnline)
            return;

        if (player == null)
        {
            throw new ArgumentNullException(nameof(player));
        }

        if (kit == null)
        {
            await RemoveKit(player, manual, token).ConfigureAwait(false);
            return;
        }

        await UniTask.SwitchToMainThread(token);

        if (!player.IsOnline)
            return;

        uint? oldKitId = player.Component<KitPlayerComponent>().ActiveKitKey;
        GrantKit(player, kit, tip);
        Manager.Signs.UpdateSigns(kit);

        Kit? oldKit = oldKitId.HasValue ? await Manager.GetKit(oldKitId.Value, CancellationToken.None).ConfigureAwait(false) : null;
        await UniTask.SwitchToMainThread(CancellationToken.None);

        if (oldKit != null && oldKitId!.Value != kit.PrimaryKey)
            Manager.Signs.UpdateSigns(oldKit);

        Manager.InvokeOnKitChanged(player, kit, oldKit);
        if (manual)
            Manager.InvokeOnManualKitChanged(player, kit, oldKit);
    }

    private async Task GrantKitRequest(WarfarePlayer player, Kit kit, IRequestResultHandler resultHandler, CancellationToken token = default)
    {
        await _droppedItemTracker.DestroyItemsDroppedByPlayerAsync(player.Steam64, false, token);
        await GiveKit(player, kit, true, true, token).ConfigureAwait(false);

        await UniTask.SwitchToMainThread(token);

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

        resultHandler.Success(player, kit);

        if (kit is { IsPaid: true, RequestCooldown: > 0 })
            _cooldownManager.StartCooldown(player, CooldownType.PremiumKit, kit.RequestCooldown, kit.InternalName);
        _cooldownManager.StartCooldown(player, CooldownType.RequestKit, _cooldownManager.Config.RequestKitCooldown);
    }

    private void GrantKit(WarfarePlayer player, Kit? kit, bool tip = true)
    {
        GameThread.AssertCurrent();

        if (!player.IsOnline)
            return;

        _logger.LogInformation("Granting kit {0}.", kit?.InternalName);
        player.Component<SkillsetPlayerComponent>().EnsureSkillsets(kit?.Skillsets.Select(x => x.Skillset) ?? Array.Empty<Skillset>());
        player.Component<KitPlayerComponent>().UpdateKit(kit);

        player.Save.KitId = kit?.PrimaryKey ?? 0;
        player.Save.Save();

        if (kit == null)
        {
            ItemUtility.ClearInventoryAndSlots(player, true);
            _ = _eventDispatcher.DispatchEventAsync(new PlayerKitChanged { Player = player, Class = Class.None, Kit = null, KitId = 0, KitName = null });
            return;
        }

        PlayerKitChanged args = new PlayerKitChanged
        {
            KitId = kit.PrimaryKey,
            Kit = kit,
            KitName = kit.InternalName,
            Class = kit.Class,
            Player = player
        };

        Manager.Distribution.DistributeKitItems(player, kit, _logger, true, tip, false);

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

            ItemAsset? asset = binding.GetAsset(kit, player.Team, _assetRedirectService, _factionDataStore);
            if (asset != null && KitEx.CanBindHotkeyTo(asset, page))
                player.UnturnedPlayer.equipment.ServerBindItemHotkey(index, asset, (byte)page, x, y);
        }

        _ = _eventDispatcher.DispatchEventAsync(args);
    }

    internal async Task RemoveKit(WarfarePlayer player, bool manual, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        uint? oldKitId = player.Component<KitPlayerComponent>().ActiveKitKey;
        GrantKit(player, null, false);

        Kit? oldKit = oldKitId.HasValue ? await Manager.GetKit(oldKitId.Value, CancellationToken.None).ConfigureAwait(false) : null;

        await UniTask.SwitchToMainThread(CancellationToken.None);
        if (oldKit != null)
            Manager.Signs.UpdateSigns(oldKit);

        Manager.InvokeOnKitChanged(player, null, oldKit);
        if (manual)
            Manager.InvokeOnManualKitChanged(player, null, oldKit);
    }

    private bool CheckOrCreateSquadForSquadleaderKit(WarfarePlayer player, Kit kit, IRequestResultHandler resultHandler)
    {
        if (player.IsSquadLeader())
            return true;

        if (player.IsNonLeaderSquadMember())
        {
            resultHandler.MissingRequirement(player, kit, _kitReqTranslations.RequestKitNotSquadleader.Translate(player));
            return false;
        }

        if (_squadManager!.AreSquadLimited(player.Team, out _))
        {
            resultHandler.MissingRequirement(player, kit, _kitReqTranslations.RequestKitNotSquadleader.Translate(player));
            return false;
        }

        _squadManager.CreateSquad(player, _squadManager.GetUniqueSquadName(player.Team));
        return true;
    }

    /// <exception cref="BaseCommandContext"/>
    public async Task BuyKit(CommandContext ctx, Kit kit, Vector3? effectPos = null, CancellationToken token = default)
    {
        throw ctx.Reply(_reqTranslations.RequestNotOwnedCreditsCantAfford, 0, kit.CreditCost);
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
                if (kit != null && kit.ContainsItem(asset.GUID, player.Team, _assetRedirectService, _factionDataStore))
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

        Manager.Distribution.DistributeKitItems(player, kit, _logger, true, ignoreAmmoBags);
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
