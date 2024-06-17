using Steamworks;

namespace Uncreated.Warfare.Commands.Dispatch;
public interface ICommandUser
{
    bool IsSuperUser { get; }
    CSteamID Steam64 { get; }

    void SendMessage(string message);
}
