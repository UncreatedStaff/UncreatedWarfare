using SDG.Unturned;
using System;
using System.Linq;
using System.Text.Json;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Quests.Types;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Quests;

// Attribute links the kit to the Type
[QuestData(EQuestType.KILL_ENEMIES)]
public class KillEnemiesQuest : BaseQuestData<KillEnemiesQuest.Tracker, KillEnemiesQuest.State, KillEnemiesQuest>
{
    // dynamic int allows for constants, ranges, or sets
    public DynamicIntegerValue KillCount;
    // > 0 will run the Tick function in trackers
    public override int TickFrequencySeconds => 0;
    // just copy paste this couldn't do it with generics.
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    // used to read from JSON, add a case for each property.
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
    }
    // States keep track of a set variation, used to keep quests synced between players and over restarts.
    public struct State : IQuestState<Tracker, KillEnemiesQuest>
    {
        // in this case we store the resulting value of kill threshold in Init(..)
        public int KillThreshold;
        public void Init(KillEnemiesQuest data)
        {
            this.KillThreshold = data.KillCount.GetValue(); // get value picks a random value if its a range or set, otherwise returns the constant.
                                                            // to get the set or constant as an array, use GetSetValue (only on dynString, dynEnum, and dynAsset)
        }
        // same as above, reading from json, except we're reading the state this time.
        public void ReadQuestState(ref Utf8JsonReader reader)
        {
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString();
                if (reader.Read() && prop.Equals("kills", StringComparison.Ordinal))
                    KillThreshold = reader.GetInt32();
            }
        }
        // writing state, not sure if this will be used or not.
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
        }
    }
    // one tracker is created per player working on the quest. Add the notify interfaces defined in QuestsMisc.cs and add cases for them in QuestManager under the events region
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private int _kills;
        // loads a tracker from a state instead of randomly picking values each time.
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold;
        }
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam())
            {
                _kills++;
                if (_kills >= KillThreshold)
                    TellCompleted();
            }
        }
        public override void ResetToDefaults() => _kills = 0;
        // translate the description of the quest, pass any data that will show up in the description once we make them
        //                                         in this case, "Kill {_kills}/{KillThreshold} enemies."
        public override string Translate() => QuestData.Translate(_player, _kills, KillThreshold);
    }
}


