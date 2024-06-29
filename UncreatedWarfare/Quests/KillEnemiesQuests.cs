using SDG.Unturned;
using System;
using System.Linq;
using System.Text.Json;
using Uncreated.Json;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Quests.Types;

// Attribute links the kit to the Type
[QuestData(QuestType.KillEnemies)]
public class KillEnemiesQuest : BaseQuestData<KillEnemiesQuest.Tracker, KillEnemiesQuest.State, KillEnemiesQuest>
{
    public DynamicIntegerValue KillCount;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, ref State state, IQuestPreset? preset) => new Tracker(this, player, ref state, preset);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
    }
    public struct State : IQuestState<KillEnemiesQuest>
    {
        [RewardField("k")]
        public DynamicIntegerValue.Choice KillThreshold;

        public readonly DynamicIntegerValue.Choice FlagValue => KillThreshold;
        public void CreateFromTemplate(KillEnemiesQuest data)
        {
            KillThreshold = data.KillCount.GetValue();
        }
        public readonly bool IsEligable(UCPlayer player) => true;

        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public readonly void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
        }
    }

    public class Tracker(BaseQuestData data, UCPlayer? target, ref State questState, IQuestPreset? preset) : BaseQuestTracker(data, target, questState, preset), INotifyOnKill
    {
        private readonly int _killThreshold = questState.KillThreshold.InsistValue();
        private int _kills;
        protected override bool CompletedCheck => _kills >= _killThreshold;
        public override short FlagValue => (short)_kills;
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
            if (e.Killer!.Steam64 == Player!.Steam64 && e.WasEffectiveKill && e.Cause != EDeathCause.SHRED)
            {
                _kills++;
                if (_kills >= _killThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        public override void ResetToDefaults() => _kills = 0;
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, Player!, _kills, _killThreshold);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}

[QuestData(QuestType.KillFromRange)]
public class KillEnemiesRangeQuest : BaseQuestData<KillEnemiesRangeQuest.Tracker, KillEnemiesRangeQuest.State, KillEnemiesRangeQuest>
{
    public DynamicIntegerValue KillCount;
    public DynamicFloatValue Range;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, ref State state, IQuestPreset? preset) => new Tracker(this, player, ref state, preset);
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
    public struct State : IQuestState<KillEnemiesRangeQuest>
    {
        [RewardField("k")]
        public DynamicIntegerValue.Choice KillThreshold;

        [RewardField("d")]
        public DynamicFloatValue.Choice Range;

        public readonly DynamicIntegerValue.Choice FlagValue => KillThreshold;
        public readonly bool IsEligable(UCPlayer player) => true;
        public void CreateFromTemplate(KillEnemiesRangeQuest data)
        {
            KillThreshold = data.KillCount.GetValue();
            Range = data.Range.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("range", StringComparison.Ordinal))
                Range = DynamicFloatValue.ReadChoice(ref reader);
        }
        public readonly void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("range", Range);
        }
    }
    public class Tracker(BaseQuestData data, UCPlayer? target, ref State questState, IQuestPreset? preset) : BaseQuestTracker(data, target, questState, preset), INotifyOnKill
    {
        private readonly int _killThreshold = questState.KillThreshold.InsistValue();
        private readonly float _range = questState.Range.InsistValue();
        private int _kills;
        protected override bool CompletedCheck => _kills >= _killThreshold;
        public override short FlagValue => (short)_kills;

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
            if (e.Killer!.Steam64 == Player!.Steam64 &&
                e.KillDistance >= _range &&
                e is
                {
                    WasEffectiveKill: true,
                    Cause: EDeathCause.GUN or EDeathCause.MISSILE or EDeathCause.GRENADE or EDeathCause.MELEE or EDeathCause.VEHICLE or EDeathCause.LANDMINE or EDeathCause.CHARGE or EDeathCause.SPLASH
                })
            {
                _kills++;
                if (_kills >= _killThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, Player!, _kills, _killThreshold, _range);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KillEnemiesWithWeapon)]
