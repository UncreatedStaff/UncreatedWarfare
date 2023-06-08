using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;

namespace Uncreated.Warfare.Commands.CommandSystem;
public abstract class Command : IExecutableCommand
{
    protected const string Default = CommandInteraction.Default;
    private readonly string _commandName;
    private readonly int _priority;
    private readonly EAdminType _allowedUsers;
    private readonly List<string> _aliases = new List<string>(0);
    public readonly IReadOnlyList<string> Aliases;
    SemaphoreSlim? IExecutableCommand.Semaphore { get; set; }
    bool IExecutableCommand.ExecuteAsynchronously => false;
    public CommandStructure? Structure { get; set; }
    IReadOnlyList<string> IExecutableCommand.Aliases => Aliases;
    public string CommandName => _commandName;
    public EAdminType AllowedPermissions => _allowedUsers;
    public int Priority => _priority;
    bool IExecutableCommand.Synchronize => false;
    protected Command(string command, EAdminType allowedUsers = 0, int priority = 0)
    {
        _commandName = command;
        _allowedUsers = allowedUsers;
        _priority = priority;
        Aliases = _aliases.AsReadOnly();
    }
    Task IExecutableCommand.Execute(CommandInteraction ctx, CancellationToken token) => throw new NotImplementedException();
    protected void AddAlias(string alias) => _aliases.Add(alias);
    /// <summary>Runs before <see cref="Execute"/>. Sends "no_permissions" translation to the player if it returns <see langword="false"/>. This could also be done in <see cref="Execute"/> if desired.</summary>
    /// <returns><see langword="true"/> if the player has permission to run the command. Otherwise returns <see langword="false"/>.</returns>
    public virtual bool CheckPermission(CommandInteraction ctx)
    {
        if (ctx.IsConsole || ctx.Caller!.Player.channel.owner.isAdmin) return true;
        return ctx.Caller.PermissionCheck(_allowedUsers, PermissionComparison.AtLeast);
    }
    public abstract void Execute(CommandInteraction ctx);
    public CommandInteraction SetupCommand(UCPlayer? caller, string[] args, string message, bool keepSlash)
    {
        if (!keepSlash && args.Length > 0)
        {
            ref string end = ref args[args.Length - 1];
            // removes the accidental ending backslash from the last argument
            if (end.Length > 1 && end.EndsWith("\\", StringComparison.Ordinal))
                end = end.Substring(0, end.Length - 1);
        }

        return new CommandInteraction(new CommandInteraction.ContextData(caller, args, message), this);
    }
}

public abstract class AsyncCommand : IExecutableCommand
{
    protected const string Default = CommandInteraction.Default;
    private readonly string _commandName;
    private readonly bool _sync;
    private readonly int _priority;
    private readonly EAdminType _allowedUsers;
    private readonly List<string> _aliases = new List<string>(0);
    public readonly IReadOnlyList<string> Aliases;
    IReadOnlyList<string> IExecutableCommand.Aliases => Aliases;
    public string CommandName => _commandName;
    bool IExecutableCommand.ExecuteAsynchronously => true;
    public CommandStructure? Structure { get; set; }
    public EAdminType AllowedPermissions => _allowedUsers;
    SemaphoreSlim? IExecutableCommand.Semaphore { get; set; }
    public int Priority => _priority;
    bool IExecutableCommand.Synchronize => _sync;
    protected AsyncCommand(string command, EAdminType allowedUsers = 0, int priority = 0, bool sync = false)
    {
        _commandName = command;
        _allowedUsers = allowedUsers;
        _priority = priority;
        Aliases = _aliases.AsReadOnly();
        _sync = sync;
    }
    protected void AddAlias(string alias) => _aliases.Add(alias);
    /// <summary>Runs before <see cref="Execute"/>. Sends "no_permissions" translation to the player if it returns <see langword="false"/>. This could also be done in <see cref="Execute"/> if desired.</summary>
    /// <returns><see langword="true"/> if the player has permission to run the command. Otherwise returns <see langword="false"/>.</returns>
    public virtual bool CheckPermission(CommandInteraction ctx)
    {
        if (ctx.IsConsole || ctx.Caller.Player.channel.owner.isAdmin) return true;
        return ctx.Caller.PermissionCheck(_allowedUsers, PermissionComparison.AtLeast);
    }
    void IExecutableCommand.Execute(CommandInteraction ctx) => throw new NotImplementedException();
    public abstract Task Execute(CommandInteraction ctx, CancellationToken token);
    public CommandInteraction SetupCommand(UCPlayer? caller, string[] args, string message, bool keepSlash)
    {
        if (!keepSlash && args.Length > 0)
        {
            ref string end = ref args[args.Length - 1];
            // removes the accidental ending backslash from the last argument
            if (end.Length > 1 && end.EndsWith("\\", StringComparison.Ordinal))
                end = end.Substring(0, end.Length - 1);
        }

        return new CommandInteraction(new CommandInteraction.ContextData(caller, args, message), this);
    }
}
