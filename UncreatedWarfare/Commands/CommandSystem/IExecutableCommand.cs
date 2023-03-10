using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;

namespace Uncreated.Warfare.Commands.CommandSystem;

[Translatable("Command")]
public interface IExecutableCommand
{
    string CommandName { get; }
    EAdminType AllowedPermissions { get; }
    SemaphoreSlim? Semaphore { get; internal set; }
    bool ExecuteAsynchronously { get; }
    /// <summary>Higher numbers gets priority. 0 is default.</summary>
    int Priority { get; }
    /// <summary>Only allow one command to execute at once.</summary>
    bool Synchronize { get; }
    /// <exception cref="NotImplementedException"/>
    Task Execute(CommandInteraction interaction, CancellationToken token);
    /// <exception cref="NotImplementedException"/>
    void Execute(CommandInteraction interaction);
    bool CheckPermission(CommandInteraction ctx);
    IReadOnlyList<string>? Aliases { get; }
    CommandInteraction SetupCommand(UCPlayer? caller, string[] args, string message, bool keepSlash);
    CommandStructure? Structure { get; set; }
}

public interface IInteractableCommand : IExecutableCommand, _Exception
{
    bool Handled { get; }
}