public class KillEnemiesQuestWeapon : BaseQuestData<KillEnemiesQuestWeapon.Tracker, KillEnemiesQuestWeapon.State, KillEnemiesQuestWeapon>
{
    public DynamicIntegerValue KillCount;
    public DynamicAssetValue<ItemWeaponAsset> Weapon = new DynamicAssetValue<ItemWeaponAsset>(DynamicValueType.Wildcard, ChoiceBehavior.Inclusive);
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, ref State state, IQuestPreset? preset) => new Tracker(this, player, ref state, preset);
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
    public struct State : IQuestState<KillEnemiesQuestWeapon>
    {
        [RewardField("k")]
        public DynamicIntegerValue.Choice KillThreshold;

        public DynamicAssetValue<ItemWeaponAsset>.Choice Weapon;

        public readonly DynamicIntegerValue.Choice FlagValue => KillThreshold;
        public readonly bool IsEligable(UCPlayer player) => true;
        public void CreateFromTemplate(KillEnemiesQuestWeapon data)
        {
            KillThreshold = data.KillCount.GetValue();
            Weapon = data.Weapon.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("weapons", StringComparison.Ordinal))
                Weapon = DynamicAssetValue<ItemWeaponAsset>.ReadChoice(ref reader);
        }
        public readonly void WriteQuestState(Utf8JsonWriter writer)
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
        public Tracker(BaseQuestData data, UCPlayer? target, ref State questState, IQuestPreset? preset) : base(data, target, questState, preset)
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
            if (e.Killer!.Steam64 == Player!.Steam64 && e.PrimaryAsset != default && e.WasEffectiveKill && e.Cause != EDeathCause.SHRED)
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
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, Player!, _kills, _killThreshold, _translationCache1, _translationCache2);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KillFromRangeWithWeapon)]
public class KillEnemiesRangeQuestWeapon : BaseQuestData<KillEnemiesRangeQuestWeapon.Tracker, KillEnemiesRangeQuestWeapon.State, KillEnemiesRangeQuestWeapon>
{
    public DynamicIntegerValue KillCount;
    public DynamicAssetValue<ItemWeaponAsset> Weapon = new DynamicAssetValue<ItemWeaponAsset>(DynamicValueType.Wildcard, ChoiceBehavior.Inclusive);
    public DynamicFloatValue Range;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, ref State state, IQuestPreset? preset) => new Tracker(this, player, ref state, preset);
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
    public struct State : IQuestState<KillEnemiesRangeQuestWeapon>
    {
        [RewardField("k")]
        public DynamicIntegerValue.Choice KillThreshold;

        public DynamicAssetValue<ItemWeaponAsset>.Choice Weapon;

        [RewardField("d")]
        public DynamicFloatValue.Choice Range;

        public readonly DynamicIntegerValue.Choice FlagValue => KillThreshold;
        public readonly bool IsEligable(UCPlayer player) => true;
        public void CreateFromTemplate(KillEnemiesRangeQuestWeapon data)
        {
            KillThreshold = data.KillCount.GetValue();
            Weapon = data.Weapon.GetValue();
            Range = data.Range.GetValue();
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
        public readonly void WriteQuestState(Utf8JsonWriter writer)
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
        public Tracker(BaseQuestData data, UCPlayer? target, ref State questState, IQuestPreset? preset) : base(data, target, questState, preset)
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
            if (e.Killer!.Steam64 == Player!.Steam64 && e.PrimaryAsset != default && e.WasEffectiveKill && e.KillDistance >= _range
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
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, Player!, _kills, _killThreshold, _range.ToString(Data.LocalLocale), _translationCache1, _translationCache2);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KillEnemiesWithKit)]
public class KillEnemiesQuestKit : BaseQuestData<KillEnemiesQuestKit.Tracker, KillEnemiesQuestKit.State, KillEnemiesQuestKit>
{
    public DynamicIntegerValue KillCount;
    public DynamicStringValue Kits = new DynamicStringValue(true, DynamicValueType.Wildcard, ChoiceBehavior.Selective);
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, ref State state, IQuestPreset? preset) => new Tracker(this, player, ref state, preset);
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
    public struct State : IQuestState<KillEnemiesQuestKit>
    {
        [RewardField("k")]
        public DynamicIntegerValue.Choice KillThreshold;

        public DynamicStringValue.Choice Kit;

        public readonly DynamicIntegerValue.Choice FlagValue => KillThreshold;
        public readonly bool IsEligable(UCPlayer player) => true;
        public void CreateFromTemplate(KillEnemiesQuestKit data)
        {
            KillThreshold = data.KillCount.GetValue();
            Kit = data.Kits.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("kit", StringComparison.Ordinal))
                Kit = DynamicStringValue.ReadChoice(ref reader);
        }
        public readonly void WriteQuestState(Utf8JsonWriter writer)
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
        public Tracker(BaseQuestData data, UCPlayer? target, ref State questState, IQuestPreset? preset) : base(data, target, questState, preset)
        {
            _killThreshold = questState.KillThreshold.InsistValue();
            _kit = questState.Kit;
            _translationCache1 = _kit.GetKitNames(Localization.GetDefaultLanguage());
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
            if (e.Killer!.Steam64 == Player!.Steam64 && e.WasEffectiveKill && (e.KillerKitName ?? e.Killer.ActiveKitName) is { } kitId && _kit.IsMatch(kitId) && e.Cause != EDeathCause.SHRED)
            {
                _kills++;
                if (_kills >= _killThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, Player!, _kills, _killThreshold, _translationCache1);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KillFromRangeWithKit)]
