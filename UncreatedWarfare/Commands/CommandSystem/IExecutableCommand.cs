using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;

namespace Uncreated.Warfare.Commands.CommandSystem;
public interface IExecutableCommand
{
    string CommandName { get; }
    EAdminType AllowedPermissions { get; }
    /// <summary>Higher numbers gets priority. 0 is default.</summary>
    int Priority { get; }
    Task Execute(CommandInteraction interaction, CancellationToken token);
    bool CheckPermission(CommandInteraction ctx);
    IReadOnlyList<string>? Aliases { get; }
    CommandInteraction SetupCommand(UCPlayer? caller, string[] args, string message, bool keepSlash);
}

public interface IInteractableCommand : IExecutableCommand, _Exception
{
    bool Handled { get; }
}