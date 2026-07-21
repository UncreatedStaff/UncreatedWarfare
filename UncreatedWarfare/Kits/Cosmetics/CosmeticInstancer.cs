using System;
using System.ComponentModel;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Kits.Cosmetics;

/// <summary>
/// Handles deciding whether players can see cosmetics or not, and makes sure that the correct players are seeing the correct sets of armor.
/// </summary>
/// <remarks>Layout-scoped.</remarks>
public class CosmeticInstancer : IAsyncEventListener<PlayerDutyStatusChanged>, IEventListener<PlayerJoined>, ILayoutHostedService
{
    private readonly ICosmeticItemProvider _itemProvider;
    private readonly IPlayerService _playerService;

    private WarfarePlayer[] _remainingPlayersBuffer = new WarfarePlayer[64];
    private ClothingInfo[] _newAssetsBuffer = new ClothingInfo[64];


    private const ENetReliability SendReliability = ENetReliability.Reliable;

    private readonly bool[] _isInstancedTable =
    [
        true,   // Shirt
        true,   // Pants
        true,   // Vest
        true,   // Hat
        false,  // Mask
        true,   // Backpack
        false   // Glasses
    ];

    public bool IsEnabled => _isEnabledIntl && _itemProvider.IsEnabled;

    private bool _isEnabledIntl;

    public CosmeticInstancer(ICosmeticItemProvider itemProvider, IPlayerService playerService)
    {
        _itemProvider = itemProvider;
        _playerService = playerService;
        _isEnabledIntl = false;
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        _isEnabledIntl = true;
        return UniTask.CompletedTask;
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        _isEnabledIntl = false;
        return UniTask.CompletedTask;
    }

    [EventListener(MustRunLast = true)]
    void IEventListener<PlayerJoined>.HandleEvent(PlayerJoined e, IServiceProvider serviceProvider)
    {
        if (e.Player.Team.IsValid)
        {
            // state was restored from same game when they left before
            UpdateClothesOnPlayer(e.Player, false);
        }
    }

    [EventListener(RequiresMainThread = true)]
    UniTask IAsyncEventListener<PlayerDutyStatusChanged>.HandleEventAsync(PlayerDutyStatusChanged e, IServiceProvider serviceProvider, CancellationToken token)
    {
        UpdateClothesOnPlayer(e.Player, playEffect: false);
        return UpdateAllClothesAsync(e.Player, token: e.Player.DisconnectToken);
    }

    /// <summary>
    /// Determines what any given player should be seeing on another player.
    /// </summary>
    /// <returns><see langword="true"/> if any instancing occurred, otherwise <see langword="false"/>.</returns>
    public bool Resolve(WarfarePlayer viewPlayer, WarfarePlayer onPlayer, ClothingType type, out ItemClothingAsset? clothing, out byte quality, out byte[] state)
    {
        GameThread.AssertCurrent();

        ClothingItem item = new ClothingItem(onPlayer.UnturnedPlayer.clothing, type);
        quality = item.Quality;
        state = item.State;

        Kit? kit = onPlayer.Component<KitPlayerComponent>().ActiveKit?.CachedKit;
        if (!IsEnabled || !ShouldInstance(type, onPlayer, kit) || !ShouldSeeInstancedClothes(onPlayer, viewPlayer))
        {
            clothing = item.Asset;
            quality = item.Quality;
            state = item.State;
            return false;
        }

        clothing = _itemProvider.Resolve(viewPlayer, onPlayer, in item, kit, ref quality, ref state);
        return true;
    }

    /// <summary>
    /// Determines whether or not to apply clothing instancing to the given player.
    /// </summary>
    public bool ShouldInstance(ClothingType type, WarfarePlayer player)
    {
        if (!_isEnabledIntl)
            return false;

        Kit? kit = player.Component<KitPlayerComponent>().ActiveKit?.CachedKit;
        return ShouldInstance(type, player, kit);
    }

    /// <inheritdoc cref="ShouldInstance(ClothingType, WarfarePlayer)"/>
    protected virtual bool ShouldInstance(ClothingType type, WarfarePlayer player, [NotNullWhen(true)] Kit? kit)
    {
        if (!_isEnabledIntl)
            return false;

        if (type > ClothingType.Glasses)
            throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(ClothingType));

        if (kit == null || !_isInstancedTable[(int)type] || player.IsOnDuty)
            return false;

