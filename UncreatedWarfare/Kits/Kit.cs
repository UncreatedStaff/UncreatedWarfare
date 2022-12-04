using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Encoding;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Kits;

public class KitOld : IListItem, ITranslationArgument, ICloneable
{
    public const int CAPACITY = 256;
    public string Name;
    [CommandSettable]
    public Class Class;
    [CommandSettable]
    public Branch Branch;
    [CommandSettable]
    public ulong Team;
    public BaseUnlockRequirement[] UnlockRequirements;
    public Skillset[] Skillsets;
    [CommandSettable]
    public ushort CreditCost;
    [CommandSettable]
    public ushort UnlockLevel;
    [CommandSettable]
    public bool IsPremium;
    [CommandSettable]
    public float PremiumCost;
    [CommandSettable]
    public bool IsLoadout;
    [CommandSettable]
    public float TeamLimit;
    [CommandSettable]
    public float Cooldown;
    [CommandSettable]
    public bool Disabled;
    [CommandSettable]
    public SquadLevel SquadLevel;
    public List<PageItem> Items;
    public List<ClothingItem> Clothes;
    public Dictionary<string, string> SignTexts;
    [CommandSettable]
    public string Weapons;
    public PrimaryKey PrimaryKey { get; set; }
    public Kit(string name)
    {
        Name = name;
        Items = new List<PageItem>();
        Clothes = new List<ClothingItem>();
        Class = Class.None;
        Branch = Branch.Default;
        Team = 0;
        UnlockRequirements = Array.Empty<BaseUnlockRequirement>();
        Skillsets = Array.Empty<Skillset>();
        CreditCost = 0;
        UnlockLevel = 0;
        IsPremium = false;
        PremiumCost = 0;
        IsLoadout = false;
        TeamLimit = 1;
        Cooldown = 0;
        SignTexts = new Dictionary<string, string> { { L.DEFAULT, "Default" } };
        Weapons = string.Empty;
        Disabled = false;
        SquadLevel = SquadLevel.Member;
    }
    public Kit() : this("default") { }
    public Kit(string kitName, List<PageItem> items, List<ClothingItem> clothing)
    {
        Name = kitName;
        Items = items ?? new List<PageItem>();
        Clothes = clothing ?? new List<ClothingItem>();
        Class = Class.None;
        Branch = Branch.Default;
        Team = 0;
        UnlockRequirements = Array.Empty<BaseUnlockRequirement>();
        Skillsets = Array.Empty<Skillset>();
        CreditCost = 0;
        UnlockLevel = 0;
        IsPremium = false;
        PremiumCost = 0;
        IsLoadout = false;
        TeamLimit = 1;
        Cooldown = 0;
        SignTexts = new Dictionary<string, string> { { L.DEFAULT, kitName.ToProperCase() } };
        Weapons = string.Empty;
        Disabled = false;
        SquadLevel = SquadLevel.Member;
    }
    public void ApplyTo(KitOld kit)
    {
        kit.Class = Class;
        kit.Branch = Branch;
        kit.Team = Team;
        kit.Items = new List<PageItem>(Items.Select(x => (PageItem)x.Clone()));
        kit.Clothes = new List<ClothingItem>(Clothes.Select(x => (ClothingItem)x.Clone()));
        kit.UnlockRequirements = new BaseUnlockRequirement[UnlockRequirements.Length];
        for (int i = 0; i < UnlockRequirements.Length; ++i)
            kit.UnlockRequirements[i] = (BaseUnlockRequirement)UnlockRequirements[i].Clone();
        kit.Skillsets = new Skillset[Skillsets.Length];
        Array.Copy(Skillsets, kit.Skillsets, Skillsets.Length);
        kit.CreditCost = CreditCost;
        kit.UnlockLevel = UnlockLevel;
        kit.IsPremium = IsPremium;
        kit.PremiumCost = PremiumCost;
        kit.IsLoadout = IsLoadout;
        kit.TeamLimit = TeamLimit;
        kit.Cooldown = Cooldown;
        kit.Disabled = Disabled;
        kit.SquadLevel = SquadLevel;
        kit.SignTexts = new Dictionary<string, string>(SignTexts);
    }
    public object Clone()
    {
        KitOld clone = new KitOld(false)
        {
            Name = Name
        };
        ApplyTo(clone);
        return clone;
    }
    /// <summary>empty constructor</summary>
    public Kit(bool dummy) { }
    public string GetDisplayName()
    {
        if (SignTexts is null) return Name;
        if (SignTexts.TryGetValue(L.DEFAULT, out string val))
            return val ?? Name;
        if (SignTexts.Count > 0)
            return SignTexts.FirstOrDefault().Value ?? Name;
        return Name;
    }
    public static KitOld?[] ReadMany(ByteReader R)
    {
        KitOld?[] kits = new KitOld[R.ReadInt32()];
        for (int i = 0; i < kits.Length; i++)
        {
            kits[i] = Read(R);
        }
        return kits;
    }
    public static KitOld? Read(ByteReader R)
    {
        if (R.ReadUInt8() == 1) return null;
        KitOld kit = new KitOld(true);
        kit.PrimaryKey = R.ReadInt32();
        kit.Name = R.ReadString();
        ushort itemCount = R.ReadUInt16();
        ushort clothesCount = R.ReadUInt16();
        List<PageItem> items = new List<PageItem>(itemCount);
        List<ClothingItem> clothes = new List<ClothingItem>(clothesCount);
        for (int i = 0; i < itemCount; i++)
        {
            items.Add(new PageItem()
            {
                Item = R.ReadGUID(),
                Amount = R.ReadUInt8(),
                Page = R.ReadUInt8(),
                X = R.ReadUInt8(),
                Y = R.ReadUInt8(),
                Rotation = R.ReadUInt8(),
                State = R.ReadBytes() ?? new byte[0]
            });
        }
        for (int i = 0; i < clothesCount; i++)
        {
            clothes.Add(new ClothingItem()
            {
                Item = R.ReadGUID(),
                Type = R.ReadEnum<ClothingType>()
            });
        }
        kit.Items = items;
        kit.Clothes = clothes;
        kit.Branch = R.ReadEnum<Branch>();
        kit.Class = R.ReadEnum<Class>();
        kit.Cooldown = R.ReadFloat();
        kit.IsPremium = R.ReadBool();
        kit.IsLoadout = R.ReadBool();
        kit.PremiumCost = R.ReadFloat();
        kit.Team = R.ReadUInt64();
        kit.TeamLimit = R.ReadFloat();
        kit.CreditCost = R.ReadUInt16();
        kit.UnlockLevel = R.ReadUInt16();
        kit.Disabled = R.ReadBool();
        kit.SquadLevel = R.ReadEnum<SquadLevel>();
        return kit;
    }
    public static void WriteMany(ByteWriter W, KitOld?[] kits)
    {
        W.Write(kits.Length);
        for (int i = 0; i < kits.Length; i++)
            Write(W, kits[i]);
    }
    public static void Write(ByteWriter W, KitOld? kit)
    {
        if (kit == null)
        {
            W.Write((byte)1);
            return;
        }
        else W.Write((byte)0);
        W.Write(kit.PrimaryKey);
        W.Write(kit.Name);
        W.Write((ushort)kit.Items.Count);
        W.Write((ushort)kit.Clothes.Count);
        for (int i = 0; i < kit.Items.Count; i++)
        {
            PageItem item = kit.Items[i];
            W.Write(item.Item);
            W.Write(item.Amount);
            W.Write(item.Page);
            W.Write(item.X);
            W.Write(item.Y);
            W.Write(item.Rotation);
            W.Write(item.State);
        }
        for (int i = 0; i < kit.Clothes.Count; i++)
        {
            ClothingItem clothing = kit.Clothes[i];
            W.Write(clothing.Item);
            W.Write(clothing.Type);
        }
        W.Write(kit.Branch);
        W.Write(kit.Class);
        W.Write(kit.Cooldown);
        W.Write(kit.IsPremium);
        W.Write(kit.IsLoadout);
        W.Write(kit.PremiumCost);
        W.Write(kit.Team);
        W.Write(kit.TeamLimit);
        W.Write(kit.CreditCost);
        W.Write(kit.UnlockLevel);
        W.Write(kit.Disabled);
        W.Write(kit.SquadLevel);
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
    public void AddUnlockRequirement(BaseUnlockRequirement req)
    {
        int index = -1;
        for (int i = 0; i < UnlockRequirements.Length; i++)
        {
            BaseUnlockRequirement unlock = UnlockRequirements[i];
            if (req == unlock)
            {
                index = i;
                break;
            }
        }
        if (index == -1)
        {
            BaseUnlockRequirement[] old = UnlockRequirements;
            UnlockRequirements = new BaseUnlockRequirement[old.Length + 1];
            if (old.Length > 0)
            {
                Array.Copy(old, 0, UnlockRequirements, 0, old.Length);
                UnlockRequirements[UnlockRequirements.Length - 1] = req;
            }
            else
            {
                UnlockRequirements[0] = req;
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
    public void AddSkillset(Skillset set)
    {
        int index = -1;
        for (int i = 0; i < Skillsets.Length; i++)
        {
            ref Skillset skillset = ref Skillsets[i];
            if (skillset == set)
            {
                index = i;
                break;
            }
        }
        if (index == -1)
        {
            Skillset[] old = Skillsets;
            Skillsets = new Skillset[old.Length + 1];
            if (old.Length > 0)
            {
                Array.Copy(old, 0, Skillsets, 0, old.Length);
                Skillsets[Skillsets.Length - 1] = set;
            }
            else
            {
                Skillsets[0] = set;
            }
        }
    }
    public bool RemoveSkillset(Skillset set)
    {
        if (Skillsets.Length == 0) return false;
        int index = -1;
        for (int i = 0; i < Skillsets.Length; i++)
        {
            ref Skillset skillset = ref Skillsets[i];
            if (skillset == set)
            {
                index = i;
                break;
            }
        }
        if (index == -1) return false;
        Skillset[] old = Skillsets;
        Skillsets = new Skillset[old.Length - 1];
        if (old.Length == 1) return true;
        if (index != 0)
            Array.Copy(old, 0, Skillsets, 0, index);
        Array.Copy(old, index + 1, Skillsets, index, old.Length - index - 1);
        return true;
    }
    [FormatDisplay("Kit Id")]
    public const string ID_FORMAT = "i";
    [FormatDisplay("Display Name")]
    public const string DISPLAY_NAME_FORMAT = "d";
    [FormatDisplay("Class (" + nameof(Kits.Class) + ")")]
    public const string CLASS_FORMAT = "c";
    string ITranslationArgument.Translate(string language, string? format, UCPlayer? target, ref TranslationFlags flags)
    {
        if (format is not null)
        {
            if (format.Equals(ID_FORMAT, StringComparison.Ordinal))
                return Name;
            else if (format.Equals(CLASS_FORMAT, StringComparison.Ordinal))
                return Localization.TranslateEnum(Class, language);
        }
        if (SignTexts.TryGetValue(language, out string dspTxt))
            return dspTxt;

        return SignTexts.Values.FirstOrDefault() ?? Name;
    }

}

public class Kit : IListItem
{
    public PrimaryKey PrimaryKey { get; set; }
    public PrimaryKey FactionKey { get; set; }
    public string Id { get; set; }
    public Class Class { get; set; }
    public Branch Branch { get; set; }
    public KitType Type { get; set; }
    public bool Disabled { get; set; }
    public int Season { get; set; }
    public SquadLevel SquadLevel { get; set; }
    public TranslationList SignText { get; set; }
    public IKitItem[] Items { get; set; }
    public BaseUnlockRequirement[] UnlockRequirements { get; set; }
    public Skillset[] Skillsets { get; set; }
    public PrimaryKey[] FactionBlacklist { get; set; }
    public string? WeaponText { get; set; }
    public FactionInfo? Faction
    {
        get => FactionKey.IsValid ? TeamManager.GetFactionInfo(FactionKey) : null;
        set
        {
            if (value is null)
                FactionKey = PrimaryKey.NotAssigned;
            else if (!value.PrimaryKey.IsValid)
                throw new ArgumentException("Invalid faction provided, no key set.", nameof(value));
            else FactionKey = value.PrimaryKey;
        }
    }

    public Kit(string id, Class @class, Branch branch, KitType type, SquadLevel squadLevel, string? weaponText, FactionInfo? faction)
    {
        Faction = faction;
        this.Id = id;
        this.Class = @class;
        this.Branch = branch;
        this.Type = type;
        this.SquadLevel = squadLevel;
        this.WeaponText = weaponText;
        SignText = new TranslationList(id);
        Items = Array.Empty<IKitItem>();
        UnlockRequirements = Array.Empty<BaseUnlockRequirement>();
        Skillsets = Array.Empty<Skillset>();
        FactionBlacklist = Array.Empty<PrimaryKey>();
    }
    public Kit() { }

    public bool IsBlacklisted(FactionInfo faction)
    {
        if (FactionBlacklist.NullOrEmpty() || faction is null || !faction.PrimaryKey.IsValid) return false;
        int pk = faction.PrimaryKey.Key;
        for (int i = 0; i < FactionBlacklist.Length; ++i)
            if (FactionBlacklist[i].Key == pk)
                return true;

        return false;
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
    public static Skillset Read(ByteReader reader)
    {
        EPlayerSpeciality speciality = (EPlayerSpeciality)reader.ReadUInt8();
        byte val = reader.ReadUInt8();
        int level = reader.ReadUInt8();
        return speciality switch
        {
            EPlayerSpeciality.SUPPORT => new Skillset((EPlayerSupport)val, level),
            EPlayerSpeciality.DEFENSE => new Skillset((EPlayerDefense)val, level),
            EPlayerSpeciality.OFFENSE => new Skillset((EPlayerOffense)val, level),
            _ => throw new Exception("Invalid value of specialty while reading skillset.")
        };
    }
    public static void Write(ByteWriter writer, Skillset skillset)
    {
        writer.Write((byte)skillset.Speciality);
        writer.Write((byte)skillset.SkillIndex);
        writer.Write((byte)skillset.Level);
    }
    public readonly void ServerSet(UCPlayer player) =>
        player.Player.skills.ServerSetSkillLevel(SpecialityIndex, SkillIndex, Level);
    public static Skillset Read(ref Utf8JsonReader reader)
    {
        bool valFound = false;
        bool lvlFound = false;
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
                            valFound = true;
                        }
                        break;
                    case "defense":
                        spec = EPlayerSpeciality.DEFENSE;
                        string? value3 = reader.GetString();
                        if (value3 != null)
                        {
                            Enum.TryParse(value3, true, out defense);
                            valFound = true;
                        }
                        break;
                    case "support":
                        spec = EPlayerSpeciality.SUPPORT;
                        string? value4 = reader.GetString();
                        if (value4 != null)
                        {
                            Enum.TryParse(value4, true, out support);
                            valFound = true;
                        }
                        break;
                    case "level":
                        if (reader.TryGetInt32(out level))
                        {
                            lvlFound = true;
                        }
                        break;
                }
            }
        }
        if (valFound && lvlFound)
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
    public override bool Equals(object? obj) => obj is Skillset skillset && EqualsHelper(in skillset, true);
    private bool EqualsHelper(in Skillset skillset, bool compareLevel)
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
    public bool Equals(Skillset other) => EqualsHelper(in other, true);
    public bool TypeEquals(in Skillset skillset) => EqualsHelper(in skillset, false);
    public static void SetDefaultSkills(UCPlayer player)
    {
        player.Player.skills.ServerSetSkillLevel((int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.SHARPSHOOTER, 7);
        player.Player.skills.ServerSetSkillLevel((int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.PARKOUR, 2);
        player.Player.skills.ServerSetSkillLevel((int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.EXERCISE, 1);
        player.Player.skills.ServerSetSkillLevel((int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.CARDIO, 5);
        player.Player.skills.ServerSetSkillLevel((int)EPlayerSpeciality.DEFENSE, (int)EPlayerDefense.VITALITY, 5);
    }
    public static bool operator ==(Skillset a, Skillset b) => a.EqualsHelper(in b, true);
    public static bool operator !=(Skillset a, Skillset b) => !a.EqualsHelper(in b, true);
    public const string COLUMN_PK = "pk";
    public const string COLUMN_TYPE = "Type";
    public const string COLUMN_SKILL = "Skill";
    public const string COLUMN_LEVEL = "Level";
    public static Schema GetDefaultSchema(string tableName, string fkColumn, string mainTable, string mainPkColumn, bool oneToOne = false, bool hasPk = false)
    {
        if (!oneToOne && fkColumn.Equals(COLUMN_PK, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Foreign key column may not be the same as \"" + COLUMN_PK + "\".", nameof(fkColumn));
        int ct = 4;
        if (!oneToOne && hasPk)
            ++ct;
        Schema.Column[] columns = new Schema.Column[ct];
        int index = 0;
        if (!oneToOne && hasPk)
        {
            columns[0] = new Schema.Column(COLUMN_PK, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true
            };
        }
        else index = -1;
        columns[++index] = new Schema.Column(fkColumn, SqlTypes.INCREMENT_KEY)
        {
            PrimaryKey = oneToOne,
            AutoIncrement = oneToOne,
            ForeignKey = true,
            ForeignKeyColumn = mainPkColumn,
            ForeignKeyTable = mainTable
        };
        columns[++index] = new Schema.Column(COLUMN_TYPE, SqlTypes.BYTE);
        columns[++index] = new Schema.Column(COLUMN_SKILL, SqlTypes.BYTE);
        columns[++index] = new Schema.Column(COLUMN_LEVEL, SqlTypes.BYTE);
        return new Schema(tableName, columns, false, typeof(Skillset));
    }
}
[JsonConverter(typeof(UnlockRequirementConverter))]
public abstract class BaseUnlockRequirement : ICloneable
{
    private static bool hasReflected = false;
    private static void Reflect()
    {
        types.Clear();
        foreach (Type type in Assembly.GetExecutingAssembly().GetTypes().Where(typeof(BaseUnlockRequirement).IsAssignableFrom))
        {
            if (Attribute.GetCustomAttribute(type, typeof(UnlockRequirementAttribute)) is UnlockRequirementAttribute att && !types.ContainsKey(att.Type))
            {
                types.Add(att.Type, new KeyValuePair<Type, string[]>(type, att.Properties));
            }
        }
        hasReflected = true;
    }
    private static readonly Dictionary<int, KeyValuePair<Type, string[]>> types = new Dictionary<int, KeyValuePair<Type, string[]>>(4);
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
                    foreach (KeyValuePair<int, KeyValuePair<Type, string[]>> propertyList in types)
                    {
                        for (int i = 0; i < propertyList.Value.Value.Length; i++)
                        {
                            if (property.Equals(propertyList.Value.Value[i], StringComparison.OrdinalIgnoreCase))
                            {
                                t = Activator.CreateInstance(propertyList.Value.Key) as BaseUnlockRequirement;
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
    public abstract object Clone();
    protected abstract void Read(ByteReader reader);
    protected abstract void Write(ByteWriter writer);
    public const string COLUMN_PK = "pk";
    public const string COLUMN_JSON = "JSON";
    public static Schema GetDefaultSchema(string tableName, string fkColumn, string mainTable, string mainPkColumn, bool oneToOne = false, bool hasPk = false)
    {
        if (!oneToOne && fkColumn.Equals(COLUMN_PK, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Foreign key column may not be the same as \"" + COLUMN_PK + "\".", nameof(fkColumn));
        int ct = 2;
        if (!oneToOne && hasPk)
            ++ct;
        Schema.Column[] columns = new Schema.Column[ct];
        int index = 0;
        if (!oneToOne && hasPk)
        {
            columns[0] = new Schema.Column(COLUMN_PK, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true
            };
        }
        else index = -1;
        columns[++index] = new Schema.Column(fkColumn, SqlTypes.INCREMENT_KEY)
        {
            PrimaryKey = oneToOne,
            AutoIncrement = oneToOne,
            ForeignKey = true,
            ForeignKeyColumn = mainPkColumn,
            ForeignKeyTable = mainTable
        };
        columns[++index] = new Schema.Column(COLUMN_JSON, SqlTypes.STRING_255);
        return new Schema(tableName, columns, false, typeof(BaseUnlockRequirement));
    }
}
public class UnlockRequirementConverter : JsonConverter<BaseUnlockRequirement>
{
    public override BaseUnlockRequirement? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => BaseUnlockRequirement.Read(ref reader);
    public override void Write(Utf8JsonWriter writer, BaseUnlockRequirement value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        BaseUnlockRequirement.Write(writer, value);
        writer.WriteEndObject();
    }
}
[UnlockRequirement(1, "unlock_level")]
public class LevelUnlockRequirement : BaseUnlockRequirement
{
    public int UnlockLevel = -1;
    public override bool CanAccess(UCPlayer player)
    {
        return player.Rank.Level >= UnlockLevel;
    }
    public override string GetSignText(UCPlayer player)
    {
        if (UnlockLevel == 0)
            return string.Empty;

        int lvl = Points.GetLevel(player.CachedXP);
        return T.KitRequiredLevel.Translate(player, RankData.GetRankAbbreviation(UnlockLevel), lvl >= UnlockLevel ? UCWarfare.GetColor("kit_level_available") : UCWarfare.GetColor("kit_level_unavailable"));
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
    public override object Clone() => new LevelUnlockRequirement() { UnlockLevel = UnlockLevel };
    protected override void Read(ByteReader reader)
    {
        UnlockLevel = reader.ReadInt32();
    }
    protected override void Write(ByteWriter writer)
    {
        writer.Write(UnlockLevel);
    }
}
[UnlockRequirement(2, "unlock_rank")]
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
        return T.KitRequiredRank.Translate(player, reqData, success && data.Order >= reqData.Order ? UCWarfare.GetColor("kit_level_available") : UCWarfare.GetColor("kit_level_unavailable"));
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
    public override object Clone() => new RankUnlockRequirement() { UnlockRank = UnlockRank };
    protected override void Read(ByteReader reader)
    {
        UnlockRank = reader.ReadInt32();
    }
    protected override void Write(ByteWriter writer)
    {
        writer.Write(UnlockRank);
    }
}
[UnlockRequirement(3, "unlock_presets", "quest_id")]
public class QuestUnlockRequirement : BaseUnlockRequirement
{
    public Guid QuestID;
    public Guid[] UnlockPresets = Array.Empty<Guid>();
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
        if (access)
            return T.KitRequiredQuestsComplete.Translate(player);
        if (Assets.find(QuestID) is QuestAsset quest)
            return T.KitRequiredQuest.Translate(player, quest, UCWarfare.GetColor("kit_level_unavailable"));

        return T.KitRequiredQuestsMultiple.Translate(player, UnlockPresets.Length, UCWarfare.GetColor("kit_level_unavailable"), UnlockPresets.Length.S());
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
    public override object Clone()
    {
        QuestUnlockRequirement req = new QuestUnlockRequirement() { QuestID = QuestID };
        req.UnlockPresets = new Guid[UnlockPresets.Length];
        Array.Copy(UnlockPresets, req.UnlockPresets, UnlockPresets.Length);
        return req;
    }
    protected override void Read(ByteReader reader)
    {
        QuestID = reader.ReadGUID();
        UnlockPresets = reader.ReadGuidArray();
    }
    protected override void Write(ByteWriter writer)
    {
        writer.Write(QuestID);
        writer.Write(UnlockPresets);
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class UnlockRequirementAttribute : Attribute
{
    public string[] Properties => _properties;
    public int Type => _type;
    /// <param name="properties">Must be unique among other unlock requirements.</param>
    public UnlockRequirementAttribute(int type, params string[] properties)
    {
        _properties = properties;
        _type = type;
    }
    private readonly string[] _properties;
    private readonly int _type;
}
public interface IClothingJar
{
    ClothingType Type { get; set; }
}

public interface IKitItem
{
    public ItemAsset? GetItem(KitOld kit, FactionInfo targetTeam, out byte amount, out byte[] state);
}
public interface IItemJar
{
    byte X { get; set; }
    byte Y { get; set; }
    byte Rotation { get; set; }
    Page Page { get; set; }
}
public interface IAssetRedirect
{
    RedirectType RedirectType { get; set; }
}
public interface IClothing
{
    Guid Item { get; set; }
    [JsonConverter(typeof(Base64Converter))]
    byte[] State { get; set; }
}
public interface IItem
{
    [JsonConverter(typeof(Base64Converter))]
    byte[] State { get; set; }
    Guid Item { get; set; }
    byte Amount { get; set; }
}

public class AssetRedirectItem : ICloneable, IItemJar, IAssetRedirect, IKitItem
{
    public RedirectType RedirectType { get; set; }
    public byte X { get; set; }
    public byte Y { get; set; }
    public byte Rotation { get; set; }
    public Page Page { get; set; }
    public AssetRedirectItem() { }
    public AssetRedirectItem(RedirectType redirectType, byte x, byte y, byte rotation, Page page)
    {
        RedirectType = redirectType;
        X = x;
        Y = y;
        Rotation = rotation;
        Page = page;
    }
    public AssetRedirectItem(AssetRedirectItem copy)
    {
        RedirectType = copy.RedirectType;
        X = copy.X;
        Y = copy.Y;
        Rotation = copy.Rotation;
        Page = copy.Page;
    }
    public object Clone() => new AssetRedirectItem(this);
    public ItemAsset? GetItem(KitOld kit, FactionInfo targetTeam, out byte amount, out byte[] state) =>
        TeamManager.GetRedirectInfo(RedirectType, kit.Faction, targetTeam, out state, out amount);
}
public class AssetRedirectClothing : ICloneable, IClothingJar, IKitItem
{
    public RedirectType RedirectType { get; set; }
    public ClothingType Type { get; set; }
    public AssetRedirectClothing() { }
    public AssetRedirectClothing(RedirectType redirectType, ClothingType type)
    {
        RedirectType = redirectType;
        Type = type;
    }
    public AssetRedirectClothing(AssetRedirectClothing copy)
    {
        RedirectType = copy.RedirectType;
        Type = copy.Type;
    }
    public object Clone() => new AssetRedirectClothing(this);
    public ItemAsset? GetItem(KitOld kit, FactionInfo targetTeam, out byte amount, out byte[] state) =>
        TeamManager.GetRedirectInfo(RedirectType, kit.Faction, targetTeam, out state, out amount);
}
public class PageItem : ICloneable, IItemJar, IItem, IKitItem
{
    private Guid _item;
    private bool _isLegacyRedirect;
    private RedirectType _legacyRedirect;

    [JsonPropertyName("id")]
    public Guid Item
    {
        get => _item;
        set
        {
            _item = value;
            _isLegacyRedirect = TeamManager.GetLegacyRedirect(value, out _legacyRedirect);
        }
    }

    [JsonPropertyName("x")]
    public byte X { get; set; }

    [JsonPropertyName("y")]
    public byte Y { get; set; }

    [JsonPropertyName("rotation")]
    public byte Rotation { get; set; }

    [JsonPropertyName("page")]
    public Page Page { get; set; }

    [JsonPropertyName("amount")]
    public byte Amount { get; set; }

    [JsonPropertyName("metadata")]
    [JsonConverter(typeof(Base64Converter))]
    public byte[] State { get; set; }
    
    public PageItem(Guid item, byte x, byte y, byte rotation, byte[] state, byte amount, Page page)
    {
        this.Item = item;
        this.X = x;
        this.Y = y;
        this.Rotation = rotation;
        this.Page = page;
        this.Amount = amount;
        this.State = state;
    }
    public PageItem(PageItem copy)
    {
        Item = copy.Item;
        X = copy.X;
        Y = copy.Y;
        Rotation = copy.Rotation;
        Page = copy.Page;
        Amount = copy.Amount;
        State = copy.State;
    }
    public PageItem() { }
    public object Clone() => new PageItem(this);
    public const string COLUMN_PK = "pk";
    public const string COLUMN_GUID = "Item";
    public const string COLUMN_X = "X";
    public const string COLUMN_Y = "Y";
    public const string COLUMN_ROTATION = "Rotation";
    public const string COLUMN_PAGE = "Page";
    public const string COLUMN_AMOUNT = "Amount";
    public const string COLUMN_METADATA = "Metadata";
    public static Schema GetDefaultSchema(string tableName, string fkColumn, string mainTable, string mainPkColumn, bool includePage = true, bool oneToOne = false, bool hasPk = false)
    {
        if (!oneToOne && fkColumn.Equals(COLUMN_PK, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Foreign key column may not be the same as \"" + COLUMN_PK + "\".", nameof(fkColumn));
        int ct = 7;
        if (!oneToOne && hasPk)
            ++ct;
        if (includePage)
            ++ct;
        Schema.Column[] columns = new Schema.Column[ct];
        int index = 0;
        if (!oneToOne && hasPk)
        {
            columns[0] = new Schema.Column(COLUMN_PK, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true
            };
        }
        else index = -1;
        columns[++index] = new Schema.Column(fkColumn, SqlTypes.INCREMENT_KEY)
        {
            PrimaryKey = oneToOne,
            AutoIncrement = oneToOne,
            ForeignKey = true,
            ForeignKeyColumn = mainPkColumn,
            ForeignKeyTable = mainTable
        };
        columns[++index] = new Schema.Column(COLUMN_GUID, SqlTypes.GUID);
        columns[++index] = new Schema.Column(COLUMN_X, SqlTypes.BYTE);
        columns[++index] = new Schema.Column(COLUMN_Y, SqlTypes.BYTE);
        columns[++index] = new Schema.Column(COLUMN_ROTATION, SqlTypes.BYTE);
        if (includePage)
            columns[++index] = new Schema.Column(COLUMN_PAGE, SqlTypes.BYTE);
        columns[++index] = new Schema.Column(COLUMN_AMOUNT, SqlTypes.BYTE);
        columns[++index] = new Schema.Column(COLUMN_METADATA, SqlTypes.BYTES_255);
        return new Schema(tableName, columns, false, typeof(PageItem));
    }

    public ItemAsset? GetItem(KitOld kit, FactionInfo targetTeam, out byte amount, out byte[] state)
    {
        if (_isLegacyRedirect)
            return TeamManager.GetRedirectInfo(_legacyRedirect, kit.Faction, targetTeam, out state, out amount);

        if (Assets.find(Item) is ItemAsset item)
        {
            amount = Amount < 1 ? item.amount : Amount;
            state = State is null ? item.getState(EItemOrigin.ADMIN) : Util.CloneBytes(State);
            return item;
        }

        state = Array.Empty<byte>();
        amount = default;
        return null;
    }
}
public class ClothingItem : ICloneable, IClothingJar, IClothing, IKitItem
{
    private Guid _item;
    private bool _isLegacyRedirect;
    private RedirectType _legacyRedirect;

    [JsonPropertyName("id")]
    public Guid Item
    {
        get => _item;
        set
        {
            _item = value;
            _isLegacyRedirect = TeamManager.GetLegacyRedirect(value, out _legacyRedirect);
        }
    }

    [JsonPropertyName("type")]
    public ClothingType Type { get; set; }

    [JsonPropertyName("metadata")]
    [JsonConverter(typeof(Base64Converter))]
    public byte[] State { get; set; }
    
    public ClothingItem(Guid id, ClothingType type, byte[] state)
    {
        this.Item = id;
        this.Type = type;
        this.State = state ?? Array.Empty<byte>();
    }

    public ClothingItem(ClothingItem copy)
    {
        Item = copy.Item;
        Type = copy.Type;
        State = copy.State;
    }

    public ClothingItem() { }

    public object Clone() => new ClothingItem(this);
    public ItemAsset? GetItem(KitOld kit, FactionInfo targetTeam, out byte amount, out byte[] state)
    {
        amount = 1;
        if (_isLegacyRedirect)
            return TeamManager.GetRedirectInfo(_legacyRedirect, kit.Faction, targetTeam, out state, out amount);

        if (Assets.find(Item) is ItemAsset item)
        {
            state = State.NullOrEmpty() ? item.getState(EItemOrigin.ADMIN) : Util.CloneBytes(State);
            return item;
        }

        state = Array.Empty<byte>();
        return null;
    }
}

/// <summary>Max field character limit: <see cref="KitEx.SQUAD_LEVEL_MAX_CHAR_LIMIT"/>.</summary>
[Translatable("Squad Level")]
public enum SquadLevel : byte
{
    [Translatable("Member")]
    Member = 0,
    [Translatable("Commander")]
    Commander = 4
}
/// <summary>Max field character limit: <see cref="KitEx.BRANCH_MAX_CHAR_LIMIT"/>.</summary>
[Translatable("Branch")]
public enum Branch : byte
{
    Default,
    Infantry,
    Armor,
    [Translatable("Air Force")]
    Airforce,
    [Translatable("Special Ops")]
    SpecOps,
    Navy
}
/// <summary>Max field character limit: <see cref="KitEx.CLOTHING_MAX_CHAR_LIMIT"/>.</summary>
public enum ClothingType : byte
{
    Shirt,
    Pants,
    Vest,
    Hat,
    Mask,
    Backpack,
    Glasses
}

/// <summary>Max field character limit: <see cref="KitEx.TYPE_MAX_CHAR_LIMIT"/>.</summary>
[Translatable("Kit Type")]
public enum KitType : byte
{
    Public,
    Elite,
    Special,
    Loadout
}

/// <summary>Max field character limit: <see cref="KitEx.REDIRECT_TYPE_CHAR_LIMIT"/>.</summary>
public enum RedirectType : byte
{
    Shirt,
    Pants,
    Vest,
    Hat,
    Mask,
    Backpack,
    Glasses,
    AmmoSupply,
    BuildSupply,
    RallyPoint,
    Radio,
    ZoneBlocker
}

public enum Page : byte
{
    Primary = 0,
    Secondary = 1,
    Hands = 2,
    Backpack = 3,
    Vest = 4,
    Shirt = 5,
    Pants = 6,
    Storage = 7,
    Area = 8
}
/// <summary>Max field character limit: <see cref="KitEx.CLASS_MAX_CHAR_LIMIT"/>.</summary>
[JsonConverter(typeof(ClassConverter))]
[Translatable("Kit Class")]
public enum Class : byte
{
    None = 0,
    [Translatable(LanguageAliasSet.RUSSIAN, "Безоружный")]
    [Translatable(LanguageAliasSet.SPANISH, "Desarmado")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Neinarmat")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Desarmado")]
    [Translatable(LanguageAliasSet.POLISH, "Nieuzbrojony")]
    Unarmed = 1,
    [Translatable("Squad Leader")]
    [Translatable(LanguageAliasSet.RUSSIAN, "Лидер отряда")]
    [Translatable(LanguageAliasSet.SPANISH, "Líder De Escuadrón")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Lider de Echipa")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Líder de Esquadrão")]
    [Translatable(LanguageAliasSet.POLISH, "Dowódca Oddziału")]
    Squadleader = 2,
    [Translatable(LanguageAliasSet.RUSSIAN, "Стрелок")]
    [Translatable(LanguageAliasSet.SPANISH, "Fusilero")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Puscas")]
    [Translatable(LanguageAliasSet.POLISH, "Strzelec")]
    Rifleman = 3,
    [Translatable(LanguageAliasSet.RUSSIAN, "Медик")]
    [Translatable(LanguageAliasSet.SPANISH, "Médico")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Medic")]
    [Translatable(LanguageAliasSet.POLISH, "Medyk")]
    Medic = 4,
    [Translatable(LanguageAliasSet.RUSSIAN, "Нарушитель")]
    [Translatable(LanguageAliasSet.SPANISH, "Brechador")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Breacher")]
    [Translatable(LanguageAliasSet.POLISH, "Wyłamywacz")]
    Breacher = 5,
    [Translatable(LanguageAliasSet.RUSSIAN, "Солдат с автоматом")]
    [Translatable(LanguageAliasSet.SPANISH, "Fusilero Automático")]
    [Translatable(LanguageAliasSet.SPANISH, "Puscas Automat")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Fuzileiro Automobilístico")]
    [Translatable(LanguageAliasSet.POLISH, "Strzelec Automatyczny")]
    AutomaticRifleman = 6,
    [Translatable(LanguageAliasSet.RUSSIAN, "Гренадёр")]
    [Translatable(LanguageAliasSet.SPANISH, "Granadero")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Grenadier")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Granadeiro")]
    [Translatable(LanguageAliasSet.POLISH, "Grenadier")]
    Grenadier = 7,
    [Translatable(LanguageAliasSet.ROMANIAN, "Mitralior")]
    MachineGunner = 8,
    [Translatable("LAT")]
    [Translatable(LanguageAliasSet.RUSSIAN, "Лёгкий противотанк")]
    [Translatable(LanguageAliasSet.SPANISH, "Anti-Tanque Ligero")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Anti-Tanc Usor")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Anti-Tanque Leve")]
    [Translatable(LanguageAliasSet.POLISH, "Lekka Piechota Przeciwpancerna")]
    LAT = 9,
    [Translatable("HAT")]
    HAT = 10,
    [Translatable(LanguageAliasSet.RUSSIAN, "Марксман")]
    [Translatable(LanguageAliasSet.SPANISH, "Tirador Designado")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Lunetist-Usor")]
    [Translatable(LanguageAliasSet.POLISH, "Zwiadowca")]
    Marksman = 11,
    [Translatable(LanguageAliasSet.RUSSIAN, "Снайпер")]
    [Translatable(LanguageAliasSet.SPANISH, "Francotirador")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Lunetist")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Franco-Atirador")]
    [Translatable(LanguageAliasSet.POLISH, "Snajper")]
    Sniper = 12,
    [Translatable("Anti-personnel Rifleman")]
    [Translatable(LanguageAliasSet.RUSSIAN, "Противопехотный")]
    [Translatable(LanguageAliasSet.SPANISH, "Fusilero Anti-Personal")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Puscas Anti-Personal")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Antipessoal")]
    [Translatable(LanguageAliasSet.POLISH, "Strzelec Przeciw-Piechotny")]
    APRifleman = 13,
    [Translatable(LanguageAliasSet.RUSSIAN, "Инженер")]
    [Translatable(LanguageAliasSet.SPANISH, "Ingeniero")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Inginer")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Engenheiro")]
    [Translatable(LanguageAliasSet.POLISH, "Inżynier")]
    CombatEngineer = 14,
    [Translatable(LanguageAliasSet.RUSSIAN, "Механик-водитель")]
    [Translatable(LanguageAliasSet.SPANISH, "Tripulante")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Echipaj")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Tripulante")]
    [Translatable(LanguageAliasSet.POLISH, "Załogant")]
    Crewman = 15,
    [Translatable(LanguageAliasSet.RUSSIAN, "Пилот")]
    [Translatable(LanguageAliasSet.SPANISH, "Piloto")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Pilot")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Piloto")]
    [Translatable(LanguageAliasSet.POLISH, "Pilot")]
    Pilot = 16,
    [Translatable("Special Ops")]
    [Translatable(LanguageAliasSet.SPANISH, "Op. Esp.")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Trupe Speciale")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Op. Esp.")]
    [Translatable(LanguageAliasSet.POLISH, "Specjalista")]
    SpecOps = 17,


    // raise ClassConverter.MAX_CLASS if adding another class!

    // Fallback values for Parsing
    [Obsolete]
    AUTOMATIC_RIFLEMAN = AutomaticRifleman,
    [Obsolete]
    MACHINE_GUNNER = MachineGunner,
    [Obsolete]
    AP_RIFLEMAN = APRifleman,
    [Obsolete]
    COMBAT_ENGINEER = CombatEngineer,
    [Obsolete]
    SPEC_OPS = SpecOps
}

public sealed class ClassConverter : JsonConverter<Class>
{
    private const Class MAX_CLASS = Class.SpecOps;
    public override Class Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetByte(out byte b))
                return (Class)b;
            throw new JsonException("Invalid EClass value.");
        }
        else if (reader.TokenType == JsonTokenType.Null)
            return Class.None;
        else if (reader.TokenType == JsonTokenType.String)
        {
            if (Enum.TryParse(reader.GetString()!, true, out Class rtn))
                return rtn;
            throw new JsonException("Invalid EClass value.");
        }
        throw new JsonException("Invalid token for EClass parameter.");
    }
    public override void Write(Utf8JsonWriter writer, Class value, JsonSerializerOptions options)
    {
        if (value >= Class.None && value <= MAX_CLASS)
            writer.WriteStringValue(value.ToString());
        else
            writer.WriteNumberValue((byte)value);
    }
}