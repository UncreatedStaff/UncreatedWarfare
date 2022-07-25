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
using Uncreated.Networking;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Kits;

public delegate void KitChangedHandler(UCPlayer player, Kit kit, string oldKit);
public class KitManager : BaseReloadSingleton
{
    private static readonly byte[] GUID_BUFFER = new byte[16];
    private readonly SemaphoreSlim _threadLocker = new SemaphoreSlim(1, 5);
    public static event KitChangedHandler OnKitChanged;
    public Dictionary<int, Kit> Kits = new Dictionary<int, Kit>(256);
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
            foreach (Kit v in singleton.Kits.Values)
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
    public override void Unload()
    {
        _isLoaded = false;
        _singleton = null!;
        PlayerLife.OnPreDeath -= PlayerLife_OnPreDeath;
    }
    public async Task ReloadKits()
    {
        SingletonEx.AssertLoaded<KitManager>(_isLoaded);
        try
        {
            await _threadLocker.WaitAsync();
            Kits.Clear();
            await Data.DatabaseManager.QueryAsync("SELECT * FROM `kit_data`;", new object[0], R =>
            {
                Kit kit = new Kit(true);
                kit.PrimaryKey = R.GetInt32(0);
                kit.Name = R.GetString(1);
                kit.Class = (EClass)R.GetInt32(2);
                kit.Branch = (EBranch)R.GetInt32(3);
                kit.Team = R.GetUInt64(4);
                kit.CreditCost = R.GetUInt16(5);
                kit.UnlockLevel = R.GetUInt16(6);
                kit.IsPremium = R.GetBoolean(7);
                kit.PremiumCost = R.GetFloat(8);
                kit.IsLoadout = R.GetBoolean(9);
                kit.TeamLimit = R.GetFloat(10);
                kit.Cooldown = R.GetInt32(11);
                kit.Disabled = R.GetBoolean(12);
                kit.Weapons = R.GetString(13);
                kit.Items = new List<KitItem>(12);
                kit.Clothes = new List<KitClothing>(5);
                kit.SignTexts = new Dictionary<string, string>(1);
                kit.UnlockRequirements = new BaseUnlockRequirement[0];
                kit.Skillsets = new Skillset[0];
                Kits.Add(kit.PrimaryKey, kit);
            });
            await Data.DatabaseManager.QueryAsync("SELECT * FROM `kit_items`;", new object[0], R =>
            {
                int kitPk = R.GetInt32(1);
                if (Kits.TryGetValue(kitPk, out Kit kit))
                {
                    KitItem item = new KitItem();
                    R.GetBytes(2, 0, GUID_BUFFER, 0, 16);
                    item.id = new Guid(GUID_BUFFER);
                    item.x = R.GetByte(3);
                    item.y = R.GetByte(4);
                    item.rotation = R.GetByte(5);
                    item.page = R.GetByte(6);
                    item.amount = R.GetByte(7);
                    item.metadata = (byte[])R[8];
                    kit.Items.Add(item);
                }
            });
            await Data.DatabaseManager.QueryAsync("SELECT * FROM `kit_clothes`;", new object[0], R =>
            {
                int kitPk = R.GetInt32(1);
                if (Kits.TryGetValue(kitPk, out Kit kit))
                {
                    KitClothing item = new KitClothing();
                    R.GetBytes(2, 0, GUID_BUFFER, 0, 16);
                    item.id = new Guid(GUID_BUFFER);
                    item.type = (EClothingType)R.GetByte(3);
                    kit.Clothes.Add(item);
                }
            });
            await Data.DatabaseManager.QueryAsync("SELECT * FROM `kit_skillsets`;", new object[0], R =>
            {
                int kitPk = R.GetInt32(1);
                if (Kits.TryGetValue(kitPk, out Kit kit))
                {
                    EPlayerSpeciality type = (EPlayerSpeciality)R.GetByte(2);
                    Skillset set;
                    byte v = R.GetByte(3);
                    byte lvl = R.GetByte(4);
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
                            return;
                    }

                    kit.AddSkillset(set);
                }
            });
            await Data.DatabaseManager.QueryAsync("SELECT * FROM `kit_lang`;", new object[0], R =>
            {
                int kitPk = R.GetInt32(1);
                if (Kits.TryGetValue(kitPk, out Kit kit))
                {
                    string lang = R.GetString(2);
                    if (!kit.SignTexts.ContainsKey(lang))
                        kit.SignTexts.Add(lang, R.GetString(3));
                    else
                        L.LogWarning("Duplicate translation for kit " + kit.Name + " (" + kit.PrimaryKey +
                                     ") for language " + lang);
                }
            });
            await Data.DatabaseManager.QueryAsync("SELECT `Kit`, `JSON` FROM `kit_unlock_requirements`;", new object[0],
                R =>
                {
                    int kitPk = R.GetInt32(0);
                    if (Kits.TryGetValue(kitPk, out Kit kit))
                    {
                        Utf8JsonReader reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(R.GetString(1)));
                        BaseUnlockRequirement? req = BaseUnlockRequirement.Read(ref reader);
                        if (req != null)
                        {
                            kit.AddUnlockRequirement(req);
                        }
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
        if (HasKit(life.player.channel.owner.playerID.steamID, out Kit kit))
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
    public static async Task<Kit> AddKit(Kit kit)
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
                    "`UnlockLevel`, `IsPremium`, `PremiumCost`, `IsLoadout`, `TeamLimit`, `Cooldown`, `Disabled`, `WeaponText`" + (hasPk ? ", `pk`" : string.Empty) + ") VALUES " +
                    "(@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, @12" + (hasPk ? ", @13" : string.Empty) + ")" +
                    "ON DUPLICATE KEY UPDATE " +
                    "`InternalName` = @0, `Class` = @1, `Branch` = @2, `Team` = @3, `CreditCost` = @4, `UnlockLevel` = @5, `IsPremium` = @6, `PremiumCost` = @7, `IsLoadout` = @8, " +
                    "`TeamLimit` = @9, `Cooldown` = @10, `Disabled` = @11, `WeaponText` = @12, `pk` = LAST_INSERT_ID(`pk`); " +
                    "SET @kitPk := (SELECT LAST_INSERT_ID() AS `pk`); " +
                    "DELETE FROM `kit_lang` WHERE `Kit` = @kitPk; " +
                    "DELETE FROM `kit_items` WHERE `Kit` = @kitPk; " +
                    "DELETE FROM `kit_clothes` WHERE `Kit` = @kitPk; " +
                    "DELETE FROM `kit_skillsets` WHERE `Kit` = @kitPk; " +
                    "DELETE FROM `kit_unlock_requirements` WHERE `Kit` = @kitPk; " +
                    "SELECT @kitPk;",
                    hasPk ? new object[14]
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
                    kit.PrimaryKey
                    }
                    : new object[13]
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
                    kit.Weapons
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
                        KitItem item = kit.Items[i];
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
                        objs[index++] = item.id.ToByteArray();
                        objs[index++] = item.x;
                        objs[index++] = item.y;
                        objs[index++] = item.rotation;
                        objs[index++] = item.page;
                        objs[index++] = item.amount;
                        objs[index++] = item.metadata;
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
                        KitClothing clothes = kit.Clothes[i];
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
                        objs[index++] = clothes.id.ToByteArray();
                        objs[index++] = (int)clothes.type;
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
    public static async Task<bool> GiveAccess(Kit kit, ulong player, EKitAccessType type)
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
                    pl.AccessibleKits = new List<Kit>(1) { kit };
                else if (!pl.AccessibleKits.Exists(x => x == kit || x.Name.Equals(kit.Name, StringComparison.Ordinal)))
                    pl.AccessibleKits.Add(kit);
                if ((kit.IsPremium || kit.IsLoadout) && Data.Is(out ITeams teams) && teams.UseTeamSelector && teams.TeamSelector is not null)
                    teams.TeamSelector.OnKitsUpdated(pl);
            }
        }
        return res;
    }
    public static async Task<bool> GiveAccess(Kit kit, UCPlayer player, EKitAccessType type)
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
            if (player != null)
            {
                if (player.AccessibleKits is null)
                    player.AccessibleKits = new List<Kit>(1) { kit };
                else if (!player.AccessibleKits.Exists(x => x == kit || x.Name.Equals(kit.Name, StringComparison.Ordinal)))
                    player.AccessibleKits.Add(kit);
                if ((kit.IsPremium || kit.IsLoadout) && Data.Is(out ITeams teams) && teams.UseTeamSelector && teams.TeamSelector is not null)
                    teams.TeamSelector.OnKitsUpdated(player);
            }
        }
        return res;
    }
    /// <summary>Does not add to <see cref="UCPlayer.AccessibleKits"/>. Use <see cref="GiveAccess(Kit, ulong, EKitAccessType)"/> or <see cref="GiveAccess(Kit, UCPlayer, EKitAccessType)"/> instead.</summary>
    public static async Task<bool> GiveAccess(int primaryKey, ulong player, EKitAccessType type)
    {
        return await Data.DatabaseManager.NonQueryAsync("INSERT INTO `kit_access` (`Kit`, `Steam64`, `AccessType`) VALUES (@0, @1, @2);", new object[3]
        {
            primaryKey, player, type.ToString()
        }) > 0;
    }
    /// <summary>Does not add to <see cref="UCPlayer.AccessibleKits"/>. Use <see cref="GiveAccess(Kit, ulong, EKitAccessType)"/> or <see cref="GiveAccess(Kit, UCPlayer, EKitAccessType)"/> instead.</summary>
    public static async Task<bool> GiveAccess(string name, ulong player, EKitAccessType type)
    {
        return await Data.DatabaseManager.NonQueryAsync("SET @pk := (SELECT `pk` FROM `kit_data` WHERE `InternalName` = @0 LIMIT 1); INSERT INTO `kit_access` (`Kit`, `Steam64`, `AccessType`) VALUES (@pk, @1, @2) WHERE @pk IS NOT NULL;", new object[3]
        {
            name, player, type.ToString()
        }) > 0;
    }
    public static async Task<bool> RemoveAccess(Kit kit, UCPlayer player)
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
            player.AccessibleKits?.RemoveAll(x => x == kit || x.Name.Equals(kit.Name, StringComparison.Ordinal));
            if ((kit.IsPremium || kit.IsLoadout) && Data.Is(out ITeams teams) && teams.UseTeamSelector && teams.TeamSelector is not null)
                teams.TeamSelector.OnKitsUpdated(player);
        }
        return res;
    }
    public static async Task<bool> RemoveAccess(Kit kit, ulong player)
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
                pl.AccessibleKits?.RemoveAll(x => x == kit || x.Name.Equals(kit.Name, StringComparison.Ordinal));
                if ((kit.IsPremium || kit.IsLoadout) && Data.Is(out ITeams teams) && teams.UseTeamSelector && teams.TeamSelector is not null)
                    teams.TeamSelector.OnKitsUpdated(pl);
            }
        }
        return res;
    }
    /// <summary>Does not remove from <see cref="UCPlayer.AccessibleKits"/>. Use <see cref="RemoveAccess(Kit, ulong)"/> or <see cref="RemoveAccess(Kit, UCPlayer)"/> instead.</summary>
    public static async Task<bool> RemoveAccess(int primaryKey, ulong player)
    {
        return await Data.DatabaseManager.NonQueryAsync("DELETE FROM `kit_access` WHERE `Steam64` = @0 AND `Kit` = @1;", new object[2]
        {
            player, primaryKey
        }) > 0;
    }
    /// <summary>Does not remove from <see cref="UCPlayer.AccessibleKits"/>. Use <see cref="RemoveAccess(Kit, ulong)"/> or <see cref="RemoveAccess(Kit, UCPlayer)"/> instead.</summary>
    public static async Task<bool> RemoveAccess(string name, ulong player)
    {
        return await Data.DatabaseManager.NonQueryAsync("SET @pk := (SELECT `pk` FROM `kit_data` WHERE `InternalName` = @0 LIMIT 1); DELETE FROM `kit_access` WHERE `Steam64` = @1 AND `Kit` = @pk;", new object[2]
        {
            name, player
        }) > 0;
    }
    public static Task<bool> HasAccess(Kit kit, ulong player)
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
    public static bool HasAccessFast(Kit kit, UCPlayer player)
    {
        return player != null && player.AccessibleKits != null && player.AccessibleKits.Exists(x => x == kit || x.Name.Equals(kit.Name, StringComparison.Ordinal));
    }
    public static bool HasAccessFast(int primaryKey, UCPlayer player)
    {
        if (primaryKey == -1) return false;
        return player != null && player.AccessibleKits != null && player.AccessibleKits.Exists(x => x.PrimaryKey == primaryKey);
    }
    public static bool HasAccessFast(string name, UCPlayer player)
    {
        if (string.IsNullOrEmpty(name)) return false;
        return player != null && player.AccessibleKits != null && player.AccessibleKits.Exists(x => x.Name.Equals(name, StringComparison.Ordinal));
    }
    public static Task<bool> DeleteKit(Kit kit)
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
        KeyValuePair<int, Kit> fod = singleton.Kits.FirstOrDefault(x => x.Value.Name.Equals(name, StringComparison.Ordinal));
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
    internal static Kit? GetRandomPublicKit()
    {
        List<Kit> selection = GetAllPublicKits();
        if (selection.Count == 0) return null;
        return selection[UnityEngine.Random.Range(0, selection.Count)];
    }
    internal static List<Kit> GetAllPublicKits()
    {
        return GetKitsWhere(x => !x.IsPremium && !x.IsLoadout && !x.Disabled && x.Class > EClass.UNARMED);
    }
    public static List<Kit> GetKitsWhere(Func<Kit, bool> predicate)
    {
        KitManager singleton = GetSingleton();
        singleton._threadLocker.Wait();
        List<Kit> rtn = singleton.Kits.Values.Where(predicate).ToList();
        singleton._threadLocker.Release();
        return rtn;
    }
    public static bool KitExists(string kitName, out Kit kit)
    {
        KitManager singleton = GetSingleton();
        singleton._threadLocker.Wait();
        kit = singleton.Kits.Values.FirstOrDefault(x => x.Name.Equals(kitName, StringComparison.Ordinal));
        singleton._threadLocker.Release();
        return kit != null;
    }
    public static bool KitExists(Func<Kit, bool> predicate, out Kit kit)
    {
        KitManager singleton = GetSingleton();
        singleton._threadLocker.Wait();
        kit = singleton.Kits.Values.FirstOrDefault(predicate);
        singleton._threadLocker.Release();
        return kit != null;
    }
    /// <exception cref="SingletonUnloadedException"/>
    internal static KitManager GetSingleton()
    {
        if (_singleton is null)
        {
            _singleton = Data.Singletons.GetSingleton<KitManager>();
            if (_singleton is null)
                throw new SingletonUnloadedException(typeof(KitManager));
        }
        return _singleton;
    }
    public static List<KitItem> ItemsFromInventory(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        List<KitItem> items = new List<KitItem>();

        for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
        {
            for (byte i = 0; i < player.Player.inventory.getItemCount(page); i++)
            {
                ItemJar jar = player.Player.inventory.getItem(page, i);
                if (Assets.find(EAssetType.ITEM, jar.item.id) is ItemAsset asset)
                {
                    items.Add(new KitItem(
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
    public static List<KitClothing> ClothesFromInventory(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        PlayerClothing playerClothes = player.Player.clothing;
        List<KitClothing> clothes = new List<KitClothing>(7);

        if (playerClothes.shirtAsset != null)
            clothes.Add(new KitClothing(TeamManager.GetClothingRedirectGuid(playerClothes.shirtAsset.GUID), EClothingType.SHIRT));
        if (playerClothes.pantsAsset != null)
            clothes.Add(new KitClothing(TeamManager.GetClothingRedirectGuid(playerClothes.pantsAsset.GUID), EClothingType.PANTS));
        if (playerClothes.vestAsset != null)
            clothes.Add(new KitClothing(TeamManager.GetClothingRedirectGuid(playerClothes.vestAsset.GUID), EClothingType.VEST));
        if (playerClothes.hatAsset != null)
            clothes.Add(new KitClothing(TeamManager.GetClothingRedirectGuid(playerClothes.hatAsset.GUID), EClothingType.HAT));
        if (playerClothes.maskAsset != null)
            clothes.Add(new KitClothing(TeamManager.GetClothingRedirectGuid(playerClothes.maskAsset.GUID), EClothingType.MASK));
        if (playerClothes.backpackAsset != null)
            clothes.Add(new KitClothing(TeamManager.GetClothingRedirectGuid(playerClothes.backpackAsset.GUID), EClothingType.BACKPACK));
        if (playerClothes.glassesAsset != null)
            clothes.Add(new KitClothing(TeamManager.GetClothingRedirectGuid(playerClothes.glassesAsset.GUID), EClothingType.GLASSES));
        
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
            foreach (Kit kit in singleton.Kits.Values)
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
        RequestSigns.UpdateAllSigns(player.Player.channel.owner);
    }
    public static bool OnQuestCompleted(UCPlayer player, Guid presetKey)
    {
        KitManager singleton = GetSingleton();
        bool affectedKit = false;
        singleton._threadLocker.Wait();
        try
        {
            foreach (Kit kit in singleton.Kits.Values)
            {
                if (!kit.IsLoadout && kit.UnlockRequirements != null)
                {
                    for (int j = 0; j < kit.UnlockRequirements.Length; j++)
                    {
                        if (kit.UnlockRequirements[j] is QuestUnlockRequirement req && req.UnlockPresets != null && req.UnlockPresets.Length > 0 && !req.CanAccess(player))
                        {
                            for (int r = 0; r < req.UnlockPresets.Length; r++)
                            {
                                if (req.UnlockPresets[r] == presetKey)
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
    public static void GiveKit(UCPlayer player, Kit kit)
    {
        if (kit == null)
            return;
        ulong team = player.GetTeam();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (HasKit(player, out Kit oldKit))
        {
            if (oldKit.Skillsets != null)
            {
                for (int i = 0; i < oldKit.Skillsets.Length; i++)
                {
                    ref Skillset skillset = ref kit.Skillsets[i];
                    for (int j = 0; j < Skillset.DEFAULT_SKILLSETS.Length; j++)
                    {
                        ref Skillset skillset2 = ref Skillset.DEFAULT_SKILLSETS[j];
                        if (skillset2.TypeEquals(ref skillset))
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
                KitItem item = kit.Items[i];
                if (item.page < PlayerInventory.PAGES - 2 && Assets.find(TeamManager.CheckAssetRedirect(item.id, team)) is ItemAsset asset)
                    p[item.page].addItem(item.x, item.y, item.rotation, new Item(asset.id, item.amount, 100, F.CloneBytes(item.metadata)));
                else
                    L.LogWarning("Invalid item {" + item.id.ToString("N") + "} in kit " + kit.Name + " for team " + team);
            }
            if (ohi)
                Data.SetOwnerHasInventory(player.Player.inventory, true);
            UCInventoryManager.SendPages(player);
        }
        else
        {
            foreach (KitClothing clothing in kit.Clothes)
            {
                if (Assets.find(TeamManager.CheckClothingAssetRedirect(clothing.id, team)) is ItemAsset asset)
                {
                    if (clothing.type == EClothingType.SHIRT)
                        player.Player.clothing.askWearShirt(asset.id, 100, asset.getState(true), true);
                    else if (clothing.type == EClothingType.PANTS)
                        player.Player.clothing.askWearPants(asset.id, 100, asset.getState(true), true);
                    else if (clothing.type == EClothingType.VEST)
                        player.Player.clothing.askWearVest(asset.id, 100, asset.getState(true), true);
                    else if (clothing.type == EClothingType.HAT)
                        player.Player.clothing.askWearHat(asset.id, 100, asset.getState(true), true);
                    else if (clothing.type == EClothingType.MASK)
                        player.Player.clothing.askWearMask(asset.id, 100, asset.getState(true), true);
                    else if (clothing.type == EClothingType.BACKPACK)
                        player.Player.clothing.askWearBackpack(asset.id, 100, asset.getState(true), true);
                    else if (clothing.type == EClothingType.GLASSES)
                        player.Player.clothing.askWearGlasses(asset.id, 100, asset.getState(true), true);
                }
            }

            foreach (KitItem k in kit.Items)
            {
                if (Assets.find(TeamManager.CheckAssetRedirect(k.id, team)) is ItemAsset asset)
                {
                    Item item = new Item(asset.id, k.amount, 100, F.CloneBytes(k.metadata));
                    if (!player.Player.inventory.tryAddItem(item, k.x, k.y, k.page, k.rotation))
                        if (player.Player.inventory.tryAddItem(item, true))
                            ItemManager.dropItem(item, player.Position, true, true, true);
                }
                else
                    L.LogWarning("Invalid item {" + k.id.ToString("N") + "} in kit " + kit.Name + " for team " + team);
            }
        }
        string oldkit = player.KitName;

        if (Data.Is(out IRevives g))
        {
            if (player.KitClass == EClass.MEDIC && kit.Class != EClass.MEDIC)
            {
                g.ReviveManager.DeregisterMedic(player);
            }
            else if (kit.Class == EClass.MEDIC)
            {
                g.ReviveManager.RegisterMedic(player);
            }
        }
        player.ChangeKit(kit);

        EBranch oldBranch = player.Branch;

        player.Branch = kit.Branch;

        if (oldBranch != player.Branch)
        {
            //Points.OnBranchChanged(player, oldBranch, kit.Branch);
        }

        OnKitChanged?.Invoke(player, kit, oldkit);
        if (oldkit != null && oldkit != string.Empty)
            UpdateSigns(oldkit);
        UpdateSigns(kit);
    }
    public static void ResupplyKit(UCPlayer player, Kit kit, bool ignoreAmmoBags = false)
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
            KitClothing clothing = kit.Clothes[i];
            if (Assets.find(clothing.id) is ItemAsset asset)
            {
                ushort old = 0;
                switch (clothing.type)
                {
                    case EClothingType.GLASSES:
                        if (player.Player.clothing.glasses != asset.id)
                        {
                            old = player.Player.clothing.glasses;
                            player.Player.clothing.askWearGlasses(asset.id, 100, asset.getState(true), true);
                        }
                        break;
                    case EClothingType.HAT:
                        if (player.Player.clothing.hat != asset.id)
                        {
                            old = player.Player.clothing.hat;
                            player.Player.clothing.askWearHat(asset.id, 100, asset.getState(true), true);
                        }
                        break;
                    case EClothingType.BACKPACK:
                        if (player.Player.clothing.backpack != asset.id)
                        {
                            old = player.Player.clothing.backpack;
                            player.Player.clothing.askWearBackpack(asset.id, 100, asset.getState(true), true);
                        }
                        break;
                    case EClothingType.MASK:
                        if (player.Player.clothing.mask != asset.id)
                        {
                            old = player.Player.clothing.mask;
                            player.Player.clothing.askWearMask(asset.id, 100, asset.getState(true), true);
                        }
                        break;
                    case EClothingType.PANTS:
                        if (player.Player.clothing.pants != asset.id)
                        {
                            old = player.Player.clothing.pants;
                            player.Player.clothing.askWearPants(asset.id, 100, asset.getState(true), true);
                        }
                        break;
                    case EClothingType.SHIRT:
                        if (player.Player.clothing.shirt != asset.id)
                        {
                            old = player.Player.clothing.shirt;
                            player.Player.clothing.askWearShirt(asset.id, 100, asset.getState(true), true);
                        }
                        break;
                    case EClothingType.VEST:
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
        foreach (KitItem i in kit.Items)
        {
            if (ignoreAmmoBags && Gamemode.Config.Barricades.AmmoBagGUID.MatchGuid(i.id))
                continue;
            if (Assets.find(i.id) is ItemAsset itemasset)
            {
                Item item = new Item(itemasset.id, i.amount, 100, F.CloneBytes(i.metadata));

                if (!player.Player.inventory.tryAddItem(item, i.x, i.y, i.page, i.rotation))
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
        if (player.IsTeam1())
            unarmedKit = TeamManager.Team1UnarmedKit;
        if (player.IsTeam2())
            unarmedKit = TeamManager.Team2UnarmedKit;

        if (KitExists(unarmedKit, out Kit kit))
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
        Kit rifleman = GetKitsWhere(k =>
                !k.Disabled && 
                k.Team == t &&
                k.Class == EClass.RIFLEMAN &&
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
    public static bool HasKit(ulong steamID, out Kit kit)
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
    public static bool HasKit(UCPlayer player, out Kit kit)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        kit = player.Kit!;
        return player.Kit != null;
    }
    public static bool HasKit(SteamPlayer player, out Kit kit) => HasKit(player.playerID.steamID.m_SteamID, out kit);
    public static bool HasKit(Player player, out Kit kit) => HasKit(player.channel.owner.playerID.steamID.m_SteamID, out kit);
    public static bool HasKit(CSteamID player, out Kit kit) => HasKit(player.m_SteamID, out kit);
    public static void UpdateText(Kit kit, string text, string language = JSONMethods.DEFAULT_LANGUAGE)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        kit.SignTexts.Remove(language);
        kit.SignTexts.Add(language, text);
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
    public static void UpdateSigns(Kit kit)
    {
        if (UCWarfare.IsMainThread)
            UpdateSignsIntl(kit, null);
        else
            UCWarfare.RunOnMainThread(() => UpdateSignsIntl(kit, null));
    }
    public static void UpdateSigns(Kit kit, UCPlayer player)
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
                    RequestSigns.Singleton[i].InvokeUpdate(player.SteamPlayer);
                }
            }
        }
    }
    private static void UpdateSignsIntl(Kit kit, UCPlayer? player)
    {
        if (RequestSigns.Loaded)
        {
            if (kit.IsLoadout)
            {
                if (player is null)
                {
                    for (int i = 0; i < RequestSigns.Singleton.Count; i++)
                    {
                        if (RequestSigns.Singleton[i].kit_name.StartsWith("loadout_", StringComparison.OrdinalIgnoreCase))
                            RequestSigns.Singleton[i].InvokeUpdate();
                    }
                }
                else
                {
                    for (int i = 0; i < RequestSigns.Singleton.Count; i++)
                    {
                        if (RequestSigns.Singleton[i].kit_name.StartsWith("loadout_", StringComparison.OrdinalIgnoreCase))
                            RequestSigns.Singleton[i].InvokeUpdate(player.SteamPlayer);
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
                    if (RequestSigns.Singleton[i].kit_name.Equals(kitId, StringComparison.Ordinal))
                        RequestSigns.Singleton[i].InvokeUpdate();
                }
            }
            else
            {
                for (int i = 0; i < RequestSigns.Singleton.Count; i++)
                {
                    if (RequestSigns.Singleton[i].kit_name.StartsWith(kitId, StringComparison.Ordinal))
                        RequestSigns.Singleton[i].InvokeUpdate(player.SteamPlayer);
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
    internal async Task<(Kit?, int)> CreateLoadout(ulong fromPlayer, ulong player, ulong team, EClass @class, string displayName)
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
            List<KitItem> items;
            List<KitClothing> clothes;
            if (team is 1 or 2 && KitExists(team == 1 ? TeamManager.Team1UnarmedKit : TeamManager.Team2UnarmedKit, out Kit unarmedKit))
            {
                items = unarmedKit.Items.ToList();
                clothes = unarmedKit.Clothes.ToList();
            }
            else
            {
                items = new List<KitItem>(0);
                clothes = new List<KitClothing>(0);
            }

            Kit loadout = new Kit(loadoutName, items, clothes);

            loadout.IsLoadout = true;
            loadout.Team = team;
            loadout.Class = @class;
            if (@class == EClass.PILOT)
                loadout.Branch = EBranch.AIRFORCE;
            else if (@class == EClass.CREWMAN)
                loadout.Branch = EBranch.ARMOR;
            else
                loadout.Branch = EBranch.INFANTRY;

            if (@class == EClass.HAT)
                loadout.TeamLimit = 0.1F;
            await UCWarfare.ToUpdate();
            UpdateText(loadout, displayName);

            await AddKit(loadout);

            ActionLogger.Add(EActionLogType.CREATE_KIT, loadoutName, fromPlayer);

            return (loadout, 0);
        }
    }
}
public static class KitEx
{
    public static bool HasItemOfID(this Kit kit, Guid ID) => kit.Items.Exists(i => i.id == ID);
    public static bool IsLimited(this Kit kit, out int currentPlayers, out int allowedPlayers, ulong team, bool requireCounts = false)
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

    public static bool IsClassLimited(this Kit kit, out int currentPlayers, out int allowedPlayers, ulong team, bool requireCounts = false)
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
    private static readonly FieldInfo[] fields = typeof(Kit).GetFields(BindingFlags.Instance | BindingFlags.Public);
    public static ESetFieldResult SetProperty(Kit kit, string property, string value)
    {
        if (kit is null) return ESetFieldResult.OBJECT_NOT_FOUND;
        if (property is null || value is null) return ESetFieldResult.FIELD_NOT_FOUND;
        FieldInfo? field = GetField(property, out ESetFieldResult reason);
        if (field is not null && reason == ESetFieldResult.SUCCESS)
        {
            if (F.TryParseAny(value, field.FieldType, out object val) && val != null && field.FieldType.IsAssignableFrom(val.GetType()))
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
        Attribute atr = Attribute.GetCustomAttribute(field, typeof(JsonSettable));
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
        public static readonly NetCallRaw<Kit?> CreateKit = new NetCallRaw<Kit?>(ReceiveCreateKit, Kit.Read, Kit.Write);
        public static readonly NetCall<string> RequestKitClass = new NetCall<string>(ReceiveRequestKitClass);
        public static readonly NetCall<string> RequestKit = new NetCall<string>(ReceiveKitRequest);
        public static readonly NetCallRaw<string[]> RequestKits = new NetCallRaw<string[]>(ReceiveKitsRequest, null, null);
        public static readonly NetCall<ulong, ulong, byte, EClass, string> RequestCreateLoadout = new NetCall<ulong, ulong, byte, EClass, string>(ReceiveCreateLoadoutRequest);
        public static readonly NetCall<string, ulong> RequestKitAccess = new NetCall<string, ulong>(ReceiveKitAccessRequest);
        public static readonly NetCall<string[], ulong> RequestKitsAccess = new NetCall<string[], ulong>(ReceiveKitsAccessRequest);


        public static readonly NetCall<string, EClass, string> SendKitClass = new NetCall<string, EClass, string>(1114);
        public static readonly NetCallRaw<Kit?> SendKit = new NetCallRaw<Kit?>(1117, Kit.Read, Kit.Write);
        public static readonly NetCallRaw<Kit?[]> SendKits = new NetCallRaw<Kit?[]>(1118, Kit.ReadMany, Kit.WriteMany);
        public static readonly NetCall<string, int> SendAckCreateLoadout = new NetCall<string, int>(1111);
        public static readonly NetCall<bool> SendAckSetKitAccess = new NetCall<bool>(1101);
        public static readonly NetCall<bool[]> SendAckSetKitsAccess = new NetCall<bool[]>(1133);
        public static readonly NetCall<byte, bool> SendKitAccess = new NetCall<byte, bool>(1135);
        public static readonly NetCall<byte, byte[]> SendKitsAccess = new NetCall<byte, byte[]>(1137);

        [NetCall(ENetCall.FROM_SERVER, 1100)]
        internal static Task ReceiveSetKitAccess(MessageContext context, ulong admin, ulong player, string kit, EKitAccessType type, bool state)
        {
            if (KitManager.KitExists(kit, out Kit k))
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
                if (KitManager.KitExists(kit, out Kit k))
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
            else if (KitManager.KitExists(kit, out Kit k))
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
                    if (KitManager.KitExists(kits[i], out Kit k))
                        outp[i] = await KitManager.HasAccess(k, player) ? (byte)2 : (byte)1;
                    else outp[i] = 3;
                }
            }
            context.Reply(SendKitsAccess, (byte)0, outp);
        }

        [NetCall(ENetCall.FROM_SERVER, 1109)]
        internal static Task ReceiveCreateKit(MessageContext context, Kit? kit)
        {
            if (kit != null) return KitManager.AddKit(kit);
            return Task.CompletedTask;
        }

        [NetCall(ENetCall.FROM_SERVER, 1113)]
        internal static void ReceiveRequestKitClass(MessageContext context, string kitID)
        {
            if (KitManager.KitExists(kitID, out Kit kit))
            {
                if (!kit.SignTexts.TryGetValue(JSONMethods.DEFAULT_LANGUAGE, out string signtext))
                    signtext = kit.SignTexts.Values.FirstOrDefault() ?? kit.Name;

                context.Reply(SendKitClass, kitID, kit.Class, signtext);
            }
            else
            {
                context.Reply(SendKitClass, kitID, EClass.NONE, kitID);
            }
        }

        [NetCall(ENetCall.FROM_SERVER, 1115)]
        internal static void ReceiveKitRequest(MessageContext context, string kitID)
        {
            if (KitManager.KitExists(kitID, out Kit kit))
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
            Kit[] kits = new Kit[kitIDs.Length];
            for (int i = 0; i < kitIDs.Length; i++)
            {
                if (KitManager.KitExists(kitIDs[i], out Kit kit))
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
        private static async Task ReceiveCreateLoadoutRequest(MessageContext context, ulong fromPlayer, ulong player, byte team, EClass @class, string displayName)
        {
            if (KitManager.Loaded)
            {
                (Kit? kit, int code) = await KitManager.GetSingleton().CreateLoadout(fromPlayer, player, team, @class, displayName);

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
