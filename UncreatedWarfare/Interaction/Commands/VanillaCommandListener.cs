using Microsoft.Extensions.DependencyInjection;
using System;

namespace Uncreated.Warfare.Interaction.Commands;

/// <summary>
/// Handles rerouting terminal output from vanilla server commands to chat.
/// </summary>
internal class VanillaCommandListener : ICommandInputOutput
{
    private readonly CommandContext _context;
    private readonly ILogger? _logger;
    private readonly ChatService? _chatService;

    public VanillaCommandListener(CommandContext context)
    {
        _context = context;

        _logger = context.Caller.IsTerminal
            ? context.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(context.CommandInfo.Type)
            : null;

        _chatService = !context.Caller.IsTerminal
            ? context.ServiceProvider.GetRequiredService<ChatService>()
            : null;
    }

    public void outputInformation(string information)
    {
        if (!_context.Caller.IsTerminal)
        {
            _context.ReplyString(information, new Color32(191, 185, 172, 255));
        }
        else
        {
            _logger!.LogInformation(information);
        }
    }

    public void outputWarning(string warning)
    {
        if (!_context.Caller.IsTerminal)
        {
            _context.ReplyString(warning, new Color32(168, 145, 138, 255));
        }
        else
        {
            _logger!.LogWarning(warning);
        }
    }

    public void outputError(string error)
    {
        if (!_context.Caller.IsTerminal)
        {
            _chatService!.Send(_context.Caller, error, new Color32(255, 140, 105, 255));
        }
        else
        {
            _logger!.LogError(error);
        }
    }

    public void shutdown(CommandWindow commandWindow)
    {
        if (_logger is IDisposable disposable)
            disposable.Dispose();
    }

    event CommandInputHandler? ICommandInputOutput.inputCommitted { add { } remove { } }
    void ICommandInputOutput.initialize(CommandWindow commandWindow) { }
    void ICommandInputOutput.update() { }
}
