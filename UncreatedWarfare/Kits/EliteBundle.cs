using Cysharp.Threading.Tasks;
using Stripe;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.SQL;
using Uncreated.Warfare.Networking.Purchasing;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Kits;
public class EliteBundle : IListItem
{
    public const string IdMetadataKey = "product_bundle_key";

    private static readonly List<ProductFeatureOptions> Features = new List<ProductFeatureOptions>
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

    public PrimaryKey PrimaryKey { get; set; }
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public decimal Cost { get; set; }
    public PrimaryKey[] KitKeys { get; set; }
    public Kit?[]? Kits { get; set; }
    public Product? Product { get; set; }
    public PrimaryKey? FactionKey { get; set; }
    public FactionInfo? Faction { get; set; }

    internal static async Task BulkAddStripeEliteBundles(IStripeService stripeService, IPurchaseRecordsInterface purchaseRecord, CancellationToken token = default)
    {
        List<(string id, EliteBundle bundle)> needsStripeBundles = new List<(string, EliteBundle)>();
        
        for (int i = 0; i < purchaseRecord.Bundles.Count; i++)
        {
            EliteBundle bundle = purchaseRecord.Bundles[i];
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

                    query.Append("metadata[\"" + IdMetadataKey + "\"]:\"")
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
                    Expand = StripeService.ProductExpandOptions
                }, cancellationToken: token).ConfigureAwait(false);

                foreach (Product product in existing.Data)
                {
                    if (product.Metadata == null || !product.Metadata.TryGetValue(IdMetadataKey, out string bundleId))
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

    internal static async Task<Product?> GetOrAddProduct(IStripeService stripeService, EliteBundle bundle, bool create = false, CancellationToken token = default)
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
                Query = $"metadata[\"{IdMetadataKey}\"]:\"{id}\"",
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

            StringBuilder sb = new StringBuilder("Included Kits:");
            if (bundle.Kits != null && bundle.Kits.Length == bundle.KitKeys.Length)
            {
                for (int i = 0; i < bundle.Kits.Length; ++i)
                {
                    sb.Append(Environment.NewLine);
                    if (bundle.Kits[i] is { } kit)
                    {
                        sb.Append(kit.GetDisplayName());
                        if (kit.EliteKitInfo is { Product.DefaultPrice.UnitAmountDecimal: { } price })
                            sb.Append($" (Individual price: {price.ToString("C", stripeService.PriceFormatting)})");
                    }
                    else sb.Append("# " + bundle.KitKeys[i].Key.ToString(CultureInfo.InvariantCulture));
                }
            }
            else
            {
                for (int i = 0; i < bundle.KitKeys.Length; ++i)
                {
                    sb.Append(Environment.NewLine)
                        .Append("# " + bundle.KitKeys[i].Key.ToString(CultureInfo.InvariantCulture));
                }
            }


            ProductCreateOptions product = new ProductCreateOptions
            {
                Name = bundle.DisplayName + " Elite Kit Bundle",
                Id = id,
                Description = sb.ToString(),
                DefaultPriceData = new ProductDefaultPriceDataOptions
                {
                    Currency = "USD",
                    UnitAmount = (long)decimal.Round(bundle.Cost * 100),
                    TaxBehavior = "inclusive"
                },
                Shippable = false,
                StatementDescriptor = id.ToUpperInvariant() + " BNDL",
                TaxCode = StripeService.TaxCode,
                Url = UCWarfare.Config.WebsiteUri == null ? null : new Uri(UCWarfare.Config.WebsiteUri, "kits/elites/bundles/" + Uri.EscapeDataString(bundle.Id)).AbsoluteUri,
                Features = Features,
                Metadata = new Dictionary<string, string>
                {
                    { IdMetadataKey, id }
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


    public const string TableBundles = "kits_bundles";
    public const string TableKits = "kits_bundle_items";

    public const string ColumnBundlePrimaryKey = "pk";
    public const string ColumnBundleId = "Id";
    public const string ColumnBundleDisplayName = "DisplayName";
    public const string ColumnBundleDescription = "Description";
    public const string ColumnBundleFaction = "Faction";
    public const string ColumnBundleCost = "Cost";

    public const string ColumnExternalId = "Bundle";

    public const string ColumnKitsKit = "Kit";

    private static Schema[] _schemas;
    public static Schema[] Schemas => _schemas ??= GetSchemas();
    private static Schema[] GetSchemas() => new Schema[]
    {
        new Schema(TableBundles, new Schema.Column[]
        {
            new Schema.Column(ColumnBundlePrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(ColumnBundleId, SqlTypes.String(KitEx.KitNameMaxCharLimit))
            {
                UniqueKey = true
            },
            new Schema.Column(ColumnBundleDisplayName, SqlTypes.String(KitEx.SignTextMaxCharLimit)),
            new Schema.Column(ColumnBundleDescription, SqlTypes.STRING_255),
            new Schema.Column(ColumnBundleFaction, SqlTypes.INCREMENT_KEY)
            {
                Nullable = true,
                ForeignKeyDeleteBehavior = ConstraintBehavior.SetNull,
                ForeignKey = true,
                ForeignKeyTable = "factions",
                ForeignKeyColumn = "pk"
            },
            new Schema.Column(ColumnBundleCost, "double")
        }, true, typeof(EliteBundle)),
        new Schema(TableKits, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalId, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyTable = TableBundles,
                ForeignKeyColumn = ColumnBundlePrimaryKey
            },
            new Schema.Column(ColumnKitsKit, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyTable = KitManager.TABLE_MAIN,
                ForeignKeyColumn = KitManager.COLUMN_PK
            }
        }, false, typeof(Kit))
    };
}