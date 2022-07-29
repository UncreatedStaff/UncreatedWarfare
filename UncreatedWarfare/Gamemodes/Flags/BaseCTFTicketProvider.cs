using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Tickets;

namespace Uncreated.Warfare.Gamemodes.Flags;
public abstract class BaseCTFTicketProvider : ITicketProvider
{
    public TicketManager Manager { get; set; }

    public BaseCTFTicketProvider() { }
    public virtual void Load()
    {
    }
    public virtual void Unload()
    {
        throw new NotImplementedException();
    }
    public virtual int GetTeamBleed(ulong team)
    {
        throw new NotImplementedException();
    }
}
