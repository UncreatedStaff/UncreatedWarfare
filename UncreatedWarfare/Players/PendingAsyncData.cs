using SDG.Unturned;
using System.Collections.Generic;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Moderation;

namespace Uncreated.Warfare.Players;
public sealed class PendingAsyncData
{
    public ulong Steam64 => Player.playerID.steamID.m_SteamID;
    public SteamPending Player { get; }
#nullable disable
    public List<PlayerIPAddress> IPAddresses { get; set; }
    public List<PlayerHWID> HWIDs { get; set; }
    public LanguagePreferences LanguagePreferences { get; set; }
#nullable restore
  
    public PendingAsyncData(SteamPending player)
    {
        Player = player;
    }
}
