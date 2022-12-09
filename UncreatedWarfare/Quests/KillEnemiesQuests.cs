using SDG.Unturned;
using System;
using System.Linq;
using System.Text.Json;
using Uncreated.Json;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Quests.Types;

// Attribute links the kit to the Type
[QuestData(QuestType.KILL_ENEMIES)]
public class KillEnemiesQuest : BaseQuestData<KillEnemiesQuest.Tracker, KillEnemiesQuest.State, KillEnemiesQuest>
{
    // dynamic int allows for constants, ranges, or sets
    public DynamicIntegerValue KillCount;
    // > 0 will run the Tick function in trackers
    public override int TickFrequencySeconds => 0;
    // just copy paste this couldn't do it with generics.
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, in preset);
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
        [RewardField("k")]
        public IDynamicValue<int>.IChoice KillThreshold;
        public IDynamicValue<int>.IChoice FlagValue => KillThreshold;
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
        private readonly int _killThreshold;
        private int _kills;
        protected override bool CompletedCheck => _kills >= _killThreshold;
        public override short FlagValue => (short)_kills;
        public override int Reward => _killThreshold * 10;
        // loads a tracker from a state instead of randomly picking values each time.
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            _killThreshold = questState.KillThreshold.InsistValue(); // insisting for a value asks for ONE value (defined with a $, otherwise it returns 0)
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
        public void OnKill(PlayerDied e)
        {
            if (e.Killer!.Steam64 == _player.Steam64 && e.Cause != EDeathCause.SHRED)
            {
                _kills++;
                if (_kills >= _killThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        public override void ResetToDefaults() => _kills = 0;
        // translate the description of the quest, pass any data that will show up in the description once we make them
        //                                         in this case, "Kill {_kills}/{KillThreshold} enemies."
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, _player, _kills, _killThreshold);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}

[QuestData(QuestType.KILL_FROM_RANGE)]
public class KillEnemiesRangeQuest : BaseQuestData<KillEnemiesRangeQuest.Tracker, KillEnemiesRangeQuest.State, KillEnemiesRangeQuest>
{
    public DynamicIntegerValue KillCount;
    public DynamicFloatValue Range;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
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
                Range = new DynamicFloatValue(new FloatRange(50, 200, 5));
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesRangeQuest>
    {
        [RewardField("k")]
        public IDynamicValue<int>.IChoice KillThreshold;
        [RewardField("d")]
        public IDynamicValue<float>.IChoice Range;
        public IDynamicValue<int>.IChoice FlagValue => KillThreshold;
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
        private readonly int _killThreshold;
        private readonly float _range;
        private int _kills;
        protected override bool CompletedCheck => _kills >= _killThreshold;
        public override short FlagValue => (short)_kills;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            _killThreshold = questState.KillThreshold.InsistValue();
            _range = questState.Range.InsistValue();
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
        public void OnKill(PlayerDied e)
        {
            if (e.Killer!.Steam64 == _player.Steam64 &&
                e.KillDistance >= _range &&
                e.Cause is EDeathCause.GUN or EDeathCause.MISSILE or EDeathCause.GRENADE or EDeathCause.MELEE or EDeathCause.VEHICLE or EDeathCause.LANDMINE or EDeathCause.CHARGE or EDeathCause.SPLASH && e.Cause != EDeathCause.SHRED)
            {
                _kills++;
                if (_kills >= _killThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, _player, _kills, _killThreshold, _range);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KILL_ENEMIES_WITH_WEAPON)]
public class KillEnemiesQuestWeapon : BaseQuestData<KillEnemiesQuestWeapon.Tracker, KillEnemiesQuestWeapon.State, KillEnemiesQuestWeapon>
{
    public DynamicIntegerValue KillCount;
    public DynamicAssetValue<ItemWeaponAsset> Weapon = new DynamicAssetValue<ItemWeaponAsset>(DynamicValueType.Wildcard, ChoiceBehavior.Inclusive);
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
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
                Weapon = new DynamicAssetValue<ItemWeaponAsset>(DynamicValueType.Wildcard, ChoiceBehavior.Inclusive);
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestWeapon>
    {
        [RewardField("k")]
        public IDynamicValue<int>.IChoice KillThreshold;
        public DynamicAssetValue<ItemWeaponAsset>.Choice Weapon;
        public IDynamicValue<int>.IChoice FlagValue => KillThreshold;
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
        private readonly int _killThreshold;
        private readonly DynamicAssetValue<ItemWeaponAsset>.Choice _weapon;
        private readonly string _translationCache1;
        private readonly string _translationCache2;
        private int _kills;
        protected override bool CompletedCheck => _kills >= _killThreshold;
        public override short FlagValue => (short)_kills;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            _killThreshold = questState.KillThreshold.InsistValue();
            _weapon = questState.Weapon;
            _translationCache1 = _weapon.GetCommaList();
            _translationCache2 = F.FilterRarityToHex(_weapon.GetAssetValueSet().FirstOrDefault()?.rarity.ToString().ToLower() ?? null!);
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
        public void OnKill(PlayerDied e)
        {
            if (e.Killer!.Steam64 == _player.Steam64 && e.PrimaryAsset != default && e.Cause != EDeathCause.SHRED)
            {
                if (_weapon.IsMatch(e.PrimaryAsset))
                {
                    _kills++;
                    if (_kills >= _killThreshold)
                        TellCompleted();
                    else
                        TellUpdated();
                }
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, _player, _kills, _killThreshold, _translationCache1, _translationCache2);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KILL_FROM_RANGE_WITH_WEAPON)]
public class KillEnemiesRangeQuestWeapon : BaseQuestData<KillEnemiesRangeQuestWeapon.Tracker, KillEnemiesRangeQuestWeapon.State, KillEnemiesRangeQuestWeapon>
{
    public DynamicIntegerValue KillCount;
    public DynamicAssetValue<ItemWeaponAsset> Weapon = new DynamicAssetValue<ItemWeaponAsset>(DynamicValueType.Wildcard, ChoiceBehavior.Inclusive);
    public DynamicFloatValue Range;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
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
                Weapon = new DynamicAssetValue<ItemWeaponAsset>(DynamicValueType.Wildcard, ChoiceBehavior.Inclusive);
        }
        else if (propertyname.Equals("range", StringComparison.Ordinal))
        {
            if (!reader.TryReadFloatValue(out Range))
                Range = new DynamicFloatValue(new FloatRange(50, 200, 5));
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesRangeQuestWeapon>
    {
        [RewardField("k")]
        public IDynamicValue<int>.IChoice KillThreshold;
        public DynamicAssetValue<ItemWeaponAsset>.Choice Weapon;
        [RewardField("d")]
        public IDynamicValue<float>.IChoice Range;
        public IDynamicValue<int>.IChoice FlagValue => KillThreshold;
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
        private readonly int _killThreshold;
        private readonly DynamicAssetValue<ItemWeaponAsset>.Choice _weapon;
        private readonly float _range;
        private readonly string _translationCache1;
        private readonly string _translationCache2;
        private int _kills;
        protected override bool CompletedCheck => _kills >= _killThreshold;
        public override short FlagValue => (short)_kills;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            _killThreshold = questState.KillThreshold.InsistValue();
            _weapon = questState.Weapon;
            _range = questState.Range.InsistValue();
            _translationCache1 = _weapon.GetCommaList();
            _translationCache2 = F.FilterRarityToHex(_weapon.GetAssetValueSet().FirstOrDefault()?.rarity.ToString().ToLower() ?? null!);
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
        public void OnKill(PlayerDied e)
        {
            if (e.Killer!.Steam64 == _player.Steam64 && e.PrimaryAsset != default && e.KillDistance >= _range
                && e.Cause is EDeathCause.GUN or EDeathCause.MISSILE or EDeathCause.GRENADE or EDeathCause.MELEE or EDeathCause.VEHICLE or EDeathCause.LANDMINE or EDeathCause.CHARGE or EDeathCause.SPLASH && e.Cause != EDeathCause.SHRED)
            {
                if (_weapon.IsMatch(e.PrimaryAsset))
                {
                    _kills++;
                    if (_kills >= _killThreshold)
                        TellCompleted();
                    else
                        TellUpdated();
                }
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, _player, _kills, _killThreshold, _range.ToString(Data.Locale), _translationCache1, _translationCache2);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KILL_ENEMIES_WITH_KIT)]
public class KillEnemiesQuestKit : BaseQuestData<KillEnemiesQuestKit.Tracker, KillEnemiesQuestKit.State, KillEnemiesQuestKit>
{
    public DynamicIntegerValue KillCount;
    public DynamicStringValue Kits = new DynamicStringValue(true, DynamicValueType.Wildcard, ChoiceBehavior.Selective);
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
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
                Kits = new DynamicStringValue(true, DynamicValueType.Wildcard, ChoiceBehavior.Selective);
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestKit>
    {
        [RewardField("k")]
        public IDynamicValue<int>.IChoice KillThreshold;
        internal DynamicStringValue.Choice Kit;
        public IDynamicValue<int>.IChoice FlagValue => KillThreshold;
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
                Kit = DynamicStringValue.ReadChoiceIntl(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("kit", Kit);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int _killThreshold;
        private readonly DynamicStringValue.Choice _kit;
        private readonly string _translationCache1;
        private int _kills;
        protected override bool CompletedCheck => _kills >= _killThreshold;
        public override short FlagValue => (short)_kills;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            _killThreshold = questState.KillThreshold.InsistValue();
            _kit = questState.Kit;
            _translationCache1 = _kit.GetKitNames(_player == null ? 0 : _player.Steam64);
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
        public void OnKill(PlayerDied e)
        {
            if (e.Killer!.Steam64 == _player.Steam64 && e.Killer.HasKit && _kit.IsMatch(e.Killer.ActiveKit!.Item!.Id) && e.Cause != EDeathCause.SHRED)
            {
                _kills++;
                if (_kills >= _killThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, _player, _kills, _killThreshold, _translationCache1);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KILL_FROM_RANGE_WITH_KIT)]
public class KillEnemiesQuestKitRange : BaseQuestData<KillEnemiesQuestKitRange.Tracker, KillEnemiesQuestKitRange.State, KillEnemiesQuestKitRange>
{
    public DynamicIntegerValue KillCount;
    public DynamicStringValue Kits;
    public DynamicFloatValue Range;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
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
                Kits = new DynamicStringValue(true, DynamicValueType.Wildcard, ChoiceBehavior.Selective);
        }
        else if (propertyname.Equals("range", StringComparison.Ordinal))
        {
            if (!reader.TryReadFloatValue(out Range))
                Range = new DynamicFloatValue(200f);
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestKitRange>
    {
        [RewardField("k")]
        public IDynamicValue<int>.IChoice KillThreshold;
        internal DynamicStringValue.Choice Kit;
        [RewardField("d")]
        public IDynamicValue<float>.IChoice Range;
        public IDynamicValue<int>.IChoice FlagValue => KillThreshold;
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
                Kit = DynamicStringValue.ReadChoiceIntl(ref reader);
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
        private readonly int _killThreshold;
        private readonly DynamicStringValue.Choice _kit;
        private readonly string _translationCache1;
        private readonly float _range;
        private int _kills;
        protected override bool CompletedCheck => _kills >= _killThreshold;
        public override short FlagValue => (short)_kills;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            _killThreshold = questState.KillThreshold.InsistValue();
            _kit = questState.Kit;
            _range = questState.Range.InsistValue();
            _translationCache1 = _kit.GetKitNames(_player == null ? 0 : _player.Steam64);
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
        public void OnKill(PlayerDied e)
        {
            if (e.Killer!.Steam64 == _player.Steam64 && e.KillDistance >= _range
                && e.Cause is EDeathCause.GUN or EDeathCause.MISSILE or EDeathCause.GRENADE or EDeathCause.MELEE or EDeathCause.VEHICLE or EDeathCause.LANDMINE or EDeathCause.CHARGE or EDeathCause.SPLASH
                && e.Killer.HasKit && _kit.IsMatch(e.Killer.ActiveKit!.Item!.Id) && e.Cause != EDeathCause.SHRED)
            {
                _kills++;
                if (_kills >= _killThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, _player, _kills, _killThreshold, _range.ToString(Data.Locale), _translationCache1);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KILL_ENEMIES_WITH_KIT_CLASS)]
public class KillEnemiesQuestKitClass : BaseQuestData<KillEnemiesQuestKitClass.Tracker, KillEnemiesQuestKitClass.State, KillEnemiesQuestKitClass>
{
    public DynamicIntegerValue KillCount;
    public DynamicEnumValue<Class> Class;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
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
                Class = new DynamicEnumValue<Class>(Kits.Class.None);
                L.LogWarning("Invalid class in quest " + QuestType);
            }
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestKitClass>
    {
        [RewardField("k")]
        public IDynamicValue<int>.IChoice KillThreshold;
        internal DynamicEnumValue<Class>.Choice Class;
        public IDynamicValue<int>.IChoice FlagValue => KillThreshold;
        public bool IsEligable(UCPlayer player) => true;
        public void Init(KillEnemiesQuestKitClass data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Class = data.Class.GetValueIntl();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("class", StringComparison.Ordinal))
                Class = DynamicEnumValue<Class>.ReadChoiceIntl(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("class", Class);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int _killThreshold;
        private readonly DynamicEnumValue<Class>.Choice _class;
        private readonly string _translationCache1;
        private int _kills;
        protected override bool CompletedCheck => _kills >= _killThreshold;
        public override short FlagValue => (short)_kills;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            _killThreshold = questState.KillThreshold.InsistValue();
            _class = questState.Class;
            _translationCache1 = _class.GetCommaList(_player == null ? 0 : _player.Steam64);
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
        public void OnKill(PlayerDied e)
        {
            if (e.Killer!.Steam64 == _player.Steam64 && _class.IsMatch(_player.KitClass))
            {
                _kills++;
                if (_kills >= _killThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, _player, _kills, _killThreshold, _translationCache1);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KILL_FROM_RANGE_WITH_CLASS)]
public class KillEnemiesQuestKitClassRange : BaseQuestData<KillEnemiesQuestKitClassRange.Tracker, KillEnemiesQuestKitClassRange.State, KillEnemiesQuestKitClassRange>
{
    public DynamicIntegerValue KillCount;
    public DynamicEnumValue<Class> Class;
    public DynamicFloatValue Range;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
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
                Class = new DynamicEnumValue<Class>(Kits.Class.None);
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
        [RewardField("k")]
        public IDynamicValue<int>.IChoice KillThreshold;
        internal DynamicEnumValue<Class>.Choice Class;
        [RewardField("d")]
        public IDynamicValue<float>.IChoice Range;
        public IDynamicValue<int>.IChoice FlagValue => KillThreshold;
        public bool IsEligable(UCPlayer player) => true;
        public void Init(KillEnemiesQuestKitClassRange data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Class = data.Class.GetValueIntl();
            this.Range = data.Range.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("class", StringComparison.Ordinal))
                Class = DynamicEnumValue<Class>.ReadChoiceIntl(ref reader);
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
        private readonly int _killThreshold;
        private readonly DynamicEnumValue<Class>.Choice _class;
        private readonly string _translationCache1;
        private readonly float _range;
        private int _kills;
        protected override bool CompletedCheck => _kills >= _killThreshold;
        public override short FlagValue => (short)_kills;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            _killThreshold = questState.KillThreshold.InsistValue();
            _class = questState.Class;
            _range = questState.Range.InsistValue();
            _translationCache1 = _class.GetCommaList(_player == null ? 0 : _player.Steam64);
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
        public void OnKill(PlayerDied e)
        {
            if (e.Killer!.Steam64 == _player.Steam64 && e.KillDistance >= _range
                && e.Cause is EDeathCause.GUN or EDeathCause.MISSILE or EDeathCause.GRENADE or EDeathCause.MELEE or EDeathCause.VEHICLE or EDeathCause.SPLASH &&
                _class.IsMatch(e.Killer.KitClass))
            {
                _kills++;
                if (_kills >= _killThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, _player, _kills, _killThreshold, _range.ToString(Data.Locale), _translationCache1);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KILL_ENEMIES_WITH_WEAPON_CLASS)]
public class KillEnemiesQuestWeaponClass : BaseQuestData<KillEnemiesQuestWeaponClass.Tracker, KillEnemiesQuestWeaponClass.State, KillEnemiesQuestWeaponClass>
{
    public DynamicIntegerValue KillCount;
    public DynamicEnumValue<WeaponClass> Class;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
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
                Class = new DynamicEnumValue<WeaponClass>(WeaponClass.Unknown);
                L.LogWarning("Invalid weapon class in quest " + QuestType);
            }
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestWeaponClass>
    {
        [RewardField("k")]
        public IDynamicValue<int>.IChoice KillThreshold;
        internal DynamicEnumValue<WeaponClass>.Choice Class;
        public IDynamicValue<int>.IChoice FlagValue => KillThreshold;
        public bool IsEligable(UCPlayer player) => true;
        public void Init(KillEnemiesQuestWeaponClass data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Class = data.Class.GetValueIntl();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("class", StringComparison.Ordinal))
                Class = DynamicEnumValue<WeaponClass>.ReadChoiceIntl(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("class", Class);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int _killThreshold;
        private readonly DynamicEnumValue<WeaponClass>.Choice _class;
        private readonly string _translationCache1;
        private int _kills;
        protected override bool CompletedCheck => _kills >= _killThreshold;
        public override short FlagValue => (short)_kills;
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            _killThreshold = questState.KillThreshold.InsistValue();
            _class = questState.Class;
            _translationCache1 = _class.GetCommaList(_player == null ? 0 : _player.Steam64);
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
        public void OnKill(PlayerDied e)
        {
            if (e.Killer!.Steam64 == _player.Steam64 && !e.PrimaryAssetIsVehicle && _class.IsMatch(e.PrimaryAsset.GetWeaponClass()) && e.Cause != EDeathCause.SHRED)
            {
                _kills++;
                if (_kills >= _killThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, _player, _kills, _killThreshold, _translationCache1);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KILL_ENEMIES_WITH_BRANCH)]
public class KillEnemiesQuestBranch : BaseQuestData<KillEnemiesQuestBranch.Tracker, KillEnemiesQuestBranch.State, KillEnemiesQuestBranch>
{
    public DynamicIntegerValue KillCount;
    public DynamicEnumValue<Branch> Branch;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
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
                Branch = new DynamicEnumValue<Branch>(Kits.Branch.Default);
                L.LogWarning("Invalid branch in quest " + QuestType);
            }
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestBranch>
    {
        [RewardField("k")]
        public IDynamicValue<int>.IChoice KillThreshold;
        internal DynamicEnumValue<Branch>.Choice Branch;
        public IDynamicValue<int>.IChoice FlagValue => KillThreshold;
        public bool IsEligable(UCPlayer player) => true;
        public void Init(KillEnemiesQuestBranch data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Branch = data.Branch.GetValueIntl();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("branch", StringComparison.Ordinal))
                Branch = DynamicEnumValue<Branch>.ReadChoiceIntl(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("branch", Branch);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int _killThreshold;
        private readonly DynamicEnumValue<Branch>.Choice _branch;
        private readonly string _translationCache1;
        private int _kills;
        protected override bool CompletedCheck => _kills >= _killThreshold;
        public override short FlagValue => (short)_kills;
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            _killThreshold = questState.KillThreshold.InsistValue();
            _branch = questState.Branch;
            _translationCache1 = _branch.GetCommaList(_player == null ? 0 : _player.Steam64);
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
        public void OnKill(PlayerDied e)
        {
            if (e.Killer!.Steam64 == _player.Steam64 && _branch.IsMatch(_player.Branch) && e.Cause != EDeathCause.SHRED)
            {
                _kills++;
                if (_kills >= _killThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, _player, _kills, _killThreshold, _translationCache1);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KILL_ENEMIES_WITH_TURRET)]
public class KillEnemiesQuestTurret : BaseQuestData<KillEnemiesQuestTurret.Tracker, KillEnemiesQuestTurret.State, KillEnemiesQuestTurret>
{
    public DynamicIntegerValue KillCount;
    public DynamicAssetValue<ItemGunAsset> Turrets;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
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
        [RewardField("k")]
        public IDynamicValue<int>.IChoice KillThreshold;
        public DynamicAssetValue<ItemGunAsset>.Choice Weapon;
        public IDynamicValue<int>.IChoice FlagValue => KillThreshold;
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
        private readonly int _killThreshold;
        private readonly DynamicAssetValue<ItemGunAsset>.Choice _weapon;
        private int _kills;
        protected override bool CompletedCheck => _kills >= _killThreshold;
        public override short FlagValue => (short)_kills;
        private readonly string _translationCache1;
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            _killThreshold = questState.KillThreshold.InsistValue();
            _weapon = questState.Weapon;
            _translationCache1 = _weapon.GetCommaList();
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
        public void OnKill(PlayerDied e)
        {
            if (e.Killer!.Steam64 == _player.Steam64 && e.Cause != EDeathCause.SHRED)
            {
                InteractableVehicle? veh = e.Killer.Player.movement.getVehicle();
                if (veh == null) return;
                for (int i = 0; i < veh.turrets.Length; i++)
                {
                    Passenger passenger = veh.turrets[i];
                    if (passenger != null && passenger.player != null && passenger.player.playerID.steamID.m_SteamID == _player.Steam64 &&
                        passenger.turret != null && _weapon.IsMatch(passenger.turret.itemID))
                    {
                        _kills++;
                        if (_kills >= _killThreshold)
                            TellCompleted();
                        else
                            TellUpdated();
                        return;
                    }
                }
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, _player, _kills, _killThreshold, _translationCache1);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KILL_ENEMIES_IN_SQUAD)]
public class KillEnemiesQuestSquad : BaseQuestData<KillEnemiesQuestSquad.Tracker, KillEnemiesQuestSquad.State, KillEnemiesQuestSquad>
{
    public DynamicIntegerValue KillCount;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
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
        [RewardField("k")]
        public IDynamicValue<int>.IChoice KillThreshold;
        public IDynamicValue<int>.IChoice FlagValue => KillThreshold;
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
        private readonly int _killThreshold;
        private int _kills;
        protected override bool CompletedCheck => _kills >= _killThreshold;
        public override short FlagValue => (short)_kills;
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            _killThreshold = questState.KillThreshold.InsistValue();
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
        public void OnKill(PlayerDied e)
        {
            if (e.Killer!.Steam64 == _player.Steam64 && _player.Squad != null && _player.Squad.Members.Count > 1 && e.Cause != EDeathCause.SHRED)
            {
                _kills++;
                if (_kills >= _killThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, _player, _kills, _killThreshold);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KILL_ENEMIES_IN_FULL_SQUAD)]
public class KillEnemiesQuestFullSquad : BaseQuestData<KillEnemiesQuestFullSquad.Tracker, KillEnemiesQuestFullSquad.State, KillEnemiesQuestFullSquad>
{
    public DynamicIntegerValue KillCount;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
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
        [RewardField("k")]
        public IDynamicValue<int>.IChoice KillThreshold;
        public IDynamicValue<int>.IChoice FlagValue => KillThreshold;
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
        private readonly int _killThreshold;
        private int _kills;
        protected override bool CompletedCheck => _kills >= _killThreshold;
        public override short FlagValue => (short)_kills;
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            _killThreshold = questState.KillThreshold.InsistValue();
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
        public void OnKill(PlayerDied e)
        {
            if (e.Killer!.Steam64 == _player.Steam64 && _player.Squad != null && _player.Squad.IsFull() && e.Cause != EDeathCause.SHRED)
            {
                _kills++;
                if (_kills >= _killThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, _player, _kills, _killThreshold);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KILL_ENEMIES_ON_POINT_DEFENSE)]
public class KillEnemiesQuestDefense : BaseQuestData<KillEnemiesQuestDefense.Tracker, KillEnemiesQuestDefense.State, KillEnemiesQuestDefense>
{
    public DynamicIntegerValue KillCount;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
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
        [RewardField("k")]
        public IDynamicValue<int>.IChoice KillThreshold;
        public IDynamicValue<int>.IChoice FlagValue => KillThreshold;
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
        private readonly int _killThreshold;
        private int _kills;
        protected override bool CompletedCheck => _kills >= _killThreshold;
        public override short FlagValue => (short)_kills;
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            _killThreshold = questState.KillThreshold.InsistValue();
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
        public void OnKill(PlayerDied e)
        {
            ulong team = e.KillerTeam;
            if (e.Killer!.Steam64 == _player.Steam64 && e.Cause != EDeathCause.SHRED)
            {
                if (Data.Is(out Gamemodes.Interfaces.IFlagTeamObjectiveGamemode fr))
                {
                    Vector3 deadPos = e.Player.Position;
                    Vector3 killerPos = e.Killer.Position;
                    if (Data.Is<Gamemodes.Flags.TeamCTF.TeamCTF>(out _))
                    {
                        if (
                            (team == 1 && fr.ObjectiveTeam1 != null && fr.ObjectiveTeam1.Owner == 1 && (fr.ObjectiveTeam1.PlayerInRange(killerPos) || fr.ObjectiveTeam1.PlayerInRange(deadPos))) ||
                            (team == 2 && fr.ObjectiveTeam2 != null && fr.ObjectiveTeam2.Owner == 2 && (fr.ObjectiveTeam2.PlayerInRange(killerPos) || fr.ObjectiveTeam2.PlayerInRange(deadPos))))
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
                                if (inv.ObjectiveTeam1 != null && fr.ObjectiveTeam1!.Owner == TeamManager.Other(team) && (inv.ObjectiveTeam1.PlayerInRange(killerPos) || inv.ObjectiveTeam1.PlayerInRange(deadPos)))
                                    goto add;
                            }
                            else if (inv.AttackingTeam == 2)
                            {
                                if (inv.ObjectiveTeam2 != null && fr.ObjectiveTeam2!.Owner == TeamManager.Other(team) && (inv.ObjectiveTeam2.PlayerInRange(killerPos) || inv.ObjectiveTeam2.PlayerInRange(deadPos)))
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
                                if (cache != null && cache.IsActive && (cache.Cache.Position - killerPos).sqrMagnitude > 3600) // 60m
                                    goto add;
                            }
                        }
                    }
                }
            }
            return;
        add:
            _kills++;
            if (_kills >= _killThreshold)
                TellCompleted();
            else
                TellUpdated();
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, _player, _kills, _killThreshold);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KILL_ENEMIES_ON_POINT_ATTACK)]
public class KillEnemiesQuestAttack : BaseQuestData<KillEnemiesQuestAttack.Tracker, KillEnemiesQuestAttack.State, KillEnemiesQuestAttack>
{
    public DynamicIntegerValue KillCount;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
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
        [RewardField("k")]
        public IDynamicValue<int>.IChoice KillThreshold;
        public IDynamicValue<int>.IChoice FlagValue => KillThreshold;
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
        private readonly int _killThreshold;
        private int _kills;
        protected override bool CompletedCheck => _kills >= _killThreshold;
        public override short FlagValue => (short)_kills;
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            _killThreshold = questState.KillThreshold.InsistValue();
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
        public void OnKill(PlayerDied e)
        {
            ulong team = e.KillerTeam;
            if (e.Killer!.Steam64 == _player.Steam64 && e.Cause != EDeathCause.SHRED)
            {
                Vector3 deadPos = e.Player.Position;
                Vector3 killerPos = e.Killer.Position;
                if (Data.Is(out Gamemodes.Interfaces.IFlagTeamObjectiveGamemode fr))
                {
                    if (Data.Is<Gamemodes.Flags.TeamCTF.TeamCTF>(out _))
                    {
                        if (
                            (team == 1 && fr.ObjectiveTeam1 != null && fr.ObjectiveTeam1.Owner == 2 && (fr.ObjectiveTeam1.PlayerInRange(killerPos) || fr.ObjectiveTeam1.PlayerInRange(deadPos))) ||
                            (team == 2 && fr.ObjectiveTeam2 != null && fr.ObjectiveTeam2.Owner == 1 && (fr.ObjectiveTeam2.PlayerInRange(killerPos) || fr.ObjectiveTeam2.PlayerInRange(deadPos))))
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
                                if (inv.ObjectiveTeam1 != null && fr.ObjectiveTeam1!.Owner == TeamManager.Other(team) && (inv.ObjectiveTeam1.PlayerInRange(killerPos) || inv.ObjectiveTeam1.PlayerInRange(deadPos)))
                                    goto add;
                            }
                            else if (inv.AttackingTeam == 2)
                            {
                                if (inv.ObjectiveTeam2 != null && fr.ObjectiveTeam2!.Owner == TeamManager.Other(team) && (inv.ObjectiveTeam2.PlayerInRange(killerPos) || inv.ObjectiveTeam2.PlayerInRange(deadPos)))
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
                                if (cache != null && cache.IsActive && (cache.Cache.Position - killerPos).sqrMagnitude > 3600) // 60m
                                    goto add;
                            }
                        }
                    }
                }
            }
            return;
        add:
            _kills++;
            if (_kills >= _killThreshold)
                TellCompleted();
            else
                TellUpdated();
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, _player, _kills, _killThreshold);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KING_SLAYER)]
public class KingSlayerQuest : BaseQuestData<KingSlayerQuest.Tracker, KingSlayerQuest.State, KingSlayerQuest>
{
    public DynamicIntegerValue KillCount;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
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
        [RewardField("k")]
        public IDynamicValue<int>.IChoice KillThreshold;
        public IDynamicValue<int>.IChoice FlagValue => KillThreshold;
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
        private readonly int _killThreshold;
        private int _kills;
        private UCPlayer? _kingSlayer;
        public override short FlagValue => (short)_kills;
        public override void ResetToDefaults() => _kills = 0;
        protected override bool CompletedCheck => _kills >= _killThreshold;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            _killThreshold = questState.KillThreshold.InsistValue();
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

        private int _prevInd = -1;
        public void OnKill(PlayerDied e)
        {
            ulong team = e.Killer!.GetTeam();
            if (e.Killer!.Steam64 == _player.Steam64 && e.Cause != EDeathCause.SHRED)
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
                    if (_kingSlayer.Steam64 == e.Player.Steam64)
                    {
                        _kills++;
                        if (_kills >= _killThreshold)
                            TellCompleted();
                        else
                            TellUpdated();
                        _prevInd = ind;
                        return;
                    }
                }
                if (_prevInd != ind)
                {
                    TellUpdated(true);
                    _prevInd = ind;
                }
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, _player, _kills, _killThreshold);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KILL_STREAK)]
public class KillStreakQuest : BaseQuestData<KillStreakQuest.Tracker, KillStreakQuest.State, KillStreakQuest>
{
    public DynamicIntegerValue StreakCount;
    public DynamicIntegerValue StreakLength;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
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
        [RewardField("strNum")]
        public IDynamicValue<int>.IChoice StreakCount;
        [RewardField("strLen")]
        public IDynamicValue<int>.IChoice StreakLength;
        public IDynamicValue<int>.IChoice FlagValue => StreakCount;
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
        private readonly int _streakCount;
        private readonly int _streakLength;
        private int _streakProgress;
        private int _streaks;
        protected override bool CompletedCheck => _streaks >= _streakCount;
        public override short FlagValue => (short)_streaks;
        public override void ResetToDefaults()
        {
            _streaks = 0;
            _streakProgress = 0;
        }
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            _streakCount = questState.StreakCount.InsistValue();
            _streakLength = questState.StreakLength.InsistValue();
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
        public void OnKill(PlayerDied e)
        {
            if (e.Killer!.Steam64 == _player.Steam64 && e.Cause != EDeathCause.SHRED)
            {
                _streakProgress++;
                if (_streakProgress >= _streakLength)
                {
                    _streakProgress = 0;
                    _streaks++;
                    if (_streaks >= _streakCount)
                        TellCompleted();
                    else
                        TellUpdated();
                }
            }
        }
        public void OnDeath(PlayerDied e)
        {
            if (e.Steam64 == _player.Steam64)
            {
                _streakProgress = 0;
                TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, _player, _streakProgress, _streakLength, _streakCount);
        public override void ManualComplete()
        {
            _streakProgress = 0;
            _streakProgress = _streakCount;
            base.ManualComplete();
        }
    }
}