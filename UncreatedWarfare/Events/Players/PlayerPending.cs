using System;
using SDG.Unturned;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Players;
public sealed class PlayerPending : BreakableEvent
{
    private readonly SteamPending _pending;
    private readonly PlayerSave? _save;
    private readonly PendingAsyncData? _asyncData;
    private string _rejectReason;
    public bool IsNewPlayer => _save is null;
    public PlayerSave? SaveData => _save;
    public SteamPending PendingPlayer => _pending;
    public ulong Steam64 => _pending.playerID.steamID.m_SteamID;
    public string PlayerName => _pending.playerID.playerName;
    public PendingAsyncData AsyncData => _asyncData ?? throw new InvalidOperationException("Only useable in the async event.");
    public ESteamRejection Rejection { get; set; } = ESteamRejection.PLUGIN;
    public string RejectReason
    {
        get => _rejectReason;
        set => _rejectReason = value ?? string.Empty;
    }
    public string CharacterName
    {
        get => _pending.playerID.characterName;
        set => _pending.playerID.characterName = value;
    }
    public string NickName
    {
        get => _pending.playerID.nickName;
        set => _pending.playerID.nickName = value;
    }
    public PlayerPending(SteamPending player, PlayerSave? saveData, PendingAsyncData? data, bool shouldAllow, string explanation)
    {
        _pending = player;
        _save = saveData;
        _rejectReason = explanation;
        _asyncData = data;
        if (!shouldAllow) Break();
    }
    public ControlException Reject(string reason, ESteamRejection rejection)
    {
        Rejection = rejection;
        return Reject(reason);
    }
    public ControlException Reject(string reason)
    {
        RejectReason = reason;
        Break();
        return new ControlException();
    }
}
