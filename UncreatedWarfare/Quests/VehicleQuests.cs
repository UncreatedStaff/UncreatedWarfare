using System;
using System.Text.Json;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Quests.Types;

[QuestData(QuestType.DestroyVehicles)]
public class DestroyVehiclesQuest : BaseQuestData<DestroyVehiclesQuest.Tracker, DestroyVehiclesQuest.State, DestroyVehiclesQuest>
{
    public DynamicIntegerValue VehicleCount;
    public DynamicEnumValue<VehicleType> VehicleType = new DynamicEnumValue<VehicleType>(DynamicValueType.Wildcard, ChoiceBehavior.Selective);
    public DynamicAssetValue<VehicleAsset> VehicleIDs = new DynamicAssetValue<VehicleAsset>(DynamicValueType.Wildcard, ChoiceBehavior.Inclusive);
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, ref State state, IQuestPreset? preset) => new Tracker(this, player, ref state, preset);
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
                VehicleType = new DynamicEnumValue<VehicleType>(DynamicValueType.Wildcard, ChoiceBehavior.Selective);
        }
        else if (propertyname.Equals("vehicle_count", StringComparison.Ordinal))
        {
            if (!reader.TryReadAssetValue(out VehicleIDs))
                VehicleIDs = new DynamicAssetValue<VehicleAsset>(DynamicValueType.Wildcard, ChoiceBehavior.Inclusive);
        }
    }
    public struct State : IQuestState<DestroyVehiclesQuest>
    {
        [RewardField("a")]
        public DynamicIntegerValue.Choice VehicleCount;

        public DynamicEnumValue<VehicleType>.Choice VehicleType;

        public DynamicAssetValue<VehicleAsset>.Choice VehicleIDs;

        public readonly DynamicIntegerValue.Choice FlagValue => VehicleCount;
        public readonly bool IsEligable(UCPlayer player) => true;
        public void CreateFromTemplate(DestroyVehiclesQuest data)
        {
            VehicleCount = data.VehicleCount.GetValue();
            VehicleType = data.VehicleType.GetValue();
            VehicleIDs = data.VehicleIDs.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("vehicle_count", StringComparison.Ordinal))
                VehicleCount = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("vehicle_type", StringComparison.Ordinal))
                VehicleType = DynamicEnumValue<VehicleType>.ReadChoice(ref reader);
            else if (prop.Equals("vehicle_ids", StringComparison.Ordinal))
                VehicleIDs = DynamicAssetValue<VehicleAsset>.ReadChoice(ref reader);
        }
        public readonly void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("vehicle_count", VehicleCount);
            writer.WriteProperty("vehicle_type", VehicleType);
            writer.WriteProperty("vehicle_ids", VehicleIDs);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyVehicleDestroyed
    {
        private readonly int _vehicleCount;
        private readonly DynamicEnumValue<VehicleType>.Choice _vehicleType;
        private readonly DynamicAssetValue<VehicleAsset>.Choice _vehicleIDs;
        private readonly string _translationCache1;
        private readonly string _translationCache2;
        private int _vehDest;
        protected override bool CompletedCheck => _vehDest >= _vehicleCount;
        public override short FlagValue => (short)_vehDest;
        public Tracker(BaseQuestData data, UCPlayer? target, ref State questState, IQuestPreset? preset) : base(data, target, questState, preset)
        {
            _vehicleCount = questState.VehicleCount.InsistValue();
            _vehicleType = questState.VehicleType;
            _vehicleIDs = questState.VehicleIDs;
            _translationCache1 = _vehicleType.GetCommaList(Localization.GetDefaultLanguage());
            _translationCache2 = _vehicleIDs.GetCommaList();
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
        public void OnVehicleDestroyed(VehicleDestroyed e, UCPlayer instigator)
        {
            if (e.VehicleData != null && e.Team != Player!.GetTeam() && _vehicleType.IsMatch(e.VehicleData.Type) && _vehicleIDs.IsMatch(e.Vehicle.asset))
            {
                if (instigator != null && instigator.Steam64 == Player!.Steam64)
                {
                    _vehDest++;
                    if (_vehDest >= _vehicleCount)
                        TellCompleted();
                    else
                        TellUpdated();
                    return;
                }

                if (e.Assists == null)
                    return;

                for (int i = 0; i < e.Assists.Length; ++i)
                {
                    if (e.Assists[i].Key != Player!.Steam64)
                        continue;

                    if (e.Assists[i].Value < 0.4f)
                        break;
                    
                    _vehDest++;
                    if (_vehDest >= _vehicleCount)
                        TellCompleted();
                    else
                        TellUpdated();
                    return;
                }
            }
        }
        protected override string Translate(bool forAsset)
        {
            if (_vehicleIDs is { Behavior: ChoiceBehavior.Inclusive, ValueType: DynamicValueType.Wildcard })
            {
                if (_vehicleType is { Behavior: ChoiceBehavior.Inclusive, ValueType: DynamicValueType.Wildcard })
                    return QuestData.Translate(forAsset, Player!, _vehDest, _vehicleCount, "vehicles");
                return QuestData.Translate(forAsset, Player!, _vehDest, _vehicleCount, _translationCache1);
            }

            return QuestData.Translate(forAsset, Player!, _vehDest, _vehicleCount, _translationCache2);
        }
        public override void ManualComplete()
        {
            _vehDest = _vehicleCount;
            base.ManualComplete();
        }
    }
}

