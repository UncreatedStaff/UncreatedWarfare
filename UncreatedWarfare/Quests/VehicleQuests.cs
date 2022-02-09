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
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
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
        public void OnVehicleDestroyed(UCPlayer owner, UCPlayer destroyer, IEnumerable<KeyValuePair<ulong, float>> assisters, EVehicleType type)
        {
            if (destroyer.Steam64 == _player.Steam64 && VehicleType.IsMatch(type)) // todo add assists?
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