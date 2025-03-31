using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Vehicles.Spawners;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Commands;

[Command("kick", "remove", "k"), SubCommandOf(typeof(VehicleCommand))]
internal sealed class VehicleKickCommand : IExecutableCommand
{
    private readonly VehicleSpawnerService _spawnerService;
    private readonly ZoneStore _zoneStore;
    private readonly ITeamManager<Team> _teamManager;
    private readonly VehicleTranslations _translations;
    private readonly IPlayerService _playerService;
    private readonly VehicleService _vehicleService;
    private readonly VehicleSeatRestrictionService _seatRestrictions;
    private readonly ChatService _chatService;
    private readonly IUserDataService _userDataService;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public VehicleKickCommand(
        VehicleSpawnerService spawnerService,
        ZoneStore zoneStore,
        TranslationInjection<VehicleTranslations> translations,
        ITeamManager<Team> teamManager,
        IPlayerService playerService,
        IUserDataService userDataService,
        ChatService chatService,
        VehicleService vehicleService,
        VehicleSeatRestrictionService seatRestrictions)
    {
        _spawnerService = spawnerService;
        _zoneStore = zoneStore;
        _teamManager = teamManager;
        _playerService = playerService;
        _userDataService = userDataService;
        _chatService = chatService;
        _vehicleService = vehicleService;
        _seatRestrictions = seatRestrictions;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        Context.AssertArgs(1);

        InteractableVehicle? vehicleTarget = VehicleTargetCommandHelper.GetVehicleTarget(Context, _spawnerService, _zoneStore);

        if (vehicleTarget == null)
            throw Context.Reply(_translations.VehicleMustBeLookingAtLinkedVehicle);

        Team vehicleTeam = _teamManager.GetTeam(vehicleTarget.lockedGroup);
        if (!vehicleTeam.IsValid || !vehicleTeam.IsFriendly(Context.Player.UnturnedPlayer.quests.groupID))
        {
            throw Context.Reply(_translations.VehicleNotOnSameTeam, vehicleTeam.Faction);
        }

        if (vehicleTarget.lockedOwner.m_SteamID != Context.CallerId.m_SteamID)
        {
            IPlayer? offlinePlayer = vehicleTarget.lockedOwner.m_SteamID == 0
                ? null
                : await _playerService.GetOfflinePlayer(vehicleTarget.lockedOwner, _userDataService, token);
            throw Context.Reply(_translations.VehicleLinkedVehicleNotOwnedByCaller, offlinePlayer);
        }

        // vehicle kick <seat ...>

        int seat = await VehicleTargetCommandHelper.GetSeat(Context, vehicleTarget, _translations, token: token);
        await UniTask.SwitchToMainThread(token);

        if (seat < 0)
        {
            throw Context.Reply(_translations.VehicleSeatNotValidText, Context.GetRange(0)!);
        }

        WarfarePlayer? kickedPlayer = _playerService.GetOnlinePlayerOrNull(vehicleTarget.passengers[seat].player);
        if (kickedPlayer == null || kickedPlayer.Equals(Context.Caller))
        {
            throw Context.Reply(_translations.VehicleSeatNotOccupied, seat + 1);
        }

        Vector3 vehiclePosition = vehicleTarget.transform.position;
        bool wantsFullKick = (Context.MatchFlag('r', "remove")
                              || Context.MatchFlag('k', "kick"))
                             && (vehicleTarget.asset.engine is not EEngine.PLANE and not EEngine.HELICOPTER
                                 || Mathf.Abs(vehicleTarget.ReplicatedSpeed) <= 0.15f
                                 || _zoneStore.IsInMainBase(vehiclePosition)
                                );

        if (wantsFullKick || !await _vehicleService.TryMovePlayerToEmptySeat(kickedPlayer))
        {
            VehicleManager.forceRemovePlayer(vehicleTarget, kickedPlayer.Steam64);
        }

        vehicleTarget.lastSeat = Time.realtimeSinceStartup;

        _seatRestrictions.AddInteractCooldown(kickedPlayer, vehicleTarget);

        _chatService.Send(kickedPlayer, _translations.VehicleOwnerKickedDM, vehicleTarget.asset, Context.Player, seat + 1);
        throw Context.Reply(_translations.VehicleKickedPlayer, vehicleTarget.asset, kickedPlayer, seat + 1);
    }
}