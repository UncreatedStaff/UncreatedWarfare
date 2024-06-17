using SDG.Unturned;
using System;
using System.Threading;
using Uncreated.Warfare.Commands.CommandSystem;

namespace Uncreated.Warfare.Commands.Dispatch;
public class CommandType
{
    /// <summary>
    /// Type that contains the code for the command.
    /// </summary>
    public Type Type { get; set; }

    /// <summary>
    /// Name of the command.
    /// </summary>
    /// <remarks>Example: 'home' for /home.</remarks>
    public string CommandName { get; set; }

    /// <summary>
    /// Higher numbers will be executed over lower numbers.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Aliases for <see cref="CommandName"/>.
    /// </summary>
    public string[] Aliases { get; set; }

    /// <summary>
    /// Optional reference to the vanilla <see cref="Command"/>.
    /// </summary>
    public Command? VanillaCommand { get; set; }

    /// <summary>
    /// Information about how to display the command in /help.
    /// </summary>
    public CommandStructure Structure { get; set; }

    /// <summary>
    /// Commands marked as synchronized all use this semaphore to synchronize their execution.
    /// </summary>
    public SemaphoreSlim? SynchronizedSemaphore { get; set; }

    /// <summary>
    /// Create a new vanilla command.
    /// </summary>
    internal CommandType(Command vanillaCommand)
    {
        Type = vanillaCommand.GetType();
        VanillaCommand = vanillaCommand;
        CommandName = vanillaCommand.command;
        Structure = new CommandStructure
        {
            Description = vanillaCommand.info
        };
    }

    /// <summary>
    /// Create a new custom command.
    /// </summary>
    /// <param name="classType">The type containing the command's code. Must implement <see cref="IExecutableCommand"/>.</param>
    internal CommandType(Type classType)
    {
        if (!typeof(IExecutableCommand).IsAssignableFrom(classType))
        {
            throw new ArgumentException("Must implement IExecutableCommand", nameof(classType));
        }

        Type = classType;
        // todo use an attribute
        CommandName = classType.Name;
    }
}
