using SDG.Unturned;
using System;
using System.Text.Json;
using Uncreated.Framework;
using Uncreated.Json;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Interfaces;

namespace Uncreated.Warfare.Quests.Types;

[QuestData(QuestType.BuildFOBs)]
public class BuildFOBsQuest : BaseQuestData<BuildFOBsQuest.Tracker, BuildFOBsQuest.State, BuildFOBsQuest>
{
    public DynamicIntegerValue BuildCount;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("fobs_required", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out BuildCount))
                BuildCount = new DynamicIntegerValue(10);
        }
    }
    public struct State : IQuestState<Tracker, BuildFOBsQuest>
    {
        [RewardField("a")]
        public IDynamicValue<int>.IChoice BuildCount;
        public IDynamicValue<int>.IChoice FlagValue => BuildCount;
        public void Init(BuildFOBsQuest data)
        {
            this.BuildCount = data.BuildCount.GetValue();
        }
        public bool IsEligable(UCPlayer player) => true;

        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("fobs_required", StringComparison.Ordinal))
                BuildCount = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("fobs_required", BuildCount);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyFOBBuilt
    {
        private readonly int _buildCount;
        private int _fobsBuilt;
        public override short FlagValue => (short)_fobsBuilt;
        public override void ResetToDefaults() => _fobsBuilt = 0;
        protected override bool CompletedCheck => _fobsBuilt >= _buildCount;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            _buildCount = questState.BuildCount.InsistValue();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("fobs_built", StringComparison.Ordinal))
                _fobsBuilt = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("fobs_built", _fobsBuilt);
        }
        public void OnFOBBuilt(UCPlayer constructor, FOB fob)
        {
            if (constructor.Steam64 == _player.Steam64)
            {
                _fobsBuilt++;
                if (_fobsBuilt >= _buildCount)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, _player, _fobsBuilt, _buildCount);
        public override void ManualComplete()
        {
            _fobsBuilt = _buildCount;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.BuildFOBsNearObjectives)]
public class BuildFOBsNearObjQuest : BaseQuestData<BuildFOBsNearObjQuest.Tracker, BuildFOBsNearObjQuest.State, BuildFOBsNearObjQuest>
{
    public DynamicIntegerValue BuildCount;
    public DynamicFloatValue BuildRange;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("fobs_required", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out BuildCount))
                BuildCount = new DynamicIntegerValue(10);
        }
        else if (propertyname.Equals("buildables_required", StringComparison.Ordinal))
        {
            if (!reader.TryReadFloatValue(out BuildRange))
                BuildRange = new DynamicFloatValue(200f);
        }
    }
    public struct State : IQuestState<Tracker, BuildFOBsNearObjQuest>
    {
        [RewardField("a")]
        public IDynamicValue<int>.IChoice BuildCount;
        [RewardField("d")]
        public IDynamicValue<float>.IChoice BuildRange;
        public IDynamicValue<int>.IChoice FlagValue => BuildCount;
        public void Init(BuildFOBsNearObjQuest data)
        {
            this.BuildCount = data.BuildCount.GetValue();
            this.BuildRange = data.BuildRange.GetValue();
        }
        public bool IsEligable(UCPlayer player) => true;
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("fobs_required", StringComparison.Ordinal))
                BuildCount = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("objective_range", StringComparison.Ordinal))
                BuildRange = DynamicFloatValue.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("fobs_required", BuildCount);
            writer.WriteProperty("objective_range", BuildRange);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyFOBBuilt
    {
        private readonly int _buildCount;
        private readonly float _sqrBuildRange;
        private int _fobsBuilt;
        public override short FlagValue => (short)_fobsBuilt;
        public override void ResetToDefaults() => _fobsBuilt = 0;
        protected override bool CompletedCheck => _fobsBuilt >= _buildCount;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            _buildCount = questState.BuildCount.InsistValue();
            _sqrBuildRange = questState.BuildRange.InsistValue();
            _sqrBuildRange *= _sqrBuildRange;
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("fobs_built", StringComparison.Ordinal))
                _fobsBuilt = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("fobs_built", _fobsBuilt);
        }
        public void OnFOBBuilt(UCPlayer constructor, FOB fob)
        {
            if (constructor.Steam64 == _player.Steam64)
            {
                ulong team = _player.GetTeam();
                if (Data.Is(out IFlagTeamObjectiveGamemode ctf))
                {
                    if (Data.Is(out IAttackDefense inv))
                    {
                        if ((inv.AttackingTeam == 1 && ctf.ObjectiveTeam1 != null && Util.SqrDistance2D(fob.transform.position, ctf.ObjectiveTeam1.Position) <= _sqrBuildRange) ||
                            (inv.AttackingTeam == 2 && ctf.ObjectiveTeam2 != null && Util.SqrDistance2D(fob.transform.position, ctf.ObjectiveTeam2.Position) <= _sqrBuildRange))
                        {
                            goto add;
                        }
                    }
                    else
                    {
                        if ((team == 1 && ctf.ObjectiveTeam1 != null && Util.SqrDistance2D(fob.transform.position, ctf.ObjectiveTeam1.Position) <= _sqrBuildRange) ||
                            (team == 2 && ctf.ObjectiveTeam2 != null && Util.SqrDistance2D(fob.transform.position, ctf.ObjectiveTeam2.Position) <= _sqrBuildRange))
                        {
                            goto add;
                        }
                    }
                }
                else if (Data.Is(out Gamemodes.Insurgency.Insurgency ins))
                {
                    for (int i = 0; i < ins.Caches.Count; i++)
                    {
                        Gamemodes.Insurgency.Insurgency.CacheData cache = ins.Caches[i];
                        if (cache != null && cache.IsActive && Util.SqrDistance2D(fob.transform.position, cache.Cache.Position) <= _sqrBuildRange)
                            goto add;
                    }
                }
            }
            return;
        add:
            _fobsBuilt++;
            if (_fobsBuilt >= _buildCount)
                TellCompleted();
            else
                TellUpdated();
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, _player, _fobsBuilt, _buildCount);
        public override void ManualComplete()
        {
            _fobsBuilt = _buildCount;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.BuildFOBOnActiveObjective)]
