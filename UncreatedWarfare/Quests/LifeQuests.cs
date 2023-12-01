using System;
using System.Text.Json;
using Uncreated.Json;

namespace Uncreated.Warfare.Quests.Types;

[QuestData(QuestType.RevivePlayers)]
public class RevivePlayersQuest : BaseQuestData<RevivePlayersQuest.Tracker, RevivePlayersQuest.State, RevivePlayersQuest>
{
    public DynamicIntegerValue ReviveCount;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, ref State state, IQuestPreset? preset) => new Tracker(this, player, ref state, preset);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("revives_required", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out ReviveCount))
                ReviveCount = new DynamicIntegerValue(10);
        }
    }
    public struct State : IQuestState<RevivePlayersQuest>
    {
        [RewardField("a")]
        public DynamicIntegerValue.Choice ReviveCount;

        public readonly DynamicIntegerValue.Choice FlagValue => ReviveCount;
        public readonly bool IsEligable(UCPlayer player) => true;
        public void Init(RevivePlayersQuest data)
        {
            ReviveCount = data.ReviveCount.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("revives_required", StringComparison.Ordinal))
                ReviveCount = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public readonly void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("revives_required", ReviveCount);
        }
    }
    public class Tracker(BaseQuestData data, UCPlayer? target, ref State questState, IQuestPreset? preset) : BaseQuestTracker(data, target, questState, preset), INotifyOnRevive
    {
        private readonly int _reviveCount = questState.ReviveCount.InsistValue();
        private int _revives;
        public override short FlagValue => (short)_revives;
        protected override bool CompletedCheck => _revives >= _reviveCount;

        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("revives", StringComparison.Ordinal))
                _revives = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("revives", _revives);
        }
        public override void ResetToDefaults() => _revives = 0;
        public void OnPlayerRevived(UCPlayer reviver, UCPlayer revived)
        {
            if (reviver.Steam64 == Player!.Steam64)
            {
                _revives++;
                if (_revives >= _reviveCount)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }

        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, Player!, _revives, _reviveCount);
        public override void ManualComplete()
        {
            _revives = _reviveCount;
            base.ManualComplete();
        }
    }
}
