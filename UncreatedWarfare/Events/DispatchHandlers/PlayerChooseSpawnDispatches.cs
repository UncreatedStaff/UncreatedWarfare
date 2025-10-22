using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Saves;

namespace Uncreated.Warfare.Events;
partial class EventDispatcher
{
    private void OnPlayerChooseSpawnAfterLogin(SteamPlayerID playerID, ref Vector3 point, ref float yaw, ref EPlayerStance initialStance, ref bool needsNewSpawnpoint)
    {
        Layout? currentLayout = WarfareModule.Singleton.IsLayoutActive() ? WarfareModule.Singleton.GetActiveLayout() : null;

        BinaryPlayerSave playerSave = new BinaryPlayerSave(playerID.steamID, _logger);
        playerSave.Load();

        bool firstTimeJoiningServer = !playerSave.WasReadFromFile;
        bool joiningIntoNewRound = currentLayout != null && playerSave.LastGameId != currentLayout.LayoutId;
        bool needsNewSpawnPointOverride = needsNewSpawnpoint || playerSave.ShouldRespawnOnJoin;

        PlayerChooseSpawnAfterLogin args = new PlayerChooseSpawnAfterLogin
        {
            PlayerID = playerID,
            PlayerSave = playerSave,
            FirstTimeJoiningServer = firstTimeJoiningServer,
            JoiningIntoNewRound = joiningIntoNewRound,
            NeedsNewSpawnPoint = needsNewSpawnPointOverride,
            SpawnPoint = point,
            Yaw = yaw,
            InitialStance = initialStance,
        };

        _ = DispatchEventAsync(args, _unloadToken, allowAsync: false);

        point = args.SpawnPoint;
        yaw = args.Yaw;
        initialStance = args.InitialStance;
        needsNewSpawnpoint = args.NeedsNewSpawnPoint;   
    }
    private void OnPlayerChooseSpawnAfterDeath(PlayerLife sender, bool wantsToSpawnAtHome, ref Vector3 position, ref float yaw)
    {
        WarfarePlayer player = _playerService.GetOnlinePlayer(sender.player);

        PlayerChooseSpawnAfterDeath args = new PlayerChooseSpawnAfterDeath
        {
            Player = player,
            WantsToRespawnAtBedroll = wantsToSpawnAtHome,
            SpawnPoint = position,
            Yaw = yaw
        };

        _ = DispatchEventAsync(args, _unloadToken, allowAsync: false);

        position = args.SpawnPoint;
        yaw = args.Yaw;
    }
}