[QuestData(EQuestType.KILL_ENEMIES_WITH_WEAPON)]
public class KillEnemiesQuestWeapon : BaseQuestData<KillEnemiesQuestWeapon.Tracker, KillEnemiesQuestWeapon.State, KillEnemiesQuestWeapon>
{
    public DynamicIntegerValue KillCount;
    public DynamicStringValue Weapons;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
        else if (propertyname.Equals("weapon_guids", StringComparison.Ordinal))
        {
            if (!reader.TryReadStringValue(out Weapons))
                Weapons = new DynamicStringValue(null);
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestWeapon>
    {
        public int KillThreshold;
        public DynamicStringValue Weapons;
        public void Init(KillEnemiesQuestWeapon data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Weapons = data.Weapons;
        }
        public void ReadQuestState(ref Utf8JsonReader reader)
        {
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString();
                if (reader.Read())
                {
                    if (prop.Equals("kills", StringComparison.Ordinal))
                        KillThreshold = reader.GetInt32();
                    else if (prop.Equals("weapon_guids", StringComparison.Ordinal))
                    {
                        if (!reader.TryReadStringValue(out Weapons))
                            Weapons = new DynamicStringValue(null);
                    }
                }
            }
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("weapon_guids", Weapons.ToString());
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private readonly Guid[] weapons;
        private int _kills;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold;
            string[] weapons = questState.Weapons.GetSetValue();
            this.weapons = new Guid[weapons.Length];
            for (int i = 0; i < weapons.Length; i++)
            {
                if (!Guid.TryParse(weapons[i].ToString(), out this.weapons[i]))
                    L.LogWarning("Failed to parse " + weapons[i] + " as a GUID in KILL_ENEMIES_WITH_WEAPON quest.");
            }
        }
        public override void ResetToDefaults() => _kills = 0;
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam() && weapons.Contains(kill.item))
            {
                _kills++;
                if (_kills >= KillThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        public override string Translate() => QuestData.Translate(_player, _kills, KillThreshold, string.Join(", ", weapons.Select(x => Assets.find(x) is ItemAsset asset ? asset.itemName : x.ToString())));
    }
}

[QuestData(EQuestType.KILL_ENEMIES_WITH_KIT)]
public class KillEnemiesQuestKit : BaseQuestData<KillEnemiesQuestKit.Tracker, KillEnemiesQuestKit.State, KillEnemiesQuestKit>
{
    public DynamicIntegerValue KillCount;
    public DynamicStringValue Kits;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
        else if (propertyname.Equals("kit", StringComparison.Ordinal))
        {
            if (!reader.TryReadStringValue(out Kits))
                Kits = new DynamicStringValue(null);
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestKit>
    {
        public int KillThreshold;
        public string Kit;
        public void Init(KillEnemiesQuestKit data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Kit = data.Kits.GetValue();
        }
        public void ReadQuestState(ref Utf8JsonReader reader)
        {
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString();
                if (reader.Read())
                {
                    if (prop.Equals("kills", StringComparison.Ordinal))
                        KillThreshold = reader.GetInt32();
                    else if (prop.Equals("kit", StringComparison.Ordinal))
                        Kit = reader.GetString();
                }
            }
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("kit", Kit ?? string.Empty);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private readonly string Kit;
        private int _kills;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold;
            Kit = questState.Kit;
        }
        public override void ResetToDefaults() => _kills = 0;
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam() &&
                KitManager.HasKit(kill.killer, out Kit kit) && kit.Name.Equals(Kit, StringComparison.OrdinalIgnoreCase))
            {
                _kills++;
                if (_kills >= KillThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        public override string Translate()
        {
            string lang = Data.Languages.TryGetValue(_player.Steam64, out string language) ? language : JSONMethods.DEFAULT_LANGUAGE;
            string signText = KitManager.KitExists(Kit, out Kit kit) ? (kit.SignTexts.TryGetValue(lang, out string st) ? st :
                (!lang.Equals(JSONMethods.DEFAULT_LANGUAGE, StringComparison.OrdinalIgnoreCase) && kit.SignTexts.TryGetValue(JSONMethods.DEFAULT_LANGUAGE, out st) ? st : kit.Name)) : Kit;
            return QuestData.Translate(_player, _kills, KillThreshold, signText);
        }
    }
}
[QuestData(EQuestType.KILL_ENEMIES_WITH_KITS)]
public class KillEnemiesQuestKits : BaseQuestData<KillEnemiesQuestKits.Tracker, KillEnemiesQuestKits.State, KillEnemiesQuestKits>
{
    public DynamicIntegerValue KillCount;
    public DynamicStringValue Kits;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
        else if (propertyname.Equals("kits", StringComparison.Ordinal))
        {
            if (!reader.TryReadStringValue(out Kits))
                Kits = new DynamicStringValue(null);
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestKits>
    {
        public int KillThreshold;
        public DynamicStringValue Kits;
        public void Init(KillEnemiesQuestKits data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Kits = data.Kits;
        }
        public void ReadQuestState(ref Utf8JsonReader reader)
        {
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString();
                if (reader.Read())
                {
                    if (prop.Equals("kills", StringComparison.Ordinal))
                        KillThreshold = reader.GetInt32();
                    else if (prop.Equals("kits", StringComparison.Ordinal))
                        if (!reader.TryReadStringValue(out Kits))
                            Kits = new DynamicStringValue(null);
                }
            }
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("kits", Kits.ToString());
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private readonly string[] Kits;
        private int _kills;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold;
            Kits = questState.Kits.GetSetValue();
        }
        public override void ResetToDefaults() => _kills = 0;
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam() &&
                KitManager.HasKit(kill.killer, out Kit kit))
            {
                for (int i = 0; i < Kits.Length; i++)
                {
                    if (kit.Name.Equals(Kits[i], StringComparison.OrdinalIgnoreCase))
                    {
                        _kills++;
                        if (_kills >= KillThreshold)
                            TellCompleted();
                        else
                            TellUpdated();
                        return;
                    }
                }
            }
        }
        public override string Translate()
        {
            string kits = string.Join(", ", Kits.Select(Kit =>
            {
                string lang = Data.Languages.TryGetValue(_player.Steam64, out string language) ? language : JSONMethods.DEFAULT_LANGUAGE;
                return KitManager.KitExists(Kit, out Kit kit) ? (kit.SignTexts.TryGetValue(lang, out string st) ? st :
                    (!lang.Equals(JSONMethods.DEFAULT_LANGUAGE, StringComparison.OrdinalIgnoreCase) && kit.SignTexts.TryGetValue(JSONMethods.DEFAULT_LANGUAGE, out st) ? st : kit.Name)) : Kit;
            }));
            return QuestData.Translate(_player, _kills, KillThreshold, kits);
        }
    }
}
[QuestData(EQuestType.KILL_ENEMIES_WITH_KIT_CLASS)]
public class KillEnemiesQuestKitClass : BaseQuestData<KillEnemiesQuestKitClass.Tracker, KillEnemiesQuestKitClass.State, KillEnemiesQuestKitClass>
{
    public DynamicIntegerValue KillCount;
    public DynamicEnumValue<EClass> Class;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
        else if (propertyname.Equals("class", StringComparison.Ordinal))
        {
            if (!reader.TryReadEnumValue(out Class))
            {
                Class = new DynamicEnumValue<EClass>(EClass.NONE);
                L.LogWarning("Invalid class in quest " + QuestType);
            }
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestKitClass>
    {
        public int KillThreshold;
        public EClass Class;
        public void Init(KillEnemiesQuestKitClass data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Class = data.Class.GetValue();
        }
        public void ReadQuestState(ref Utf8JsonReader reader)
        {
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString();
                if (reader.Read())
                {
                    if (prop.Equals("kills", StringComparison.Ordinal))
                        KillThreshold = reader.GetInt32();
                    else if (prop.Equals("class", StringComparison.Ordinal))
                        reader.TryReadEnumValue(out Class);
                }
            }
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("class", Class.ToString());
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private readonly EClass Class;
        private int _kills;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold;
            Class = questState.Class;
        }
        public override void ResetToDefaults() => _kills = 0;
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam() &&
                KitManager.HasKit(kill.killer, out Kit kit) && kit.Class == Class)
            {
                _kills++;
                if (_kills >= KillThreshold)
                    TellCompleted();
                else
                    TellUpdated();
                return;
            }
        }
        public override string Translate() => QuestData.Translate(_player, _kills, KillThreshold, Class.ToString());
    }
}
[QuestData(EQuestType.KILL_ENEMIES_WITH_KIT_CLASSES)]
public class KillEnemiesQuestKitClasses : BaseQuestData<KillEnemiesQuestKitClasses.Tracker, KillEnemiesQuestKitClasses.State, KillEnemiesQuestKitClasses>
{
    public DynamicIntegerValue KillCount;
    public DynamicEnumValue<EClass> Class;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
        else if (propertyname.Equals("class", StringComparison.Ordinal))
        {
            if (!reader.TryReadEnumValue(out Class))
            {
                Class = new DynamicEnumValue<EClass>(EClass.NONE);
                L.LogWarning("Invalid class in quest " + QuestType);
            }
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestKitClasses>
    {
        public int KillThreshold;
        public DynamicEnumValue<EClass> Class;
        public void Init(KillEnemiesQuestKitClasses data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Class = data.Class;
        }
        public void ReadQuestState(ref Utf8JsonReader reader)
        {
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString();
                if (reader.Read())
                {
                    if (prop.Equals("kills", StringComparison.Ordinal))
                        KillThreshold = reader.GetInt32();
                    else if (prop.Equals("class", StringComparison.Ordinal))
                        if (!reader.TryReadEnumValue(out Class))
                        {
                            Class = new DynamicEnumValue<EClass>(EClass.NONE);
                            L.LogWarning("Invalid class in quest " + EQuestType.KILL_ENEMIES_WITH_KIT_CLASSES);
                        }
                }
            }
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("class", Class.ToString());
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private readonly EClass[] Classes;
        private int _kills;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold;
            Classes = questState.Class.GetSetValue();
        }

        public override void ResetToDefaults() => _kills = 0;
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam() &&
                KitManager.HasKit(kill.killer, out Kit kit) && Classes.Contains(kit.Class))
            {
                _kills++;
                if (_kills >= KillThreshold)
                    TellCompleted();
                else
                    TellUpdated();
                return;
            }
        }
        public override string Translate() => QuestData.Translate(_player, _kills, KillThreshold, string.Join(", ", Classes.Select(x => x.ToString())));
    }
}
[QuestData(EQuestType.KILL_ENEMIES_WITH_WEAPON_CLASS)]
public class KillEnemiesQuestWeaponClass : BaseQuestData<KillEnemiesQuestWeaponClass.Tracker, KillEnemiesQuestWeaponClass.State, KillEnemiesQuestWeaponClass>
{
    public DynamicIntegerValue KillCount;
    public DynamicEnumValue<EWeaponClass> Class;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
        else if (propertyname.Equals("class", StringComparison.Ordinal))
        {
            if (!reader.TryReadEnumValue(out Class))
            {
                Class = new DynamicEnumValue<EWeaponClass>(EWeaponClass.UNKNOWN);
                L.LogWarning("Invalid weapon class in quest " + QuestType);
            }
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestWeaponClass>
    {
        public int KillThreshold;
        public EWeaponClass Class;
        public void Init(KillEnemiesQuestWeaponClass data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Class = data.Class.GetValue();
        }
        public void ReadQuestState(ref Utf8JsonReader reader)
        {
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString();
                if (reader.Read())
                {
                    if (prop.Equals("kills", StringComparison.Ordinal))
                        KillThreshold = reader.GetInt32();
                    else if (prop.Equals("class", StringComparison.Ordinal))
                        reader.TryReadEnumValue(out Class);
                }
            }
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("class", Class.ToString());
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private readonly EWeaponClass Class;
        private int _kills;
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold;
            Class = questState.Class;
        }
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam() && kill.item.GetWeaponClass() == Class)
            {
                _kills++;
                if (_kills >= KillThreshold)
                    TellCompleted();
                else
                    TellUpdated();
                return;
            }
        }
        public override string Translate() => QuestData.Translate(_player, _kills, KillThreshold, Class.ToString());
    }
}

