using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Revives;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Gamemodes.TeamDeathmatch
{
    public class TeamDeathmatch : TeamGamemode, IKitRequests, IVehicles, IFOBs, ISquads, IRevives, ITeamScore
    {
        public TeamDeathmatch() : base(nameof(TeamDeathmatch), 0f)
        {

        }

        public override string DisplayName => "Team Deathmatch";
        public override bool UseWhitelist => true;
        public override bool UseJoinUI => true;
        public override bool TransmitMicWhileNotActive => true;
        public override bool EnableAMC => true;
        public override bool ShowXPUI => true;
        public override bool ShowOFPUI => true;
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
        public int Team1Score => _t1score;
        public int Team2Score => _t2score;
        protected int _t1score = 0;
        protected int _t2score = 0;

        public override void DeclareWin(ulong winner)
        {

        }
        public override void StartNextGame(bool onLoad = false)
        {
            base.StartNextGame(onLoad);
            _t1score = 0;
            _t2score = 0;
        }
        protected override void EventLoopAction() 
        {

        }
        public override void Init()
        {
            base.Init();
            _FOBManager = new FOBManager();
            _squadManager = new SquadManager();
            _kitManager = new KitManager();
            _reviveManager = new ReviveManager();
            _vehicleBay = new VehicleBay();
            _vehicleSpawner = new VehicleSpawner();
            _requestSigns = new RequestSigns();
            _vehicleSigns = new VehicleSigns();
            _structureSaver = new StructureSaver();
        }
        public override void OnLevelLoaded()
        {
            RepairManager.LoadRepairStations();
            RallyManager.WipeAllRallies();
            VehicleSigns.InitAllSigns();
            base.OnLevelLoaded();
        }
    }
}
