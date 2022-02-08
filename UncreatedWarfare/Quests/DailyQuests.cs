using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Quests.Types;

namespace Uncreated.Warfare.Quests;

public static class DailyQuests
{
    private static DateTime LastRefresh;
    public const int DAILY_QUEST_COUNT = 3;
    public static BaseQuestData[] DailyQuestDatas = new BaseQuestData[DAILY_QUEST_COUNT];
    public static IQuestState[] States = new IQuestState[DAILY_QUEST_COUNT];
    public static List<BaseQuestTracker> DailyTrackers = new List<BaseQuestTracker>();
    /// <summary>Checks if a day has passed.</summary>
    public static void Tick()
    {
        DateTime now = DateTime.Now;
        if ((now - LastRefresh).TotalDays > 1d)
        {
            LastRefresh = now;
            for (int i = DailyTrackers.Count - 1; i >= 0; i--)
            {
                QuestManager.DeregisterTracker(DailyTrackers[i]);
                DailyTrackers.RemoveAt(i);
            }
            CreateNewDailyQuests();
        }
    }
    /// <summary>Should run on player connected.</summary>
    public static void RegisterDailyTrackers(UCPlayer player)
    {
        for (int i = 0; i < DailyQuestDatas.Length; i++)
        {
            BaseQuestTracker tracker = DailyQuestDatas[i].GetTracker(player, ref States[i]);
            tracker.IsDailyQuest = true;
            DailyTrackers.Add(tracker);
        }
    }
    /// <summary>Runs every day, creates the daily quests for the day.</summary>
    public static void CreateNewDailyQuests()
    {
        LastRefresh = DateTime.Now;
        if (QuestManager.Quests.Count <= DAILY_QUEST_COUNT)
        {
            L.LogError("Not enough quest types defined to create " + DAILY_QUEST_COUNT + " daily quests.");
            return;
        }
        int[] ints = new int[DAILY_QUEST_COUNT];
        for (int i = 0; i < DAILY_QUEST_COUNT; i++)
        {
            int rndPick;
            // pick DAILY_QUEST_COUNT unique values
            while (true)
            {
                exists:;
                rndPick = UnityEngine.Random.Range(0, QuestManager.Quests.Count);
                for (int p = 0; p < i; p++)
                {
                    if (ints[p] == rndPick)
                        goto exists;
                }
                break;
            }

            ints[i] = rndPick;
            DailyQuestDatas[i] = QuestManager.Quests[rndPick];
            States[i] = DailyQuestDatas[i].GetState();
        }
    }
}
