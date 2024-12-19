using Uncreated.Warfare.Injures;
using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("injure", "down"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugInjureCommand : IExecutableCommand
{
    public required CommandContext Context { get; init; }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        PlayerInjureComponent? injureComponent = Context.Player.ComponentOrNull<PlayerInjureComponent>();

        if (injureComponent == null)
            throw Context.SendGamemodeError();

        Player player = Context.Player.UnturnedPlayer;
        int damage = player.life.health + 1;
        DamageTool.damagePlayer(new DamagePlayerParameters(player)
        {
            cause = EDeathCause.KILL,
            limb = ELimb.SPINE,
            killer = Context.CallerId,
            direction = player.look.aim.forward,
            damage = damage,
            times = 1f,
            applyGlobalArmorMultiplier = false,
            trackKill = false,
            ragdollEffect = ERagdollEffect.NONE,
            respectArmor = false
        }, out _);

        Context.ReplyString($"Damaged player: {damage}.");

        return UniTask.CompletedTask;
    }
}