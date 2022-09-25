using JetBrains.Annotations;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Encoding;
using Uncreated.Framework;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Quests;

namespace Uncreated.Warfare.Kits;

public class Kit : ITranslationArgument, ICloneable
{
    public const int CAPACITY = 256;
    internal int PrimaryKey = -1;
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
    public ushort CreditCost;
    [JsonSettable]
    public ushort UnlockLevel;
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
    [JsonSettable]
    public bool Disabled;
    [JsonSettable]
    public ESquadLevel SquadLevel;
    public List<KitItem> Items;
    public List<KitClothing> Clothes;
    public Dictionary<string, string> SignTexts;
    [JsonSettable]
    public string Weapons;
    public Kit(string name)
    {
        Name = name;
        Items = new List<KitItem>();
        Clothes = new List<KitClothing>();
        Class = EClass.NONE;
        Branch = EBranch.DEFAULT;
        Team = 0;
        UnlockRequirements = new BaseUnlockRequirement[0];
        Skillsets = new Skillset[0];
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
        SquadLevel = ESquadLevel.MEMBER;
    }
    public Kit() : this("default") { }
    public Kit(string kitName, List<KitItem> items, List<KitClothing> clothing)
    {
        Name = kitName;
        Items = items ?? new List<KitItem>();
        Clothes = clothing ?? new List<KitClothing>();
        Class = EClass.NONE;
        Branch = EBranch.DEFAULT;
        Team = 0;
        UnlockRequirements = new BaseUnlockRequirement[0];
        Skillsets = new Skillset[0];
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
        SquadLevel = ESquadLevel.MEMBER;
    }
    public void ApplyTo(Kit kit)
    {
        kit.Class = Class;
        kit.Branch = Branch;
        kit.Team = Team;
        kit.Items = new List<KitItem>(Items.Select(x => (KitItem)x.Clone()));
        kit.Clothes = new List<KitClothing>(Clothes.Select(x => (KitClothing)x.Clone()));
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
        Kit clone = new Kit(false)
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
        List<KitItem> items = new List<KitItem>(itemCount);
        List<KitClothing> clothes = new List<KitClothing>(clothesCount);
        for (int i = 0; i < itemCount; i++)
        {
            items.Add(new KitItem()
            {
                Id = R.ReadGUID(),
                Amount = R.ReadUInt8(),
                Page = R.ReadUInt8(),
                X = R.ReadUInt8(),
                Y = R.ReadUInt8(),
                Rotation = R.ReadUInt8(),
                Metadata = R.ReadBytes() ?? new byte[0]
            });
        }
        for (int i = 0; i < clothesCount; i++)
        {
            clothes.Add(new KitClothing()
            {
                Id = R.ReadGUID(),
                Type = R.ReadEnum<EClothingType>()
            });
        }
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
        kit.CreditCost = R.ReadUInt16();
        kit.UnlockLevel = R.ReadUInt16();
        kit.Disabled = R.ReadBool();
        kit.SquadLevel = R.ReadEnum<ESquadLevel>();
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
        for (int i = 0; i < kit.Items.Count; i++)
        {
            KitItem item = kit.Items[i];
            W.Write(item.Id);
            W.Write(item.Amount);
            W.Write(item.Page);
            W.Write(item.X);
            W.Write(item.Y);
            W.Write(item.Rotation);
            W.Write(item.Metadata);
        }
        for (int i = 0; i < kit.Clothes.Count; i++)
        {
            KitClothing clothing = kit.Clothes[i];
            W.Write(clothing.Id);
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
    [FormatDisplay("Class (" + nameof(EClass) + ")")]
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
    public Guid QuestID = default;
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
public class KitItem : ICloneable
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("x")]
    public byte X { get; set; }

    [JsonPropertyName("y")]
    public byte Y { get; set; }

    [JsonPropertyName("rotation")]
    public byte Rotation { get; set; }

    [JsonPropertyName("page")]
    public byte Page { get; set; }

    [JsonPropertyName("amount")]
    public byte Amount { get; set; }

    [JsonPropertyName("metadata")]
    [JsonConverter(typeof(Base64Converter))]
    public byte[] Metadata { get; set; }

    [JsonConstructor]
    public KitItem(Guid id, byte x, byte y, byte rotation, byte[] metadata, byte amount, byte page)
    {
        this.Id = id;
        this.X = x;
        this.Y = y;
        this.Rotation = rotation;
        this.Metadata = metadata;
        this.Amount = amount;
        this.Page = page;
    }
    public KitItem() { }
    public object Clone() => new KitItem(Id, X, Y, Rotation, Metadata, Amount, Page);
}
public class KitClothing : ICloneable
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("type")]
    public EClothingType Type { get; set; }

    [JsonConstructor]
    public KitClothing(Guid id, EClothingType type)
    {
        this.Id = id;
        this.Type = type;
    }
    public KitClothing() { }

    public object Clone() => new KitClothing(Id, Type);
}

