using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Gamemodes.Interfaces
{
    public interface IGamemode
    {
        string DisplayName { get; }
        long GameID { get; }
        string Name { get; }
        EState State { get; }
    }
}
