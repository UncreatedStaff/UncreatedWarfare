﻿using Stripe;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.SQL;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Kits;

namespace Uncreated.Warfare.Networking.Purchasing;

[NotMapped]
public class StripeEliteKit
{
    private static readonly List<ProductFeatureOptions> Features = new List<ProductFeatureOptions>
    {
        new ProductFeatureOptions
        {
            Name = "Permanent"
        }
    };

    public PrimaryKey PrimaryKey { get; private set; }
    public string KitId { get; private set; }
    public Product Product { get; private set; }
    internal static async Task BulkAddStripeEliteKits(IStripeService stripeService, IPurchaseRecordsInterface purchaseRecord, CancellationToken token = default)
    {
        KitManager? manager = UCWarfare.IsLoaded ? KitManager.GetSingletonQuick() : null;

        List<(string id, Kit kit)> needsStripeKits = new List<(string, Kit)>();

        await stripeService.Semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            for (int i = 0; i < purchaseRecord.Kits.Count; i++)
            {
                Kit kit = purchaseRecord.Kits[i];
                if (kit is not { Type: KitType.Elite, EliteKitInfo: null })
                    continue;

                needsStripeKits.Add((kit.InternalName, kit));
            }

            if (needsStripeKits.Count == 0)
                return;
            StringBuilder query = new StringBuilder(256);
            int index = 0;
            do
            {
                int max = Math.Min(needsStripeKits.Count, index + 10);
                for (int i = index; i < max; i++)
                {
                    if (i != index) query.Append(" OR ");

                    query.Append("metadata[\"" + PurchaseRecordsInterface<WarfareDbContext>.BundleIdMetadataKey + "\"]:\"")
                        .Append(needsStripeKits[i].id)
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
                    if (product.Metadata == null || !product.Metadata.TryGetValue(PurchaseRecordsInterface<WarfareDbContext>.BundleIdMetadataKey, out string kitId))
                    {
                        L.LogWarning($"Unknown product id key from searched product: {product.Name}.");
                        continue;
                    }

                    (_, Kit kit) = needsStripeKits.Find(x => x.id.Equals(kitId, StringComparison.Ordinal));
                    kit.EliteKitInfo = new StripeEliteKit
                    {
                        KitId = kit.InternalName,
                        PrimaryKey = kit.PrimaryKey,
                        Product = product
                    };
                    if (product is { DefaultPrice.UnitAmountDecimal: { } price })
                    {
                        price = decimal.Round(decimal.Divide(price, 100m), 2);
                        decimal oldPrice = kit.PremiumCost;
                        kit.PremiumCost = price;
                        if (manager is { IsLoading: true } && oldPrice != price)
                            kit.IsLoadDirty = true;
                    }
                }
            } while (index < needsStripeKits.Count);
        }
        finally
        {
            stripeService.Semaphore.Release();
        }
    }
    internal static async Task<StripeEliteKit?> GetOrAddProduct(IStripeService stripeService, Kit kit, bool create = false, CancellationToken token = default)
    {
        if (kit is null)
            throw new ArgumentNullException(nameof(kit));

        if (kit.Type != KitType.Elite)
            throw new ArgumentException("Kit type must be KitType.Elite.", nameof(kit));

        if (!UCWarfare.IsLoaded || stripeService?.StripeClient == null)
            throw new InvalidOperationException("Stripe service is not loaded in Data.StripeService.");

        string id = kit.InternalName;

        StripeEliteKit eliteKit = new StripeEliteKit
        {
            KitId = id,
            PrimaryKey = kit.PrimaryKey,
        };
        Product newProduct;
        await stripeService.Semaphore.WaitAsync(token);
        try
        {
            StripeSearchResult<Product> existing = await stripeService.ProductService.SearchAsync(new ProductSearchOptions
            {
                Query = $"metadata[\"{PurchaseRecordsInterface<WarfareDbContext>.BundleIdMetadataKey}\"]:\"{id}\"",
                Limit = 1
            }, cancellationToken: token).ConfigureAwait(false);
            
            if (existing.Data.Count > 0)
            {
                eliteKit.Product = existing.Data.First();
                return eliteKit;
            }

            if (!create)
            {
                eliteKit.Product = null!;
                return null;
            }
            ProductCreateOptions product = new ProductCreateOptions
            {
                Name = kit.GetDisplayName(),
                Id = id,
                Description = Localization.TranslateEnum(kit.Class) + " kit in the " + Localization.TranslateEnum(kit.Branch) + " branch.",
                DefaultPriceData = new ProductDefaultPriceDataOptions
                {
                    Currency = "USD",
                    UnitAmount = (long)decimal.Round(kit.PremiumCost * 100),
                    TaxBehavior = "inclusive"
                },
                Shippable = false,
                StatementDescriptor = id.ToUpperInvariant(),
                TaxCode = StripeService.TaxCode,
                Url = UCWarfare.IsLoaded && UCWarfare.Config.WebsiteUri != null ? new Uri(UCWarfare.Config.WebsiteUri, "kits/elites/" + Uri.EscapeDataString(kit.InternalName)).AbsoluteUri : ("https://uncreated.network/kits/elites/" + Uri.EscapeDataString(kit.InternalName)),
                Features = Features,
                Metadata = new Dictionary<string, string>
                {
                    { PurchaseRecordsInterface<WarfareDbContext>.BundleIdMetadataKey, id }
                }
            };

            newProduct = await stripeService.ProductService.CreateAsync(product, cancellationToken: token).ConfigureAwait(false);
        }
        finally
        {
            stripeService.Semaphore.Release();
        }
        await UCWarfare.ToUpdate(token);
        eliteKit.Product = newProduct;
        return eliteKit;
    }
}
