using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles.Spawners;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Commands;

[Command("give", "transfer", "g"), SubCommandOf(typeof(VehicleCommand))]
internal sealed class VehicleGiveCommand : IExecutableCommand
{
    private readonly VehicleSpawnerService _spawnerService;
    private readonly ZoneStore _zoneStore;
    private readonly ITeamManager<Team> _teamManager;
    private readonly VehicleTranslations _translations;
    private readonly IPlayerService _playerService;
    private readonly AssetConfiguration _assetConfiguration;
    private readonly ChatService _chatService;
    private readonly IUserDataService _userDataService;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public VehicleGiveCommand(
        VehicleSpawnerService spawnerService,
        ZoneStore zoneStore,
        TranslationInjection<VehicleTranslations> translations,
        ITeamManager<Team> teamManager,
        IPlayerService playerService,
        IUserDataService userDataService,
        AssetConfiguration assetConfiguration,
        ChatService chatService)
    {
        _spawnerService = spawnerService;
        _zoneStore = zoneStore;
        _teamManager = teamManager;
        _playerService = playerService;
        _userDataService = userDataService;
        _assetConfiguration = assetConfiguration;
        _chatService = chatService;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

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

        // vehicle give <player>
        (_, WarfarePlayer? onlinePlayer) = await Context.TryGetPlayer(0, remainder: true, searchType: PlayerNameType.NickName);

        if (onlinePlayer == null || !onlinePlayer.Team.IsFriendly(vehicleTeam))
            throw Context.SendPlayerNotFound();

        await UniTask.SwitchToMainThread(token);

        if (!onlinePlayer.IsOnline)
            throw new OperationCanceledException();

        VehicleManager.ServerSetVehicleLock(vehicleTarget, onlinePlayer.Steam64, onlinePlayer.Team.GroupId, true);

        if (_assetConfiguration.GetAssetLink<EffectAsset>("Effects:UnlockVehicleSound").TryGetAsset(out EffectAsset? effect))
            EffectUtility.TriggerEffect(effect, EffectManager.SMALL, vehicleTarget.transform.position, true);

        _chatService.Send(onlinePlayer, _translations.VehicleGivenDm, vehicleTarget.asset, Context.Player);
        Context.Reply(_translations.VehicleGiven, vehicleTarget.asset, onlinePlayer);
    }
}