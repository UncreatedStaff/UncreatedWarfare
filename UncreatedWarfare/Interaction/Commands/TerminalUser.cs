using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Interaction.Commands;

[CannotApplyEqualityOperator]
public class TerminalUser : ICommandUser
{
    private readonly ILogger _logger;
    public static TerminalUser Instance { get; } = new TerminalUser(WarfareModule.Singleton.GlobalLogger);
    static TerminalUser() { }

    public bool IsSuperUser => true;
    public bool IsTerminal => true;
    public bool IMGUI => false;
    public bool IsDisconnected => false;
    public CSteamID Steam64 => CSteamID.Nil;

    public TerminalUser(ILogger logger)
    {
        _logger = logger;
    }

    public void SendMessage(string message)
    {
        GameThread.AssertCurrent();
        if (!_logger.IsEnabled(LogLevel.Information))
        {
            // note: using critical so it can't be turned off,
            // "msg: " is removed automatically by WarfareFormattedLogValues in the constructor
            _logger.LogCritical("msg: " + message);
        }
        else
        {
            _logger.LogInformation(message);
        }
    }

    public IModerationActor GetModerationActor()
    {
        return Actors.Console;
    }

    public override int GetHashCode() => 990;
    public override bool Equals(object? obj)
    {
        return obj is TerminalUser;
    }

    public override string ToString()
    {
        return "Console";
    }
}
