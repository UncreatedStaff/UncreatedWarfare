﻿using Stripe;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Steamworks;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Networking.Purchasing;
using Uncreated.Warfare.Teams;
using static Uncreated.Warfare.Kits.KitManager;

namespace Uncreated.Warfare.Kits;
public interface IPurchaseRecordsInterface
{
    IReadOnlyList<EliteBundle> Bundles { get; }
    IReadOnlyList<Kit> Kits { get; }
    Product LoadoutProduct { get; }
    UCSemaphore Semaphore { get; }
    Task VerifyTables(CancellationToken token = default);
    Task RefreshAll(CancellationToken token = default);
    Task RefreshBundles(CancellationToken token = default);
    Task RefreshKits(CancellationToken token = default);
}

public abstract class PurchaseRecordsInterface : IPurchaseRecordsInterface, IDisposable
{
    public const string LoadoutId = "loadout";

    private const string LoadoutDescription = "A loadout is your own custom version of an existing kit class (e.g Rifleman, Medic) that you can request from a special sign.\r\n" +
                                              "You can have pretty much anything in your loadout as long as each item selection adds up to 10 points.\r\n" +
                                              "For most people that's 1 primary + 1 secondary or attachments, or some combination of both (which covers basically every gun in Uncreated Armory).";

    private static readonly List<ProductFeatureOptions> LoadoutFeatures = new List<ProductFeatureOptions>
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


    private EliteBundle[] _bundles = null!;
    private Kit[] _kits = null!;
    public IReadOnlyList<EliteBundle> Bundles { get; private set; }
    public IReadOnlyList<Kit> Kits { get; set; }
    public Product LoadoutProduct { get; set; }
    public abstract IMySqlDatabase Sql { get; }
    public abstract IStripeService StripeService { get; }
    public UCSemaphore Semaphore { get; } = new UCSemaphore();
    public static async Task<T> Create<T>(CancellationToken token = default) where T : PurchaseRecordsInterface, new()
    {
        T pri = new T();
        await pri.VerifyTables(token).ConfigureAwait(false);
        await pri.RefreshAll(token).ConfigureAwait(false);
        return pri;
    }
    public Task VerifyTables(CancellationToken token = default)
    {
        Schema[] schemas = new Schema[SCHEMAS.Length + EliteBundle.Schemas.Length];
        Array.Copy(SCHEMAS, schemas, SCHEMAS.Length);
        Array.Copy(EliteBundle.Schemas, 0, schemas, SCHEMAS.Length, EliteBundle.Schemas.Length);
        return Sql.VerifyTables(schemas, token);
    }
    public async Task RefreshAll(CancellationToken token = default)
    {
        await RefreshLoadoutProduct(token).ConfigureAwait(false);
        await RefreshBundles(true, token).ConfigureAwait(false);
        await RefreshKits(false, false, token).ConfigureAwait(false);
        await FetchStripeBundleProducts(token).ConfigureAwait(false);
    }
    public async Task RefreshLoadoutProduct(CancellationToken token = default)
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

                if (prod == null)
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

