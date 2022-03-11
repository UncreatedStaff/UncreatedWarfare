using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Uncreated.Networking.Encoding;
using Uncreated.Warfare.Quests;

namespace Uncreated.Warfare.Kits;

public class Kit
{
    public string DisplayName => Class switch
    {
        EClass.UNARMED => "Unarmed",
        EClass.SQUADLEADER => "Squad Leader",
        EClass.RIFLEMAN => "Rifleman",
        EClass.MEDIC => "Medic",
        EClass.BREACHER => "Breacher",
        EClass.AUTOMATIC_RIFLEMAN => "Automatic Rifleman",
        EClass.GRENADIER => "Grenadier",
        EClass.MACHINE_GUNNER => "Machine Gunner",
        EClass.LAT => "Light Anti-Tank",
        EClass.HAT => "Heavy Anti-Tank",
        EClass.MARKSMAN => "Designated Marksman",
        EClass.SNIPER => "Sniper",
        EClass.AP_RIFLEMAN => "Anti-Personnel Rifleman",
        EClass.COMBAT_ENGINEER => "Combat Engineer",
        EClass.CREWMAN => "Crewman",
        EClass.PILOT => "Pilot",
        _ => Name,
    };
    public string Name;
    [JsonSettable]
    public EClass Class;
    [JsonSettable]
    public EBranch Branch;
    [JsonSettable]
    public ulong Team;
    public BaseUnlockRequirement[] UnlockRequirements;
    public Skillset[] Skillsets;
    [JsonSettable]
    public ushort TicketCost;
    [JsonSettable]
    public bool IsPremium;
    [JsonSettable]
    public float PremiumCost;
    [JsonSettable]
    public bool IsLoadout;
    [JsonSettable]
    public float TeamLimit;
    [JsonSettable]
    public float Cooldown;
    public List<KitItem> Items;
    public List<KitClothing> Clothes;
    public List<ulong> AllowedUsers;
    public Dictionary<string, string> SignTexts;
    [JsonSettable]
    public string Weapons;
    public Kit()
    {
        Name = "default";
        Items = new List<KitItem>();
        Clothes = new List<KitClothing>();
        Class = EClass.NONE;
        Branch = EBranch.DEFAULT;
        Team = 0;
        UnlockRequirements = new BaseUnlockRequirement[0];
        Skillsets = new Skillset[0];
        TicketCost = 1;
        IsPremium = false;
        PremiumCost = 0;
        IsLoadout = false;
        TeamLimit = 1;
        Cooldown = 0;
        AllowedUsers = new List<ulong>();
        SignTexts = new Dictionary<string, string> { { "en-us", "Default" } };
        Weapons = string.Empty;
    }
    /// <summary>empty constructor</summary>
    public Kit(bool dummy) { }
    public static Kit?[] ReadMany(ByteReader R)
    {
        Kit?[] kits = new Kit[R.ReadInt32()];
        for (int i = 0; i < kits.Length; i++)
        {
            kits[i] = Read(R);
        }
        return kits;
    }
    public static Kit? Read(ByteReader R)
    {
        if (R.ReadUInt8() == 1) return null;
        Kit kit = new Kit(true);
        kit.Name = R.ReadString();
        ushort itemCount = R.ReadUInt16();
        ushort clothesCount = R.ReadUInt16();
        ushort allowedUsersCount = R.ReadUInt16();
        List<KitItem> items = new List<KitItem>(itemCount);
        List<KitClothing> clothes = new List<KitClothing>(clothesCount);
        List<ulong> allowedUsers = new List<ulong>(allowedUsersCount);
        for (int i = 0; i < itemCount; i++)
        {
            items.Add(new KitItem()
            {
                id = R.ReadGUID(),
                amount = R.ReadUInt8(),
                page = R.ReadUInt8(),
                x = R.ReadUInt8(),
                y = R.ReadUInt8(),
                rotation = R.ReadUInt8(),
                metadata = Convert.ToBase64String(R.ReadBytes())
            });
        }
        for (int i = 0; i < clothesCount; i++)
        {
            clothes.Add(new KitClothing()
            {
                id = R.ReadGUID(),
                type = R.ReadEnum<EClothingType>(),
                state = Convert.ToBase64String(R.ReadBytes())
            });
        }
        for (int i = 0; i < allowedUsersCount; i++)
            allowedUsers.Add(R.ReadUInt64());
        kit.AllowedUsers = allowedUsers;
        kit.Items = items;
        kit.Clothes = clothes;
        kit.Branch = R.ReadEnum<EBranch>();
        kit.Class = R.ReadEnum<EClass>();
        kit.Cooldown = R.ReadFloat();
        kit.IsPremium = R.ReadBool();
        kit.IsLoadout = R.ReadBool();
        kit.PremiumCost = R.ReadFloat();
        kit.Team = R.ReadUInt64();
        kit.TeamLimit = R.ReadFloat();
        kit.TicketCost = R.ReadUInt16();
        return kit;
    }
    public static void WriteMany(ByteWriter W, Kit?[] kits)
    {
        W.Write(kits.Length);
        for (int i = 0; i < kits.Length; i++)
            Write(W, kits[i]);
    }
    public static void Write(ByteWriter W, Kit? kit)
    {
        if (kit == null)
        {
            W.Write((byte)1);
            return;
        }
        else W.Write((byte)0);
        W.Write(kit.Name);
        W.Write((ushort)kit.Items.Count);
        W.Write((ushort)kit.Clothes.Count);
        W.Write((ushort)kit.AllowedUsers.Count);
        for (int i = 0; i < kit.Items.Count; i++)
        {
            KitItem item = kit.Items[i];
            W.Write(item.id);
            W.Write(item.amount);
            W.Write(item.page);
            W.Write(item.x);
            W.Write(item.y);
            W.Write(item.rotation);
            W.Write(Convert.FromBase64String(item.metadata));
        }
        for (int i = 0; i < kit.Clothes.Count; i++)
        {
            KitClothing clothing = kit.Clothes[i];
            W.Write(clothing.id);
            W.Write(clothing.type);
            W.Write(Convert.FromBase64String(clothing.state));
        }
        for (int i = 0; i < kit.AllowedUsers.Count; i++)
            W.Write(kit.AllowedUsers[i]);
        W.Write(kit.Branch);
        W.Write(kit.Class);
        W.Write(kit.Cooldown);
        W.Write(kit.IsPremium);
        W.Write(kit.IsLoadout);
        W.Write(kit.PremiumCost);
        W.Write(kit.Team);
        W.Write(kit.TeamLimit);
        W.Write(kit.TicketCost);
    }


