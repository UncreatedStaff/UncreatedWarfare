using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Uncreated.Warfare.Quests.Types;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Quests;

[QuestData(EQuestType.BUILD_BUILDABLES_ANY)]
public class BuildAnyBuildablesQuest : BaseQuestData<BuildAnyBuildablesQuest.Tracker, BuildAnyBuildablesQuest.State, BuildAnyBuildablesQuest>
{
    public DynamicIntegerValue BuildCount;
    public DynamicEnumValue<FOBs.EBuildableType> BuildableType;

    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("buildables_required", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out BuildCount))
                BuildCount = new DynamicIntegerValue(10);
        }
        else if (propertyname.Equals("buildable_type", StringComparison.Ordinal))
        {
            if (!reader.TryReadEnumValue(out BuildableType))
                BuildableType = new DynamicEnumValue<FOBs.EBuildableType>(FOBs.EBuildableType.FOB_BUNKER);
        }
    }
    public struct State : IQuestState<Tracker, BuildAnyBuildablesQuest>
    {
        public int BuildCount;
        public FOBs.EBuildableType BuildableType;
        public void Init(BuildAnyBuildablesQuest data)
        {
            this.BuildCount = data.BuildCount.GetValue();
            this.BuildableType = data.BuildableType.GetValue();
        }
        public void ReadQuestState(ref Utf8JsonReader reader)
        {
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString();
                if (reader.Read())
                {
                    if (prop.Equals("buildables_required", StringComparison.Ordinal))
                        BuildCount = reader.GetInt32();
                    else if (prop.Equals("buildable_type", StringComparison.Ordinal))
                        reader.TryReadEnumValue(out BuildableType);
                }
            }
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("buildables_required", BuildCount);
            writer.WriteProperty("buildable_type", BuildableType.ToString());
        }
    }
    public class Tracker : BaseQuestTracker, INotifyBuildableBuilt
    {
        private readonly int BuildCount = 0;
        public FOBs.EBuildableType BuildableType;
        private int _buildablesBuilt;
        public override void ResetToDefaults() => _buildablesBuilt = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            BuildCount = questState.BuildCount;
            BuildableType = questState.BuildableType;
        }
        public void OnBuildableBuilt(UCPlayer constructor, FOBs.BuildableData buildable)
        {
            if (buildable.type == BuildableType && constructor.Steam64 == _player.Steam64)
            {
                _buildablesBuilt++;
                if (_buildablesBuilt >= BuildCount)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        public override string Translate() => QuestData.Translate(_player, _buildablesBuilt, BuildCount, BuildableType.ToString());
    }
}
[QuestData(EQuestType.BUILD_BUILDABLES_SPECIFIC)]
public class BuildSpecificBuildablesQuest : BaseQuestData<BuildSpecificBuildablesQuest.Tracker, BuildSpecificBuildablesQuest.State, BuildSpecificBuildablesQuest>
{
    public DynamicIntegerValue BuildCount;
    public DynamicAssetValue<ItemBarricadeAsset> BaseIDs;

    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("buildables_required", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out BuildCount))
                BuildCount = new DynamicIntegerValue(10);
        }
        else if (propertyname.Equals("base_ids", StringComparison.Ordinal))
        {
            if (!reader.TryReadAssetValue(out BaseIDs))
            {
                BaseIDs = new DynamicAssetValue<ItemBarricadeAsset>();
                L.LogWarning("Failed to read asset from " + QuestType);
            }
        }
    }
    public struct State : IQuestState<Tracker, BuildSpecificBuildablesQuest>
    {
        public int BuildCount;
        public DynamicAssetValue<ItemBarricadeAsset> BaseIDs;
        public void Init(BuildSpecificBuildablesQuest data)
        {
            this.BuildCount = data.BuildCount.GetValue();
            this.BaseIDs = data.BaseIDs;
        }
        public void ReadQuestState(ref Utf8JsonReader reader)
        {
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString();
                if (reader.Read())
                {
                    if (prop.Equals("buildables_required", StringComparison.Ordinal))
                        BuildCount = reader.GetInt32();
                    else if (prop.Equals("base_ids", StringComparison.Ordinal))
                        reader.TryReadAssetValue(out BaseIDs);
                }
            }
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("buildables_required", BuildCount);
            writer.WriteProperty("base_ids", BaseIDs.ToString());
        }
    }
    public class Tracker : BaseQuestTracker, INotifyBuildableBuilt
    {
        private readonly int BuildCount = 0;
        public Guid[] BaseIDs;
        private int _buildablesBuilt;
        public override void ResetToDefaults() => _buildablesBuilt = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            BuildCount = questState.BuildCount;
            BaseIDs = questState.BaseIDs.GetSetValue();
        }
        public void OnBuildableBuilt(UCPlayer constructor, FOBs.BuildableData buildable)
        {
            if (constructor.Steam64 == _player.Steam64 && BaseIDs.Contains(buildable.foundationID))
            {
                _buildablesBuilt++;
                if (_buildablesBuilt >= BuildCount)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        public override string Translate() => QuestData.Translate(_player, _buildablesBuilt, BuildCount, string.Join(", ",
            BaseIDs.Select(x => Assets.find(x) is ItemBarricadeAsset asset ? asset.itemName : x.ToString("N"))));
    }
}
[QuestData(EQuestType.BUILD_FOBS)]
public class BuildFOBsQuest : BaseQuestData<BuildFOBsQuest.Tracker, BuildFOBsQuest.State, BuildFOBsQuest>
{
    public DynamicIntegerValue BuildCount;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
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
        public int BuildCount;
        public void Init(BuildFOBsQuest data)
        {
            this.BuildCount = data.BuildCount.GetValue();
        }
        public void ReadQuestState(ref Utf8JsonReader reader)
        {
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString();
                if (reader.Read())
                {
                    if (prop.Equals("fobs_required", StringComparison.Ordinal))
                        BuildCount = reader.GetInt32();
                }
            }
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
        public override void ResetToDefaults() => _fobsBuilt = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            BuildCount = questState.BuildCount;
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
        public override string Translate() => QuestData.Translate(_player, _fobsBuilt, BuildCount);
    }
}
[QuestData(EQuestType.BUILD_FOBS_NEAR_OBJECTIVES)]
public class BuildFOBsNearObjQuest : BaseQuestData<BuildFOBsNearObjQuest.Tracker, BuildFOBsNearObjQuest.State, BuildFOBsNearObjQuest>
{
    public DynamicIntegerValue BuildCount;
    public DynamicFloatValue BuildRange;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
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
        public int BuildCount;
        public float BuildRange;
        public void Init(BuildFOBsNearObjQuest data)
        {
            this.BuildCount = data.BuildCount.GetValue();
            this.BuildRange = data.BuildRange.GetValue();
        }
        public void ReadQuestState(ref Utf8JsonReader reader)
        {
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString();
                if (reader.Read())
                {
                    if (prop.Equals("fobs_required", StringComparison.Ordinal))
                        BuildCount = reader.GetInt32();
                    else if (prop.Equals("buildables_required", StringComparison.Ordinal))
                        BuildRange = reader.GetSingle();
                }
            }
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("fobs_required", BuildCount);
            writer.WriteProperty("buildables_required", BuildRange);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyFOBBuilt
    {
        private readonly int BuildCount = 0;
        private readonly float SqrBuildRange = 0f;
        private int _fobsBuilt;
        public override void ResetToDefaults() => _fobsBuilt = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            BuildCount = questState.BuildCount;
            SqrBuildRange = questState.BuildRange * questState.BuildRange;
        }
        public void OnFOBBuilt(UCPlayer constructor, Components.FOB fob)
        {
            if (constructor.Steam64 == _player.Steam64)
            {
                ulong team = _player.GetTeam();
                if (Data.Is(out Gamemodes.Flags.TeamCTF.TeamCTF ctf))
                {
                    if ((team == 1 && ctf.ObjectiveTeam1 != null && F.SqrDistance2D(fob.Position, ctf.ObjectiveTeam1.Position) <= SqrBuildRange) ||
                        (team == 2 && ctf.ObjectiveTeam2 != null && F.SqrDistance2D(fob.Position, ctf.ObjectiveTeam2.Position) <= SqrBuildRange))
                    {
                        goto add;
                    }
                }
                else if (Data.Is(out Gamemodes.Flags.Invasion.Invasion inv))
                {
                    if ((inv.AttackingTeam == 1 && ctf.ObjectiveTeam1 != null && F.SqrDistance2D(fob.Position, ctf.ObjectiveTeam1.Position) <= SqrBuildRange) ||
                        (inv.AttackingTeam == 2 && ctf.ObjectiveTeam2 != null && F.SqrDistance2D(fob.Position, ctf.ObjectiveTeam2.Position) <= SqrBuildRange))
                    {
                        goto add;
                    }
                }
                else if (Data.Is(out Gamemodes.Insurgency.Insurgency ins))
                {
                    for (int i = 0; i < ins.Caches.Count; i++)
                    {
                        Gamemodes.Insurgency.Insurgency.CacheData cache = ins.Caches[i];
                        if (cache != null && cache.IsActive && F.SqrDistance2D(fob.Position, ctf.ObjectiveTeam1.Position) <= SqrBuildRange)
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
        public override string Translate() => QuestData.Translate(_player, _fobsBuilt, BuildCount);
    }
}
[QuestData(EQuestType.DELIVER_SUPPLIES)]
public class DeliverSuppliesQuest : BaseQuestData<DeliverSuppliesQuest.Tracker, DeliverSuppliesQuest.State, DeliverSuppliesQuest>
{
    public DynamicIntegerValue SupplyCount;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
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
        public int SupplyCount;
        public void Init(DeliverSuppliesQuest data)
        {
            this.SupplyCount = data.SupplyCount.GetValue();
        }
        public void ReadQuestState(ref Utf8JsonReader reader)
        {
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString();
                if (reader.Read())
                {
                    if (prop.Equals("supply_count", StringComparison.Ordinal))
                        SupplyCount = reader.GetInt32();
                }
            }
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
        public override void ResetToDefaults() => _suppliesDelivered = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            SupplyCount = questState.SupplyCount;
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
        public override string Translate() => QuestData.Translate(_player, _suppliesDelivered, SupplyCount);
    }
    public enum ESupplyType : byte { AMMO, BUILD }
}
[QuestData(EQuestType.ENTRENCHING_TOOL_HITS)]
public class UseEntrenchingToolQuest : BaseQuestData<UseEntrenchingToolQuest.Tracker, UseEntrenchingToolQuest.State, UseEntrenchingToolQuest>
{
    public DynamicIntegerValue HitCount;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("successful_hits", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out HitCount))
                HitCount = new DynamicIntegerValue(250);
        }
    }
    public struct State : IQuestState<Tracker, UseEntrenchingToolQuest>
    {
        public int HitCount;
        public void Init(UseEntrenchingToolQuest data)
        {
            this.HitCount = data.HitCount.GetValue();
        }
        public void ReadQuestState(ref Utf8JsonReader reader)
        {
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString();
                if (reader.Read())
                {
                    if (prop.Equals("successful_hits", StringComparison.Ordinal))
                        HitCount = reader.GetInt32();
                }
            }
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("successful_hits", HitCount);
        }
    }
    public class Tracker : BaseQuestTracker, INotifySuppliesConsumed
    {
        private readonly int SupplyCount = 0;
        private int _suppliesDelivered;
        public override void ResetToDefaults() => _suppliesDelivered = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            SupplyCount = questState.HitCount;
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
        public override string Translate() => QuestData.Translate(_player, _suppliesDelivered, SupplyCount);
    }
}