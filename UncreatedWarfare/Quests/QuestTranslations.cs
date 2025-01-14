using System;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;

namespace Uncreated.Warfare.Quests;

public sealed class QuestTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Quests";

    [TranslationData("Sent as a warning before daily quests regenerate. Usually one is sent 1 hour before and 10 minutes before they regenerate.")]
    public readonly Translation<TimeSpan> DailyQuestNextDayWarning = new Translation<TimeSpan>("<#99bacc>Daily Missions will expire in <#eef4f6>{0}</color>.", arg0Fmt: TimeAddon.Create(TimeSpanFormatType.Long));

    [TranslationData("Sent when new daily quests are generated.")]
    public readonly Translation DailyQuestNextDay = new Translation("<#99bacc>Daily Missions have been regenerated.");

    [TranslationData("Sent when a quest is updated, example the player got a kill.")]
    public readonly Translation<string> QuestUpdated = new Translation<string>("<#99bacc>Quest updated: <#eef4f6>{0}</color>.");

    [TranslationData("Sent when a quest is completed.")]
    public readonly Translation<string> QuestCompleted = new Translation<string>("<#99bacc>Quest completed: <#eef4f6>{0}</color>.");
}