    public void WriteJson(Utf8JsonWriter writer)
    {
        writer.WritePropertyName(nameof(Name));
        writer.WriteStringValue(Name);

        writer.WritePropertyName(nameof(Class));
        writer.WriteNumberValue((byte)Class);

        writer.WritePropertyName(nameof(Branch));
        writer.WriteNumberValue((byte)Branch);

        writer.WritePropertyName(nameof(Team));
        writer.WriteNumberValue((byte)Team);

        writer.WritePropertyName(nameof(UnlockRequirements));
        writer.WriteStartArray();
        for (int i = 0; i < UnlockRequirements.Length; i++)
        {
            writer.WriteStartObject();
            BaseUnlockRequirement.Write(writer, UnlockRequirements[i]);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        writer.WritePropertyName(nameof(Skillsets));
        writer.WriteStartArray();
        for (int i = 0; i < Skillsets.Length; i++)
        {
            writer.WriteStartObject();
            Skillset.Write(writer, ref Skillsets[i]);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        writer.WritePropertyName(nameof(TicketCost));
        writer.WriteNumberValue(TicketCost);

        writer.WritePropertyName(nameof(IsPremium));
        writer.WriteBooleanValue(IsPremium);

        writer.WritePropertyName(nameof(PremiumCost));
        writer.WriteNumberValue(PremiumCost);

        writer.WritePropertyName(nameof(IsLoadout));
        writer.WriteBooleanValue(IsLoadout);

        writer.WritePropertyName(nameof(TeamLimit));
        writer.WriteNumberValue(TeamLimit);

        writer.WritePropertyName(nameof(Cooldown));
        writer.WriteNumberValue(Cooldown);

        writer.WritePropertyName(nameof(Items));
        writer.WriteStartArray();
        for (int i = 0; i < Items.Count; i++)
        {
            writer.WriteStartObject();
            Items[i].WriteJson(writer);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        writer.WritePropertyName(nameof(Clothes));
        writer.WriteStartArray();
        for (int i = 0; i < Clothes.Count; i++)
        {
            writer.WriteStartObject();
            Clothes[i].WriteJson(writer);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        writer.WritePropertyName(nameof(AllowedUsers));
        writer.WriteStartArray();
        for (int i = 0; i < AllowedUsers.Count; i++)
        {
            writer.WriteNumberValue(AllowedUsers[i]);
        }
        writer.WriteEndArray();
        
        writer.WritePropertyName(nameof(SignTexts));
        writer.WriteStartObject();
        foreach (KeyValuePair<string, string> kvp in SignTexts)
        {
            writer.WritePropertyName(kvp.Key);
            writer.WriteStringValue(kvp.Value);
        }
        writer.WriteEndObject();

        writer.WritePropertyName(nameof(Weapons));
        writer.WriteStringValue(Weapons);

        writer.WritePropertyName(nameof(DisplayName));
        writer.WriteStringValue(DisplayName);
    }
    public void ReadJson(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) return;
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString()!;
                if (reader.Read())
                {
                    switch (prop)
                    {
                        case nameof(Name):
                            Name = reader.GetString()!;
                            break;
                        case nameof(Class):
                            Class = (EClass)reader.GetByte();
                            break;
                        case nameof(Branch):
                            Branch = (EBranch)reader.GetByte();
                            break;
                        case nameof(Team):
                            Team = reader.GetUInt64();
                            break;
                        case nameof(UnlockRequirements):
                            if (reader.TokenType == JsonTokenType.StartArray)
                            {
                                List<BaseUnlockRequirement> reqs = new List<BaseUnlockRequirement>(2);
                                while (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
                                {
                                    BaseUnlockRequirement? bur = BaseUnlockRequirement.Read(ref reader);
                                    while (reader.TokenType != JsonTokenType.EndObject) if (!reader.Read()) break;
                                    if (bur != null) reqs.Add(bur);
                                }
                                UnlockRequirements = reqs.ToArray();
                                while (reader.TokenType != JsonTokenType.EndArray) if (!reader.Read()) break;
                            }
                            break;
                        case nameof(Skillsets):
                            if (reader.TokenType == JsonTokenType.StartArray)
                            {
                                List<Skillset> sets = new List<Skillset>(2);
                                while (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
                                {
                                    Skillset skillset = Skillset.Read(ref reader);
                                    while (reader.TokenType != JsonTokenType.EndObject) if (!reader.Read()) break;
                                    sets.Add(skillset);
                                }
                                Skillsets = sets.ToArray();
                                while (reader.TokenType != JsonTokenType.EndArray) if (!reader.Read()) break;
                            }
                            break;
                        case nameof(TicketCost):
                            TicketCost = reader.GetUInt16();
                            break;
                        case nameof(IsPremium):
                            IsPremium = reader.GetBoolean();
                            break;
                        case nameof(PremiumCost):
                            PremiumCost = reader.GetSingle();
                            break;
                        case nameof(IsLoadout):
                            IsLoadout = reader.GetBoolean();
                            break;
                        case nameof(TeamLimit):
                            TeamLimit = reader.GetSingle();
                            break;
                        case nameof(Cooldown):
                            Cooldown = reader.GetSingle();
                            break;
                        case nameof(Weapons):
                            Weapons = reader.GetString()!;
                            break;
                        case nameof(AllowedUsers):
                            if (reader.TokenType == JsonTokenType.StartArray)
                            {
                                AllowedUsers = new List<ulong>(16);
                                while (reader.Read() && reader.TokenType == JsonTokenType.Number)
                                    AllowedUsers.Add(reader.GetUInt64());
                                while (reader.TokenType != JsonTokenType.EndArray) if (!reader.Read()) break;
                            }
                            break;
                        case nameof(Items):
                            if (reader.TokenType == JsonTokenType.StartArray)
                            {
                                Items = new List<KitItem>(32);
                                while (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
                                {
                                    KitItem item = new KitItem();
                                    item.ReadJson(ref reader);
                                    while (reader.TokenType != JsonTokenType.EndObject) if (!reader.Read()) break;
                                    Items.Add(item);
                                }
                                while (reader.TokenType != JsonTokenType.EndArray) if (!reader.Read()) break;
                            }
                            break;
                        case nameof(Clothes):
                            if (reader.TokenType == JsonTokenType.StartArray)
                            {
                                Clothes = new List<KitClothing>(7);
                                while (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
                                {
                                    KitClothing clothing = new KitClothing();
                                    clothing.ReadJson(ref reader);
                                    while (reader.TokenType != JsonTokenType.EndObject) if (!reader.Read()) break;
                                    Clothes.Add(clothing);
                                }
                                while (reader.TokenType != JsonTokenType.EndArray) if (!reader.Read()) break;
                            }
                            break;
                        case nameof(SignTexts):
                            if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                SignTexts = new Dictionary<string, string>(2);
                                while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
                                {
                                    string key = reader.GetString()!;
                                    if (reader.Read() && reader.TokenType == JsonTokenType.String)
                                        SignTexts.Add(key, reader.GetString()!);
                                }
                            }
                            break;
                    }
                }
            }
        }
    }
    public void AddSimpleLevelUnlock(int level)
    {
        int index = -1;
        for (int i = 0; i < UnlockRequirements.Length; i++)
        {
            BaseUnlockRequirement unlock = UnlockRequirements[i];
            if (unlock is LevelUnlockRequirement unlockLevel)
            {
                unlockLevel.UnlockLevel = level;
                index = i;
                break;
            }
        }
        if (index == -1)
        {
            LevelUnlockRequirement unlock = new LevelUnlockRequirement();
            unlock.UnlockLevel = level;
            BaseUnlockRequirement[] old = UnlockRequirements;
            UnlockRequirements = new BaseUnlockRequirement[old.Length + 1];
            if (old.Length > 0)
            {
                Array.Copy(old, 0, UnlockRequirements, 0, old.Length);
                UnlockRequirements[UnlockRequirements.Length - 1] = unlock;
            }
            else
            {
                UnlockRequirements[0] = unlock;
            }
        }
    }
    public bool RemoveLevelUnlock()
    {
        if (UnlockRequirements.Length == 0) return false;
        int index = -1;
        for (int i = 0; i < UnlockRequirements.Length; i++)
        {
            LevelUnlockRequirement unlock = new LevelUnlockRequirement();
            if (unlock is LevelUnlockRequirement unlockLevel)
            {
                index = i;
                break;
            }
        }
        if (index == -1) return false;
        BaseUnlockRequirement[] old = UnlockRequirements;
        UnlockRequirements = new BaseUnlockRequirement[old.Length - 1];
        if (old.Length == 1) return true;
        if (index != 0)
            Array.Copy(old, 0, UnlockRequirements, 0, index);
        Array.Copy(old, index + 1, UnlockRequirements, index, old.Length - index - 1);
        return true;
    }
}
public readonly struct Skillset : IEquatable<Skillset>
{
    public readonly EPlayerSpeciality Speciality;
    public readonly EPlayerOffense Offense;
    public readonly EPlayerDefense Defense;
    public readonly EPlayerSupport Support;

    public static readonly Skillset[] DEFAULT_SKILLSETS = new Skillset[]
    {
        new Skillset(EPlayerOffense.SHARPSHOOTER, 7),
        new Skillset(EPlayerOffense.PARKOUR, 2),
        new Skillset(EPlayerOffense.EXERCISE, 1),
        new Skillset(EPlayerOffense.CARDIO, 5),
        new Skillset(EPlayerDefense.VITALITY, 5),
    };
    public readonly int SpecialityIndex => (int)Speciality;
    public readonly int SkillIndex => Speciality switch
    {
        EPlayerSpeciality.OFFENSE => (int)Offense,
        EPlayerSpeciality.DEFENSE => (int)Defense,
        EPlayerSpeciality.SUPPORT => (int)Support,
        _ => -1
    };
    public readonly int Level;
    public Skillset(EPlayerOffense skill, int level)
    {
        Speciality = EPlayerSpeciality.OFFENSE;
        Offense = skill;
        Level = level;
        Defense = default;
        Support = default;
    }
    public Skillset(EPlayerDefense skill, int level)
    {
        Speciality = EPlayerSpeciality.DEFENSE;
        Defense = skill;
        Level = level;
        Offense = default;
        Support = default;
    }
    public Skillset(EPlayerSupport skill, int level)
    {
        Speciality = EPlayerSpeciality.SUPPORT;
        Support = skill;
        Level = level;
        Offense = default;
        Defense = default;
    }
    public readonly void ServerSet(UCPlayer player) => 
        player.Player.skills.ServerSetSkillLevel(SpecialityIndex, SkillIndex, Level);
    public static Skillset Read(ref Utf8JsonReader reader)
    {
        bool f2 = false;
        bool f3 = false;
        EPlayerSpeciality spec = default;
        EPlayerOffense offense = default;
        EPlayerDefense defense = default;
        EPlayerSupport support = default;
        int level = -1;
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            string? property = reader.GetString();
            if (reader.Read() && property != null)
            {
                switch (property)
                {
                    case "offense":
                        spec = EPlayerSpeciality.OFFENSE;
                        string? value2 = reader.GetString();
                        if (value2 != null)
                        {
                            Enum.TryParse(value2, true, out offense);
                            f2 = true;
                        }
                        break;
                    case "defense":
                        spec = EPlayerSpeciality.DEFENSE;
                        string? value3 = reader.GetString();
                        if (value3 != null)
                        {
                            Enum.TryParse(value3, true, out defense);
                            f2 = true;
                        }
                        break;
                    case "support":
                        spec = EPlayerSpeciality.SUPPORT;
                        string? value4 = reader.GetString();
                        if (value4 != null)
                        {
                            Enum.TryParse(value4, true, out support);
                            f2 = true;
                        }
                        break;
                    case "level":
                        if (reader.TryGetInt32(out level))
                        {
                            f3 = true;
                        }
                        break;
                }
            }
        }
        if (f2 && f3)
        {
            switch (spec)
            {
                case EPlayerSpeciality.OFFENSE:
                    return new Skillset(offense, level);
                case EPlayerSpeciality.DEFENSE:
                    return new Skillset(defense, level);
                case EPlayerSpeciality.SUPPORT:
                    return new Skillset(support, level);
            }
        }
        L.Log("Error parsing skillset.");
        return default;
    }
    public static void Write(Utf8JsonWriter writer, ref Skillset skillset)
    {
        switch (skillset.Speciality)
        {
            case EPlayerSpeciality.OFFENSE:
                writer.WriteString("offense", skillset.Offense.ToString());
                break;
            case EPlayerSpeciality.DEFENSE:
                writer.WriteString("defense", skillset.Defense.ToString());
                break;
            case EPlayerSpeciality.SUPPORT:
                writer.WriteString("support", skillset.Support.ToString());
                break;
        }
        writer.WriteNumber("level", skillset.Level);
    }
    public override bool Equals(object? obj) => obj is Skillset skillset && EqualsHelper(ref skillset, true);
    private bool EqualsHelper(ref Skillset skillset, bool compareLevel)
    {
        if (compareLevel && skillset.Level != Level) return false;
        if (skillset.Speciality == Speciality)
        {
            switch (Speciality)
            {
                case EPlayerSpeciality.OFFENSE:
                    return skillset.Offense == Offense;
                case EPlayerSpeciality.DEFENSE:
                    return skillset.Defense == Defense;
                case EPlayerSpeciality.SUPPORT:
                    return skillset.Support == Support;
            }
        }
        return false;
    }
    public override string ToString()
    {
        return Speciality switch
        {
            EPlayerSpeciality.OFFENSE => "Offense: " + Offense.ToString(),
            EPlayerSpeciality.DEFENSE => "Defense: " + Defense.ToString(),
            EPlayerSpeciality.SUPPORT => "Support: " + Support.ToString(),
            _ => "Invalid object."
        };
    }
    public override int GetHashCode()
    {
        int hashCode = 1232939970;
        hashCode = hashCode * -1521134295 + Speciality.GetHashCode();
        hashCode = hashCode * -1521134295 + Level.GetHashCode();
        switch (Speciality)
        {
            case EPlayerSpeciality.OFFENSE:
                hashCode = hashCode * -1521134295 + Offense.GetHashCode();
                break;
            case EPlayerSpeciality.DEFENSE:
                hashCode = hashCode * -1521134295 + Defense.GetHashCode();
                break;
            case EPlayerSpeciality.SUPPORT:
                hashCode = hashCode * -1521134295 + Support.GetHashCode();
                break;
        }
        return hashCode;
    }
    public bool Equals(Skillset other) => EqualsHelper(ref other, true);
    public bool TypeEquals(ref Skillset skillset) => EqualsHelper(ref skillset, false);
    public static bool operator ==(Skillset a, Skillset b) => a.EqualsHelper(ref b, true);
    public static bool operator !=(Skillset a, Skillset b) => !a.EqualsHelper(ref b, true);
}

public abstract class BaseUnlockRequirement
{
    private static bool hasReflected = false;
    private static void Reflect()
    {
        types.Clear();
        Type[] array = Assembly.GetExecutingAssembly().GetTypes();
        for (int i = 0; i < array.Length; i++)
        {
            Type type = array[i];
            if (!types.ContainsKey(type) && Attribute.GetCustomAttribute(type, typeof(UnlockRequirementAttribute)) is UnlockRequirementAttribute att)
            {
                types.Add(type, att.Properties);
            }
        }
        hasReflected = true;
    }
    private static readonly Dictionary<Type, string[]> types = new Dictionary<Type, string[]>(4);
    public abstract bool CanAccess(UCPlayer player);
    public static BaseUnlockRequirement? Read(ref Utf8JsonReader reader)
    {
        if (!hasReflected) Reflect();
        BaseUnlockRequirement? t = null;
        while (reader.TokenType == JsonTokenType.PropertyName || (reader.Read() && reader.TokenType == JsonTokenType.PropertyName))
        {
            string? property = reader.GetString();
            if (reader.Read() && property != null)
            {
                if (t == null)
                {
                    foreach (KeyValuePair<Type, string[]> propertyList in types)
                    {
                        for (int i = 0; i < propertyList.Value.Length; i++)
                        {
                            if (property.Equals(propertyList.Value[i], StringComparison.OrdinalIgnoreCase))
                            {
                                t = Activator.CreateInstance(propertyList.Key) as BaseUnlockRequirement;
                                goto done;
                            }
                        }
                    }
                }
                else
                {
                    t.ReadProperty(ref reader, property);
                }
                continue;
                done:
                if (t != null)
                    t.ReadProperty(ref reader, property);
                else
                {
                    L.LogWarning("Failed to find property \"" + property + "\" when parsing unlock requirements.");
                }
            }
        }
        return t;
    }
    public static void Write(Utf8JsonWriter writer, BaseUnlockRequirement requirement)
    {
        requirement.WriteProperties(writer);
    }

    protected abstract void ReadProperty(ref Utf8JsonReader reader, string property);
    protected abstract void WriteProperties(Utf8JsonWriter writer);
    public abstract string GetSignText(UCPlayer player);
}
[UnlockRequirement("unlock_level")]
public class LevelUnlockRequirement : BaseUnlockRequirement
{
    public int UnlockLevel = -1;
    public override bool CanAccess(UCPlayer player)
    {
        return Point.Points.GetLevel(player.CachedXP) >= UnlockLevel;
    }
    public override string GetSignText(UCPlayer player)
    {
        int lvl = Point.Points.GetLevel(player.CachedXP);
        return Translation.Translate("kit_required_level", player.Steam64, UnlockLevel.ToString(Data.Locale), lvl >= UnlockLevel ? UCWarfare.GetColorHex("kit_level_available") : UCWarfare.GetColorHex("kit_level_unavailable"));
    }
    protected override void ReadProperty(ref Utf8JsonReader reader, string property)
    {
        if (property.Equals("unlock_level", StringComparison.OrdinalIgnoreCase))
        {
            reader.TryGetInt32(out UnlockLevel);
        }
    }
    protected override void WriteProperties(Utf8JsonWriter writer)
    {
        writer.WriteNumber("unlock_level", UnlockLevel);
    }
}
[UnlockRequirement("unlock_rank")]
public class RankUnlockRequirement : BaseUnlockRequirement
{
    public int UnlockRank = -1;
    public override bool CanAccess(UCPlayer player)
    {
        ref Ranks.RankData data = ref Ranks.RankManager.GetRank(player, out bool success);
        return success && data.Order >= UnlockRank;
    }
    public override string GetSignText(UCPlayer player)
    {
        ref Ranks.RankData data = ref Ranks.RankManager.GetRank(player, out bool success);
        ref Ranks.RankData reqData = ref Ranks.RankManager.GetRank(UnlockRank, out _);
        return Translation.Translate("kit_required_rank", player.Steam64, reqData.ColorizedName(player.Steam64), success && data.Order >= reqData.Order ? UCWarfare.GetColorHex("kit_level_available") : UCWarfare.GetColorHex("kit_level_unavailable"));
    }
    protected override void ReadProperty(ref Utf8JsonReader reader, string property)
    {
        if (property.Equals("unlock_rank", StringComparison.OrdinalIgnoreCase))
        {
            reader.TryGetInt32(out UnlockRank);
        }
    }
    protected override void WriteProperties(Utf8JsonWriter writer)
    {
        writer.WriteNumber("unlock_rank", UnlockRank);
    }
}
[UnlockRequirement("unlock_presets", "quest_id")]
public class QuestUnlockRequirement : BaseUnlockRequirement
{
    public Guid QuestID = default;
    public Guid[] UnlockPresets = new Guid[0];
    public override bool CanAccess(UCPlayer player)
    {
        QuestManager.QuestComplete(player, QuestID);
        for (int i = 0; i < UnlockPresets.Length; i++)
        {
            if (!QuestManager.QuestComplete(player, UnlockPresets[i]))
                return false;
        }
        return true;
    }
    public override string GetSignText(UCPlayer player)
    {
        bool access = CanAccess(player);
        if (Assets.find(QuestID) is QuestAsset quest)
        {
            return Translation.Translate(access ? "kit_required_quest_done" : "kit_required_quest", player, quest.questName,
                access ? UCWarfare.GetColorHex("kit_level_available") : UCWarfare.GetColorHex("kit_level_unavailable"));
        }
        return Translation.Translate(access ? "kit_required_quest_done" : "kit_required_quest_unknown", player, UnlockPresets.Length.ToString(Data.Locale), 
            access ? UCWarfare.GetColorHex("kit_level_available") : UCWarfare.GetColorHex("kit_level_unavailable"), UnlockPresets.Length.S());
    }
    protected override void ReadProperty(ref Utf8JsonReader reader, string property)
    {
        if (property.Equals("unlock_presets", StringComparison.OrdinalIgnoreCase))
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                List<Guid> ids = new List<Guid>(4);
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TryGetGuid(out Guid guid) && !ids.Contains(guid))
                        ids.Add(guid);
                }
                UnlockPresets = ids.ToArray();
            }
        }
        else if (property.Equals("quest_id", StringComparison.OrdinalIgnoreCase))
        {
            if (!reader.TryGetGuid(out QuestID))
                L.LogWarning("Failed to convert " + property + " with value \"" + (reader.GetString() ?? "null") + "\" to a GUID.");
        }
    }
    protected override void WriteProperties(Utf8JsonWriter writer)
    {
        writer.WritePropertyName("unlock_presets");
        writer.WriteStartArray();
        for (int i = 0; i < UnlockPresets.Length; i++)
        {
            writer.WriteStringValue(UnlockPresets[i]);
        }
        writer.WriteEndArray();

        writer.WriteString("quest_id", QuestID);
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class UnlockRequirementAttribute : Attribute
{
    public string[] Properties => _properties;
    /// <param name="properties">MUST BE UNIQUE</param>
    public UnlockRequirementAttribute(params string[] properties)
    {
        _properties = properties;
    }
    private readonly string[] _properties;
}

public class KitItem : IJsonReadWrite
{
    public Guid id;
    public byte x;
    public byte y;
    public byte rotation;
    public string metadata;
    public byte amount;
    public byte page;
    public KitItem(Guid id, byte x, byte y, byte rotation, string metadata, byte amount, byte page)
    {
        this.id = id;
        this.x = x;
        this.y = y;
        this.rotation = rotation;
        this.metadata = metadata;
        this.amount = amount;
        this.page = page;
    }
    public KitItem() { }

    public void WriteJson(Utf8JsonWriter writer)
    {
        writer.WritePropertyName(nameof(id));
        writer.WriteStringValue(id);
        writer.WritePropertyName(nameof(x));
        writer.WriteNumberValue(x);
        writer.WritePropertyName(nameof(y));
        writer.WriteNumberValue(y);
        writer.WritePropertyName(nameof(rotation));
        writer.WriteNumberValue(rotation);
        writer.WritePropertyName(nameof(metadata));
        writer.WriteStringValue(metadata);
        writer.WritePropertyName(nameof(amount));
        writer.WriteNumberValue(amount);
        writer.WritePropertyName(nameof(page));
        writer.WriteNumberValue(page);
    }
    public void ReadJson(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) return;
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString()!;
                if (reader.Read())
                {
                    switch (prop)
                    {
                        case nameof(id):
                            id = reader.GetGuid();
                            break;
                        case nameof(x):
                            x = reader.GetByte();
                            break;
                        case nameof(y):
                            y = reader.GetByte();
                            break;
                        case nameof(rotation):
                            rotation = reader.GetByte();
                            break;
                        case nameof(metadata):
                            metadata = reader.GetString()!;
                            break;
                        case nameof(amount):
                            amount = reader.GetByte();
                            break;
                        case nameof(page):
                            page = reader.GetByte();
                            break;
                    }
                }
            }
        }
    }
}
public class KitClothing : IJsonReadWrite
{
    public Guid id;
    public string state;
    public EClothingType type;
    public KitClothing(Guid id, string state, EClothingType type)
    {
        this.id = id;
        this.state = state;
        this.type = type;
    }
    public KitClothing() { }

