using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Quests.Parameters;

namespace Uncreated.Warfare.Quests.Daily.Workshop;
internal static class DailyQuestAssetWriter
{
    /// <summary>
    /// Writes daily quests to .dat files as <see cref="QuestAsset"/> files.
    /// </summary>
    public static void WriteDailyQuestFiles(DailyQuestDay[] days, string contentFolder)
    {
        if (Directory.Exists(contentFolder))
            Directory.Delete(contentFolder, recursive: true);

        Directory.CreateDirectory(contentFolder);

        // meta file allows Unturned to find the mod item
        File.WriteAllBytes(Path.Combine(contentFolder, "Object.meta"), new byte[1]);

        string daysFolder = Path.Combine(contentFolder, "NPCs", "Quests");

        StringBuilder descBuilder = new StringBuilder();

        string descBase = $"Every day, {DailyQuestService.PresetLength} missions are generated that you can complete to gain rewards.<br>Total Rewards:";

        for (int i = 0; i < days.Length; ++i)
        {
            DailyQuestDay? day = days[i];

            if (day?.Presets == null || day.Presets.Any(x => x?.TemplateName == null))
            {
                continue;
            }

            string fileName = "DailyQuest" + i.ToString("D2", CultureInfo.InvariantCulture);
            string dayDir = Path.Combine(daysFolder, fileName);
            Directory.CreateDirectory(dayDir);

            int xpSum = 0, credSum = 0, repSum = 0;

            using (StreamWriter writer = new StreamWriter(Path.Combine(dayDir, fileName + ".dat")))
            {
                using DatWriter datWriter = new DatWriter(writer);

                datWriter.WriteDictionaryStart("Metadata");
                datWriter.WriteKeyValue("GUID", day.Asset);
                datWriter.WriteKeyValue("Type", typeof(QuestAsset).AssemblyQualifiedName);
                datWriter.WriteDictionaryEnd();

                datWriter.WriteDictionaryStart("Asset");
                datWriter.WriteKeyValue("ID", day.Id);
                datWriter.WriteEmptyLine();
                datWriter.WriteKey("Conditions");

                datWriter.WriteListStart();

                for (int presetIndex = 0; presetIndex < day.Presets.Length; presetIndex++)
                {
                    datWriter.WriteDictionaryStart();

                    DailyQuestPreset preset = day.Presets[presetIndex]!;

                    QuestParameterValue<int> flagValueParameter = preset.State.FlagValue;

                    short flagValue = flagValueParameter.SelectionType == ParameterSelectionType.Selective || flagValueParameter.ValueType == ParameterValueType.Constant
                        ? (short)flagValueParameter.GetSingleValue()
                        : (short)0;

                    datWriter.WriteKeyValue("Type", nameof(ENPCConditionType.FLAG_SHORT));
                    datWriter.WriteKeyValue("ID", preset.Flag);
                    datWriter.WriteKeyValue("Value", flagValue);
                    datWriter.WriteKeyValue("Logic", nameof(ENPCLogicType.GREATER_THAN_OR_EQUAL_TO));
                    datWriter.WriteKeyValue("TextId", $"Condition_{presetIndex.ToString(CultureInfo.InvariantCulture)}");
                    datWriter.WriteKeyValue("Preset", preset.Key);

                    datWriter.WriteDictionaryEnd();

                    if (preset.RewardOverrides == null)
                        continue;

                    XPReward? xp = preset.RewardOverrides.OfType<XPReward>().FirstOrDefault();
                    CreditsReward? credits = preset.RewardOverrides.OfType<CreditsReward>().FirstOrDefault();
                    ReputationReward? reputation = preset.RewardOverrides.OfType<ReputationReward>().FirstOrDefault();

                    if (xp != null)
                        xpSum += xp.XP;
                    if (credits != null)
                        credSum += credits.Credits;
                    if (reputation != null)
                        repSum += reputation.Reputation;
                }

                datWriter.WriteListEnd();
            }

            using (StreamWriter writer = new StreamWriter(Path.Combine(dayDir, "English.dat")))
            {
                using DatWriter datWriter = new DatWriter(writer);

                datWriter.WriteKeyValue("Name", "<color=#ffff99><color=#cedcde>"
                                                + day.StartTime.ToString("MMM d")
                                                + GetDaySuffix(day.StartTime.Day)
                                                + "</color> Daily Mission</color>");

                descBuilder.Append(descBase);
                if (xpSum != 0)
                {
                    descBuilder.Append("<br> • <color=#ffffff>").Append(xpSum).Append("</color> <color=#e3b552>XP</color>");
                }
                if (credSum != 0)
                {
                    descBuilder.Append("<br> • <color=#ffffff>").Append(credSum).Append("</color> <color=#b8ffc1>C</color>");
                }
                if (repSum != 0)
                {
                    descBuilder.Append("<br> • <color=#ffffff>").Append(repSum).Append("</color> <color=#66ff66>Rep</color>");
                }

                datWriter.WriteKeyValue("Description", descBuilder.ToString());
                descBuilder.Clear();

                datWriter.WriteEmptyLine();
                for (int presetIndex = 0; presetIndex < day.Presets.Length; presetIndex++)
                {
                    DailyQuestPreset preset = day.Presets[presetIndex]!;

                    string text = preset.State.CreateQuestDescriptiveString();
                    datWriter.WriteKeyValue("Condition_" + presetIndex.ToString(CultureInfo.InvariantCulture), text);
                }
            }
        }
    }

    private static string GetDaySuffix(int day)
    {
        // teens dont apply to the ending rule
        if (day is > 3 and < 21)
            return "th";

        return (day % 10) switch
        {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ => "th"
        };
    }
}
