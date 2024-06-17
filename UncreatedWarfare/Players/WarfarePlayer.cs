using SDG.Unturned;
using Steamworks;

namespace Uncreated.Warfare.Players;
public class WarfarePlayer
{
    public Player Player { get; }
    public SteamPlayer SteamPlayer { get; }
    public CSteamID Steam64 { get; }

    /// <summary>
    /// Name visible to group members.
    /// </summary>
    public string NickName => SteamPlayer.playerID.nickName;

    /// <summary>
    /// Name visible to all players.
    /// </summary>
    public string CharacterName => SteamPlayer.playerID.characterName;

    /// <summary>
    /// Name on their Steam profile.
    /// </summary>
    public string SteamName => SteamPlayer.playerID.playerName;
    internal WarfarePlayer(Player player)
    {
        Player = player;
        SteamPlayer = player.channel.owner;
        Steam64 = player.channel.owner.playerID.steamID;
    }
}
