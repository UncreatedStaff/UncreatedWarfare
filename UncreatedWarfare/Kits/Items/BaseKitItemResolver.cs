using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Kits.Items;

public class BaseKitItemResolver : IKitItemResolver
{
    private readonly AssetRedirectService _redirectService;
    private readonly ILogger<BaseKitItemResolver> _logger;

    public BaseKitItemResolver(IServiceProvider serivceProvider, ILogger<BaseKitItemResolver> logger)
    {
        _redirectService = serivceProvider.GetRequiredService<AssetRedirectService>();
        _logger = logger;
    }

    public virtual KitItemResolutionResult ResolveKitItem(IItem item, Kit? kit, Team requestingTeam)
    {
        byte[] state;
        byte amount;

        if (item is IConcreteItem concrete && concrete.Item.TryGetAsset(out ItemAsset? asset))
        {
            amount = concrete.Amount == byte.MaxValue ? asset.amount : concrete.Amount;
            state = concrete.State == null ? asset.getState(true) : concrete.State.CloneBytes();
            return new KitItemResolutionResult(asset, state, amount, concrete.Quality);
        }

        if (item is not IRedirectedItem redirect
            || (asset = _redirectService.ResolveRedirect(redirect.Item, redirect.Variant ?? string.Empty, kit?.Faction.NullIfDefault(), requestingTeam, out state, out amount)) == null)
        {
            _logger.LogDebug($"Item not found: {item}.");
            return new KitItemResolutionResult(null, Array.Empty<byte>(), 0, 100);
        }

        return new KitItemResolutionResult(asset, state, amount, 100);
    }

    /// <inheritdoc />
    public bool ContainsItem(Kit kitWithItems, IAssetLink<ItemAsset> asset, Team requestingTeam, bool includeAttachments = false)
    {
        IKitItem[] items = kitWithItems.Items;

        foreach (IKitItem item in items)
        {
            if (item is not IConcreteItem concrete)
                continue;

            if (concrete.Item.MatchAsset(asset))
                return true;
            if (includeAttachments && CheckAttached(concrete.Item, concrete.State, asset))
                return true;
        }

        if (!asset.TryGetAsset(out ItemAsset? itemAsset) || !_redirectService.TryFindRedirectType(itemAsset, out RedirectType redirectType, out _, out _, false))
        {
            return false;
        }

        foreach (IKitItem item in items)
        {
            if (item is not IRedirectedItem redirect)
                continue;

            if (redirect.Item == redirectType)
                return true;
        }

        return false;
    }

    /// <inheritdoc />
    public int CountItems(Kit kitWithItems, IAssetLink<ItemAsset> asset, Team requestingTeam, bool includeAttachments = false)
    {
        IKitItem[] items = kitWithItems.Items;

        int ct = 0;
        foreach (IKitItem item in items)
        {
            if (item is not IConcreteItem concrete)
                continue;

            if (concrete.Item.MatchAsset(asset))
                ++ct;
            else if (includeAttachments && CheckAttached(concrete.Item, concrete.State, asset))
                ++ct;
        }

        // concrete items shouldn't coexist with redirected items.
        if (ct > 0)
            return ct;

        if (!asset.TryGetAsset(out ItemAsset? itemAsset) || !_redirectService.TryFindRedirectType(itemAsset, out RedirectType redirectType, out _, out _, false))
        {
            return 0;
        }

        foreach (IKitItem item in items)
        {
            if (item is not IRedirectedItem redirect)
                continue;

            if (redirect.Item == redirectType)
                ++ct;
        }

        return ct;
    }

    private static bool CheckAttached(IAssetLink<ItemAsset> asset, byte[]? state, IAssetLink<ItemAsset> comparator)
    {
        if (state == null
            || state.Length != 18
            || !asset.TryGetAsset(out ItemAsset? gun)
            || gun is not ItemGunAsset
            || !comparator.TryGetId(out ushort id))
        {
            return false;
        }

        for (int a = 0; a < 5; ++a)
        {
            if (BitConverter.ToUInt16(state, a * 2) == id)
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// Replaces the barrel on all guns with the dootpressor during april fools.
/// </summary>
public class DootpressorKitItemResolver : BaseKitItemResolver
{
    private readonly IAssetLink<ItemBarrelAsset> _dootpressor;
    public DootpressorKitItemResolver(IServiceProvider serivceProvider, ILogger<BaseKitItemResolver> logger) : base(serivceProvider, logger)
    {
        _dootpressor = serivceProvider.GetRequiredService<AssetConfiguration>().GetAssetLink<ItemBarrelAsset>("Items:AprilFoolsBarrel");
    }

    /// <inheritdoc />
    public override KitItemResolutionResult ResolveKitItem(IItem item, Kit? kit, Team requestingTeam)
    {
        KitItemResolutionResult result = base.ResolveKitItem(item, kit, requestingTeam);
        if (HolidayUtil.isHolidayActive(ENPCHoliday.APRIL_FOOLS)
            && _dootpressor.TryGetAsset(out ItemBarrelAsset? barrel)
            && result is { Asset.type: EItemType.GUN, State.Length: >= 8 })
        {
            BitConverter.TryWriteBytes(result.State.AsSpan((int)AttachmentType.Barrel), barrel.id);
            result.State[(int)AttachmentType.Barrel / 2 + 13] = 100;
        }

        return result;
    }
}