using Microsoft.Extensions.Logging;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players.Saves;
using UnityEngine;

namespace Uncreated.Warfare.Players;
public class WarfarePlayer
{
    private ILogger<WarfarePlayer> _logger;
    public CSteamID Steam64 { get; }
    public Player UnturnedPlayer { get; }
    public SteamPlayer SteamPlayer { get; }
    public ITransportConnection Connection => SteamPlayer.transportConnection;
    public Vector3 Position => UnturnedPlayer.transform.position;
    public Team Team { get; private set; }

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
    public BinaryPlayerSave Save { get; }
    internal WarfarePlayer(Player player, ILogger<WarfarePlayer> logger)
    {
        _logger = logger;
        UnturnedPlayer = player;
        SteamPlayer = player.channel.owner;
        Steam64 = player.channel.owner.playerID.steamID;
        Save = new BinaryPlayerSave(Steam64.m_SteamID, logger);
        Save.Load();
    }

    public void UpdateTeam(Team team)
    {
        Team = team;
    }
}
