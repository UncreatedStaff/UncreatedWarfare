using DanielWillett.ReflectionTools;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Events;

namespace Uncreated.Warfare.Commands.Dispatch;
public class CommandDispatcher : IDisposable
{
    private readonly WarfareModule _module;
    private readonly UserPermissionStore _permissions;
    private ICommandUser? _currentVanillaCommandExecutor;
    public CommandParser Parser { get; }
    public IReadOnlyList<CommandType> Commands { get; }
    public CommandDispatcher(WarfareModule module, UserPermissionStore permissions)
    {
        _module = module;
        _permissions = permissions;

        Parser = new CommandParser(this);

        // discover commands
        Assembly warfareAssembly = Assembly.GetExecutingAssembly();

        List<Assembly> assemblies = [ warfareAssembly ];

        foreach (AssemblyName referencedAssembly in warfareAssembly.GetReferencedAssemblies())
        {
            try
            {
                assemblies.Add(Assembly.Load(referencedAssembly));
            }
            catch
            {
                L.LogDebug($"Unable to load referenced assembly {referencedAssembly}.");
            }
        }

        List<Type> types = Accessor.GetTypesSafe(assemblies, removeIgnored: true)
            .Where(typeof(IExecutableCommand).IsAssignableFrom)
            .ToList();

        List<CommandType> commands = new List<CommandType>(types.Count + Commander.commands.Count);

        commands.AddRange(types.Select(type => new CommandType(type)));
        commands.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        // add vanilla commands
        commands.AddRange(Commander.commands.Select(vanillaCommand => new CommandType(vanillaCommand)));

        Commands = new ReadOnlyCollection<CommandType>(commands);

        ChatManager.onCheckPermissions += OnChatProcessing;
        CommandWindow.onCommandWindowInputted += OnCommandInput;
    }

    /// <summary>
    /// Find information about a command by name.
    /// </summary>
    public CommandType? FindCommand(string search)
    {
        CommandType? cmd = F.StringFind(Commands, x => x.CommandName, x => x.Priority,
            x => x.CommandName.Length, search, descending: true, equalsOnly: true);
        if (cmd != null)
            return cmd;

        foreach (CommandType command in Commands)
        {
            if (command.Aliases != null)
            {
                if (command.Aliases.Any(x => x.Equals(search, StringComparison.InvariantCultureIgnoreCase)))
                    return command;
            }
        }
        cmd = F.StringFind(Commands, x => x.CommandName, x => x.Priority,
            x => x.CommandName.Length, search, descending: true, equalsOnly: false);

        return cmd;
    }

    /// <summary>
    /// Start executing a parsed command.
    /// </summary>
    internal void ExecuteCommand(CommandType command, ICommandUser user, string[] args, string originalMessage)
    {
        ThreadUtil.assertIsGameThread();

        // take off common trailing slash when missing the enter key
        if (args.Length > 0 && args[^1].EndsWith('\\'))
        {
            args[^1] = args[^1][..^1];
        }

        UniTask.Create(async () =>
        {
            if (command.VanillaCommand != null)
            {
                await ExecuteVanillaCommandAsync(user, command.VanillaCommand, args);
                return;
            }

            await ExecuteCommandAsync(command, user, args, originalMessage);
        });
    }

    // todo
    internal void OnLog(string message)
    {
        _currentVanillaCommandExecutor?.SendMessage("<#bfb9ac>" + message + "</color>");
    }

    /// <summary>
    /// Execute a vanilla command.
    /// </summary>
    public async UniTask ExecuteVanillaCommandAsync(ICommandUser user, Command vanillaCommand, string[] args, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread();

        if (!await _permissions.HasPermissionAsync(user, new PermissionLeaf(vanillaCommand.command, unturned: true, warfare: false) /* unturned::command */, token))
        {
            await UniTask.SwitchToMainThread();
            user.SendMessage(T.NoPermissions.Translate(user as UCPlayer));
            return;
        }

        await UniTask.SwitchToMainThread();
        _currentVanillaCommandExecutor = user;
        try
        {
            vanillaCommand.check(user.Steam64, vanillaCommand.command, string.Join('/', args));
        }
        catch (Exception ex)
        {
            L.LogError(ex);
        }
        finally
        {
            _currentVanillaCommandExecutor = null;
        }
    }

    /// <summary>
    /// Execute a custom command.
    /// </summary>
    private async UniTask ExecuteCommandAsync(CommandType command, ICommandUser user, string[] args, string originalMessage, CancellationToken token = default)
    {
        SemaphoreSlim? lockTaken = command.SynchronizedSemaphore;
        if (lockTaken != null)
        {
            await lockTaken.WaitAsync(token);
        }

        try
        {
            await UniTask.SwitchToMainThread();

            CommandContext ctx = new CommandContext(user, args, originalMessage, command, _module.ScopedProvider);

            IExecutableCommand cmdInstance = (IExecutableCommand)ActivatorUtilities.CreateInstance(_module.ScopedProvider, command.Type, [ ctx ]);
            ctx.Command = cmdInstance;

            if (!CheckCommandOnCooldown(ctx))
                return;

            ctx.CheckIsolatedCooldown();

            await AssertPermissions(command, ctx, token);

            await UniTask.SwitchToMainThread();

            try
            {
                await cmdInstance.ExecuteAsync(token);
                await UniTask.SwitchToMainThread();

                if (!ctx.Responded)
                {
                    ctx.SendUnknownError();
                }
                CheckCommandShouldStartCooldown(ctx);
            }
            catch (OperationCanceledException)
            {
                await UniTask.SwitchToMainThread();

                ctx.Reply(T.ErrorCommandCancelled);
                CheckCommandShouldStartCooldown(ctx);

                L.LogDebug($"Execution of {command.CommandName} was cancelled for {ctx.CallerId}.");
            }
            catch (ControlException)
            {
                await UniTask.SwitchToMainThread();
                if (!ctx.Responded)
                {
                    ctx.SendUnknownError();
                }
                CheckCommandShouldStartCooldown(ctx);
            }
            catch (Exception ex)
            {
                await UniTask.SwitchToMainThread();
                ctx.SendUnknownError();
                CheckCommandShouldStartCooldown(ctx);
                L.LogError($"Execution of {command.CommandName} failed for {ctx.CallerId}.");
                L.LogError(ex);
            }
        }
        finally
        {
            lockTaken?.Release();
        }
    }

