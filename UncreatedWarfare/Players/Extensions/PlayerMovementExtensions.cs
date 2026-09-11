using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Players.Extensions;

/// <summary>
/// Extension members for <see cref="PlayerMovement"/>-related functionality.
/// </summary>
public static class PlayerMovementExtensions
{
    extension(WarfarePlayer player)
    {
        /// <summary>
        /// The vehicle this <paramref name="player"/> is in, or <see langword="null"/> if they're not in a vehicle.
        /// </summary>
        /// <exception cref="GameThreadException"/>
        public InteractableVehicle? Vehicle
        {
            get
            {
                GameThread.AssertCurrent();

                return player.UnturnedPlayer.movement.getVehicle();
            }
        }

        /// <summary>
        /// Whether or not this <paramref name="player"/> is driving a vehicle.
        /// </summary>
        /// <exception cref="GameThreadException"/>
        public bool IsDriving
        {
            get
            {
                GameThread.AssertCurrent();

                PlayerMovement movement = player.UnturnedPlayer.movement;
                return movement.getVehicle() is not null && movement.getSeat() == 0;
            }
        }

        /// <summary>
        /// The index of the seat this player is in, or <c>-1</c> if the player isn't in a vehicle.
        /// </summary>
        /// <exception cref="GameThreadException"/>
        public int VehicleSeatIndex
        {
            get
            {
                GameThread.AssertCurrent();

                PlayerMovement movement = player.UnturnedPlayer.movement;
                if (movement.getVehicle() is null)
                    return -1;

                return movement.getSeat();
            }
        }

        /// <summary>
        /// Get the vehicle the player is in.
        /// </summary>
        /// <param name="vehicle">The vehicle the player is in, or <see langword="null"/> if they're not in a vehicle.</param>
        /// <returns><see langword="true"/> if the player is in a vehicle, otherwise <see langword="false"/>.</returns>
        /// <exception cref="GameThreadException"/>
        public bool TryGetVehicle([NotNullWhen(true)] out InteractableVehicle? vehicle)
        {
            return player.TryGetVehicle(out vehicle, out _);
        }

        /// <summary>
        /// Get the vehicle the player is in and the seat index they're sitting in.
        /// </summary>
        /// <param name="vehicle">The vehicle the player is in, or <see langword="null"/> if they're not in a vehicle.</param>
        /// <param name="seat">The index of the seat the player is in, or <c>0</c> if they're not in a vehicle.</param>
        /// <returns><see langword="true"/> if the player is in a vehicle, otherwise <see langword="false"/>.</returns>
        /// <exception cref="GameThreadException"/>
        public bool TryGetVehicle([NotNullWhen(true)] out InteractableVehicle? vehicle, out byte seat)
        {
            GameThread.AssertCurrent();

            PlayerMovement movement = player.UnturnedPlayer.movement;
            InteractableVehicle v = movement.getVehicle();
            if (v == null)
            {
                vehicle = null;
                seat = 0;
                return false;
            }

            vehicle = v;
            seat = movement.getSeat();
            return true;
        }

        /// <summary>
        /// Sets the plugin movement speed multiplier to <c>1.0</c> for the given <paramref name="player"/>.
        /// </summary>
        /// <exception cref="GameThreadException"/>
        public void ResetSpeedMultiplier()
        {
            player.SetSpeedMultiplier(1f);
        }

        /// <summary>
        /// Updates the plugin movement speed multiplier for the given <paramref name="player"/>.
        /// </summary>
        /// <exception cref="GameThreadException"/>
        public void SetSpeedMultiplier(float speedMultiplier)
        {
            GameThread.AssertCurrent();

            PlayerMovement movement = player.UnturnedPlayer.movement;
            if (Mathf.Approximately(movement.pluginSpeedMultiplier, speedMultiplier))
                return;

            movement.sendPluginSpeedMultiplier(speedMultiplier);
        }

        /// <summary>
        /// Sets the plugin jump height multiplier to <c>1.0</c> for the given <paramref name="player"/>.
        /// </summary>
        /// <exception cref="GameThreadException"/>
        public void ResetJumpMultiplier()
        {
            player.SetJumpMultiplier(1f);
        }

        /// <summary>
        /// Updates the plugin jump height multiplier for the given <paramref name="player"/>.
        /// </summary>
        /// <exception cref="GameThreadException"/>
        public void SetJumpMultiplier(float jumpHeightMultiplier)
        {
            GameThread.AssertCurrent();

            PlayerMovement movement = player.UnturnedPlayer.movement;
            if (Mathf.Approximately(movement.pluginJumpMultiplier, jumpHeightMultiplier))
                return;

            movement.sendPluginJumpMultiplier(jumpHeightMultiplier);
        }

        /// <summary>
        /// Sets the plugin gravity multiplier to <c>1.0</c> for the given <paramref name="player"/>.
        /// </summary>
        /// <exception cref="GameThreadException"/>
        public void ResetGravityMultiplier()
        {
            player.SetGravityMultiplier(1f);
        }

        /// <summary>
        /// Updates the plugin gravity multiplier for the given <paramref name="player"/>.
        /// </summary>
        /// <exception cref="GameThreadException"/>
        public void SetGravityMultiplier(float gravityMultiplier)
        {
            GameThread.AssertCurrent();

            PlayerMovement movement = player.UnturnedPlayer.movement;
            if (Mathf.Approximately(movement.pluginGravityMultiplier, gravityMultiplier))
                return;

            movement.sendPluginGravityMultiplier(gravityMultiplier);
        }

        /// <summary>
        /// Freezes this player by setting their speed, jump height, and gravity multipliers to <c>0.0</c>.
        /// </summary>
        public void FreezeMovement()
        {
            player.SetSpeedMultiplier(0f);
            player.SetJumpMultiplier(0f);
            player.SetGravityMultiplier(0f);
        }

        /// <summary>
        /// Unfreezes this player by setting their speed, jump height, and gravity multipliers to <c>1.0</c>.
        /// </summary>
        public void UnfreezeMovement()
        {
            player.ResetSpeedMultiplier();
            player.ResetJumpMultiplier();
            player.ResetGravityMultiplier();
        }
    }
}