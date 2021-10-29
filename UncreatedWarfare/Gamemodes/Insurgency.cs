using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Revives;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Gamemodes
{
    internal class Insurgency : TeamGamemode, ITeams, IFOBs, IVehicles, IKitRequests, IRevives, ISquads, IImplementsLeaderboard, IStructureSaving
    {
        private readonly Config<InsurgencyConfig> insurgencyConfig;
        public InsurgencyConfig Config { get => insurgencyConfig.Data; }

        public Insurgency()
            : base("Insurgency", 0.25F)
        {

        }

        public override string DisplayName => "Insurgency";

        protected VehicleSpawner _vehicleSpawner;
        public VehicleSpawner VehicleSpawner => _vehicleSpawner;
        protected VehicleBay _vehicleBay;
        public VehicleBay VehicleBay => _vehicleBay;
        protected VehicleSigns _vehicleSigns;
        public VehicleSigns VehicleSigns => _vehicleSigns;
        protected FOBManager _FOBManager;
        public FOBManager FOBManager => _FOBManager;
        protected RequestSigns _requestSigns;
        public RequestSigns RequestSigns => _requestSigns;
        protected KitManager _kitManager;
        public KitManager KitManager => _kitManager;
        protected ReviveManager _reviveManager;
        public ReviveManager ReviveManager => _reviveManager;
        protected SquadManager _squadManager;
        public SquadManager SquadManager => _squadManager;
        protected StructureSaver _structureSaver;
        public StructureSaver StructureSaver => _structureSaver;

        public bool isScreenUp => throw new NotImplementedException();

        public ILeaderboard Leaderboard => throw new NotImplementedException();

        public override void Init()
        {
            base.Init();
            _FOBManager = new FOBManager();
            _squadManager = new SquadManager();
            _kitManager = new KitManager();
            _vehicleBay = new VehicleBay();
            _reviveManager = new ReviveManager();
        }

        public override void OnLevelLoaded()
        {
            _structureSaver = new StructureSaver();
            _vehicleSpawner = new VehicleSpawner();
            _vehicleSigns = new VehicleSigns();
            _requestSigns = new RequestSigns();
            FOBManager.LoadFobsFromMap();
            RepairManager.LoadRepairStations();
            VehicleSpawner.OnLevelLoaded();
            RallyManager.WipeAllRallies();
            VehicleSigns.InitAllSigns();
            base.OnLevelLoaded();
        }

        public override void DeclareWin(ulong winner)
        {
            
        }

        protected override void EventLoopAction()
        {
            
        }

        public class InsurgencyConfig : ConfigData
        {

            public InsurgencyConfig() => SetDefaults();
            public override void SetDefaults()
            {
                
            }
        }
    }
}
