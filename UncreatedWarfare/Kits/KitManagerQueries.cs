using MySqlConnector;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Json;
using Uncreated.SQL;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Kits;
partial class KitManager
{
    // ReSharper disable InconsistentNaming
    public const string TABLE_MAIN = "kits";
    public const string TABLE_ITEMS = "kits_items";
    public const string TABLE_UNLOCK_REQUIREMENTS = "kits_unlock_requirements";
    public const string TABLE_SKILLSETS = "kits_skillsets";
    public const string TABLE_FACTION_FILTER = "kits_faction_filters";
    public const string TABLE_MAP_FILTER = "kits_map_filters";
    public const string TABLE_SIGN_TEXT = "kits_sign_text";
    public const string TABLE_ACCESS = "kits_access";
    public const string TABLE_REQUEST_SIGNS = "kits_request_signs";
    public const string TABLE_FAVORITES = "kits_favorites";
    public const string TABLE_HOTKEYS = "kits_hotkeys";
    public const string TABLE_LAYOUT_TRANSFORMATIONS = "kits_layouts";

    public const string COLUMN_PK = "pk";
    public const string COLUMN_KIT_ID = "Id";
    public const string COLUMN_FACTION = "Faction";
    public const string COLUMN_CLASS = "Class";
    public const string COLUMN_BRANCH = "Branch";
    public const string COLUMN_TYPE = "Type";
    public const string COLUMN_REQUEST_COOLDOWN = "RequestCooldown";
    public const string COLUMN_TEAM_LIMIT = "TeamLimit";
    public const string COLUMN_SEASON = "Season";
    public const string COLUMN_DISABLED = "Disabled";
    public const string COLUMN_WEAPONS = "Weapons";
    public const string COLUMN_COST_CREDITS = "CreditCost";
    public const string COLUMN_COST_PREMIUM = "PremiumCost";
    public const string COLUMN_SQUAD_LEVEL = "SquadLevel";
    public const string COLUMN_REQUIRES_NITRO = "RequiresNitro";
    public const string COLUMN_MAPS_WHITELIST = "MapFilterIsWhitelist";
    public const string COLUMN_FACTIONS_WHITELIST = "FactionFilterIsWhitelist";
    public const string COLUMN_CREATOR = "Creator";
    public const string COLUMN_LAST_EDITOR = "LastEditor";
    public const string COLUMN_CREATION_TIME = "CreatedAt";
    public const string COLUMN_LAST_EDIT_TIME = "LastEditedAt";

    public const string COLUMN_FILTER_FACTION = "Faction";
    public const string COLUMN_FILTER_MAP = "Map";
    public const string COLUMN_REQUEST_SIGN = "RequestSign";

    public const string COLUMN_EXT_PK = "Kit";
    public const string COLUMN_ITEM_GUID = "Item";
    public const string COLUMN_ITEM_X = "X";
    public const string COLUMN_ITEM_Y = "Y";
    public const string COLUMN_ITEM_ROTATION = "Rotation";
    public const string COLUMN_ITEM_PAGE = "Page";
    public const string COLUMN_ITEM_CLOTHING = "ClothingSlot";
    public const string COLUMN_ITEM_REDIRECT = "Redirect";
    public const string COLUMN_ITEM_AMOUNT = "Amount";
    public const string COLUMN_ITEM_METADATA = "Metadata";

    public const string COLUMN_FAVORITE_PLAYER = "Steam64";

    public const string COLUMN_ACCESS_STEAM_64 = "Steam64";
    public const string COLUMN_ACCESS_TYPE = "AccessType";
    public const string COLUMN_ACCESS_DATE_GIVEN = "GivenAt";

    public const string COLUMN_HOTKEY_PLAYER = "Steam64";
    public const string COLUMN_HOTKEY_SLOT = "Slot";