        return _itemProvider.ShouldKitCosmeticsBeInstanced(kit);
    }

    /// <summary>
    /// Updates the clothes on <paramref name="player"/> for all other players.
    /// </summary>
    public void UpdateClothesOnPlayer(WarfarePlayer player, bool playEffect)
    {
        if (!_isEnabledIntl)
            return;

        GameThread.AssertCurrent();

        for (int c = 0; c < ClothingItem.Count; ++c)
        {
            SendClothingToClientForPlayer(player, (ClothingType)c, playEffect);
            playEffect = false;
        }
    }

    /// <summary>
    /// Updates a <paramref name="onPlayer"/>'s clothes for <paramref name="viewPlayer"/>.
    /// </summary>
    public void UpdateClothesForPlayer(WarfarePlayer viewPlayer, WarfarePlayer onPlayer, CancellationToken token = default)
    {
        if (!_isEnabledIntl)
            return;

        GameThread.AssertCurrent();

        Kit? kit = onPlayer.Component<KitPlayerComponent>().ActiveKit?.CachedKit;

        NetId netId = onPlayer.UnturnedPlayer.clothing.GetNetId();

        bool instance = kit != null && ShouldSeeInstancedClothes(onPlayer, viewPlayer);

        ITransportConnection connection = viewPlayer.Connection;

        for (int c = 0; c < ClothingItem.Count; ++c)
        {
            ClothingItem item = new ClothingItem(onPlayer.UnturnedPlayer.clothing, (ClothingType)c);

            bool isThisInstanced = instance && ShouldInstance(item.Type, onPlayer);

            ClientInstanceMethod<Guid, byte, byte[], bool>? rpc = ItemUtility.GetClothingRpc(item.Type);
            if (rpc == null)
            {
                WarfareModule.Singleton.GlobalLogger.LogWarning($"Unable to replicate clothes to clients. Missing {item.Type} RPC.");
                return;
            }

            ItemClothingAsset? asset = item.Asset;
            if (!isThisInstanced)
            {
                rpc.Invoke(netId, SendReliability, connection, asset?.GUID ?? Guid.Empty, item.Quality, item.State, false);
            }
            else
            {
                byte q = item.Quality;
                byte[] s = item.State;
                asset = _itemProvider.Resolve(viewPlayer, onPlayer, in item, kit!, ref q, ref s);
                rpc.Invoke(netId, SendReliability, connection, asset?.GUID ?? Guid.Empty, Math.Clamp(q, (byte)0, (byte)100), s ?? Array.Empty<byte>(), false);
            }
        }
    }

    /// <summary>
    /// Updates every other player's clothes for <paramref name="player"/>.
    /// </summary>
    /// <remarks>Runs spread out over frames so could take a while depending on the player count.</remarks>
    public async UniTask UpdateAllClothesAsync(WarfarePlayer player, bool instant = false, CancellationToken token = default)
    {
        if (!_isEnabledIntl)
            return;

        bool enemies = player.Save.ViewEnemyCosmetics;
        bool friendlies = player.Save.ViewFriendlyCosmetics;

        int ctOnThisFrame = 0;
        const int perFrame = 2;

        foreach (WarfarePlayer otherPlayer in _playerService.OnlinePlayers)
        {
            if (otherPlayer.Equals(player))
                continue;

            ++ctOnThisFrame;
            if (!instant && ctOnThisFrame == perFrame)
            {
                ctOnThisFrame = 0;
                await UniTask.NextFrame(token);

                if (player.Save.ViewEnemyCosmetics != enemies || player.Save.ViewFriendlyCosmetics != friendlies)
                    return;
            }

            UpdateClothesForPlayer(player, otherPlayer, token);
        }
    }

    protected virtual bool ShouldSeeInstancedClothes(WarfarePlayer onPlayer, WarfarePlayer viewPlayer)
    {
        if (onPlayer.Equals(viewPlayer))
        {
            // players should always see their own cosmetics
            return false;
        }

        if (viewPlayer.IsOnDuty)
            return false;

        if (onPlayer.Team.IsFriendly(viewPlayer.Team))
        {
            if (viewPlayer.Save.ViewFriendlyCosmetics)
                return false;
        }
        else if (onPlayer.Team.IsOpponent(viewPlayer.Team))
        {
            if (viewPlayer.Save.ViewEnemyCosmetics)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Replaces the askWear functions and the SendWear RPCs.
    /// Changes the clothing on a player and sends the correct updates to all players.
    /// </summary>
    /// <param name="type">The type of clothing to assign.</param>
    /// <param name="player">The player who's clothes are changing.</param>
    /// <param name="clothing">The clothing item being put on, or <see langword="null"/> if the clothing item is being removed.</param>
    /// <param name="quality">The quality of the clothing item. <c>0</c> to <c>100</c>.</param>
    /// <param name="state">The state array of the clothing item.</param>
    /// <param name="playEffect">Whether or not to play the 'zipping' sound when the clothing is changed.</param>
    /// <exception cref="ArgumentException">Clothing asset type doesn't correspond to <paramref name="type"/>.</exception>
    /// <exception cref="GameThreadException"/>
    public void SetClothing(ClothingType type, WarfarePlayer player, ItemClothingAsset? clothing, byte quality, byte[] state, bool playEffect)
    {
        GameThread.AssertCurrent();

        if (!IsEnabled || !ShouldInstance(type, player))
        {
            if (ItemUtility.HasClothingRpc(type))
            {
                ItemUtility.SendWearClothing(player.UnturnedPlayer, Provider.GatherRemoteClientConnections(), true, clothing, type, quality, state, playEffect);
            }
            else
            {
                ClothingItem item = new ClothingItem(player.UnturnedPlayer.clothing, type);
                ItemClothingAsset? old = item.Asset;
                item.AskWear(clothing, quality, state, playEffect);

                if (old != null)
                    ItemUtility.RemoveAutoItem(player.UnturnedPlayer, old);
            }
        }

        SetClothingShortcut(type, player, clothing, quality, state, playEffect);
    }

    /// <inheritdoc cref="SetClothing"/>
    internal void SetClothingShortcut(ClothingType type, WarfarePlayer player, ItemClothingAsset? clothing, byte quality, byte[] state, bool playEffect)
    {
        PlayerClothing playerClothing = player.UnturnedPlayer.clothing;
        ClothingItem item = new ClothingItem(playerClothing, type);
        if (clothing != null && !item.ValidAsset(clothing))
        {
            throw new ArgumentException($"Invalid clothing asset type for the {type} slot: {clothing.GetType().Name} ({clothing.FriendlyName}).");
        }

        quality = Math.Clamp(quality, (byte)0, (byte)100);

        Guid guid = clothing?.GUID ?? Guid.Empty;

        // set the items on the server
        switch (type)
        {
            case ClothingType.Shirt:
                playerClothing.ReceiveWearShirt(guid, quality, state, playEffect);
                break;

            case ClothingType.Pants:
                playerClothing.ReceiveWearPants(guid, quality, state, playEffect);
                break;

            case ClothingType.Vest:
                playerClothing.ReceiveWearVest(guid, quality, state, playEffect);
                break;

            case ClothingType.Hat:
                playerClothing.ReceiveWearHat(guid, quality, state, playEffect);
                break;

            case ClothingType.Mask:
                playerClothing.ReceiveWearMask(guid, quality, state, playEffect);
                break;

            case ClothingType.Backpack:
                playerClothing.ReceiveWearBackpack(guid, quality, state, playEffect);
                break;

            case ClothingType.Glasses:
                playerClothing.ReceiveWearGlasses(guid, quality, state, playEffect);
                break;

            default:
                throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(ClothingType));
        }

        // send the items to the clients
        SendClothingToClientForPlayer(type, player, clothing, quality, state, playEffect);
    }

    private void SendClothingToClientForPlayer(WarfarePlayer player, ClothingType type, bool playEffect)
    {
        ClothingItem item = new ClothingItem(player.UnturnedPlayer.clothing, type);
        SendClothingToClientForPlayer(item.Type, player, item.Asset, item.Quality, item.State, playEffect);
    }

    private void SendClothingToClientForPlayer(ClothingType type, WarfarePlayer player, ItemClothingAsset? clothing, byte quality, byte[] state, bool playEffect)
    {
        Guid guid = clothing?.GUID ?? Guid.Empty;

        ClothingItem item = new ClothingItem(player.UnturnedPlayer.clothing, type);

        List<ITransportConnection> clientPool = TransportConnectionPoolHelper.Claim(Provider.clients.Count);

        ClientInstanceMethod<Guid, byte, byte[], bool>? rpc = ItemUtility.GetClothingRpc(type);
        if (rpc == null)
        {
            WarfareModule.Singleton.GlobalLogger.LogWarning($"Unable to replicate clothes to clients. Missing {type} RPC.");
            return;
        }

        Kit? kit = player.Component<KitPlayerComponent>().ActiveKit?.CachedKit;
        int otherPlayerCount = 0;
        try
        {
            if (kit == null)
            {
                foreach (WarfarePlayer otherPlayer in _playerService.OnlinePlayers)
                {
                    clientPool.Add(otherPlayer.Connection);
                }
            }
            else
            {
                if (_remainingPlayersBuffer.Length < _playerService.OnlinePlayers.Count)
                {
                    _remainingPlayersBuffer = new WarfarePlayer[_playerService.OnlinePlayers.Count];
                }

                foreach (WarfarePlayer otherPlayer in _playerService.OnlinePlayers)
                {
                    if (!ShouldSeeInstancedClothes(player, otherPlayer))
                    {
                        clientPool.Add(otherPlayer.Connection);
                    }
                    else
                    {
                        _remainingPlayersBuffer[otherPlayerCount] = otherPlayer;
                        ++otherPlayerCount;
                    }
                }
            }

            NetId netId = player.UnturnedPlayer.clothing.GetNetId();

            if (clientPool.Count > 0)
            {
                // send to all players who see the actual clothing
                rpc.Invoke(netId, SendReliability, clientPool, guid, quality, state, playEffect);
                clientPool.Clear();
            }

            if (otherPlayerCount <= 0)
                return;

            if (_itemProvider.PlayerAgnostic)
            {
                foreach (WarfarePlayer pl in _remainingPlayersBuffer)
                {
                    clientPool.Add(pl.Connection);
                }

                ItemClothingAsset? newAsset = _itemProvider.Resolve(null, player, in item, kit!, ref quality, ref state);
                guid = newAsset?.GUID ?? Guid.Empty;
                rpc.Invoke(netId, SendReliability, clientPool, guid, quality, state, playEffect);
            }
            else
            {
                if (_newAssetsBuffer.Length < otherPlayerCount)
                    _newAssetsBuffer = new ClothingInfo[otherPlayerCount];

                // group players by which items they should be seeing so we can bulk-send them
                for (int i = 0; i < otherPlayerCount; ++i)
                {
                    WarfarePlayer otherPlayer = _remainingPlayersBuffer[i];
                    byte q = quality;
                    byte[] s = state;
                    ItemClothingAsset? newAsset = _itemProvider.Resolve(otherPlayer, player, in item, kit!, ref q, ref s);

                    ref ClothingInfo info = ref _newAssetsBuffer[i];
                    info.Asset = newAsset;
                    info.Quality = Math.Clamp(q, (byte)0, (byte)100);
                    info.State = s ?? Array.Empty<byte>();
                }

                Array.Sort(_newAssetsBuffer, _remainingPlayersBuffer, 0, otherPlayerCount, ClothingComparer.Instance);

                ClothingInfo previous = default;
                previous.Quality = 255; // so it doesn't equal by default
                for (int i = 0; i < otherPlayerCount; ++i)
                {
                    WarfarePlayer otherPlayer = _remainingPlayersBuffer[i];

                    ref ClothingInfo other = ref _newAssetsBuffer[i];
                    if (!other.Equals(ref previous))
                    {
                        previous.State ??= previous.Asset?.getState(true) ?? Array.Empty<byte>();
                        rpc.Invoke(netId, SendReliability, clientPool, previous.Asset?.GUID ?? Guid.Empty, previous.Quality, previous.State, playEffect);
                        previous = other;
                        clientPool.Clear();
                    }

                    clientPool.Add(otherPlayer.Connection);
                }

                if (clientPool.Count > 0)
                {
                    previous.State ??= previous.Asset?.getState(true) ?? Array.Empty<byte>();
                    rpc.Invoke(netId, SendReliability, clientPool, previous.Asset?.GUID ?? Guid.Empty, previous.Quality, previous.State, playEffect);
                }
            }
        }
        finally
        {
            Array.Clear(_remainingPlayersBuffer, 0, otherPlayerCount);
            Array.Clear(_newAssetsBuffer, 0, otherPlayerCount);
        }
    }

    private struct ClothingInfo
    {
        public ItemClothingAsset? Asset;
        public byte Quality;
        public byte[]? State;

        public bool Equals(ref ClothingInfo other)
        {
            if (Asset != other.Asset)
            {
                if (Asset == null)
                    return false;
                if (other.Asset == null)
                    return false;
                if (Asset.GUID != other.Asset.GUID)
                    return false;
            }

            if (Quality != other.Quality)
                return false;

            if (State == null)
                return other.State == null;
            if (other.State == null)
                return false;

            if (State.Length != other.State.Length)
                return false;

            for (int i = 0; i < State.Length; ++i)
            {
                if (State[i] != other.State[i])
                    return false;
            }

            return true;
        }
    }

    private sealed class ClothingComparer : IComparer<ClothingInfo>
    {
        public static readonly ClothingComparer Instance = new ClothingComparer();

        public int Compare(ClothingInfo x, ClothingInfo y)
        {
            if (x.Asset != y.Asset)
            {
                if (x.Asset == null)
                    return -1;
                if (y.Asset == null)
                    return 1;

                return x.Asset.GUID.CompareTo(y.Asset.GUID);
            }

            if (x.Quality != y.Quality)
                return x.Quality - y.Quality;

            byte[] xState = x.State!, yState = y.State!;
            if (xState.Length != yState.Length)
            {
                return xState.Length > yState.Length ? 1 : -1;
            }

            for (int i = 0; i < xState.Length; ++i)
            {
                int cmp = xState[i] - yState[i];
                if (cmp != 0)
                    return cmp;
            }

            return 0;
        }
    }
}