using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Stripe;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Kits.Bundles;
using Uncreated.Warfare.Networking.Purchasing;

namespace Uncreated.Warfare.Kits;
public interface IPurchaseRecordsInterface
{
    IReadOnlyList<EliteBundle> Bundles { get; }
    IReadOnlyList<Kit> Kits { get; }
    Product LoadoutProduct { get; }
    SemaphoreSlim Semaphore { get; }
    Task RefreshAll(CancellationToken token = default);
    Task RefreshBundles(CancellationToken token = default);
    Task RefreshKits(CancellationToken token = default);
}

public abstract class PurchaseRecordsInterface<TDbContext> : IPurchaseRecordsInterface, IDisposable where TDbContext : IKitsDbContext, new()
{
    public const string LoadoutId = "loadout";

    public const string BundleIdMetadataKey = "product_bundle_key";

    private const string LoadoutDescription = "A loadout is your own custom version of an existing kit class (e.g Rifleman, Medic) that you can request from a special sign.\r\n" +
                                              "You can have pretty much anything in your loadout as long as each item selection adds up to 10 points.\r\n" +
                                              "For most people that's 1 primary + 1 secondary or attachments, or some combination of both (which covers basically every gun in Uncreated Armory).";

    protected static readonly List<ProductFeatureOptions> LoadoutFeatures = new List<ProductFeatureOptions>
    {
        new ProductFeatureOptions
        {
            Name = "Permanent"
        },
        new ProductFeatureOptions
        {
            Name = "Customizable"
        }
    };

    protected static readonly List<ProductFeatureOptions> BundleFeatures = new List<ProductFeatureOptions>
    {
        new ProductFeatureOptions
        {
            Name = "Permanent"
        },
        new ProductFeatureOptions
        {
            Name = "Bulk Discount"
        }
    };


