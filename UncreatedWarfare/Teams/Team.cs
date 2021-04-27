using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UncreatedWarfare.Teams
{
    public class Team
    {
        public static readonly Team Neutral = new Team(ulong.MaxValue, "Neutral");
        public ulong ID { get; private set; }
        public string Name { get; protected set; }
        public string LocalizedName { 
            get
            {
                if (ID == 1)
                    return F.Translate("team_1");
                else if (ID == 2)
                    return F.Translate("team_2");
                else return Name;
            } 
        }
        public string TeamColorHex
        {
            get
            {
                if (ID == 1)
                    return UCWarfare.I.ColorsHex["team_1_color"];
                else if (ID == 2)
                    return UCWarfare.I.ColorsHex["team_2_color"];
                else return UCWarfare.I.ColorsHex["neutral_color"];
            }
        }
        public Color TeamColor
        {
            get
            {
                if (ID == 1)
                    return UCWarfare.I.Colors["team_1_color"];
                else if (ID == 2)
                    return UCWarfare.I.Colors["team_2_color"];
                else return UCWarfare.I.Colors["neutral_color"];
            }
        }
        public Team(ulong id, string name)
        {
            this.ID = id;
            this.Name = name;
        }
    }
}
