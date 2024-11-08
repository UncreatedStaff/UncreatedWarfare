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
        _logger.LogInformation(message);
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
