using DanielWillett.ReflectionTools;
using SDG.NetTransport;
using System;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Players.Extensions;
public static class PlayerStanceExtensions
{
    private static readonly InstanceSetter<PlayerStance, EPlayerStance>? SetStance = Accessor.GenerateInstanceSetter<PlayerStance, EPlayerStance>("_stance", throwOnError: false);

    private static readonly ClientInstanceMethod<uint, EPlayerStance, Vector3, Vector3, byte, int, int>? SendSimulateMispredictedInputs = ReflectionUtility.FindRpc<PlayerInput, ClientInstanceMethod<uint, EPlayerStance, Vector3, Vector3, byte, int, int>>("SendSimulateMispredictedInputs");
    private static readonly Action<PlayerStance, bool>? CallReplicateStance = Accessor.GenerateInstanceCaller<PlayerStance, Action<PlayerStance, bool>>("replicateStance", throwOnError: false);
    private static readonly Action<PlayerStance, EPlayerStance>? CallInternalSetStance = Accessor.GenerateInstanceCaller<PlayerStance, Action<PlayerStance, EPlayerStance>>("internalSetStance", throwOnError: false);
    private static readonly InstanceGetter<PlayerMovement, Vector3>? GetVelocity = Accessor.GenerateInstanceGetter<PlayerMovement, Vector3>("velocity");

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

        if (SendSimulateMispredictedInputs == null || CallInternalSetStance == null || GetVelocity == null || CallReplicateStance == null)
            return false;

        PlayerInput playerInput = player.UnturnedPlayer.input;
        SendSimulateMispredictedInputs.Invoke(playerInput.GetNetId(), ENetReliability.Unreliable, player.Connection,
            playerInput.simulation, stance, player.Position, GetVelocity(player.UnturnedPlayer.movement),
            player.UnturnedPlayer.life.stamina, (int)playerInput.simulation, (int)playerInput.simulation);
        CallInternalSetStance(playerStance, stance);
        CallReplicateStance(playerStance, false);
        return true;
    }
}