    public void WriteJson(Utf8JsonWriter writer)
    {
        writer.WritePropertyName(nameof(id));
        writer.WriteStringValue(id);
        writer.WritePropertyName(nameof(state));
        writer.WriteStringValue(state);
        writer.WritePropertyName(nameof(type));
        writer.WriteNumberValue((byte)type);
    }
    public void ReadJson(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) return;
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString()!;
                if (reader.Read())
                {
                    switch (prop)
                    {
                        case nameof(id):
                            id = reader.GetGuid();
                            break;
                        case nameof(state):
                            state = reader.GetString()!;
                            break;
                        case nameof(type):
                            type = (EClothingType)reader.GetByte();
                            break;
                    }
                }
            }
        }
    }
}
[Translatable]
public enum EBranch : byte
{
    DEFAULT,
    INFANTRY,
    ARMOR,
    AIRFORCE,
    SPECOPS,
    NAVY
}
public enum EClothingType : byte
{
    SHIRT,
    PANTS,
    VEST,
    HAT,
    MASK,
    BACKPACK,
    GLASSES
}
[Translatable]
public enum EClass : byte
{
    NONE, //0 
    UNARMED, //1
    SQUADLEADER, //2
    RIFLEMAN, //3
    MEDIC, //4
    BREACHER, //5
    AUTOMATIC_RIFLEMAN, //6
    GRENADIER, //7
    MACHINE_GUNNER, //8
    LAT, //9
    HAT, //10
    MARKSMAN, //11
    SNIPER, //12
    AP_RIFLEMAN, //13
    COMBAT_ENGINEER, //14
    CREWMAN, //15
    PILOT, //16
    SPEC_OPS // 17
}