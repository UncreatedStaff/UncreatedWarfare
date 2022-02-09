using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Quests.Types;


[QuestData(EQuestType.CAPTURE_OBJECTIVES)]
public class CaptureObjectivesQuest : BaseQuestData<CaptureObjectivesQuest.Tracker, CaptureObjectivesQuest.State, CaptureObjectivesQuest>
{
    public DynamicIntegerValue ObjectiveCount;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("objective_count", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out ObjectiveCount))
                ObjectiveCount = new DynamicIntegerValue(10);
        }
    }
    public struct State : IQuestState<Tracker, CaptureObjectivesQuest>
    {
        public IDynamicValue<int>.IChoice ObjectiveCount;
        public void Init(CaptureObjectivesQuest data)
        {
            this.ObjectiveCount = data.ObjectiveCount.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("objective_count", StringComparison.Ordinal))
                ObjectiveCount = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("objective_count", ObjectiveCount);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnObjectiveCaptured
    {
        private readonly int ObjectiveCount = 0;
        private int _captures;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            ObjectiveCount = questState.ObjectiveCount.InsistValue();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("objectives_captured", StringComparison.Ordinal))
                _captures = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("objectives_captured", _captures);
        }
        public override void ResetToDefaults() => _captures = 0;
        public void OnObjectiveCaptured(ulong[] participants)
        {
            for (int i = 0; i < participants.Length; i++)
            {
                if (participants[i] == _player.Steam64)
                {
                    _captures++;
                    if (_captures >= ObjectiveCount)
                        TellCompleted();
                    else
                        TellUpdated();
                    return;
                }
            }
        }
        public override string Translate() => QuestData.Translate(_player, _captures, ObjectiveCount);
    }
}
