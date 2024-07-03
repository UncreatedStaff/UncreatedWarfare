using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;

namespace Uncreated.Warfare.Events;
public class PlayerEvent
{
    public required UCPlayer Player { get; init; }
    public CSteamID Steam64 => Player.CSteamID;
    public Player PlayerObject => Player.Player;
    public SteamPlayer SteamPlayer => Player.Player.channel.owner;
    public SteamPlayerID PlayerId => Player.Player.channel.owner.playerID;
    public ITransportConnection Connection => Player.Player.channel.owner.transportConnection;
    public PlayerAnimator Animator => Player.Player.animator;
    public PlayerClothing Clothing => Player.Player.clothing;
    public PlayerInventory Inventory => Player.Player.inventory;
    public PlayerEquipment Equipment => Player.Player.equipment;
    public PlayerLife Life => Player.Player.life;
    public PlayerCrafting Crafting => Player.Player.crafting;
    public PlayerSkills Skills => Player.Player.skills;
    public PlayerMovement Movement => Player.Player.movement;
    public PlayerLook Look => Player.Player.look;
    public PlayerStance Stance => Player.Player.stance;
    public PlayerInput Input => Player.Player.input;
    public PlayerVoice Voice => Player.Player.voice;
    public PlayerInteract Interact => Player.Player.interact;
    public PlayerWorkzone Workzone => Player.Player.workzone;
    public PlayerQuests Quests => Player.Player.quests;

    public bool MatchPlayer(Player other) => other.channel.owner.playerID.steamID.m_SteamID == Player.Steam64;
    public bool MatchPlayer(SteamPlayer other) => other.playerID.steamID.m_SteamID == Player.Steam64;
    public bool MatchPlayer(SteamPlayerID other) => other.steamID.m_SteamID == Player.Steam64;
    public bool MatchPlayer(UCPlayer other) => other.Steam64 == Player.Steam64;
}