public class KillEnemiesQuestKitRange : BaseQuestData<KillEnemiesQuestKitRange.Tracker, KillEnemiesQuestKitRange.State, KillEnemiesQuestKitRange>
{
    public DynamicIntegerValue KillCount;
    public DynamicStringValue Kits;
    public DynamicFloatValue Range;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, ref State state, IQuestPreset? preset) => new Tracker(this, player, ref state, preset);
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
    public struct State : IQuestState<KillEnemiesQuestKitRange>
    {
        [RewardField("k")]
        public DynamicIntegerValue.Choice KillThreshold;

        public DynamicStringValue.Choice Kit;

        [RewardField("d")]
        public DynamicFloatValue.Choice Range;

        public readonly DynamicIntegerValue.Choice FlagValue => KillThreshold;
        public readonly bool IsEligable(UCPlayer player) => true;
        public void CreateFromTemplate(KillEnemiesQuestKitRange data)
        {
            KillThreshold = data.KillCount.GetValue();
            Kit = data.Kits.GetValue();
            Range = data.Range.GetValue();
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
        public readonly void WriteQuestState(Utf8JsonWriter writer)
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
        public Tracker(BaseQuestData data, UCPlayer? target, ref State questState, IQuestPreset? preset) : base(data, target, questState, preset)
        {
            _killThreshold = questState.KillThreshold.InsistValue();
            _kit = questState.Kit;
            _range = questState.Range.InsistValue();
            _translationCache1 = _kit.GetKitNames(Localization.GetDefaultLanguage());
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
            if (e.Killer!.Steam64 == Player!.Steam64 && e.WasEffectiveKill && e.KillDistance >= _range
                && e.Cause is EDeathCause.GUN or EDeathCause.MISSILE or EDeathCause.GRENADE or EDeathCause.MELEE or EDeathCause.VEHICLE or EDeathCause.LANDMINE or EDeathCause.CHARGE or EDeathCause.SPLASH
                && (e.KillerKitName ?? e.Killer.ActiveKitName) is { } kitId && _kit.IsMatch(kitId) && e.Cause != EDeathCause.SHRED)
            {
                _kills++;
                if (_kills >= _killThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, Player!, _kills, _killThreshold, _range.ToString(Data.LocalLocale), _translationCache1);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KillEnemiesWithKitClass)]
public class KillEnemiesQuestKitClass : BaseQuestData<KillEnemiesQuestKitClass.Tracker, KillEnemiesQuestKitClass.State, KillEnemiesQuestKitClass>
{
    public DynamicIntegerValue KillCount;
    public DynamicEnumValue<Class> Class;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, ref State state, IQuestPreset? preset) => new Tracker(this, player, ref state, preset);
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
    public struct State : IQuestState<KillEnemiesQuestKitClass>
    {
        [RewardField("k")]
        public DynamicIntegerValue.Choice KillThreshold;

        public DynamicEnumValue<Class>.Choice Class;

        public readonly DynamicIntegerValue.Choice FlagValue => KillThreshold;
        public readonly bool IsEligable(UCPlayer player) => true;
        public void CreateFromTemplate(KillEnemiesQuestKitClass data)
        {
            KillThreshold = data.KillCount.GetValue();
            Class = data.Class.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("class", StringComparison.Ordinal))
                Class = DynamicEnumValue<Class>.ReadChoice(ref reader);
        }
        public readonly void WriteQuestState(Utf8JsonWriter writer)
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
        public Tracker(BaseQuestData data, UCPlayer? target, ref State questState, IQuestPreset? preset) : base(data, target, questState, preset)
        {
            _killThreshold = questState.KillThreshold.InsistValue();
            _class = questState.Class;
            _translationCache1 = _class.GetCommaList(Localization.GetDefaultLanguage());
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
            if (e.Killer!.Steam64 == Player!.Steam64 && e.WasEffectiveKill && _class.IsMatch(Player!.KitClass))
            {
                _kills++;
                if (_kills >= _killThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, Player!, _kills, _killThreshold, _translationCache1);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KillFromRangeWithClass)]
public class KillEnemiesQuestKitClassRange : BaseQuestData<KillEnemiesQuestKitClassRange.Tracker, KillEnemiesQuestKitClassRange.State, KillEnemiesQuestKitClassRange>
{
    public DynamicIntegerValue KillCount;
    public DynamicEnumValue<Class> Class;
    public DynamicFloatValue Range;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, ref State state, IQuestPreset? preset) => new Tracker(this, player, ref state, preset);
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
    public struct State : IQuestState<KillEnemiesQuestKitClassRange>
    {
        [RewardField("k")]
        public DynamicIntegerValue.Choice KillThreshold;

        public DynamicEnumValue<Class>.Choice Class;

        [RewardField("d")]
        public DynamicFloatValue.Choice Range;

        public readonly DynamicIntegerValue.Choice FlagValue => KillThreshold;
        public readonly bool IsEligable(UCPlayer player) => true;
        public void CreateFromTemplate(KillEnemiesQuestKitClassRange data)
        {
            KillThreshold = data.KillCount.GetValue();
            Class = data.Class.GetValue();
            Range = data.Range.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("class", StringComparison.Ordinal))
                Class = DynamicEnumValue<Class>.ReadChoice(ref reader);
            else if (prop.Equals("range", StringComparison.Ordinal))
                Range = DynamicFloatValue.ReadChoice(ref reader);
        }
        public readonly void WriteQuestState(Utf8JsonWriter writer)
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
        public Tracker(BaseQuestData data, UCPlayer? target, ref State questState, IQuestPreset? preset) : base(data, target, questState, preset)
        {
            _killThreshold = questState.KillThreshold.InsistValue();
            _class = questState.Class;
            _range = questState.Range.InsistValue();
            _translationCache1 = _class.GetCommaList(Localization.GetDefaultLanguage());
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
            if (e.Killer!.Steam64 == Player!.Steam64 && e.WasEffectiveKill && e.KillDistance >= _range
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
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, Player!, _kills, _killThreshold, _range.ToString(Data.LocalLocale), _translationCache1);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KillEnemiesWithWeaponClass)]
public class KillEnemiesQuestWeaponClass : BaseQuestData<KillEnemiesQuestWeaponClass.Tracker, KillEnemiesQuestWeaponClass.State, KillEnemiesQuestWeaponClass>
{
    public DynamicIntegerValue KillCount;
    public DynamicEnumValue<WeaponClass> Class;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, ref State state, IQuestPreset? preset) => new Tracker(this, player, ref state, preset);
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
    public struct State : IQuestState<KillEnemiesQuestWeaponClass>
    {
        [RewardField("k")]
        public DynamicIntegerValue.Choice KillThreshold;

        public DynamicEnumValue<WeaponClass>.Choice Class;

        public readonly DynamicIntegerValue.Choice FlagValue => KillThreshold;
        public readonly bool IsEligable(UCPlayer player) => true;
        public void CreateFromTemplate(KillEnemiesQuestWeaponClass data)
        {
            KillThreshold = data.KillCount.GetValue();
            Class = data.Class.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("class", StringComparison.Ordinal))
                Class = DynamicEnumValue<WeaponClass>.ReadChoice(ref reader);
        }
        public readonly void WriteQuestState(Utf8JsonWriter writer)
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
        public Tracker(BaseQuestData data, UCPlayer? target, ref State questState, IQuestPreset? preset) : base(data, target, questState, preset)
        {
            _killThreshold = questState.KillThreshold.InsistValue();
            _class = questState.Class;
            _translationCache1 = _class.GetCommaList(Localization.GetDefaultLanguage());
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
            if (e.Killer!.Steam64 == Player!.Steam64 && e is { WasEffectiveKill: true, PrimaryAssetIsVehicle: false } && _class.IsMatch(e.PrimaryAsset.GetWeaponClass()) && e.Cause != EDeathCause.SHRED)
            {
                _kills++;
                if (_kills >= _killThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, Player!, _kills, _killThreshold, _translationCache1);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KillEnemiesWithBranch)]
public class KillEnemiesQuestBranch : BaseQuestData<KillEnemiesQuestBranch.Tracker, KillEnemiesQuestBranch.State, KillEnemiesQuestBranch>
{
    public DynamicIntegerValue KillCount;
    public DynamicEnumValue<Branch> Branch;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, ref State state, IQuestPreset? preset) => new Tracker(this, player, ref state, preset);
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
    public struct State : IQuestState<KillEnemiesQuestBranch>
    {
        [RewardField("k")]
        public DynamicIntegerValue.Choice KillThreshold;

        public DynamicEnumValue<Branch>.Choice Branch;

        public readonly DynamicIntegerValue.Choice FlagValue => KillThreshold;
        public readonly bool IsEligable(UCPlayer player) => true;
        public void CreateFromTemplate(KillEnemiesQuestBranch data)
        {
            KillThreshold = data.KillCount.GetValue();
            Branch = data.Branch.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("branch", StringComparison.Ordinal))
                Branch = DynamicEnumValue<Branch>.ReadChoice(ref reader);
        }
        public readonly void WriteQuestState(Utf8JsonWriter writer)
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
        public Tracker(BaseQuestData data, UCPlayer? target, ref State questState, IQuestPreset? preset) : base(data, target, questState, preset)
        {
            _killThreshold = questState.KillThreshold.InsistValue();
            _branch = questState.Branch;
            _translationCache1 = _branch.GetCommaList(Localization.GetDefaultLanguage());
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
            if (e.Killer!.Steam64 == Player!.Steam64 && e.WasEffectiveKill && _branch.IsMatch(Player!.KitBranch) && e.Cause != EDeathCause.SHRED)
            {
                _kills++;
                if (_kills >= _killThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, Player!, _kills, _killThreshold, _translationCache1);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KillEnemiesWithTurret)]
public class KillEnemiesQuestTurret : BaseQuestData<KillEnemiesQuestTurret.Tracker, KillEnemiesQuestTurret.State, KillEnemiesQuestTurret>
{
    public DynamicIntegerValue KillCount;
    public DynamicAssetValue<ItemGunAsset> Turrets;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, ref State state, IQuestPreset? preset) => new Tracker(this, player, ref state, preset);
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
    public struct State : IQuestState<KillEnemiesQuestTurret>
    {
        [RewardField("k")]
        public DynamicIntegerValue.Choice KillThreshold;

        public DynamicAssetValue<ItemGunAsset>.Choice Weapon;

        public readonly DynamicIntegerValue.Choice FlagValue => KillThreshold;
        public readonly bool IsEligable(UCPlayer player) => true;
        public void CreateFromTemplate(KillEnemiesQuestTurret data)
        {
            KillThreshold = data.KillCount.GetValue();
            Weapon = data.Turrets.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("turret", StringComparison.Ordinal))
                Weapon = DynamicAssetValue<ItemGunAsset>.ReadChoice(ref reader);
        }
        public readonly void WriteQuestState(Utf8JsonWriter writer)
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
        public Tracker(BaseQuestData data, UCPlayer? target, ref State questState, IQuestPreset? preset) : base(data, target, questState, preset)
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
            if (e.Killer!.Steam64 == Player!.Steam64 && e.WasEffectiveKill && e.Cause != EDeathCause.SHRED)
            {
                InteractableVehicle? veh = e.Killer.Player.movement.getVehicle();
                if (veh == null) return;
                for (int i = 0; i < veh.turrets.Length; i++)
                {
                    Passenger passenger = veh.turrets[i];
                    if (passenger is { player: not null } && passenger.player.playerID.steamID.m_SteamID == Player!.Steam64 &&
                        passenger.turret != null && _weapon.IsMatch(passenger.turret.itemID))
                    {
                        if (VehicleBay.GetSingletonQuick() is { } manager)
                        {
                            if (manager.GetDataSync(veh.asset.GUID) is { } data && VehicleData.IsEmplacement(data.Type))
                                return;
                        }
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
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, Player!, _kills, _killThreshold, _translationCache1);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KillEnemiesWithEmplacement)]
public class KillEnemiesQuestEmplacement : BaseQuestData<KillEnemiesQuestEmplacement.Tracker, KillEnemiesQuestEmplacement.State, KillEnemiesQuestEmplacement>
{
    public DynamicIntegerValue KillCount;
    public DynamicAssetValue<ItemGunAsset> Turrets;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, ref State state, IQuestPreset? preset) => new Tracker(this, player, ref state, preset);
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
    public struct State : IQuestState<KillEnemiesQuestEmplacement>
    {
        [RewardField("k")]
        public DynamicIntegerValue.Choice KillThreshold;

        public DynamicAssetValue<ItemGunAsset>.Choice Weapon;

        public readonly DynamicIntegerValue.Choice FlagValue => KillThreshold;
        public readonly bool IsEligable(UCPlayer player) => true;
        public void CreateFromTemplate(KillEnemiesQuestEmplacement data)
        {
            KillThreshold = data.KillCount.GetValue();
            Weapon = data.Turrets.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("turret", StringComparison.Ordinal))
                Weapon = DynamicAssetValue<ItemGunAsset>.ReadChoice(ref reader);
        }
        public readonly void WriteQuestState(Utf8JsonWriter writer)
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
        public Tracker(BaseQuestData data, UCPlayer? target, ref State questState, IQuestPreset? preset) : base(data, target, questState, preset)
        {
            _killThreshold = questState.KillThreshold.InsistValue();
            _weapon = questState.Weapon;
            _translationCache1 = _weapon.IsWildcardInclusive() ? "any emplacement" : _weapon.GetCommaList();
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
            if (e.Killer!.Steam64 == Player!.Steam64 && e.WasEffectiveKill && e.Cause != EDeathCause.SHRED)
            {
                InteractableVehicle? veh = e.Killer.Player.movement.getVehicle();
                if (veh == null) return;
                for (int i = 0; i < veh.turrets.Length; i++)
                {
                    Passenger passenger = veh.turrets[i];
                    if (passenger is { player: not null } && passenger.player.playerID.steamID.m_SteamID == Player!.Steam64 &&
                        passenger.turret != null && _weapon.IsMatch(passenger.turret.itemID))
                    {
                        if (VehicleBay.GetSingletonQuick() is not { } manager || manager.GetDataSync(veh.asset.GUID) is not { } data || !VehicleData.IsEmplacement(data.Type))
                            return;
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
        protected override string Translate(bool forAsset)
        {
            if (_weapon is { Behavior: ChoiceBehavior.Inclusive, ValueType: DynamicValueType.Wildcard })
            {
                return QuestData.Translate(forAsset, Player!, _kills, _killThreshold, "any emplacement");
            }

            return QuestData.Translate(forAsset, Player!, _kills, _killThreshold, _translationCache1);
        }

        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KillEnemiesInSquad)]
public class KillEnemiesQuestSquad : BaseQuestData<KillEnemiesQuestSquad.Tracker, KillEnemiesQuestSquad.State, KillEnemiesQuestSquad>
{
    public DynamicIntegerValue KillCount;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, ref State state, IQuestPreset? preset) => new Tracker(this, player, ref state, preset);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
    }
    public struct State : IQuestState<KillEnemiesQuestSquad>
    {
        [RewardField("k")]
        public DynamicIntegerValue.Choice KillThreshold;

        public readonly DynamicIntegerValue.Choice FlagValue => KillThreshold;
        public readonly bool IsEligable(UCPlayer player) => true;
        public void CreateFromTemplate(KillEnemiesQuestSquad data)
        {
            KillThreshold = data.KillCount.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public readonly void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
        }
    }
    public class Tracker(BaseQuestData data, UCPlayer? target, ref State questState, IQuestPreset? preset) : BaseQuestTracker(data, target, questState, preset), INotifyOnKill
    {
        private readonly int _killThreshold = questState.KillThreshold.InsistValue();
        private int _kills;
        protected override bool CompletedCheck => _kills >= _killThreshold;
        public override short FlagValue => (short)_kills;
        public override void ResetToDefaults() => _kills = 0;

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
            if (e.Killer!.Steam64 == Player!.Steam64 && Player!.Squad != null && Player!.Squad.Members.Count > 1 && e.Cause != EDeathCause.SHRED)
            {
                _kills++;
                if (_kills >= _killThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, Player!, _kills, _killThreshold);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KillEnemiesInFullSquad)]
public class KillEnemiesQuestFullSquad : BaseQuestData<KillEnemiesQuestFullSquad.Tracker, KillEnemiesQuestFullSquad.State, KillEnemiesQuestFullSquad>
{
    public DynamicIntegerValue KillCount;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, ref State state, IQuestPreset? preset) => new Tracker(this, player, ref state, preset);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
    }
    public struct State : IQuestState<KillEnemiesQuestFullSquad>
    {
        [RewardField("k")]
        public DynamicIntegerValue.Choice KillThreshold;

        public readonly DynamicIntegerValue.Choice FlagValue => KillThreshold;
        public readonly bool IsEligable(UCPlayer player) => true;
        public void CreateFromTemplate(KillEnemiesQuestFullSquad data)
        {
            KillThreshold = data.KillCount.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public readonly void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
        }
    }
    public class Tracker(BaseQuestData data, UCPlayer? target, ref State questState, IQuestPreset? preset) : BaseQuestTracker(data, target, questState, preset), INotifyOnKill
    {
        private readonly int _killThreshold = questState.KillThreshold.InsistValue();
        private int _kills;
        protected override bool CompletedCheck => _kills >= _killThreshold;
        public override short FlagValue => (short)_kills;
        public override void ResetToDefaults() => _kills = 0;

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
            if (e.Killer!.Steam64 == Player!.Steam64 && e.WasEffectiveKill && Player!.Squad != null && Player!.Squad.IsFull() && e.Cause != EDeathCause.SHRED)
            {
                _kills++;
                if (_kills >= _killThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, Player!, _kills, _killThreshold);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KillEnemiesOnPointDefense)]
public class KillEnemiesQuestDefense : BaseQuestData<KillEnemiesQuestDefense.Tracker, KillEnemiesQuestDefense.State, KillEnemiesQuestDefense>
{
    public DynamicIntegerValue KillCount;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, ref State state, IQuestPreset? preset) => new Tracker(this, player, ref state, preset);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
    }
    public struct State : IQuestState<KillEnemiesQuestDefense>
    {
        [RewardField("k")]
        public DynamicIntegerValue.Choice KillThreshold;

        public readonly DynamicIntegerValue.Choice FlagValue => KillThreshold;
        public readonly bool IsEligable(UCPlayer player) => true;
        public void CreateFromTemplate(KillEnemiesQuestDefense data)
        {
            KillThreshold = data.KillCount.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public readonly void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
        }
    }
    public class Tracker(BaseQuestData data, UCPlayer? target, ref State questState, IQuestPreset? preset) : BaseQuestTracker(data, target, questState, preset), INotifyOnKill
    {
        private readonly int _killThreshold = questState.KillThreshold.InsistValue();
        private int _kills;
        protected override bool CompletedCheck => _kills >= _killThreshold;
        public override short FlagValue => (short)_kills;
        public override void ResetToDefaults() => _kills = 0;

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
            if (e.Killer!.Steam64 == Player!.Steam64 && e.WasEffectiveKill && e.Cause != EDeathCause.SHRED)
            {
                if (Data.Is(out Gamemodes.Interfaces.IFlagTeamObjectiveGamemode fr))
                {
                    Vector3 deadPos = e.Player.Position;
                    Vector3 killerPos = e.Killer.Position;
                    if (Data.Is<Gamemodes.Flags.TeamCTF.TeamCTF>(out _))
                    {
                        if (
                            (team == 1 && fr.ObjectiveTeam1 is { Owner: 1 } && (fr.ObjectiveTeam1.PlayerInRange(killerPos) || fr.ObjectiveTeam1.PlayerInRange(deadPos))) ||
                            (team == 2 && fr.ObjectiveTeam2 is { Owner: 2 } && (fr.ObjectiveTeam2.PlayerInRange(killerPos) || fr.ObjectiveTeam2.PlayerInRange(deadPos))))
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
                                if (cache is { IsActive: true } && (cache.Cache.Position - killerPos).sqrMagnitude > 3600) // 60m
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
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, Player!, _kills, _killThreshold);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KillEnemiesOnPointAttack)]
public class KillEnemiesQuestAttack : BaseQuestData<KillEnemiesQuestAttack.Tracker, KillEnemiesQuestAttack.State, KillEnemiesQuestAttack>
{
    public DynamicIntegerValue KillCount;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, ref State state, IQuestPreset? preset) => new Tracker(this, player, ref state, preset);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
    }
    public struct State : IQuestState<KillEnemiesQuestAttack>
    {
        [RewardField("k")]
        public DynamicIntegerValue.Choice KillThreshold;

        public readonly DynamicIntegerValue.Choice FlagValue => KillThreshold;
        public readonly bool IsEligable(UCPlayer player) => true;
        public void CreateFromTemplate(KillEnemiesQuestAttack data)
        {
            KillThreshold = data.KillCount.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public readonly void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
        }
    }
    public class Tracker(BaseQuestData data, UCPlayer? target, ref State questState, IQuestPreset? preset) : BaseQuestTracker(data, target, questState, preset), INotifyOnKill
    {
        private readonly int _killThreshold = questState.KillThreshold.InsistValue();
        private int _kills;
        protected override bool CompletedCheck => _kills >= _killThreshold;
        public override short FlagValue => (short)_kills;
        public override void ResetToDefaults() => _kills = 0;

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
            if (e.Killer!.Steam64 == Player!.Steam64 && e.WasEffectiveKill && e.Cause != EDeathCause.SHRED)
            {
                Vector3 deadPos = e.Player.Position;
                Vector3 killerPos = e.Killer.Position;
                if (Data.Is(out Gamemodes.Interfaces.IFlagTeamObjectiveGamemode fr))
                {
                    if (Data.Is<Gamemodes.Flags.TeamCTF.TeamCTF>(out _))
                    {
                        if (
                            (team == 1 && fr.ObjectiveTeam1 is { Owner: 2 } && (fr.ObjectiveTeam1.PlayerInRange(killerPos) || fr.ObjectiveTeam1.PlayerInRange(deadPos))) ||
                            (team == 2 && fr.ObjectiveTeam2 is { Owner: 1 } && (fr.ObjectiveTeam2.PlayerInRange(killerPos) || fr.ObjectiveTeam2.PlayerInRange(deadPos))))
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
                                if (cache is { IsActive: true } && (cache.Cache.Position - killerPos).sqrMagnitude > 3600) // 60m
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
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, Player!, _kills, _killThreshold);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KingSlayer)]
public class KingSlayerQuest : BaseQuestData<KingSlayerQuest.Tracker, KingSlayerQuest.State, KingSlayerQuest>
{
    public DynamicIntegerValue KillCount;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, ref State state, IQuestPreset? preset) => new Tracker(this, player, ref state, preset);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
    }
    public struct State : IQuestState<KingSlayerQuest>
    {
        [RewardField("k")]
        public DynamicIntegerValue.Choice KillThreshold;

        public readonly DynamicIntegerValue.Choice FlagValue => KillThreshold;
        public readonly bool IsEligable(UCPlayer player) => true;
        public void CreateFromTemplate(KingSlayerQuest data)
        {
            KillThreshold = data.KillCount.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public readonly void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
        }
    }
    public class Tracker(BaseQuestData data, UCPlayer? target, ref State questState, IQuestPreset? preset) : BaseQuestTracker(data, target, questState, preset), INotifyOnKill
    {
        private readonly int _killThreshold = questState.KillThreshold.InsistValue();
        private int _kills;
        private UCPlayer? _kingSlayer;
        public override short FlagValue => (short)_kills;
        public override void ResetToDefaults() => _kills = 0;
        protected override bool CompletedCheck => _kills >= _killThreshold;

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
            if (e.Killer!.Steam64 == Player!.Steam64 && e.WasEffectiveKill && e.Cause != EDeathCause.SHRED)
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
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, Player!, _kills, _killThreshold);
        public override void ManualComplete()
        {
            _kills = _killThreshold;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.KillStreak)]