                LoadoutProduct = prod;
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
    public Task RefreshBundles(CancellationToken token = default) => RefreshBundles(false, token);
    public async Task RefreshBundles(bool holdStripeReload = false, CancellationToken token = default)
    {
        await Semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            List<EliteBundle> bundles = new List<EliteBundle>(16);

            await Sql.QueryAsync(F.BuildSelect(
                EliteBundle.TableBundles, EliteBundle.ColumnBundlePrimaryKey, EliteBundle.ColumnBundleId, EliteBundle.ColumnBundleDisplayName,
                EliteBundle.ColumnBundleDescription, EliteBundle.ColumnBundleFaction, EliteBundle.ColumnBundleCost), null, reader =>
                {
                    bundles.Add(new EliteBundle
                    {
                        PrimaryKey = reader.GetInt32(0),
                        Id = reader.GetString(1),
                        DisplayName = reader.GetString(2),
                        Description = reader.GetString(3),
                        FactionKey = reader.IsDBNull(4) ? null : new PrimaryKey(reader.GetInt32(4)),
                        Cost = decimal.Round(new decimal(reader.GetDouble(5)), 2)
                    });
                }, token).ConfigureAwait(false);

            List<PrimaryKeyPair<PrimaryKey>> kits = new List<PrimaryKeyPair<PrimaryKey>>();
            await Sql.QueryAsync("SELECT " + SqlTypes.ColumnList(EliteBundle.ColumnExternalId, EliteBundle.ColumnKitsKit) +
                                 " FROM `" + EliteBundle.TableKits + "` ORDER BY `" + EliteBundle.ColumnExternalId + "`;",
                null, reader =>
                {
                    kits.Add(new PrimaryKeyPair<PrimaryKey>(reader.GetInt32(0), reader.GetInt32(1)));
                }, token).ConfigureAwait(false);

            F.ApplyQueriedList(kits, (key, arr) =>
            {
                EliteBundle bundle = bundles.Find(x => x.PrimaryKey.Key == key);
                if (bundle != null)
                    bundle.KitKeys = arr;
            }, false);

            if (!TeamManager.FactionsLoaded)
                await TeamManager.ReloadFactions(token).ConfigureAwait(false);

            for (int i = 0; i < bundles.Count; ++i)
            {
                EliteBundle bundle = bundles[i];
                if (!bundle.FactionKey.HasValue)
                    continue;

                bundle.Faction = TeamManager.GetFactionInfo(bundle.FactionKey.Value);
            }

            if (_kits != null)
            {
                // fill in kits
                for (int i = 0; i < bundles.Count; ++i)
                {
                    EliteBundle bundle = bundles[i];
                    if (bundle.KitKeys == null)
                        continue;

                    bundle.Kits = new Kit[bundle.KitKeys.Length];

                    for (int j = 0; j < bundle.KitKeys.Length; ++j)
                    {
                        int pk = bundle.KitKeys[j].Key;
                        for (int k = 0; k < _kits.Length; ++k)
                        {
                            if (_kits[k].PrimaryKey.Key == pk)
                            {
                                bundle.Kits[j] = _kits[k];
                                break;
                            }
                        }
                    }
                }
            }


            _bundles = bundles.ToArray();
            Bundles = new ReadOnlyCollection<EliteBundle>(_bundles);

            if (!holdStripeReload)
            {
                await FetchStripeBundleProductsIntl(token).ConfigureAwait(false);
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }
    public async Task FetchStripeBundleProducts(CancellationToken token = default)
    {
        await Semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await FetchStripeBundleProductsIntl(token).ConfigureAwait(false);
        }
        finally
        {
            Semaphore.Release();
        }
    }
    public async Task FetchStripeKitProducts(CancellationToken token = default)
    {
        await Semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await FetchStripeKitProductsIntl(token).ConfigureAwait(false);
        }
        finally
        {
            Semaphore.Release();
        }
    }
    private async Task FetchStripeBundleProductsIntl(CancellationToken token)
    {
        await EliteBundle.BulkAddStripeEliteBundles(StripeService, this, token);
        if (UCWarfare.IsLoaded)
        {
            for (int i = 0; i < _bundles.Length; ++i)
            {
                EliteBundle bundle = _bundles[i];
                if (bundle.Product == null)
                {
                    L.Log($"Creating stripe product for {bundle.DisplayName}.");
                    await EliteBundle.GetOrAddProduct(StripeService, bundle, true, token).ConfigureAwait(false);
                    L.Log("  ... Done");
                }
            }
        }
    }
    private async Task FetchStripeKitProductsIntl(CancellationToken token)
    {
        await StripeEliteKit.BulkAddStripeEliteKits(StripeService, this, token);
        if (UCWarfare.IsLoaded)
        {
            for (int i = 0; i < _kits.Length; ++i)
            {
                Kit kit = _kits[i];
                if (kit is { Type: KitType.Elite, EliteKitInfo: null })
                {
                    L.Log($"Creating stripe product for {kit.GetDisplayName()}.");
                    await StripeEliteKit.GetOrAddProduct(StripeService, kit, true, token).ConfigureAwait(false);
                    L.Log("  ... Done");
                }
            }
        }
    }
    public Task RefreshKits(CancellationToken token = default) => RefreshKits(false, false, token);
    public async Task RefreshKits(bool forceNotUseKitManager = false, bool holdStripeReload = false, CancellationToken token = default)
    {
        await Semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (!forceNotUseKitManager && UCWarfare.IsLoaded && Data.Singletons != null && Data.Gamemode != null && GetSingletonQuick() is { } kitManager)
            {
                await kitManager.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    await kitManager.WriteWaitAsync(token).ConfigureAwait(false);
                    try
                    {
                        _kits = new Kit[kitManager.Items.Count];
                        int index = -1;
                        for (int i = 0; i < kitManager.Items.Count; ++i)
                        {
                            SqlItem<Kit> proxy = kitManager.Items[i];
                            Kit? kit = proxy.Item;
                            if (kit != null)
                                _kits[++index] = kit;
                        }

                        if (index != kitManager.Items.Count - 1)
                            Array.Resize(ref _kits, index + 1);

                        Kits = new ReadOnlyCollection<Kit>(_kits);
                    }
                    finally
                    {
                        kitManager.WriteRelease();
                    }
                }
                finally
                {
                    kitManager.Release();
                }

                return;
            }

            List<Kit> kits = new List<Kit>(256);

            await Sql.QueryAsync(F.BuildSelect(TABLE_MAIN, COLUMN_PK, COLUMN_KIT_ID, COLUMN_FACTION,
                COLUMN_CLASS, COLUMN_BRANCH, COLUMN_TYPE, COLUMN_REQUEST_COOLDOWN, COLUMN_TEAM_LIMIT,
                COLUMN_SEASON, COLUMN_DISABLED, COLUMN_WEAPONS, COLUMN_SQUAD_LEVEL, COLUMN_COST_CREDITS,
                COLUMN_COST_PREMIUM, COLUMN_REQUIRES_NITRO, COLUMN_MAPS_WHITELIST, COLUMN_FACTIONS_WHITELIST, COLUMN_CREATOR,
                COLUMN_LAST_EDITOR, COLUMN_CREATION_TIME, COLUMN_LAST_EDIT_TIME), null, reader =>
                {
                    kits.Add(ReadKit(reader));
                }, token).ConfigureAwait(false);

            List<PrimaryKeyPair<IKitItem>> items = new List<PrimaryKeyPair<IKitItem>>();
            await Sql.QueryAsync("SELECT " + SqlTypes.ColumnList(COLUMN_EXT_PK, COLUMN_ITEM_GUID, COLUMN_ITEM_X, COLUMN_ITEM_Y, COLUMN_ITEM_ROTATION, COLUMN_ITEM_PAGE,
                COLUMN_ITEM_CLOTHING, COLUMN_ITEM_REDIRECT, COLUMN_ITEM_AMOUNT, COLUMN_ITEM_METADATA) + " FROM `" + TABLE_ITEMS + "` ORDER BY `" + COLUMN_EXT_PK + "`;",
                null, reader =>
                {
                    items.Add(new PrimaryKeyPair<IKitItem>(reader.GetInt32(0), ReadItem(reader)));
                }, token).ConfigureAwait(false);

            F.ApplyQueriedList(items, (key, arr) =>
            {
                Kit kit = kits.Find(x => x.PrimaryKey.Key == key);
                if (kit != null)
                    kit.Items = arr;
            }, false);

            List<PrimaryKeyPair<UnlockRequirement>> unlockRequirements = new List<PrimaryKeyPair<UnlockRequirement>>();
            await Sql.QueryAsync("SELECT " + SqlTypes.ColumnList(COLUMN_EXT_PK, UnlockRequirement.COLUMN_JSON) + " FROM `" + TABLE_UNLOCK_REQUIREMENTS + "` ORDER BY `" + COLUMN_EXT_PK + "`;",
                null, reader =>
                {
                    UnlockRequirement? req = UnlockRequirement.Read(reader);
                    if (req != null)
                        unlockRequirements.Add(new PrimaryKeyPair<UnlockRequirement>(reader.GetInt32(0), req));
                }, token).ConfigureAwait(false);

            F.ApplyQueriedList(unlockRequirements, (key, arr) =>
            {
                Kit kit = kits.Find(x => x.PrimaryKey.Key == key);
                if (kit != null)
                    kit.UnlockRequirements = arr;
            }, false);

            List<PrimaryKeyPair<Skillset>> skillsetRequirements = new List<PrimaryKeyPair<Skillset>>();
            await Sql.QueryAsync("SELECT " + SqlTypes.ColumnList(COLUMN_EXT_PK, Skillset.COLUMN_SKILL, Skillset.COLUMN_LEVEL) + " FROM `" + TABLE_SKILLSETS + "` ORDER BY `" + COLUMN_EXT_PK + "`;",
                null, reader =>
                {
                    Skillset skillset = Skillset.Read(reader);
                    skillsetRequirements.Add(new PrimaryKeyPair<Skillset>(reader.GetInt32(0), skillset));
                }, token).ConfigureAwait(false);

            F.ApplyQueriedList(skillsetRequirements, (key, arr) =>
            {
                Kit kit = kits.Find(x => x.PrimaryKey.Key == key);
                if (kit != null)
                    kit.Skillsets = arr;
            }, false);

            List<PrimaryKeyPair<PrimaryKey>> pkRequirements = new List<PrimaryKeyPair<PrimaryKey>>();
            await Sql.QueryAsync("SELECT " + SqlTypes.ColumnList(COLUMN_EXT_PK, COLUMN_FILTER_FACTION) + " FROM `" + TABLE_FACTION_FILTER + "` ORDER BY `" + COLUMN_EXT_PK + "`;",
                null, reader =>
                {
                    pkRequirements.Add(new PrimaryKeyPair<PrimaryKey>(reader.GetInt32(0), new PrimaryKey(reader.GetInt32(1))));
                }, token).ConfigureAwait(false);

            F.ApplyQueriedList(pkRequirements, (key, arr) =>
            {
                Kit kit = kits.Find(x => x.PrimaryKey.Key == key);
                if (kit != null)
                    kit.FactionFilter = arr;
            }, false);

            pkRequirements.Clear();

            await Sql.QueryAsync("SELECT " + SqlTypes.ColumnList(COLUMN_EXT_PK, COLUMN_FILTER_MAP) + " FROM `" + TABLE_MAP_FILTER + "` ORDER BY `" + COLUMN_EXT_PK + "`;",
                null, reader =>
                {
                    pkRequirements.Add(new PrimaryKeyPair<PrimaryKey>(reader.GetInt32(0), new PrimaryKey(reader.GetInt32(1))));
                }, token).ConfigureAwait(false);

            F.ApplyQueriedList(pkRequirements, (key, arr) =>
            {
                Kit kit = kits.Find(x => x.PrimaryKey.Key == key);
                if (kit != null)
                    kit.MapFilter = arr;
            }, false);

            pkRequirements.Clear();

            await Sql.QueryAsync("SELECT " + SqlTypes.ColumnList(COLUMN_EXT_PK, COLUMN_REQUEST_SIGN) + " FROM `" + TABLE_REQUEST_SIGNS + "` ORDER BY `" + COLUMN_EXT_PK + "`;",
                null, reader =>
                {
                    pkRequirements.Add(new PrimaryKeyPair<PrimaryKey>(reader.GetInt32(0), new PrimaryKey(reader.GetInt32(1))));
                }, token).ConfigureAwait(false);

            F.ApplyQueriedList(pkRequirements, (key, arr) =>
            {
                Kit kit = kits.Find(x => x.PrimaryKey.Key == key);
                if (kit != null)
                    kit.RequestSigns = arr;
            }, false);

            await Sql.QueryAsync("SELECT " + SqlTypes.ColumnList(COLUMN_EXT_PK, F.COLUMN_LANGUAGE, F.COLUMN_VALUE) + " FROM `" + TABLE_SIGN_TEXT + "` ORDER BY `" + COLUMN_EXT_PK + "`;",
                null, reader =>
                {
                    int key = reader.GetInt32(0);
                    Kit kit = kits.Find(x => x.PrimaryKey.Key == key);
                    if (kit != null)
                        F.ReadToTranslationList(reader, kit.SignText ??= new TranslationList(kit.Type is KitType.Special or KitType.Loadout ? 1 : 4));
                }, token).ConfigureAwait(false);

            F.ApplyQueriedList(pkRequirements, (key, arr) =>
            {
                Kit kit = kits.Find(x => x.PrimaryKey.Key == key);
                if (kit != null)
                    kit.RequestSigns = arr;
            }, false);

            _kits = kits.ToArray();
            Kits = new ReadOnlyCollection<Kit>(_kits);

            if (_bundles != null)
            {
                // replace existing kit objects in bundles with the new ones

                for (int i = 0; i < _bundles.Length; ++i)
                {
                    EliteBundle? bundle = _bundles[i];
                    if (bundle?.Kits == null)
                        continue;
                    for (int j = 0; j < bundle.Kits.Length; ++i)
                    {
                        Kit? kit = bundle.Kits[j];
                        if (kit == null)
                            continue;
                        int key = kit.PrimaryKey.Key;
                        for (int k = 0; k < kits.Count; ++k)
                        {
                            if (kits[k].PrimaryKey.Key == key)
                            {
                                bundle.Kits[k] = kits[k];
                                break;
                            }
                        }
                    }
                }
            }

            if (!holdStripeReload)
            {
                await StripeEliteKit.BulkAddStripeEliteKits(StripeService, this, token).ConfigureAwait(false);
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public void Dispose()
    {
        if (StripeService is IDisposable disp)
            disp.Dispose();
        Semaphore.Dispose();
    }
}

internal class WarfarePurchaseRecordsInterface : PurchaseRecordsInterface
{
    public override IMySqlDatabase Sql => Data.AdminSql;
    public override IStripeService StripeService => Data.WarfareStripeService;
}