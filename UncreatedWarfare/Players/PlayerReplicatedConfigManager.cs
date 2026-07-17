using Microsoft.Extensions.DependencyInjection;
using System;
using System.Runtime.ExceptionServices;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Players;

/// <summary>
/// Handles updates to players' <see cref="ModeConfigData"/> info.
/// </summary>
/// <typeparam name="TState">Generic state. Starts out as <see langword="default"/> in apply and is retained until after the undo function is called.</typeparam>
/// <param name="config">The config to edit.</param>
/// <param name="state">State passed between apply and undo.</param>
public delegate void ModifyModeConfig<TState>(ModeConfigData config, ref TState state);

/// <inheritdoc cref="ModifyModeConfig{TState}"/>
public delegate void ModifyModeConfig(ModeConfigData config);

/// <summary>
/// Tracks client-side changes to <see cref="Provider.modeConfigData"/> so they can be updated when being sent to players.
/// </summary>
public sealed class PlayerReplicatedConfigManager
{
    private readonly LinkedList<ReplicatedConfigUpdate> _globalUpdates = new LinkedList<ReplicatedConfigUpdate>();
    private readonly IPlayerService _playerService;

    public PlayerReplicatedConfigManager(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    /// <summary>
    /// Replicates a specific change to the mode config by modifying the server config, sending the update, then un-modifying it.
    /// The <paramref name="undo"/> callback must perfectly undo all changes made to the config.
    /// </summary>
    /// <typeparam name="TState">Data transferred between both callbacks. Begins in <paramref name="apply"/> with a value of <see langword="default"/>, and keeps its value in <paramref name="undo"/>.</typeparam>
    /// <param name="apply">Function that sets the differences in values sent to the player.</param>
    /// <param name="undo">Function that un-does all changes to the config back to its original state.</param>
    /// <exception cref="GameThreadException"/>
    /// <exception cref="InvalidOperationException">Broken by a game update.</exception>
    /// <returns>A value that will remove the global update when disposed.</returns>
    [MustUseReturnValue, Pure]
    public IDisposable ReplicateGlobalConfigChange<TState>(ModifyModeConfig<TState> apply, ModifyModeConfig<TState> undo)
        where TState : struct
    {
        GameThread.AssertCurrent();

        IDisposable disposable = new ReplicatedConfigUpdate<TState>(this, null, apply, undo);
        try
        {
            ReplicateConfigToAllPlayers();
        }
        catch
        {
            disposable.Dispose();
            throw;
        }

        return disposable;
    }

    /// <inheritdoc cref="ReplicateGlobalConfigChange{TState}"/>
    [MustUseReturnValue, Pure]
    public IDisposable ReplicateGlobalConfigChange(ModifyModeConfig apply, ModifyModeConfig undo)
    {
        GameThread.AssertCurrent();

        IDisposable disposable = new ReplicatedConfigUpdateStateless(this, null, apply, undo);
        try
        {
            ReplicateConfigToAllPlayers();
        }
        catch
        {
            disposable.Dispose();
            throw;
        }

        return disposable;
    }

    private void ReplicateConfigToAllPlayers()
    {
        if (ReplicateConfigPatches.Callback == null)
        {
            throw new InvalidOperationException("Failed to get ReplicateClient from Provider.accept. A game update broke this feature.");
        }

        PooledTransportConnectionList list = TransportConnectionPoolHelper.Claim(Provider.clients.Count);
        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            ReplicatedConfigComponent component = player.Component<ReplicatedConfigComponent>();
            if (component.Updates.Count == 0)
            {
                list.Add(player.Connection);
                continue;
            }

            ReplicateConfigToPlayer(component);
        }

        if (list.Count == 0)
        {
            return;
        }

        Apply(null);
        try
        {
            NetMessages.SendMessageToClients(EClientMessage.ReplicateConfig, ENetReliability.Reliable, list, ReplicateConfigPatches.Callback);
        }
        finally
        {
            Undo(null);
        }
    }

    private void ReplicateConfigToPlayer(ReplicatedConfigComponent component)
    {
        if (ReplicateConfigPatches.Callback == null)
        {
            throw new InvalidOperationException("Failed to get ReplicateClient from Provider.accept. A game update broke this feature.");
        }

        Apply(component);
        try
        {
            NetMessages.SendMessageToClient(EClientMessage.ReplicateConfig, ENetReliability.Reliable, component.Player.Connection, ReplicateConfigPatches.Callback);
        }
        finally
        {
            Undo(component);
        }
    }

    /// <summary>
    /// Apply all updates for a player. Must be followed directly by <see cref="Undo"/>.
    /// </summary>
    internal void Apply(ReplicatedConfigComponent? player)
    {
        GameThread.AssertCurrent();

        int appliedGlobal = 0, appliedPlayer = 0;
        try
        {
            foreach (ReplicatedConfigUpdate update in _globalUpdates)
            {
                update.Apply();
                ++appliedGlobal;
            }
            if (player != null)
            {
                foreach (ReplicatedConfigUpdate update in player.Updates)
                {
                    update.Apply();
                    ++appliedPlayer;
                }
            }
        }
        catch
        {
            // undo in order on fail
            int skipped = _globalUpdates.Count;
            for (LinkedListNode<ReplicatedConfigUpdate>? node = _globalUpdates.Last; node != null; node = node.Previous)
            {
                if (skipped > appliedGlobal)
                {
                    --skipped;
                    continue;
                }

                node.Value.Undo();
            }
            if (player != null)
            {
                LinkedList<ReplicatedConfigUpdate> list = player.Updates;
                skipped = list.Count;
                for (LinkedListNode<ReplicatedConfigUpdate>? node = list.Last; node != null; node = node.Previous)
                {
                    if (skipped > appliedPlayer)
                    {
                        --skipped;
                        continue;
                    }

                    node.Value.Undo();
                }
            }
            throw;
        }
    }

