using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Quests.Types;

[QuestData(EQuestType.REVIVE_PLAYERS)]
public class RevivePlayersQuest : BaseQuestData<RevivePlayersQuest.Tracker, RevivePlayersQuest.State, RevivePlayersQuest>
{
    public DynamicIntegerValue ReviveCount;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("revives_required", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out ReviveCount))
                ReviveCount = new DynamicIntegerValue(10);
        }
    }
    public struct State : IQuestState<Tracker, RevivePlayersQuest>
    {
        public bool IsEligable(UCPlayer player) => true;
        public IDynamicValue<int>.IChoice ReviveCount;
        public void Init(RevivePlayersQuest data)
        {
            this.ReviveCount = data.ReviveCount.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("revives_required", StringComparison.Ordinal))
                ReviveCount = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("revives_required", ReviveCount);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnRevive
    {
        private readonly int ReviveCount = 0;
        private int _revives;
        public override short FlagValue => (short)_revives;
        protected override bool CompletedCheck => _revives >= ReviveCount;

        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            ReviveCount = questState.ReviveCount.InsistValue();
        }
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
            if (reviver.Steam64 == _player.Steam64)
            {
                _revives++;
                if (_revives >= ReviveCount)
                    TellCompleted();
                else
                    TellUpdated();
                return;
            }
        }

        public override string Translate() => QuestData.Translate(_player, _revives, ReviveCount);
    }
}
