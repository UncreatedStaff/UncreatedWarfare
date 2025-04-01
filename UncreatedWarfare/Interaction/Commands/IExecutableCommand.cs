using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Interaction.Commands;

/// <summary>
/// A command that can be executed.
/// </summary>
[Translatable("Command")]
public interface ICommand;

/// <summary>
/// A command that can be executed.
/// </summary>
public interface IExecutableCommand : ICommand
{
#nullable disable
    /// <summary>
    /// The context used to execute the command.
    /// </summary>
    CommandContext Context { get; init; }
#nullable restore
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