using Newtonsoft.Json;
using SDG.Unturned;
using System;
using System.Collections.Generic;

namespace Uncreated.Warfare.Gamemodes
{
    public class GamemodeConfigs : ConfigData
    {
        public BARRICADE_IDS Barricades;
        public ITEM_IDS Items;
        public UI_CONFIG UI;
        public TEAM_CTF_CONFIG TeamCTF;
        public INVASION Invasion;
        public INSURGENCY Insurgency;
        public MAP_CONFIG[] MapsConfig;
        [JsonIgnore]
        public MAP_CONFIG MapConfig 
        { 
            get
            {
                if (!_mapset)
                {
                    if (MapsConfig == null || MapsConfig.Length == 0)
                    {
                        _map = new MAP_CONFIG()
                        {
                            Map = Provider.map
                        };
                        _map.SetDefaults();
                        MapsConfig = new MAP_CONFIG[1] { _map };
                        _mapset = true;
                        Gamemode.ConfigObj.Save();
                        return _map;
                    }
                    for (int i = 0; i < MapsConfig.Length; i++)
                    {
                        if (MapsConfig[i].Map == Provider.map)
                        {
                            _map = MapsConfig[i];
                            _mapset = true;
                            return _map;
                        }
                    }
                    MAP_CONFIG[] old = MapsConfig;
                    MapsConfig = new MAP_CONFIG[old.Length + 1];
                    Array.Copy(old, 0, MapsConfig, 0, old.Length);
                    _map = new MAP_CONFIG()
                    {
                        Map = Provider.map
                    };
                    _map.SetDefaults();
                    MapsConfig[MapsConfig.Length - 1] = _map;
                    _mapset = true;
                    Gamemode.ConfigObj.Save();
                    return _map;
                }
                else return _map;
            }
        }
        [JsonIgnore]
        private MAP_CONFIG _map;
        [JsonIgnore]
        private bool _mapset;
        public GENERAL_GM_CONFIG GeneralConfig;

        public GamemodeConfigs() => SetDefaults();
        public override void SetDefaults()
        {
            Barricades = new BARRICADE_IDS();
            Barricades.SetDefaults();
            Items = new ITEM_IDS();
            Items.SetDefaults();
            UI = new UI_CONFIG();
            UI.SetDefaults();
            Invasion = new INVASION();
            Invasion.SetDefaults();
            Insurgency = new INSURGENCY();
            Insurgency.SetDefaults();
            TeamCTF = new TEAM_CTF_CONFIG();
            TeamCTF.SetDefaults();
            MapsConfig = new MAP_CONFIG[2]
            {
                new MAP_CONFIG() { Map = "Nuijamaa" },
                new MAP_CONFIG() { Map = "Gulf of Aqaba" }
            };
            for (int i = 0; i < MapsConfig.Length; i++)
                MapsConfig[i].SetDefaults();
            GeneralConfig = new GENERAL_GM_CONFIG();
            GeneralConfig.SetDefaults();
        }
    }
    public struct GENERAL_GM_CONFIG
    {
        public float AMCKillTime;
        public float LeaderboardDelay;
        public float LeaderboardTime;
        public void SetDefaults()
        {
            AMCKillTime = 10f;
            LeaderboardDelay = 15f;
            LeaderboardTime = 30f;
        }
    }

