using SDG.Unturned;
using System.Collections.Generic;
using Steamworks;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Moderation;

namespace Uncreated.Warfare.Players;

#nullable disable

/// <summary>
/// Add properties here that are fetched during pre-join and need to be saved for after the player actually joins.
/// </summary>
public sealed class PendingAsyncData
{
    /// <summary>
    /// Steam ID of the player this data is for.
    /// </summary>
    public CSteamID Steam64 { get; }

    /// <summary>
    /// The player this data is for.
    /// </summary>
    public SteamPending Player { get; }

    /// <summary>
    /// List of all IP addresses stored for the joining player.
    /// </summary>
    public List<PlayerIPAddress> IPAddresses { get; set; }

    /// <summary>
    /// List of all hardware IDs stored for the joining player.
    /// </summary>
    public List<PlayerHWID> HWIDs { get; set; }

    /// <summary>
    /// L10N and I14N settings for the joining player.
    /// </summary>
    public LanguagePreferences LanguagePreferences { get; set; }
  
    public PendingAsyncData(SteamPending player)
    {
        Player = player;
        Steam64 = Player.playerID.steamID;
    }
}
