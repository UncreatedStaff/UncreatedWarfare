using SDG.Unturned;
using System;
using System.Text.Json;
using Uncreated.Json;
using Uncreated.Warfare.Events.Vehicles;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Quests.Types;

[QuestData(EQuestType.DESTROY_VEHICLES)]
public class DestroyVehiclesQuest : BaseQuestData<DestroyVehiclesQuest.Tracker, DestroyVehiclesQuest.State, DestroyVehiclesQuest>
{
    public DynamicIntegerValue VehicleCount;
    public DynamicEnumValue<EVehicleType> VehicleType = new DynamicEnumValue<EVehicleType>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ONE);
    public DynamicAssetValue<VehicleAsset> VehicleIDs = new DynamicAssetValue<VehicleAsset>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ALL);
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("vehicle_count", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out VehicleCount))
                VehicleCount = new DynamicIntegerValue(10);
        }
        else if (propertyname.Equals("vehicle_type", StringComparison.Ordinal))
        {
            if (!reader.TryReadEnumValue(out VehicleType))
                VehicleType = new DynamicEnumValue<EVehicleType>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ONE);
        }
        else if (propertyname.Equals("vehicle_count", StringComparison.Ordinal))
        {
            if (!reader.TryReadAssetValue(out VehicleIDs))
                VehicleIDs = new DynamicAssetValue<VehicleAsset>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ALL);
        }
    }
    public struct State : IQuestState<Tracker, DestroyVehiclesQuest>
    {
        public IDynamicValue<int>.IChoice VehicleCount;
        internal DynamicEnumValue<EVehicleType>.Choice VehicleType;
        public DynamicAssetValue<VehicleAsset>.Choice VehicleIDs;
        public IDynamicValue<int>.IChoice FlagValue => VehicleCount;
        public bool IsEligable(UCPlayer player) => true;
        public void Init(DestroyVehiclesQuest data)
        {
            this.VehicleCount = data.VehicleCount.GetValue();
            this.VehicleType = data.VehicleType.GetValueIntl();
            this.VehicleIDs = data.VehicleIDs.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("vehicle_count", StringComparison.Ordinal))
                VehicleCount = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("vehicle_type", StringComparison.Ordinal))
                VehicleType = DynamicEnumValue<EVehicleType>.ReadChoiceIntl(ref reader);
            else if (prop.Equals("vehicle_ids", StringComparison.Ordinal))
                VehicleIDs = DynamicAssetValue<VehicleAsset>.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("vehicle_count", VehicleCount);
            writer.WriteProperty("vehicle_type", VehicleType);
            writer.WriteProperty("vehicle_ids", VehicleIDs);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyVehicleDestroyed
    {
        private readonly int VehicleCount;
        internal readonly DynamicEnumValue<EVehicleType>.Choice VehicleType;
        private readonly DynamicAssetValue<VehicleAsset>.Choice VehicleIDs;
        private readonly string translationCache1;
        private readonly string translationCache2;
        private int _vehDest;
        protected override bool CompletedCheck => _vehDest >= VehicleCount;
        public override short FlagValue => (short)_vehDest;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            VehicleCount = questState.VehicleCount.InsistValue();
            VehicleType = questState.VehicleType;
            VehicleIDs = questState.VehicleIDs;
            translationCache1 = VehicleType.GetCommaList(_player == null ? 0 : _player.Steam64);
            translationCache2 = VehicleIDs.GetCommaList();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("vehicles_destroyed", StringComparison.Ordinal))
                _vehDest = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("vehicles_destroyed", _vehDest);
        }
        public override void ResetToDefaults() => _vehDest = 0;
        public void OnVehicleDestroyed(VehicleDestroyed e)
        {
            if (e.VehicleData != null && e.Team != _player.GetTeam() && VehicleType.IsMatch(e.VehicleData.Type) && VehicleIDs.IsMatch(e.Vehicle.asset))
            {
                if (e.Instigator != null && e.Instigator.Steam64 == _player.Steam64)
                {
                    _vehDest++;
                    if (_vehDest >= VehicleCount)
                        TellCompleted();
                    else
                        TellUpdated();
                    return;
                }
                if (e.Assists != null)
                {
                    for (int i = 0; i < e.Assists.Length; ++i)
                    {
                        if (e.Assists[i].Key == _player.Steam64)
                        {
                            if (e.Assists[i].Value >= 0.4f)
                            {
                                _vehDest++;
                                if (_vehDest >= VehicleCount)
                                    TellCompleted();
                                else
                                    TellUpdated();
                                return;
                            }

                            break;
                        }
                    }
                }
            }
        }
        protected override string Translate(bool forAsset)
        {
            if (VehicleIDs.Behavior == EChoiceBehavior.ALLOW_ALL && VehicleIDs.ValueType == EDynamicValueType.ANY)
                return QuestData!.Translate(forAsset, _player, _vehDest, VehicleCount, translationCache1);
            else
                return QuestData!.Translate(forAsset, _player, _vehDest, VehicleCount, translationCache2);
        }
        public override void ManualComplete()
        {
            _vehDest = VehicleCount;
            base.ManualComplete();
        }
    }
}

