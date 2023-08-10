using SDG.Unturned;

namespace Uncreated.Warfare.Players;
public sealed class PendingAsyncData
{
    public ulong Steam64 => Player.playerID.steamID.m_SteamID;
    public SteamPending Player { get; }
#nullable disable
    public PlayerLanguagePreferences LanguagePreferences { get; set; }

#nullable restore
    public PendingAsyncData(SteamPending player)
    {
        Player = player;
    }
}
