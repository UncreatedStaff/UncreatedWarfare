using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Tickets;
public interface ITicketProvider
{
    TicketManager Manager { get; internal set; }
    void Load();
    void Unload();
    int GetTeamBleed(ulong team);
}