public class BuildFOBsOnObjQuest : BaseQuestData<BuildFOBsOnObjQuest.Tracker, BuildFOBsOnObjQuest.State, BuildFOBsOnObjQuest>
{
    public DynamicIntegerValue BuildCount;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("fobs_required", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out BuildCount))
                BuildCount = new DynamicIntegerValue(10);
        }
    }
    public struct State : IQuestState<Tracker, BuildFOBsOnObjQuest>
    {
        [RewardField("a")]
        public IDynamicValue<int>.IChoice BuildCount;
        public IDynamicValue<int>.IChoice FlagValue => BuildCount;
        public void Init(BuildFOBsOnObjQuest data)
        {
            this.BuildCount = data.BuildCount.GetValue();
        }
        public bool IsEligable(UCPlayer player) => true;
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("fobs_required", StringComparison.Ordinal))
                BuildCount = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("fobs_required", BuildCount);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyFOBBuilt
    {
        private readonly int _buildCount;
        private int _fobsBuilt;
        public override short FlagValue => (short)_fobsBuilt;
        public override void ResetToDefaults() => _fobsBuilt = 0;
        protected override bool CompletedCheck => _fobsBuilt >= _buildCount;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            _buildCount = questState.BuildCount.InsistValue();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("fobs_built", StringComparison.Ordinal))
                _fobsBuilt = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("fobs_built", _fobsBuilt);
        }
        public void OnFOBBuilt(UCPlayer constructor, FOB fob)
        {
            if (constructor.Steam64 == _player.Steam64)
            {
                ulong team = _player.GetTeam();
                if (Data.Is(out IFlagTeamObjectiveGamemode ctf))
                {
                    if (Data.Is(out IAttackDefense inv))
                    {
                        if ((inv.AttackingTeam == 1 && ctf.ObjectiveTeam1 != null && ctf.ObjectiveTeam1.PlayerInRange(fob.transform.position)) ||
                            (inv.AttackingTeam == 2 && ctf.ObjectiveTeam2 != null && ctf.ObjectiveTeam2.PlayerInRange(fob.transform.position)))
                        {
                            goto add;
                        }
                    }
                    else
                    {
                        if ((team == 1 && ctf.ObjectiveTeam1 != null && ctf.ObjectiveTeam1.PlayerInRange(fob.transform.position)) ||
                            (team == 2 && ctf.ObjectiveTeam2 != null && ctf.ObjectiveTeam2.PlayerInRange(fob.transform.position)))
                        {
                            goto add;
                        }
                    }
                }
                else if (Data.Is(out Gamemodes.Insurgency.Insurgency ins))
                {
                    for (int i = 0; i < ins.Caches.Count; i++)
                    {
                        Gamemodes.Insurgency.Insurgency.CacheData cache = ins.Caches[i];
                        if (cache != null && cache.IsActive && Util.SqrDistance2D(fob.transform.position, cache.Cache.Position) <= 100f)
                            goto add;
                    }
                }
            }
            return;
        add:
            _fobsBuilt++;
            if (_fobsBuilt >= _buildCount)
                TellCompleted();
            else
                TellUpdated();
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, _player, _fobsBuilt, _buildCount);
        public override void ManualComplete()
        {
            _fobsBuilt = _buildCount;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.DeliverSupplies)]
