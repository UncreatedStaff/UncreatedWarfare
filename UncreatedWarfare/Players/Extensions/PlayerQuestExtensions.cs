using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Players.Extensions;

public static class PlayerQuestExtensions
{
    /// <summary>
    /// Set the flag value if it isn't already correct.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    /// <returns><see langword="true"/> if it was set, otherwise <see langword="false"/> if it was already correct.</returns>
    public static bool SetFlag(this WarfarePlayer player, ushort flagId, short value)
    {
        GameThread.AssertCurrent();

        PlayerQuests quests = player.UnturnedPlayer.quests;
        if (quests.getFlag(flagId, out short oldValue) && oldValue == value)
            return false;
        
        quests.sendSetFlag(flagId, value);
        return true;
    }
}
