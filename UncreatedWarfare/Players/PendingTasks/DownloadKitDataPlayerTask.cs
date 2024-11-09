using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players.ItemTracking;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Players.PendingTasks;

[PlayerTask]
internal class DownloadKitDataPlayerTask : IPlayerPendingTask
{
    private readonly IKitsDbContext _dbContext;
    private readonly ILogger<DownloadKitDataPlayerTask> _logger;

    private List<uint>? _access;
    private List<HotkeyBinding>? _hotkeys;
    private List<ItemLayoutTransformationData>? _transformationData;

    public DownloadKitDataPlayerTask(IKitsDbContext dbContext, ILogger<DownloadKitDataPlayerTask> logger)
    {
        dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> RunAsync(PlayerPending e, CancellationToken token)
    {
        bool anyChanges = await DownloadHotkeys(e, token).ConfigureAwait(false);
        await DownloadLayouts(e, token, anyChanges).ConfigureAwait(false);
        await DownloadAccess(e, token).ConfigureAwait(false);

        return true;
    }

    private async Task<bool> DownloadHotkeys(PlayerPending e, CancellationToken token)
    {
        ulong s64 = e.Steam64.m_SteamID;

        List<KitHotkey> kitHotkeys = await _dbContext.KitHotkeys
            .Include(x => x.Kit)
            .ThenInclude(x => x.ItemModels)
            .Where(x => x.Steam64 == s64)
            .ToListAsync(token)
            .ConfigureAwait(false);

        List<HotkeyBinding> bindings = new List<HotkeyBinding>();

        bool anyChanges = false;
        foreach (KitHotkey hotkey in kitHotkeys)
        {
            if (!KitEx.ValidSlot(hotkey.Slot))
            {
                _logger.LogWarning("Invalid hotkey slot {0} in hotkey for kit {1} for player {2}.", hotkey.Slot, hotkey.KitId, e.Steam64);
                _dbContext.Remove(hotkey);
                anyChanges = true;
                continue;
            }

            if (hotkey.Kit == null)
            {
                _logger.LogWarning("Kit {0} for player {1}'s {2} hotkey not found.", hotkey.KitId, e.Steam64, hotkey.Slot);
                _dbContext.Remove(hotkey);
                anyChanges = true;
                continue;
            }

            Kit kit = hotkey.Kit;

            KitItemModel? item = kit.ItemModels
                .FirstOrDefault(x => x.Page.HasValue && x.Page.Value == hotkey.Page && x.X.HasValue && x.X.Value == hotkey.X && x.Y.HasValue && x.Y.Value == hotkey.Y);

            if (item != null)
            {
                if (hotkey.Redirect.HasValue)
                {
                    if (!item.Redirect.HasValue || item.Redirect.Value != hotkey.Redirect.Value)
                        item = null;
                }
                else if (hotkey.Item.HasValue)
                {
                    if (!item.Item.HasValue || !item.Item.Value.Equals(hotkey.Item.Value))
                        item = null;
                }
                else item = null;
            }

            IPageKitItem? itemValue = item?.CreateRuntimeItem() as IPageKitItem;

            if (itemValue == null)
            {
                if (hotkey.Item.HasValue)
                {
                    itemValue = new SpecificPageKitItem(0u, hotkey.Item.Value, hotkey.X, hotkey.Y, 0, hotkey.Page, 1, Array.Empty<byte>());
                }
                else
                {
                    RedirectType? redir = hotkey.Redirect;
                    if (!redir.HasValue)
                    {
                        _logger.LogWarning("Invalid redirect type and GUID for kit {0} for player {1}'s {2} hotkey.", hotkey.KitId, e.Steam64, hotkey.Slot);
                        _dbContext.Remove(hotkey);
                        anyChanges = true;
                        continue;
                    }

                    itemValue = new AssetRedirectPageKitItem(0u, hotkey.X, hotkey.Y, 0, hotkey.Page, redir.Value, null);
                }
            }

            _dbContext.Entry(hotkey).State = EntityState.Detached;

            HotkeyBinding binding = new HotkeyBinding(hotkey.KitId, hotkey.Slot, itemValue, hotkey);
            bindings.Add(binding);
        }

        _hotkeys = bindings;
        return anyChanges;
    }

    private async Task DownloadLayouts(PlayerPending e, CancellationToken token, bool anyChanges)
    {
        ulong s64 = e.Steam64.m_SteamID;

        List<KitLayoutTransformation> kitLayoutTransformations = await _dbContext.KitLayoutTransformations
            .Include(x => x.Kit)
            .ThenInclude(x => x.ItemModels)
            .Where(x => x.Steam64 == s64)
            .ToListAsync(token)
            .ConfigureAwait(false);

        List<ItemLayoutTransformationData> layouts = new List<ItemLayoutTransformationData>();

        foreach (KitLayoutTransformation layoutTransformation in kitLayoutTransformations)
        {
            ItemLayoutTransformationData layout = new ItemLayoutTransformationData(layoutTransformation.OldPage,
                layoutTransformation.NewPage, layoutTransformation.OldX, layoutTransformation.OldY,
                layoutTransformation.NewX, layoutTransformation.NewY, layoutTransformation.NewRotation,
                layoutTransformation.KitId, layoutTransformation);

            KitItemModel? item = layoutTransformation.Kit.ItemModels.FirstOrDefault(x =>
                x is IPageKitItem jar && jar.Page == layout.OldPage && jar.X == layout.OldX && jar.Y == layout.OldY);

            if (item == null)
            {
                _logger.LogWarning("{0}'s layout transformation for kit {1} has an invalid item position: {2}, ({3}, {4})",
                    e.Steam64, layout.Kit, layout.OldPage, layout.OldX, layout.OldY);
                _dbContext.Remove(layoutTransformation);
                anyChanges = true;
                continue;
            }

            _dbContext.Entry(layoutTransformation).State = EntityState.Detached;

            layouts.Add(layout);
        }

        _transformationData = layouts;

        if (anyChanges)
        {
            await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        }
    }

    private async Task DownloadAccess(PlayerPending e, CancellationToken token)
    {
        ulong s64 = e.Steam64.m_SteamID;

        _access = await _dbContext.KitAccess
            .Where(x => x.Steam64 == s64)
            .Select(x => x.KitId)
            .ToListAsync(token)
            .ConfigureAwait(false);
    }

    public void Apply(WarfarePlayer player)
    {
        player.Component<ItemTrackingPlayerComponent>().LayoutTransformations = _transformationData;
        player.Component<HotkeyPlayerComponent>().HotkeyBindings = _hotkeys;
        player.Component<KitPlayerComponent>().AccessibleKits = _access;
    }

    bool IPlayerPendingTask.CanReject => false;
}