    public const string COLUMN_LAYOUT_PLAYER = "Steam64";
    public const string COLUMN_LAYOUT_OLD_PAGE = "OldPage";
    public const string COLUMN_LAYOUT_NEW_PAGE = "NewPage";
    public const string COLUMN_LAYOUT_OLD_X = "OldX";
    public const string COLUMN_LAYOUT_NEW_X = "NewX";
    public const string COLUMN_LAYOUT_OLD_Y = "OldY";
    public const string COLUMN_LAYOUT_NEW_Y = "NewY";
    public const string COLUMN_LAYOUT_NEW_ROTATION = "NewRotation";
    internal static readonly Schema[] SCHEMAS =
    {
        new Schema(TABLE_MAIN, new Schema.Column[]
        {
            new Schema.Column(COLUMN_PK, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(COLUMN_KIT_ID, SqlTypes.String(KitEx.KitNameMaxCharLimit)),
            new Schema.Column(COLUMN_FACTION, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = FactionInfo.COLUMN_PK,
                ForeignKeyTable = FactionInfo.TABLE_MAIN,
                Nullable = true,
                ForeignKeyDeleteBehavior = ConstraintBehavior.NoAction
            },
            new Schema.Column(COLUMN_CLASS, SqlTypes.Enum(Class.None)),
            new Schema.Column(COLUMN_BRANCH, SqlTypes.Enum(Branch.Default)),
            new Schema.Column(COLUMN_TYPE, SqlTypes.Enum<KitType>()),
            new Schema.Column(COLUMN_REQUEST_COOLDOWN, SqlTypes.FLOAT) { Nullable = true },
            new Schema.Column(COLUMN_COST_CREDITS, SqlTypes.INT) { Nullable = true },
            new Schema.Column(COLUMN_COST_PREMIUM, "double") { Nullable = true },
            new Schema.Column(COLUMN_TEAM_LIMIT, SqlTypes.FLOAT) { Nullable = true },
            new Schema.Column(COLUMN_SEASON, SqlTypes.BYTE) { Nullable = true },
            new Schema.Column(COLUMN_DISABLED, SqlTypes.BOOLEAN) { Nullable = true },
            new Schema.Column(COLUMN_WEAPONS, SqlTypes.String(KitEx.WeaponTextMaxCharLimit)) { Nullable = true },
            new Schema.Column(COLUMN_SQUAD_LEVEL, SqlTypes.Enum<SquadLevel>()) { Nullable = true },
            new Schema.Column(COLUMN_MAPS_WHITELIST, SqlTypes.BOOLEAN) { Nullable = true },
            new Schema.Column(COLUMN_FACTIONS_WHITELIST, SqlTypes.BOOLEAN) { Nullable = true },
            new Schema.Column(COLUMN_REQUIRES_NITRO, SqlTypes.BOOLEAN) { Nullable = true },
            new Schema.Column(COLUMN_CREATOR, SqlTypes.STEAM_64) { Nullable = true },
            new Schema.Column(COLUMN_LAST_EDITOR, SqlTypes.STEAM_64) { Nullable = true },
            new Schema.Column(COLUMN_CREATION_TIME, SqlTypes.DATETIME) { Nullable = true },
            new Schema.Column(COLUMN_LAST_EDIT_TIME, SqlTypes.DATETIME) { Nullable = true },
        }, true, typeof(Kit)),
        new Schema(TABLE_ITEMS, new Schema.Column[]
        {
            new Schema.Column(COLUMN_EXT_PK, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyTable = TABLE_MAIN,
                ForeignKeyColumn = COLUMN_PK
            },
            new Schema.Column(COLUMN_ITEM_GUID, SqlTypes.GUID_STRING) { Nullable = true },
            new Schema.Column(COLUMN_ITEM_X, SqlTypes.BYTE) { Nullable = true },
            new Schema.Column(COLUMN_ITEM_Y, SqlTypes.BYTE) { Nullable = true },
            new Schema.Column(COLUMN_ITEM_ROTATION, SqlTypes.BYTE) { Nullable = true },
            new Schema.Column(COLUMN_ITEM_PAGE, SqlTypes.Enum<Page>()) { Nullable = true },
            new Schema.Column(COLUMN_ITEM_CLOTHING, SqlTypes.Enum<ClothingType>()) { Nullable = true },
            new Schema.Column(COLUMN_ITEM_REDIRECT, SqlTypes.Enum<RedirectType>()) { Nullable = true },
            new Schema.Column(COLUMN_ITEM_AMOUNT, SqlTypes.BYTE) { Nullable = true },
            new Schema.Column(COLUMN_ITEM_METADATA, SqlTypes.Binary(KitEx.MaxStateArrayLimit)) { Nullable = true },
        }, false, typeof(IKitItem)),
        UnlockRequirement.GetDefaultSchema(TABLE_UNLOCK_REQUIREMENTS, COLUMN_EXT_PK, TABLE_MAIN, COLUMN_PK),
        Skillset.GetDefaultSchema(TABLE_SKILLSETS, COLUMN_EXT_PK, TABLE_MAIN, COLUMN_PK),
        F.GetForeignKeyListSchema(TABLE_FACTION_FILTER, COLUMN_EXT_PK, COLUMN_FILTER_FACTION, TABLE_MAIN, COLUMN_PK, FactionInfo.TABLE_MAIN, FactionInfo.COLUMN_PK),
        F.GetForeignKeyListSchema(TABLE_MAP_FILTER, COLUMN_EXT_PK, COLUMN_FILTER_MAP, TABLE_MAIN, COLUMN_PK, null, null),
        F.GetTranslationListSchema(TABLE_SIGN_TEXT, COLUMN_EXT_PK, TABLE_MAIN, COLUMN_PK, KitEx.SignTextMaxCharLimit),
        new Schema(TABLE_ACCESS, new Schema.Column[]
        {
            new Schema.Column(COLUMN_EXT_PK, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyTable = TABLE_MAIN,
                ForeignKeyColumn = COLUMN_PK
            },
            new Schema.Column(COLUMN_ACCESS_STEAM_64, SqlTypes.STEAM_64),
            new Schema.Column(COLUMN_ACCESS_TYPE, SqlTypes.Enum<KitAccessType>()),
            new Schema.Column(COLUMN_ACCESS_DATE_GIVEN, SqlTypes.DATETIME) { Nullable = true },
        }, false, null),
        F.GetForeignKeyListSchema(TABLE_REQUEST_SIGNS, COLUMN_EXT_PK, COLUMN_REQUEST_SIGN, TABLE_MAIN, COLUMN_PK, StructureSaver.TABLE_MAIN, StructureSaver.COLUMN_PK, deleteBehavior: ConstraintBehavior.Cascade, updateBehavior: ConstraintBehavior.Cascade),
        F.GetListSchema<ulong>(TABLE_FAVORITES, COLUMN_EXT_PK, COLUMN_FAVORITE_PLAYER, TABLE_MAIN, COLUMN_PK),
        new Schema(TABLE_HOTKEYS, new Schema.Column[]
        {
            new Schema.Column(COLUMN_HOTKEY_PLAYER, SqlTypes.STEAM_64),
            new Schema.Column(COLUMN_EXT_PK, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyTable = TABLE_MAIN,
                ForeignKeyColumn = COLUMN_PK
            },
            new Schema.Column(COLUMN_HOTKEY_SLOT, SqlTypes.BYTE),
            new Schema.Column(COLUMN_ITEM_PAGE, SqlTypes.Enum<Page>()),
            new Schema.Column(COLUMN_ITEM_X, SqlTypes.BYTE),
            new Schema.Column(COLUMN_ITEM_Y, SqlTypes.BYTE),
            new Schema.Column(COLUMN_ITEM_GUID, SqlTypes.GUID_STRING) { Nullable = true },
            new Schema.Column(COLUMN_ITEM_REDIRECT, SqlTypes.Enum<RedirectType>()) { Nullable = true },
        }, false, typeof(HotkeyBinding)),
        new Schema(TABLE_LAYOUT_TRANSFORMATIONS, new Schema.Column[]
        {
            new Schema.Column(COLUMN_HOTKEY_PLAYER, SqlTypes.STEAM_64),
            new Schema.Column(COLUMN_EXT_PK, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyTable = TABLE_MAIN,
                ForeignKeyColumn = COLUMN_PK
            },
            new Schema.Column(COLUMN_LAYOUT_OLD_PAGE, SqlTypes.Enum<Page>()),
            new Schema.Column(COLUMN_LAYOUT_OLD_X, SqlTypes.BYTE),
            new Schema.Column(COLUMN_LAYOUT_OLD_Y, SqlTypes.BYTE),
            new Schema.Column(COLUMN_LAYOUT_NEW_PAGE, SqlTypes.Enum<Page>()),
            new Schema.Column(COLUMN_LAYOUT_NEW_X, SqlTypes.BYTE),
            new Schema.Column(COLUMN_LAYOUT_NEW_Y, SqlTypes.BYTE),
            new Schema.Column(COLUMN_LAYOUT_NEW_ROTATION, SqlTypes.BYTE)
        }, false, typeof(ItemTransformation))
    };

    public static async Task DownloadPlayersKitData(IEnumerable<UCPlayer> playerList, bool lockPurchaseSync,
        CancellationToken token = default)
    {
        UCPlayer[] players = playerList.ToArrayFast(true);
        if (players.Length == 0)
            return;
        for (int i = 0; i < players.Length; ++i)
        {
            UCPlayer player = players[i];
            if (player.IsDownloadingKitData && !player.HasDownloadedKitData)
            {
                L.LogDebug("Spin-waiting for player kit data for " + player + "...");
                UCWarfare.SpinWaitUntil(() => player.HasDownloadedKitData, 2500);
                return;
            }
            
            player.IsDownloadingKitData = true;
        }

        List<KeyValuePair<ulong, HotkeyBinding>>? bindingsToDelete = null;
        List<KeyValuePair<ulong, LayoutTransformation>>? layoutsToDelete = null;


        if (lockPurchaseSync)
        {
            Task[] tasks = new Task[players.Length];
            for (int i = 0; i < players.Length; ++i)
            {
                UCPlayer pl = players[i];
                CancellationToken token2 = token;
                token2.CombineIfNeeded(pl.DisconnectToken);
                tasks[i] = pl.PurchaseSync.WaitAsync(token2);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        try
        {
            StringBuilder playerInList = new StringBuilder(players.Length * 32);

            object[] args = new object[players.Length];
            for (int i = 0; i < players.Length; ++i)
            {
                if (i != 0) playerInList.Append(',');
                playerInList.Append('@').Append(i.ToString(Data.AdminLocale));
                args[i] = players[i].Steam64;
            }

            playerInList.Append(");");
            string inList = playerInList.ToString();
            List<int>?[] kitOutput = new List<int>[players.Length];
            List<HotkeyBinding>?[] bindings = new List<HotkeyBinding>[players.Length];
            List<LayoutTransformation>?[] layouts = new List<LayoutTransformation>[players.Length];

            await Data.AdminSql.QueryAsync("SELECT `" + COLUMN_EXT_PK + "`,`" + COLUMN_ACCESS_STEAM_64 + "` FROM `" +
                                           TABLE_ACCESS +
                                           "` WHERE `" + COLUMN_ACCESS_STEAM_64 + "` IN (" + inList, args,
                reader =>
                {
                    ulong s64 = reader.GetUInt64(1);
                    int k = reader.GetInt32(0);
                    int index = Array.FindIndex(players, x => x.Steam64 == s64);
                    if (index != -1 && new PrimaryKey(k).IsValid)
                    {
                        ref List<int>? list = ref kitOutput[index];
                        (list ??= new List<int>(16)).Add(k);
                    }
                }, token).ConfigureAwait(false);
            await Data.AdminSql.QueryAsync(F.BuildSelectWhereIn(TABLE_HOTKEYS, COLUMN_HOTKEY_PLAYER,
                    COLUMN_EXT_PK, COLUMN_HOTKEY_SLOT,
                    COLUMN_ITEM_PAGE, COLUMN_ITEM_X, COLUMN_ITEM_Y,
                    COLUMN_ITEM_GUID, COLUMN_ITEM_REDIRECT, COLUMN_HOTKEY_PLAYER) + inList, args,
                reader =>
                {
                    ulong s64 = reader.GetUInt64(7);
                    int index = Array.FindIndex(players, x => x.Steam64 == s64);
                    if (index != -1)
                    {
                        UCPlayer player = players[index];
                        ref List<HotkeyBinding>? list = ref bindings[index];
                        PrimaryKey kit = reader.GetInt32(0);
                        bool del = false;
                        if (!kit.IsValid)
                        {
                            L.LogWarning("Invalid kit primary key (" + kit + ") in player " + player + "'s hotkeys.");
                            del = true;
                        }

                        byte slot = reader.GetByte(1);
                        if (!KitEx.ValidSlot(slot))
                        {
                            L.LogWarning("Invalid kit slot (" + slot + ") in player " + player + "'s hotkeys.");
                            del = true;
                        }

                        IItemJar jar;
                        byte x = reader.GetByte(3), y = reader.GetByte(4);
                        Page? page = reader.ReadStringEnum<Page>(2);
                        if (!page.HasValue)
                        {
                            L.LogWarning("Failed to read page from player " + player + "'s hotkeys.");
                            del = true;
                        }

                        Guid? guid = reader.IsDBNull(5) ? null : reader.ReadGuidString(5);
                        if (guid.HasValue)
                        {
                            jar = new PageItem(guid.Value, x, y, 0, Array.Empty<byte>(), 1,
                                page ?? (Page)byte.MaxValue);
                        }
                        else
                        {
                            RedirectType? redir = reader.IsDBNull(6) ? null : reader.ReadStringEnum<RedirectType>(6);
                            if (!redir.HasValue)
                            {
                                L.LogWarning("Failed to read redirect type and GUID from player " + player +
                                             "'s hotkeys.");
                                del = true;
                                jar = new AssetRedirectItem(RedirectType.None, x, y, 0, (Page)byte.MaxValue);
                            }
                            else
                            {
                                jar = new AssetRedirectItem(redir.Value, x, y, 0, page ?? (Page)byte.MaxValue);
                            }
                        }

                        HotkeyBinding b = new HotkeyBinding(kit, slot, jar);

                        if (!del)
                            (list ??= new List<HotkeyBinding>(4)).Add(b);
                        else
                            (bindingsToDelete ??= new List<KeyValuePair<ulong, HotkeyBinding>>()).Add(
                                new KeyValuePair<ulong, HotkeyBinding>(player.Steam64, b));
                    }
                }, token).ConfigureAwait(false);
            await Data.AdminSql.QueryAsync(F.BuildSelectWhereIn(TABLE_LAYOUT_TRANSFORMATIONS, COLUMN_LAYOUT_PLAYER,
                                               COLUMN_EXT_PK, COLUMN_LAYOUT_OLD_PAGE,
                                               COLUMN_LAYOUT_NEW_PAGE, COLUMN_LAYOUT_OLD_X, COLUMN_LAYOUT_NEW_X,
                                               COLUMN_LAYOUT_OLD_Y, COLUMN_LAYOUT_NEW_Y, COLUMN_LAYOUT_NEW_ROTATION,
                                               COLUMN_LAYOUT_PLAYER) +
                                           inList, args,
                reader =>
                {
                    ulong s64 = reader.GetUInt64(8);
                    int index = Array.FindIndex(players, x => x.Steam64 == s64);
                    if (index != -1)
                    {
                        UCPlayer player = players[index];
                        ref List<LayoutTransformation>? list = ref layouts[index];
                        PrimaryKey kit = reader.GetInt32(0);
                        bool del = false;
                        if (!kit.IsValid)
                        {
                            L.LogWarning("Invalid kit primary key (" + kit + ") in player " + player + "'s hotkeys.");
                            del = true;
                        }

                        Page? oldPage = reader.ReadStringEnum<Page>(1), newPage = reader.ReadStringEnum<Page>(2);
                        byte oldX = reader.GetByte(3), newX = reader.GetByte(4);
                        byte oldY = reader.GetByte(5), newY = reader.GetByte(6);
                        byte newRot = reader.GetByte(7);
                        if (!oldPage.HasValue || !newPage.HasValue)
                        {
                            L.LogWarning("Failed to read old or new page from player " + player + "'s layout data.");
                            del = true;
                        }

                        if (newRot > 3)
                        {
                            L.LogWarning("Invalid rotation " + newRot + " in " + player + "'s layout data.");
                            del = true;
                        }

                        LayoutTransformation l = new LayoutTransformation(oldPage ?? (Page)byte.MaxValue,
                            newPage ?? (Page)byte.MaxValue, oldX, oldY, newX, newY, newRot, kit);

                        if (!del)
                            (list ??= new List<LayoutTransformation>(4)).Add(l);
                        else
                            (layoutsToDelete ??= new List<KeyValuePair<ulong, LayoutTransformation>>()).Add(
                                new KeyValuePair<ulong, LayoutTransformation>(player.Steam64, l));
                    }
                }, token).ConfigureAwait(false);



            KitManager? singleton = GetSingletonQuick();
            if (singleton == null)
                throw new SingletonUnloadedException(typeof(KitManager));

            for (int i = 0; i < players.Length; ++i)
                players[i].AccessibleKits = new List<SqlItem<Kit>>(kitOutput[i] is { } l1 ? l1.Count : 0);

            await singleton.WaitAsync(token).ConfigureAwait(false);
            try
            {
                for (int p = 0; p < players.Length; ++p)
                {
                    UCPlayer player = players[p];
                    if (!player.IsOnline)
                        continue;
                    List<int>? kits = kitOutput[p];
                    if (kits != null)
                    {
                        for (int i = 0; i < kits.Count; ++i)
                        {
                            SqlItem<Kit>? proxy = singleton.FindProxyNoLock(kits[i]);
                            if (proxy is not null)
                            {
                                (player.AccessibleKits ??= new List<SqlItem<Kit>>()).Add(proxy);
                            }
                        }
                    }

                    List<LayoutTransformation>? layouts2 = layouts[p];
                    if (layouts2 is { Count: > 0 })
                    {
                        for (int i = 0; i < layouts2.Count; ++i)
                        {
                            LayoutTransformation l = layouts2[i];
                            SqlItem<Kit>? proxy = singleton.FindProxyNoLock(l.Kit);
                            Kit? kit = proxy?.Item;
                            if (kit is null)
                            {
                                L.LogWarning("Kit for " + player + "'s layout transformation (" + l.Kit + ") not found.");
                                goto del;
                            }

                            // find matching item
                            IKitItem? item = kit.Items?.FirstOrDefault(x =>
                                x is IItemJar jar && jar.Page == l.OldPage && jar.X == l.OldX && jar.Y == l.OldY);
                            if (item == null)
                            {
                                L.LogWarning(player + "'s layout transformation for kit " + l.Kit +
                                             " has an invalid item position: " + l.OldPage + ", (" + l.OldX + ", " +
                                             l.OldY + ").");
                                goto del;
                            }

                            continue;
                            del:
                            (layoutsToDelete ??= new List<KeyValuePair<ulong, LayoutTransformation>>()).Add(
                                new KeyValuePair<ulong, LayoutTransformation>(player.Steam64, l));
                            layouts2.RemoveAtFast(i);
                            --i;
                        }
                    }

                    List<HotkeyBinding>? bindings2 = bindings[p];
                    if (bindings2 is { Count: > 0 })
                    {
                        for (int i = 0; i < bindings2.Count; ++i)
                        {
                            HotkeyBinding b = bindings2[i];
                            SqlItem<Kit>? proxy = singleton.FindProxyNoLock(b.Kit);
                            Kit? kit = proxy?.Item;
                            if (kit is null)
                            {
                                L.LogWarning("Kit for " + player + "'s hotkey (" + b.Kit + ") not found.");
                                goto del;
                            }

                            // find matching item
                            IKitItem? item = kit.Items?.FirstOrDefault(x =>
                                x is IItemJar jar && jar.Page == b.Item.Page && jar.X == b.Item.X &&
                                jar.Y == b.Item.Y &&
                                (jar is IItem i1 && b.Item is IItem i2 && i1.Item == i2.Item ||
                                 jar is IAssetRedirect r1 &&
                                 b.Item is IAssetRedirect r2 && r1.RedirectType == r2.RedirectType));
                            if (item == null)
                            {
                                L.LogWarning(player + "'s hotkey for kit " + b.Kit + " has an invalid item position: " +
                                             b.Item + ".");
                                goto del;
                            }

                            continue;
                            del:
                            (bindingsToDelete ??= new List<KeyValuePair<ulong, HotkeyBinding>>()).Add(
                                new KeyValuePair<ulong, HotkeyBinding>(player.Steam64, b));
                            bindings2.RemoveAtFast(i);
                            --i;
                        }
                    }

                    player.HotkeyBindings = bindings2;
                    player.LayoutTransformations = layouts2;
                }
            }
            finally
            {
                singleton.Release();
            }
        }
        finally
        {
            for (int i = 0; i < players.Length; ++i)
            {
                UCPlayer player = players[i];
                player.HasDownloadedKitData = true;
                player.IsDownloadingKitData = false;
                if (lockPurchaseSync)
                    player.PurchaseSync.Release();
            }
        }

        if (bindingsToDelete is { Count: > 0 } || layoutsToDelete is { Count: > 0 })
        {
            int bct = bindingsToDelete?.Count ?? 0;
            int lct = layoutsToDelete?.Count ?? 0;
            StringBuilder sb = new StringBuilder((bct + lct) * 32);
            const int blen = 3;
            const int llen = 5;
            object[] args = new object[bct * blen + lct * llen];
            for (int i = 0; i < bct; ++i)
            {
                KeyValuePair<ulong, HotkeyBinding> b = bindingsToDelete![i];
                int st = i * blen;
                args[st] = b.Key;
                HotkeyBinding t = b.Value;
                args[st + 1] = t.Slot;
                args[st + 2] = t.Kit.Key;
                sb.Append($"DELETE FROM `{TABLE_HOTKEYS}` " +
                          $"WHERE `{COLUMN_HOTKEY_PLAYER}`={st} " +
                          $"AND `{COLUMN_HOTKEY_SLOT}`=@{st + 1} " +
                          $"AND `{COLUMN_EXT_PK}`=@{st + 2}; ");
            }

            for (int i = 0; i < lct; ++i)
            {
                KeyValuePair<ulong, LayoutTransformation> l = layoutsToDelete![i];
                int st = bct * blen + i * llen;
                args[st] = l.Key;
                LayoutTransformation t = l.Value;
                args[st + 1] = t.OldPage.ToString();
                args[st + 2] = t.OldX;
                args[st + 3] = t.OldY;
                args[st + 4] = t.Kit.Key;
                sb.Append($"DELETE FROM `{TABLE_LAYOUT_TRANSFORMATIONS}` " +
                          $"WHERE `{COLUMN_LAYOUT_PLAYER}`=@{st} " +
                          $"AND `{COLUMN_LAYOUT_OLD_PAGE}`=@{st + 1} " +
                          $"AND `{COLUMN_LAYOUT_OLD_X}`=@{st + 2} " +
                          $"AND `{COLUMN_LAYOUT_OLD_Y}`=@{st + 3} " +
                          $"AND `{COLUMN_EXT_PK}`=@{st + 4}; ");
            }

            UCWarfare.RunTask(async (q, args, token) => await Data.AdminSql.NonQueryAsync(q, args, token).ConfigureAwait(false),
                sb.ToString(), args, token, ctx: "Delete invalid hotkeys and/or layout transformations for " + players.Length + " player(s).");
        }

        await UCWarfare.ToUpdate(token);
        Signs.UpdateKitSigns(null, null);
    }

    public static Task DownloadPlayerKitData(UCPlayer player, bool lockPurchaseSync, CancellationToken token = default) =>
        DownloadPlayersKitData(new UCPlayer[] { player }, lockPurchaseSync, token);

    private static async Task<bool> AddAccessRow(PrimaryKey kit, ulong player, KitAccessType type, CancellationToken token = default)
    {
        return await Data.AdminSql.NonQueryAsync(
            $"INSERT INTO `{TABLE_ACCESS}` ({SqlTypes.ColumnList(COLUMN_EXT_PK, COLUMN_ACCESS_STEAM_64, COLUMN_ACCESS_TYPE, COLUMN_ACCESS_DATE_GIVEN)}) " +
             "VALUES (@0, @1, @2, @3);", new object[] { kit.Key, player, type.ToString(), DateTime.UtcNow }, token).ConfigureAwait(false) > 0;
    }
    /// <remarks>Thread Safe</remarks>
    public static async Task<bool> HasAccess(PrimaryKey kit, ulong player, CancellationToken token = default)
    {
        if (!kit.IsValid)
            return false;
        int ct = 0;
        await Data.DatabaseManager.QueryAsync($"SELECT COUNT(`{COLUMN_ACCESS_STEAM_64}`) FROM `{TABLE_ACCESS}` " +
                                              $"WHERE `{COLUMN_EXT_PK}`=@0 AND `{COLUMN_ACCESS_STEAM_64}`=@1 LIMIT 1;", new object[]
        {
            kit.Key, player
        }, reader => ct = reader.GetInt32(0), token).ConfigureAwait(false);
        return ct > 0;
    }
    private static async Task<bool> AddAccessRow(string kit, ulong player, KitAccessType type, CancellationToken token = default)
    {
        PrimaryKey pk = PrimaryKey.NotAssigned;
        await Data.AdminSql.QueryAsync($"SELECT `{COLUMN_PK}` FROM `{TABLE_MAIN}` WHERE `{COLUMN_KIT_ID}`=@0;",
            new object[] { kit },
            reader =>
            {
                pk = reader.GetInt32(0);
            }, token).ConfigureAwait(false);
        if (pk.IsValid)
            return await AddAccessRow(pk, player, type, token).ConfigureAwait(false);
        return false;
    }
    private static async Task<bool> RemoveAccessRow(PrimaryKey kit, ulong player, CancellationToken token = default)
    {
        return await Data.AdminSql.NonQueryAsync(
            $"DELETE FROM `{TABLE_ACCESS}` WHERE `{COLUMN_EXT_PK}`=@0 AND `{COLUMN_ACCESS_STEAM_64}`=@1;",
            new object[] { kit.Key, player }, token).ConfigureAwait(false) > 0;
    }
    private static async Task<bool> RemoveAccessRow(string kit, ulong player, CancellationToken token = default)
    {
        PrimaryKey pk = PrimaryKey.NotAssigned;
        await Data.AdminSql.QueryAsync($"SELECT `{COLUMN_PK}` FROM `{TABLE_MAIN}` WHERE `{COLUMN_KIT_ID}`=@0;",
            new object[] { kit },
            reader =>
            {
                pk = reader.GetInt32(0);
            }, token).ConfigureAwait(false);
        if (pk.IsValid)
            return await RemoveAccessRow(pk, player, token).ConfigureAwait(false);
        return false;
    }

    /// <remarks>Thread Safe</remarks>
    public static async Task<bool> RemoveHotkey(PrimaryKey kit, ulong player, byte slot, CancellationToken token = default)
    {
        if (!KitEx.ValidSlot(slot))
            throw new ArgumentException("Invalid slot number.", nameof(slot));
        int ct = await Data.AdminSql.NonQueryAsync($"DELETE FROM `{TABLE_HOTKEYS}` " +
                                                   $"WHERE `{COLUMN_HOTKEY_PLAYER}`=@0 " +
                                                   $"AND `{COLUMN_HOTKEY_SLOT}`=@1 " +
                                                   $"AND `{COLUMN_EXT_PK}`=@2;",
            new object[] { player, slot, kit.Key }, token);
        return ct > 0;
    }
    /// <remarks>Thread Safe</remarks>
    public static async Task<bool> RemoveHotkey(PrimaryKey kit, ulong player, byte x, byte y, Page page, CancellationToken token = default)
    {
        int ct = await Data.AdminSql.NonQueryAsync($"DELETE FROM `{TABLE_HOTKEYS}` " +
                                                   $"WHERE `{COLUMN_HOTKEY_PLAYER}`=@0 " +
                                                   $"AND `{COLUMN_EXT_PK}`=@1 " +
                                                   $"AND `{COLUMN_ITEM_X}`=@2 " +
                                                   $"AND `{COLUMN_ITEM_Y}`=@3 " +
                                                   $"AND `{COLUMN_ITEM_PAGE}`=@4;",
            new object[] { player, kit.Key, x, y, page.ToString() }, token);
        return ct > 0;
    }
    /// <remarks>Thread Safe</remarks>
    public static async Task<bool> AddHotkey(PrimaryKey kit, ulong player, byte slot, IItemJar item, CancellationToken token = default)
    {
        if (item is not IItem and not IAssetRedirect)
            throw new ArgumentException("Item must also implement IItem or IAssetRedirect.", nameof(item));
        if (!KitEx.ValidSlot(slot))
            throw new ArgumentException("Invalid slot number.", nameof(slot));

        string q = $"DELETE FROM `{TABLE_HOTKEYS}` " +
                   $"WHERE `{COLUMN_HOTKEY_PLAYER}`=@0 " +
                   $"AND `{COLUMN_EXT_PK}`=@1 " +
                   $"AND (`{COLUMN_HOTKEY_SLOT}`=@2 OR (`{COLUMN_ITEM_X}`=@3 AND `{COLUMN_ITEM_Y}`=@4 AND `{COLUMN_ITEM_PAGE}`=@5));" +
                   $"INSERT INTO `{TABLE_HOTKEYS}` ({SqlTypes.ColumnList(COLUMN_HOTKEY_PLAYER, COLUMN_EXT_PK, COLUMN_HOTKEY_SLOT,
                       COLUMN_ITEM_X, COLUMN_ITEM_Y, COLUMN_ITEM_PAGE, COLUMN_ITEM_GUID,
                       COLUMN_ITEM_REDIRECT)}) VALUES (@0, @1, @2, @3, @4, @5, @6, @7);";

        int ct = await Data.AdminSql.NonQueryAsync(q,
            new object[]
            {
                player,
                kit.Key,
                slot,
                item.X,
                item.Y,
                item.Page.ToString(),
                item is IItem item2 ? item2.Item.ToString("N") : DBNull.Value,
                item is IAssetRedirect redir ? redir.RedirectType.ToString() : DBNull.Value
            }, token).ConfigureAwait(false);
        return ct > 0;
    }
    internal async Task SaveFavorites(UCPlayer player, List<PrimaryKey> favoriteKits, CancellationToken token = default)
    {
        token.CombineIfNeeded(UCWarfare.UnloadCancel);
        object[] args = new object[favoriteKits.Count + 1];
        args[0] = player.Steam64;
        StringBuilder sb = new StringBuilder("(");
        for (int i = 0; i < favoriteKits.Count; ++i)
        {
            args[i + 1] = favoriteKits[i].Key;
            if (i != 0)
                sb.Append("),(");
            sb.Append("@0,@").Append(i + 1);
        }

        sb.Append(");");

        if (args.Length <= 1)
        {
            await Sql.NonQueryAsync($"DELETE FROM `{TABLE_FAVORITES}` WHERE `{COLUMN_FAVORITE_PLAYER}`=@0;", args, token).ConfigureAwait(false);
            return;
        }
        await Sql.NonQueryAsync($"DELETE FROM `{TABLE_FAVORITES}` WHERE `{COLUMN_FAVORITE_PLAYER}`=@0;" +
                                F.StartBuildOtherInsertQueryNoUpdate(TABLE_FAVORITES,
                                    COLUMN_FAVORITE_PLAYER, COLUMN_EXT_PK) + sb, args, token).ConfigureAwait(false);
        player.KitMenuData.FavoritesDirty = false;
    }
    public static async Task ResetLayout(UCPlayer player, PrimaryKey kit, bool lockPurchaseSync, CancellationToken token = default)
    {
        if (lockPurchaseSync)
            await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        try
        {
            player.LayoutTransformations?.RemoveAll(x => x.Kit.Key == kit.Key);
            await Data.AdminSql.NonQueryAsync($"DELETE FROM `{TABLE_LAYOUT_TRANSFORMATIONS}` WHERE `{COLUMN_HOTKEY_PLAYER}`=@0 AND `{COLUMN_EXT_PK}`=@1;",
                new object[] { player.Steam64, kit.Key }, token).ConfigureAwait(false);
        }
        finally
        {
            if (lockPurchaseSync)
                player.PurchaseSync.Release();
        }
    }
    public static async Task SaveLayout(UCPlayer player, SqlItem<Kit> proxy, bool lockPurchaseSync, bool lockKit, CancellationToken token = default)
    {
        await UCWarfare.ToUpdate(token);
        List<LayoutTransformation> active = GetLayoutTransformations(player, proxy.LastPrimaryKey);
        List<(Page Page, Item Item, byte X, byte Y, byte Rotation, byte SizeX, byte SizeY)> inventory = new List<(Page, Item, byte, byte, byte, byte, byte)>(24);
        for (int pageIndex = 0; pageIndex < PlayerInventory.STORAGE; ++pageIndex)
        {
            Items page = player.Player.inventory.items[pageIndex];
            int c = page.getItemCount();
            for (int index = 0; index < c; ++index)
            {
                ItemJar jar = page.getItem((byte)index);
                if (jar.item == null)
                    continue;
                inventory.Add(((Page)pageIndex, jar.item, jar.x, jar.y, jar.rot, jar.size_x, jar.size_y));
            }
        }

        // ensure validity of 'active', remove non-kit items, try to add missing items
        List<IItemJar> items = new List<IItemJar>(active.Count);
        if (lockPurchaseSync)
            await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (lockKit)
                await proxy.Enter(token).ConfigureAwait(false);
            try
            {
                if (proxy.Item is not { Items: { } kitItems } kit)
                    return;
                for (int i = 0; i < active.Count; ++i)
                {
                    LayoutTransformation t = active[i];
                    IItemJar? original = (IItemJar?)kitItems.FirstOrDefault(x => x is IItemJar jar && jar.X == t.OldX && jar.Y == t.OldY && jar.Page == t.OldPage);
                    if (original == null)
                    {
                        // item is not a part of the kit
                        active.RemoveAtFast(i--);
                        continue;
                    }
                    if (items.Contains(original))
                    {
                        L.LogWarning("Duplicate layout transformation for item " + original + ", skipping.");
                        active.RemoveAtFast(i--);
                        continue;
                    }
                    if (original.X == t.NewX && original.Y == t.NewY && original.Page == t.NewPage && original.Rotation == t.NewRotation)
                    {
                        L.LogWarning("Identity layout transformation for item " + original + ", skipping.");
                        active.RemoveAtFast(i--);
                        continue;
                    }
                    items.Add(original);
                    L.LogDebug($"Found active: {t.OldPage} -> {t.NewPage}, ({t.OldX} -> {t.NewX}, {t.OldY} -> {t.NewY}) new rot: {t.NewRotation}.");
                }

                // check for missing items
                foreach (IItemJar jar in kitItems.OfType<IItemJar>().Where(x => !items.Contains(x)))
                {
                    L.LogDebug("Missing item " + jar + ", trying to fit somewhere.");
                    byte sizeX1, sizeY1;
                    ItemAsset? asset;
                    if (jar is IItem itemSpec)
                    {
                        if (Assets.find<ItemAsset>(itemSpec.Item) is { } ia)
                        {
                            sizeX1 = ia.size_x;
                            sizeY1 = ia.size_y;
                            asset = ia;
                        }
                        else continue;
                    }
                    else if (((IKitItem)jar).GetItem(kit, TeamManager.GetFactionSafe(player.GetTeam()), out _, out _) is { } ia)
                    {
                        sizeX1 = ia.size_x;
                        sizeY1 = ia.size_y;
                        asset = ia;
                    }
                    else continue;
                    (Page Page, Item Item, byte X, byte Y, byte Rotation, byte SizeX, byte SizeY)? colliding = null;
                    foreach ((Page Page, Item Item, byte X, byte Y, byte Rotation, byte SizeX, byte SizeY) item in inventory)
                    {
                        if (item.Page != jar.Page) continue;
                        if (UCInventoryManager.IsOverlapping(jar.X, jar.Y, sizeX1, sizeY1, item.X, item.Y, item.SizeX, item.SizeY, jar.Rotation, item.Rotation))
                        {
                            L.LogDebug("Found colliding item: " + item.Page + ", (" + item.X + ", " + item.Y + ").");
                            colliding = item;
                            break;
                        }
                    }

                    if (!colliding.HasValue || colliding.Value.X == jar.X && colliding.Value.Y == jar.Y && colliding.Value.Rotation == jar.Rotation
                        && colliding.Value.Item.GetAsset() is { } ia2 && ia2.GUID == asset.GUID)
                    {
                        L.LogDebug("Found no collisions (or the collision was the same item).");
                        continue;
                    }
                    byte origx, origy;
                    Page origPage;
                    ItemTransformation t = player.ItemTransformations.FirstOrDefault(x => x.Item == colliding.Value.Item);
                    if (t.Item == null)
                    {
                        ItemDropTransformation t2 = player.ItemDropTransformations.FirstOrDefault(x => x.Item == colliding.Value.Item);
                        if (t.Item == null)
                        {
                            L.LogDebug("Unable to find transformations for original item blocking " + jar + ".");
                            continue;
                        }
                        origx = t2.OldX;
                        origy = t2.OldY;
                        origPage = t2.OldPage;
                    }
                    else
                    {
                        origx = t.OldX;
                        origy = t.OldY;
                        origPage = t.OldPage;
                    }
                    IItemJar? orig = (IItemJar?)kitItems.FirstOrDefault(x => x is IItemJar jar && jar.X == origx && jar.Y == origy && jar.Page == origPage);
                    if (orig == null)
                    {
                        L.LogDebug("Unable to find original item blocking " + jar + ".");
                        continue;
                    }
                    if (colliding.Value.SizeX == sizeX1 && colliding.Value.SizeY == sizeY1)
                    {
                        active.Add(new LayoutTransformation(jar.Page, orig.Page, jar.X, jar.Y, orig.X, orig.Y, orig.Rotation, proxy.LastPrimaryKey));
                        L.LogDebug("Moved item to original position of other item: " + jar + " -> " + orig + ".");
                    }
                    else if (colliding.Value.SizeX == sizeY1 && colliding.Value.SizeY == sizeX1)
                    {
                        active.Add(new LayoutTransformation(jar.Page, orig.Page, jar.X, jar.Y, orig.X, orig.Y, (byte)(orig.Rotation + 1 % 4), proxy.LastPrimaryKey));
                        L.LogDebug("Moved item to original position of other item (rotated 90 degrees): " + jar + " -> " + orig + ".");
                    }
                }


                StringBuilder sb = new StringBuilder($"DELETE FROM `{TABLE_LAYOUT_TRANSFORMATIONS}` " +
                           $"WHERE `{COLUMN_HOTKEY_PLAYER}`=@0 " +
                           $"AND `{COLUMN_EXT_PK}`=@1;");
                if (active.Count == 0)
                {
                    await Data.AdminSql.NonQueryAsync(sb.ToString(), new object[] { player.Steam64, proxy.LastPrimaryKey.Key }, token).ConfigureAwait(false);
                    return;
                }

                sb.Append($"INSERT INTO `{TABLE_LAYOUT_TRANSFORMATIONS}` ({SqlTypes.ColumnList(COLUMN_LAYOUT_PLAYER, COLUMN_EXT_PK, COLUMN_LAYOUT_OLD_PAGE, COLUMN_LAYOUT_OLD_X, COLUMN_LAYOUT_OLD_Y,
                        COLUMN_LAYOUT_NEW_PAGE, COLUMN_LAYOUT_NEW_X, COLUMN_LAYOUT_NEW_Y, COLUMN_LAYOUT_NEW_ROTATION)}) VALUES ");
                const int len = 7;
                const int clamp = 2;
                object[] objs = new object[clamp + len * active.Count];
                objs[0] = player.Steam64;
                objs[1] = proxy.LastPrimaryKey.Key;
                player.LayoutTransformations?.RemoveAll(x => x.Kit.Key == kit.PrimaryKey.Key);
                for (int i = 0; i < active.Count; ++i)
                {
                    LayoutTransformation a = active[i];
                    (player.LayoutTransformations ??= new List<LayoutTransformation>()).Add(a);
                    int startIndex = clamp + i * len;
                    F.AppendPropertyList(sb, startIndex, len, i, clamp);
                    objs[startIndex] = a.OldPage.ToString();
                    objs[startIndex + 1] = a.OldX;
                    objs[startIndex + 2] = a.OldY;
                    objs[startIndex + 3] = a.NewPage.ToString();
                    objs[startIndex + 4] = a.NewX;
                    objs[startIndex + 5] = a.NewY;
                    objs[startIndex + 6] = a.NewRotation;
                }
                await Data.AdminSql.NonQueryAsync(sb.ToString(), objs, token).ConfigureAwait(false);
            }
            finally
            {
                if (lockKit)
                    proxy.Release();
            }
        }
        finally
        {
            if (lockPurchaseSync)
                player.PurchaseSync.Release();
        }
    }

    internal static async Task OnItemsChangedLayoutHandler(IKitItem[] oldItems, Kit kit, CancellationToken token = default)
    {
        await UCWarfare.ToUpdate(token);
        if (!kit.PrimaryKey.IsValid)
            return;
        IKitItem[] newItems = kit.Items;
        List<IItemJar> changed = new List<IItemJar>(newItems.Length / 2);
        for (int i = 0; i < oldItems.Length; ++i)
        {
            IKitItem old = oldItems[i];

            if (old is IItemJar jar)
            {
                for (int k = 0; k < newItems.Length; ++k)
                {
                    if (old.Equals(newItems[k]))
                        goto c;
                }
                changed.Add(jar);
            }
            c: ;
        }

        if (changed.Count == 0)
            return;
        const string q = $"DELETE FROM `{TABLE_LAYOUT_TRANSFORMATIONS}` WHERE `{COLUMN_EXT_PK}`=@0 AND ";
        StringBuilder sb = new StringBuilder(changed.Count * 48);
        const int len = 3;
        object[] objs = new object[1 + changed.Count * len];
        objs[0] = kit.PrimaryKey.Key;
        for (int i = 0; i < changed.Count; ++i)
        {
            int stInd = 1 + len * i;
            sb.Append(q + $"`{COLUMN_LAYOUT_OLD_PAGE}`=@{stInd.ToString(Data.AdminLocale)} " +
                      $"AND `{COLUMN_LAYOUT_OLD_X}`=@{(stInd + 1).ToString(Data.AdminLocale)} " +
                      $"AND `{COLUMN_LAYOUT_OLD_Y}`=@{(stInd + 2).ToString(Data.AdminLocale)};");
            IItemJar jar = changed[i];
            objs[stInd] = jar.Page.ToString();
            objs[stInd + 1] = jar.X;
            objs[stInd + 2] = jar.Y;
            List<UCPlayer> pl = PlayerManager.OnlinePlayers.ToList();
            for (int p = 0; p < pl.Count; ++p)
            {
                UCPlayer player = pl[p];
                await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    List<LayoutTransformation>? cache = player.LayoutTransformations;
                    if (cache == null)
                        continue;
                    for (int c = 0; c < cache.Count; ++c)
                    {
                        LayoutTransformation lt = cache[c];
                        if (lt.Kit.Key == kit.PrimaryKey.Key && lt.OldPage == jar.Page && lt.OldX == jar.X && lt.OldY == jar.Y)
                            cache.RemoveAtFast(c--);
                    }
                }
                finally
                {
                    player.PurchaseSync.Release();
                }
            }
        }

        await Data.AdminSql.NonQueryAsync(sb.ToString(), objs, token).ConfigureAwait(false);
    }

    // ReSharper restore InconsistentNaming
    [Obsolete]
    protected override async Task AddOrUpdateItem(Kit? item, PrimaryKey pk, CancellationToken token = default)
    {
        if (item == null)
        {
            if (!pk.IsValid)
                throw new ArgumentException("If item is null, pk must have a value to delete the item.", nameof(pk));
            await Sql.NonQueryAsync($"DELETE FROM `{TABLE_MAIN}` WHERE `{COLUMN_PK}`=@0;", new object[] { pk.Key }, token).ConfigureAwait(false);
            return;
        }
        bool hasPk = pk.IsValid;
        int pk2 = PrimaryKey.NotAssigned;
        object[] objs = new object[hasPk ? 21 : 20];
        objs[0] = item.Id ??= (hasPk ? pk.ToString() : "invalid_" + unchecked((uint)DateTime.UtcNow.Ticks));
        objs[1] = item.FactionKey.IsValid && item.FactionKey.Key != 0 ? item.FactionKey.Key : DBNull.Value;
        objs[2] = item.Class.ToString();
        objs[3] = item.Branch.ToString();
        objs[4] = item.Type.ToString();
        objs[5] = item.RequestCooldown <= 0f ? DBNull.Value : item.RequestCooldown;
        objs[6] = item.TeamLimit;
        objs[7] = item.Season;
        objs[8] = item.Disabled;
        objs[9] = (object?)item.WeaponText ?? DBNull.Value;
        objs[10] = item.SquadLevel <= SquadLevel.Member ? DBNull.Value : item.SquadLevel.ToString();
        objs[11] = item.CreditCost == 0 ? DBNull.Value : item.CreditCost;
        objs[12] = item.Type is KitType.Public or KitType.Special ? DBNull.Value : item.PremiumCost;
        objs[13] = item.RequiresNitro;
        objs[14] = item.MapFilterIsWhitelist;
        objs[15] = item.FactionFilterIsWhitelist;
        objs[16] = item.Creator;
        objs[17] = item.LastEditor;
        objs[18] = item.CreatedTimestamp.UtcDateTime;
        objs[19] = item.LastEditedTimestamp.UtcDateTime;
        if (hasPk)
            objs[20] = pk.Key;
        await Sql.QueryAsync(F.BuildInitialInsertQuery(TABLE_MAIN, COLUMN_PK, hasPk, null, null,
                COLUMN_KIT_ID, COLUMN_FACTION, COLUMN_CLASS, COLUMN_BRANCH,
                COLUMN_TYPE, COLUMN_REQUEST_COOLDOWN, COLUMN_TEAM_LIMIT, COLUMN_SEASON,
                COLUMN_DISABLED, COLUMN_WEAPONS, COLUMN_SQUAD_LEVEL, COLUMN_COST_CREDITS,
                COLUMN_COST_PREMIUM, COLUMN_REQUIRES_NITRO, COLUMN_MAPS_WHITELIST, COLUMN_FACTIONS_WHITELIST, COLUMN_CREATOR,
                COLUMN_LAST_EDITOR, COLUMN_CREATION_TIME, COLUMN_LAST_EDIT_TIME),
            objs, reader =>
            {
                pk2 = reader.GetInt32(0);
            }, token).ConfigureAwait(false);
        if (pk2 >= 0)
            item.PrimaryKey = pk2;
        StringBuilder builder = new StringBuilder(168);
        if (item.Items is { Length: > 0 })
        {
            builder.Append($"DELETE FROM `{TABLE_ITEMS}` WHERE `{COLUMN_EXT_PK}` = @0; INSERT INTO `{TABLE_ITEMS}` ({SqlTypes.ColumnList(
                COLUMN_EXT_PK, COLUMN_ITEM_GUID, COLUMN_ITEM_X, COLUMN_ITEM_Y, COLUMN_ITEM_ROTATION, COLUMN_ITEM_PAGE,
                COLUMN_ITEM_CLOTHING, COLUMN_ITEM_REDIRECT, COLUMN_ITEM_AMOUNT, COLUMN_ITEM_METADATA)}) VALUES ");
            objs = new object[item.Items.Length * 10];
            bool one = false;
            for (int i = 0; i < item.Items.Length; ++i)
            {
                IKitItem item2 = item.Items[i];
                if (item2 is not IBaseItem && item2 is not IAssetRedirect || item2 is not IItemJar && item2 is not IClothingJar)
                {
                    L.LogWarning("Item in kit \"" + item.Id + "\" (" + item2 + ") not a valid type: " + item2.GetType().Name + ".");
                    continue;
                }

                one = true;
                int index2 = i * 10;
                IItem? itemObj = item2 as IItem;
                IBaseItem? baseObj = item2 as IBaseItem;
                objs[index2] = pk2;
                objs[index2 + 1] = baseObj != null ? baseObj.Item.ToString("N") : DBNull.Value;
                if (item2 is IItemJar jarObj)
                {
                    objs[index2 + 2] = jarObj.X;
                    objs[index2 + 3] = jarObj.Y;
                    objs[index2 + 4] = jarObj.Rotation % 4;
                    objs[index2 + 5] = jarObj.Page.ToString();
                }
                else
                    objs[index2 + 2] = objs[index2 + 3] = objs[index2 + 4] = objs[index2 + 5] = DBNull.Value;

                objs[index2 + 6] = item2 is IClothingJar jar ? jar.Type.ToString() : DBNull.Value;
                objs[index2 + 7] = item2 is IAssetRedirect redirObj ? redirObj.RedirectType.ToString() : DBNull.Value;
                objs[index2 + 8] = itemObj != null ? itemObj.Amount : DBNull.Value;
                objs[index2 + 9] = baseObj != null ? baseObj.State : DBNull.Value;
                F.AppendPropertyList(builder, index2, 10);
            }
            builder.Append(';');
            if (one)
                await Sql.NonQueryAsync(builder.ToString(), objs, token).ConfigureAwait(false);
            builder.Clear();
        }
        else
        {
            await Sql.NonQueryAsync($"DELETE FROM `{TABLE_ITEMS}` WHERE `{COLUMN_EXT_PK}` = @0;", new object[] { pk2 }, token).ConfigureAwait(false);
        }

        objs = new object[
            (item.UnlockRequirements != null ? item.UnlockRequirements.Length * 2 : 0) +
            (item.Skillsets != null ? item.Skillsets.Length * 3 : 0) +
            (item.FactionFilter != null ? item.FactionFilter.Length * 2 : 0) +
            (item.MapFilter != null ? item.MapFilter.Length * 2 : 0) +
            (item.RequestSigns != null ? item.RequestSigns.Length * 2 : 0) +
            (item.SignText != null ? item.SignText.Count * 3 : 0)];
        int index = 0;
        if (item.UnlockRequirements is { Length: > 0 })
        {
            builder.Append($"DELETE FROM `{TABLE_UNLOCK_REQUIREMENTS}` WHERE `{COLUMN_EXT_PK}` = @0; INSERT INTO `{TABLE_UNLOCK_REQUIREMENTS}` ({SqlTypes.ColumnList(
                COLUMN_EXT_PK, UnlockRequirement.COLUMN_JSON)}) VALUES ");
            using MemoryStream str = new MemoryStream(48);
            for (int i = 0; i < item.UnlockRequirements.Length; ++i)
            {
                UnlockRequirement req = item.UnlockRequirements[i];
                if (i != 0)
                    str.Seek(0L, SeekOrigin.Begin);
                Utf8JsonWriter writer = new Utf8JsonWriter(str, JsonEx.condensedWriterOptions);
                UnlockRequirement.Write(writer, req);
                writer.Dispose();
                string json = System.Text.Encoding.UTF8.GetString(str.GetBuffer(), 0, checked((int)str.Position));
                objs[index] = pk2;
                objs[index + 1] = json;
                F.AppendPropertyList(builder, index, 2, i);
                index += 2;
            }
            builder.Append("; ");
        }
        else
        {
            await Sql.NonQueryAsync($"DELETE FROM `{TABLE_UNLOCK_REQUIREMENTS}` WHERE `{COLUMN_EXT_PK}` = @0;", new object[] { pk2 }, token).ConfigureAwait(false);
        }

        if (item.Skillsets is { Length: > 0 })
        {
            builder.Append($"DELETE FROM `{TABLE_SKILLSETS}` WHERE `{COLUMN_EXT_PK}` = @0; INSERT INTO `{TABLE_SKILLSETS}` ({SqlTypes.ColumnList(
                COLUMN_EXT_PK, Skillset.COLUMN_SKILL, Skillset.COLUMN_LEVEL)}) VALUES ");
            for (int i = 0; i < item.Skillsets.Length; ++i)
            {
                Skillset set = item.Skillsets[i];
                if (set.Speciality is not EPlayerSpeciality.DEFENSE and not EPlayerSpeciality.OFFENSE and not EPlayerSpeciality.SUPPORT)
                    continue;
                objs[index] = pk2;
                objs[index + 1] = set.Speciality switch
                { EPlayerSpeciality.DEFENSE => set.Defense.ToString(), EPlayerSpeciality.OFFENSE => set.Offense.ToString(), _ => set.Support.ToString() };
                objs[index + 2] = set.Level;
                F.AppendPropertyList(builder, index, 3, i);
                index += 3;
            }
            builder.Append("; ");
        }
        else
        {
            await Sql.NonQueryAsync($"DELETE FROM `{TABLE_SKILLSETS}` WHERE `{COLUMN_EXT_PK}` = @0;", new object[] { pk2 }, token).ConfigureAwait(false);
        }

        if (item.FactionFilter is { Length: > 0 } && item.FactionFilter.Any(x => x.IsValid))
        {
            builder.Append($"DELETE FROM `{TABLE_FACTION_FILTER}` WHERE `{COLUMN_EXT_PK}` = @0; INSERT INTO `{TABLE_FACTION_FILTER}` ({SqlTypes.ColumnList(
                COLUMN_EXT_PK, COLUMN_FILTER_FACTION)}) VALUES ");
            for (int i = 0; i < item.FactionFilter.Length; ++i)
            {
                PrimaryKey f = item.FactionFilter[i];
                if (!f.IsValid)
                    continue;
                objs[index] = pk2;
                objs[index + 1] = f.Key;
                F.AppendPropertyList(builder, index, 2, i);
                index += 2;
            }
            builder.Append("; ");
        }
        else
        {
            await Sql.NonQueryAsync($"DELETE FROM `{TABLE_FACTION_FILTER}` WHERE `{COLUMN_EXT_PK}` = @0;", new object[] { pk2 }, token).ConfigureAwait(false);
        }
        if (item.MapFilter is { Length: > 0 } && item.MapFilter.Any(x => x.IsValid))
        {
            builder.Append($"DELETE FROM `{TABLE_MAP_FILTER}` WHERE `{COLUMN_EXT_PK}` = @0; INSERT INTO `{TABLE_MAP_FILTER}` ({SqlTypes.ColumnList(
                COLUMN_EXT_PK, COLUMN_FILTER_MAP)}) VALUES ");
            for (int i = 0; i < item.MapFilter.Length; ++i)
            {
                PrimaryKey f = item.MapFilter[i];
                if (!f.IsValid)
                    continue;
                objs[index] = pk2;
                objs[index + 1] = f.Key;
                F.AppendPropertyList(builder, index, 2, i);
                index += 2;
            }
            builder.Append("; ");
        }
        else
        {
            await Sql.NonQueryAsync($"DELETE FROM `{TABLE_MAP_FILTER}` WHERE `{COLUMN_EXT_PK}` = @0;", new object[] { pk2 }, token).ConfigureAwait(false);
        }
        if (item.RequestSigns is { Length: > 0 } && item.RequestSigns.Any(x => x.IsValid))
        {
            builder.Append($"DELETE FROM `{TABLE_REQUEST_SIGNS}` WHERE `{COLUMN_EXT_PK}` = @0; INSERT INTO `{TABLE_REQUEST_SIGNS}` ({SqlTypes.ColumnList(
                COLUMN_EXT_PK, COLUMN_REQUEST_SIGN)}) VALUES ");
            for (int i = 0; i < item.RequestSigns.Length; ++i)
            {
                PrimaryKey f = item.RequestSigns[i];
                if (!f.IsValid)
                    continue;
                objs[index] = pk2;
                objs[index + 1] = f.Key;
                F.AppendPropertyList(builder, index, 2, i);
                index += 2;
            }
            builder.Append("; ");
        }
        else
        {
            await Sql.NonQueryAsync($"DELETE FROM `{TABLE_REQUEST_SIGNS}` WHERE `{COLUMN_EXT_PK}` = @0;", new object[] { pk2 }, token).ConfigureAwait(false);
        }
        if (item.SignText is { Count: > 0 })
        {
            builder.Append($"DELETE FROM `{TABLE_SIGN_TEXT}` WHERE `{COLUMN_EXT_PK}` = @0; INSERT INTO `{TABLE_SIGN_TEXT}` ({SqlTypes.ColumnList(
                COLUMN_EXT_PK, F.COLUMN_LANGUAGE, F.COLUMN_VALUE)}) VALUES ");
            int i = 0;
            foreach (KeyValuePair<string, string> pair in item.SignText)
            {
                objs[index] = pk2;
                objs[index + 1] = pair.Key;
                objs[index + 2] = pair.Value;
                F.AppendPropertyList(builder, index, 3, i++);
                index += 3;
            }
            builder.Append(';');
        }
        else
        {
            await Sql.NonQueryAsync($"DELETE FROM `{TABLE_SIGN_TEXT}` WHERE `{COLUMN_EXT_PK}` = @0;", new object[] { pk2 }, token).ConfigureAwait(false);
        }

        if (builder.Length > 0)
        {
            await Sql.NonQueryAsync(builder.ToString(), objs, token).ConfigureAwait(false);
            builder.Clear();
        }
    }
    [Obsolete]
    protected override async Task<Kit?> DownloadItem(PrimaryKey pk, CancellationToken token = default)
    {
        Kit? obj = null;
        if (!pk.IsValid)
            throw new ArgumentException("Primary key is not valid.", nameof(pk));
        int pk2 = pk;
        object[] pkObjs = { pk2 };
        await Sql.QueryAsync(F.BuildSelectWhereLimit1(TABLE_MAIN, COLUMN_PK, 0, COLUMN_KIT_ID, COLUMN_FACTION,
                COLUMN_CLASS, COLUMN_BRANCH, COLUMN_TYPE, COLUMN_REQUEST_COOLDOWN, COLUMN_TEAM_LIMIT,
                COLUMN_SEASON, COLUMN_DISABLED, COLUMN_WEAPONS, COLUMN_SQUAD_LEVEL, COLUMN_COST_CREDITS,
                COLUMN_COST_PREMIUM, COLUMN_REQUIRES_NITRO, COLUMN_MAPS_WHITELIST, COLUMN_FACTIONS_WHITELIST, COLUMN_CREATOR,
                COLUMN_LAST_EDITOR, COLUMN_CREATION_TIME, COLUMN_LAST_EDIT_TIME)
            , pkObjs, reader =>
            {
                obj = ReadKit(reader, -1);
            }, token).ConfigureAwait(false);
        if (obj != null)
        {
            List<IKitItem> items = new List<IKitItem>(16);
            await Sql.QueryAsync(
                $"SELECT {SqlTypes.ColumnList(
                    COLUMN_ITEM_GUID, COLUMN_ITEM_X, COLUMN_ITEM_Y, COLUMN_ITEM_ROTATION, COLUMN_ITEM_PAGE,
                    COLUMN_ITEM_CLOTHING, COLUMN_ITEM_REDIRECT, COLUMN_ITEM_AMOUNT, COLUMN_ITEM_METADATA)} " +
                $"FROM `{TABLE_ITEMS}` WHERE `{COLUMN_EXT_PK}`=@0;", pkObjs, reader =>
                {
                    items.Add(ReadItem(reader, -1));
                }, token).ConfigureAwait(false);
            obj.Items = items.ToArray();
            items = null!;

            await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(UnlockRequirement.COLUMN_JSON)} " +
                                 $"FROM `{TABLE_UNLOCK_REQUIREMENTS}` WHERE `{COLUMN_EXT_PK}`=@0;", pkObjs, reader =>
                                 {
                                     UnlockRequirement? req = UnlockRequirement.Read(reader, -1);
                                     if (req == null)
                                         throw new FormatException("Invalid unlock requirement from JSON data \"" + reader.GetString(0) + "\".");
                                     UnlockRequirement[]? arr = obj.UnlockRequirements;
                                     Util.AddToArray(ref arr, req);
                                     obj.UnlockRequirements = arr!;
                                 }, token).ConfigureAwait(false);
            await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(Skillset.COLUMN_SKILL, Skillset.COLUMN_LEVEL)} " +
                                 $"FROM `{TABLE_SKILLSETS}` WHERE `{COLUMN_EXT_PK}`=@0;", pkObjs, reader =>
                                 {
                                     Skillset set = Skillset.Read(reader);
                                     Skillset[]? arr = obj.Skillsets;
                                     Util.AddToArray(ref arr, set);
                                     obj.Skillsets = arr!;
                                 }, token).ConfigureAwait(false);
            await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_FILTER_FACTION)} " +
                                 $"FROM `{TABLE_FACTION_FILTER}`;", null, reader =>
                                 {
                                     int faction = reader.GetInt32(0);
                                     PrimaryKey[]? arr = obj.FactionFilter;
                                     Util.AddToArray(ref arr, faction);
                                     obj.FactionFilter = arr!;
                                 }, token).ConfigureAwait(false);
            await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_FILTER_MAP)} " +
                                 $"FROM `{TABLE_MAP_FILTER}`;", null, reader =>
                                 {
                                     int faction = reader.GetInt32(0);
                                     PrimaryKey[]? arr = obj.MapFilter;
                                     Util.AddToArray(ref arr, faction);
                                     obj.MapFilter = arr!;
                                 }, token).ConfigureAwait(false);
            await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_REQUEST_SIGN)} " +
                                 $"FROM `{TABLE_REQUEST_SIGNS}`;", null, reader =>
                                 {
                                     int structure = reader.GetInt32(0);
                                     PrimaryKey[]? arr = obj.RequestSigns;
                                     Util.AddToArray(ref arr, structure);
                                     obj.RequestSigns = arr!;
                                 }, token).ConfigureAwait(false);
            await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(F.COLUMN_LANGUAGE, F.COLUMN_VALUE)} " +
                                 $"FROM `{TABLE_SIGN_TEXT}` WHERE `{COLUMN_EXT_PK}`=@0;", pkObjs, reader =>
                                 {
                                     F.ReadToTranslationList(reader, obj.SignText ??= new TranslationList(1), -1);
                                 }, token).ConfigureAwait(false);

            VerifyKitIntegrity(obj);
        }
        GC.Collect();
        return obj;
    }
    [Obsolete]
    protected override async Task<Kit[]> DownloadAllItems(CancellationToken token = default)
    {
        List<Kit> list = new List<Kit>(32);
        await Sql.QueryAsync(F.BuildSelect(TABLE_MAIN, COLUMN_PK, COLUMN_KIT_ID, COLUMN_FACTION,
            COLUMN_CLASS, COLUMN_BRANCH, COLUMN_TYPE, COLUMN_REQUEST_COOLDOWN, COLUMN_TEAM_LIMIT,
            COLUMN_SEASON, COLUMN_DISABLED, COLUMN_WEAPONS, COLUMN_SQUAD_LEVEL, COLUMN_COST_CREDITS,
            COLUMN_COST_PREMIUM, COLUMN_REQUIRES_NITRO, COLUMN_MAPS_WHITELIST, COLUMN_FACTIONS_WHITELIST, COLUMN_CREATOR,
            COLUMN_LAST_EDITOR, COLUMN_CREATION_TIME, COLUMN_LAST_EDIT_TIME),
            null, reader =>
            {
                list.Add(ReadKit(reader));
            }, token).ConfigureAwait(false);

        if (list.Count == 0)
            return list.ToArray();

        List<KeyValuePair<int, IKitItem>> tempList = new List<KeyValuePair<int, IKitItem>>(list.Count * 16);
        await Sql.QueryAsync(
            $"SELECT {SqlTypes.ColumnList(
                COLUMN_EXT_PK, COLUMN_ITEM_GUID, COLUMN_ITEM_X, COLUMN_ITEM_Y, COLUMN_ITEM_ROTATION, COLUMN_ITEM_PAGE,
                COLUMN_ITEM_CLOTHING, COLUMN_ITEM_REDIRECT, COLUMN_ITEM_AMOUNT, COLUMN_ITEM_METADATA)} " +
            $"FROM `{TABLE_ITEMS}`;", null, reader =>
            {
                int pk = reader.GetInt32(0);
                IKitItem item = ReadItem(reader);
                tempList.Add(new KeyValuePair<int, IKitItem>(pk, item));
            }, token).ConfigureAwait(false);

        int[] ct = new int[list.Count];
        for (int i = 0; i < tempList.Count; ++i)
        {
            int pk = tempList[i].Key;
            for (int j = 0; j < list.Count; ++j)
            {
                if (list[j].PrimaryKey.Key == pk)
                {
                    ++ct[j];
                    break;
                }
            }
        }

        for (int i = 0; i < list.Count; ++i)
            list[i].Items = new IKitItem[ct[i]];

        for (int i = 0; i < tempList.Count; ++i)
        {
            int pk = tempList[i].Key;
            for (int j = 0; j < list.Count; ++j)
            {
                if (list[j].PrimaryKey.Key == pk)
                {
                    Kit kit = list[j];
                    kit.Items[kit.Items.Length - ct[j]--] = tempList[i].Value;
                    break;
                }
            }
        }

        tempList = null!;
        await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_EXT_PK, UnlockRequirement.COLUMN_JSON)} " +
                             $"FROM `{TABLE_UNLOCK_REQUIREMENTS}`;", null, reader =>
                             {
                                 int pk = reader.GetInt32(0);
                                 for (int i = 0; i < list.Count; ++i)
                                 {
                                     if (list[i].PrimaryKey.Key == pk)
                                     {
                                         UnlockRequirement? req = UnlockRequirement.Read(reader);
                                         if (req != null)
                                         {
                                             UnlockRequirement[]? arr = list[i].UnlockRequirements;
                                             Util.AddToArray(ref arr, req);
                                             list[i].UnlockRequirements = arr!;
                                             break;
                                         }
                                         throw new FormatException("Invalid unlock requirement from JSON data \"" + reader.GetString(1) + "\".");
                                     }
                                 }
                             }, token).ConfigureAwait(false);
        await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_EXT_PK, Skillset.COLUMN_SKILL, Skillset.COLUMN_LEVEL)} " +
                             $"FROM `{TABLE_SKILLSETS}`;", null, reader =>
                             {
                                 int pk = reader.GetInt32(0);
                                 for (int i = 0; i < list.Count; ++i)
                                 {
                                     if (list[i].PrimaryKey.Key == pk)
                                     {
                                         Skillset set = Skillset.Read(reader);
                                         Skillset[]? arr = list[i].Skillsets;
                                         Util.AddToArray(ref arr, set);
                                         list[i].Skillsets = arr!;
                                         break;
                                     }
                                 }
                             }, token).ConfigureAwait(false);
        await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_EXT_PK, COLUMN_FILTER_FACTION)} " +
                             $"FROM `{TABLE_FACTION_FILTER}`;", null, reader =>
                             {
                                 int pk = reader.GetInt32(0);
                                 for (int i = 0; i < list.Count; ++i)
                                 {
                                     if (list[i].PrimaryKey.Key == pk)
                                     {
                                         int faction = reader.GetInt32(1);
                                         PrimaryKey[]? arr = list[i].FactionFilter;
                                         Util.AddToArray(ref arr, faction);
                                         list[i].FactionFilter = arr!;
                                         break;
                                     }
                                 }
                             }, token).ConfigureAwait(false);
        await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_EXT_PK, COLUMN_FILTER_MAP)} " +
                             $"FROM `{TABLE_MAP_FILTER}`;", null, reader =>
                             {
                                 int pk = reader.GetInt32(0);
                                 for (int i = 0; i < list.Count; ++i)
                                 {
                                     if (list[i].PrimaryKey.Key == pk)
                                     {
                                         int faction = reader.GetInt32(1);
                                         PrimaryKey[]? arr = list[i].MapFilter;
                                         Util.AddToArray(ref arr, faction);
                                         list[i].MapFilter = arr!;
                                         break;
                                     }
                                 }
                             }, token).ConfigureAwait(false);
        await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_EXT_PK, COLUMN_REQUEST_SIGN)} " +
                             $"FROM `{TABLE_REQUEST_SIGNS}`;", null, reader =>
                             {
                                 int pk = reader.GetInt32(0);
                                 for (int i = 0; i < list.Count; ++i)
                                 {
                                     if (list[i].PrimaryKey.Key == pk)
                                     {
                                         int faction = reader.GetInt32(1);
                                         PrimaryKey[]? arr = list[i].RequestSigns;
                                         Util.AddToArray(ref arr, faction);
                                         list[i].RequestSigns = arr!;
                                         break;
                                     }
                                 }
                             }, token).ConfigureAwait(false);
        await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_EXT_PK, F.COLUMN_LANGUAGE, F.COLUMN_VALUE)} " +
                             $"FROM `{TABLE_SIGN_TEXT}`;", null, reader =>
                             {
                                 int pk = reader.GetInt32(0);
                                 for (int i = 0; i < list.Count; ++i)
                                 {
                                     if (list[i].PrimaryKey.Key == pk)
                                     {
                                         F.ReadToTranslationList(reader, list[i].SignText ??= new TranslationList(1));
                                         break;
                                     }
                                 }
                             }, token).ConfigureAwait(false);
        for (int i = 0; i < list.Count; ++i)
            VerifyKitIntegrity(list[i]);
        GC.Collect();
        return list.ToArray();
    }
    /// <exception cref="FormatException"/>
    internal static Kit ReadKit(MySqlDataReader reader, int colOffset = 0)
    {
        string id = reader.GetString(colOffset + 1);
        if (id.Length < 1 || id.Length > KitEx.KitNameMaxCharLimit)
            throw new FormatException("Invalid kit ID: \"" + id + "\".");
        Class @class = reader.ReadStringEnum<Class>(colOffset + 3) ?? throw new FormatException("Invalid class: \"" + reader.GetString(colOffset + 3) + "\".");
        KitType type = reader.ReadStringEnum<KitType>(colOffset + 5) ?? throw new FormatException("Invalid type: \"" + reader.GetString(colOffset + 5) + "\".");
        return new Kit
        {
            Type = type,
            Branch = reader.ReadStringEnum<Branch>(colOffset + 4) ?? throw new FormatException("Invalid branch: \"" + reader.GetString(colOffset + 4) + "\"."),
            SquadLevel = reader.IsDBNull(colOffset + 11)
                ? SquadLevel.Member
                : reader.ReadStringEnum<SquadLevel>(colOffset + 11) ?? throw new FormatException("Invalid squad level: \"" + reader.GetString(colOffset + 11) + "\"."),
            PrimaryKey = colOffset >= 0 ? reader.GetInt32(colOffset + 0) : PrimaryKey.NotAssigned,
            Id = id,
            FactionKey = reader.IsDBNull(colOffset + 2) ? -1 : reader.GetInt32(colOffset + 2),
            Class = @class,
            RequestCooldown = reader.IsDBNull(colOffset + 6) ? 0f : reader.GetFloat(colOffset + 6),
            TeamLimit = reader.IsDBNull(colOffset + 7) ? GetDefaultTeamLimit(@class) : reader.GetFloat(colOffset + 7),
            Season = reader.IsDBNull(colOffset + 8) ? UCWarfare.Season : reader.GetByte(colOffset + 8),
            Disabled = !reader.IsDBNull(colOffset + 9) && reader.GetBoolean(colOffset + 9),
            WeaponText = reader.IsDBNull(colOffset + 10) ? null : reader.GetString(colOffset + 10),
            CreditCost = reader.IsDBNull(colOffset + 12) ? 0 : reader.GetInt32(colOffset + 12),
            PremiumCost = type is KitType.Public or KitType.Special || reader.IsDBNull(colOffset + 13)
                ? 0
                : type is KitType.Loadout && UCWarfare.IsLoaded
                    ? decimal.Round(UCWarfare.Config.LoadoutCost, 2)
                    : new decimal(Math.Round(reader.GetDouble(colOffset + 13), 2)),
            RequiresNitro = !reader.IsDBNull(colOffset + 14) && reader.GetBoolean(colOffset + 14),
            MapFilterIsWhitelist = !reader.IsDBNull(colOffset + 15) && reader.GetBoolean(colOffset + 15),
            FactionFilterIsWhitelist = !reader.IsDBNull(colOffset + 16) && reader.GetBoolean(colOffset + 16),
            Creator = reader.IsDBNull(colOffset + 17) ? 0ul : reader.GetUInt64(colOffset + 17),
            LastEditor = reader.IsDBNull(colOffset + 18) ? 0ul : reader.GetUInt64(colOffset + 18),
            CreatedTimestamp = reader.IsDBNull(colOffset + 19)
                ? DateTimeOffset.MinValue
                : reader.GetDateTimeOffset(colOffset + 19),
            LastEditedTimestamp = reader.IsDBNull(colOffset + 20)
                ? DateTimeOffset.MinValue
                : reader.GetDateTimeOffset(colOffset + 20)
        };
    }
    /// <exception cref="FormatException"/>
    internal static IKitItem ReadItem(MySqlDataReader reader, int colOffset = 0)
    {
        IKitItem item;
        bool hasGuid = !reader.IsDBNull(colOffset + 1);
        bool hasRedir = !reader.IsDBNull(colOffset + 7);
        bool hasPageStuff = !(reader.IsDBNull(colOffset + 2) || reader.IsDBNull(colOffset + 3) || reader.IsDBNull(colOffset + 5));
        bool hasClothing = !reader.IsDBNull(colOffset + 6);
        if (!hasGuid && !hasRedir)
            throw new FormatException("Item row must either have a GUID or a redirect type.");
        if (!hasPageStuff && !hasClothing)
            throw new FormatException("Item row must either have jar information or a clothing type.");
        if (hasGuid)
        {
            Guid? guid = reader.ReadGuidString(colOffset + 1);
            if (!guid.HasValue)
            {
                if (hasRedir)
                {
                    L.LogWarning("Invalid GUID in item row: \"" + reader.GetString(colOffset + 1) + "\", falling back to redirect type.");
                    goto redir;
                }
                throw new FormatException("Invalid GUID in item row: \"" + reader.GetString(colOffset + 1) + "\".");
            }
            if (hasPageStuff)
            {
                byte x = reader.GetByte(colOffset + 2),
                     y = reader.GetByte(colOffset + 3),
                     rot = reader.IsDBNull(colOffset + 4) ? (byte)0 : reader.GetByte(colOffset + 4),
                     amt = reader.IsDBNull(colOffset + 8) ? (byte)0 : reader.GetByte(colOffset + 8);
                rot %= 4;
                Page? pg = reader.ReadStringEnum<Page>(colOffset + 5);
                if (!pg.HasValue)
                    throw new FormatException("Invalid page in item row: \"" + reader.GetString(colOffset + 5) + "\".");
                item = new PageItem(guid.Value, x, y, rot, reader.IsDBNull(colOffset + 9) ? Array.Empty<byte>() : reader.ReadByteArray(colOffset + 9), amt, pg.Value);
            }
            else
            {
                ClothingType? type = reader.ReadStringEnum<ClothingType>(colOffset + 6);
                if (!type.HasValue)
                    throw new FormatException("Invalid clothing type in item row: \"" + reader.GetString(colOffset + 6) + "\".");
                item = new ClothingItem(guid.Value, type.Value, reader.IsDBNull(colOffset + 9) ? Array.Empty<byte>() : reader.ReadByteArray(colOffset + 9));
            }

            return item;
        }
    redir:
        RedirectType? redirect = reader.ReadStringEnum<RedirectType>(colOffset + 7);
        if (!redirect.HasValue)
            throw new FormatException("Invalid redirect in item row: \"" + reader.GetString(colOffset + 7) + "\".");
        if (hasPageStuff)
        {
            byte x = reader.GetByte(colOffset + 2),
                y = reader.GetByte(colOffset + 3),
                rot = reader.IsDBNull(colOffset + 4) ? (byte)0 : reader.GetByte(colOffset + 4);
            rot %= 4;
            Page? pg = reader.ReadStringEnum<Page>(colOffset + 5);
            if (!pg.HasValue)
                throw new FormatException("Invalid page in item row: \"" + reader.GetString(colOffset + 5) + "\".");
            item = new AssetRedirectItem(redirect.Value, x, y, rot, pg.Value);
        }
        else
        {
            ClothingType? type = reader.ReadStringEnum<ClothingType>(colOffset + 6);
            if (!type.HasValue)
                throw new FormatException("Invalid clothing type in item row: \"" + reader.GetString(colOffset + 6) + "\".");
            item = new AssetRedirectClothing(redirect.Value, type.Value);
        }

        return item;
    }

