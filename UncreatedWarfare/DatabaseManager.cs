using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncreatedWarfare
{
    public class DatabaseManager
    {
        public void AddXP(EXPGainType type)
        {
            if (!UCWarfare.I.XPData.ContainsKey(type)) return;
            int amount = UCWarfare.I.XPData[type];

        }
    }
    public enum EXPGainType : byte
    {
        CAP_INCREASE,
        WIN,
        KILL,
        DEFENCE_KILL,
        OFFENCE_KILL,
        CAPTURE_KILL,
        CAPTURE,
        HOLDING_POINT
    }
    public enum ECreditsGainType : byte
    {
        CAPTURE,
        WIN
    }
}
