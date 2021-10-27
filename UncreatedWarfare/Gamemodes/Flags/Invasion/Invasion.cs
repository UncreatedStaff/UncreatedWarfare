using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;

namespace Uncreated.Warfare.Gamemodes.Flags.Invasion
{
    public class Invasion : TicketGamemode
    {
        public override string DisplayName => throw new NotImplementedException();
        private readonly Config<InvasionData> _config;
        public InvasionData Config { get => _config.Data; }
        public int ObjectiveT1Index;
        public int ObjectiveT2Index;
        public Flag ObjectiveTeam1 { get => Rotation[ObjectiveT1Index]; }
        public Flag ObjectiveTeam2 { get => Rotation[ObjectiveT2Index]; }
        public Invasion(string Name, float EventLoopSpeed) : base(Name, EventLoopSpeed)
        {
            _config = new Config<InvasionData>(Data.FlagStorage, "invasion.json");
        }

        public override void DeclareWin(ulong winner)
        {

        }
        public override void LoadRotation()
        {
            if (AllFlags == null) return;
            ResetFlags();
            OnFlag.Clear();
            if (Config.PathingMode == ObjectivePathing.EPathingMode.AUTODISTANCE)
            {
                Config.PathingData.Set();
                Rotation = ObjectivePathing.CreateAutoPath(AllFlags);
            }
            else if (Config.PathingMode == ObjectivePathing.EPathingMode.LEVELS)
            {
                Rotation = ObjectivePathing.CreatePathUsingLevels(AllFlags, Config.MaxFlagsPerLevel);
            }
            else if (Config.PathingMode == ObjectivePathing.EPathingMode.ADJACENCIES)
            {
                Rotation = ObjectivePathing.PathWithAdjacents(AllFlags, Config.team1adjacencies, Config.team2adjacencies);
            }
            else
            {
                F.LogWarning("Invalid pathing value, no flags will be loaded. Expect errors.");
            }
            ObjectiveT1Index = 0;
            ObjectiveT2Index = Rotation.Count - 1;
            if (Config.DiscoveryForesight < 1)
            {
                F.LogWarning("Discovery Foresight is set to 0 in Flag Settings. The players can not see their next flags.");
            }
            else
            {
                for (int i = 0; i < Config.DiscoveryForesight; i++)
                {
                    if (i >= Rotation.Count || i < 0) break;
                    Rotation[i].Discover(1);
                }
                for (int i = Rotation.Count - 1; i > Rotation.Count - 1 - Config.DiscoveryForesight; i--)
                {
                    if (i >= Rotation.Count || i < 0) break;
                    Rotation[i].Discover(2);
                }
            }
            foreach (Flag flag in Rotation)
            {
                InitFlag(flag); //subscribe to abstract events.
            }
            foreach (SteamPlayer client in Provider.clients)
            {
                CTFUI.ClearListUI(client.transportConnection, Config.FlagUICount);
                CTFUI.SendFlagListUI(client.transportConnection, client.playerID.steamID.m_SteamID, client.GetTeam(), Rotation, Config.FlagUICount, Config.AttackIcon, Config.DefendIcon);
            }
            PrintFlagRotation();
            EvaluatePoints();
        }
        protected override void FlagOwnerChanged(ulong OldOwner, ulong NewOwner, Flag flag) => throw new NotImplementedException();
        protected override void FlagPointsChanged(float NewPoints, float OldPoints, Flag flag) => throw new NotImplementedException();
        protected override void PlayerEnteredFlagRadius(Flag flag, Player player) => throw new NotImplementedException();
        protected override void PlayerLeftFlagRadius(Flag flag, Player player) => throw new NotImplementedException();
        protected override bool TimeToCheck()
        {
            if (_counter > Config.FlagCounterMax)
            {
                _counter = 0;
                return true;
            }
            else
            {
                _counter++;
                return false;
            }
        }
        protected override bool TimeToTicket()
        {
            if (_counter2 > 1 / Config.PlayerCheckSpeedSeconds)
            {
                _counter2 = 0;
                return true;
            }
            else
            {
                _counter2++;
                return false;
            }
        }
        protected override void EvaluateTickets() => throw new NotImplementedException();
    }

    public class InvasionData : ConfigData
    {
        public float PlayerCheckSpeedSeconds;
        public bool UseUI;
        public bool UseChat;
        [JsonConverter(typeof(StringEnumConverter))]
        public ObjectivePathing.EPathingMode PathingMode;
        public int MaxFlagsPerLevel;
        public ushort CaptureUI;
        public ushort FlagUIIdFirst;
        public int FlagUICount;
        public bool EnablePlayerCount;
        public bool ShowPointsOnUI;
        public int FlagCounterMax;
        public bool AllowPlayersToCaptureInVehicle;
        public bool HideUnknownFlags;
        public uint DiscoveryForesight;
        public int RequiredPlayerDifferenceToCapture;
        public string ProgressChars;
        public char PlayerIcon;
        public char AttackIcon;
        public char DefendIcon;
        public bool ShowLeaderboard;
        public TeamCTFData.AutoObjectiveData PathingData;
        public int end_delay;
        public float NearOtherBaseKillTimer;
        public int xpSecondInterval;
        // 0-360
        public float team1spawnangle;
        public float team2spawnangle;
        public float lobbyspawnangle;
        public Dictionary<int, float> team1adjacencies;
        public Dictionary<int, float> team2adjacencies;
        public InvasionData() => SetDefaults();
        public override void SetDefaults()
        {
            this.PlayerCheckSpeedSeconds = 0.25f;
            this.PathingMode = ObjectivePathing.EPathingMode.ADJACENCIES;
            this.MaxFlagsPerLevel = 2;
            this.UseUI = true;
            this.UseChat = false;
            this.CaptureUI = 36000;
            this.FlagUIIdFirst = 36010;
            this.FlagUICount = 10;
            this.EnablePlayerCount = true;
            this.ShowPointsOnUI = true;
            this.FlagCounterMax = 1;
            this.HideUnknownFlags = true;
            this.DiscoveryForesight = 2;
            this.AllowPlayersToCaptureInVehicle = false;
            this.RequiredPlayerDifferenceToCapture = 2;
            this.ProgressChars = "¶·¸¹º»:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            this.PlayerIcon = '³';
            this.AttackIcon = 'µ';
            this.DefendIcon = '´';
            this.ShowLeaderboard = true;
            this.PathingData = new TeamCTFData.AutoObjectiveData();
            this.end_delay = 15;
            this.NearOtherBaseKillTimer = 10f;
            this.team1spawnangle = 0f;
            this.team2spawnangle = 0f;
            this.lobbyspawnangle = 0f;
            this.team1adjacencies = new Dictionary<int, float>();
            this.team2adjacencies = new Dictionary<int, float>();
            this.xpSecondInterval = 10;
        }
    }
}
