using SDG.Unturned;
using System;
using System.Linq;
using System.Text.Json;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Quests.Types;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Quests.Types;

// Attribute links the kit to the Type
[QuestData(EQuestType.KILL_ENEMIES)]
public class KillEnemiesQuest : BaseQuestData<KillEnemiesQuest.Tracker, KillEnemiesQuest.State, KillEnemiesQuest>
{
    // dynamic int allows for constants, ranges, or sets
    public DynamicIntegerValue KillCount;
    // > 0 will run the Tick function in trackers
    public override int TickFrequencySeconds => 0;
    // just copy paste this couldn't do it with generics.
    protected override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
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
        public IDynamicValue<int>.IChoice KillThreshold;
        public void Init(KillEnemiesQuest data)
        {
            this.KillThreshold = data.KillCount.GetValue(); // get value picks a random value if its a range or set, otherwise returns the constant.
        }
        public bool IsEligable(UCPlayer player) => true;

        // same as above, reading from json, except we're reading the state this time.
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
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
        private readonly int KillThreshold;
        private int _kills;
        protected override bool CompletedCheck => _kills >= KillThreshold;
        public override short FlagValue => (short)_kills;
        // loads a tracker from a state instead of randomly picking values each time.
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold.InsistValue(); // insisting for a value asks for ONE value (defined with a $, otherwise it returns 0)
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("kills", StringComparison.Ordinal))
                _kills = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", _kills);
        }
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam())
            {
                _kills++;
                if (_kills >= KillThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        public override void ResetToDefaults() => _kills = 0;
        // translate the description of the quest, pass any data that will show up in the description once we make them
        //                                         in this case, "Kill {_kills}/{KillThreshold} enemies."
        protected override string Translate() => QuestData!.Translate(_player, _kills, KillThreshold);
    }
}