public class DeliverSuppliesQuest : BaseQuestData<DeliverSuppliesQuest.Tracker, DeliverSuppliesQuest.State, DeliverSuppliesQuest>
{
    public DynamicIntegerValue SupplyCount;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("supply_count", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out SupplyCount))
                SupplyCount = new DynamicIntegerValue(10);
        }
    }
    public struct State : IQuestState<Tracker, DeliverSuppliesQuest>
    {
        [RewardField("a")]
        public IDynamicValue<int>.IChoice SupplyCount;
        public IDynamicValue<int>.IChoice FlagValue => SupplyCount;
        public void Init(DeliverSuppliesQuest data)
        {
            this.SupplyCount = data.SupplyCount.GetValue();
        }
        public bool IsEligable(UCPlayer player) => true;
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("supply_count", StringComparison.Ordinal))
                SupplyCount = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("supply_count", SupplyCount);
        }
    }
    public class Tracker : BaseQuestTracker, INotifySuppliesConsumed
    {
        private readonly int _supplyCount;
        private int _suppliesDelivered;
        public override short FlagValue => (short)_suppliesDelivered;
        public override void ResetToDefaults() => _suppliesDelivered = 0;
        protected override bool CompletedCheck => _suppliesDelivered >= _supplyCount;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            _supplyCount = questState.SupplyCount.InsistValue();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("supplies_delivered", StringComparison.Ordinal))
                _suppliesDelivered = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("supplies_delivered", _suppliesDelivered);
        }
        public void OnSuppliesConsumed(FOB fob, ulong player, int amount)
        {
            if (player == _player.Steam64)
            {
                _suppliesDelivered += amount;
                if (_suppliesDelivered >= _supplyCount)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, _player, _suppliesDelivered, _supplyCount);
        public override void ManualComplete()
        {
            _suppliesDelivered = _supplyCount;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.ShovelBuildables)]
