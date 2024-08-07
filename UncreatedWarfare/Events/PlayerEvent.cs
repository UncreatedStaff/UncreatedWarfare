using SDG.NetTransport;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events;
public class PlayerEvent : IPlayerEvent
{
    public required WarfarePlayer Player { get; init; }
    public CSteamID Steam64 => Player.Steam64;
    public Player PlayerObject => Player.UnturnedPlayer;
    public SteamPlayer SteamPlayer => Player.UnturnedPlayer.channel.owner;
    public SteamPlayerID PlayerId => Player.UnturnedPlayer.channel.owner.playerID;
    public ITransportConnection Connection => Player.UnturnedPlayer.channel.owner.transportConnection;
    public PlayerAnimator Animator => Player.UnturnedPlayer.animator;
    public PlayerClothing Clothing => Player.UnturnedPlayer.clothing;
    public PlayerInventory Inventory => Player.UnturnedPlayer.inventory;
    public PlayerEquipment Equipment => Player.UnturnedPlayer.equipment;
    public PlayerLife Life => Player.UnturnedPlayer.life;
    public PlayerCrafting Crafting => Player.UnturnedPlayer.crafting;
    public PlayerSkills Skills => Player.UnturnedPlayer.skills;
    public PlayerMovement Movement => Player.UnturnedPlayer.movement;
    public PlayerLook Look => Player.UnturnedPlayer.look;
    public PlayerStance Stance => Player.UnturnedPlayer.stance;
    public PlayerInput Input => Player.UnturnedPlayer.input;
    public PlayerVoice Voice => Player.UnturnedPlayer.voice;
    public PlayerInteract Interact => Player.UnturnedPlayer.interact;
    public PlayerWorkzone Workzone => Player.UnturnedPlayer.workzone;
    public PlayerQuests Quests => Player.UnturnedPlayer.quests;

    public bool MatchPlayer(Player other) => other.channel.owner.playerID.steamID.m_SteamID == Player.Steam64.m_SteamID;
    public bool MatchPlayer(SteamPlayer other) => other.playerID.steamID.m_SteamID == Player.Steam64.m_SteamID;
    public bool MatchPlayer(SteamPlayerID other) => other.steamID.m_SteamID == Player.Steam64.m_SteamID;
    public bool MatchPlayer(WarfarePlayer other) => other.Steam64 == Player.Steam64;
}

public interface IPlayerEvent
{
    WarfarePlayer Player { get; }
    CSteamID Steam64 { get; }
}