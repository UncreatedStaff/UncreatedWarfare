using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Deaths;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Models.GameData;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Players;
public class PlayerDied : PlayerEvent
{
    private DamagePlayerParameters _parameters;

    /// <summary>
    /// Properties for how players are damaged.
    /// </summary>
    public ref readonly DamagePlayerParameters Parameters => ref _parameters;

    public EDeathCause Cause
    {
        get => _parameters.cause;
        internal set => _parameters.cause = value;
    }

    public EDeathCause MessageCause { get; internal set; }

    public ELimb Limb
    {
        get => _parameters.limb;
        internal set => _parameters.limb = value;
    }

    public WarfarePlayer? Killer { get; internal set; }

    public CSteamID Instigator
    {
        get => _parameters.killer;
        internal set => _parameters.killer = value;
    }

    public bool WasTeamkill { get; internal set; }
    public bool ThirdPartyAtFault { get; internal set; }
    public bool WasSuicide { get; internal set; }
    public bool WasBleedout { get; internal set; }
    public bool WasEffectiveKill => !WasSuicide && !WasTeamkill;
    public Team DeadTeam { get; internal set; }
    public Team? KillerTeam { get; internal set; }
    public Team? ThirdPartyTeam { get; internal set; }
    public IAssetLink<Asset>? PrimaryAsset { get; internal set; }
    public IAssetLink<Asset>? SecondaryAsset { get; internal set; }
    public IAssetLink<VehicleAsset>? TurretVehicleOwner { get; internal set; }
    public float KillDistance { get; internal set; }
    public string? KillerKitName { get; internal set; }
    public string? PlayerKitName { get; internal set; }
    public string? DefaultMessage { get; internal set; }
    public string? MessageKey { get; internal set; }
    public Class? KillerClass { get; internal set; }
    public Branch? KillerBranch { get; internal set; }
    public DeathFlags MessageFlags { get; internal set; }
    public WarfarePlayer? DriverAssist { get; internal set; }
    public WarfarePlayer? ThirdParty { get; internal set; }
    public CSteamID? ThirdPartyId { get; internal set; }
    public InteractableVehicle? ActiveVehicle { get; internal set; }
    public Vector3 Point { get; internal set; }
    public Vector3 KillerPoint { get; internal set; }
    public Vector3 ThirdPartyPoint { get; internal set; }
    public SessionRecord? Session { get; internal set; }
    public SessionRecord? KillerSession { get; internal set; }
    public SessionRecord? ThirdPartySession { get; internal set; }
    public float TimeDeployed { get; internal set; }

    public bool IsGuaranteedCheaterKill { get; set; }
    public bool WillBan { get; set; }

    public PlayerDied(in DamagePlayerParameters parameters)
    {
        _parameters = parameters;
    }
}