#if DEBUG // Migrating old kits
    public async Task MigrateOldKits(CancellationToken token = default)
    {
        Dictionary<int, OldKit> kits = new Dictionary<int, OldKit>(256);
        int rows = 0;
        // todo actually import loadouts later, too much work rn
        await Sql.QueryAsync("SELECT * FROM `kit_data`;", null, reader =>
        {
            OldKit kit = new OldKit
            {
                PrimaryKey = reader.GetInt32(0),
                Name = reader.GetString(1),
                Class = (Class)reader.GetInt32(2),
                Branch = (Branch)reader.GetInt32(3),
                Team = reader.GetUInt64(4),
                CreditCost = reader.GetUInt16(5),
                UnlockLevel = reader.GetUInt16(6),
                IsPremium = reader.GetBoolean(7),
                PremiumCost = reader.GetFloat(8),
                IsLoadout = reader.GetBoolean(9),
                TeamLimit = reader.GetFloat(10),
                Cooldown = reader.GetInt32(11),
                Disabled = reader.GetBoolean(12),
                Weapons = reader.GetString(13),
                SquadLevel = (SquadLevel)reader.GetByte(14)
            };
            kits.Add(kit.PrimaryKey.Key, kit);
            ++rows;
        }, token).ConfigureAwait(false);
        L.Log($"kit_data: Read {rows} row(s).");
        rows = 0;
        await Sql.QueryAsync("SELECT * FROM `kit_items`;", Array.Empty<object>(), reader =>
        {
            int kitPk = reader.GetInt32(1);
            if (kits.TryGetValue(kitPk, out OldKit kit))
            {
                PageItem item = new PageItem
                {
                    Item = reader.ReadGuid(2),
                    X = reader.GetByte(3),
                    Y = reader.GetByte(4),
                    Rotation = reader.GetByte(5),
                    Page = (Page)reader.GetByte(6),
                    Amount = reader.GetByte(7),
                    State = reader.ReadByteArray(8)
                };
                (kit.Items ??= new List<PageItem>(32)).Add(item);
                ++rows;
            }
        }, token).ConfigureAwait(false);
        L.Log($"kit_items: Read {rows} row(s).");
        rows = 0;
        await Sql.QueryAsync("SELECT * FROM `kit_clothes`;", Array.Empty<object>(), reader =>
        {
            int kitPk = reader.GetInt32(1);
            if (kits.TryGetValue(kitPk, out OldKit kit))
            {
                ClothingItem clothing = new ClothingItem()
                {
                    Item = reader.ReadGuid(2),
                    Type = (ClothingType)reader.GetByte(3)
                };
                (kit.Clothes ??= new List<ClothingItem>(7)).Add(clothing);
                ++rows;
            }
        }, token).ConfigureAwait(false);
        L.Log($"kit_clothes: Read {rows} row(s).");
        rows = 0;
        await Sql.QueryAsync("SELECT * FROM `kit_skillsets`;", Array.Empty<object>(), reader =>
        {
            int kitPk = reader.GetInt32(1);
            if (kits.TryGetValue(kitPk, out OldKit kit))
            {
                EPlayerSpeciality type = (EPlayerSpeciality)reader.GetByte(2);
                Skillset set;
                byte v = reader.GetByte(3);
                byte lvl = reader.GetByte(4);
                bool def = false;
                switch (type)
                {
                    case EPlayerSpeciality.OFFENSE:
                        set = new Skillset((EPlayerOffense)v, lvl);
                        break;
                    case EPlayerSpeciality.DEFENSE:
                        set = new Skillset((EPlayerDefense)v, lvl);
                        break;
                    case EPlayerSpeciality.SUPPORT:
                        set = new Skillset((EPlayerSupport)v, lvl);
                        break;
                    default:
                        set = default;
                        def = true;
                        break;
                }
                if (!def)
                {
                    (kit.Skillsets ??= new List<Skillset>(1)).Add(set);
                    ++rows;
                }
                else
                    L.LogWarning("Invalid skillset for kit " + kitPk.ToString(Data.AdminLocale) + ".");
            }
        }, token).ConfigureAwait(false);
        L.Log($"kit_skillsets: Read {rows} row(s).");
        rows = 0;
        await Sql.QueryAsync("SELECT `Kit`, `Language`, `Text` FROM `kit_lang`;", Array.Empty<object>(), reader =>
        {
            int kitPk = reader.GetInt32(0);
            if (kits.TryGetValue(kitPk, out OldKit kit))
            {
                string lang = reader.GetString(1);
                kit.SignText ??= new TranslationList(1);
                if (!kit.SignText.ContainsKey(lang))
                {
                    kit.SignText.Add(lang, reader.GetString(2));
                    ++rows;
                }
                else
                    L.LogWarning("Duplicate translation for kit " + kit.Name + " (" + kit.PrimaryKey + ") for language " + lang);
            }
        }, token).ConfigureAwait(false);
        L.Log($"kit_lang: Read {rows} row(s).");
        rows = 0;
        await Sql.QueryAsync("SELECT `Kit`, `JSON` FROM `kit_unlock_requirements`;", Array.Empty<object>(), reader =>
        {
            int kitPk = reader.GetInt32(0);
            if (kits.TryGetValue(kitPk, out OldKit kit))
            {
                Utf8JsonReader jsonReader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(reader.GetString(1)));
                UnlockRequirement? req = UnlockRequirement.Read(ref jsonReader);
                if (req != null)
                {
                    (kit.UnlockRequirements ??= new List<UnlockRequirement>(1)).Add(req);
                    ++rows;
                }
            }
        }, token);
        L.Log($"kit_unlock_requirements: Read {rows} row(s).");
        rows = 0;
        await Sql.QueryAsync("SELECT `Kit`, `Steam64`, `AccessType` FROM `kit_access`;", Array.Empty<object>(), reader =>
        {
            int kitPk = reader.GetInt32(0);
            if (kits.TryGetValue(kitPk, out OldKit kit))
            {
                string type = reader.GetString(2);
                if (!Enum.TryParse(type, true, out KitAccessType etype))
                {
                    etype = type.Equals("QUEST_REWARD", StringComparison.OrdinalIgnoreCase) ? KitAccessType.QuestReward : KitAccessType.Unknown;
                }
                ulong s64 = reader.GetUInt64(1);
                (kit.Access ??= new List<AccessRow>(1)).Add(new AccessRow(s64, etype));
                ++rows;
            }
        }, token);
        L.Log($"kit_access: Read {rows} row(s).");
        rows = 0;
        List<KeyValuePair<Kit, List<AccessRow>?>> newKits = new List<KeyValuePair<Kit, List<AccessRow>?>>(kits.Count);
        foreach (OldKit kit in kits.Values)
        {
            string id = kit.Name!;
            FactionInfo? faction = null;
            if (!kit.IsLoadout)
            {
                if (id.StartsWith("prem_"))
                    id = id.Substring(5);
                if (id.StartsWith("me", StringComparison.OrdinalIgnoreCase))
                    faction = TeamManager.FindFactionInfo(FactionInfo.MEC);
                else if (id.StartsWith("us", StringComparison.OrdinalIgnoreCase))
                    faction = TeamManager.FindFactionInfo(FactionInfo.USA);
                else if (id.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
                    faction = TeamManager.FindFactionInfo(FactionInfo.Russia);
                else if (id.StartsWith("ge", StringComparison.OrdinalIgnoreCase))
                    faction = TeamManager.FindFactionInfo(FactionInfo.Germany);
                else if (id.StartsWith("usmc", StringComparison.OrdinalIgnoreCase))
                    faction = TeamManager.FindFactionInfo(FactionInfo.USMC);
                else if (id.StartsWith("sov", StringComparison.OrdinalIgnoreCase))
                    faction = TeamManager.FindFactionInfo(FactionInfo.Soviet);
                else if (id.StartsWith("pl", StringComparison.OrdinalIgnoreCase))
                    faction = TeamManager.FindFactionInfo(FactionInfo.Poland);
                else if (id.StartsWith("mi", StringComparison.OrdinalIgnoreCase))
                    faction = TeamManager.FindFactionInfo(FactionInfo.Militia);
                else if (id.StartsWith("idf", StringComparison.OrdinalIgnoreCase))
                    faction = TeamManager.FindFactionInfo(FactionInfo.Israel);
                else if (id.StartsWith("fr", StringComparison.OrdinalIgnoreCase))
                    faction = TeamManager.FindFactionInfo(FactionInfo.France);
                else if (id.StartsWith("caf", StringComparison.OrdinalIgnoreCase))
                    faction = TeamManager.FindFactionInfo(FactionInfo.Canada);
                else if (id.StartsWith("sa", StringComparison.OrdinalIgnoreCase))
                    faction = TeamManager.FindFactionInfo(FactionInfo.SouthAfrica);
                else if (id.StartsWith("mz", StringComparison.OrdinalIgnoreCase))
                    faction = TeamManager.FindFactionInfo(FactionInfo.Mozambique);
            }

            SqlItem<Kit>? existing = await FindProxy(kit.PrimaryKey, token).ConfigureAwait(false);
            if (existing?.Item is { } newKit2 && !newKit2.Id.Equals(kit.Name))
                kit.PrimaryKey = PrimaryKey.NotAssigned;

            Kit newKit = new Kit(kit.Name!, kit.Class == Class.None ? Class.Unarmed : kit.Class, kit.Branch == Branch.Default ? GetDefaultBranch(kit.Class) : kit.Branch,
                kit.IsPremium
                    ? (kit.PremiumCost < 0
                        ? KitType.Special
                        : KitType.Elite)
                    : (kit.IsLoadout
                        ? KitType.Loadout
                        : KitType.Public),
                kit.SquadLevel, faction)
            {
                PrimaryKey = kit.PrimaryKey,
                CreditCost = kit.CreditCost,
                Disabled = kit.Disabled,
                Season = kit.Disabled ? 1 : 2,
                WeaponText = kit.Weapons,
                TeamLimit = kit.TeamLimit,
                PremiumCost = kit.PremiumCost < 0 ? 0m : decimal.Round((decimal)kit.PremiumCost, 2),
                RequestCooldown = kit.Cooldown,
                CreatedTimestamp = DateTimeOffset.MinValue
            };
            if (!kit.IsLoadout && faction != null)
            {
                if (newKit.Type == KitType.Public)
                {
                    newKit.FactionFilterIsWhitelist = false;
                }
                else if (faction.FactionId.Equals(FactionInfo.USA, StringComparison.OrdinalIgnoreCase) ||
                         faction.FactionId.Equals(FactionInfo.Canada, StringComparison.OrdinalIgnoreCase) ||
                         faction.FactionId.Equals(FactionInfo.USMC, StringComparison.OrdinalIgnoreCase))
                {
                    newKit.FactionFilter = new PrimaryKey[]
                    {
                        TeamManager.FindFactionInfo(FactionInfo.Russia)!.PrimaryKey,
                        TeamManager.FindFactionInfo(FactionInfo.MEC)!.PrimaryKey
                    };
                    newKit.FactionFilterIsWhitelist = false;
                }
                else if (faction.FactionId.Equals(FactionInfo.Russia, StringComparison.OrdinalIgnoreCase) ||
                         faction.FactionId.Equals(FactionInfo.Soviet, StringComparison.OrdinalIgnoreCase) ||
                         faction.FactionId.Equals(FactionInfo.MEC, StringComparison.OrdinalIgnoreCase))
                {
                    newKit.FactionFilter = new PrimaryKey[]
                    {
                        TeamManager.FindFactionInfo(FactionInfo.USA)!.PrimaryKey
                    };
                    newKit.FactionFilterIsWhitelist = false;
                }
            }
            if (kit.Skillsets != null)
                newKit.Skillsets = kit.Skillsets.ToArray();
            if (kit.UnlockRequirements != null)
                newKit.UnlockRequirements = kit.UnlockRequirements.ToArray();
            List<IKitItem> items = new List<IKitItem>((kit.Items != null ? kit.Items.Count : 0) + (kit.Clothes != null ? kit.Clothes.Count : 0));
            if (kit.Items != null)
            {
                foreach (PageItem item in kit.Items)
                {
                    RedirectType? t = item.LegacyRedirect;
                    if (t.HasValue)
                    {
                        items.Add(new AssetRedirectItem(t.Value, item.X, item.Y, item.Rotation, item.Page));
                    }
                    else
                    {
                        items.Add(item);
                    }
                }
            }
            if (kit.Clothes != null)
            {
                foreach (ClothingItem item in kit.Clothes)
                {
                    RedirectType? t = item.LegacyRedirect;
                    if (t.HasValue)
                    {
                        items.Add(new AssetRedirectClothing(t.Value, item.Type));
                    }
                    else
                    {
                        items.Add(item);
                    }
                }
            }
            newKit.Items = items.ToArray();
            if (kit.UnlockLevel > 0)
            {
                LevelUnlockRequirement req = new LevelUnlockRequirement { UnlockLevel = kit.UnlockLevel };
                UnlockRequirement[] reqs = newKit.UnlockRequirements;
                Util.AddToArray(ref reqs!, req);
                newKit.UnlockRequirements = reqs;
            }
            if (kit.SignText != null)
            {
                newKit.SignText = kit.SignText;
            }
            newKits.Add(new KeyValuePair<Kit, List<AccessRow>?>(newKit, kit.Access));
        }
        await WaitAsync(token).ConfigureAwait(false);
        string @base =
            $"INSERT INTO `{TABLE_ACCESS}` ({SqlTypes.ColumnList(COLUMN_EXT_PK, COLUMN_ACCESS_STEAM_64, COLUMN_ACCESS_TYPE)}) " +
            "VALUES ";
        StringBuilder builder = new StringBuilder(@base, 256);
        try
        {
            foreach (KeyValuePair<Kit, List<AccessRow>?> kvp in newKits)
            {
                SqlItem<Kit> res = await AddOrUpdateNoLock(kvp.Key, token).ConfigureAwait(false);
                Kit kit = res.Item!;
                L.Log($"Created kit: " + kit.Id + ".");
                if (kvp.Value is { Count: > 0 } list)
                {
                    int pk = kit.PrimaryKey.Key;
                    if (list.Count == 1)
                    {
                        AccessRow row = list[0];
                        if (await AddAccessRow(pk, row.Steam64, row.Type == KitAccessType.Unknown
                                ? (kit.Type switch
                                {
                                    KitType.Public => KitAccessType.Credits,
                                    KitType.Loadout or KitType.Elite => KitAccessType.Purchase,
                                    _ => KitAccessType.Event
                                })
                                : row.Type, token).ConfigureAwait(false))
                            L.Log($"Added 1 row to {kit.Id}'s access list.");
                    }
                    else
                    {
                        object[] objs = new object[list.Count * 3];
                        for (int i = 0; i < list.Count; ++i)
                        {
                            AccessRow row = list[i];
                            int index = i * 3;
                            objs[index] = pk;
                            objs[index + 1] = row.Steam64;
                            objs[index + 2] = row.Type == KitAccessType.Unknown
                                ? (kit.Type switch
                                {
                                    KitType.Public => KitAccessType.Credits,
                                    KitType.Loadout or KitType.Elite => KitAccessType.Purchase,
                                    _ => KitAccessType.Event
                                })
                                : row.Type;
                            F.AppendPropertyList(builder, index, 3);
                        }
                        builder.Append(';');
                        rows = await Data.AdminSql.NonQueryAsync(builder.ToString(), objs, token).ConfigureAwait(false);
                        builder.Clear();
                        builder.Append(@base);
                        L.Log($"Added {rows} rows to {kit.Id}'s access list.");
                    }
                }
            }
        }
        finally
        {
            Release();
        }
    }

    private class OldKit
    {
        public PrimaryKey PrimaryKey = PrimaryKey.NotAssigned;
        public string? Name;
        public Class Class;
        public Branch Branch;
        public ulong Team;
        public List<UnlockRequirement>? UnlockRequirements;
        public List<Skillset>? Skillsets;
        public ushort CreditCost;
        public ushort UnlockLevel;
        public bool IsPremium;
        public float PremiumCost;
        public bool IsLoadout;
        public float TeamLimit;
        public float Cooldown;
        public bool Disabled;
        public SquadLevel SquadLevel;
        public List<PageItem>? Items;
        public List<ClothingItem>? Clothes;
        public List<AccessRow>? Access;
        public TranslationList? SignText;
        public string Weapons;
    }

    private readonly struct AccessRow
    {
        public readonly ulong Steam64;
        public readonly KitAccessType Type;
        public AccessRow(ulong steam64, KitAccessType type)
        {
            Steam64 = steam64;
            Type = type;
        }
    }
#endif
}