[QuestData(EQuestType.DRIVE_DISTANCE)]
public class DriveDistanceQuest : BaseQuestData<DriveDistanceQuest.Tracker, DriveDistanceQuest.State, DriveDistanceQuest>
{
    public DynamicIntegerValue Amount;
    public DynamicAssetValue<VehicleAsset> Vehicles = new DynamicAssetValue<VehicleAsset>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ALL);
    public DynamicEnumValue<EVehicleType> VehicleType = new DynamicEnumValue<EVehicleType>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ONE);
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("distance", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out Amount))
                Amount = new DynamicIntegerValue(1000);
        }
        else if (propertyname.Equals("vehicle_type", StringComparison.Ordinal))
        {
            if (!reader.TryReadEnumValue(out VehicleType))
                VehicleType = new DynamicEnumValue<EVehicleType>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ONE);
        }
        else if (propertyname.Equals("vehicle_ids", StringComparison.Ordinal))
        {
            if (!reader.TryReadAssetValue(out Vehicles))
                Vehicles = new DynamicAssetValue<VehicleAsset>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ALL);
        }
    }
    public struct State : IQuestState<Tracker, DriveDistanceQuest>
    {
        public IDynamicValue<int>.IChoice Amount;
        internal DynamicEnumValue<EVehicleType>.Choice VehicleTypes;
        public DynamicAssetValue<VehicleAsset>.Choice Vehicles;
        public IDynamicValue<int>.IChoice FlagValue => Amount;
        public bool IsEligable(UCPlayer player) => true;
        public void Init(DriveDistanceQuest data)
        {
            this.Amount = data.Amount.GetValue();
            this.Vehicles = data.Vehicles.GetValue();
            this.VehicleTypes = data.VehicleType.GetValueIntl();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("distance", StringComparison.Ordinal))
                Amount = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("vehicle_type", StringComparison.Ordinal))
                VehicleTypes = DynamicEnumValue<EVehicleType>.ReadChoiceIntl(ref reader);
            else if (prop.Equals("vehicle_ids", StringComparison.Ordinal))
                Vehicles = DynamicAssetValue<VehicleAsset>.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("distance", Amount);
            writer.WriteProperty("vehicle_type", VehicleTypes);
            writer.WriteProperty("vehicle_ids", Vehicles);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyVehicleDistanceUpdates
    {
        private readonly float Distance = 0;
        private readonly DynamicAssetValue<VehicleAsset>.Choice Vehicles;
        private readonly DynamicEnumValue<EVehicleType>.Choice VehicleType;
        private readonly string translationCache1;
        private readonly string translationCache2;
        private float _travelled;
        protected override bool CompletedCheck => _travelled >= Distance;
        public override short FlagValue => (short)_travelled;
        public override void ResetToDefaults() => _travelled = 0;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            Distance = questState.Amount.InsistValue();
            Vehicles = questState.Vehicles;
            VehicleType = questState.VehicleTypes;
            translationCache1 = VehicleType.GetCommaList(_player == null ? 0 : _player.Steam64);
            translationCache2 = Vehicles.GetCommaList();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("distance_travelled", StringComparison.Ordinal))
                _travelled = reader.GetSingle();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("distance_travelled", _travelled);
        }

        private uint lastInstID = 0;
        private EVehicleType lastType;
        public void OnDistanceUpdated(ulong lastDriver, float totalDistance, float newDistance, Components.VehicleComponent vehicle)
        {
            if (_player.Steam64 == lastDriver && Vehicles.IsMatch(vehicle.Vehicle.asset.GUID))
            {
                if (!(VehicleType.ValueType != EDynamicValueType.ANY && VehicleType.Behavior != EChoiceBehavior.ALLOW_ALL) && lastInstID != vehicle.Vehicle.instanceID)
                {
                    lastInstID = vehicle.Vehicle.instanceID;
                    VehicleData? data = VehicleBay.GetSingletonQuick()?.GetDataSync(vehicle.Vehicle.asset.GUID);
                    if (data != null)
                        lastType = data.Type;
                    else
                        lastType = EVehicleType.NONE;
                }
                if (VehicleType.IsMatch(lastType))
                {
                    _travelled += newDistance;
                    if (_travelled >= Distance)
                        TellCompleted();
                    else
                        TellUpdated();
                }
            }
        }
        protected override string Translate(bool forAsset)
        {
            if (Vehicles.Behavior == EChoiceBehavior.ALLOW_ALL && Vehicles.ValueType == EDynamicValueType.ANY)
                return QuestData!.Translate(forAsset, _player, _travelled, Distance, translationCache1);
            else
                return QuestData!.Translate(forAsset, _player, _travelled, Distance, translationCache2);
        }
        public override void ManualComplete()
        {
            _travelled = Distance;
            base.ManualComplete();
        }
    }
}
[QuestData(EQuestType.TRANSPORT_PLAYERS)]
public class TransportPlayersQuest : BaseQuestData<TransportPlayersQuest.Tracker, TransportPlayersQuest.State, TransportPlayersQuest>
{
    public DynamicIntegerValue Amount;
    public DynamicAssetValue<VehicleAsset> Vehicles = new DynamicAssetValue<VehicleAsset>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ALL);
    public DynamicEnumValue<EVehicleType> VehicleType = new DynamicEnumValue<EVehicleType>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ONE);
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("distance", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out Amount))
                Amount = new DynamicIntegerValue(1000);
        }
        else if (propertyname.Equals("vehicle_type", StringComparison.Ordinal))
        {
            if (!reader.TryReadEnumValue(out VehicleType))
                VehicleType = new DynamicEnumValue<EVehicleType>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ONE);
        }
        else if (propertyname.Equals("vehicle_ids", StringComparison.Ordinal))
        {
            if (!reader.TryReadAssetValue(out Vehicles))
                Vehicles = new DynamicAssetValue<VehicleAsset>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ALL);
        }
    }
    public struct State : IQuestState<Tracker, TransportPlayersQuest>
    {
        public IDynamicValue<int>.IChoice Amount;
        public DynamicAssetValue<VehicleAsset>.Choice Vehicles;
        public IDynamicValue<EVehicleType>.IChoice VehicleTypes;
        public IDynamicValue<int>.IChoice FlagValue => Amount;
        public bool IsEligable(UCPlayer player) => true;
        public void Init(TransportPlayersQuest data)
        {
            this.Amount = data.Amount.GetValue();
            this.Vehicles = data.Vehicles.GetValue();
            this.VehicleTypes = data.VehicleType.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("distance", StringComparison.Ordinal))
                Amount = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("vehicle_type", StringComparison.Ordinal))
                VehicleTypes = DynamicEnumValue<EVehicleType>.ReadChoice(ref reader);
            else if (prop.Equals("vehicle_ids", StringComparison.Ordinal))
                Vehicles = DynamicAssetValue<VehicleAsset>.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("distance", Amount);
            writer.WriteProperty("vehicle_type", VehicleTypes);
            writer.WriteProperty("vehicle_ids", Vehicles);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyVehicleDistanceUpdates
    {
        private readonly float Distance = 0;
        private readonly DynamicAssetValue<VehicleAsset>.Choice Vehicles;
        private readonly IDynamicValue<EVehicleType>.IChoice VehicleType;
        private float _travelled;
        protected override bool CompletedCheck => _travelled >= Distance;
        public override short FlagValue => (short)_travelled;
        public override void ResetToDefaults() => _travelled = 0;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            Distance = questState.Amount.InsistValue();
            Vehicles = questState.Vehicles;
            VehicleType = questState.VehicleTypes;
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("distance_travelled", StringComparison.Ordinal))
                _travelled = reader.GetSingle();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("distance_travelled", _travelled);
        }

        private uint lastInstID;
        private EVehicleType lastType;
        public void OnDistanceUpdated(ulong lastDriver, float totalDistance, float newDistance, Components.VehicleComponent vehicle)
        {
            if (_player.Steam64 == lastDriver && Vehicles.IsMatch(vehicle.Vehicle.asset.GUID))
            {
                if (!(VehicleType.ValueType != EDynamicValueType.ANY && VehicleType.Behavior != EChoiceBehavior.ALLOW_ALL) && lastInstID != vehicle.Vehicle.instanceID)
                {
                    lastInstID = vehicle.Vehicle.instanceID;
                    VehicleData? data = VehicleBay.GetSingletonQuick()?.GetDataSync(vehicle.Vehicle.asset.GUID);
                    if (data != null)
                        lastType = data.Type;
                    else
                        lastType = EVehicleType.NONE;
                }
                if (VehicleType.IsMatch(lastType))
                {
                    _travelled += newDistance;
                    if (_travelled >= Distance)
                        TellCompleted();
                    else
                        TellUpdated();
                }
            }
        }
        protected override string Translate(bool forAsset) => QuestData!.Translate(forAsset, _player, _travelled, Distance, Vehicles.GetCommaList());
        public override void ManualComplete()
        {
            _travelled = Distance;
            base.ManualComplete();
        }
    }
}