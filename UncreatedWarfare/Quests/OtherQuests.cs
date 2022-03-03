using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Uncreated.Networking;

namespace Uncreated.Warfare.Quests.Types;


[QuestData(EQuestType.DISCORD_KEY_SET_BOOL)]
public class DiscordKeySetQuest : BaseQuestData<DiscordKeySetQuest.Tracker, DiscordKeySetQuest.State, DiscordKeySetQuest>
{
    public DynamicStringValue ItemDisplayName;
    public DynamicStringValue ItemKey;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("item_name", StringComparison.Ordinal))
        {
            if (!reader.TryReadStringValue(out ItemDisplayName, false))
                ItemDisplayName = new DynamicStringValue(false, "INVALID_QUEST_SETUP");
        }
        else if (propertyname.Equals("item_key", StringComparison.Ordinal))
        {
            if (!reader.TryReadStringValue(out ItemKey, false))
                ItemKey = new DynamicStringValue(false, "00000000000000000000000000000000");
        }
    }
    public struct State : IQuestState<Tracker, DiscordKeySetQuest>
    {
        public IDynamicValue<string>.IChoice ItemDisplayName;
        public IDynamicValue<string>.IChoice ItemKey;
        public void Init(DiscordKeySetQuest data)
        {
            this.ItemDisplayName = data.ItemDisplayName.GetValue();
            this.ItemKey = data.ItemKey.GetValue();
        }
        public bool IsEligable(UCPlayer player) => true;

        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("item_name", StringComparison.Ordinal))
                ItemDisplayName = DynamicStringValue.ReadChoice(ref reader);
            else if (prop.Equals("item_key", StringComparison.Ordinal))
                ItemKey = DynamicStringValue.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("item_name", ItemDisplayName);
            writer.WriteProperty("item_key", ItemKey);
        }
    }
    public class Tracker : BaseQuestTracker
    {
        public readonly string ItemName;
        public readonly string ItemKey;
        private bool _hasReceivedKey;
        protected override bool CompletedCheck => _hasReceivedKey;
        public override short FlagValue => (short)(_hasReceivedKey ? 1 : 0);
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            ItemName = questState.ItemDisplayName.InsistValue();
            ItemKey = questState.ItemKey.InsistValue();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if ((reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False) && prop.Equals("has_received_key", StringComparison.Ordinal))
                _hasReceivedKey = reader.GetBoolean();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("has_received_key", _hasReceivedKey);
        }

        public override void ResetToDefaults() => _hasReceivedKey = false;

        private void OnKeyStateReceived(bool state)
        {
            _hasReceivedKey |= state;
            if (_hasReceivedKey)
                TellCompleted();
            else 
                TellUpdated();
        }
        internal static readonly NetCall<ulong, string, bool> SendDiscordKeyState = new NetCall<ulong, string, bool>(1124);
        [NetCall(ENetCall.FROM_SERVER, 1124)]
        internal static void ReceiveDiscordKeyState(IConnection connection, ulong player, string key, bool state)
        {
            if (!string.IsNullOrEmpty(key))
            {
                List<(Guid, ulong)> alreadyUpdated = new List<(Guid, ulong)>(1);
                foreach (Tracker tracker in QuestManager.RegisteredTrackers.OfType<Tracker>())
                {
                    if (tracker.Player.Steam64 == player && key.Equals(tracker.ItemKey, StringComparison.Ordinal))
                    {
                        tracker.OnKeyStateReceived(state);
                        alreadyUpdated.Add((tracker.PresetKey, tracker.QuestData?.Presets.FirstOrDefault(x => x.Key == tracker.PresetKey).Team ?? 0));
                    }
                }
                for (int i = 0; i < QuestManager.Quests.Count; i++)
                {
                    if (QuestManager.Quests[i] is DiscordKeySetQuest quest)
                    {
                        for (int j = 0; j < quest._presets.Length; j++)
                        {
                            ref Preset preset = ref quest._presets[j];
                            for (int k = 0; k < alreadyUpdated.Count; k++)
                            {
                                (Guid, ulong) v = alreadyUpdated[k];
                                if (v.Item1 == preset.Key && v.Item2 == preset.Team) goto next;
                            }
                            if (key.Equals(preset._state.ItemKey.InsistValue(), StringComparison.Ordinal))
                            {
                                State state2 = preset._state;
                                Tracker tracker = new Tracker(null!, ref state2);
                                QuestManager.ReadProgress(player, tracker, preset.Team);
                                tracker._hasReceivedKey = true;
                                QuestManager.SaveProgress(player, tracker, preset.Team);
                            }
                            next: ;
                        }
                    }
                }
            }
        }
        protected override string Translate() => QuestData!.Translate(_player, ItemName);
        public override void ManualComplete()
        {
            _hasReceivedKey = true;
            base.ManualComplete();
        }
    }
}

[QuestData(EQuestType.PLACEHOLDER)]
public class PlaceholderQuest : BaseQuestData<PlaceholderQuest.Tracker, PlaceholderQuest.State, PlaceholderQuest>
{
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader) { }
    public struct State : IQuestState<Tracker, PlaceholderQuest>
    {
        public void Init(PlaceholderQuest data) { }
        public bool IsEligable(UCPlayer player) => true;
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop) { }
        public void WriteQuestState(Utf8JsonWriter writer) { }
    }
    public class Tracker : BaseQuestTracker
    {
        protected override bool CompletedCheck => false;
        public override short FlagValue => 0;
        public Tracker(UCPlayer target, ref State questState) : base(target) { }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader) { }
        public override void WriteQuestProgress(Utf8JsonWriter writer) { }
        public override void ResetToDefaults() { }
        protected override string Translate() => QuestData!.Translate(_player);
    }
}