public class HelpBuildQuest : BaseQuestData<HelpBuildQuest.Tracker, HelpBuildQuest.State, HelpBuildQuest>
{
    public DynamicIntegerValue Amount;
    public DynamicAssetValue<ItemBarricadeAsset> BaseIDs = new DynamicAssetValue<ItemBarricadeAsset>(DynamicValueType.Wildcard, ChoiceBehavior.Inclusive);
    public DynamicEnumValue<BuildableType> BuildableType = new DynamicEnumValue<BuildableType>(DynamicValueType.Wildcard, ChoiceBehavior.Selective);
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("buildables_required", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out Amount))
                Amount = new DynamicIntegerValue(250);
        }
        else if (propertyname.Equals("buildable_type", StringComparison.Ordinal))
        {
            if (!reader.TryReadEnumValue(out BuildableType))
                BuildableType = new DynamicEnumValue<BuildableType>(DynamicValueType.Wildcard, ChoiceBehavior.Selective);
        }
        else if (propertyname.Equals("base_ids", StringComparison.Ordinal))
        {
            if (!reader.TryReadAssetValue(out BaseIDs))
                BaseIDs = new DynamicAssetValue<ItemBarricadeAsset>(DynamicValueType.Wildcard, ChoiceBehavior.Inclusive);
        }
    }
    public struct State : IQuestState<Tracker, HelpBuildQuest>
    {
        [RewardField("a")]
        public IDynamicValue<int>.IChoice Amount;
        public DynamicAssetValue<ItemBarricadeAsset>.Choice BaseIDs;
        internal DynamicEnumValue<BuildableType>.Choice BuildableType;
        public IDynamicValue<int>.IChoice FlagValue => Amount;
        public void Init(HelpBuildQuest data)
        {
            this.Amount = data.Amount.GetValue();
            this.BaseIDs = data.BaseIDs.GetValue();
            this.BuildableType = data.BuildableType.GetValueIntl();
        }
        public bool IsEligable(UCPlayer player) => true;
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("buildables_required", StringComparison.Ordinal))
                Amount = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("buildable_type", StringComparison.Ordinal))
                BuildableType = DynamicEnumValue<BuildableType>.ReadChoiceIntl(ref reader);
            else if (prop.Equals("base_ids", StringComparison.Ordinal))
                BaseIDs = DynamicAssetValue<ItemBarricadeAsset>.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("buildables_required", Amount);
            writer.WriteProperty("buildable_type", BaseIDs);
            writer.WriteProperty("base_ids", BuildableType);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyBuildableBuilt
    {
        private readonly int _amount;
        private readonly DynamicAssetValue<ItemBarricadeAsset>.Choice _baseIds;
        private readonly DynamicEnumValue<BuildableType>.Choice _buildableType;
        private int _built;
        private readonly string _translationCache1;
        private readonly string _translationCache2;
        protected override bool CompletedCheck => _built >= _amount;
        public override short FlagValue => (short)_built;
        public override void ResetToDefaults() => _built = 0;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            _amount = questState.Amount.InsistValue();
            _baseIds = questState.BaseIDs;
            _buildableType = questState.BuildableType;
            _translationCache1 = _buildableType.GetCommaList(_player == null ? 0 : _player.Steam64);
            _translationCache2 = _baseIds.GetCommaList();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("buildables_built", StringComparison.Ordinal))
                _built = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("buildables_built", _built);
        }
        // TODO redo this
        [Obsolete("redo this function plz")]
        public void OnBuildableBuilt(UCPlayer player, BuildableData buildable)
        {
            if (player.Steam64 == _player.Steam64 && _buildableType.IsMatch(buildable.Type) && buildable.Foundation.ValidReference(out Guid guid) && _baseIds.IsMatch(guid))
            {
                _built++;
                if (_built >= _amount)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset)
        {
            if (_baseIds.Behavior == ChoiceBehavior.Inclusive && _baseIds.ValueType == DynamicValueType.Wildcard)
            {
                if (_buildableType.Behavior == ChoiceBehavior.Inclusive && _buildableType.ValueType == DynamicValueType.Wildcard)
                    return QuestData.Translate(forAsset, _player, _built, _amount, "buildables");

                return QuestData.Translate(forAsset, _player, _built, _amount, _translationCache1);
            }

            return QuestData.Translate(forAsset, _player, _built, _amount, _translationCache2);
        }

        public override void ManualComplete()
        {
            _built = _amount;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.TeammatesDeployOnFOB)]
public class FOBUseQuest : BaseQuestData<FOBUseQuest.Tracker, FOBUseQuest.State, FOBUseQuest>
{
    public DynamicIntegerValue UseCount;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("deployments", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out UseCount))
                UseCount = new DynamicIntegerValue(10);
        }
    }
    public struct State : IQuestState<Tracker, FOBUseQuest>
    {
        [RewardField("a")]
        public IDynamicValue<int>.IChoice UseCount;
        public IDynamicValue<int>.IChoice FlagValue => UseCount;
        public void Init(FOBUseQuest data)
        {
            this.UseCount = data.UseCount.GetValue();
        }
        public bool IsEligable(UCPlayer player) => true;

        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("deployments", StringComparison.Ordinal))
                UseCount = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("deployments", UseCount);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyBunkerSpawn
    {
        private readonly int _useCount;
        private int _fobUses;
        protected override bool CompletedCheck => _fobUses >= _useCount;
        public override short FlagValue => (short)_fobUses;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            _useCount = questState.UseCount.InsistValue();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("deployments", StringComparison.Ordinal))
                _fobUses = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("deployments", _fobUses);
        }
        public override void ResetToDefaults() => _fobUses = 0;
        public void OnPlayerSpawnedAtBunker(BunkerComponent component, UCPlayer spawner)
        {
            if (spawner.Steam64 != _player.Steam64 && spawner.GetTeam() == _player.GetTeam()
                && (component.Owner == _player.Steam64 || component.Builders[_player.Steam64] >= 0.25f))
            {
                _fobUses++;
                if (_fobUses >= _useCount)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, _player, _fobUses, _useCount);
        public override void ManualComplete()
        {
            _fobUses = _useCount;
            base.ManualComplete();
        }
    }
}