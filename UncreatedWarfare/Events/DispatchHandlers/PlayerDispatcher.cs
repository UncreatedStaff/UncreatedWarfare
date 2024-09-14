using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Injures;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events;
partial class EventDispatcher2
{
    /// <summary>
    /// Invoked by <see cref="DamageTool.damagePlayerRequested"/> when a player starts to get damaged. Can be cancelled.
    /// </summary>
    public void OnPlayerDamageRequested(ref DamagePlayerParameters parameters, ref bool shouldallow)
    {
        if (!shouldallow || parameters.times == 0f)
            return;

        WarfarePlayer player = _playerService.GetOnlinePlayer(parameters.player);

        DamagePlayerRequested args = new DamagePlayerRequested(in parameters, _playerService)
        {
            Player = player
        };

        // can't support async event handlers because any code calling damagePlayer
        // may expect the player to take damage or die instantly
        //  ex. hitmarkers are handled by checking which players were damaged immediately after shooting
        shouldallow = DispatchEventAsync(args, _unloadToken, allowAsync: false).GetAwaiter().GetResult();

        if (shouldallow)
            parameters = args.Parameters;
    }

    private void OnPlayerPerformingAid(Player instigator, Player target, ItemConsumeableAsset asset, ref bool shouldAllow)
    {
        WarfarePlayer medic = _playerService.GetOnlinePlayer(instigator);
        WarfarePlayer player = _playerService.GetOnlinePlayer(target);

        AidPlayerRequested args = new AidPlayerRequested
        {
            Item = AssetLink.Create(asset),
            Player = player,
            Medic = medic,
            IsRevive = false
        };

        shouldAllow = DispatchEventAsync(args, _unloadToken, allowAsync: false).GetAwaiter().GetResult();

        if (shouldAllow && args.IsRevive)
        {
            player.ComponentOrNull<PlayerInjureComponent>()?.PrepAidRevive(args);
        }
    }
}
