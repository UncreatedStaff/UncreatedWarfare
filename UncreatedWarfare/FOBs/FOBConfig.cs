using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncreatedWarfare.FOBs
{
    public class FOBConfig
    {
        [JsonIgnore]
        public readonly string directory;

        public ushort Team1BuildID;
        public ushort Team2BuildID;
        public ushort Team1AmmoID;
        public ushort Team2AmmoID;
        public ushort FOBBaseID;
        public ushort FOBID;
        public ushort FOBRequiredBuild;
        public uint FOBDeployDelay;
        public byte FobLimit;

        public ushort AmmoCrateBaseID;
        public ushort AmmoCrateID;
        public ushort AmmoCrateRequiredBuild;
        public ushort RepairStationBaseID;
        public ushort RepairStationID;
        public ushort RepairStationRequiredBuild;
        public ushort MortarID;
        public ushort MortarBaseID;
        public ushort MortarRequiredBuild;
        public ushort MortarShellID;

        public List<Emplacement> Emplacements;

        public List<Fortification> Fortifications;

        public List<ushort> LogiTruckIDs;

        public bool EnableCombatLogger;
        public uint CombatCooldown;

        public bool EnableDeployCooldown;
        public uint DeployCooldown;
        public bool DeployCancelOnMove;
        public bool DeployCancelOnDamage;

        public bool ShouldRespawnAtMain;
        public bool ShouldWipeAllFOBsOnRoundedEnded;
        public bool ShouldSendPlayersBackToMainOnRoundEnded;
        public bool ShouldKillMaincampers;

        public class Emplacement
        {
            public ushort vehicleID;
            public ushort baseID;
            public ushort ammoID;
            public ushort ammoAmount;
            public ushort requiredBuild;
        }

        public class Fortification
        {
            public ushort barricade_id;
            public ushort base_id;
            public ushort required_build;
        }

        public FOBConfig(string directory)
        {
            if (!File.Exists(directory))
            {
                StreamWriter creator = File.CreateText(directory);
                creator.WriteLine("");
                creator.Close();
                creator.Dispose();

                LoadDefaults();
            }
        }

        public void LoadDefaults()
        {
            Team1BuildID = 38312;
            Team2BuildID = 38313;
            Team1AmmoID = 38314;
            Team2AmmoID = 38315;
            FOBBaseID = 38310;
            FOBID = 38311;
            FOBRequiredBuild = 20;
            FOBDeployDelay = 10;
            FobLimit = 10;

            AmmoCrateBaseID = 38316;
            AmmoCrateID = 38317;
            AmmoCrateRequiredBuild = 3;

            RepairStationBaseID = 38318;
            RepairStationID = 38319;
            RepairStationRequiredBuild = 10;

            MortarID = 38313;
            MortarBaseID = 38336;
            MortarRequiredBuild = 10;
            MortarShellID = 38330;

            LogiTruckIDs = new List<ushort>() { 38305, 38306 };

            Fortifications = new List<Fortification>() {
                new Fortification
                {
                    base_id = 38350,
                    barricade_id = 38351,
                    required_build = 1
                },
                new Fortification
                {
                    base_id = 38352,
                    barricade_id = 38353,
                    required_build = 1
                },
                new Fortification
                {
                    base_id = 38354,
                    barricade_id = 38355,
                    required_build = 1
                }
            };

            Emplacements = new List<Emplacement>() {
                new Emplacement
                {
                    baseID = 38345,
                    vehicleID = 38316,
                    ammoID = 38302,
                    ammoAmount = 2,
                    requiredBuild = 6
                },
                new Emplacement
                {
                    baseID = 38346,
                    vehicleID = 38317,
                    ammoID = 38305,
                    ammoAmount = 2,
                    requiredBuild = 6
                },
                new Emplacement
                {
                    baseID = 38342,
                    vehicleID = 38315,
                    ammoID = 38341,
                    ammoAmount = 1,
                    requiredBuild = 10
                },
                new Emplacement
                {
                    baseID = 38339,
                    vehicleID = 38314,
                    ammoID = 38338,
                    ammoAmount = 1,
                    requiredBuild = 10
                },
                new Emplacement
                {
                    baseID = 38336,
                    vehicleID = 38313,
                    ammoID = 38330,
                    ammoAmount = 3,
                    requiredBuild = 8
                },
            };

            EnableCombatLogger = true;
            CombatCooldown = 60;

            EnableDeployCooldown = false;
            DeployCooldown = 120;
            DeployCancelOnMove = true;
            DeployCancelOnDamage = true;

            ShouldRespawnAtMain = true;
            ShouldSendPlayersBackToMainOnRoundEnded = true;
            ShouldWipeAllFOBsOnRoundedEnded = true;
            ShouldKillMaincampers = true;

            StreamWriter file = File.CreateText(directory);
            JsonWriter writer = new JsonTextWriter(file);

            JsonSerializer serializer = new JsonSerializer();

            serializer.Formatting = Formatting.Indented;

            try
            {
                serializer.Serialize(writer, this);
                writer.Close();
            }
            catch (Exception ex)
            {
                writer.Close();
                throw ex;
            }
        }
    }
}