[QuestData(QuestType.DriveDistance)]
public class DriveDistanceQuest : BaseQuestData<DriveDistanceQuest.Tracker, DriveDistanceQuest.State, DriveDistanceQuest>
{
    public DynamicIntegerValue Amount;
    public DynamicAssetValue<VehicleAsset> Vehicles = new DynamicAssetValue<VehicleAsset>(DynamicValueType.Wildcard, ChoiceBehavior.Inclusive);
    public DynamicEnumValue<VehicleType> VehicleType = new DynamicEnumValue<VehicleType>(DynamicValueType.Wildcard, ChoiceBehavior.Selective);
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, ref State state, IQuestPreset? preset) => new Tracker(this, player, ref state, preset);
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
                VehicleType = new DynamicEnumValue<VehicleType>(DynamicValueType.Wildcard, ChoiceBehavior.Selective);
        }
        else if (propertyname.Equals("vehicle_ids", StringComparison.Ordinal))
        {
            if (!reader.TryReadAssetValue(out Vehicles))
                Vehicles = new DynamicAssetValue<VehicleAsset>(DynamicValueType.Wildcard, ChoiceBehavior.Inclusive);
        }
    }
    public struct State : IQuestState<DriveDistanceQuest>
    {
        [RewardField("d")]
        public DynamicIntegerValue.Choice Amount;
        internal DynamicEnumValue<VehicleType>.Choice VehicleTypes;
        public DynamicAssetValue<VehicleAsset>.Choice Vehicles;
        public readonly DynamicIntegerValue.Choice FlagValue => Amount;
        public readonly bool IsEligable(UCPlayer player) => true;
        public void CreateFromTemplate(DriveDistanceQuest data)
        {
            Amount = data.Amount.GetValue();
            Vehicles = data.Vehicles.GetValue();
            VehicleTypes = data.VehicleType.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("distance", StringComparison.Ordinal))
                Amount = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("vehicle_type", StringComparison.Ordinal))
                VehicleTypes = DynamicEnumValue<VehicleType>.ReadChoice(ref reader);
            else if (prop.Equals("vehicle_ids", StringComparison.Ordinal))
                Vehicles = DynamicAssetValue<VehicleAsset>.ReadChoice(ref reader);
        }
        public readonly void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("distance", Amount);
            writer.WriteProperty("vehicle_type", VehicleTypes);
            writer.WriteProperty("vehicle_ids", Vehicles);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyVehicleDistanceUpdates
    {
        private readonly float _distance;
        private readonly DynamicAssetValue<VehicleAsset>.Choice _vehicles;
        private readonly DynamicEnumValue<VehicleType>.Choice _vehicleType;
        private readonly string _translationCache1;
        private readonly string _translationCache2;
        private float _travelled;
        protected override bool CompletedCheck => _travelled >= _distance;
        public override short FlagValue => (short)_travelled;
        public override void ResetToDefaults() => _travelled = 0;
        public Tracker(BaseQuestData data, UCPlayer? target, ref State questState, IQuestPreset? preset) : base(data, target, questState, preset)
        {
            _distance = questState.Amount.InsistValue();
            _vehicles = questState.Vehicles;
            _vehicleType = questState.VehicleTypes;
            _translationCache1 = _vehicleType.GetCommaList(Localization.GetDefaultLanguage());
            _translationCache2 = _vehicles.GetCommaList();
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

        private uint _lastInstID;
        private VehicleType _lastType;
        public void OnDistanceUpdated(ulong lastDriver, float totalDistance, float newDistance, Components.VehicleComponent vehicle)
        {
            if (Player!.Steam64 == lastDriver && _vehicles.IsMatch(vehicle.Vehicle.asset.GUID))
            {
                if (!(_vehicleType.ValueType != DynamicValueType.Wildcard && _vehicleType.Behavior != ChoiceBehavior.Inclusive) && _lastInstID != vehicle.Vehicle.instanceID)
                {
                    _lastInstID = vehicle.Vehicle.instanceID;
                    VehicleData? data = VehicleBay.GetSingletonQuick()?.GetDataSync(vehicle.Vehicle.asset.GUID);
                    _lastType = data?.Type ?? Warfare.Vehicles.VehicleType.None;
                }

                if (!_vehicleType.IsMatch(_lastType))
                    return;

                _travelled += newDistance;
                if (_travelled >= _distance)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset)
        {
            if (_vehicles is { Behavior: ChoiceBehavior.Inclusive, ValueType: DynamicValueType.Wildcard })
            {
                if (_vehicleType is { Behavior: ChoiceBehavior.Inclusive, ValueType: DynamicValueType.Wildcard })
                    return QuestData.Translate(forAsset, Player!, Mathf.RoundToInt(_travelled), _distance, "any vehicle");
                return QuestData.Translate(forAsset, Player!, Mathf.RoundToInt(_travelled), _distance, _translationCache1);
            }
            
            return QuestData.Translate(forAsset, Player!, Mathf.RoundToInt(_travelled), _distance, _translationCache2);
        }
        public override void ManualComplete()
        {
            _travelled = _distance;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.TransportPlayers)]
public class TransportPlayersQuest : BaseQuestData<TransportPlayersQuest.Tracker, TransportPlayersQuest.State, TransportPlayersQuest>
{
    public DynamicIntegerValue Amount;
    public DynamicAssetValue<VehicleAsset> Vehicles = new DynamicAssetValue<VehicleAsset>(DynamicValueType.Wildcard, ChoiceBehavior.Inclusive);
    public DynamicEnumValue<VehicleType> VehicleType = new DynamicEnumValue<VehicleType>(DynamicValueType.Wildcard, ChoiceBehavior.Selective);
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, ref State state, IQuestPreset? preset) => new Tracker(this, player, ref state, preset);
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
                VehicleType = new DynamicEnumValue<VehicleType>(DynamicValueType.Wildcard, ChoiceBehavior.Selective);
        }
        else if (propertyname.Equals("vehicle_ids", StringComparison.Ordinal))
        {
            if (!reader.TryReadAssetValue(out Vehicles))
                Vehicles = new DynamicAssetValue<VehicleAsset>(DynamicValueType.Wildcard, ChoiceBehavior.Inclusive);
        }
    }
    public struct State : IQuestState<TransportPlayersQuest>
    {
        [RewardField("a")]
        public DynamicIntegerValue.Choice Amount;
        public DynamicAssetValue<VehicleAsset>.Choice Vehicles;
        public IDynamicValue<VehicleType>.IChoice VehicleTypes;
        public readonly DynamicIntegerValue.Choice FlagValue => Amount;
        public readonly bool IsEligable(UCPlayer player) => true;
        public void CreateFromTemplate(TransportPlayersQuest data)
        {
            Amount = data.Amount.GetValue();
            Vehicles = data.Vehicles.GetValue();
            VehicleTypes = data.VehicleType.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("distance", StringComparison.Ordinal))
                Amount = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("vehicle_type", StringComparison.Ordinal))
                VehicleTypes = DynamicEnumValue<VehicleType>.ReadChoice(ref reader);
            else if (prop.Equals("vehicle_ids", StringComparison.Ordinal))
                Vehicles = DynamicAssetValue<VehicleAsset>.ReadChoice(ref reader);
        }
        public readonly void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("distance", Amount);
            writer.WriteProperty("vehicle_type", VehicleTypes);
            writer.WriteProperty("vehicle_ids", Vehicles);
        }
    }
    public class Tracker(BaseQuestData data, UCPlayer? target, ref State questState, IQuestPreset? preset) : BaseQuestTracker(data, target, questState, preset), INotifyVehicleDistanceUpdates
    {
        private readonly float _distance = questState.Amount.InsistValue();
        private readonly DynamicAssetValue<VehicleAsset>.Choice _vehicles = questState.Vehicles;
        private readonly IDynamicValue<VehicleType>.IChoice _vehicleType = questState.VehicleTypes;
        private float _travelled;
        protected override bool CompletedCheck => _travelled >= _distance;
        public override short FlagValue => (short)_travelled;
        public override void ResetToDefaults() => _travelled = 0;

        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("distance_travelled", StringComparison.Ordinal))
                _travelled = reader.GetSingle();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("distance_travelled", _travelled);
        }

        private uint _lastInstID;
        private VehicleType _lastType;
        public void OnDistanceUpdated(ulong lastDriver, float totalDistance, float newDistance, Components.VehicleComponent vehicle)
        {
            if (Player!.Steam64 == lastDriver && _vehicles.IsMatch(vehicle.Vehicle.asset.GUID))
            {
                if (!(_vehicleType.ValueType != DynamicValueType.Wildcard && _vehicleType.Behavior != ChoiceBehavior.Inclusive) && _lastInstID != vehicle.Vehicle.instanceID)
                {
                    _lastInstID = vehicle.Vehicle.instanceID;
                    VehicleData? vehicleData = VehicleBay.GetSingletonQuick()?.GetDataSync(vehicle.Vehicle.asset.GUID);
                    _lastType = vehicleData?.Type ?? Warfare.Vehicles.VehicleType.None;
                }

                if (_vehicleType.IsMatch(_lastType))
                {
                    _travelled += newDistance;
                    if (_travelled >= _distance)
                        TellCompleted();
                    else
                        TellUpdated();
                }
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, Player!, Mathf.RoundToInt(_travelled), _distance, _vehicles.GetCommaList());
        public override void ManualComplete()
        {
            _travelled = _distance;
            base.ManualComplete();
        }
    }
}