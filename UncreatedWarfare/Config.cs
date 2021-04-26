using Rocket.API;
using System.Xml.Serialization;

namespace UncreatedWarfare
{
    public class Config : IRocketPluginConfiguration
    {
        [XmlElement("Modules")]
        public Modules Modules;
        public void LoadDefaults()
        {
            Modules = new Modules();
        }
    }
    public class Modules
    {
        public bool PlayerList;
        public bool Kits;
        public bool Rename;
        public bool VehicleSpawning;
        public bool FOBs;
        public bool Roles;
        public bool UI;
        public bool AdminLogging;
        public bool MainCampPrevention;
        public Modules()
        {
            this.PlayerList = true;
            this.Kits = true;
            this.Rename = true;
            this.VehicleSpawning = true;
            this.FOBs = true;
            this.Roles = true;
            this.UI = true;
            this.AdminLogging = true;
            this.MainCampPrevention = true;
        }
    }
}