[QuestData(EQuestType.KILL_ENEMIES_WITH_BRANCH)]
public class KillEnemiesQuestBranch : BaseQuestData<KillEnemiesQuestBranch.Tracker, KillEnemiesQuestBranch.State, KillEnemiesQuestBranch>
{
    public DynamicIntegerValue KillCount;
    public DynamicEnumValue<EBranch> Branch;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
        else if (propertyname.Equals("branch", StringComparison.Ordinal))
        {
            if (!reader.TryReadEnumValue(out Branch))
            {
                Branch = new DynamicEnumValue<EBranch>(EBranch.DEFAULT);
                L.LogWarning("Invalid branch in quest " + QuestType);
            }
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestBranch>
    {
        public int KillThreshold;
        public EBranch Branch;
        public void Init(KillEnemiesQuestBranch data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Branch = data.Branch.GetValue();
        }
        public void ReadQuestState(ref Utf8JsonReader reader)
        {
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString();
                if (reader.Read())
                {
                    if (prop.Equals("kills", StringComparison.Ordinal))
                        KillThreshold = reader.GetInt32();
                    else if (prop.Equals("branch", StringComparison.Ordinal))
                        reader.TryReadEnumValue(out Branch);
                }
            }
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("branch", Branch.ToString());
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private readonly EBranch Branch;
        private int _kills;
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold;
            Branch = questState.Branch;
        }
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && _player.Branch == Branch && kill.dead.GetTeam() != kill.killer.GetTeam())
            {
                _kills++;
                if (_kills >= KillThreshold)
                    TellCompleted();
                else
                    TellUpdated();
                return;
            }
        }
        public override string Translate() => QuestData.Translate(_player, _kills, KillThreshold, Branch.ToString());
    }
}
[QuestData(EQuestType.KILL_ENEMIES_WITH_TURRET)]
public class KillEnemiesQuestTurret : BaseQuestData<KillEnemiesQuestTurret.Tracker, KillEnemiesQuestTurret.State, KillEnemiesQuestTurret>
{
    public DynamicIntegerValue KillCount;
    public DynamicAssetValue<ItemGunAsset> Turrets;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
        else if (propertyname.Equals("turret", StringComparison.Ordinal))
        {
            if (!reader.TryReadAssetValue(out Turrets))
            {
                L.LogWarning("Invalid turret GUID(s) in quest " + QuestType);
            }
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestTurret>
    {
        public int KillThreshold;
        public Guid Weapon;
        public void Init(KillEnemiesQuestTurret data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Weapon = data.Turrets.GetValue();
        }
        public void ReadQuestState(ref Utf8JsonReader reader)
        {
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString();
                if (reader.Read())
                {
                    if (prop.Equals("kills", StringComparison.Ordinal))
                        KillThreshold = reader.GetInt32();
                    else if (prop.Equals("turret", StringComparison.Ordinal))
                        reader.TryGetGuid(out Weapon);
                }
            }
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("turret", Weapon.ToString());
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private readonly ItemGunAsset Weapon;
        private int _kills;
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold;
            Weapon = Assets.find(questState.Weapon) as ItemGunAsset;
        }
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam())
            {
                InteractableVehicle veh = kill.killer.movement.getVehicle();
                if (veh == null) return;
                for (int i = 0; i < veh.turrets.Length; i++)
                {
                    if (veh.turrets[i] != null && veh.turrets[i].player?.player.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && veh.turrets[i].turret?.itemID == Weapon.id)
                    {
                        _kills++;
                        if (_kills >= KillThreshold)
                            TellCompleted();
                        else
                            TellUpdated();
                        return;
                    }
                }
            }
        }
        public override string Translate() => QuestData.Translate(_player, _kills, KillThreshold, Weapon.itemName);
    }
}
[QuestData(EQuestType.KILL_ENEMIES_IN_SQUAD)]
public class KillEnemiesQuestSquad : BaseQuestData<KillEnemiesQuestSquad.Tracker, KillEnemiesQuestSquad.State, KillEnemiesQuestSquad>
{
    public DynamicIntegerValue KillCount;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestSquad>
    {
        public int KillThreshold;
        public void Init(KillEnemiesQuestSquad data)
        {
            this.KillThreshold = data.KillCount.GetValue();
        }
        public void ReadQuestState(ref Utf8JsonReader reader)
        {
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString();
                if (reader.Read())
                {
                    if (prop.Equals("kills", StringComparison.Ordinal))
                        KillThreshold = reader.GetInt32();
                }
            }
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private int _kills;
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold;
        }
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && _player.Squad != null && _player.Squad.Members.Count > 1 && kill.dead.GetTeam() != kill.killer.GetTeam())
            {
                _kills++;
                if (_kills >= KillThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        public override string Translate() => QuestData.Translate(_player, _kills, KillThreshold);
    }
}
[QuestData(EQuestType.KILL_ENEMIES_IN_FULL_SQUAD)]
public class KillEnemiesQuestFullSquad : BaseQuestData<KillEnemiesQuestFullSquad.Tracker, KillEnemiesQuestFullSquad.State, KillEnemiesQuestFullSquad>
{
    public DynamicIntegerValue KillCount;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestFullSquad>
    {
        public int KillThreshold;
        public void Init(KillEnemiesQuestFullSquad data)
        {
            this.KillThreshold = data.KillCount.GetValue();
        }
        public void ReadQuestState(ref Utf8JsonReader reader)
        {
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString();
                if (reader.Read())
                {
                    if (prop.Equals("kills", StringComparison.Ordinal))
                        KillThreshold = reader.GetInt32();
                }
            }
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private int _kills;
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold;
        }
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && _player.Squad != null && _player.Squad.IsFull() && kill.dead.GetTeam() != kill.killer.GetTeam())
            {
                _kills++;
                if (_kills >= KillThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        public override string Translate() => QuestData.Translate(_player, _kills, KillThreshold);
    }
}
[QuestData(EQuestType.KILL_ENEMIES_ON_POINT_DEFENSE)]
public class KillEnemiesQuestDefense : BaseQuestData<KillEnemiesQuestDefense.Tracker, KillEnemiesQuestDefense.State, KillEnemiesQuestDefense>
{
    public DynamicIntegerValue KillCount;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestDefense>
    {
        public int KillThreshold;
        public void Init(KillEnemiesQuestDefense data)
        {
            this.KillThreshold = data.KillCount.GetValue();
        }
        public void ReadQuestState(ref Utf8JsonReader reader)
        {
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString();
                if (reader.Read())
                {
                    if (prop.Equals("kills", StringComparison.Ordinal))
                        KillThreshold = reader.GetInt32();
                }
            }
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private int _kills;
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold;
        }
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            ulong team = kill.killer.GetTeam();
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != team)
            {
                if (Data.Is(out Gamemodes.Interfaces.IFlagTeamObjectiveGamemode fr))
                {
                    if (Data.Is<Gamemodes.Flags.TeamCTF.TeamCTF>(out _))
                    {
                        if (
                            (team == 1 && fr.ObjectiveTeam1 != null && fr.ObjectiveTeam1.Owner == 1 && fr.ObjectiveTeam1.PlayerInRange(kill.killer.transform.position)) ||
                            (team == 2 && fr.ObjectiveTeam2 != null && fr.ObjectiveTeam2.Owner == 2 && fr.ObjectiveTeam2.PlayerInRange(kill.killer.transform.position)))
                        {
                            goto add;
                        }
                    }
                    else if (Data.Is(out Gamemodes.Flags.Invasion.Invasion inv))
                    {
                        if (inv.DefendingTeam == team)
                        {
                            if (inv.AttackingTeam == 1)
                            {
                                if (inv.ObjectiveTeam1 != null && fr.ObjectiveTeam1.Owner == TeamManager.Other(team) && inv.ObjectiveTeam1.PlayerInRange(kill.killer.transform.position))
                                    goto add;
                            }
                            else if (inv.AttackingTeam == 2)
                            {
                                if (inv.ObjectiveTeam2 != null && fr.ObjectiveTeam2.Owner == TeamManager.Other(team) && inv.ObjectiveTeam2.PlayerInRange(kill.killer.transform.position))
                                    goto add;
                            }
                        }
                    }
                    else if (Data.Is(out Gamemodes.Insurgency.Insurgency ins))
                    {
                        if (ins.DefendingTeam == team)
                        {
                            for (int i = 0; i < ins.Caches.Count; i++)
                            {
                                Gamemodes.Insurgency.Insurgency.CacheData cache = ins.Caches[i];
                                if (cache != null && cache.IsActive && (cache.Cache.Position - kill.killer.transform.position).sqrMagnitude > 3600) // 60m
                                    goto add;
                            }
                        }
                    }
                }
            }
            return;
            add:
            _kills++;
            if (_kills >= KillThreshold)
                TellCompleted();
            else
                TellUpdated();
        }
        public override string Translate() => QuestData.Translate(_player, _kills, KillThreshold);
    }
}
[QuestData(EQuestType.KILL_ENEMIES_ON_POINT_ATTACK)]
public class KillEnemiesQuestAttack : BaseQuestData<KillEnemiesQuestAttack.Tracker, KillEnemiesQuestAttack.State, KillEnemiesQuestAttack>
{
    public DynamicIntegerValue KillCount;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestAttack>
    {
        public int KillThreshold;
        public void Init(KillEnemiesQuestAttack data)
        {
            this.KillThreshold = data.KillCount.GetValue();
        }
        public void ReadQuestState(ref Utf8JsonReader reader)
        {
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString();
                if (reader.Read())
                {
                    if (prop.Equals("kills", StringComparison.Ordinal))
                        KillThreshold = reader.GetInt32();
                }
            }
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private int _kills;
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold;
        }
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            ulong team = kill.killer.GetTeam();
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != team)
            {
                if (Data.Is(out Gamemodes.Interfaces.IFlagTeamObjectiveGamemode fr))
                {
                    if (Data.Is<Gamemodes.Flags.TeamCTF.TeamCTF>(out _))
                    {
                        if (
                            (team == 1 && fr.ObjectiveTeam1 != null && fr.ObjectiveTeam1.Owner == 2 && fr.ObjectiveTeam1.PlayerInRange(kill.killer.transform.position)) ||
                            (team == 2 && fr.ObjectiveTeam2 != null && fr.ObjectiveTeam2.Owner == 1 && fr.ObjectiveTeam2.PlayerInRange(kill.killer.transform.position)))
                        {
                            goto add;
                        }
                    }
                    else if (Data.Is(out Gamemodes.Flags.Invasion.Invasion inv))
                    {
                        if (inv.AttackingTeam == team)
                        {
                            if (inv.AttackingTeam == 1)
                            {
                                if (inv.ObjectiveTeam1 != null && fr.ObjectiveTeam1.Owner == TeamManager.Other(team) && inv.ObjectiveTeam1.PlayerInRange(kill.killer.transform.position))
                                    goto add;
                            }
                            else if (inv.AttackingTeam == 2)
                            {
                                if (inv.ObjectiveTeam2 != null && fr.ObjectiveTeam2.Owner == TeamManager.Other(team) && inv.ObjectiveTeam2.PlayerInRange(kill.killer.transform.position))
                                    goto add;
                            }
                        }
                    }
                    else if (Data.Is(out Gamemodes.Insurgency.Insurgency ins))
                    {
                        if (ins.AttackingTeam == team)
                        {
                            for (int i = 0; i < ins.Caches.Count; i++)
                            {
                                Gamemodes.Insurgency.Insurgency.CacheData cache = ins.Caches[i];
                                if (cache != null && cache.IsActive && (cache.Cache.Position - kill.killer.transform.position).sqrMagnitude > 3600) // 60m
                                    goto add;
                            }
                        }
                    }
                }
            }
            return;
            add:
            _kills++;
            if (_kills >= KillThreshold)
                TellCompleted();
            else
                TellUpdated();
        }
        public override string Translate() => QuestData.Translate(_player, _kills, KillThreshold);
    }
}