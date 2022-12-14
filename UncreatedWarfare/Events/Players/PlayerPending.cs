using System.Threading.Tasks;
using SDG.Unturned;

namespace Uncreated.Warfare.Events.Players;
public sealed class PlayerPending : BreakableEvent
{
    private readonly SteamPending _pending;
    private readonly PlayerSave? _save;
    private string _rejectReason;
    public bool IsNewPlayer => _save is null;
    public PlayerSave? SaveData => _save;
    public SteamPending PendingPlayer => _pending;
    public ulong Steam64 => _pending.playerID.steamID.m_SteamID;
    public string PlayerName => _pending.playerID.playerName;
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
    public PlayerPending(SteamPending player, PlayerSave? saveData, bool shouldAllow, string explanation)
    {
        _pending = player;
        _save = saveData;
        if (!shouldAllow) Break();
        _rejectReason = explanation;
    }
    public ControlException Reject(string reason)
    {
        RejectReason = reason;
        Break();
        return new ControlException();
    }
}
