using SDG.Unturned;
using System;
using System.Text.Json;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.FOBs;
using Uncreated.Json;
using Uncreated.Framework;

namespace Uncreated.Warfare.Quests.Types;

[QuestData(EQuestType.BUILD_FOBS)]
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
        private readonly int BuildCount = 0;
        private int _fobsBuilt;
        public override short FlagValue => (short)_fobsBuilt;
        public override void ResetToDefaults() => _fobsBuilt = 0;
        protected override bool CompletedCheck => _fobsBuilt >= BuildCount;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            BuildCount = questState.BuildCount.InsistValue();
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
        public void OnFOBBuilt(UCPlayer constructor, Components.FOB fob)
        {
            if (constructor.Steam64 == _player.Steam64)
            {
                _fobsBuilt++;
                if (_fobsBuilt >= BuildCount)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData!.Translate(forAsset, _player, _fobsBuilt, BuildCount);
        public override void ManualComplete()
        {
            _fobsBuilt = BuildCount;
            base.ManualComplete();
        }
    }
}
[QuestData(EQuestType.BUILD_FOBS_NEAR_OBJECTIVES)]
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
        public IDynamicValue<int>.IChoice BuildCount;
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
        private readonly int BuildCount = 0;
        private readonly float SqrBuildRange = 0f;
        private int _fobsBuilt;
        public override short FlagValue => (short)_fobsBuilt;
        public override void ResetToDefaults() => _fobsBuilt = 0;
        protected override bool CompletedCheck => _fobsBuilt >= BuildCount;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            BuildCount = questState.BuildCount.InsistValue();
            SqrBuildRange = questState.BuildRange.InsistValue();
            SqrBuildRange *= SqrBuildRange;
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
                if (Data.Is(out Gamemodes.Flags.TeamCTF.TeamCTF ctf))
                {
                    if ((team == 1 && ctf.ObjectiveTeam1 != null && Util.SqrDistance2D(fob.Position, ctf.ObjectiveTeam1.Position) <= SqrBuildRange) ||
                        (team == 2 && ctf.ObjectiveTeam2 != null && Util.SqrDistance2D(fob.Position, ctf.ObjectiveTeam2.Position) <= SqrBuildRange))
                    {
                        goto add;
                    }
                }
                else if (Data.Is(out Gamemodes.Flags.Invasion.Invasion inv))
                {
                    if ((inv.AttackingTeam == 1 && inv.ObjectiveTeam1 != null && Util.SqrDistance2D(fob.Position, inv.ObjectiveTeam1.Position) <= SqrBuildRange) ||
                        (inv.AttackingTeam == 2 && inv.ObjectiveTeam2 != null && Util.SqrDistance2D(fob.Position, inv.ObjectiveTeam2.Position) <= SqrBuildRange))
                    {
                        goto add;
                    }
                }
                else if (Data.Is(out Gamemodes.Insurgency.Insurgency ins))
                {
                    for (int i = 0; i < ins.Caches.Count; i++)
                    {
                        Gamemodes.Insurgency.Insurgency.CacheData cache = ins.Caches[i];
                        if (cache != null && cache.IsActive && Util.SqrDistance2D(fob.Position, cache.Cache.Position) <= SqrBuildRange)
                            goto add;
                    }
                }
            }
            return;
        add:
            _fobsBuilt++;
            if (_fobsBuilt >= BuildCount)
                TellCompleted();
            else
                TellUpdated();
        }
        protected override string Translate(bool forAsset) => QuestData!.Translate(forAsset, _player, _fobsBuilt, BuildCount);
        public override void ManualComplete()
        {
            _fobsBuilt = BuildCount;
            base.ManualComplete();
        }
    }
}
[QuestData(EQuestType.BUILD_FOB_ON_ACTIVE_OBJECTIVE)]
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
        private readonly int BuildCount = 0;
        private int _fobsBuilt;
        public override short FlagValue => (short)_fobsBuilt;
        public override void ResetToDefaults() => _fobsBuilt = 0;
        protected override bool CompletedCheck => _fobsBuilt >= BuildCount;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            BuildCount = questState.BuildCount.InsistValue();
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
        public void OnFOBBuilt(UCPlayer constructor, Components.FOB fob)
        {
            if (constructor.Steam64 == _player.Steam64)
            {
                ulong team = _player.GetTeam();
                if (Data.Is(out Gamemodes.Flags.TeamCTF.TeamCTF ctf))
                {
                    if ((team == 1 && ctf.ObjectiveTeam1 != null && ctf.ObjectiveTeam1.PlayerInRange(fob.Position)) ||
                        (team == 2 && ctf.ObjectiveTeam2 != null && ctf.ObjectiveTeam2.PlayerInRange(fob.Position)))
                    {
                        goto add;
                    }
                }
                else if (Data.Is(out Gamemodes.Flags.Invasion.Invasion inv))
                {
                    if ((inv.AttackingTeam == 1 && ctf.ObjectiveTeam1 != null && ctf.ObjectiveTeam1.PlayerInRange(fob.Position)) ||
                        (inv.AttackingTeam == 2 && ctf.ObjectiveTeam2 != null && ctf.ObjectiveTeam2.PlayerInRange(fob.Position)))
                    {
                        goto add;
                    }
                }
                else if (Data.Is(out Gamemodes.Insurgency.Insurgency ins))
                {
                    for (int i = 0; i < ins.Caches.Count; i++)
                    {
                        Gamemodes.Insurgency.Insurgency.CacheData cache = ins.Caches[i];
                        if (cache != null && cache.IsActive && Util.SqrDistance2D(fob.Position, cache.Cache.Position) <= 100f)
                            goto add;
                    }
                }
            }
            return;
        add:
            _fobsBuilt++;
            if (_fobsBuilt >= BuildCount)
                TellCompleted();
            else
                TellUpdated();
        }
        protected override string Translate(bool forAsset) => QuestData!.Translate(forAsset, _player, _fobsBuilt, BuildCount);
        public override void ManualComplete()
        {
            _fobsBuilt = BuildCount;
            base.ManualComplete();
        }
    }
}
[QuestData(EQuestType.DELIVER_SUPPLIES)]
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
        private readonly int SupplyCount = 0;
        private int _suppliesDelivered;
        public override short FlagValue => (short)_suppliesDelivered;
        public override void ResetToDefaults() => _suppliesDelivered = 0;
        protected override bool CompletedCheck => _suppliesDelivered >= SupplyCount;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            SupplyCount = questState.SupplyCount.InsistValue();
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
        public void OnSuppliesConsumed(Components.FOB fob, ulong player, int amount)
        {
            if (player == _player.Steam64)
            {
                _suppliesDelivered += amount;
                if (_suppliesDelivered >= SupplyCount)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData!.Translate(forAsset, _player, _suppliesDelivered, SupplyCount);
        public override void ManualComplete()
        {
            _suppliesDelivered = SupplyCount;
            base.ManualComplete();
        }
    }
    public enum ESupplyType : byte { AMMO, BUILD }
}
[QuestData(EQuestType.SHOVEL_BUILDABLES)]
public class HelpBuildQuest : BaseQuestData<HelpBuildQuest.Tracker, HelpBuildQuest.State, HelpBuildQuest>
{
    public DynamicIntegerValue Amount;
    public DynamicAssetValue<ItemBarricadeAsset> BaseIDs = new DynamicAssetValue<ItemBarricadeAsset>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ALL);
    public DynamicEnumValue<EBuildableType> BuildableType = new DynamicEnumValue<EBuildableType>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ONE);
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
                BuildableType = new DynamicEnumValue<EBuildableType>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ONE);
        }
        else if (propertyname.Equals("base_ids", StringComparison.Ordinal))
        {
            if (!reader.TryReadAssetValue(out BaseIDs))
                BaseIDs = new DynamicAssetValue<ItemBarricadeAsset>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ALL);
        }
    }
    public struct State : IQuestState<Tracker, HelpBuildQuest>
    {
        public IDynamicValue<int>.IChoice Amount;
        public DynamicAssetValue<ItemBarricadeAsset>.Choice BaseIDs;
        public IDynamicValue<EBuildableType>.IChoice BuildableType;
        public IDynamicValue<int>.IChoice FlagValue => Amount;
        public void Init(HelpBuildQuest data)
        {
            this.Amount = data.Amount.GetValue();
            this.BaseIDs = data.BaseIDs.GetValue();
            this.BuildableType = data.BuildableType.GetValue();
        }
        public bool IsEligable(UCPlayer player) => true;
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("buildables_required", StringComparison.Ordinal))
                Amount = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("buildable_type", StringComparison.Ordinal))
                BuildableType = DynamicEnumValue<EBuildableType>.ReadChoice(ref reader);
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
        private readonly int Amount = 0;
        private readonly DynamicAssetValue<ItemBarricadeAsset>.Choice BaseIDs;
        private readonly IDynamicValue<EBuildableType>.IChoice BuildableType;
        private int _built;
        protected override bool CompletedCheck => _built >= Amount;
        public override short FlagValue => (short)_built;
        public override void ResetToDefaults() => _built = 0;
        public override int Reward => Amount * 10;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            Amount = questState.Amount.InsistValue();
            BaseIDs = questState.BaseIDs;
            BuildableType = questState.BuildableType;
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
            if (player.Steam64 == _player.Steam64 && BuildableType.IsMatch(buildable.Type) && buildable.Foundation.ValidReference(out Guid guid) && BaseIDs.IsMatch(guid))
            {
                _built++;
                if (_built >= Amount)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData!.Translate(forAsset, _player, _built, Amount, BaseIDs.GetCommaList(), BuildableType);
        public override void ManualComplete()
        {
            _built = Amount;
            base.ManualComplete();
        }
    }
}
[QuestData(EQuestType.TEAMMATES_DEPLOY_ON_FOB)]
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
        private readonly int UseCount = 0;
        private int _fobUses;
        protected override bool CompletedCheck => _fobUses >= UseCount;
        public override short FlagValue => (short)_fobUses;
        public override int Reward => UseCount * 10;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            UseCount = questState.UseCount.InsistValue();
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
        public void OnPlayerSpawnedAtBunker(BuiltBuildableComponent bunker, FOB fob, UCPlayer spawner)
        {
            if (/*spawner.Steam64 != _player.Steam64 && */spawner.GetTeam() == _player.GetTeam()
                && bunker != null &&
                bunker.GetPlayerContribution(_player.Steam64) >= 0.25f)
            {
                _fobUses++;
                if (_fobUses >= UseCount)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData!.Translate(forAsset, _player, _fobUses, UseCount);
        public override void ManualComplete()
        {
            _fobUses = UseCount;
            base.ManualComplete();
        }
    }
}