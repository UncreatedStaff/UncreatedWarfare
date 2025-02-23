using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System.Reflection;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal sealed class UseableGunReceiveChangeFiremode : IHarmonyPatch
{
    private static MethodInfo? _target;

    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = typeof(UseableGun).GetMethod(nameof(UseableGun.ReceiveChangeFiremode),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (_target != null)
        {
            patcher.Patch(_target, prefix: Accessor.GetMethod(Prefix));
            logger.LogDebug("Patched {0} for change firemode event.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition(nameof(UseableGun.ReceiveChangeFiremode))
                .DeclaredIn<UseableGun>(isStatic: false)
                .WithParameter<EFiremode>("newFiremode")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Prefix));
        logger.LogDebug("Unpatched {0} for change firemode event.", _target);
        _target = null;
    }

    private static bool _isInPatch;

    private static bool Prefix(UseableGun __instance, EFiremode newFiremode)
    {
        if (_isInPatch)
            return true;

        IPlayerService playerService = WarfareModule.Singleton.ServiceProvider.Resolve<IPlayerService>();

        WarfarePlayer player = playerService.GetOnlinePlayer(__instance.player);

        ItemJar? item = player.GetHeldItem(out _);
        ItemGunAsset? gun = item?.GetAsset<ItemGunAsset>();
        if (gun == null)
            return false;

        ChangeFiremodeRequested args = new ChangeFiremodeRequested
        {
            Player = player,
            CurrentFiremode = (EFiremode)item!.item.state[11],
            Firemode = newFiremode,
            Asset = gun,
            Useable = __instance,
            Item = item
        };

        EventContinuations.Dispatch(args, WarfareModule.EventDispatcher, player.DisconnectToken, out bool shouldAllow, static args =>
        {
            if (!args.Player.IsOnline
                || args.Item != args.Player.GetHeldItem(out _))
            {
                return;
            }

            EFiremode firemode = (EFiremode)args.Item.item.state[11];
            if (firemode == args.Firemode)
                return;

            if (args.Player.UnturnedPlayer.equipment.useable is not UseableGun gun)
                return;

            _isInPatch = true;
            try
            {
                gun.ReceiveChangeFiremode(args.Firemode);
            }
            finally
            {
                _isInPatch = false;
            }
        }, _ => true);

        return shouldAllow;
    }
}