using DanielWillett.ReflectionTools;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Collections;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.Interaction.Commands;
public class CommandDispatcher : IDisposable, IHostedService, IEventListener<PlayerLeft>
{
    private readonly WarfareModule _module;
    private readonly UserPermissionStore _permissions;
    private readonly ChatService _chatService;
    private readonly IPlayerService _playerService;
    private readonly ILoopTickerFactory _tickerFactory;
    private readonly ILogger<CommandDispatcher> _logger;
    private readonly CooldownManager? _cooldownManager;
    public CommandParser Parser { get; }
    public IReadOnlyList<CommandInfo> Commands { get; }
    public CommandDispatcher(IServiceProvider serviceProvider)
    {
        _module = serviceProvider.GetRequiredService<WarfareModule>();
        _permissions = serviceProvider.GetRequiredService<UserPermissionStore>();
        _logger = serviceProvider.GetRequiredService<ILogger<CommandDispatcher>>();
        _chatService = serviceProvider.GetRequiredService<ChatService>();
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
        _tickerFactory = serviceProvider.GetRequiredService<ILoopTickerFactory>();

        _cooldownManager = serviceProvider.GetService<CooldownManager>();

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
                _logger.LogDebug("Unable to load referenced assembly {0}.", referencedAssembly);
            }
        }

        List<Type> types = Accessor.GetTypesSafe(assemblies, removeIgnored: true)
            .Where(typeof(ICommand).IsAssignableFrom)
            .ToList();

        List<CommandInfo> allCommands = new List<CommandInfo>(types.Count);
        List<CommandInfo> parentCommands = new List<CommandInfo>(types.Count + Commander.commands.Count);

        List<Type> circularReferenceBuffer = new List<Type>();

        foreach (Type commandType in types)
        {
            if (commandType.IsAbstract)
                continue;

            if (parentCommands.Any(x => x.Type == commandType))
                continue;

            circularReferenceBuffer.Add(commandType);
            CommandInfo info = new CommandInfo(commandType, _logger, GetParentInfo(commandType, allCommands, _logger, circularReferenceBuffer));
            circularReferenceBuffer.Clear();

            allCommands.Add(info);
            if (!info.IsSubCommand)
                parentCommands.Add(info);
        }

        parentCommands.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        // add vanilla commands
        parentCommands.AddRange(Commander.commands.Select(vanillaCommand => new CommandInfo(vanillaCommand)));
        
        // register redirects
        foreach (CommandInfo command in allCommands)
        {
            if (command.VanillaCommand != null || !command.Type.TryGetAttributeSafe(out RedirectCommandToAttribute redirAttribute) || redirAttribute.CommandType == null)
            {
                continue;
            }

            command.RedirectCommandInfo = allCommands.Find(x => x.Type == redirAttribute.CommandType);
            if (command.RedirectCommandInfo == null)
            {
                _logger.LogWarning("Redirect command {0} not registered.", Accessor.Formatter.Format(redirAttribute.CommandType));
            }

            command.IsExecutable = command.VanillaCommand != null || (command.RedirectCommandInfo == null && typeof(IExecutableCommand).IsAssignableFrom(command.Type));
            if (command is { IsExecutable: false, SubCommands.Length: 0, RedirectCommandInfo: null })
            {
                _logger.LogWarning("Command type {0} isn't executable and has no sub-commands, which is practically useless.", Accessor.Formatter.Format(command.Type));
            }
        }

        Commands = new ReadOnlyCollection<CommandInfo>(parentCommands);

        ChatManager.onCheckPermissions += OnChatProcessing;
        CommandWindow.onCommandWindowInputted += OnCommandInput;
        return;

        // recursively create parent info's if they don't already exist for this command
        static CommandInfo? GetParentInfo(Type commandType, List<CommandInfo> commands, ILogger logger, List<Type> circularReferenceBuffer)
        {
            if (!commandType.TryGetAttributeSafe(out SubCommandOfAttribute subCommand) || subCommand.ParentType == null || !typeof(ICommand).IsAssignableFrom(subCommand.ParentType))
                return null;

            CommandInfo? existingParentInfo = commands.FirstOrDefault(x => x.Type == subCommand.ParentType);
            if (existingParentInfo == null)
            {
                if (circularReferenceBuffer.Contains(subCommand.ParentType))
                {
                    throw new InvalidOperationException($"Circular reference detected in parent commands. {Accessor.ExceptionFormatter.Format(subCommand.ParentType)} <- ... -> {Accessor.ExceptionFormatter.Format(commandType)}.");
                }

                circularReferenceBuffer.Add(subCommand.ParentType);
                CommandInfo? parentInfo = GetParentInfo(subCommand.ParentType, commands, logger, circularReferenceBuffer);
                existingParentInfo = new CommandInfo(subCommand.ParentType, logger, parentInfo);
                if (!existingParentInfo.IsSubCommand)
                    commands.Add(existingParentInfo);
            }

            return existingParentInfo;
        }
    }
    UniTask IHostedService.StartAsync(CancellationToken token) => UniTask.CompletedTask;
    UniTask IHostedService.StopAsync(CancellationToken token) => UniTask.CompletedTask;

    /// <summary>
    /// Remove all wait tasks for a disconnected player.
    /// </summary>
    void IEventListener<PlayerLeft>.HandleEvent(PlayerLeft e, IServiceProvider serviceProvider)
    {
        foreach (CommandInfo commandType in Commands)
        {
            lock (commandType.WaitTasks)
            {
                for (int i = commandType.WaitTasks.Count - 1; i >= 0; --i)
                {
                    if (!Equals(commandType.WaitTasks[i].User, e.Player))
                        continue;

                    commandType.WaitTasks[i].MarkDisconnected();
                    commandType.WaitTasks.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>
    /// Wait for a <paramref name="commandType"/> command to be executed by <paramref name="user"/>.
    /// </summary>
    public CommandWaitTask WaitForCommand(Type commandType, ICommandUser user, TimeSpan timeout = default, CommandWaitOptions options = CommandWaitOptions.Default, CancellationToken token = default)
    {
        CommandInfo? command = FindCommand(commandType ?? throw new ArgumentNullException(nameof(commandType)));
        return WaitForCommand(
            command ?? throw new ArgumentException($"No registered command with type {Accessor.ExceptionFormatter.Format(commandType)}.", nameof(commandType)),
            user, timeout, options, token
        );
    }

    /// <summary>
    /// Wait for a <paramref name="commandType"/> command to be executed by any user.
    /// </summary>
    public CommandWaitTask WaitForCommand(Type commandType, TimeSpan timeout = default, CommandWaitOptions options = CommandWaitOptions.Default, CancellationToken token = default)
    {
        CommandInfo? command = FindCommand(commandType ?? throw new ArgumentNullException(nameof(commandType)));
        return WaitForCommand(
            command ?? throw new ArgumentException($"No registered command with type {Accessor.ExceptionFormatter.Format(commandType)}.", nameof(commandType)),
            timeout, options, token
        );
    }

    /// <summary>
    /// Wait for a <paramref name="commandType"/> command to be executed by <paramref name="user"/>.
    /// </summary>
    public CommandWaitTask WaitForCommand(CommandInfo commandType, ICommandUser user, TimeSpan timeout = default, CommandWaitOptions options = CommandWaitOptions.Default, CancellationToken token = default)
    {
        if (timeout == TimeSpan.Zero)
            timeout = TimeSpan.FromSeconds(15d);
        return new CommandWaitTask(commandType ?? throw new ArgumentNullException(nameof(commandType)), user, timeout, token, options, this, _tickerFactory);
    }

    /// <summary>
    /// Wait for a <paramref name="commandType"/> command to be executed by any user.
    /// </summary>
    public CommandWaitTask WaitForCommand(CommandInfo commandType, TimeSpan timeout = default, CommandWaitOptions options = CommandWaitOptions.Default, CancellationToken token = default)
    {
        if (timeout == TimeSpan.Zero)
            timeout = TimeSpan.FromSeconds(15d);
        return new CommandWaitTask(commandType ?? throw new ArgumentNullException(nameof(commandType)), null, timeout, token, options, this, _tickerFactory);
    }

    internal void RegisterCommandWaitTask(CommandWaitTask commandWaitTask)
    {
        if (commandWaitTask.IsCompleted)
            return;

        lock (commandWaitTask.Command.WaitTasks)
        {
            commandWaitTask.Command.WaitTasks.Add(commandWaitTask);
        }
    }


    /// <summary>
    /// Find information about a command by name.
    /// </summary>
    public CommandInfo? FindCommand(string search)
    {
        CommandInfo? cmd = CollectionUtility.StringFind(Commands.OrderByDescending(x => x.Priority).ThenBy(x => x.CommandName.Length), x => x.CommandName, search, equalsOnly: true);
        if (cmd != null)
            return cmd;

        foreach (CommandInfo command in Commands)
        {
            if (command.Aliases != null && command.Aliases.Any(x => x.Equals(search, StringComparison.InvariantCultureIgnoreCase)))
                return command;
        }

        cmd = CollectionUtility.StringFind(Commands.OrderByDescending(x => x.Priority).ThenBy(x => x.CommandName.Length), x => x.CommandName, search, equalsOnly: false);
        return cmd;
    }

    /// <summary>
    /// Find information about a command by name.
    /// </summary>
    public CommandInfo? FindCommand(Type commandType)
    {
        foreach (CommandInfo command in Commands)
        {
            if (command.Type == commandType)
            {
                return command;
            }
        }

        return null;
    }

    /// <summary>
    /// Start executing a parsed command.
    /// </summary>
    internal void ExecuteCommand(CommandInfo command, ICommandUser user, string[] args, string originalMessage)
    {
        GameThread.AssertCurrent();

        // take off common trailing slash when missing the enter key
        if (originalMessage.EndsWith('\\') /* not quoted */ && args.Length > 0 && args[^1].EndsWith('\\'))
        {
            args[^1] = args[^1][..^1];
        }

        List<CommandWaitTask>? foundTasks = null;
        if (command.WaitTasks.Count > 0)
        {
            lock (command.WaitTasks)
            {
                for (int i = command.WaitTasks.Count - 1; i >= 0; --i)
                {
                    CommandWaitTask task = command.WaitTasks[i];

                    if (task.User != null && !task.User.Equals(user))
                        continue;

                    (foundTasks ??= new List<CommandWaitTask>(1)).Add(task);
                    command.WaitTasks.RemoveAt(i);
                }
            }
        }

        foreach (CommandInfo cmd in Commands)
        {
            lock (cmd.WaitTasks)
            {
                for (int i = cmd.WaitTasks.Count - 1; i >= 0; --i)
                {
                    CommandWaitTask task = command.WaitTasks[i];
                    if ((task.Options & CommandWaitOptions.AbortOnOtherCommandExecuted) == 0 || task.User != null && !task.User.Equals(user) || foundTasks != null && foundTasks.Contains(task))
                        continue;

                    // task is removed by Abort, lock won't deadlock on same thread
                    task.Abort();
                }
            }
        }

        if (foundTasks != null && foundTasks.Exists(task => (task.Options & CommandWaitOptions.BlockOriginalExecution) != 0))
        {
            Lazy<CommandContext> contextFactory = new Lazy<CommandContext>(
                () => new CommandContext(user, CancellationToken.None, args, originalMessage, command, _module.ScopedProvider.Resolve<IServiceProvider>()),
                    LazyThreadSafetyMode.ExecutionAndPublication);

            foreach (CommandWaitTask task in foundTasks)
            {
                task.MarkCompleted(contextFactory, user);
            }
            return;
        }

        UniTask.Create(async () =>
        {
            await ExecuteCommandAsync(command, user, args, originalMessage, foundTasks, user is WarfarePlayer player ? player.DisconnectToken : CancellationToken.None);
        });
    }

    /// <summary>
    /// Execute a parsed command.
    /// </summary>
    public async UniTask ExecuteCommandAsync(CommandInfo command, ICommandUser user, string[] args, string originalMessage, List<CommandWaitTask>? waitTasks, CancellationToken token = default)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        args ??= Array.Empty<string>();
        originalMessage ??= string.Empty;

        int offset;
        if (!command.IsSubCommand)
        {
            ResolveSubCommand(ref command, args, out offset);
        }
        else
        {
            BacktrackSubCommand(command, ref args, out offset);
        }

        if (command.Type != typeof(HelpCommand) && offset < args.Length && (string.Equals(args[offset], "help", StringComparison.InvariantCultureIgnoreCase)
                                                                            || string.Equals(args[offset], "hlep", StringComparison.InvariantCultureIgnoreCase)
                                                                            || string.Equals(args[offset], "?", StringComparison.InvariantCultureIgnoreCase)))
        {
            CommandInfo? helpCommand = FindCommand(typeof(HelpCommand));
            if (helpCommand != null)
                command = helpCommand;
        }

        if (command.VanillaCommand != null)
        {
            await ExecuteVanillaCommandAsync(user, command, args, originalMessage, waitTasks, token);
            return;
        }

        await ExecuteCommandAsync(command, user, args, originalMessage, offset, waitTasks, token);
    }

    private static void BacktrackSubCommand(CommandInfo command, ref string[] args, out int offset)
    {
        offset = 0;
        for (CommandInfo? parent = command.ParentCommand; parent != null; parent = parent.ParentCommand)
        {
            ++offset;
        }

        if (offset == 0)
            return;

        string[] newArgs = new string[offset + args.Length];
        Array.Copy(args, 0, newArgs, offset, args.Length);
        for (CommandInfo subCommand = command; subCommand?.ParentCommand != null; subCommand = subCommand.ParentCommand)
        {
            newArgs[--offset] = subCommand.CommandName;
        }
    }

    private static void ResolveSubCommand(ref CommandInfo command, IReadOnlyList<string> args, out int offset)
    {
        offset = 0;
        if (command.SubCommands.Length == 0)
            return;

        for (int i = 0; i < args.Count; ++i)
        {
            string arg = args[i];
            bool found = false;

            for (int j = 0; j < command.SubCommands.Length; ++j)
            {
                CommandInfo parameter = command.SubCommands[j];
                if (!arg.Equals(parameter.CommandName, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                command = parameter;
                found = true;
                ++offset;
                break;
            }

            if (found)
                continue;

            for (int j = 0; j < command.SubCommands.Length; ++j)
            {
                CommandInfo parameter = command.SubCommands[j];
                for (int k = 0; k < parameter.Aliases.Length; ++k)
                {
                    if (!arg.Equals(parameter.Aliases[k], StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    command = parameter;
                    found = true;
                    ++offset;
                    break;
                }

                if (found)
                    break;
            }

            if (!found)
                return;
        }

        while (command.RedirectCommandInfo != null)
        {
            command = command.RedirectCommandInfo;
        }
    }

    /// <summary>
    /// Execute a vanilla command.
    /// </summary>
    private async UniTask ExecuteVanillaCommandAsync(ICommandUser user, CommandInfo commandInfo, string[] args, string originalMessage, List<CommandWaitTask>? waitTasks, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread();

        Command vanillaCommand = commandInfo.VanillaCommand!;
        if (!await _permissions.HasPermissionAsync(user, commandInfo.DefaultPermission /* vanilla commands won't have any special permission rules */, token))
        {
            await UniTask.SwitchToMainThread();
            CommonTranslations translations = _module.ScopedProvider.Resolve<TranslationInjection<CommonTranslations>>().Value;
            _chatService.Send(user, translations.NoPermissions);
            return;
        }

        if (GameThread.IsCurrent)
        {
            // this has to be done to get out of the CommandWindow.update loop since we're adding IO handlers it throws a collection modified in foreach error.
            await UniTask.WaitForEndOfFrame(_module.ServiceProvider.Resolve<WarfareLifetimeComponent>(), token);
        }
        else
        {
            await UniTask.SwitchToMainThread(token);
        }


        Type commandType = vanillaCommand.GetType();

        CommandContext ctx = new CommandContext(user, token, args, originalMessage, commandInfo, _module.ScopedProvider.Resolve<IServiceProvider>());
        VanillaCommandListener listener = new VanillaCommandListener(ctx);
        Dedicator.commandWindow.addIOHandler(listener);
        try
        {
            vanillaCommand.check(user.Steam64, vanillaCommand.command, string.Join('/', args));
            if (!ctx.Responded)
            {
                ctx.Reply(ctx.CommonTranslations.VanillaCommandDidNotRespond);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing vanilla command {0}.", Accessor.Formatter.Format(commandType));
        }
        finally
        {
            Dedicator.commandWindow.removeIOHandler(listener);

            if (waitTasks != null)
            {
                Lazy<CommandContext> contextFactory = new Lazy<CommandContext>(ctx);
                foreach (CommandWaitTask waitTask in waitTasks)
                {
                    waitTask.MarkCompleted(contextFactory, user);
                }
            }
        }
    }

    /// <summary>
    /// Execute a custom command.
    /// </summary>
    private async UniTask ExecuteCommandAsync(CommandInfo command, ICommandUser user, string[] args, string originalMessage, int argumentOffset, List<CommandWaitTask>? waitTasks, CancellationToken token = default)
    {
        SemaphoreSlim? lockTaken = command.SynchronizedSemaphore;
        if (lockTaken != null)
        {
            await lockTaken.WaitAsync(token);
        }

        CommandInfo? switchInfo = null;

        CancellationTokenSource src = new CancellationTokenSource();
        CancellationTokenSource linkedSrc = CancellationTokenSource.CreateLinkedTokenSource(token, src.Token);
        ILifetimeScope scope = _module.ScopedProvider.BeginLifetimeScope(LifetimeScopeTags.Command);
        try
        {
            IServiceProvider serviceProvider = scope.Resolve<IServiceProvider>();

            await UniTask.SwitchToMainThread();

            CommandContext ctx = new CommandContext(user, linkedSrc.Token, args, originalMessage, command, serviceProvider)
            {
                ArgumentOffset = argumentOffset
            };

            if (!command.IsExecutable)
            {
                if (command.RedirectCommandInfo != null)
                {
                    ctx.SwitchToCommand(command.RedirectCommandInfo.Type);
                }
                else if (command.Type != typeof(HelpCommand))
                {
                    ctx.SendHelp();
                }
                else
                {
                    ctx.Reply(ctx.CommonTranslations.NotImplemented);
                }
            }
            else
            {
                IExecutableCommand cmdInstance;
                try
                {
                    cmdInstance = (IExecutableCommand)ReflectionUtility.CreateInstanceFixed(serviceProvider, command.Type, [ ctx ]);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogInformation(
                        "Failed to run the \"{0}\" command because of a missing service. This could be expected if the command isn't enabled in the current layout.",
                        command.CommandName
                    );
                    _logger.LogInformation(ex.Message);
                    ctx.SendGamemodeError();
                    return;
                }

                if (cmdInstance.Context != ctx)
                {
                    cmdInstance.Context = ctx;
                }

                ctx.Command = cmdInstance;

                if (!CheckCommandOnCooldown(ctx))
                    return;

                ctx.CheckIsolatedCooldown();

                try
                {
                    await AssertPermissions(command, ctx, token);

                    await UniTask.SwitchToMainThread();

                    await cmdInstance.ExecuteAsync(token);
                    src.Cancel();
                    await UniTask.SwitchToMainThread();

                    if (!ctx.Responded)
                    {
                        ctx.SendUnknownError();
                    }
                    CheckCommandShouldStartCooldown(ctx);
                }
                catch (OperationCanceledException)
                {
                    src.Cancel();
                    await UniTask.SwitchToMainThread();

                    ctx.Reply(ctx.CommonTranslations.ErrorCommandCancelled);
                    CheckCommandShouldStartCooldown(ctx);

                    _logger.LogDebug("Execution of {0} was cancelled for {1}.", command.CommandName, ctx.CallerId.m_SteamID);
                }
                catch (ControlException)
                {
                    src.Cancel();
                    await UniTask.SwitchToMainThread();
                    if (!ctx.Responded)
                    {
                        ctx.SendUnknownError();
                    }
                    CheckCommandShouldStartCooldown(ctx);
                }
                catch (Exception ex)
                {
                    src.Cancel();
                    await UniTask.SwitchToMainThread();
                    ctx.SendUnknownError();
                    CheckCommandShouldStartCooldown(ctx);
                    _logger.LogError(ex, "Execution of {0} failed for {1}.", command.CommandName, ctx.CallerId.m_SteamID);
                }
            }

            if (waitTasks != null)
            {
                Lazy<CommandContext> contextFactory = new Lazy<CommandContext>(ctx);
                foreach (CommandWaitTask waitTask in waitTasks)
                {
                    waitTask.MarkCompleted(contextFactory, user);
                }
            }

            Type? switchCommand = ctx.SwitchCommand;
            if (switchCommand != null)
            {
                switchInfo = FindCommand(switchCommand);
                if (switchInfo == null)
                {
                    _logger.LogError("Invalid switch command type: {0} in command {1}.", Accessor.Formatter.Format(switchCommand), Accessor.Formatter.Format(command.Type));
                    return;
                }

                // special argument transformation handling for /help
                if (switchInfo.Type == typeof(HelpCommand))
                {
                    args = ctx.ParametersWithFlags;
                    if (args.Length > 0 && (args[^1].Equals("help", StringComparison.InvariantCultureIgnoreCase)
                                            || args[^1].Equals("hlep", StringComparison.InvariantCultureIgnoreCase)
                                            || args[^1].Equals("?", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        // remove ending help in cases such as '/clear inventory help' and insert old command name as first argument
                        Array.Copy(args, 0, args, 1, args.Length);
                        args[0] = command.CommandName;
                    }
                    else
                    {
                        // insert old command name as first argument
                        string[] newArgs = new string[args.Length + 1];
                        Array.Copy(args, 0, newArgs, 1, args.Length);
                        newArgs[0] = command.CommandName;
                        args = newArgs;
                    }
                    
                    // new args resemble '/help clear inventory'
                }
                else
                {
                    ctx.ArgumentOffset += switchInfo.HierarchyLevel - command.HierarchyLevel;
                    args = ctx.Parameters.ToArray();
                }

#if DEBUG
                _logger.LogDebug("Switching to command type {0} with args [ /{1} {2} ] from command type {3}.",
                    Accessor.Formatter.Format(switchInfo.Type),
                    switchInfo.CommandName,
                    args.Length != 0 ? "\"" + string.Join("\" \"", args) + "\"" : 0,
                    Accessor.Formatter.Format(command.Type)
                );
#endif
            }
        }
        finally
        {
            src.Dispose();
            linkedSrc.Dispose();
            lockTaken?.Release();
            await scope.DisposeAsync();
        }

        // run switch-to command
        if (switchInfo == null)
            return;

        await ExecuteCommandAsync(switchInfo, user, args, originalMessage, null, token);
    }

    /// <summary>
    /// Throw a <see cref="CommandContext"/> if a command doesn't have permission to be ran by <paramref name="ctx"/>.
    /// </summary>
    public static async UniTask AssertPermissions(CommandInfo command, CommandContext ctx, CancellationToken token = default)
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
        WarfarePlayer? pl = _playerService.GetOnlinePlayer(player);
        if (pl is null || string.IsNullOrWhiteSpace(text)) return;
        shouldExecuteCommand = false;
        ReadOnlySpan<char> textSpan = text;

        // remove accidental \ when pressing enter
        if (textSpan.Length > 0 && textSpan[^1] == '\\')
        {
            textSpan = textSpan[..^1];
        }

        try
        {
            if (!Parser.TryRunCommand(pl, textSpan, ref shouldList, true) && !shouldList)
            {
                CommonTranslations translations = _module.ScopedProvider.Resolve<TranslationInjection<CommonTranslations>>().Value;
                _chatService.Send(pl, translations.UnknownCommand);
            }
        }
        catch (Exception ex)
        {
            CommonTranslations translations = _module.ScopedProvider.Resolve<TranslationInjection<CommonTranslations>>().Value;
            _chatService.Send(pl, translations.UnknownError);
            _logger.LogError(ex, "Error executing player command: \"{0}\".", text);
        }
    }
    
    private void OnCommandInput(string text, ref bool shouldExecuteCommand)
    {
        try
        {
            if (shouldExecuteCommand && Parser.TryRunCommand(TerminalUser.Instance, text, ref shouldExecuteCommand, false))
            {
                shouldExecuteCommand = false;
            }
            else if (!shouldExecuteCommand)
            {
                _logger.LogError("Unknown command.");
            }
        }
        catch (Exception ex)
        {
            shouldExecuteCommand = false;
            _logger.LogError(ex, "Error executing player command: \"{0}\".", text);
        }
    }

    internal bool CheckCommandOnCooldown(CommandContext context)
    {
        if (context.Player == null
            // todo || context.Player.OnDuty()
            || _cooldownManager == null
            || context.CommandInfo == null
            || !_cooldownManager.HasCooldown(context.Player, CooldownType.Command, out Cooldown cooldown, context.CommandInfo))
        {
            return true;
        }

        if (context.Command is ICompoundingCooldownCommand compounding)
        {
            cooldown.Duration *= compounding.CompoundMultiplier;
            if (compounding.MaxCooldown > 0 && cooldown.Duration > compounding.MaxCooldown)
                cooldown.Duration = compounding.MaxCooldown;
        }

        _chatService.Send(context.Player, context.CommonTranslations.CommandCooldown, cooldown, context.CommandInfo.CommandName);
        return false;

    }

    internal void CheckCommandShouldStartCooldown(CommandContext context)
    {
        if (context.CommandCooldownTime is > 0f && context.Player != null && /* todo !context.Player.OnDuty() && */ _cooldownManager != null && context.CommandInfo != null)
        {
            _cooldownManager.StartCooldown(context.Player, CooldownType.Command, context.CommandCooldownTime.Value, context.CommandInfo);
        }

        if (!context.OnIsolatedCooldown)
        {
            if (context.IsolatedCommandCooldownTime is > 0f && context.Player != null && /* todo !context.Player.OnDuty() && */ _cooldownManager != null && context.CommandInfo != null)
                _cooldownManager.StartCooldown(context.Player, CooldownType.IsolatedCommand, context.IsolatedCommandCooldownTime.Value, context.CommandInfo);
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

        foreach (CommandInfo commandInfo in Commands)
        {
            commandInfo.SynchronizedSemaphore?.Dispose();
        }
    }
}