[QuestData(EQuestType.KILL_FROM_RANGE)]
public class KillEnemiesRangeQuest : BaseQuestData<KillEnemiesRangeQuest.Tracker, KillEnemiesRangeQuest.State, KillEnemiesRangeQuest>
{
    public DynamicIntegerValue KillCount;
    public DynamicFloatValue Range;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
        else if (propertyname.Equals("range", StringComparison.Ordinal))
        {
            if (!reader.TryReadFloatValue(out Range))
                Range = new DynamicFloatValue(new FloatRange(50, 200));
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesRangeQuest>
    {
        public IDynamicValue<int>.IChoice KillThreshold;
        public IDynamicValue<float>.IChoice Range;
        public bool IsEligable(UCPlayer player) => true;
        public void Init(KillEnemiesRangeQuest data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Range = data.Range.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("range", StringComparison.Ordinal))
                Range = DynamicFloatValue.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("range", Range);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private readonly float Range;
        private int _kills;
        protected override bool CompletedCheck => _kills >= KillThreshold;
        public override short FlagValue => (short)_kills;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold.InsistValue();
            Range = questState.Range.InsistValue();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("kills", StringComparison.Ordinal))
                _kills = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", _kills);
        }
        public override void ResetToDefaults() => _kills = 0;
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam() && kill.distance >= Range && kill.item != default)
            {
                _kills++;
                if (_kills >= KillThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate() => QuestData!.Translate(_player, _kills, KillThreshold);
    }
}
[QuestData(EQuestType.KILL_ENEMIES_WITH_WEAPON)]
public class KillEnemiesQuestWeapon : BaseQuestData<KillEnemiesQuestWeapon.Tracker, KillEnemiesQuestWeapon.State, KillEnemiesQuestWeapon>
{
    public DynamicIntegerValue KillCount;
    public DynamicAssetValue<ItemWeaponAsset> Weapon = new DynamicAssetValue<ItemWeaponAsset>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ALL);
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
        else if (propertyname.Equals("weapons", StringComparison.Ordinal))
        {
            if (!reader.TryReadAssetValue(out Weapon))
                Weapon = new DynamicAssetValue<ItemWeaponAsset>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ALL);
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestWeapon>
    {
        public IDynamicValue<int>.IChoice KillThreshold;
        public DynamicAssetValue<ItemWeaponAsset>.Choice Weapon;
        public bool IsEligable(UCPlayer player) => true;
        public void Init(KillEnemiesQuestWeapon data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Weapon = data.Weapon.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("weapons", StringComparison.Ordinal))
                Weapon = DynamicAssetValue<ItemWeaponAsset>.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("weapons", Weapon);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private readonly DynamicAssetValue<ItemWeaponAsset>.Choice Weapon;
        private string translationCache1;
        private int _kills;
        protected override bool CompletedCheck => _kills >= KillThreshold;
        public override short FlagValue => (short)_kills;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold.InsistValue();
            Weapon = questState.Weapon;
            translationCache1 = Weapon.GetCommaList();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("kills", StringComparison.Ordinal))
                _kills = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", _kills);
        }
        public override void ResetToDefaults() => _kills = 0;
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam() && kill.item != default)
            {
                if (Weapon.IsMatch(kill.item))
                {
                    _kills++;
                    if (_kills >= KillThreshold)
                        TellCompleted();
                    else
                        TellUpdated();
                }
            }
        }
        protected override string Translate() => QuestData!.Translate(_player, _kills, KillThreshold, translationCache1);
    }
}
[QuestData(EQuestType.KILL_FROM_RANGE_WITH_WEAPON)]
public class KillEnemiesRangeQuestWeapon : BaseQuestData<KillEnemiesRangeQuestWeapon.Tracker, KillEnemiesRangeQuestWeapon.State, KillEnemiesRangeQuestWeapon>
{
    public DynamicIntegerValue KillCount;
    public DynamicAssetValue<ItemWeaponAsset> Weapon = new DynamicAssetValue<ItemWeaponAsset>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ALL);
    public DynamicFloatValue Range;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
        else if (propertyname.Equals("weapons", StringComparison.Ordinal))
        {
            if (!reader.TryReadAssetValue(out Weapon))
                Weapon = new DynamicAssetValue<ItemWeaponAsset>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ALL);
        }
        else if (propertyname.Equals("range", StringComparison.Ordinal))
        {
            if (!reader.TryReadFloatValue(out Range))
                Range = new DynamicFloatValue(new FloatRange(50, 200));
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesRangeQuestWeapon>
    {
        public IDynamicValue<int>.IChoice KillThreshold;
        public DynamicAssetValue<ItemWeaponAsset>.Choice Weapon;
        public IDynamicValue<float>.IChoice Range;
        public bool IsEligable(UCPlayer player) => true;
        public void Init(KillEnemiesRangeQuestWeapon data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Weapon = data.Weapon.GetValue();
            this.Range = data.Range.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("weapons", StringComparison.Ordinal))
                Weapon = DynamicAssetValue<ItemWeaponAsset>.ReadChoice(ref reader);
            else if (prop.Equals("range", StringComparison.Ordinal))
                Range = DynamicFloatValue.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("weapons", Weapon);
            writer.WriteProperty("range", Range);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private readonly DynamicAssetValue<ItemWeaponAsset>.Choice Weapon;
        private readonly float Range;
        private string translationCache1;
        private int _kills;
        protected override bool CompletedCheck => _kills >= KillThreshold;
        public override short FlagValue => (short)_kills;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold.InsistValue();
            Weapon = questState.Weapon;
            translationCache1 = Weapon.GetCommaList();
            Range = questState.Range.InsistValue();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("kills", StringComparison.Ordinal))
                _kills = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", _kills);
        }
        public override void ResetToDefaults() => _kills = 0;
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam() && kill.distance >= Range && kill.item != default)
            {
                if (Weapon.IsMatch(kill.item))
                {
                    _kills++;
                    if (_kills >= KillThreshold)
                        TellCompleted();
                    else
                        TellUpdated();
                }
            }
        }
        protected override string Translate() => QuestData!.Translate(_player, _kills, KillThreshold, translationCache1);
    }
}
[QuestData(EQuestType.KILL_ENEMIES_WITH_KIT)]
public class KillEnemiesQuestKit : BaseQuestData<KillEnemiesQuestKit.Tracker, KillEnemiesQuestKit.State, KillEnemiesQuestKit>
{
    public DynamicIntegerValue KillCount;
    public DynamicStringValue Kits;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
        else if (propertyname.Equals("kit", StringComparison.Ordinal))
        {
            if (!reader.TryReadStringValue(out Kits, true))
                Kits = new DynamicStringValue(true, EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ONE);
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestKit>
    {
        public IDynamicValue<int>.IChoice KillThreshold;
        public IDynamicValue<string>.IChoice Kit;
        public bool IsEligable(UCPlayer player) => true;
        public void Init(KillEnemiesQuestKit data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Kit = data.Kits.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("kit", StringComparison.Ordinal))
                Kit = DynamicStringValue.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("kit", Kit);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private readonly IDynamicValue<string>.IChoice Kit;
        private int _kills;
        protected override bool CompletedCheck => _kills >= KillThreshold;
        public override short FlagValue => (short)_kills;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold.InsistValue();
            Kit = questState.Kit;
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("kills", StringComparison.Ordinal))
                _kills = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", _kills);
        }
        public override void ResetToDefaults() => _kills = 0;
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam() &&
                KitManager.HasKit(kill.killer, out Kit kit) && Kit.IsMatch(kit.Name))
            {
                _kills++;
                if (_kills >= KillThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate()
        {
            return QuestData!.Translate(_player, _kills, KillThreshold, Kit.ToString());
        }
    }
}
[QuestData(EQuestType.KILL_FROM_RANGE_WITH_KIT)]
public class KillEnemiesQuestKitRange : BaseQuestData<KillEnemiesQuestKitRange.Tracker, KillEnemiesQuestKitRange.State, KillEnemiesQuestKitRange>
{
    public DynamicIntegerValue KillCount;
    public DynamicStringValue Kits;
    public DynamicFloatValue Range;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
        else if (propertyname.Equals("kit", StringComparison.Ordinal))
        {
            if (!reader.TryReadStringValue(out Kits, true))
                Kits = new DynamicStringValue(true, EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ONE);
        }
        else if (propertyname.Equals("range", StringComparison.Ordinal))
        {
            if (!reader.TryReadFloatValue(out Range))
                Range = new DynamicFloatValue(200f);
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestKitRange>
    {
        public IDynamicValue<int>.IChoice KillThreshold;
        public IDynamicValue<string>.IChoice Kit;
        public IDynamicValue<float>.IChoice Range;
        public bool IsEligable(UCPlayer player) => true;
        public void Init(KillEnemiesQuestKitRange data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Kit = data.Kits.GetValue();
            this.Range = data.Range.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("kit", StringComparison.Ordinal))
                Kit = DynamicStringValue.ReadChoice(ref reader);
            else if (prop.Equals("range", StringComparison.Ordinal))
                Range = DynamicFloatValue.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("kit", Kit);
            writer.WriteProperty("range", Range);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private readonly IDynamicValue<string>.IChoice Kit;
        private readonly float Range;
        private int _kills;
        protected override bool CompletedCheck => _kills >= KillThreshold;
        public override short FlagValue => (short)_kills;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold.InsistValue();
            Kit = questState.Kit;
            Range = questState.Range.InsistValue();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("kills", StringComparison.Ordinal))
                _kills = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", _kills);
        }
        public override void ResetToDefaults() => _kills = 0;
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam() && kill.distance <= Range &&
                KitManager.HasKit(kill.killer, out Kit kit) && Kit.IsMatch(kit.Name))
            {
                _kills++;
                if (_kills >= KillThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate()
        {
            return QuestData!.Translate(_player, _kills, KillThreshold, Kit.ToString());
        }
    }
}
[QuestData(EQuestType.KILL_ENEMIES_WITH_KIT_CLASS)]
public class KillEnemiesQuestKitClass : BaseQuestData<KillEnemiesQuestKitClass.Tracker, KillEnemiesQuestKitClass.State, KillEnemiesQuestKitClass>
{
    public DynamicIntegerValue KillCount;
    public DynamicEnumValue<EClass> Class;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
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
        public IDynamicValue<int>.IChoice KillThreshold;
        public IDynamicValue<EClass>.IChoice Class;
        public bool IsEligable(UCPlayer player) => true;
        public void Init(KillEnemiesQuestKitClass data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Class = data.Class.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("class", StringComparison.Ordinal))
                Class = DynamicEnumValue<EClass>.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("class", Class);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private readonly IDynamicValue<EClass>.IChoice Class;
        private int _kills;
        protected override bool CompletedCheck => _kills >= KillThreshold;
        public override short FlagValue => (short)_kills;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold.InsistValue();
            Class = questState.Class;
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("kills", StringComparison.Ordinal))
                _kills = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", _kills);
        }
        public override void ResetToDefaults() => _kills = 0;
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam() &&
                KitManager.HasKit(kill.killer, out Kit kit) && Class.IsMatch(kit.Class))
            {
                _kills++;
                if (_kills >= KillThreshold)
                    TellCompleted();
                else
                    TellUpdated();
                return;
            }
        }
        protected override string Translate() => QuestData!.Translate(_player, _kills, KillThreshold, Class.ToString());
    }
}
[QuestData(EQuestType.KILL_FROM_RANGE_WITH_CLASS)]
public class KillEnemiesQuestKitClassRange : BaseQuestData<KillEnemiesQuestKitClassRange.Tracker, KillEnemiesQuestKitClassRange.State, KillEnemiesQuestKitClassRange>
{
    public DynamicIntegerValue KillCount;
    public DynamicEnumValue<EClass> Class;
    public DynamicFloatValue Range;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
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
        else if (propertyname.Equals("range", StringComparison.Ordinal))
        {
            if (!reader.TryReadFloatValue(out Range))
                Range = new DynamicFloatValue(200f);
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestKitClassRange>
    {
        public IDynamicValue<int>.IChoice KillThreshold;
        public IDynamicValue<EClass>.IChoice Class;
        public IDynamicValue<float>.IChoice Range;
        public bool IsEligable(UCPlayer player) => true;
        public void Init(KillEnemiesQuestKitClassRange data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Class = data.Class.GetValue();
            this.Range = data.Range.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("class", StringComparison.Ordinal))
                Class = DynamicEnumValue<EClass>.ReadChoice(ref reader);
            else if (prop.Equals("range", StringComparison.Ordinal))
                Range = DynamicFloatValue.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("class", Class);
            writer.WriteProperty("range", Range);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private readonly IDynamicValue<EClass>.IChoice Class;
        private readonly float Range;
        private int _kills;
        protected override bool CompletedCheck => _kills >= KillThreshold;
        public override short FlagValue => (short)_kills;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold.InsistValue();
            Class = questState.Class;
            Range = questState.Range.InsistValue();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("kills", StringComparison.Ordinal))
                _kills = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", _kills);
        }
        public override void ResetToDefaults() => _kills = 0;
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam() && kill.distance >= Range &&
                KitManager.HasKit(kill.killer, out Kit kit) && Class.IsMatch(kit.Class))
            {
                _kills++;
                if (_kills >= KillThreshold)
                    TellCompleted();
                else
                    TellUpdated();
                return;
            }
        }
        protected override string Translate() => QuestData!.Translate(_player, _kills, KillThreshold, Class.ToString());
    }
}
[QuestData(EQuestType.KILL_ENEMIES_WITH_WEAPON_CLASS)]
public class KillEnemiesQuestWeaponClass : BaseQuestData<KillEnemiesQuestWeaponClass.Tracker, KillEnemiesQuestWeaponClass.State, KillEnemiesQuestWeaponClass>
{
    public DynamicIntegerValue KillCount;
    public DynamicEnumValue<EWeaponClass> Class;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
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
        public IDynamicValue<int>.IChoice KillThreshold;
        public IDynamicValue<EWeaponClass>.IChoice Class;
        public bool IsEligable(UCPlayer player) => true;
        public void Init(KillEnemiesQuestWeaponClass data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Class = data.Class.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("class", StringComparison.Ordinal))
                Class = DynamicEnumValue<EWeaponClass>.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("class", Class);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private readonly IDynamicValue<EWeaponClass>.IChoice Class;
        private int _kills;
        protected override bool CompletedCheck => _kills >= KillThreshold;
        public override short FlagValue => (short)_kills;
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold.InsistValue();
            Class = questState.Class;
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("kills", StringComparison.Ordinal))
                _kills = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", _kills);
        }
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam() && Class.IsMatch(kill.item.GetWeaponClass()))
            {
                _kills++;
                if (_kills >= KillThreshold)
                    TellCompleted();
                else
                    TellUpdated();
                return;
            }
        }
        protected override string Translate() => QuestData!.Translate(_player, _kills, KillThreshold, Class.ToString());
    }
}
[QuestData(EQuestType.KILL_ENEMIES_WITH_BRANCH)]
public class KillEnemiesQuestBranch : BaseQuestData<KillEnemiesQuestBranch.Tracker, KillEnemiesQuestBranch.State, KillEnemiesQuestBranch>
{
    public DynamicIntegerValue KillCount;
    public DynamicEnumValue<EBranch> Branch;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
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
        public IDynamicValue<int>.IChoice KillThreshold;
        public IDynamicValue<EBranch>.IChoice Branch;
        public bool IsEligable(UCPlayer player) => true;
        public void Init(KillEnemiesQuestBranch data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Branch = data.Branch.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("branch", StringComparison.Ordinal))
                Branch = DynamicEnumValue<EBranch>.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("branch", Branch);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private readonly IDynamicValue<EBranch>.IChoice Branch;
        private int _kills;
        protected override bool CompletedCheck => _kills >= KillThreshold;
        public override short FlagValue => (short)_kills;
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold.InsistValue();
            Branch = questState.Branch;
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("kills", StringComparison.Ordinal))
                _kills = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", _kills);
        }
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && Branch.IsMatch(_player.Branch) && kill.dead.GetTeam() != kill.killer.GetTeam())
            {
                _kills++;
                if (_kills >= KillThreshold)
                    TellCompleted();
                else
                    TellUpdated();
                return;
            }
        }
        protected override string Translate() => QuestData!.Translate(_player, _kills, KillThreshold, Branch.ToString());
    }
}
[QuestData(EQuestType.KILL_ENEMIES_WITH_TURRET)]
public class KillEnemiesQuestTurret : BaseQuestData<KillEnemiesQuestTurret.Tracker, KillEnemiesQuestTurret.State, KillEnemiesQuestTurret>
{
    public DynamicIntegerValue KillCount;
    public DynamicAssetValue<ItemGunAsset> Turrets;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
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
        public IDynamicValue<int>.IChoice KillThreshold;
        public DynamicAssetValue<ItemGunAsset>.Choice Weapon;
        public bool IsEligable(UCPlayer player) => true;
        public void Init(KillEnemiesQuestTurret data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Weapon = data.Turrets.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("turret", StringComparison.Ordinal))
                Weapon = DynamicAssetValue<ItemGunAsset>.ReadChoice(ref reader);
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
        private readonly DynamicAssetValue<ItemGunAsset>.Choice Weapon;
        private int _kills;
        protected override bool CompletedCheck => _kills >= KillThreshold;
        public override short FlagValue => (short)_kills;
        private string translationCache1;
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold.InsistValue();
            Weapon = questState.Weapon;
            translationCache1 = Weapon.GetCommaList();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("kills", StringComparison.Ordinal))
                _kills = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", _kills);
        }
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam())
            {
                InteractableVehicle veh = kill.killer.movement.getVehicle();
                if (veh == null) return;
                for (int i = 0; i < veh.turrets.Length; i++)
                {
                    if (veh.turrets[i] != null && veh.turrets[i].player?.player.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && 
                        veh.turrets[i].turret != null && Weapon.IsMatch(veh.turrets[i].turret.itemID))
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
        protected override string Translate() => QuestData!.Translate(_player, _kills, KillThreshold, translationCache1);
    }
}
[QuestData(EQuestType.KILL_ENEMIES_IN_SQUAD)]
public class KillEnemiesQuestSquad : BaseQuestData<KillEnemiesQuestSquad.Tracker, KillEnemiesQuestSquad.State, KillEnemiesQuestSquad>
{
    public DynamicIntegerValue KillCount;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
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
        public IDynamicValue<int>.IChoice KillThreshold;
        public bool IsEligable(UCPlayer player) => true;
        public void Init(KillEnemiesQuestSquad data)
        {
            this.KillThreshold = data.KillCount.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
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
        protected override bool CompletedCheck => _kills >= KillThreshold;
        public override short FlagValue => (short)_kills;
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold.InsistValue();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("kills", StringComparison.Ordinal))
                _kills = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", _kills);
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
        protected override string Translate() => QuestData!.Translate(_player, _kills, KillThreshold);
    }
}
[QuestData(EQuestType.KILL_ENEMIES_IN_FULL_SQUAD)]
public class KillEnemiesQuestFullSquad : BaseQuestData<KillEnemiesQuestFullSquad.Tracker, KillEnemiesQuestFullSquad.State, KillEnemiesQuestFullSquad>
{
    public DynamicIntegerValue KillCount;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
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
        public IDynamicValue<int>.IChoice KillThreshold;
        public bool IsEligable(UCPlayer player) => true;
        public void Init(KillEnemiesQuestFullSquad data)
        {
            this.KillThreshold = data.KillCount.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
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
        protected override bool CompletedCheck => _kills >= KillThreshold;
        public override short FlagValue => (short)_kills;
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold.InsistValue();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("kills", StringComparison.Ordinal))
                _kills = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", _kills);
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
        protected override string Translate() => QuestData!.Translate(_player, _kills, KillThreshold);
    }
}
[QuestData(EQuestType.KILL_ENEMIES_ON_POINT_DEFENSE)]
public class KillEnemiesQuestDefense : BaseQuestData<KillEnemiesQuestDefense.Tracker, KillEnemiesQuestDefense.State, KillEnemiesQuestDefense>
{
    public DynamicIntegerValue KillCount;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
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
        public IDynamicValue<int>.IChoice KillThreshold;
        public bool IsEligable(UCPlayer player) => true;
        public void Init(KillEnemiesQuestDefense data)
        {
            this.KillThreshold = data.KillCount.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
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
        protected override bool CompletedCheck => _kills >= KillThreshold;
        public override short FlagValue => (short)_kills;
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold.InsistValue();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("kills", StringComparison.Ordinal))
                _kills = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", _kills);
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
                                if (inv.ObjectiveTeam1 != null && fr.ObjectiveTeam1!.Owner == TeamManager.Other(team) && inv.ObjectiveTeam1.PlayerInRange(kill.killer.transform.position))
                                    goto add;
                            }
                            else if (inv.AttackingTeam == 2)
                            {
                                if (inv.ObjectiveTeam2 != null && fr.ObjectiveTeam2!.Owner == TeamManager.Other(team) && inv.ObjectiveTeam2.PlayerInRange(kill.killer.transform.position))
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
        protected override string Translate() => QuestData!.Translate(_player, _kills, KillThreshold);
    }
}
[QuestData(EQuestType.KILL_ENEMIES_ON_POINT_ATTACK)]
public class KillEnemiesQuestAttack : BaseQuestData<KillEnemiesQuestAttack.Tracker, KillEnemiesQuestAttack.State, KillEnemiesQuestAttack>
{
    public DynamicIntegerValue KillCount;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
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
        public IDynamicValue<int>.IChoice KillThreshold;
        public bool IsEligable(UCPlayer player) => true;
        public void Init(KillEnemiesQuestAttack data)
        {
            this.KillThreshold = data.KillCount.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
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
        protected override bool CompletedCheck => _kills >= KillThreshold;
        public override short FlagValue => (short)_kills;
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold.InsistValue();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("kills", StringComparison.Ordinal))
                _kills = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", _kills);
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
                                if (inv.ObjectiveTeam1 != null && fr.ObjectiveTeam1!.Owner == TeamManager.Other(team) && inv.ObjectiveTeam1.PlayerInRange(kill.killer.transform.position))
                                    goto add;
                            }
                            else if (inv.AttackingTeam == 2)
                            {
                                if (inv.ObjectiveTeam2 != null && fr.ObjectiveTeam2!.Owner == TeamManager.Other(team) && inv.ObjectiveTeam2.PlayerInRange(kill.killer.transform.position))
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
        protected override string Translate() => QuestData!.Translate(_player, _kills, KillThreshold);
    }
}
[QuestData(EQuestType.KING_SLAYER)]
public class KingSlayerQuest : BaseQuestData<KingSlayerQuest.Tracker, KingSlayerQuest.State, KingSlayerQuest>
{
    public DynamicIntegerValue KillCount;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
    }
    public struct State : IQuestState<Tracker, KingSlayerQuest>
    {
        public IDynamicValue<int>.IChoice KillThreshold;
        public bool IsEligable(UCPlayer player) => true;
        public void Init(KingSlayerQuest data)
        {
            this.KillThreshold = data.KillCount.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
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
        private UCPlayer? _kingSlayer;
        public override short FlagValue => (short)_kills;
        public override void ResetToDefaults() => _kills = 0;
        protected override bool CompletedCheck => _kills >= KillThreshold;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold.InsistValue();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("kills", StringComparison.Ordinal))
                _kills = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", _kills);
        }

        private int prevInd = -1;
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            ulong team = kill.killer.GetTeam();
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != team)
            {
                int maxXp = 0;
                int ind = -1;
                ulong other = TeamManager.Other(team);
                for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                {
                    if (PlayerManager.OnlinePlayers[i].GetTeam() == other)
                    {
                        if (PlayerManager.OnlinePlayers[i].CachedXP > maxXp)
                        {
                            ind = i;
                            maxXp = PlayerManager.OnlinePlayers[i].CachedXP;
                        }
                    }
                }
                if (ind != -1)
                {
                    _kingSlayer = PlayerManager.OnlinePlayers[ind];
                    if (PlayerManager.OnlinePlayers[ind].Steam64 == kill.dead.channel.owner.playerID.steamID.m_SteamID)
                    {
                        _kills++;
                        if (_kills >= KillThreshold)
                            TellCompleted();
                        else
                            TellUpdated();
                        prevInd = ind;
                        return;
                    }
                }
                if (prevInd != ind)
                {
                    TellUpdated(true);
                    prevInd = ind;
                }
            }
        }
        protected override string Translate() => QuestData!.Translate(_player, _kills, KillThreshold, _kingSlayer != null ? F.GetPlayerOriginalNames(_kingSlayer).CharacterName : "Unknown");
    }
}
[QuestData(EQuestType.KILL_STREAK)]
public class KillStreakQuest : BaseQuestData<KillStreakQuest.Tracker, KillStreakQuest.State, KillStreakQuest>
{
    public DynamicIntegerValue StreakCount;
    public DynamicIntegerValue StreakLength;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("num_streaks", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out StreakCount))
                StreakCount = new DynamicIntegerValue(5);
        }
        else if (propertyname.Equals("streak_length", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out StreakLength))
                StreakLength = new DynamicIntegerValue(5);
        }
    }
    public struct State : IQuestState<Tracker, KillStreakQuest>
    {
        public IDynamicValue<int>.IChoice StreakCount;
        public IDynamicValue<int>.IChoice StreakLength;
        public bool IsEligable(UCPlayer player) => true;
        public void Init(KillStreakQuest data)
        {
            this.StreakCount = data.StreakCount.GetValue();
            this.StreakLength = data.StreakLength.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("num_streaks", StringComparison.Ordinal))
                StreakCount = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("streak_length", StringComparison.Ordinal))
                StreakLength = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("num_streaks", StreakCount);
            writer.WriteProperty("streak_length", StreakLength);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill, INotifyOnDeath
    {
        private readonly int StreakCount = 0;
        private readonly int StreakLength = 0;
        private int _streakProgress;
        private int _streaks;
        protected override bool CompletedCheck => _streaks >= StreakCount;
        public override short FlagValue => (short)_streaks;
        public override void ResetToDefaults()
        {
            _streaks = 0;
            _streakProgress = 0;
        }
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            StreakCount = questState.StreakCount.InsistValue();
            StreakLength = questState.StreakLength.InsistValue();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("current_streak_progress", StringComparison.Ordinal))
                _streakProgress = reader.GetInt32();
            else if (reader.TokenType == JsonTokenType.Number && prop.Equals("streaks_completed", StringComparison.Ordinal))
                _streaks = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("current_streak_progress", _streakProgress);
            writer.WriteProperty("streaks_completed", _streaks);
        }
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            ulong team = kill.killer.GetTeam();
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != team)
            {
                _streakProgress++;
                if (_streakProgress >= StreakLength)
                {
                    _streakProgress = 0;
                    _streaks++;
                    if (_streaks >= StreakCount)
                        TellCompleted();
                    else
                        TellUpdated();
                }
            }
        }
        public void OnDeath(UCWarfare.DeathEventArgs death)
        {
            if (death.dead.channel.owner.playerID.steamID.m_SteamID == _player.Steam64)
            {
                _streakProgress = 0;
                TellUpdated();
            }
        }
        public void OnSuicide(UCWarfare.SuicideEventArgs death)
        {
            if (death.dead.channel.owner.playerID.steamID.m_SteamID == _player.Steam64)
            {
                _streakProgress = 0;
                TellUpdated();
            }
        }
        protected override string Translate() => QuestData!.Translate(_player, _streakProgress, StreakLength, StreakCount);
    }
}