    private EliteBundle[] _bundles = null!;
    private Kit[] _kits = null!;
    public IReadOnlyList<EliteBundle> Bundles { get; private set; }
    public IReadOnlyList<Kit> Kits { get; set; }
    public Product LoadoutProduct { get; set; }
    public abstract IStripeService StripeService { get; }
    public bool FilterLoadouts { get; set; } = true;
    public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);
    public static async Task<T> Create<T>(bool createMissingProducts, CancellationToken token = default) where T : PurchaseRecordsInterface<TDbContext>, new()
    {
        T pri = new T();
        await pri.RefreshAll(createMissingProducts, token).ConfigureAwait(false);
        return pri;
    }
    public Task RefreshAll(CancellationToken token = default) => RefreshAll(false, token);
    public async Task RefreshAll(bool createMissingProducts, CancellationToken token = default)
    {
        await RefreshLoadoutProduct(createMissingProducts, token).ConfigureAwait(false);
        await RefreshBundles(true, false, token).ConfigureAwait(false);
        await RefreshKits(false, false, createMissingProducts, token).ConfigureAwait(false);
        await FetchStripeBundleProducts(createMissingProducts, token).ConfigureAwait(false);
    }

    protected virtual IQueryable<EliteBundle> OnInclude(IQueryable<EliteBundle> set)
    {
        IIncludableQueryable<EliteBundle, Kit> kit = set
            .Include(x => x.Kits)
                .ThenInclude(x => x.Kit);

        OnIncludeForBundles(kit);

        return set.Include(x => x.Faction);
    }
    protected virtual IIncludableQueryable<EliteBundle, Kit> OnIncludeForBundles(IIncludableQueryable<EliteBundle, Kit> set)
    {
        set.ThenInclude(x => x.Translations);
        set.ThenInclude(x => x.Faction);

        return set;
    }
    protected virtual IQueryable<Kit> OnInclude(IQueryable<Kit> set)
    {
        return set
            .Include(x => x.Bundles).ThenInclude(x => x.Bundle)
            .Include(x => x.Translations)
            .Include(x => x.Faction);
    }
    public async Task RefreshLoadoutProduct(bool createIfNotFound, CancellationToken token = default)
    {
        if (StripeService?.StripeClient == null)
            return;

        await Semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await StripeService.Semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                StripeSearchResult<Product> existing = await StripeService.ProductService.SearchAsync(new ProductSearchOptions
                {
                    Query = $"metadata[\"{LoadoutId}\"]:\"{LoadoutId}\"",
                    Limit = 1
                }, cancellationToken: token).ConfigureAwait(false);

                Product? prod = existing.FirstOrDefault();

                if (prod == null && createIfNotFound)
                {
                    ProductCreateOptions product = new ProductCreateOptions
                    {
                        Name = "Custom Loadout",
                        Id = LoadoutId,
                        Description = LoadoutDescription,
                        DefaultPriceData = new ProductDefaultPriceDataOptions
                        {
                            Currency = "USD",
                            UnitAmount = (long)decimal.Round((UCWarfare.IsLoaded ? UCWarfare.Config.LoadoutCost : 10m) * 100),
                            TaxBehavior = "inclusive"
                        },
                        Shippable = false,
                        StatementDescriptor = "LOADOUT",
                        TaxCode = Networking.Purchasing.StripeService.TaxCode,
                        Url = UCWarfare.IsLoaded && UCWarfare.Config.WebsiteUri != null ? new Uri(UCWarfare.Config.WebsiteUri, "kits/loadout").AbsoluteUri : "https://uncreated.network/kits/loadout",
                        Features = LoadoutFeatures,
                        Metadata = new Dictionary<string, string>
                        {
                            { LoadoutId, LoadoutId }
                        }
                    };

                    prod = await StripeService.ProductService.CreateAsync(product, cancellationToken: token).ConfigureAwait(false);
                }

                LoadoutProduct = prod!;
            }
            finally
            {
                StripeService.Semaphore.Release();
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }
    public Task RefreshBundles(CancellationToken token = default) => RefreshBundles(false, false, token);
    public async Task RefreshBundles(bool holdStripeReload = false, bool createMissingStripeBundles = false, CancellationToken token = default)
    {
        await Semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await using IKitsDbContext dbContext = new TDbContext();
            _bundles = await OnInclude(dbContext.EliteBundles)
                .ToArrayAsync(token).ConfigureAwait(false);

            CleanupReferences();

            Bundles = new ReadOnlyCollection<EliteBundle>(_bundles);

            if (!holdStripeReload)
            {
                await FetchStripeBundleProductsIntl(createMissingStripeBundles, token).ConfigureAwait(false);
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }
    public async Task FetchStripeBundleProducts(bool createMissingProducts, CancellationToken token = default)
    {
        if (StripeService?.StripeClient == null)
            return;

        await Semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await FetchStripeBundleProductsIntl(createMissingProducts, token).ConfigureAwait(false);
        }
        finally
        {
            Semaphore.Release();
        }
    }
    public async Task FetchStripeKitProducts(bool createMissingProducts, CancellationToken token = default)
    {
        if (StripeService?.StripeClient == null)
            return;

        await Semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await FetchStripeKitProductsIntl(createMissingProducts, token).ConfigureAwait(false);
        }
        finally
        {
            Semaphore.Release();
        }
    }
    private async Task FetchStripeBundleProductsIntl(bool create, CancellationToken token)
    {
        if (StripeService?.StripeClient == null)
            return;

        await BulkAddStripeEliteBundles(StripeService, token).ConfigureAwait(false);
        if (!UCWarfare.IsLoaded)
            return;

        for (int i = 0; i < _bundles.Length; ++i)
        {
            EliteBundle bundle = _bundles[i];
            if (bundle.Product == null)
            {
                L.Log($"Creating stripe product for {bundle.DisplayName}.");
                await GetOrAddProduct(StripeService, bundle, create, token).ConfigureAwait(false);
                L.Log("  ... Done");
            }
        }
    }
    private async Task FetchStripeKitProductsIntl(bool create, CancellationToken token)
    {
        if (StripeService?.StripeClient == null)
            return;

        await StripeEliteKit.BulkAddStripeEliteKits(StripeService, this, token).ConfigureAwait(false);

        if (!UCWarfare.IsLoaded)
            return;

        for (int i = 0; i < _kits.Length; ++i)
        {
            Kit kit = _kits[i];
            if (kit is not { Type: KitType.Elite, EliteKitInfo: null })
                continue;

            L.Log($"Creating stripe product for {kit.GetDisplayName()}.");
            await StripeEliteKit.GetOrAddProduct(StripeService, kit, create, token).ConfigureAwait(false);
            L.Log("  ... Done");
        }
    }
    public Task RefreshKits(CancellationToken token = default) => RefreshKits(false, false, false, token);
    public async Task RefreshKits(bool forceNotUseKitManager = false, bool holdStripeReload = false, bool createMissingStripeKits = false, CancellationToken token = default)
    {
        await Semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await using IKitsDbContext dbContext = new TDbContext();
            if (!forceNotUseKitManager && UCWarfare.IsLoaded && Data.Singletons != null && Data.Gamemode != null && KitManager.GetSingletonQuick() is { } kitManager)
            {
                //_kits = new Kit[kitManager.Items.Count];
                //for (int i = 0; i < kitManager.Items.Count; ++i)
                //{
                //    Kit kit = kitManager.Items[i];
                //    _kits[i] = kit; todo
                //}
            }
            else
            {
                _kits = await OnInclude(dbContext.Kits).ToArrayAsync(token).ConfigureAwait(false);
            }

            CleanupReferences();

            Kits = new ReadOnlyCollection<Kit>(_kits);

            if (!holdStripeReload)
            {
                await FetchStripeKitProductsIntl(createMissingStripeKits, token).ConfigureAwait(false);
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }
    private void CleanupReferences()
    {
        if (_bundles is not { Length: > 0 } || _kits is not { Length: > 0 })
            return;

        for (int i = 0; i < _kits.Length; ++i)
        {
            Kit kit = _kits[i];
            if (kit.Bundles is not { Count: > 0 })
                continue;

            foreach (KitEliteBundle bundleLink in kit.Bundles)
            {
                EliteBundle? other = Array.Find(_bundles, x => x.PrimaryKey == bundleLink.BundleId);
                if (other != null)
                    bundleLink.Bundle = other;

                if (bundleLink.Bundle.Kits == null)
                    continue;

                foreach (KitEliteBundle kitLink in bundleLink.Bundle.Kits)
                {
                    if (kitLink.KitId == kit.PrimaryKey)
                        kitLink.Kit = kit;
                    kitLink.Bundle = bundleLink.Bundle;
                }
            }
        }
        for (int i = 0; i < _bundles.Length; ++i)
        {
            EliteBundle bundle = _bundles[i];
            if (bundle.Kits is not { Count: > 0 })
                continue;

            foreach (KitEliteBundle kitLink in bundle.Kits)
            {
                Kit? other = Array.Find(_kits, x => x.PrimaryKey == kitLink.BundleId);
                if (other != null)
                    kitLink.Kit = other;

                if (kitLink.Kit.Bundles == null)
                    continue;

                foreach (KitEliteBundle bundleLink in kitLink.Kit.Bundles)
                {
                    if (bundleLink.BundleId == bundle.PrimaryKey)
                        bundleLink.Bundle = bundle;
                    bundleLink.Kit = kitLink.Kit;
                }
            }
        }
    }
    public void Dispose()
    {
        if (StripeService is IDisposable disp)
            disp.Dispose();
        Semaphore.Dispose();
    }

    internal async Task BulkAddStripeEliteBundles(IStripeService stripeService, CancellationToken token = default)
    {
        List<(string id, EliteBundle bundle)> needsStripeBundles = new List<(string, EliteBundle)>();

        for (int i = 0; i < Bundles.Count; i++)
        {
            EliteBundle bundle = Bundles[i];
            if (bundle == null)
                continue;

            needsStripeBundles.Add((bundle.Id, bundle));
        }

        if (needsStripeBundles.Count == 0)
            return;

        await stripeService.Semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            StringBuilder query = new StringBuilder(256);
            int index = 0;
            do
            {
                int max = Math.Min(needsStripeBundles.Count, index + 10);
                for (int i = index; i < max; i++)
                {
                    if (i != index) query.Append(" OR ");

                    query.Append("metadata[\"" + BundleIdMetadataKey + "\"]:\"")
                        .Append(needsStripeBundles[i].id)
                        .Append('\"');
                }

                index = max;

                string q = query.ToString();
                query.Clear();

                StripeSearchResult<Product> existing = await stripeService.ProductService.SearchAsync(new ProductSearchOptions
                {
                    Query = q,
                    Limit = 100,
                    Expand = Networking.Purchasing.StripeService.ProductExpandOptions
                }, cancellationToken: token).ConfigureAwait(false);

                foreach (Product product in existing.Data)
                {
                    if (product.Metadata == null || !product.Metadata.TryGetValue(BundleIdMetadataKey, out string bundleId))
                    {
                        L.LogWarning($"Unknown product bundle id key from searched product: {product.Name}.");
                        continue;
                    }

                    (_, EliteBundle bundle) = needsStripeBundles.Find(x => x.id.Equals(bundleId, StringComparison.Ordinal));
                    bundle.Product = product;
                    if (bundle.Product is { DefaultPrice.UnitAmountDecimal: { } price })
                        bundle.Cost = price;
                }

            } while (index < needsStripeBundles.Count);
        }
        finally
        {
            stripeService.Semaphore.Release();
        }
    }

    internal async Task<Product?> GetOrAddProduct(IStripeService stripeService, EliteBundle bundle, bool create = false, CancellationToken token = default)
    {
        if (bundle is null)
            throw new ArgumentNullException(nameof(bundle));

        if (!UCWarfare.IsLoaded || stripeService?.StripeClient == null)
            throw new InvalidOperationException("Stripe service is not loaded in Data.StripeService.");

        string id = bundle.Id;

        Product newProduct;
        await stripeService.Semaphore.WaitAsync(token);
        try
        {
            StripeSearchResult<Product> existing = await stripeService.ProductService.SearchAsync(new ProductSearchOptions
            {
                Query = $"metadata[\"{BundleIdMetadataKey}\"]:\"{id}\"",
                Limit = 1
            }, cancellationToken: token).ConfigureAwait(false);

            if (existing.Data.Count > 0)
            {
                bundle.Product = existing.Data.First();
                return bundle.Product;
            }

            if (!create)
            {
                bundle.Product = null;
                return null;
            }

            string desc = bundle.Description;
            if (bundle.Kits != null)
            {
                StringBuilder sb = new StringBuilder("Included Kits:");
                foreach (Kit includedKit in bundle.Kits.Select(x => x.Kit))
                {
                    sb.Append(Environment.NewLine);
                    sb.Append(includedKit.GetDisplayName());
                    if (includedKit.EliteKitInfo is { Product.DefaultPrice.UnitAmountDecimal: { } price })
                        sb.Append($" (Individual price: {price.ToString("C", stripeService.PriceFormatting)})");
                }
                desc = Environment.NewLine + Environment.NewLine + sb;
            }


            ProductCreateOptions product = new ProductCreateOptions
            {
                Name = bundle.DisplayName + " Elite Kit Bundle",
                Id = id,
                Description = desc,
                DefaultPriceData = new ProductDefaultPriceDataOptions
                {
                    Currency = "USD",
                    UnitAmount = (long)decimal.Round(bundle.Cost * 100),
                    TaxBehavior = "inclusive"
                },
                Shippable = false,
                StatementDescriptor = id.ToUpperInvariant() + " BNDL",
                TaxCode = Networking.Purchasing.StripeService.TaxCode,
                Url = UCWarfare.Config.WebsiteUri == null ? null : new Uri(UCWarfare.Config.WebsiteUri, "kits/elites/bundles/" + Uri.EscapeDataString(bundle.Id)).AbsoluteUri,
                Features = BundleFeatures,
                Metadata = new Dictionary<string, string>
                {
                    { BundleIdMetadataKey, id }
                }
            };

            newProduct = await stripeService.ProductService.CreateAsync(product, cancellationToken: token).ConfigureAwait(false);
        }
        finally
        {
            stripeService.Semaphore.Release();
        }
        await UniTask.SwitchToMainThread(token);
        bundle.Product = newProduct;
        return newProduct;
    }
}
#if NETSTANDARD || NETFRAMEWORK
internal class WarfarePurchaseRecordsInterface : PurchaseRecordsInterface<WarfareDbContext>
{
    public override IStripeService StripeService => Data.WarfareStripeService;
}
#endif