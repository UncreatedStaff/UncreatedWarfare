using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Quests.Types;

[QuestData(EQuestType.DESTROY_VEHICLES)]
public class DestroyVehiclesQuest : BaseQuestData<DestroyVehiclesQuest.Tracker, DestroyVehiclesQuest.State, DestroyVehiclesQuest>
{
    public DynamicIntegerValue VehicleCount;
    public DynamicEnumValue<EVehicleType> VehicleType;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
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
    }
    public struct State : IQuestState<Tracker, DestroyVehiclesQuest>
    {
        public IDynamicValue<int>.IChoice VehicleCount;
        public IDynamicValue<EVehicleType>.IChoice VehicleType;
        public void Init(DestroyVehiclesQuest data)
        {
            this.VehicleCount = data.VehicleCount.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("vehicle_count", StringComparison.Ordinal))
                VehicleCount = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("vehicle_type", StringComparison.Ordinal))
                VehicleType = DynamicEnumValue<EVehicleType>.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("vehicle_count", VehicleCount);
            writer.WriteProperty("vehicle_type", VehicleType);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyVehicleDestroyed
    {
        private readonly int VehicleCount = 0;
        public IDynamicValue<EVehicleType>.IChoice VehicleType;
        private int _vehDest;
        public override short FlagValue => (short)_vehDest;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            VehicleCount = questState.VehicleCount.InsistValue();
            VehicleType = questState.VehicleType;
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
        public void OnVehicleDestroyed(UCPlayer owner, UCPlayer destroyer, VehicleData data, Components.VehicleComponent component)
        {
            if (destroyer.Steam64 == _player.Steam64 && VehicleType.IsMatch(data.Type))
            {
                _vehDest++;
                if (_vehDest >= VehicleCount)
                    TellCompleted();
                else
                    TellUpdated();
                return;
            }
        }
        public override string Translate() => QuestData.Translate(_player, _vehDest, VehicleCount, VehicleType.ToString());
    }
}

[QuestData(EQuestType.DRIVE_DISTANCE)]
public class DriveDistanceQuest : BaseQuestData<DriveDistanceQuest.Tracker, DriveDistanceQuest.State, DriveDistanceQuest>
{
    public DynamicFloatValue Amount;
    public DynamicAssetValue<VehicleAsset> Vehicles = new DynamicAssetValue<VehicleAsset>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ALL);
    public DynamicEnumValue<EVehicleType> VehicleType = new DynamicEnumValue<EVehicleType>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ONE);
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("distance", StringComparison.Ordinal))
        {
            if (!reader.TryReadFloatValue(out Amount))
                Amount = new DynamicFloatValue(1000f);
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
        public IDynamicValue<float>.IChoice Amount;
        public DynamicAssetValue<VehicleAsset>.Choice Vehicles;
        public IDynamicValue<EVehicleType>.IChoice VehicleTypes;
        public void Init(DriveDistanceQuest data)
        {
            this.Amount = data.Amount.GetValue();
            this.Vehicles = data.Vehicles.GetValue();
            this.VehicleTypes = data.VehicleType.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("distance", StringComparison.Ordinal))
                Amount = DynamicFloatValue.ReadChoice(ref reader);
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
        public override short FlagValue => (short)_travelled;
        public override void ResetToDefaults() => _travelled = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
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

        private uint lastInstID = 0;
        private EVehicleType lastType;
        public void OnDistanceUpdated(ulong lastDriver, float totalDistance, float newDistance, Components.VehicleComponent vehicle)
        {
            if (_player.Steam64 == lastDriver && Vehicles.IsMatch(vehicle.Vehicle.asset.GUID))
            {
                if (!(VehicleType.ValueType != EDynamicValueType.ANY && VehicleType.Behavior != EChoiceBehavior.ALLOW_ALL) && lastInstID != vehicle.Vehicle.instanceID)
                {
                    lastInstID = vehicle.Vehicle.instanceID;
                    if (VehicleBay.VehicleExists(vehicle.Vehicle.asset.GUID, out VehicleData data))
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
        public override string Translate() => QuestData.Translate(_player, _travelled, Distance, Vehicles.GetCommaList());
    }
}
[QuestData(EQuestType.TRANSPORT_PLAYERS)]
public class TransportPlayersQuest : BaseQuestData<TransportPlayersQuest.Tracker, TransportPlayersQuest.State, TransportPlayersQuest>
{
    public DynamicFloatValue Amount;
    public DynamicAssetValue<VehicleAsset> Vehicles = new DynamicAssetValue<VehicleAsset>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ALL);
    public DynamicEnumValue<EVehicleType> VehicleType = new DynamicEnumValue<EVehicleType>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ONE);
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("distance", StringComparison.Ordinal))
        {
            if (!reader.TryReadFloatValue(out Amount))
                Amount = new DynamicFloatValue(1000f);
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
        public IDynamicValue<float>.IChoice Amount;
        public DynamicAssetValue<VehicleAsset>.Choice Vehicles;
        public IDynamicValue<EVehicleType>.IChoice VehicleTypes;
        public void Init(TransportPlayersQuest data)
        {
            this.Amount = data.Amount.GetValue();
            this.Vehicles = data.Vehicles.GetValue();
            this.VehicleTypes = data.VehicleType.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("distance", StringComparison.Ordinal))
                Amount = DynamicFloatValue.ReadChoice(ref reader);
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
        public override short FlagValue => (short)_travelled;
        public override void ResetToDefaults() => _travelled = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
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

        private uint lastInstID = 0;
        private EVehicleType lastType;
        public void OnDistanceUpdated(ulong lastDriver, float totalDistance, float newDistance, Components.VehicleComponent vehicle)
        {
            if (_player.Steam64 == lastDriver && Vehicles.IsMatch(vehicle.Vehicle.asset.GUID))
            {
                if (!(VehicleType.ValueType != EDynamicValueType.ANY && VehicleType.Behavior != EChoiceBehavior.ALLOW_ALL) && lastInstID != vehicle.Vehicle.instanceID)
                {
                    lastInstID = vehicle.Vehicle.instanceID;
                    if (VehicleBay.VehicleExists(vehicle.Vehicle.asset.GUID, out VehicleData data))
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
        public override string Translate() => QuestData.Translate(_player, _travelled, Distance, Vehicles.GetCommaList());
    }
}