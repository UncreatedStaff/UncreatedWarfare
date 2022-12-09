using SDG.Unturned;
using Steamworks;
using System;

namespace Uncreated.Warfare.Events.Players;
public class PlayerDied : PlayerEvent
{
    public EDeathCause Cause { get; internal set; }
    public ELimb Limb { get; internal set; }
    public UCPlayer? Killer { get; internal set; }
    public CSteamID Instigator { get; internal set; }
    public bool WasTeamkill { get; internal set; }
    public ulong DeadTeam { get; internal set; }
    public ulong KillerTeam { get; internal set; }
    public Guid PrimaryAsset { get; internal set; }
    public Guid SecondaryItem { get; internal set; }
    public Guid TurretVehicleOwner { get; internal set; }
    public bool PrimaryAssetIsVehicle { get; internal set; }
    public float KillDistance { get; internal set; }
    public string? KillerKitName { get; internal set; }
    public string? PlayerKitName { get; internal set; }
    public Deaths.DeathMessageArgs LocalizationArgs { get; internal set; }
    public UCPlayer? DriverAssist { get; internal set; }
    public InteractableVehicle? ActiveVehicle { get; internal set; }
    public PlayerDied(UCPlayer player) : base(player)
    {
    }
}
