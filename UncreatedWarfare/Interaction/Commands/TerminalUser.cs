using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Interaction.Commands;
public class TerminalUser : ICommandUser
{
    public bool IsSuperUser => true;
    public bool IsTerminal => true;
    public bool IMGUI => false;
    public CSteamID Steam64 => CSteamID.Nil;
    public void SendMessage(string message)
    {
        GameThread.AssertCurrent();
        CommandWindow.Log(message);
    }
}