    public struct UI_CONFIG
    {
        public Guid CaptureGUID;
        public Guid FlagListGUID;
        public Guid HeaderGUID;
        public Guid FOBListGUID;
        public Guid SquadListGUID;
        public Guid SquadMenuGUID;
        public Guid RallyGUID;
        public Guid OrderUI;
        public Guid XPGUID;
        public Guid OfficerGUID;
        public Guid CTFLeaderboardGUID;
        public Guid InsurgencyLeaderboardGUID;
        public Guid NearbyResourcesGUID;
        public int FlagUICount;
        public int MaxSquadMembers;
        public int MaxSquads;
        public bool EnablePlayerCount;
        public bool ShowPointsOnUI;
        public string ProgressChars;
        public char PlayerIcon;
        public char AttackIcon;
        public char DefendIcon;
        public char LockIcon;
        public void SetDefaults()
        {
            CaptureGUID = new Guid("76a9ffb4-659a-4940-80d9-8c8ef7733815");
            FlagListGUID = new Guid("c01fe46d-9b79-4364-aca6-a3887a028164");
            HeaderGUID = new Guid("c14fe9ff-ee6d-4f8d-be7f-57885f678edd");
            FOBListGUID = new Guid("2c01a369-43ea-4518-9d86-6f5463f8e5e9");
            SquadListGUID = new Guid("5acd091f-1e7b-4f93-ac9f-5431729ac5cc");
            SquadMenuGUID = new Guid("98154002-fbcd-4b74-9955-2d6497db8fc5");
            RallyGUID = new Guid("a280ac3f-e8c1-486c-adc8-eca331e8ce32");
            OrderUI = new Guid("57a08eb9c4cb4fd2ad30a3e413e29b27");
            XPGUID = new Guid("d6de0a80-25de-44d2-9a99-a41937a58a59");
            OfficerGUID = new Guid("9fd31b77-6b74-4b72-847f-2dc00dba93a8");
            CTFLeaderboardGUID = new Guid("b83389df-1245-438d-b188-89af94f04960");
            InsurgencyLeaderboardGUID = new Guid("c0bf4518-d941-4319-b67d-bb46ad88ba2f");
            NearbyResourcesGUID = new Guid("3775a1e7-d84b-47e7-9cac-ecd5e6b2a224");
            MaxSquadMembers = 6;
            MaxSquads = 8;
            FlagUICount = 10;
            EnablePlayerCount = true;
            ShowPointsOnUI = false;
            ProgressChars = "ĀāĂăĄąĆćĈĉĊċČčĎďĐđĒēĔĕĖėĘęĚěĜĝĞğĠġĢģĤĥĦħĨĩĪīĬĭĮįİıĲĳĴĵĶķĸĹĺĻļĽľĿŀ";
            PlayerIcon = '³';
            AttackIcon = 'µ';
            DefendIcon = '´';
            LockIcon = '²';
        }
    }
    public struct BARRICADE_IDS
    {
        public Guid InsurgencyCacheGUID;
        public Guid FOBRadioDamagedGUID;
        public Guid FOBGUID;
        public Guid FOBBaseGUID;
        public Guid AmmoCrateGUID;
        public Guid AmmoCrateBaseGUID;
        public Guid RepairStationGUID;
        public Guid RepairStationBaseGUID;
        public Guid AmmoBagGUID;
        public Guid T1RallyPointGUID;
        public Guid T2RallyPointGUID;
        public Guid VehicleBayGUID;
        public Guid[] TimeLimitedStorages;
        public Guid[] FOBRadioGUIDs;
        public void SetDefaults()
        {
            InsurgencyCacheGUID = new Guid("39051f33-f244-49b4-b341-7d0d666a4f27");
            FOBRadioDamagedGUID = new Guid("07e68489e3b547879fa26f94ea227522");
            FOBGUID = new Guid("61c349f1-0000-498f-a2b9-2c029d38e523");
            FOBBaseGUID = new Guid("1bb17277-dd81-48df-9f4c-53d1a19b2503");
            AmmoCrateGUID = new Guid("6fe20851-9d7c-45b0-be38-273118eea7fd");
            AmmoCrateBaseGUID = new Guid("eccfe06e-53d0-41d5-b83c-614ffa62ee59");
            RepairStationGUID = new Guid("c0d11e06-6669-4dde-a667-377b4c0580be");
            RepairStationBaseGUID = new Guid("26a6b91c-d194-4730-a0f2-8e5f299cebf9");
            AmmoBagGUID = new Guid("16f55b99-9e9b-4f15-8be1-2645e41dd753");
            T1RallyPointGUID = new Guid("5e1db525-1793-41d3-b0c7-576876212a81");
            T2RallyPointGUID = new Guid("0d789536-0c80-440f-be4a-45eba28b2007");
            VehicleBayGUID = new Guid("c076f9e9-f35f-42a4-b8b5-711dfb230010");
            TimeLimitedStorages = new Guid[4]
            {
                AmmoCrateGUID,
                RepairStationGUID,
                new Guid("a2eb7659-0cf7-4401-aeb7-ff4b4b79fd86"), // supply crate
                new Guid("2193aa0b-272f-4cc1-938f-719c8e8badb1")  // supply roll
            };
            FOBRadioGUIDs = new Guid[3]
            {
                new Guid("7715ad81f1e24f60bb8f196dd09bd4ef"),
                new Guid("fb910102ad954169abd4b0cb06a112c8"),
                new Guid("c7754ac78083421da73006b12a56811a")
            };
        }
    }
    public struct ITEM_IDS
    {
        public Guid T1Build;
        public Guid T2Build;
        public Guid T1Ammo;
        public Guid T2Ammo;
        public Guid EntrenchingTool;
        public void SetDefaults()
        {
            T1Build = new Guid("a70978a0-b47e-4017-a026-1e676af57042");
            T2Build = new Guid("6a8b8b3c-7960-4aee-a97f-53c235947a1f");
            T1Ammo = new Guid("51e1e372-bf53-41e1-b4b1-6a0eacce37eb");
            T2Ammo = new Guid("8dd66da5-affa-480b-a324-e270e52a46d7");
            EntrenchingTool = new Guid("6cee2662e8884d7bad3a5d743f8222da");
        }
    }

