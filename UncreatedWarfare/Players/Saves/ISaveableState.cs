using System;
using System.Collections.Generic;
using System.Text;

namespace Uncreated.Warfare.Players.Saves;
public interface ISaveableState
{
    public void Save();
    public void Load();
}
