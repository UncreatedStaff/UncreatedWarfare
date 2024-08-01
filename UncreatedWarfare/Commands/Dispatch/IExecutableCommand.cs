namespace Uncreated.Warfare.Commands.Dispatch;

/// <summary>
/// A command that can be executed.
/// </summary>
[Translatable("Command")]
public interface IExecutableCommand
{
    /// <summary>
    /// The context used to execute the command.
    /// </summary>
    CommandContext Context { set; }

    /// <summary>
    /// Actually execute the command.
    /// </summary>
    /// <exception cref="CommandContext"/>
    UniTask ExecuteAsync(CancellationToken token);
}

/// <summary>
/// Allows for a compounding cooldown on a command. Used for request spamming.
/// </summary>
public interface ICompoundingCooldownCommand : IExecutableCommand
{
    float CompoundMultiplier { get; }
    float MaxCooldown { get; }
}