    public struct MAP_CONFIG
    {
        public string Map;
        public Guid T1ZoneBlocker;
        public Guid T2ZoneBlocker;
        public Dictionary<int, float> Team1Adjacencies;
        public Dictionary<int, float> Team2Adjacencies;
        public SerializableTransform[] CacheSpawns;
        public void AddCacheSpawn(SerializableTransform t)
        {
            if (CacheSpawns == null || CacheSpawns.Length == 0)
            {
                CacheSpawns = new SerializableTransform[1] { t };
            }
            else
            {
                SerializableTransform[] old = CacheSpawns;
                CacheSpawns = new SerializableTransform[old.Length + 1];
                Array.Copy(old, CacheSpawns, old.Length);
                CacheSpawns[CacheSpawns.Length - 1] = t;
                Gamemode.ConfigObj.Save();
            }
        }
        public void SetDefaults()
        {
            switch (Map)
            {
                case "Nuijamaa":
                    T1ZoneBlocker = new Guid("57927806-0501-4735-ab01-2f1f7adaf714");
                    T2ZoneBlocker = new Guid("b4c0a51b-7005-4ad5-b6fe-06aead982d94");
                    CacheSpawns = NuijamaaDefaultCaches;
                    Team1Adjacencies = new Dictionary<int, float>()
                    {
                        { 16, 1f },
                        { 15, 1f },
                        { 1, 1f },
                    };
                    Team2Adjacencies = new Dictionary<int, float>()
                    {
                        { 12, 1f },
                        { 3, 1f },
                        { 8, 1f }
                    };
                    break;
                default:
                    T1ZoneBlocker = Guid.Empty;
                    T2ZoneBlocker = Guid.Empty;
                    CacheSpawns = new SerializableTransform[0];
                    Team1Adjacencies = new Dictionary<int, float>();
                    Team2Adjacencies = new Dictionary<int, float>();
                    break;
            }
        }