    /// <summary>
    /// Undo all updates for a player. Done in reverse order as they were applied.
    /// </summary>
    internal void Undo(ReplicatedConfigComponent? player)
    {
        GameThread.AssertCurrent();

        List<Exception>? exes = null;
        for (LinkedListNode<ReplicatedConfigUpdate>? node = _globalUpdates.Last; node != null; node = node.Previous)
        {
            try
            {
                node.Value.Undo();
            }
            catch (Exception ex)
            {
                (exes ??= []).Add(ex);
            }
        }

        if (player != null)
        {
            LinkedList<ReplicatedConfigUpdate> list = player.Updates;
            for (LinkedListNode<ReplicatedConfigUpdate>? node = list.Last; node != null; node = node.Previous)
            {
                try
                {
                    node.Value.Undo();
                }
                catch (Exception ex)
                {
                    (exes ??= []).Add(ex);
                }
            }
        }

        if (exes == null)
            return;

        if (exes.Count == 1)
            ExceptionDispatchInfo.Throw(exes[0]);

        throw new AggregateException(exes);
    }

    [PlayerComponent]
    internal sealed class ReplicatedConfigComponent : IPlayerComponent
    {
#nullable disable

        private PlayerReplicatedConfigManager _manager;

#nullable restore

        // player-specific updates, applied in order
        internal LinkedList<ReplicatedConfigUpdate> Updates = new LinkedList<ReplicatedConfigUpdate>();

        public required WarfarePlayer Player { get; init; }

        public void Init(IServiceProvider serviceProvider, bool isOnJoin)
        {
            if (isOnJoin)
                _manager = serviceProvider.GetRequiredService<PlayerReplicatedConfigManager>();
        }

        internal IDisposable AddUpdate<TState>(ModifyModeConfig<TState> apply, ModifyModeConfig<TState> undo)
            where TState : struct
        {
            return new ReplicatedConfigUpdate<TState>(_manager, this, apply, undo);
        }

        internal IDisposable AddUpdate(ModifyModeConfig apply, ModifyModeConfig undo)
        {
            return new ReplicatedConfigUpdateStateless(_manager, this, apply, undo);
        }

        /// <inheritdoc cref="PlayerReplicatedConfigManager.ReplicateConfigToPlayer"/>
        internal void Replicate() => _manager.ReplicateConfigToPlayer(this);
    }

    internal abstract class ReplicatedConfigUpdate : IDisposable
    {
        protected readonly PlayerReplicatedConfigManager Manager;
        protected readonly ReplicatedConfigComponent? PlayerComponent;
        protected readonly LinkedListNode<ReplicatedConfigUpdate> Node;

        protected ReplicatedConfigUpdate(PlayerReplicatedConfigManager manager, ReplicatedConfigComponent? playerComponent)
        {
            Manager = manager;
            PlayerComponent = playerComponent;

            LinkedList<ReplicatedConfigUpdate> list = PlayerComponent == null ? Manager._globalUpdates : PlayerComponent.Updates;
            Node = list.AddFirst(this);
        }

        internal abstract void Apply();

        internal abstract void Undo();

        public void Dispose()
        {
            LinkedList<ReplicatedConfigUpdate> list = PlayerComponent == null ? Manager._globalUpdates : PlayerComponent.Updates;

            try
            {
                list.Remove(Node);
            }
            catch (InvalidOperationException)
            {
                // already disposed
                return;
            }

            if (PlayerComponent == null)
            {
                Manager.ReplicateConfigToAllPlayers();
            }
            else if (PlayerComponent.Player.IsOnline)
            {
                Manager.ReplicateConfigToPlayer(PlayerComponent);
            }
        }
    }

    internal sealed class ReplicatedConfigUpdateStateless : ReplicatedConfigUpdate
    {
        private readonly ModifyModeConfig _apply;
        private readonly ModifyModeConfig _undo;

        private ModeConfigData? _modeConfigData;

        public ReplicatedConfigUpdateStateless(
            PlayerReplicatedConfigManager manager,
            ReplicatedConfigComponent? playerComponent,
            ModifyModeConfig apply,
            ModifyModeConfig undo
        ) : base(manager, playerComponent)
        {
            _apply = apply;
            _undo = undo;
        }

        internal override void Apply()
        {
            _modeConfigData = Provider.modeConfigData;
            _apply(_modeConfigData);
        }

        internal override void Undo()
        {
            _undo(_modeConfigData!);
            _modeConfigData = null;
        }
    }

    internal class ReplicatedConfigUpdate<TState> : ReplicatedConfigUpdate
        where TState : struct
    {
        private readonly ModifyModeConfig<TState> _apply;
        private readonly ModifyModeConfig<TState> _undo;

        private ModeConfigData? _modeConfigData;
        private TState _state;

        public ReplicatedConfigUpdate(
            PlayerReplicatedConfigManager manager,
            ReplicatedConfigComponent? playerComponent,
            ModifyModeConfig<TState> apply,
            ModifyModeConfig<TState> undo
        ) : base(manager, playerComponent)
        {
            _apply = apply;
            _undo = undo;
        }

        internal override void Apply()
        {
            _modeConfigData = Provider.modeConfigData;
            _apply(_modeConfigData, ref _state);
        }

        internal override void Undo()
        {
            _undo(_modeConfigData!, ref _state);
            _state = default;
            _modeConfigData = null;
        }
    }
}