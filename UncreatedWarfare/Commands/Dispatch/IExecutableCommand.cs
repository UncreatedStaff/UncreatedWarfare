using Cysharp.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Commands.Dispatch;

/// <summary>
/// A command that can be executed.
/// </summary>
[Translatable("Command")]
public interface IExecutableCommand
{
    /// <summary>
    /// Actually execute the command.
    /// </summary>
    /// <exception cref="CommandContext"/>
    UniTask Execute(CancellationToken token);

    /// <summary>
    /// Check if a user can run this command. More detailed checking can be done in the command itself.
    /// </summary>
    /// <returns><see langword="true"/> if the user is allowed to run the command, otherwise <see langword="false"/>.</returns>
    ValueTask<bool> CheckPermission(CancellationToken token);
}

/// <summary>
/// Allows for a compounding cooldown on a command. Used for request spamming.
/// </summary>
public interface ICompoundingCooldownCommand : IExecutableCommand
{
    float CompoundMultiplier { get; }
    float MaxCooldown { get; }
}