        private static SerializableTransform[] NuijamaaDefaultCaches => 
            new SerializableTransform[91] 
            {
                new SerializableTransform(211.300583f, 37.7143173f, 61.399395f, 0f, 179.149933f, 0f),
                new SerializableTransform(-11.5022888f, 70.63667f, -261.72052f, 0f, 88.94999f, 0f),
                new SerializableTransform(8.11329651f, 70.63667f, -249.7733f, 0f, 272.250061f, 0f),
                new SerializableTransform(5.92330933f, 65.88658f, -260.0689f, 0f, 178.500061f, 0f),
                new SerializableTransform(-9.233465f, 65.88658f, -251.471329f, 0f, 359.1f, 0f),
                new SerializableTransform(420.090576f, 71.5975f, -142.901291f, 0f, 0.8499718f, 0f),
                new SerializableTransform(465.664459f, 67.7518539f, -119.160088f, 0f, 265.3f, 0f),
                new SerializableTransform(382.011169f, 57.22876f, -240.3982f, 0f, 88.54994f, 0f),
                new SerializableTransform(613.8219f, 55.2565155f, -254.794357f, 0f, 112.850037f, 0f),
                new SerializableTransform(670.832153f, 55.73659f, -169.284378f, 0f, 180.950058f, 0f),
                new SerializableTransform(583.959534f, 55.238884f, -172.474655f, 0f, 272.750031f, 0f),
                new SerializableTransform(533.4562f, 55.5497131f, -173.006577f, 0f, 88.4001f, 0f),
                new SerializableTransform(206.92511f, 57.23698f, -294.8498f, 0f, 89.10002f, 0f),
                new SerializableTransform(189.179108f, 57.30183f, -250.772156f, 0f, 179.549911f, 0f),
                new SerializableTransform(185.191574f, 57.30183f, -265.599152f, 0f, 85.94989f, 0f),
                new SerializableTransform(176.814941f, 57.30183f, -271.1832f, 0f, 86.84988f, 0f),
                new SerializableTransform(-410.5612f, 40.891037f, -668.213135f, 0f, 34.0499573f, 0f),
                new SerializableTransform(-422.312866f, 40.891037f, -657.6499f, 0f, 34.94998f, 0f),
                new SerializableTransform(-412.7648f, 40.891037f, -669.8023f, 0f, 218.09993f, 0f),
                new SerializableTransform(-412.309082f, 45.6410446f, -670.1775f, 0f, 218.400146f, 0f),
                new SerializableTransform(-418.3301f, 45.6410446f, -666.79895f, 0f, 308.100159f, 0f),
                new SerializableTransform(-502.8874f, 40.9491577f, -93.64789f, 0f, 42.4999733f, 0f),
                new SerializableTransform(-502.523865f, 40.9491577f, -79.45072f, 0f, 222.350037f, 0f),
                new SerializableTransform(-484.608856f, 40.9402428f, -137.176758f, 0f, 347.600128f, 0f),
                new SerializableTransform(-482.23584f, 40.91754f, -149.0282f, 0f, 346.250122f, 0f),
                new SerializableTransform(-481.528778f, 40.9402466f, -154.310181f, 0f, 171.350159f, 0f),
                new SerializableTransform(-507.527344f, 41.4843445f, -266.21286f, 0f, 345.8f, 0f),
                new SerializableTransform(-506.384949f, 46.2349434f, -252.794846f, 0f, 255.350067f, 0f),
                new SerializableTransform(-513.4673f, 46.2349434f, -252.60997f, 0f, 254.750061f, 0f),
                new SerializableTransform(-520.7801f, 46.23494f, -254.459274f, 0f, 254.750061f, 0f),
                new SerializableTransform(-518.1406f, 41.0946922f, -274.926331f, 0f, 86.750145f, 0f),
                new SerializableTransform(-513.371948f, 41.4843445f, -253.286926f, 0f, 259.700134f, 0f),
                new SerializableTransform(-526.634033f, 40.98004f, 430.530273f, 0f, 345.450043f, 0f),
                new SerializableTransform(-542.970764f, 40.98004f, 405.267059f, 0f, 33.15008f, 0f),
                new SerializableTransform(-522.314758f, 42.6923676f, 351.44104f, 0f, 346.650146f, 0f),
                new SerializableTransform(-536.995056f, 42.6923676f, 346.5384f, 0f, 166.350113f, 0f),
                new SerializableTransform(-531.787354f, 42.6923676f, 348.352631f, 0f, 77.7001953f, 0f),
                new SerializableTransform(372.0398f, 41.7456474f, 184.827316f, 0f, 214.600189f, 0f),
                new SerializableTransform(314.761627f, 41.718914f, 99.99346f, 0f, 34.45018f, 0f),
                new SerializableTransform(186.7212f, 35.2399635f, -498.943634f, 0f, 217.90007f, 0f),
                new SerializableTransform(184.842773f, 35.2399635f, -490.844727f, 0f, 308.500061f, 0f),
                new SerializableTransform(186.420181f, 35.2399635f, -492.4004f, 0f, 129.100037f, 0f),
                new SerializableTransform(206.586655f, 35.1705627f, -504.037781f, 0f, 178.90007f, 0f),
                new SerializableTransform(466.251251f, 45.2722359f, -401.8267f, 0f, 274.950043f, 0f),
                new SerializableTransform(-139.82843f, 46.3012733f, 210.834854f, 0f, 134.650116f, 0f),
                new SerializableTransform(-187.632187f, 46.3038139f, 195.410187f, 0f, 134.249878f, 0f),
                new SerializableTransform(-225.656189f, 46.30116f, 229.42128f, 0f, 314.549866f, 0f),
                new SerializableTransform(-224.612335f, 46.30116f, 237.214615f, 0f, 134.099884f, 0f),
                new SerializableTransform(-205.013519f, 46.3040428f, 267.498474f, 0f, 315.925079f, 0f),
                new SerializableTransform(-246.430023f, 46.3011551f, 259.2519f, 0f, 137.249741f, 0f),
                new SerializableTransform(588.2777f, 35.1819649f, 279.46228f, 0f, 269.149963f, 0f),
                new SerializableTransform(586.707458f, 39.9319725f, 270.513641f, 0f, 0.0500241555f, 0f),
                new SerializableTransform(586.898743f, 39.9319725f, 287.373474f, 0f, 181.250122f, 0f),
                new SerializableTransform(688.4736f, 35.1764946f, 312.650238f, 0f, 175.100159f, 0f),
                new SerializableTransform(700.934143f, 39.9264946f, 302.0669f, 0f, 270.6502f, 0f),
                new SerializableTransform(684.0695f, 39.9264946f, 311.978455f, 0f, 91.40021f, 0f),
                new SerializableTransform(684.063f, 39.9264946f, 302.3407f, 0f, 90.6502f, 0f),
                new SerializableTransform(803.445f, 50.04394f, 515.1205f, 0f, 308.150146f, 0f),
                new SerializableTransform(802.002136f, 54.7939453f, 529.871033f, 0f, 217.400146f, 0f),
                new SerializableTransform(802.9041f, 54.75959f, 514.750061f, 0f, 310.550079f, 0f),
                new SerializableTransform(790.8298f, 54.7939453f, 516.738342f, 0f, 41.000164f, 0f),
                new SerializableTransform(-602.1001f, 40.8446732f, 247.16597f, 0f, 98.50005f, 0f),
                new SerializableTransform(-648.6162f, 40.90011f, 243.529846f, 0f, 1.74994516f, 0f),
                new SerializableTransform(-660.66394f, 40.90011f, 262.497375f, 0f, 264.699921f, 0f),
                new SerializableTransform(-665.3761f, 40.8595352f, 296.391418f, 0f, 182.94986f, 0f),
                new SerializableTransform(-655.8673f, 40.8521042f, 296.3904f, 0f, 183.099945f, 0f),
                new SerializableTransform(-97.37492f, 46.617733f, 646.7229f, 0f, 90.05f, 0f),
                new SerializableTransform(-62.9612846f, 46.617733f, 627.3119f, 0f, 93.7998047f, 0f),
                new SerializableTransform(-58.07756f, 46.617733f, 624.209961f, 0f, 40.0997734f, 0f),
                new SerializableTransform(60.5422859f, 40.907093f, 430.864746f, 0f, 106.799957f, 0f),
                new SerializableTransform(8.730333f, 40.88394f, 447.091339f, 0f, 105.824913f, 0f),
                new SerializableTransform(-90.71227f, 46.617733f, 649.608765f, 0f, 145.999908f, 0f),
                new SerializableTransform(48.3672638f, 40.88661f, 475.216156f, 0f, 283.725037f, 0f),
                new SerializableTransform(-78.24946f, 34.3677139f, 640.677063f, 0f, 125.749916f, 0f),
                new SerializableTransform(-70.5189362f, 34.3677139f, 627.6236f, 0f, 280.699951f, 0f),
                new SerializableTransform(99.10344f, 40.9936028f, 464.4927f, 0f, 105.375137f, 0f),
                new SerializableTransform(-46.1998672f, 34.3677139f, 618.179565f, 0f, 127.549957f, 0f),
                new SerializableTransform(-30.4742985f, 34.3677139f, 617.7121f, 0f, 257.899933f, 0f),
                new SerializableTransform(60.8913155f, 40.90628f, 430.435852f, 0f, 105.300117f, 0f),
                new SerializableTransform(0.122714f, 40.88394f, 442.549164f, 0f, 105.750305f, 0f),
                new SerializableTransform(-733.278564f, 47.2598648f, 462.001343f, 0f, 267.899963f, 0f),
                new SerializableTransform(-745.6896f, 47.82388f, 455.580444f, 0f, 85.49992f, 0f),
                new SerializableTransform(-733.2538f, 47.1943932f, 453.418182f, 0f, 275.249878f, 0f),
                new SerializableTransform(-219.196991f, 48.64708f, -806.9547f, 0f, 281.150055f, 0f),
                new SerializableTransform(-212.715f, 48.64708f, -814.1917f, 0f, 195.500015f, 0f),
                new SerializableTransform(-216.277176f, 48.64708f, -823.0809f, 0f, 284.599976f, 0f),
                new SerializableTransform(-210.211f, 48.64708f, -808.9972f, 0f, 106.849968f, 0f),
                new SerializableTransform(291.617859f, 38.38968f, -573.3151f, 0f, 271.3f, 0f),
                new SerializableTransform(189.369614f, 45.0764236f, -447.277283f, 0f, 31.12429f, 0f),
                new SerializableTransform(265.075653f, 42.2353f, 389.17807f, 0f, 177.650055f, 0f),
                new SerializableTransform(269.669922f, 42.2353f, 380.341553f, 0f, 268.8501f, 0f)
            };
    }
    public struct TEAM_CTF_CONFIG
    {
        public int StagingTime;
        public float EvaluateTime;
        public int TicketXPInterval;
        public int RequiredPlayerDifferenceToCapture;
        public int OverrideContestDifference;
        public bool AllowVehicleCapture;
        public int DiscoveryForesight;
        public int FlagTickInterval;
        public int TicketsFlagCaptured;
        public int TicketsFlagLost;
        public void SetDefaults()
        {
            StagingTime = 90;
            EvaluateTime = 0.25f;
            TicketXPInterval = 10;
            OverrideContestDifference = 2;
            AllowVehicleCapture = false;
            DiscoveryForesight = 2;
            FlagTickInterval = 4;
            TicketsFlagCaptured = 20;
            TicketsFlagLost = -20;
            RequiredPlayerDifferenceToCapture = 2;
        }
    }
    public struct INVASION
    {
        public int StagingTime;
        public int DiscoveryForesight;
        public string SpecialFOBName;
        public int TicketsFlagCaptured;
        public int AttackStartingTickets;
        public int TicketXPInterval;
        public void SetDefaults()
        {
            StagingTime = 150;
            DiscoveryForesight = 2;
            SpecialFOBName = "VCP";
            TicketsFlagCaptured = 150;
            AttackStartingTickets = 250;
            TicketXPInterval = 10;
        }
    }
    public struct INSURGENCY
    {
        public int MinStartingCaches;
        public int MaxStartingCaches;
        public int StagingTime;
        public int AttackStartingTickets;
        public int CacheDiscoverRange;
        public int IntelPointsToSpawn;
        public int IntelPointsToDiscovery;
        public int XPCacheDestroyed;
        public int XPCacheTeamkilled;
        public int TicketsCache;
        public int CacheStartingBuild;
        public Dictionary<ushort, int> CacheItems;
        public void SetDefaults()
        {
            MinStartingCaches = 4;
            MaxStartingCaches = 6;
            StagingTime = 150;
            AttackStartingTickets = 300;
            CacheDiscoverRange = 75;
            IntelPointsToDiscovery = 30;
            IntelPointsToSpawn = 15;
            XPCacheDestroyed = 800;
            XPCacheTeamkilled = -8000;
            TicketsCache = 80;
            CacheStartingBuild = 15;
            CacheItems = new Dictionary<ushort, int>();
        }
    }
}
