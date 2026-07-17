using System;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Players.Extensions;

/// <summary>
/// Extentions for replicating config changes to individual players.
/// For global changes use the <see cref="PlayerReplicatedConfigManager"/> service.
/// </summary>
public static class ReplicateConfigExtensions
{
    extension(WarfarePlayer player)
    {
        /// <inheritdoc cref="PlayerReplicatedConfigManager.ReplicateGlobalConfigChange{TState}"/>
        [MustUseReturnValue, Pure]
        public IDisposable? ReplicateConfigChange<TState>(ModifyModeConfig<TState> apply, ModifyModeConfig<TState> undo)
            where TState : struct
        {
            GameThread.AssertCurrent();

            if (!player.IsOnline)
                return null;

            PlayerReplicatedConfigManager.ReplicatedConfigComponent component = player.Component<PlayerReplicatedConfigManager.ReplicatedConfigComponent>();
            IDisposable disposable = component.AddUpdate(apply, undo);
            try
            {
                component.Replicate();
            }
            catch
            {
                disposable.Dispose();
                throw;
            }

            return disposable;
        }

        /// <inheritdoc cref="ReplicateConfigExtensions.ReplicateConfigChange{TState}"/>
        [MustUseReturnValue, Pure]
        public IDisposable? ReplicateConfigChange(ModifyModeConfig apply, ModifyModeConfig undo)
        {
            GameThread.AssertCurrent();

            if (!player.IsOnline)
                return null;

            PlayerReplicatedConfigManager.ReplicatedConfigComponent component = player.Component<PlayerReplicatedConfigManager.ReplicatedConfigComponent>();
            IDisposable disposable = component.AddUpdate(apply, undo);
            try
            {
                component.Replicate();
            }
            catch
            {
                disposable.Dispose();
                throw;
            }

            return disposable;
        }
    }
}