using System;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Models.Stats;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("crash"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugCrashServerCommand : IExecutableCommand
{
    private readonly DatabaseStatsBuffer _buffer;
    public required CommandContext Context { get; init; }

    public DebugCrashServerCommand(DatabaseStatsBuffer buffer)
    {
        _buffer = buffer;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        Vector3 startPos = Context.Player.Position;

        Context.ReplyString("Prone to stop.");

        VehicleAsset quad = Assets.find<VehicleAsset>(new Guid("f65dec9bc1484a1fae6dbd6c10c51124"));

        int ct = 0;
        while (Context.Player.UnturnedPlayer.stance.stance is EPlayerStance.STAND or EPlayerStance.SITTING)
        {
            Console.WriteLine(++ct);
            InteractableVehicle vehicle = VehicleManager.spawnVehicleV2(quad, startPos, Quaternion.identity, new Color32(255, 255, 255, 255));

            await UniTask.Delay(250, cancellationToken: token);
            vehicle.askDamage((ushort)(vehicle.health + 1), false);

            await UniTask.Delay(100, cancellationToken: token);
            if (Context.Player.UnturnedPlayer.life.isDead)
            {
                Context.Player.UnturnedPlayer.life.ReceiveRespawnRequest(false);
            }

            await UniTask.Delay(100, cancellationToken: token);
            Context.Player.Position = startPos;

            await UniTask.Delay(RandomUtility.GetInteger(25, 400), cancellationToken: token);
        }

        throw Context.Defer();
    }
}