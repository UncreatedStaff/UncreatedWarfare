using Newtonsoft.Json;
using SDG.Unturned;
using System;
using System.Collections.Generic;

namespace Uncreated.Warfare.Gamemodes
{
    public class GamemodeConfigs : ConfigData
    {
        public BARRICADE_IDS Barricades;
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
            UI = new UI_CONFIG();
            UI.SetDefaults();
            Invasion = new INVASION();
            Invasion.SetDefaults();
            Insurgency = new INSURGENCY();
            Insurgency.SetDefaults();
            TeamCTF = new TEAM_CTF_CONFIG();
            TeamCTF.SetDefaults();
            MapsConfig = new MAP_CONFIG[1]
            {
                new MAP_CONFIG() { Map = "Nuijamaa" }
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
        [JsonConverter(typeof(GuidConverter))]
        public Guid CaptureGUID;
        [JsonConverter(typeof(GuidConverter))]
        public Guid FlagListGUID;
        [JsonConverter(typeof(GuidConverter))]
        public Guid HeaderGUID;
        [JsonConverter(typeof(GuidConverter))]
        public Guid FOBListGUID;
        [JsonConverter(typeof(GuidConverter))]
        public Guid SquadListGUID;
        [JsonConverter(typeof(GuidConverter))]
        public Guid SquadMenuGUID;
        [JsonConverter(typeof(GuidConverter))]
        public Guid RallyGUID;
        [JsonConverter(typeof(GuidConverter))]
        public Guid XPGUID;
        [JsonConverter(typeof(GuidConverter))]
        public Guid OfficerGUID;
        [JsonConverter(typeof(GuidConverter))]
        public Guid CTFLeaderboardGUID;
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
            CaptureGUID = new Guid(new byte[16] { 118, 169, 255, 180, 101, 154, 73, 64, 128, 217, 140, 142, 247, 115, 56, 21 });
            FlagListGUID = new Guid(new byte[16] { 192, 31, 228, 109, 155, 121, 67, 100, 172, 166, 163, 136, 122, 2, 129, 100 });
            HeaderGUID = new Guid(new byte[16] { 222, 185, 232, 166, 155, 170, 77, 96, 180, 80, 68, 63, 9, 154, 67, 109 });
            FOBListGUID = new Guid(new byte[16] { 44, 1, 163, 105, 67, 234, 69, 24, 157, 134, 111, 84, 99, 248, 229, 233 });
            SquadListGUID = new Guid(new byte[16] { 90, 205, 9, 31, 30, 123, 79, 147, 172, 159, 84, 49, 114, 154, 197, 204 });
            SquadMenuGUID = new Guid(new byte[16] { 152, 21, 64, 2, 251, 205, 75, 116, 153, 85, 45, 100, 151, 219, 143, 197 });
            RallyGUID = new Guid(new byte[16] { 162, 128, 172, 63, 232, 193, 72, 108, 173, 200, 236, 163, 49, 232, 206, 50 });
            XPGUID = new Guid(new byte[16] { 214, 222, 10, 128, 37, 222, 68, 210, 154, 153, 164, 25, 55, 165, 138, 89 });
            OfficerGUID = new Guid(new byte[16] { 159, 211, 27, 119, 107, 116, 75, 114, 132, 127, 45, 192, 13, 186, 147, 168 });
            CTFLeaderboardGUID = new Guid(new byte[16] { 184, 51, 137, 223, 18, 69, 67, 141, 177, 136, 137, 175, 148, 240, 73, 96 });
            MaxSquadMembers = 6;
            MaxSquads = 8;
            FlagUICount = 10;
            EnablePlayerCount = true;
            ShowPointsOnUI = false;
            ProgressChars = "¶·¸¹º»:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            PlayerIcon = '³';
            AttackIcon = 'µ';
            DefendIcon = '´';
            LockIcon = '²';
        }
    }
    public struct BARRICADE_IDS
    {
        [JsonConverter(typeof(GuidConverter))]
        public Guid InsurgencyCacheGUID;
        [JsonConverter(typeof(GuidConverter))]
        public Guid FOBGUID;
        [JsonConverter(typeof(GuidConverter))]
        public Guid FOBBaseGUID;
        public void SetDefaults()
        {
            InsurgencyCacheGUID = new Guid(new byte[16] { 57, 5, 31, 51, 242, 68, 73, 180, 179, 65, 125, 13, 102, 106, 79, 39 });
            FOBGUID = new Guid(new byte[16] { 97, 195, 73, 241, 0, 0, 73, 143, 162, 185, 44, 2, 157, 56, 229, 35 });
            FOBBaseGUID = new Guid(new byte[16] { 27, 177, 114, 119, 221, 129, 72, 223, 159, 76, 83, 209, 161, 155, 37, 3 });
        }
    }
    public struct MAP_CONFIG
    {
        public string Map;

        [JsonConverter(typeof(GuidConverter))]
        public Guid T1ZoneBlocker;
        [JsonConverter(typeof(GuidConverter))]
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
                    T1ZoneBlocker = new Guid(new byte[16] { 163, 201, 65, 197, 247, 23, 74, 71, 153, 248, 9, 167, 243, 12, 132, 120 });
                    T2ZoneBlocker = new Guid(new byte[16] { 209, 2, 197, 7, 216, 50, 70, 64, 162, 139, 208, 244, 234, 163, 117, 168 });
                    CacheSpawns = NuijamaaDefaultCaches;
                    break;
                default:
                    T1ZoneBlocker = Guid.Empty;
                    T2ZoneBlocker = Guid.Empty;
                    CacheSpawns = new SerializableTransform[0];
                    break;
            }
            Team1Adjacencies = new Dictionary<int, float>();
            Team2Adjacencies = new Dictionary<int, float>();
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
        public bool ShowLeaderboard;
        public int OverrideContestDifference;
        public bool AllowVehicleCapture;
        public int DiscoveryForesight;
        public int FlagTickInterval;
        public void SetDefaults()
        {
            StagingTime = 90;
            EvaluateTime = 0.25f;
            TicketXPInterval = 10;
            ShowLeaderboard = true;
            OverrideContestDifference = 2;
            AllowVehicleCapture = false;
            DiscoveryForesight = 2;
            FlagTickInterval = 4;
        }
    }
    public struct INVASION
    {
        public int StagingTime;
        public int DiscoveryForesight;
        public string SpecialFOBName;
        public void SetDefaults()
        {
            StagingTime = 90;
            DiscoveryForesight = 2;
            SpecialFOBName = "VCP";
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

    public class GuidConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(Guid);
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                throw new JsonReaderException("Type GUID can not be null! (GuidConverter.ReadJson)");
            }
            if (reader.TokenType == JsonToken.String)
            {
                string v = reader.Value.ToString();
                if (Guid.TryParseExact(v, "N", out Guid res))
                    return res;
                else if (Guid.TryParse(v, out res))
                    return res;
                else
                {
                    throw new JsonReaderException("Unable to parse " + v + " as a GUID! (GuidConverter.ReadJson)");
                }
            }
            else
                throw new JsonReaderException("Unable to parse non-string value as a GUID! (GuidConverter.ReadJson)");
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is Guid guid)
            {
                writer.WriteValue(guid.ToString("N"));
            }
            else
            {
                writer.WriteNull();
            }
        }
    }
}