public class KillStreakQuest : BaseQuestData<KillStreakQuest.Tracker, KillStreakQuest.State, KillStreakQuest>
{
    public DynamicIntegerValue StreakCount;
    public DynamicIntegerValue StreakLength;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, ref State state, IQuestPreset? preset) => new Tracker(this, player, ref state, preset);
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
    public struct State : IQuestState<KillStreakQuest>
    {
        [RewardField("strNum")]
        public DynamicIntegerValue.Choice StreakCount;

        [RewardField("strLen")]
        public DynamicIntegerValue.Choice StreakLength;

        public readonly DynamicIntegerValue.Choice FlagValue => StreakCount;
        public readonly bool IsEligable(UCPlayer player) => true;
        public void CreateFromTemplate(KillStreakQuest data)
        {
            StreakCount = data.StreakCount.GetValue();
            StreakLength = data.StreakLength.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("num_streaks", StringComparison.Ordinal))
                StreakCount = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("streak_length", StringComparison.Ordinal))
                StreakLength = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public readonly void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("num_streaks", StreakCount);
            writer.WriteProperty("streak_length", StreakLength);
        }
    }
    public class Tracker(BaseQuestData data, UCPlayer? target, ref State questState, IQuestPreset? preset) : BaseQuestTracker(data, target, questState, preset), INotifyOnKill, INotifyOnDeath
    {
        private readonly int _streakCount = questState.StreakCount.InsistValue();
        private readonly int _streakLength = questState.StreakLength.InsistValue();
        private int _streakProgress;
        private int _streaks;
        protected override bool CompletedCheck => _streaks >= _streakCount;
        public override short FlagValue => (short)_streaks;
        public override void ResetToDefaults()
        {
            _streaks = 0;
            _streakProgress = 0;
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
            if (e.Killer!.Steam64 == Player!.Steam64 && e.WasEffectiveKill && e.Cause != EDeathCause.SHRED && !e.WasTeamkill)
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
            if (e.Steam64 == Player!.Steam64)
            {
                _streakProgress = 0;
                TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, Player!, _streakProgress, _streakCount, _streakLength);
        public override void ManualComplete()
        {
            _streakProgress = 0;
            _streakProgress = _streakCount;
            base.ManualComplete();
        }
    }
}