    /// <summary>
    /// Throw a <see cref="CommandContext"/> if a command doesn't have permission to be ran by <paramref name="ctx"/>.
    /// </summary>
    public static async UniTask AssertPermissions(CommandType command, CommandContext ctx, CancellationToken token = default)
    {
        if (command.OtherPermissionsAreAnd)
        {
            if (command.DefaultPermission.Valid)
            {
                await ctx.AssertPermissions(command.DefaultPermission, token);
            }

            if (command.OtherPermissions.Length > 0)
            {
                await ctx.AssertPermissionsAnd(token, command.OtherPermissions);
            }
        }
        else
        {
            if (command.DefaultPermission.Valid)
            {
                if (!await ctx.HasPermission(command.DefaultPermission, token))
                {
                    if (command.OtherPermissions.Length == 0)
                        throw ctx.SendNoPermission(command.DefaultPermission);

                    await ctx.AssertPermissionsOr(token, command.OtherPermissions);
                }
            }
            else
            {
                await ctx.AssertPermissionsOr(token, command.OtherPermissions);
            }
        }
    }
    private void OnChatProcessing(SteamPlayer player, string text, ref bool shouldExecuteCommand, ref bool shouldList)
    {
        UCPlayer? pl = UCPlayer.FromSteamPlayer(player);
        if (pl is null || string.IsNullOrWhiteSpace(text)) return;
        shouldExecuteCommand = false;
        // remove accidental \
        if (text.EndsWith("\\", StringComparison.Ordinal))
            text = text.Substring(0, text.Length - 1);
        if (!Parser.TryRunCommand(pl, text, ref shouldList, true) && !shouldList)
        {
            player.SendChat(T.UnknownCommand);
        }
    }
    private void OnCommandInput(string text, ref bool shouldExecuteCommand)
    {
        if (shouldExecuteCommand && Parser.TryRunCommand(null! /* todo make console user */, text, ref shouldExecuteCommand, false))
            shouldExecuteCommand = false;
        else if (!shouldExecuteCommand)
        {
            L.Log("Unknown command.", ConsoleColor.Red);
        }
    }
    internal bool CheckCommandOnCooldown(CommandContext context)
    {
        if (context.Player == null
            || context.Player.OnDuty()
            || !CooldownManager.IsLoaded
            || context.CommandInfo == null
            || !CooldownManager.HasCooldown(context.Player, CooldownType.Command, out Cooldown cooldown, context.CommandInfo))
        {
            return true;
        }

        if (context.Command is ICompoundingCooldownCommand compounding)
        {
            cooldown.Duration *= compounding.CompoundMultiplier;
            if (compounding.MaxCooldown > 0 && cooldown.Duration > compounding.MaxCooldown)
                cooldown.Duration = compounding.MaxCooldown;
        }

        context.Player.SendChat(T.CommandCooldown, cooldown, context.CommandInfo.CommandName);
        return false;

    }
    internal void CheckCommandShouldStartCooldown(CommandContext context)
    {
        if (context.CommandCooldownTime is > 0f && context.Player != null && !context.Player.OnDuty() && CooldownManager.IsLoaded && context.CommandInfo != null)
            CooldownManager.StartCooldown(context.Player, CooldownType.Command, context.CommandCooldownTime.Value, context.CommandInfo);
        if (!context.OnIsolatedCooldown)
        {
            if (context.IsolatedCommandCooldownTime is > 0f && context.Player != null && !context.Player.OnDuty() && CooldownManager.IsLoaded && context.CommandInfo != null)
                CooldownManager.StartCooldown(context.Player, CooldownType.IsolatedCommand, context.IsolatedCommandCooldownTime.Value, context.CommandInfo);
        }
        else if (context.IsolatedCommandCooldownTime is > 0f)
        {
            if (context.Command is ICompoundingCooldownCommand compounding)
            {
                context.IsolatedCooldown!.Duration *= compounding.CompoundMultiplier;
                if (compounding.MaxCooldown > 0 && context.IsolatedCooldown.Duration > compounding.MaxCooldown)
                    context.IsolatedCooldown.Duration = compounding.MaxCooldown;
            }
            else context.IsolatedCooldown!.Duration = context.IsolatedCommandCooldownTime.Value;
        }
    }

    void IDisposable.Dispose()
    {
        ChatManager.onCheckPermissions -= OnChatProcessing;
        CommandWindow.onCommandWindowInputted -= OnCommandInput;

        foreach (CommandType commandInfo in Commands)
        {
            commandInfo.SynchronizedSemaphore?.Dispose();
        }
    }
}
