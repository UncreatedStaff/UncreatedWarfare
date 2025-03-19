using DanielWillett.ReflectionTools;
using SDG.Framework.Utilities;
using System;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Players.Extensions;
public static class PlayerStanceExtensions
{
    private static readonly InstanceSetter<PlayerStance, EPlayerStance>? SetStance = Accessor.GenerateInstanceSetter<PlayerStance, EPlayerStance>("_stance", throwOnError: false);
    private static readonly InstanceGetter<PlayerStance, float>? GetLastStanceTime = Accessor.GenerateInstanceGetter<PlayerStance, float>("lastStance", throwOnError: false);

    private static readonly Action<PlayerStance, bool>? CallReplicateStance = Accessor.GenerateInstanceCaller<PlayerStance, Action<PlayerStance, bool>>("replicateStance", throwOnError: false);

    /// <summary>
    /// Sets the stance of the player and replicates it to everyone.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    // todo test
    public static bool TrySetStance(this WarfarePlayer player, EPlayerStance stance)
    {
        GameThread.AssertCurrent();

        PlayerStance playerStance = player.UnturnedPlayer.stance;
        if (player.UnturnedPlayer.life.isDead)
        {
            if (CallReplicateStance == null || SetStance == null)
                return false;

            SetStance(playerStance, stance);
            CallReplicateStance(playerStance, true);
            return true;
        }

        if (playerStance.stance == stance)
            return true;
        
        playerStance.checkStance(stance, all /* notifyOwner */: true);
        if (playerStance.stance == stance)
            return true;

        // ping will take care of any precision issues
        float timeLeft = GetLastStanceTime == null
            ? PlayerStance.COOLDOWN
            : PlayerStance.COOLDOWN - (Time.realtimeSinceStartup - GetLastStanceTime(playerStance));

        TimeUtility.InvokeAfterDelay(() =>
        {
            if (!player.IsOnline)
                return;

            player.UnturnedPlayer.stance.checkStance(stance, all /* notifyOwner */: true);
        }, timeLeft);
        return true;
    }
}