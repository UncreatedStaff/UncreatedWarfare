using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Kits;

public delegate void KitChangedHandler(UCPlayer player, Kit kit, string oldKit);
public class KitManager : IDisposable
{
    public static event KitChangedHandler OnKitChanged;
    public static KitManager Instance { get; private set; }
    private bool _disp = false;
    public Dictionary<int, Kit> Kits;
    public KitManager()
    {
        if (Instance != null && !Instance._disp)
        {
            Instance.Dispose();
        }
        Instance = this;
        PlayerLife.OnPreDeath += PlayerLife_OnPreDeath;
        Kits = new Dictionary<int, Kit>();
    }
    public async Task Reload()
    {
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
                item.id = new Guid((byte[])R[2]);
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
                item.id = new Guid((byte[])R[2]);
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
                    L.LogWarning("Duplicate translation for kit " + kit.Name + " (" + kit.PrimaryKey + ") for language " + lang);
            }
        });
        await Data.DatabaseManager.QueryAsync("SELECT `Kit`, `JSON` FROM `kit_unlock_requirements`;", new object[0], R =>
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
    private void PlayerLife_OnPreDeath(PlayerLife life)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
    public static async Task<Kit?> AddKit(Kit? kit)
    {
        if (kit != null)
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
            if (pk == -1) return null;
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
            if (Instance.Kits.ContainsKey(pk))
                Instance.Kits[pk] = kit;
            else 
                Instance.Kits.Add(pk, kit);
        }

        return kit;
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
            if (pl != null && pl.AccessibleKits != null)
            {
                if (!pl.AccessibleKits.Exists(x => x == kit || x.Name.Equals(kit.Name, StringComparison.Ordinal)))
                    pl.AccessibleKits.Add(kit);
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
            if (player.AccessibleKits != null)
            {
                if (!player.AccessibleKits.Exists(x => x == kit || x.Name.Equals(kit.Name, StringComparison.Ordinal)))
                    player.AccessibleKits.Add(kit);
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
            player.AccessibleKits?.RemoveAll(x => x == kit || x.Name.Equals(kit.Name, StringComparison.Ordinal));
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
            pl?.AccessibleKits?.RemoveAll(x => x == kit || x.Name.Equals(kit.Name, StringComparison.Ordinal));
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
        Instance.Kits.Remove(primaryKey);
        return await Data.DatabaseManager.NonQueryAsync("DELETE FROM `kit_data` WHERE `pk` = @0;", new object[1] { primaryKey }) > 0;
    }
    public static async Task<bool> DeleteKit(string name)
    {
        KeyValuePair<int, Kit> fod = Instance.Kits.FirstOrDefault(x => x.Value.Name.Equals(name, StringComparison.Ordinal));
        if (fod.Value != null)
            Instance.Kits.Remove(fod.Key);
        return await Data.DatabaseManager.NonQueryAsync("SET @pk := (SELECT `pk` FROM `kit_data` WHERE `InternalName` = @0 LIMIT 1); " +
            "DELETE FROM `kit_data` WHERE @pk IS NOT NULL AND `pk` = @pk;", new object[1]
        {
            name
        }) > 0;
    }
    public static IEnumerable<Kit> GetKitsWhere(Func<Kit, bool> predicate) => Instance.Kits.Values.Where(predicate);
    public static bool KitExists(string kitName, out Kit kit)
    {
        kit = Instance.Kits.Values.FirstOrDefault(x => x.Name.Equals(kitName, StringComparison.Ordinal));
        return kit != null;
    }
    public static bool KitExists(Func<Kit, bool> predicate, out Kit kit)
    {
        kit = Instance.Kits.Values.FirstOrDefault(predicate);
        return kit != null;
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
                        asset.GUID,
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
            clothes.Add(new KitClothing(playerClothes.shirtAsset.GUID, EClothingType.SHIRT));
        if (playerClothes.pantsAsset != null)
            clothes.Add(new KitClothing(playerClothes.pantsAsset.GUID, EClothingType.PANTS));
        if (playerClothes.vestAsset != null)
            clothes.Add(new KitClothing(playerClothes.vestAsset.GUID, EClothingType.VEST));
        if (playerClothes.hatAsset != null)
            clothes.Add(new KitClothing(playerClothes.hatAsset.GUID, EClothingType.HAT));
        if (playerClothes.maskAsset != null)
            clothes.Add(new KitClothing(playerClothes.maskAsset.GUID, EClothingType.MASK));
        if (playerClothes.backpackAsset != null)
            clothes.Add(new KitClothing(playerClothes.backpackAsset.GUID, EClothingType.BACKPACK));
        if (playerClothes.glassesAsset != null)
            clothes.Add(new KitClothing(playerClothes.glassesAsset.GUID, EClothingType.GLASSES));
        
        return clothes;
    }
    public static void OnPlayerJoinedQuestHandling(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        foreach (Kit kit in Instance.Kits.Values)
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

        RequestSigns.InvokeLangUpdateForAllSigns(player.Player.channel.owner);
    }
    public static bool OnQuestCompleted(UCPlayer player, Guid presetKey)
    {
        bool affectedKit = false;
        foreach (Kit kit in Instance.Kits.Values)
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
        return affectedKit;
    }

    public static void GiveKit(UCPlayer player, Kit kit)
    {
        if (kit == null)
            return;
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
        UCInventoryManager.ClearInventory(player);
        foreach (KitClothing clothing in kit.Clothes)
        {
            if (Assets.find(clothing.id) is ItemAsset asset)
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
            if (Assets.find(k.id) is ItemAsset asset)
            {
                Item item = new Item(asset.id, k.amount, 100, F.CloneBytes(k.metadata));
                if (!player.Player.inventory.tryAddItem(item, k.x, k.y, k.page, k.rotation))
                    if (player.Player.inventory.tryAddItem(item, true))
                        ItemManager.dropItem(item, player.Position, true, true, true);
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
            RequestSigns.InvokeLangUpdateForSignsOfKit(oldkit);
        RequestSigns.InvokeLangUpdateForSignsOfKit(kit.Name);
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
            if (ignoreAmmoBags && Gamemode.Config.Barricades.AmmoBagGUID == i.id)
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
        string unarmedKit = "";
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
    public static bool HasKit(UnturnedPlayer player, out Kit kit) => HasKit(player.Player.channel.owner.playerID.steamID.m_SteamID, out kit);
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
        RequestSigns.InvokeLangUpdateForSignsOfKit(kit.Name);
    }
    public static IEnumerable<Kit> GetAccessibleKits(ulong playerID)
    {
        UCPlayer? pl = UCPlayer.FromID(playerID);
        if (pl != null && pl.AccessibleKits != null)
        {
            return pl.AccessibleKits;
        }
        else return Array.Empty<Kit>();
    }
    public void Dispose()
    {
        PlayerLife.OnPreDeath -= PlayerLife_OnPreDeath;
        _disp = true;
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
    private static readonly FieldInfo[] fields = typeof(Kit).GetFields();
    /// <summary>reason [ 0: success, 1: no field, 2: invalid field, 3: non-saveable property ]</summary>
    private static FieldInfo? GetField(string property, out byte reason)
    {
        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i].Name == property) // case sensitive search
            {
                if (ValidateField(fields[i], out reason))
                {
                    return fields[i];
                }
            }
        }
        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i].Name.ToLower() == property.ToLower()) // case insensitive search if case sensitive search netted no results
            {
                if (ValidateField(fields[i], out reason))
                {
                    return fields[i];
                }
            }
        }
        reason = 1;
        return default;
    }
    private static object? ParseInput(string input, Type type, out bool parsed)
    {
        if (input == default || type == default)
        {
            parsed = false;
            return default;
        }
        if (type == typeof(object))
        {
            parsed = true;
            return input;
        }
        if (type == typeof(string))
        {
            parsed = true;
            return input;
        }
        if (type == typeof(bool))
        {
            string lowercase = input.ToLower();
            if (lowercase == "true")
            {
                parsed = true;
                return true;
            }
            else if (lowercase == "false")
            {
                parsed = true;
                return false;
            }
            else
            {
                parsed = false;
                return default;
            }
        }
        if (type == typeof(char))
        {
            if (input.Length == 1)
            {
                parsed = true;
                return input[0];
            }
        }
        if (type.IsEnum)
        {
            try
            {
                object output = Enum.Parse(type, input, true);
                if (output == default)
                {
                    parsed = false;
                    return default;
                }
                parsed = true;
                return output;
            }
            catch (ArgumentNullException)
            {
                parsed = false;
                return default;
            }
            catch (ArgumentException)
            {
                parsed = false;
                return default;
            }
        }
        if (!type.IsPrimitive)
        {
            L.LogError("Can not parse non-primitive types except for strings and enums.");
            parsed = false;
            return default;
        }

        if (type == typeof(int))
        {
            if (int.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out int result))
            {
                parsed = true;
                return result;
            }
        }
        else if (type == typeof(ushort))
        {
            if (ushort.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out ushort result))
            {
                parsed = true;
                return result;
            }
        }
        else if (type == typeof(ulong))
        {
            if (ulong.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out ulong result))
            {
                parsed = true;
                return result;
            }
        }
        else if (type == typeof(float))
        {
            if (float.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out float result))
            {
                parsed = true;
                return result;
            }
        }
        else if (type == typeof(decimal))
        {
            if (decimal.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out decimal result))
            {
                parsed = true;
                return result;
            }
        }
        else if (type == typeof(double))
        {
            if (double.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out double result))
            {
                parsed = true;
                return result;
            }
        }
        else if (type == typeof(byte))
        {
            if (byte.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out byte result))
            {
                parsed = true;
                return result;
            }
        }
        else if (type == typeof(sbyte))
        {
            if (sbyte.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out sbyte result))
            {
                parsed = true;
                return result;
            }
        }
        else if (type == typeof(short))
        {
            if (short.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out short result))
            {
                parsed = true;
                return result;
            }
        }
        else if (type == typeof(uint))
        {
            if (uint.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out uint result))
            {
                parsed = true;
                return result;
            }
        }
        else if (type == typeof(long))
        {
            if (long.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out long result))
            {
                parsed = true;
                return result;
            }
        }
        parsed = false;
        return default;
    }
    /// <summary>Fields must be instanced, non-readonly, and have the <see cref="JsonSettable"/> attribute to be set.</summary>
    public static Kit SetProperty(Kit obj, string property, string value, out bool set, out bool parsed, out bool found, out bool allowedToChange)
    {
        FieldInfo? field = GetField(property, out byte reason);
        if (reason != 0)
        {
            if (reason == 1 || reason == 2)
            {
                set = false;
                parsed = false;
                found = false;
                allowedToChange = false;
                return obj;
            }
            else if (reason == 3)
            {
                set = false;
                parsed = false;
                found = true;
                allowedToChange = false;
                return obj;
            }
        }
        found = true;
        allowedToChange = true;
        object? parsedValue;
        if (field == null)
        {
            parsed = false;
            parsedValue = null;
        }
        else parsedValue = ParseInput(value, field.FieldType, out parsed);
        if (parsed)
        {
            if (field != default)
            {
                try
                {
                    field.SetValue(obj, parsedValue);
                    set = true;
                    Task.Run(async () => await KitManager.AddKit(obj)).ConfigureAwait(false);
                    return obj;
                }
                catch (FieldAccessException ex)
                {
                    L.LogError(ex);
                    set = false;
                    return obj;
                }
                catch (TargetException ex)
                {
                    L.LogError(ex);
                    set = false;
                    return obj;
                }
                catch (ArgumentException ex)
                {
                    L.LogError(ex);
                    set = false;
                    return obj;
                }
            }
            else
            {
                set = false;
                return obj;
            }
        }
        else
        {
            set = false;
            return obj;
        }
    }
    /// <summary>reason [ 0: success, 1: no field, 2: invalid field, 3: non-saveable property ]</summary>
    private static bool ValidateField(FieldInfo field, out byte reason)
    {
        if (field == default)
        {
            L.LogError("Kit saver: field not found.");
            reason = 1;
            return false;
        }
        if (field.IsStatic)
        {
            L.LogError("Kit saver tried to save to a static property.");
            reason = 2;
            return false;
        }
        if (field.IsInitOnly)
        {
            L.LogError("Kit saver tried to save to a readonly property.");
            reason = 2;
            return false;
        }
        IEnumerator<CustomAttributeData> attributes = field.CustomAttributes.GetEnumerator();
        bool settable = false;
        while (attributes.MoveNext())
        {
            if (attributes.Current.AttributeType == typeof(JsonSettable))
            {
                settable = true;
                break;
            }
        }
        attributes.Dispose();
        if (!settable)
        {
            L.LogError("Kit saver tried to save to a non json-savable property.");
            reason = 3;
            return false;
        }
        reason = 0;
        return true;
    }
    /// <summary>Fields must be instanced, non-readonly, and have the <see cref="JsonSettable"/> attribute to be set.</summary>
    public static bool SetProperty(Func<Kit, bool> selector, string property, string value, out bool foundObject, out bool setSuccessfully, out bool parsed, out bool found, out bool allowedToChange)
    {
        if (KitManager.KitExists(selector, out Kit selected))
        {
            foundObject = true;
            SetProperty(selected, property, value, out setSuccessfully, out parsed, out found, out allowedToChange);
            return setSuccessfully;
        }
        else
        {
            foundObject = false;
            setSuccessfully = false;
            parsed = false;
            found = false;
            allowedToChange = false;
            return false;
        }
    }
    public static bool SetProperty<V>(Func<Kit, bool> selector, string property, V value, out bool foundObject, out bool setSuccessfully, out bool foundproperty, out bool allowedToChange)
    {
        if (KitManager.KitExists(selector, out Kit selected))
        {
            foundObject = true;
            SetProperty(selected, property, value, out setSuccessfully, out foundproperty, out allowedToChange);
            return setSuccessfully;
        }
        else
        {
            foundObject = false;
            setSuccessfully = false;
            foundproperty = false;
            allowedToChange = false;
            return false;
        }
    }
    public static Kit SetProperty<V>(Kit obj, string property, V value, out bool success, out bool found, out bool allowedToChange)
    {
        FieldInfo? field = GetField(property, out byte reason);
        if (reason != 0)
        {
            if (reason == 1 || reason == 2)
            {
                found = false;
                allowedToChange = false;
                success = false;
                return obj;
            }
            else if (reason == 3)
            {
                found = true;
                allowedToChange = false;
                success = false;
                return obj;
            }
        }
        found = true;
        allowedToChange = true;
        if (field != default)
        {
            if (field.FieldType.IsAssignableFrom(typeof(V)))
            {
                try
                {
                    field.SetValue(obj, value);
                    success = true;
                    Task.Run(async () => await KitManager.AddKit(obj)).ConfigureAwait(false);
                    return obj;
                }
                catch (FieldAccessException ex)
                {
                    L.LogError(ex);
                    success = false;
                    return obj;
                }
                catch (TargetException ex)
                {
                    L.LogError(ex);
                    success = false;
                    return obj;
                }
                catch (ArgumentException ex)
                {
                    L.LogError(ex);
                    success = false;
                    return obj;
                }
            }
            else
            {
                success = false;
                return obj;
            }
        }
        else
        {
            success = false;
            return obj;
        }
    }
    public static bool IsPropertyValid<TEnum>(object name, out TEnum property) where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(name.ToString(), out var p))
        {
            property = p;
            return true;
        }
        property = p;
        return false;
    }
    public static class NetCalls
    {
        public static readonly NetCall<ulong, string, EKitAccessType> GiveKitAccess = new NetCall<ulong, string, EKitAccessType>(ReceiveGiveKitAccess);
        public static readonly NetCall<ulong, string> RemoveKitAccess = new NetCall<ulong, string>(ReceiveRemoveKitAccess);
        public static readonly NetCallRaw<Kit?> CreateKit = new NetCallRaw<Kit?>(ReceiveCreateKit, Kit.Read, Kit.Write);
        public static readonly NetCall<string> RequestKitClass = new NetCall<string>(ReceiveRequestKitClass);
        public static readonly NetCall<string> RequestKit = new NetCall<string>(ReceiveKitRequest);
        public static readonly NetCallRaw<string[]> RequestKits = new NetCallRaw<string[]>(ReceiveKitsRequest, null, null);

        public static readonly NetCall<string, EClass, string> SendKitClass = new NetCall<string, EClass, string>(1114);
        public static readonly NetCallRaw<Kit?> SendKit = new NetCallRaw<Kit?>(1117, Kit.Read, Kit.Write);
        public static readonly NetCallRaw<Kit?[]> SendKits = new NetCallRaw<Kit?[]>(1118, Kit.ReadMany, Kit.WriteMany);

        [NetCall(ENetCall.FROM_SERVER, 1100)]
        internal static Task ReceiveGiveKitAccess(IConnection connection, ulong player, string kit, EKitAccessType type)
        {
            if (KitManager.KitExists(kit, out Kit k))
                return KitManager.GiveAccess(k, player, type);
            return Task.CompletedTask;
        }

        [NetCall(ENetCall.FROM_SERVER, 1101)]
        internal static Task ReceiveRemoveKitAccess(IConnection connection, ulong player, string kit)
        {
            if (KitManager.KitExists(kit, out Kit k))
                return KitManager.RemoveAccess(k, player);
            return Task.CompletedTask;
        }

        [NetCall(ENetCall.FROM_SERVER, 1109)]
        internal static Task ReceiveCreateKit(IConnection connection, Kit? kit) => KitManager.AddKit(kit);

        [NetCall(ENetCall.FROM_SERVER, 1113)]
        internal static void ReceiveRequestKitClass(IConnection connection, string kitID)
        {
            if (KitManager.KitExists(kitID, out Kit kit))
            {
                if (!kit.SignTexts.TryGetValue(JSONMethods.DEFAULT_LANGUAGE, out string signtext))
                    signtext = kit.SignTexts.Values.FirstOrDefault() ?? kit.Name;

                SendKitClass.Invoke(connection, kitID, kit.Class, signtext);
            }
            else
            {
                SendKitClass.Invoke(connection, kitID, EClass.NONE, kit.Name);
            }
        }

        [NetCall(ENetCall.FROM_SERVER, 1115)]
        internal static void ReceiveKitRequest(IConnection connection, string kitID)
        {
            if (KitManager.KitExists(kitID, out Kit kit))
            {
                SendKit.Invoke(connection, kit);
            }
            else
            {
                SendKit.Invoke(connection, null);
            }
        }
        [NetCall(ENetCall.FROM_SERVER, 1116)]
        internal static void ReceiveKitsRequest(IConnection connection, string[] kitIDs)
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
            SendKits.Invoke(connection, kits);
        }
    }
}
public enum EKitAccessType : byte
{
    UNKNOWN,
    CREDITS,
    EVENT,
    PURCHASE
}
