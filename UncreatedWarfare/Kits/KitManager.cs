using MySqlConnector;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Json;
using Uncreated.Networking;
using Uncreated.SQL;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Sync;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Kits;

public class KitManager : ListSqlSingleton<KitOld>
{
    public override bool AwaitLoad => true;
    public override MySqlDatabase Sql => Data.AdminSql;
    public KitManager(Schema[] schemas) : base(schemas)
    {
    }
    public KitManager(string reloadKey, Schema[] schemas) : base(reloadKey, schemas)
    {
    }
    protected override Task AddOrUpdateItem(KitOld? item, PrimaryKey pk, CancellationToken token = new CancellationToken())
    {
        throw new NotImplementedException();
    }
    protected override Task<KitOld?> DownloadItem(PrimaryKey pk, CancellationToken token = new CancellationToken())
    {
        throw new NotImplementedException();
    }
    protected override Task<KitOld[]> DownloadAllItems(CancellationToken token = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public const string TABLE_MAIN = "kits";
    public const string TABLE_ITEMS = "kits_items";
    public const string COLUMN_PK = "pk";
    public const string COLUMN_EXT_PK = "Kit";
    public const string COLUMN_KIT_ID = "Id";
    public const string COLUMN_FACTION = "Faction";
    public const string COLUMN_CLASS = "Class";
    public const string COLUMN_BRANCH = "Branch";
    public const string COLUMN_TYPE = "Type";
    public const string COLUMN_REQUEST_COOLDOWN = "RequestCooldown";
    public const string COLUMN_SEASON = "Season";
    public const string COLUMN_DISABLED = "Disabled";
    public const string COLUMN_WEAPONS = "Weapons";
    public const string COLUMN_SQUAD_LEVEL = "SquadLevel";
    private static readonly Schema[] SCHEMAS =
    {
        new Schema(TABLE_MAIN, new Schema.Column[]
        {
            new Schema.Column(COLUMN_PK, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(COLUMN_KIT_ID, "varchar(" + KitEx.KIT_NAME_MAX_CHAR_LIMIT + ")"),
            new Schema.Column(COLUMN_FACTION, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = FactionInfo.COLUMN_PK,
                ForeignKeyTable = FactionInfo.TABLE_MAIN,
                Nullable = true
            },
            new Schema.Column(COLUMN_CLASS, "varchar(" + KitEx.CLASS_MAX_CHAR_LIMIT + ")"),
            new Schema.Column(COLUMN_BRANCH, "varchar(" + KitEx.BRANCH_MAX_CHAR_LIMIT + ")"),
            new Schema.Column(COLUMN_TYPE, "varchar(" + KitEx.TYPE_MAX_CHAR_LIMIT + ")"),
            new Schema.Column(COLUMN_REQUEST_COOLDOWN, SqlTypes.FLOAT) { Nullable = true },
            new Schema.Column(COLUMN_SEASON, SqlTypes.BYTE) { Nullable = true },
            new Schema.Column(COLUMN_DISABLED, SqlTypes.BOOLEAN) { Nullable = true },
            new Schema.Column(COLUMN_WEAPONS, "varchar(" + KitEx.WEAPON_TEXT_MAX_CHAR_LIMIT + ")") { Nullable = true },
            new Schema.Column(COLUMN_SQUAD_LEVEL, "varchar(" + KitEx.SQUAD_LEVEL_MAX_CHAR_LIMIT + ")") { Nullable = true }
        }, true, typeof(KitOld)),
        PageItem.GetDefaultSchema(TABLE_ITEMS, COLUMN_EXT_PK, TABLE_MAIN, COLUMN_PK, true, false, false),
    };
}

public class KitManagerOld : BaseReloadSingleton
{
    private static readonly byte[] GUID_BUFFER = new byte[16];
    internal readonly SemaphoreSlim _threadLocker = new SemaphoreSlim(1, 5);
    public static event KitChangedCallback? OnPlayersKitChanged;
    public static event KitCallback? OnKitCreated;
    public static event KitCallback? OnKitDeleted;
    public static event KitCallback? OnKitUpdated;
    public static event KitAccessCallback? OnKitAccessChanged;
    public Dictionary<int, KitOld> Kits = new Dictionary<int, KitOld>(256);
    private static KitManager _singleton;
    public static bool Loaded => _singleton.IsLoaded();
    public KitManager() : base("kits") { }
    public override void Load()
    {
        PlayerLife.OnPreDeath += PlayerLife_OnPreDeath;
        Kits.Clear();
    }
    public override void Reload()
    {
        Task.Run(async () =>
        {
            await ReloadKits();
            await UCWarfare.ToUpdate();
            if (RequestSigns.Loaded)
            {
                RequestSigns.UpdateAllSigns();
            }
            if (!KitExists(TeamManager.Team1UnarmedKit, out _))
                L.LogError("Team 1's unarmed kit, \"" + TeamManager.Team1UnarmedKit + "\", was not found, it should be added to \"" + Data.Paths.KitsStorage + "kits.json\".");
            if (!KitExists(TeamManager.Team2UnarmedKit, out _))
                L.LogError("Team 2's unarmed kit, \"" + TeamManager.Team2UnarmedKit + "\", was not found, it should be added to \"" + Data.Paths.KitsStorage + "kits.json\".");
            if (!KitExists(TeamManager.DefaultKit, out _))
                L.LogError("The default kit, \"" + TeamManager.DefaultKit + "\", was not found, it should be added to \"" + Data.Paths.KitsStorage + "kits.json\".");
        }).ConfigureAwait(false);
    }
    internal static string Search(string searchTerm)
    {
        KitManager singleton = GetSingleton();
        StringBuilder sb = new StringBuilder();
        singleton._threadLocker.Wait();
        try
        {
            int c = 0;
            foreach (KitOld v in singleton.Kits.Values)
            {
                if (v.GetDisplayName().IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) != -1)
                {
                    if (c != 0)
                        sb.Append(", ");
                    sb.Append(v.Name);
                    if (++c > 12) break;
                }
            }
        }
        finally
        {
            singleton._threadLocker.Release();
        }
        return sb.ToString();
    }
    public static float GetDefaultTeamLimit(Class @class) => @class switch
    {
        Class.HAT => 0.1f,
        _ => 1f
    };
    public override void Unload()
    {
        _isLoaded = false;
        _singleton = null!;
        PlayerLife.OnPreDeath -= PlayerLife_OnPreDeath;
    }
    public static void TryGiveKitOnJoinTeam(UCPlayer player)
    {
        ulong team = player.GetTeam();
        if (team is 1 or 2)
        {
            if (KitExists(team == 1 ? TeamManager.Team1UnarmedKit : TeamManager.Team2UnarmedKit, out KitOld unarmed))
                GiveKit(player, unarmed);
            else if (KitExists(TeamManager.DefaultKit, out unarmed))
                GiveKit(player, unarmed);
            else L.LogWarning("Unable to give " + player.CharacterName + " a kit.");
        }
        else if (KitExists(TeamManager.DefaultKit, out KitOld @default))
            GiveKit(player, @default);
        else L.LogWarning("Unable to give " + player.CharacterName + " a kit.");
    }
    private static void ReadToKit(MySqlDataReader reader, KitOld kit)
    {
        kit.PrimaryKey = reader.GetInt32(0);
        kit.Name = reader.GetString(1);
        kit.Class = (Class)reader.GetInt32(2);
        kit.Branch = (Branch)reader.GetInt32(3);
        kit.Team = reader.GetUInt64(4);
        kit.CreditCost = reader.GetUInt16(5);
        kit.UnlockLevel = reader.GetUInt16(6);
        kit.IsPremium = reader.GetBoolean(7);
        kit.PremiumCost = reader.GetFloat(8);
        kit.IsLoadout = reader.GetBoolean(9);
        kit.TeamLimit = reader.GetFloat(10);
        kit.Cooldown = reader.GetInt32(11);
        kit.Disabled = reader.GetBoolean(12);
        kit.Weapons = reader.GetString(13);
        kit.SquadLevel = (SquadLevel)reader.GetByte(14);
        kit.Items = new List<PageItem>(12);
        kit.Clothes = new List<ClothingItem>(5);
        kit.SignTexts = new Dictionary<string, string>(1);
        kit.UnlockRequirements = Array.Empty<BaseUnlockRequirement>();
        kit.Skillsets = Array.Empty<Skillset>();
    }
    private static void ReadToKitItem(MySqlDataReader reader, PageItem item)
    {
        lock (GUID_BUFFER)
        {
            reader.GetBytes(2, 0, GUID_BUFFER, 0, 16);
            item.Item = new Guid(GUID_BUFFER);
            item.X = reader.GetByte(3);
            item.Y = reader.GetByte(4);
            item.Rotation = reader.GetByte(5);
            item.Page = reader.GetByte(6);
            item.Amount = reader.GetByte(7);
            item.State = (byte[])reader[8];
        }
    }
    private static void ReadToKitClothing(MySqlDataReader reader, ClothingItem clothing)
    {
        lock (GUID_BUFFER)
        {
            reader.GetBytes(2, 0, GUID_BUFFER, 0, 16);
            clothing.Item = new Guid(GUID_BUFFER);
            clothing.Type = (ClothingType)reader.GetByte(3);
        }
    }
    private static Skillset ReadSkillset(MySqlDataReader reader)
    {
        EPlayerSpeciality type = (EPlayerSpeciality)reader.GetByte(2);
        Skillset set;
        byte v = reader.GetByte(3);
        byte lvl = reader.GetByte(4);
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
                return new Skillset(EPlayerOffense.OVERKILL, int.MinValue);
        }
        return set;
    }
    private static BaseUnlockRequirement? ReadUnlockRequirement(MySqlDataReader reader)
    {
        Utf8JsonReader jsonReader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(reader.GetString(1)));
        return BaseUnlockRequirement.Read(ref jsonReader);
    }
    public async Task RedownloadKit(string name)
    {
        bool wasNew = false;
        if (!KitExists(name, out KitOld kit))
        {
            kit = new KitOld(true);
            wasNew = true;
        }
        await _threadLocker.WaitAsync();
        try
        {
            await Data.AdminSql.QueryAsync("SELECT * FROM `kit_data` WHERE `InternalName` = @0;", new object[1] { name }, R =>
            {
                ReadToKit(R, kit);
            });

            if (wasNew && kit.PrimaryKey > 0)
            {
                Kits.Add(kit.PrimaryKey, kit);
            }
            else
            {
                L.LogWarning("Unable to find kit " + name + " to auto-update it.");
                return;
            }
            int pk = kit.PrimaryKey;
            object[] parameters = new object[] { pk };
            await Data.AdminSql.QueryAsync("SELECT * FROM `kit_items` WHERE `Kit` = @0;", parameters, R =>
            {
                PageItem item = new PageItem();
                ReadToKitItem(R, item);
                kit.Items.Add(item);
            }).ConfigureAwait(false);
            await Data.AdminSql.QueryAsync("SELECT * FROM `kit_clothes` WHERE `Kit` = @0;", parameters, R =>
            {
                ClothingItem clothing = new ClothingItem();
                ReadToKitClothing(R, clothing);
                kit.Clothes.Add(clothing);
            }).ConfigureAwait(false);
            await Data.AdminSql.QueryAsync("SELECT * FROM `kit_skillsets` WHERE `Kit` = @0;", parameters, R =>
            {
                Skillset set = ReadSkillset(R);
                if (set.Level != int.MinValue)
                    kit.AddSkillset(set);
                else
                    L.LogWarning("Invalid skillset for kit " + kit.Name!.ToString());
            }).ConfigureAwait(false);
            await Data.AdminSql.QueryAsync("SELECT * FROM `kit_lang` WHERE `Kit` = @0;", parameters, R =>
            {
                string lang = R.GetString(2);
                if (!kit.SignTexts.ContainsKey(lang))
                    kit.SignTexts.Add(lang, R.GetString(3));
                else
                    L.LogWarning("Duplicate translation for kit " + kit.Name + " (" + kit.PrimaryKey +
                                 ") for language " + lang);
            }).ConfigureAwait(false);
            await Data.AdminSql.QueryAsync("SELECT `Kit`, `JSON` FROM `kit_unlock_requirements` WHERE `Kit` = @0;", parameters, R =>
            {
                int kitPk = R.GetInt32(0);
                if (Kits.TryGetValue(kitPk, out KitOld kit))
                {
                    BaseUnlockRequirement? req = ReadUnlockRequirement(R);
                    if (req != null)
                        kit.AddUnlockRequirement(req);
                }
            }).ConfigureAwait(false);
            UpdateSigns(kit);
        }
        finally
        {
            _threadLocker.Release();
        }
    }
    public async Task ReloadKits()
    {
        SingletonEx.AssertLoaded<KitManager>(_isLoaded);
        await _threadLocker.WaitAsync();
        try
        {
            Kits.Clear();
            await Data.AdminSql.QueryAsync("SELECT * FROM `kit_data`;", Array.Empty<object>(), R =>
            {
                KitOld kit = new KitOld(true);
                ReadToKit(R, kit);
                Kits.Add(kit.PrimaryKey, kit);
            });
            await Data.AdminSql.QueryAsync("SELECT * FROM `kit_items`;", Array.Empty<object>(), R =>
            {
                int kitPk = R.GetInt32(1);
                if (Kits.TryGetValue(kitPk, out KitOld kit))
                {
                    PageItem item = new PageItem();
                    ReadToKitItem(R, item);
                    kit.Items.Add(item);
                }
            });
            await Data.AdminSql.QueryAsync("SELECT * FROM `kit_clothes`;", Array.Empty<object>(), R =>
            {
                int kitPk = R.GetInt32(1);
                if (Kits.TryGetValue(kitPk, out KitOld kit))
                {
                    ClothingItem clothing = new ClothingItem();
                    ReadToKitClothing(R, clothing);
                    kit.Clothes.Add(clothing);
                }
            });
            await Data.AdminSql.QueryAsync("SELECT * FROM `kit_skillsets`;", Array.Empty<object>(), R =>
            {
                int kitPk = R.GetInt32(1);
                if (Kits.TryGetValue(kitPk, out KitOld kit))
                {
                    Skillset set = ReadSkillset(R);
                    if (set.Level != int.MinValue)
                        kit.AddSkillset(set);
                    else
                        L.LogWarning("Invalid skillset for kit " + kitPk.ToString());
                }
            });
            await Data.AdminSql.QueryAsync("SELECT `Kit`, `Language`, `Text` FROM `kit_lang`;", Array.Empty<object>(), R =>
            {
                int kitPk = R.GetInt32(0);
                if (Kits.TryGetValue(kitPk, out KitOld kit))
                {
                    string lang = R.GetString(1);
                    if (!kit.SignTexts.ContainsKey(lang))
                        kit.SignTexts.Add(lang, R.GetString(2));
                    else
                        L.LogWarning("Duplicate translation for kit " + kit.Name + " (" + kit.PrimaryKey +
                                     ") for language " + lang);
                }
            });
            await Data.AdminSql.QueryAsync("SELECT `Kit`, `JSON` FROM `kit_unlock_requirements`;", Array.Empty<object>(), R =>
            {
                int kitPk = R.GetInt32(0);
                if (Kits.TryGetValue(kitPk, out KitOld kit))
                {
                    BaseUnlockRequirement? req = ReadUnlockRequirement(R);
                    if (req != null)
                        kit.AddUnlockRequirement(req);
                }
            });
        }
        finally
        {
            _threadLocker.Release();
        }
    }
    private void PlayerLife_OnPreDeath(PlayerLife life)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        SingletonEx.AssertLoaded<KitManager>(_isLoaded);
        if (HasKit(life.player.channel.owner.playerID.steamID, out KitOld kit))
        {
            for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
            {
                if (page == PlayerInventory.AREA)
                    continue;

                for (byte index = 0; index < life.player.inventory.getItemCount(page); index++)
                {
                    ItemJar jar = life.player.inventory.getItem(page, index);

                    if (Assets.find(EAssetType.ITEM, jar.item.id) is not ItemAsset asset) continue;
                    float percentage = (float)jar.item.amount / asset.amount;

                    bool notInKit = !kit.HasItemOfID(asset.GUID) && Whitelister.IsWhitelisted(asset.GUID, out _);
                    if (notInKit || (percentage < 0.3 && asset.type != EItemType.GUN))
                    {
                        if (notInKit)
                        {
                            ItemManager.dropItem(jar.item, life.player.transform.position, false, true, true);
                        }

                        life.player.inventory.removeItem(page, index);
                        index--;
                    }
                }
            }
        }
    }
    public static async Task<KitOld> AddKit(KitOld kit)
    {
        KitManager singleton = GetSingleton();
        if (kit != null)
        {
            await singleton._threadLocker.WaitAsync();
            try
            {
                bool hasPk = kit.PrimaryKey > -1;
                int pk = -1;
                await Data.DatabaseManager.QueryAsync(
                    "INSERT INTO `kit_data` (`InternalName`, `Class`, `Branch`, `Team`, `CreditCost`, " +
                    "`UnlockLevel`, `IsPremium`, `PremiumCost`, `IsLoadout`, `TeamLimit`, `Cooldown`, `Disabled`, `WeaponText`, `SquadLevel`" + (hasPk ? ", `pk`" : string.Empty) + ") VALUES " +
                    "(@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, @12, @13" + (hasPk ? ", @14" : string.Empty) + ")" +
                    "ON DUPLICATE KEY UPDATE " +
                    "`InternalName` = @0, `Class` = @1, `Branch` = @2, `Team` = @3, `CreditCost` = @4, `UnlockLevel` = @5, `IsPremium` = @6, `PremiumCost` = @7, `IsLoadout` = @8, " +
                    "`TeamLimit` = @9, `Cooldown` = @10, `Disabled` = @11, `WeaponText` = @12, `SquadLevel` = @13, `pk` = LAST_INSERT_ID(`pk`); " +
                    "SET @kitPk := (SELECT LAST_INSERT_ID() AS `pk`); " +
                    "DELETE FROM `kit_lang` WHERE `Kit` = @kitPk; " +
                    "DELETE FROM `kit_items` WHERE `Kit` = @kitPk; " +
                    "DELETE FROM `kit_clothes` WHERE `Kit` = @kitPk; " +
                    "DELETE FROM `kit_skillsets` WHERE `Kit` = @kitPk; " +
                    "DELETE FROM `kit_unlock_requirements` WHERE `Kit` = @kitPk; " +
                    "SELECT @kitPk;",
                    hasPk ? new object[15]
                    {
                        kit.Name,
                        (int)kit.Class,
                        (int)kit.Branch,
                        kit.Team,
                        kit.CreditCost,
                        kit.UnlockLevel,
                        kit.IsPremium,
                        kit.PremiumCost,
                        kit.IsLoadout,
                        kit.TeamLimit,
                        kit.Cooldown,
                        kit.Disabled,
                        kit.Weapons,
                        (byte)kit.SquadLevel,
                        kit.PrimaryKey
                    }
                    : new object[14]
                    {
                        kit.Name,
                        (int)kit.Class,
                        (int)kit.Branch,
                        kit.Team,
                        kit.CreditCost,
                        kit.UnlockLevel,
                        kit.IsPremium,
                        kit.PremiumCost,
                        kit.IsLoadout,
                        kit.TeamLimit,
                        kit.Cooldown,
                        kit.Disabled,
                        kit.Weapons,
                        (byte)kit.SquadLevel
                    }, R =>
                    {
                        pk = R.GetInt32(0);
                    });
                if (pk == -1) return null!;
                kit.PrimaryKey = pk;
                if (kit.Items.Count > 0)
                {
                    StringBuilder builder = new StringBuilder("INSERT INTO `kit_items` (`Kit`, `GUID`, `PosX`, `PosY`, `Rotation`, `Page`, `Amount`, `Metadata`) VALUES ", 512);
                    object[] objs = new object[kit.Items.Count * 8];
                    for (int i = 0; i < kit.Items.Count; ++i)
                    {
                        PageItem item = kit.Items[i];
                        if (i != 0)
                            builder.Append(", ");
                        builder.Append('(');
                        int index = i * 8;
                        for (int j = 0; j < 8; ++j)
                        {
                            if (j != 0)
                                builder.Append(", ");
                            builder.Append('@').Append(index + j);
                        }
                        objs[index++] = pk;
                        objs[index++] = item.Item.ToByteArray();
                        objs[index++] = item.X;
                        objs[index++] = item.Y;
                        objs[index++] = item.Rotation;
                        objs[index++] = item.Page;
                        objs[index++] = item.Amount;
                        objs[index++] = item.State;
                        builder.Append(')');
                    }
                    builder.Append(';');
                    await Data.DatabaseManager.NonQueryAsync(builder.ToString(), objs);
                }
                if (kit.Clothes.Count > 0)
                {
                    StringBuilder builder = new StringBuilder("INSERT INTO `kit_clothes` (`Kit`, `GUID`, `Type`) VALUES ", 128);
                    object[] objs = new object[kit.Clothes.Count * 3];
                    for (int i = 0; i < kit.Clothes.Count; ++i)
                    {
                        ClothingItem clothes = kit.Clothes[i];
                        if (i != 0)
                            builder.Append(", ");
                        builder.Append('(');
                        int index = i * 3;
                        for (int j = 0; j < 3; ++j)
                        {
                            if (j != 0)
                                builder.Append(", ");
                            builder.Append('@').Append(index + j);
                        }
                        objs[index++] = pk;
                        objs[index++] = clothes.Item.ToByteArray();
                        objs[index++] = (int)clothes.Type;
                        builder.Append(')');
                    }
                    builder.Append(';');
                    await Data.DatabaseManager.NonQueryAsync(builder.ToString(), objs);
                }
                if (kit.SignTexts.Count > 0)
                {
                    StringBuilder builder = new StringBuilder("INSERT INTO `kit_lang` (`Kit`, `Language`, `Text`) VALUES ", 128);
                    object[] objs = new object[kit.SignTexts.Count * 3];
                    int i = 0;
                    foreach (KeyValuePair<string, string> pair in kit.SignTexts)
                    {
                        if (pair.Key.Length != 5) continue;
                        if (i != 0)
                            builder.Append(", ");
                        builder.Append('(');
                        int index = i * 3;
                        for (int j = 0; j < 3; ++j)
                        {
                            if (j != 0)
                                builder.Append(", ");
                            builder.Append('@').Append(index + j);
                        }
                        objs[index++] = pk;
                        objs[index++] = pair.Key;
                        objs[index++] = pair.Value;
                        builder.Append(')');
                        ++i;
                    }
                    builder.Append(';');
                    await Data.DatabaseManager.NonQueryAsync(builder.ToString(), objs);
                }
                if (kit.Skillsets.Length > 0)
                {
                    StringBuilder builder = new StringBuilder("INSERT INTO `kit_skillsets` (`Kit`, `Type`, `Skill`, `Level`) VALUES ", 128);
                    object[] objs = new object[kit.Skillsets.Length * 4];
                    for (int i = 0; i < kit.Skillsets.Length; ++i)
                    {
                        Skillset set = kit.Skillsets[i];
                        if (i != 0)
                            builder.Append(", ");
                        builder.Append('(');
                        int index = i * 4;
                        for (int j = 0; j < 4; ++j)
                        {
                            if (j != 0)
                                builder.Append(", ");
                            builder.Append('@').Append(index + j);
                        }
                        objs[index++] = pk;
                        objs[index++] = (byte)set.Speciality;
                        objs[index++] = (byte)set.SkillIndex;
                        objs[index++] = (byte)set.Level;
                        builder.Append(')');
                    }
                    builder.Append(';');
                    await Data.DatabaseManager.NonQueryAsync(builder.ToString(), objs);
                }
                if (kit.UnlockRequirements.Length > 0)
                {
                    StringBuilder builder = new StringBuilder("INSERT INTO `kit_unlock_requirements` (`Kit`, `JSON`) VALUES ", 128);
                    object[] objs = new object[kit.UnlockRequirements.Length * 2];
                    using (MemoryStream str = new MemoryStream(128))
                    {
                        for (int i = 0; i < kit.UnlockRequirements.Length; ++i)
                        {
                            if (i != 0)
                                builder.Append(", ");
                            builder.Append('(');
                            int index = i * 3;
                            for (int j = 0; j < 2; ++j)
                            {
                                if (j != 0)
                                    builder.Append(", ");
                                builder.Append('@').Append(index + j);
                            }
                            objs[index++] = pk;
                            Utf8JsonWriter writer = new Utf8JsonWriter(str, JsonEx.condensedWriterOptions);
                            BaseUnlockRequirement.Write(writer, kit.UnlockRequirements[i]);
                            writer.Dispose();
                            objs[index++] = System.Text.Encoding.UTF8.GetString(str.ToArray());
                            str.Position = 0;
                            str.SetLength(0);
                            builder.Append(')');
                        }
                    }
                    builder.Append(';');
                    await Data.DatabaseManager.NonQueryAsync(builder.ToString(), objs);
                }
                if (singleton.Kits.ContainsKey(pk))
                    singleton.Kits[pk] = kit;
                else
                    singleton.Kits.Add(pk, kit);
            }
            finally
            {
                singleton._threadLocker.Release();
            }
        }

        return kit!;
    }
    public static async Task<bool> GiveAccess(KitOld kit, ulong player, EKitAccessType type)
    {
        bool res;
        if (kit.PrimaryKey != -1)
        {
            res = await GiveAccess(kit.PrimaryKey, player, type);
        }
        else
        {
            res = await GiveAccess(kit.Name, player, type);
        }
        if (res)
        {
            UCPlayer? pl = UCPlayer.FromID(player);
            if (pl != null)
            {
                if (pl.AccessibleKits is null)
                    pl.AccessibleKits = new List<string>(1) { kit.Name };
                else if (!pl.AccessibleKits.Exists(x => x.Equals(kit.Name, StringComparison.Ordinal)))
                    pl.AccessibleKits.Add(kit.Name);
                if ((kit.IsPremium || kit.IsLoadout) && Data.Is(out ITeams teams) && teams.UseTeamSelector && teams.TeamSelector is not null)
                    teams.TeamSelector.OnKitsUpdated(pl);
            }
        }
        return res;
    }
    public static async Task<bool> GiveAccess(KitOld kit, UCPlayer player, EKitAccessType type)
    {
        bool res;
        if (kit.PrimaryKey != -1)
        {
            res = await GiveAccess(kit.PrimaryKey, player.Steam64, type);
        }
        else
        {
            res = await GiveAccess(kit.Name, player.Steam64, type);
        }
        if (res)
        {
            if (player != null)
            {
                if (player.AccessibleKits is null)
                    player.AccessibleKits = new List<string>(1) { kit.Name };
                else if (!player.AccessibleKits.Exists(x => x.Equals(kit.Name, StringComparison.Ordinal)))
                    player.AccessibleKits.Add(kit.Name);
                if ((kit.IsPremium || kit.IsLoadout) && Data.Is(out ITeams teams) && teams.UseTeamSelector && teams.TeamSelector is not null)
                    teams.TeamSelector.OnKitsUpdated(player);
            }
        }
        return res;
    }
    /// <summary>Does not add to <see cref="UCPlayer.AccessibleKits"/>. Use <see cref="GiveAccess(KitOld, ulong, EKitAccessType)"/> or <see cref="GiveAccess(KitOld, UCPlayer, EKitAccessType)"/> instead.</summary>
    public static async Task<bool> GiveAccess(int primaryKey, ulong player, EKitAccessType type)
    {
        return await Data.DatabaseManager.NonQueryAsync("INSERT INTO `kit_access` (`Kit`, `Steam64`, `AccessType`) VALUES (@0, @1, @2);", new object[3]
        {
            primaryKey, player, type.ToString()
        }) > 0;
    }
    /// <summary>Does not add to <see cref="UCPlayer.AccessibleKits"/>. Use <see cref="GiveAccess(KitOld, ulong, EKitAccessType)"/> or <see cref="GiveAccess(KitOld, UCPlayer, EKitAccessType)"/> instead.</summary>
    public static async Task<bool> GiveAccess(string name, ulong player, EKitAccessType type)
    {
        return await Data.DatabaseManager.NonQueryAsync("SET @pk := (SELECT `pk` FROM `kit_data` WHERE `InternalName` = @0 LIMIT 1); INSERT INTO `kit_access` (`Kit`, `Steam64`, `AccessType`) VALUES (@pk, @1, @2) WHERE @pk IS NOT NULL;", new object[3]
        {
            name, player, type.ToString()
        }) > 0;
    }
    public static async Task<bool> RemoveAccess(KitOld kit, UCPlayer player)
    {
        bool res;
        if (kit.PrimaryKey != -1)
        {
            res = await RemoveAccess(kit.PrimaryKey, player.Steam64);
        }
        else
        {
            res = await RemoveAccess(kit.Name, player.Steam64);
        }
        if (res)
        {
            UCWarfare.RunOnMainThread(() =>
            {
                player.AccessibleKits?.RemoveAll(x => x.Equals(kit.Name, StringComparison.Ordinal));
                if ((kit.IsPremium || kit.IsLoadout) && Data.Is(out ITeams teams) && teams.UseTeamSelector && teams.TeamSelector is not null)
                    teams.TeamSelector.OnKitsUpdated(player);
            });
        }
        return res;
    }
    public static async Task<bool> RemoveAccess(KitOld kit, ulong player)
    {
        bool res;
        if (kit.PrimaryKey != -1)
        {
            res = await RemoveAccess(kit.PrimaryKey, player);
        }
        else
        {
            res = await RemoveAccess(kit.Name, player);
        }
        if (res)
        {
            UCPlayer? pl = UCPlayer.FromID(player);
            if (pl is not null)
            {
                pl.AccessibleKits?.RemoveAll(x => x.Equals(kit.Name, StringComparison.Ordinal));
                if ((kit.IsPremium || kit.IsLoadout) && Data.Is(out ITeams teams) && teams.UseTeamSelector && teams.TeamSelector is not null)
                    teams.TeamSelector.OnKitsUpdated(pl);
            }
        }
        return res;
    }
    /// <summary>Does not remove from <see cref="UCPlayer.AccessibleKits"/>. Use <see cref="RemoveAccess(KitOld, ulong)"/> or <see cref="RemoveAccess(KitOld, UCPlayer)"/> instead.</summary>
    public static async Task<bool> RemoveAccess(int primaryKey, ulong player)
    {
        return await Data.DatabaseManager.NonQueryAsync("DELETE FROM `kit_access` WHERE `Steam64` = @0 AND `Kit` = @1;", new object[2]
        {
            player, primaryKey
        }) > 0;
    }
    /// <summary>Does not remove from <see cref="UCPlayer.AccessibleKits"/>. Use <see cref="RemoveAccess(KitOld, ulong)"/> or <see cref="RemoveAccess(KitOld, UCPlayer)"/> instead.</summary>
    public static async Task<bool> RemoveAccess(string name, ulong player)
    {
        return await Data.DatabaseManager.NonQueryAsync("SET @pk := (SELECT `pk` FROM `kit_data` WHERE `InternalName` = @0 LIMIT 1); DELETE FROM `kit_access` WHERE `Steam64` = @1 AND `Kit` = @pk;", new object[2]
        {
            name, player
        }) > 0;
    }
    public static Task<bool> HasAccess(KitOld kit, ulong player)
    {
        if (kit.PrimaryKey != -1)
        {
            return HasAccess(kit.PrimaryKey, player);
        }
        else
        {
            return HasAccess(kit.Name, player);
        }
    }
    public static async Task<bool> HasAccess(int primaryKey, ulong player)
    {
        int ct = 0;
        await Data.DatabaseManager.QueryAsync("SELECT COUNT(`Steam64`) FROM `kit_access` WHERE `Steam64` = @0 AND `Kit` = @1;", new object[2]
        {
            player, primaryKey
        }, R => { ct = R.GetInt32(0); });
        return ct > 0;
    }
    public static async Task<bool> HasAccess(string name, ulong player)
    {
        int ct = 0;
        await Data.DatabaseManager.QueryAsync("SET @pk := (SELECT `pk` FROM `kit_data` WHERE `InternalName` = @1 LIMIT 1); SELECT COUNT(`Steam64`) FROM `kit_access` WHERE `Steam64` = @0 AND `Kit` = @pk;", new object[2]
        {
            player, name
        }, R => { ct = R.GetInt32(0); });
        return ct > 0;
    }
    public static bool HasAccessFast(KitOld kit, UCPlayer player)
    {
        return player != null && player.AccessibleKits != null && player.AccessibleKits.Exists(x => x.Equals(kit.Name, StringComparison.Ordinal));
    }
    public static bool HasAccessFast(string name, UCPlayer player)
    {
        if (string.IsNullOrEmpty(name)) return false;
        return player != null && player.AccessibleKits != null && player.AccessibleKits.Exists(x => x.Equals(name, StringComparison.Ordinal));
    }
    public static Task<bool> DeleteKit(KitOld kit)
    {
        if (kit.PrimaryKey != -1)
        {
            return DeleteKit(kit.PrimaryKey);
        }
        else
        {
            return DeleteKit(kit.Name);
        }
    }
    public static async Task<bool> DeleteKit(int primaryKey)
    {
        if (primaryKey == -1) return false;
        KitManager singleton = GetSingleton();
        await singleton._threadLocker.WaitAsync();
        singleton.Kits.Remove(primaryKey);
        singleton._threadLocker.Release();
        DequipKit(primaryKey);
        return await Data.DatabaseManager.NonQueryAsync("DELETE FROM `kit_data` WHERE `pk` = @0;", new object[1] { primaryKey }) > 0;
    }
    public static void DequipKit(string name)
    {
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            if (pl.Kit is not null && pl.Kit.Name.Equals(name, StringComparison.Ordinal))
                TryGiveRiflemanKit(pl);
        }
    }
    public static void DequipKit(int primaryKey)
    {
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            if (pl.Kit is not null && pl.Kit.PrimaryKey == primaryKey)
                TryGiveRiflemanKit(pl);
        }
    }
    public static async Task<bool> DeleteKit(string name)
    {
        KitManager singleton = GetSingleton();
        await singleton._threadLocker.WaitAsync();
        KeyValuePair<int, KitOld> fod = singleton.Kits.FirstOrDefault(x => x.Value.Name.Equals(name, StringComparison.Ordinal));
        if (fod.Value != null)
            singleton.Kits.Remove(fod.Key);
        singleton._threadLocker.Release();
        DequipKit(name);
        return await Data.DatabaseManager.NonQueryAsync("SET @pk := (SELECT `pk` FROM `kit_data` WHERE `InternalName` = @0 LIMIT 1); " +
            "DELETE FROM `kit_data` WHERE @pk IS NOT NULL AND `pk` = @pk;", new object[1]
        {
            name
        }) > 0;
    }
    internal static void ApplyKitChange(KitOld kit)
    {
        KitManager singleton = GetSingleton();
        singleton._threadLocker.Wait();
        for (int i = 0; i < singleton.Kits.Count; ++i)
        {
            if (singleton.Kits[i].Name.Equals(kit.Name, StringComparison.OrdinalIgnoreCase))
            {
                KitOld kit2 = singleton.Kits[i];
                kit.ApplyTo(kit2);
                UpdateSigns(kit2);
                goto rtn;
            }
        }
        singleton.Kits.Add(kit.PrimaryKey, kit);
        UpdateSigns(kit);
    rtn:
        singleton._threadLocker.Release();
    }
    internal static KitOld? GetRandomPublicKit()
    {
        List<KitOld> selection = GetAllPublicKits();
        if (selection.Count == 0) return null;
        return selection[UnityEngine.Random.Range(0, selection.Count)];
    }
    internal static List<KitOld> GetAllPublicKits()
    {
        return GetKitsWhere(x => !x.IsPremium && !x.IsLoadout && !x.Disabled && x.Class > Class.Unarmed);
    }
    public static List<KitOld> GetKitsWhere(Func<KitOld, bool> predicate)
    {
        KitManager singleton = GetSingleton();
        singleton._threadLocker.Wait();
        List<KitOld> rtn = singleton.Kits.Values.Where(predicate).ToList();
        singleton._threadLocker.Release();
        return rtn;
    }
    public static bool KitExists(string kitName, out KitOld kit)
    {
        KitManager singleton = GetSingleton();
        singleton._threadLocker.Wait();
        kit = singleton.Kits.Values.FirstOrDefault(x => x.Name.Equals(kitName, StringComparison.Ordinal))!;
        singleton._threadLocker.Release();
        return kit != null;
    }
    public static bool KitExists(Func<KitOld, bool> predicate, out KitOld kit)
    {
        KitManager singleton = GetSingleton();
        singleton._threadLocker.Wait();
        kit = singleton.Kits.Values.FirstOrDefault(predicate)!;
        singleton._threadLocker.Release();
        return kit != null;
    }
    /// <exception cref="SingletonUnloadedException"/>
    internal static KitManager GetSingleton()
    {
        if (_singleton is null)
        {
            _singleton = Data.Singletons.GetSingleton<KitManager>()!;
            if (_singleton is null)
                throw new SingletonUnloadedException(typeof(KitManager));
        }
        return _singleton;
    }
    public static List<PageItem> ItemsFromInventory(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        List<PageItem> items = new List<PageItem>();

        for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
        {
            for (byte i = 0; i < player.Player.inventory.getItemCount(page); i++)
            {
                ItemJar jar = player.Player.inventory.getItem(page, i);
                if (Assets.find(EAssetType.ITEM, jar.item.id) is ItemAsset asset)
                {
                    items.Add(new PageItem(
                        TeamManager.GetRedirectGuid(asset.GUID),
                        jar.x,
                        jar.y,
                        jar.rot,
                        jar.item.metadata,
                        jar.item.amount,
                        page
                    ));
                }
            }
        }

        return items;
    }
    public static List<ClothingItem> ClothesFromInventory(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        PlayerClothing playerClothes = player.Player.clothing;
        List<ClothingItem> clothes = new List<ClothingItem>(7);

        if (playerClothes.shirtAsset != null)
            clothes.Add(new ClothingItem(TeamManager.GetClothingRedirectGuid(playerClothes.shirtAsset.GUID), ClothingType.Shirt));
        if (playerClothes.pantsAsset != null)
            clothes.Add(new ClothingItem(TeamManager.GetClothingRedirectGuid(playerClothes.pantsAsset.GUID), ClothingType.Pants));
        if (playerClothes.vestAsset != null)
            clothes.Add(new ClothingItem(TeamManager.GetClothingRedirectGuid(playerClothes.vestAsset.GUID), ClothingType.Vest));
        if (playerClothes.hatAsset != null)
            clothes.Add(new ClothingItem(TeamManager.GetClothingRedirectGuid(playerClothes.hatAsset.GUID), ClothingType.Hat));
        if (playerClothes.maskAsset != null)
            clothes.Add(new ClothingItem(TeamManager.GetClothingRedirectGuid(playerClothes.maskAsset.GUID), ClothingType.Mask));
        if (playerClothes.backpackAsset != null)
            clothes.Add(new ClothingItem(TeamManager.GetClothingRedirectGuid(playerClothes.backpackAsset.GUID), ClothingType.Backpack));
        if (playerClothes.glassesAsset != null)
            clothes.Add(new ClothingItem(TeamManager.GetClothingRedirectGuid(playerClothes.glassesAsset.GUID), ClothingType.Glasses));

        return clothes;
    }
    public static void OnPlayerJoinedQuestHandling(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        KitManager singleton = GetSingleton();
        singleton._threadLocker.Wait();
        try
        {
            foreach (KitOld kit in singleton.Kits.Values)
            {
                if (!kit.IsLoadout && kit.UnlockRequirements != null)
                {
                    for (int j = 0; j < kit.UnlockRequirements.Length; j++)
                    {
                        if (kit.UnlockRequirements[j] is QuestUnlockRequirement req && req.UnlockPresets != null && req.UnlockPresets.Length > 0 && !req.CanAccess(player))
                        {
                            if (Assets.find(req.QuestID) is QuestAsset quest)
                            {
                                player.Player.quests.sendAddQuest(quest.id);
                            }
                            else
                            {
                                L.LogWarning("Unknown quest id " + req.QuestID + " in kit requirement for " + kit.Name);
                            }
                            for (int r = 0; r < req.UnlockPresets.Length; r++)
                            {
                                BaseQuestTracker? tracker = QuestManager.CreateTracker(player, req.UnlockPresets[r]);
                                if (tracker == null)
                                {
                                    L.LogWarning("Failed to create tracker for kit " + kit.Name + ", player " + player.Name.PlayerName);
                                }
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            singleton._threadLocker.Release();
        }
        RequestSigns.UpdateAllSigns(player);
    }
    public static bool OnQuestCompleted(QuestCompleted e)
    {
        KitManager singleton = GetSingleton();
        bool affectedKit = false;
        singleton._threadLocker.Wait();
        try
        {
            foreach (KitOld kit in singleton.Kits.Values)
            {
                if (!kit.IsLoadout && kit.UnlockRequirements != null)
                {
                    for (int j = 0; j < kit.UnlockRequirements.Length; j++)
                    {
                        if (kit.UnlockRequirements[j] is QuestUnlockRequirement req && req.UnlockPresets != null && req.UnlockPresets.Length > 0 && !req.CanAccess(e.Player))
                        {
                            for (int r = 0; r < req.UnlockPresets.Length; r++)
                            {
                                if (req.UnlockPresets[r] == e.PresetKey)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            singleton._threadLocker.Release();
        }
        return affectedKit;
    }
    public static void GiveKit(UCPlayer player, KitOld kit)
    {
        if (kit == null)
            return;
        ulong team = player.GetTeam();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (HasKit(player, out KitOld oldKit))
        {
            if (oldKit.Skillsets != null)
            {
                for (int i = 0; i < oldKit.Skillsets.Length; i++)
                {
                    ref Skillset skillset = ref kit.Skillsets[i];
                    for (int j = 0; j < Skillset.DEFAULT_SKILLSETS.Length; j++)
                    {
                        ref Skillset skillset2 = ref Skillset.DEFAULT_SKILLSETS[j];
                        if (skillset2.TypeEquals(in skillset))
                        {
                            for (int k = 0; k < kit.Skillsets.Length; k++)
                            {
                                ref Skillset skillset3 = ref kit.Skillsets[j];
                                if (skillset2 == skillset3) goto next;
                            }
                            skillset2.ServerSet(player);
                            goto next;
                        }
                    }
                    player.Player.skills.ServerSetSkillLevel(skillset.SpecialityIndex, skillset.SkillIndex, 0);
                next:;
                }
            }
        }
        if (kit.Skillsets != null)
        {
            for (int i = 0; i < kit.Skillsets.Length; i++)
            {
                ref Skillset skillset = ref kit.Skillsets[i];
                skillset.ServerSet(player);
            }
        }
        UCInventoryManager.ClearInventory(player, !Data.UseFastKits);

        if (Data.UseFastKits)
        {
            UCInventoryManager.LoadClothes(player, kit.Clothes);

            Items[] p = player.Player.inventory.items;
            bool ohi = Data.GetOwnerHasInventory(player.Player.inventory);
            if (ohi)
                Data.SetOwnerHasInventory(player.Player.inventory, false);
            for (int i = 0; i < kit.Items.Count; ++i)
            {
                PageItem item = kit.Items[i];
                if (item.Page < PlayerInventory.PAGES - 2 && Assets.find(TeamManager.CheckAssetRedirect(item.Item, team)) is ItemAsset asset)
                    p[item.Page].addItem(item.X, item.Y, item.Rotation, new Item(asset.id, item.Amount, 100, Util.CloneBytes(item.State)));
                else
                    L.LogWarning("Invalid item {" + item.Item.ToString("N") + "} in kit " + kit.Name + " for team " + team);
            }
            if (ohi)
                Data.SetOwnerHasInventory(player.Player.inventory, true);
            UCInventoryManager.SendPages(player);
        }
        else
        {
            foreach (ClothingItem clothing in kit.Clothes)
            {
                if (Assets.find(TeamManager.CheckClothingAssetRedirect(clothing.Item, team)) is ItemAsset asset)
                {
                    if (clothing.Type == ClothingType.Shirt)
                        player.Player.clothing.askWearShirt(asset.id, 100, asset.getState(true), true);
                    else if (clothing.Type == ClothingType.Pants)
                        player.Player.clothing.askWearPants(asset.id, 100, asset.getState(true), true);
                    else if (clothing.Type == ClothingType.Vest)
                        player.Player.clothing.askWearVest(asset.id, 100, asset.getState(true), true);
                    else if (clothing.Type == ClothingType.Hat)
                        player.Player.clothing.askWearHat(asset.id, 100, asset.getState(true), true);
                    else if (clothing.Type == ClothingType.Mask)
                        player.Player.clothing.askWearMask(asset.id, 100, asset.getState(true), true);
                    else if (clothing.Type == ClothingType.Backpack)
                        player.Player.clothing.askWearBackpack(asset.id, 100, asset.getState(true), true);
                    else if (clothing.Type == ClothingType.Glasses)
                        player.Player.clothing.askWearGlasses(asset.id, 100, asset.getState(true), true);
                }
            }

            foreach (PageItem k in kit.Items)
            {
                if (Assets.find(TeamManager.CheckAssetRedirect(k.Item, team)) is ItemAsset asset)
                {
                    Item item = new Item(asset.id, k.Amount, 100, Util.CloneBytes(k.State));
                    if (!player.Player.inventory.tryAddItem(item, k.X, k.Y, k.Page, k.Rotation))
                        if (player.Player.inventory.tryAddItem(item, true))
                            ItemManager.dropItem(item, player.Position, true, true, true);
                }
                else
                    L.LogWarning("Invalid item {" + k.Item.ToString("N") + "} in kit " + kit.Name + " for team " + team);
            }
        }
        string oldkit = player.KitName;

        if (Data.Is(out IRevives g))
        {
            if (player.KitClass == Class.Medic && kit.Class != Class.Medic)
            {
                g.ReviveManager.DeregisterMedic(player);
            }
            else if (kit.Class == Class.Medic)
            {
                g.ReviveManager.RegisterMedic(player);
            }
        }
        player.ChangeKit(kit);

        Branch oldBranch = player.Branch;

        player.Branch = kit.Branch;

        if (oldBranch != player.Branch)
        {
            //Points.OnBranchChanged(player, oldBranch, kit.Branch);
        }

        OnPlayersKitChanged?.Invoke(player, kit, oldkit);
        if (oldkit != null && oldkit != string.Empty)
            UpdateSigns(oldkit);
        UpdateSigns(kit);
    }
    public static void ResupplyKit(UCPlayer player, KitOld kit, bool ignoreAmmoBags = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        List<ItemJar> nonKitItems = new List<ItemJar>();

        for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
        {
            if (page == PlayerInventory.AREA)
                continue;

            byte count = player.Player.inventory.getItemCount(page);

            for (byte index = 0; index < count; index++)
            {
                ItemJar jar = player.Player.inventory.getItem(page, 0);
                if (Assets.find(EAssetType.ITEM, jar.item.id) is not ItemAsset asset) continue;
                if (!kit.HasItemOfID(asset.GUID) && Whitelister.IsWhitelisted(asset.GUID, out _))
                {
                    nonKitItems.Add(jar);
                }
                player.Player.inventory.removeItem(page, 0);
            }
        }

        for (int i = 0; i < kit.Clothes.Count; i++)
        {
            ClothingItem clothing = kit.Clothes[i];
            if (Assets.find(clothing.Item) is ItemAsset asset)
            {
                ushort old = 0;
                switch (clothing.Type)
                {
                    case ClothingType.Glasses:
                        if (player.Player.clothing.glasses != asset.id)
                        {
                            old = player.Player.clothing.glasses;
                            player.Player.clothing.askWearGlasses(asset.id, 100, asset.getState(true), true);
                        }
                        break;
                    case ClothingType.Hat:
                        if (player.Player.clothing.hat != asset.id)
                        {
                            old = player.Player.clothing.hat;
                            player.Player.clothing.askWearHat(asset.id, 100, asset.getState(true), true);
                        }
                        break;
                    case ClothingType.Backpack:
                        if (player.Player.clothing.backpack != asset.id)
                        {
                            old = player.Player.clothing.backpack;
                            player.Player.clothing.askWearBackpack(asset.id, 100, asset.getState(true), true);
                        }
                        break;
                    case ClothingType.Mask:
                        if (player.Player.clothing.mask != asset.id)
                        {
                            old = player.Player.clothing.mask;
                            player.Player.clothing.askWearMask(asset.id, 100, asset.getState(true), true);
                        }
                        break;
                    case ClothingType.Pants:
                        if (player.Player.clothing.pants != asset.id)
                        {
                            old = player.Player.clothing.pants;
                            player.Player.clothing.askWearPants(asset.id, 100, asset.getState(true), true);
                        }
                        break;
                    case ClothingType.Shirt:
                        if (player.Player.clothing.shirt != asset.id)
                        {
                            old = player.Player.clothing.shirt;
                            player.Player.clothing.askWearShirt(asset.id, 100, asset.getState(true), true);
                        }
                        break;
                    case ClothingType.Vest:
                        if (player.Player.clothing.vest != asset.id)
                        {
                            old = player.Player.clothing.vest;
                            player.Player.clothing.askWearVest(asset.id, 100, asset.getState(true), true);
                        }
                        break;
                }
                if (old != 0)
                    player.Player.inventory.removeItem(2, 0);
            }
        }
        foreach (PageItem i in kit.Items)
        {
            if (ignoreAmmoBags && Gamemode.Config.BarricadeAmmoBag.MatchGuid(i.Item))
                continue;
            if (Assets.find(i.Item) is ItemAsset itemasset)
            {
                Item item = new Item(itemasset.id, i.Amount, 100, Util.CloneBytes(i.State));

                if (!player.Player.inventory.tryAddItem(item, i.X, i.Y, i.Page, i.Rotation))
                    player.Player.inventory.tryAddItem(item, true);
            }
        }

        foreach (ItemJar jar in nonKitItems)
        {
            player.Player.inventory.tryAddItem(jar.item, true);
        }
    }
    public static bool TryGiveUnarmedKit(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        string unarmedKit = string.Empty;
        if (player.IsTeam1)
            unarmedKit = TeamManager.Team1UnarmedKit;
        else if (player.IsTeam2)
            unarmedKit = TeamManager.Team2UnarmedKit;

        if (KitExists(unarmedKit, out KitOld kit))
        {
            GiveKit(player, kit);
            return true;
        }
        return false;
    }
    public static bool TryGiveRiflemanKit(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong t = player.GetTeam();
        KitOld rifleman = GetKitsWhere(k =>
                !k.Disabled &&
                k.Team == t &&
                k.Class == Class.Rifleman &&
                !k.IsPremium &&
                !k.IsLoadout &&
                k.TeamLimit == 1 &&
                k.UnlockRequirements.Length == 0
            ).FirstOrDefault();

        if (rifleman != null)
        {
            GiveKit(player, rifleman);
            return true;
        }
        return false;
    }
    public static bool HasKit(ulong steamID, out KitOld kit)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer? player = UCPlayer.FromID(steamID);

        if (player == null)
        {
            PlayerSave? save = PlayerManager.GetSave(steamID);
            if (save == null)
            {
                kit = null!;
                return false;
            }
            KitExists(save.KitName, out kit);
            return kit != null;
        }
        else
        {
            KitExists(player.KitName, out kit);
            return kit != null;
        }
    }
    public static bool HasKit(UCPlayer player, out KitOld kit)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        kit = player.Kit!;
        return player.Kit != null;
    }
    public static bool HasKit(SteamPlayer player, out KitOld kit) => HasKit(player.playerID.steamID.m_SteamID, out kit);
    public static bool HasKit(Player player, out KitOld kit) => HasKit(player.channel.owner.playerID.steamID.m_SteamID, out kit);
    public static bool HasKit(CSteamID player, out KitOld kit) => HasKit(player.m_SteamID, out kit);
    public static void UpdateText(KitOld kit, string text, string language = L.DEFAULT, bool updateSigns = true)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        kit.SignTexts.Remove(language);
        kit.SignTexts.Add(language, text);
        if (updateSigns && UCWarfare.IsMainThread)
            UpdateSigns(kit);
    }
    public static void UpdateSigns()
    {
        if (UCWarfare.IsMainThread)
            UpdateSignsIntl(null);
        else
            UCWarfare.RunOnMainThread(() => UpdateSignsIntl(null));
    }
    public static void UpdateSigns(UCPlayer player)
    {
        if (UCWarfare.IsMainThread)
            UpdateSignsIntl(player);
        else
            UCWarfare.RunOnMainThread(() => UpdateSignsIntl(player));
    }
    public static void UpdateSigns(KitOld kit)
    {
        if (UCWarfare.IsMainThread)
            UpdateSignsIntl(kit, null);
        else
            UCWarfare.RunOnMainThread(() => UpdateSignsIntl(kit, null));
    }
    public static void UpdateSigns(KitOld kit, UCPlayer player)
    {
        if (UCWarfare.IsMainThread)
            UpdateSignsIntl(kit, player);
        else
            UCWarfare.RunOnMainThread(() => UpdateSignsIntl(kit, player));
    }
    public static void UpdateSigns(string kitId)
    {
        if (UCWarfare.IsMainThread)
            UpdateSignsIntl(kitId, null);
        else
            UCWarfare.RunOnMainThread(() => UpdateSignsIntl(kitId, null));
    }
    public static void UpdateSigns(string kitId, UCPlayer player)
    {
        if (UCWarfare.IsMainThread)
            UpdateSignsIntl(kitId, player);
        else
            UCWarfare.RunOnMainThread(() => UpdateSignsIntl(kitId, player));
    }
    private static void UpdateSignsIntl(UCPlayer? player)
    {
        if (RequestSigns.Loaded)
        {
            if (player is null)
            {
                for (int i = 0; i < RequestSigns.Singleton.Count; i++)
                {
                    RequestSigns.Singleton[i].InvokeUpdate();
                }
            }
            else
            {
                for (int i = 0; i < RequestSigns.Singleton.Count; i++)
                {
                    RequestSigns.Singleton[i].InvokeUpdate(player);
                }
            }
        }
    }
    private static void UpdateSignsIntl(KitOld kit, UCPlayer? player)
    {
        if (RequestSigns.Loaded)
        {
            if (kit.IsLoadout)
            {
                if (player is null)
                {
                    for (int i = 0; i < RequestSigns.Singleton.Count; i++)
                    {
                        if (RequestSigns.Singleton[i].KitName.StartsWith("loadout_", StringComparison.OrdinalIgnoreCase))
                            RequestSigns.Singleton[i].InvokeUpdate();
                    }
                }
                else
                {
                    for (int i = 0; i < RequestSigns.Singleton.Count; i++)
                    {
                        if (RequestSigns.Singleton[i].KitName.StartsWith("loadout_", StringComparison.OrdinalIgnoreCase))
                            RequestSigns.Singleton[i].InvokeUpdate(player);
                    }
                }
            }
            else
            {
                UpdateSignsIntl(kit.Name, player);
            }
        }
    }
    private static void UpdateSignsIntl(string kitId, UCPlayer? player)
    {
        if (RequestSigns.Loaded)
        {
            if (player is null)
            {
                for (int i = 0; i < RequestSigns.Singleton.Count; i++)
                {
                    if (RequestSigns.Singleton[i].KitName.Equals(kitId, StringComparison.Ordinal))
                        RequestSigns.Singleton[i].InvokeUpdate();
                }
            }
            else
            {
                for (int i = 0; i < RequestSigns.Singleton.Count; i++)
                {
                    if (RequestSigns.Singleton[i].KitName.Equals(kitId, StringComparison.Ordinal))
                        RequestSigns.Singleton[i].InvokeUpdate(player);
                }
            }
        }
    }
    public static async Task<char> GetLoadoutCharacter(ulong playerId)
    {
        char let = 'a';
        await Data.DatabaseManager.QueryAsync("SELECT `InternalName` FROM `kit_data` WHERE `InternalName` LIKE @0 ORDER BY `InternalName`;", new object[1]
        {
            playerId.ToString() + "_%"
        }, R =>
        {
            string name = R.GetString(0);
            if (name.Length < 19)
                return;
            char let2 = name[18];
            if (let2 == let)
                let++;
        });
        return let;
    }
    internal async Task<(KitOld?, int)> CreateLoadout(ulong fromPlayer, ulong player, ulong team, Class @class, string displayName)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        char let = await GetLoadoutCharacter(player);
        string loadoutName = player.ToString() + "_" + let;

        if (KitExists(loadoutName, out _))
            return (null, 555);
        else
        {
            List<PageItem> items;
            List<ClothingItem> clothes;
            if (team is 1 or 2 && KitExists(team == 1 ? TeamManager.Team1UnarmedKit : TeamManager.Team2UnarmedKit, out KitOld unarmedKit))
            {
                items = unarmedKit.Items.ToList();
                clothes = unarmedKit.Clothes.ToList();
            }
            else
            {
                items = new List<PageItem>(0);
                clothes = new List<ClothingItem>(0);
            }

            KitOld loadout = new KitOld(loadoutName, items, clothes);

            loadout.IsLoadout = true;
            loadout.Team = team;
            loadout.Class = @class;
            if (@class == Class.Pilot)
                loadout.Branch = Branch.Airforce;
            else if (@class == Class.Crewman)
                loadout.Branch = Branch.Armor;
            else
                loadout.Branch = Branch.Infantry;

            if (@class == Class.HAT)
                loadout.TeamLimit = 0.1F;
            await UCWarfare.ToUpdate();
            UpdateText(loadout, displayName);

            await AddKit(loadout);

            ActionLogger.Add(EActionLogType.CREATE_KIT, loadoutName, fromPlayer);

            return (loadout, 0);
        }
    }
    internal static void InvokeKitCreated(KitOld kit)
    {
        OnKitCreated?.Invoke(kit);
        if (UCWarfare.Config.EnableSync)
        {
            KitSync.OnKitUpdated(kit.Name);
        }
    }

    internal static void InvokeKitDeleted(KitOld kit)
    {
        OnKitDeleted?.Invoke(kit);
        if (UCWarfare.Config.EnableSync)
        {
            KitSync.OnKitDeleted(kit.Name);
        }
    }

    internal static void InvokeKitUpdated(KitOld kit)
    {
        OnKitUpdated?.Invoke(kit);
        if (UCWarfare.Config.EnableSync)
        {
            KitSync.OnKitUpdated(kit.Name);
        }
    }

    internal static void InvokeKitAccessUpdated(KitOld kit, ulong player)
    {
        OnKitAccessChanged?.Invoke(kit, player);
        if (UCWarfare.Config.EnableSync)
        {
            KitSync.OnAccessChanged(player);
        }
    }
}

public delegate void KitCallback(KitOld kit);
public delegate void KitAccessCallback(KitOld kit, ulong player);
public delegate void KitChangedCallback(UCPlayer player, KitOld kit, string oldKit);
public static class KitEx
{
    public const int BRANCH_MAX_CHAR_LIMIT = 16;
    public const int CLOTHING_MAX_CHAR_LIMIT = 16;
    public const int CLASS_MAX_CHAR_LIMIT = 20;
    public const int TYPE_MAX_CHAR_LIMIT = 16;
    public const int REDIRECT_TYPE_CHAR_LIMIT = 16;
    public const int SQUAD_LEVEL_MAX_CHAR_LIMIT = 16;
    public const int KIT_NAME_MAX_CHAR_LIMIT = 25;
    public const int WEAPON_TEXT_MAX_CHAR_LIMIT = 50;
    public static bool HasItemOfID(this KitOld kit, Guid ID) => kit.Items.Exists(i => i.Item == ID);
    public static bool IsLimited(this KitOld kit, out int currentPlayers, out int allowedPlayers, ulong team, bool requireCounts = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong Team = team == 1 || team == 2 ? team : kit.Team;
        currentPlayers = 0;
        allowedPlayers = 24;
        if (!requireCounts && (kit.IsPremium || kit.TeamLimit >= 1f))
            return false;
        IEnumerable<UCPlayer> friendlyPlayers = Team == 0 ? PlayerManager.OnlinePlayers : PlayerManager.OnlinePlayers.Where(k => k.GetTeam() == Team);
        allowedPlayers = (int)Math.Ceiling(kit.TeamLimit * friendlyPlayers.Count());
        currentPlayers = friendlyPlayers.Count(k => k.KitName == kit.Name);
        if (kit.IsPremium || kit.TeamLimit >= 1f)
            return false;
        return currentPlayers + 1 > allowedPlayers;
    }

    public static bool IsClassLimited(this KitOld kit, out int currentPlayers, out int allowedPlayers, ulong team, bool requireCounts = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong Team = team == 1 || team == 2 ? team : kit.Team;
        currentPlayers = 0;
        allowedPlayers = 24;
        if (!requireCounts && (kit.IsPremium || kit.TeamLimit >= 1f))
            return false;
        IEnumerable<UCPlayer> friendlyPlayers = Team == 0 ? PlayerManager.OnlinePlayers : PlayerManager.OnlinePlayers.Where(k => k.GetTeam() == Team);
        allowedPlayers = (int)Math.Ceiling(kit.TeamLimit * friendlyPlayers.Count());
        currentPlayers = friendlyPlayers.Count(k => k.KitClass == kit.Class);
        if (kit.IsPremium || kit.TeamLimit >= 1f)
            return false;
        return currentPlayers + 1 > allowedPlayers;
    }
    private static readonly FieldInfo[] fields = typeof(KitOld).GetFields(BindingFlags.Instance | BindingFlags.Public);
    public static ESetFieldResult SetProperty(KitOld kit, string property, string value, out FieldInfo? field)
    {
        field = null;
        if (kit is null) return ESetFieldResult.OBJECT_NOT_FOUND;
        if (property is null || value is null) return ESetFieldResult.FIELD_NOT_FOUND;
        field = GetField(property, out ESetFieldResult reason);
        if (field is not null && reason == ESetFieldResult.SUCCESS)
        {
            if (Util.TryParseAny(value, field.FieldType, out object val) && val != null && field.FieldType.IsAssignableFrom(val.GetType()))
            {
                try
                {
                    field.SetValue(kit, val);
                }
                catch (Exception ex)
                {
                    L.LogError(ex);
                    return ESetFieldResult.FIELD_NOT_SERIALIZABLE;
                }
                return ESetFieldResult.SUCCESS;
            }
            return ESetFieldResult.INVALID_INPUT;
        }
        else return reason;
    }
    private static FieldInfo? GetField(string property, out ESetFieldResult reason)
    {
        for (int i = 0; i < fields.Length; i++)
        {
            FieldInfo fi = fields[i];
            if (fi.Name.Equals(property, StringComparison.Ordinal))
                return ValidateField(fi, out reason) ? fi : null;
        }
        for (int i = 0; i < fields.Length; i++)
        {
            FieldInfo fi = fields[i];
            if (fi.Name.Equals(property, StringComparison.OrdinalIgnoreCase))
                return ValidateField(fi, out reason) ? fi : null;
        }
        reason = ESetFieldResult.FIELD_NOT_FOUND;
        return default;
    }
    private static bool ValidateField(FieldInfo field, out ESetFieldResult reason)
    {
        if (field == null || field.IsStatic || field.IsInitOnly)
        {
            reason = ESetFieldResult.FIELD_NOT_FOUND;
            return false;
        }
        Attribute atr = Attribute.GetCustomAttribute(field, typeof(CommandSettable));
        if (atr is not null)
        {
            reason = ESetFieldResult.SUCCESS;
            return true;
        }
        else
        {
            reason = ESetFieldResult.FIELD_PROTECTED;
            return false;
        }
    }
    public static class NetCalls
    {
        public static readonly NetCall<ulong, ulong, string, EKitAccessType, bool> RequestSetKitAccess = new NetCall<ulong, ulong, string, EKitAccessType, bool>(ReceiveSetKitAccess);
        public static readonly NetCall<ulong, ulong, string[], EKitAccessType, bool> RequestSetKitsAccess = new NetCall<ulong, ulong, string[], EKitAccessType, bool>(ReceiveSetKitsAccess);
        public static readonly NetCallRaw<KitOld?> CreateKit = new NetCallRaw<KitOld?>(ReceiveCreateKit, KitOld.Read, KitOld.Write);
        public static readonly NetCall<string> RequestKitClass = new NetCall<string>(ReceiveRequestKitClass);
        public static readonly NetCall<string> RequestKit = new NetCall<string>(ReceiveKitRequest);
        public static readonly NetCallRaw<string[]> RequestKits = new NetCallRaw<string[]>(ReceiveKitsRequest, null, null);
        public static readonly NetCall<ulong, ulong, byte, Class, string> RequestCreateLoadout = new NetCall<ulong, ulong, byte, Class, string>(ReceiveCreateLoadoutRequest);
        public static readonly NetCall<string, ulong> RequestKitAccess = new NetCall<string, ulong>(ReceiveKitAccessRequest);
        public static readonly NetCall<string[], ulong> RequestKitsAccess = new NetCall<string[], ulong>(ReceiveKitsAccessRequest);


        public static readonly NetCall<string, Class, string> SendKitClass = new NetCall<string, Class, string>(1114);
        public static readonly NetCallRaw<KitOld?> SendKit = new NetCallRaw<KitOld?>(1117, KitOld.Read, KitOld.Write);
        public static readonly NetCallRaw<KitOld?[]> SendKits = new NetCallRaw<KitOld?[]>(1118, KitOld.ReadMany, KitOld.WriteMany);
        public static readonly NetCall<string, int> SendAckCreateLoadout = new NetCall<string, int>(1111);
        public static readonly NetCall<bool> SendAckSetKitAccess = new NetCall<bool>(1101);
        public static readonly NetCall<bool[]> SendAckSetKitsAccess = new NetCall<bool[]>(1133);
        public static readonly NetCall<byte, bool> SendKitAccess = new NetCall<byte, bool>(1135);
        public static readonly NetCall<byte, byte[]> SendKitsAccess = new NetCall<byte, byte[]>(1137);

        [NetCall(ENetCall.FROM_SERVER, 1100)]
        internal static Task ReceiveSetKitAccess(MessageContext context, ulong admin, ulong player, string kit, EKitAccessType type, bool state)
        {
            if (KitManager.KitExists(kit, out KitOld k))
            {
                Task<bool> t = state ? KitManager.GiveAccess(k, player, type) : KitManager.RemoveAccess(k, player);
                if (state)
                    ActionLogger.Add(EActionLogType.CHANGE_KIT_ACCESS, player.ToString(Data.Locale) + " GIVEN ACCESS TO " + kit + ", REASON: " + type.ToString(), admin);
                else
                    ActionLogger.Add(EActionLogType.CHANGE_KIT_ACCESS, player.ToString(Data.Locale) + " DENIED ACCESS TO " + kit, admin);
                context.Reply(SendAckSetKitAccess, true);
                KitManager.UpdateSigns(k);
                return t;
            }
            context.Reply(SendAckSetKitAccess, false);
            return Task.CompletedTask;
        }
        [NetCall(ENetCall.FROM_SERVER, 1132)]
        internal static async Task ReceiveSetKitsAccess(MessageContext context, ulong admin, ulong player, string[] kits, EKitAccessType type, bool state)
        {
            bool[] successes = new bool[kits.Length];
            for (int i = 0; i < kits.Length; ++i)
            {
                string kit = kits[i];
                if (KitManager.KitExists(kit, out KitOld k))
                {
                    Task<bool> t = state ? KitManager.GiveAccess(k, player, type) : KitManager.RemoveAccess(k, player);
                    await t;
                    if (state)
                        ActionLogger.Add(EActionLogType.CHANGE_KIT_ACCESS, player.ToString(Data.Locale) + " GIVEN ACCESS TO " + kit + ", REASON: " + type.ToString(), admin);
                    else
                        ActionLogger.Add(EActionLogType.CHANGE_KIT_ACCESS, player.ToString(Data.Locale) + " DENIED ACCESS TO " + kit, admin);
                    successes[i] = true;
                }
            }
            context.Reply(SendAckSetKitsAccess, successes);
        }
        [NetCall(ENetCall.FROM_SERVER, 1134)]
        private static async Task ReceiveKitAccessRequest(MessageContext context, string kit, ulong player)
        {
            if (!KitManager.Loaded)
                context.Reply(SendKitAccess, (byte)1, false);
            else if (KitManager.KitExists(kit, out KitOld k))
                context.Reply(SendKitAccess, (byte)0, await KitManager.HasAccess(kit, player));
            else
                context.Reply(SendKitAccess, (byte)2, false);
        }

        [NetCall(ENetCall.FROM_SERVER, 1136)]
        private static async Task ReceiveKitsAccessRequest(MessageContext context, string[] kits, ulong player)
        {
            byte[] outp = new byte[kits.Length];
            if (!KitManager.Loaded)
                context.Reply(SendKitsAccess, (byte)1, outp);
            else
            {
                for (int i = 0; i < kits.Length; ++i)
                {
                    if (KitManager.KitExists(kits[i], out KitOld k))
                        outp[i] = await KitManager.HasAccess(k, player) ? (byte)2 : (byte)1;
                    else outp[i] = 3;
                }
            }
            context.Reply(SendKitsAccess, (byte)0, outp);
        }

        [NetCall(ENetCall.FROM_SERVER, 1109)]
        internal static Task ReceiveCreateKit(MessageContext context, KitOld? kit)
        {
            if (kit != null) return KitManager.AddKit(kit);
            return Task.CompletedTask;
        }

        [NetCall(ENetCall.FROM_SERVER, 1113)]
        internal static void ReceiveRequestKitClass(MessageContext context, string kitID)
        {
            if (KitManager.KitExists(kitID, out KitOld kit))
            {
                if (!kit.SignTexts.TryGetValue(L.DEFAULT, out string signtext))
                    signtext = kit.SignTexts.Values.FirstOrDefault() ?? kit.Name;

                context.Reply(SendKitClass, kitID, kit.Class, signtext);
            }
            else
            {
                context.Reply(SendKitClass, kitID, Class.None, kitID);
            }
        }

        [NetCall(ENetCall.FROM_SERVER, 1115)]
        internal static void ReceiveKitRequest(MessageContext context, string kitID)
        {
            if (KitManager.KitExists(kitID, out KitOld kit))
            {
                context.Reply(SendKit, kit);
            }
            else
            {
                context.Reply(SendKit, kit);
            }
        }
        [NetCall(ENetCall.FROM_SERVER, 1116)]
        internal static void ReceiveKitsRequest(MessageContext context, string[] kitIDs)
        {
            KitOld[] kits = new KitOld[kitIDs.Length];
            for (int i = 0; i < kitIDs.Length; i++)
            {
                if (KitManager.KitExists(kitIDs[i], out KitOld kit))
                {
                    kits[i] = kit;
                }
                else
                {
                    kits[i] = null!;
                }
            }
            context.Reply(SendKits, kits);
        }
        [NetCall(ENetCall.FROM_SERVER, 1110)]
        private static async Task ReceiveCreateLoadoutRequest(MessageContext context, ulong fromPlayer, ulong player, byte team, Class @class, string displayName)
        {
            if (KitManager.Loaded)
            {
                (KitOld? kit, int code) = await KitManager.GetSingleton().CreateLoadout(fromPlayer, player, team, @class, displayName);

                context.Reply(SendAckCreateLoadout, kit is null ? string.Empty : kit.Name, code);
            }
            else
            {
                context.Reply(SendAckCreateLoadout, string.Empty, 554);
            }
        }
    }
}
public enum EKitAccessType : byte
{
    UNKNOWN,
    CREDITS,
    EVENT,
    PURCHASE,
    QUEST_REWARD
}