[Translatable("Squad Level")]
public enum ESquadLevel : byte
{
    [Translatable("Member")]
    MEMBER = 0,
    [Translatable("Commander")]
    COMMANDER = 4
}
[Translatable("Branch")]
public enum EBranch : byte
{
    DEFAULT,
    INFANTRY,
    ARMOR,
    [Translatable("Air Force")]
    AIRFORCE,
    [Translatable("Special Ops")]
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
[JsonConverter(typeof(ClassConverter))]
[Translatable("Kit Class")]
public enum EClass : byte
{
    NONE = 0, //0
    [Translatable(LanguageAliasSet.RUSSIAN, "Безоружный")]
    [Translatable(LanguageAliasSet.SPANISH, "Desarmado")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Neinarmat")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Desarmado")]
    [Translatable(LanguageAliasSet.POLISH, "Nieuzbrojony")]
    UNARMED = 1,
    [Translatable("Squad Leader")]
    [Translatable(LanguageAliasSet.RUSSIAN, "Лидер отряда")]
    [Translatable(LanguageAliasSet.SPANISH, "Líder De Escuadrón")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Lider de Echipa")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Líder de Esquadrão")]
    [Translatable(LanguageAliasSet.POLISH, "Dowódca Oddziału")]
    SQUADLEADER = 2,
    [Translatable(LanguageAliasSet.RUSSIAN, "Стрелок")]
    [Translatable(LanguageAliasSet.SPANISH, "Fusilero")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Puscas")]
    [Translatable(LanguageAliasSet.POLISH, "Strzelec")]
    RIFLEMAN = 3,
    [Translatable(LanguageAliasSet.RUSSIAN, "Медик")]
    [Translatable(LanguageAliasSet.SPANISH, "Médico")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Medic")]
    [Translatable(LanguageAliasSet.POLISH, "Medyk")]
    MEDIC = 4,
    [Translatable(LanguageAliasSet.RUSSIAN, "Нарушитель")]
    [Translatable(LanguageAliasSet.SPANISH, "Brechador")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Breacher")]
    [Translatable(LanguageAliasSet.POLISH, "Wyłamywacz")]
    BREACHER = 5,
    [Translatable(LanguageAliasSet.RUSSIAN, "Солдат с автоматом")]
    [Translatable(LanguageAliasSet.SPANISH, "Fusilero Automático")]
    [Translatable(LanguageAliasSet.SPANISH, "Puscas Automat")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Fuzileiro Automobilístico")]
    [Translatable(LanguageAliasSet.POLISH, "Strzelec Automatyczny")]
    AUTOMATIC_RIFLEMAN = 6,
    [Translatable(LanguageAliasSet.RUSSIAN, "Гренадёр")]
    [Translatable(LanguageAliasSet.SPANISH, "Granadero")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Grenadier")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Granadeiro")]
    [Translatable(LanguageAliasSet.POLISH, "Grenadier")]
    GRENADIER = 7,
    [Translatable(LanguageAliasSet.ROMANIAN, "Mitralior")]
    MACHINE_GUNNER = 8,
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
    MARKSMAN = 11,
    [Translatable(LanguageAliasSet.RUSSIAN, "Снайпер")]
    [Translatable(LanguageAliasSet.SPANISH, "Francotirador")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Lunetist")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Franco-Atirador")]
    [Translatable(LanguageAliasSet.POLISH, "Snajper")]
    SNIPER = 12,
    [Translatable("Anti-personnel Rifleman")]
    [Translatable(LanguageAliasSet.RUSSIAN, "Противопехотный")]
    [Translatable(LanguageAliasSet.SPANISH, "Fusilero Anti-Personal")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Puscas Anti-Personal")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Antipessoal")]
    [Translatable(LanguageAliasSet.POLISH, "Strzelec Przeciw-Piechotny")]
    AP_RIFLEMAN = 13,
    [Translatable(LanguageAliasSet.RUSSIAN, "Инженер")]
    [Translatable(LanguageAliasSet.SPANISH, "Ingeniero")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Inginer")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Engenheiro")]
    [Translatable(LanguageAliasSet.POLISH, "Inżynier")]
    COMBAT_ENGINEER = 14,
    [Translatable(LanguageAliasSet.RUSSIAN, "Механик-водитель")]
    [Translatable(LanguageAliasSet.SPANISH, "Tripulante")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Echipaj")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Tripulante")]
    [Translatable(LanguageAliasSet.POLISH, "Załogant")]
    CREWMAN = 15,
    [Translatable(LanguageAliasSet.RUSSIAN, "Пилот")]
    [Translatable(LanguageAliasSet.SPANISH, "Piloto")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Pilot")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Piloto")]
    [Translatable(LanguageAliasSet.POLISH, "Pilot")]
    PILOT = 16,
    [Translatable("Special Ops")]
    [Translatable(LanguageAliasSet.SPANISH, "Op. Esp.")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Trupe Speciale")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Op. Esp.")]
    [Translatable(LanguageAliasSet.POLISH, "Specjalista")]
    SPEC_OPS = 17
}

public sealed class ClassConverter : JsonConverter<EClass>
{
    private const EClass MAX_CLASS = EClass.SPEC_OPS;
    public override EClass Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetByte(out byte b))
                return (EClass)b;
            throw new JsonException("Invalid EClass value.");
        }
        else if (reader.TokenType == JsonTokenType.Null)
            return EClass.NONE;
        else if (reader.TokenType == JsonTokenType.String)
        {
            if (Enum.TryParse(reader.GetString()!, true, out EClass rtn))
                return rtn;
            throw new JsonException("Invalid EClass value.");
        }
        throw new JsonException("Invalid token for EClass parameter.");
    }
    public override void Write(Utf8JsonWriter writer, EClass value, JsonSerializerOptions options)
    {
        if (value >= EClass.NONE && value <= MAX_CLASS)
            writer.WriteStringValue(value.ToString());
        else
            writer.WriteNumberValue((byte)value);
    }
}