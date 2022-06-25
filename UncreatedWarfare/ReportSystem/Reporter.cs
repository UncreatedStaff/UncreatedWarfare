//#define USE_VOICE
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using UnityEngine;

namespace Uncreated.Warfare.ReportSystem;

public class Reporter : MonoBehaviour
{
    void Start()
    {
        EventDispatcher.OnPlayerDied += OnPlayerDied;
    }
    void OnDestroy()
    {
        EventDispatcher.OnPlayerDied -= OnPlayerDied;
    }
    public Report? CreateReport(ulong reporter, ulong violator, string message)
    {
        for (int i = 0; i < data.Count; ++i)
        {
            if (data[i].Steam64 == violator)
                return data[i].CustomReport(message, reporter);
        }
        return null;
    }
    public ChatAbuseReport? CreateChatAbuseReport(ulong reporter, ulong violator, string message)
    {
        for (int i = 0; i < data.Count; ++i)
        {
            if (data[i].Steam64 == violator)
                return data[i].ChatAbuseReport(message, reporter);
        }
        return null;
    }
    public CheatingReport? CreateCheatingReport(ulong reporter, ulong violator, string message)
    {
        for (int i = 0; i < data.Count; ++i)
        {
            if (data[i].Steam64 == violator)
                return data[i].CheatingReport(message, reporter);
        }
        return null;
    }
    public VoiceChatAbuseReport? CreateVoiceChatAbuseReport(ulong reporter, ulong violator, string message)
    {
        for (int i = 0; i < data.Count; ++i)
        {
            if (data[i].Steam64 == violator)
                return data[i].VoiceChatAbuseReport(message, reporter);
        }
        return null;
    }
    public SoloingReport? CreateSoloingReport(ulong reporter, ulong violator, string message)
    {
        for (int i = 0; i < data.Count; ++i)
        {
            if (data[i].Steam64 == violator)
                return data[i].SoloingReport(message, reporter);
        }
        return null;
    }
    public WastingAssetsReport? CreateWastingAssetsReport(ulong reporter, ulong violator, string message)
    {
        for (int i = 0; i < data.Count; ++i)
        {
            if (data[i].Steam64 == violator)
                return data[i].WastingAssetsReport(message, reporter);
        }
        return null;
    }
    public IntentionalTeamkillReport? CreateIntentionalTeamkillReport(ulong reporter, ulong violator, string message)
    {
        for (int i = 0; i < data.Count; ++i)
        {
            if (data[i].Steam64 == violator)
                return data[i].IntentionalTeamkillReport(message, reporter);
        }
        return null;
    }
    public GreifingFOBsReport? CreateGreifingFOBsReport(ulong reporter, ulong violator, string message)
    {
        for (int i = 0; i < data.Count; ++i)
        {
            if (data[i].Steam64 == violator)
                return data[i].GreifingFOBsReport(message, reporter);
        }
        return null;
    }
    private void TickPlayer(PlayerData data, float deltaTime, float time)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Player player = data.player;
        InteractableVehicle veh = player.movement.getVehicle();
        if (veh != null)
        {
            float lowerLimit = time - 1200f;

            Guid g = veh.asset.GUID;
            // check if the player is in an emplacement
            if (FOBs.FOBManager.Config.Buildables.Exists(x => x.Emplacement is not null && x.Emplacement.EmplacementVehicle == g))
                goto timeAdj;
            int passengerCount = 0;
            for (int i = 0; i < veh.passengers.Length; i++)
            {
                if (veh.passengers[i] is not null)
                {
                    if (++passengerCount > 1) break;
                }
            }
            if (passengerCount == 1)
            {
                if (!data.soloTime.TryGetValue(veh.asset.GUID, out List<float> times))
                {
                    times = new List<float>(120) { time };
                    data.soloTime.Add(veh.asset.GUID, times);
                }
                else
                {
                    times.Add(time);
                }
            }
            timeAdj:
            foreach (KeyValuePair<Guid, List<float>> kvp in data.soloTime)
            {
                List<float> times = kvp.Value;
                for (int i = 0; i < times.Count; ++i)
                {
                    if (times[i] < lowerLimit)
                        times.RemoveAt(i--);
                    else break;
                }
            }
            for (int i = 0; i < data.recentRequests.Count; i++)
            {
                if (data.recentRequests[i].requestTime < lowerLimit)
                    data.recentRequests.RemoveAt(i--);
                else break;
            }
            for (int i = 0; i < data.recentFriendlyDamages.Count; i++)
            {
                if (data.recentFriendlyDamages[i].time < lowerLimit)
                    data.recentFriendlyDamages.RemoveAt(i--);
                else break;
            }
            for (int i = 0; i < data.teamkills.Count; i++)
            {
                if (data.teamkills[i].time < lowerLimit)
                    data.teamkills.RemoveAt(i--);
                else break;
            }
            for (int i = 0; i < data.vehicleTeamkills.Count; i++)
            {
                if (data.vehicleTeamkills[i].time < lowerLimit)
                    data.vehicleTeamkills.RemoveAt(i--);
                else break;
            }
        }
    }
    float lastTick = 0;
    private void Update()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float dt = Time.deltaTime;
        float time = Time.realtimeSinceStartup;
        bool tickTime = time - lastTick > 5f;
        float dt2 = time - lastTick;
        for (int i = data.Count - 1; i >= 0; --i)
        {
            PlayerData player = data[i];
            if (!player.isOnline)
            {
                player.offlineTime += dt;
                if (player.offlineTime > 600f) // remove from list after 10 minutes
                {
                    data.RemoveAt(i);
                    continue;
                }
            }
            else player.onlineTime += dt;
            for (int j = 0; j < player.recentRequests.Count; j++)
            {
                VehicleLifeData data = player.recentRequests[j];
                if (!data.died)
                {
                    data.lifeTime += dt;
                    player.recentRequests[j] = data;
                }
            }
            if (tickTime)
            {
                if (player.isOnline)
                {
                    if (player.player == null)
                    {
                        SteamPlayer pl = PlayerTool.getSteamPlayer(player.Steam64);
                        if (pl == null)
                        {
                            player.isOnline = false;
                            continue;
                        }
                        else
                        {
                            player.player = pl.player;
                        }
                    }
                    TickPlayer(player, dt2, time);
                }
            }
        }
        if (tickTime) lastTick = Time.realtimeSinceStartup;
    }
    private void OnPlayerDied(PlayerDied e)
    {
        if (e.WasTeamkill && e.Killer is not null)
        {
            ulong k = e.Killer;
            for (int i = 0; i < data.Count; ++i)
            {
                if (data[i].Steam64 == k)
                {
                    data[i].teamkills.Add(new Teamkill()
                    {
                        cause = e.Cause,
                        dead = e.Player,
                        time = Time.realtimeSinceStartup,
                        weapon = e.PrimaryAsset
                    });
                    return;
                }
            }
        }
    }
    internal void OnVehicleRequest(ulong player, Guid vehicle, uint bayInstID)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < data.Count; ++i)
        {
            if (data[i].Steam64 == player)
            {
                data[i].recentRequests.Add(new VehicleLifeData()
                {
                    bayInstId = bayInstID,
                    died = false,
                    lifeTime = 0,
                    requestTime = Time.realtimeSinceStartup,
                    vehicle = vehicle
                });
                break;
            }
        }    
    }
    internal void OnVehicleDied(ulong owner, uint bayInstId, ulong killer, Guid vehicle, Guid weapon, EDamageOrigin origin, bool tk)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (bayInstId != uint.MaxValue)
        {
            for (int i = 0; i < data.Count; ++i)
            {
                if (data[i].Steam64 == owner)
                {
                    PlayerData playerData = data[i];
                    for (int j = 0; j < playerData.recentRequests.Count; j++)
                    {
                        VehicleLifeData data = playerData.recentRequests[j];
                        if (data.bayInstId == bayInstId && !data.died)
                        {
                            data.died = true;
                            playerData.recentRequests[j] = data;
                            break;
                        }
                    }
                    break;
                }
            }
        }
        if (tk)
        {
            for (int i = 0; i < data.Count; ++i)
            {
                if (data[i].Steam64 == killer)
                {
                    PlayerData playerData = data[i];
                    playerData.vehicleTeamkills.Add(new VehicleTeamkill()
                    {
                        owner = owner,
                        time = Time.realtimeSinceStartup,
                        vehicle = vehicle,
                        weapon = weapon,
                        origin = origin
                    });
                    break;
                }
            }
        }
    }
    internal void OnPlayerJoin(SteamPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < data.Count; ++i)
        {
            if (data[i].Steam64 == player.playerID.steamID.m_SteamID)
            {
                PlayerData playerData = data[i];
                playerData.isOnline = true;
                playerData.onlineTime = 0;
                playerData.characterName = player.playerID.characterName;
                playerData.playerName = player.playerID.playerName;
                playerData.nickName = player.playerID.nickName;
                playerData.player = player.player;
                return;
            }
        }
        data.Add(new PlayerData(player.playerID.steamID.m_SteamID) 
        {
            isOnline = true,
            characterName = player.playerID.characterName,
            playerName = player.playerID.playerName,
            nickName = player.playerID.nickName,
            player = player.player
        });
    }
    /// <summary>Slow, use rarely.</summary>
    public ulong RecentPlayerNameCheck(string name, UCPlayer.ENameSearchType type)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (type == UCPlayer.ENameSearchType.CHARACTER_NAME)
        {
            foreach (PlayerData current in data.OrderBy(x => x.characterName.Length))
            {
                if (current.characterName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                    return current.Steam64;
            }
            foreach (PlayerData current in data.OrderBy(x => x.nickName.Length))
            {
                if (current.nickName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                    return current.Steam64;
            }
            foreach (PlayerData current in data.OrderBy(x => x.playerName.Length))
            {
                if (current.playerName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                    return current.Steam64;
            }
            return 0;
        }
        else if (type == UCPlayer.ENameSearchType.NICK_NAME)
        {
            foreach (PlayerData current in data.OrderBy(x => x.nickName.Length))
            {
                if (current.nickName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                    return current.Steam64;
            }
            foreach (PlayerData current in data.OrderBy(x => x.characterName.Length))
            {
                if (current.characterName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                    return current.Steam64;
            }
            foreach (PlayerData current in data.OrderBy(x => x.playerName.Length))
            {
                if (current.playerName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                    return current.Steam64;
            }
            return 0;
        }
        else if (type == UCPlayer.ENameSearchType.PLAYER_NAME)
        {
            foreach (PlayerData current in data.OrderBy(x => x.playerName.Length))
            {
                if (current.playerName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                    return current.Steam64;
            }
            foreach (PlayerData current in data.OrderBy(x => x.nickName.Length))
            {
                if (current.nickName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                    return current.Steam64;
            }
            foreach (PlayerData current in data.OrderBy(x => x.characterName.Length))
            {
                if (current.characterName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                    return current.Steam64;
            }
            return 0;
        }
        else return RecentPlayerNameCheck(name, UCPlayer.ENameSearchType.CHARACTER_NAME);
    }
    internal void OnPlayerChat(ulong player, string message)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < data.Count; ++i)
        {
            if (data[i].Steam64 == player)
            {
                data[i].InsertChat(message);
                return;
            }
        }
    }
    internal void OnDamagedStructure(ulong player, StructureDamageData strdata)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < data.Count; ++i)
        {
            if (data[i].Steam64 == player)
            {
                PlayerData playerData = data[i];
                for (int j = 0; j < playerData.recentFriendlyDamages.Count; j++)
                {
                    if (playerData.recentFriendlyDamages[j].instId == strdata.instId)
                    {
                        if (!playerData.recentFriendlyDamages[j].broke)
                        {
                            strdata.damage += playerData.recentFriendlyDamages[j].damage;
                            playerData.recentFriendlyDamages[j] = strdata;
                            return;
                        }
                    }
                }
                playerData.recentFriendlyDamages.Add(strdata);
                return;
            }
        }
    }
    internal void OnDestroyedStructure(ulong player, uint instId)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < data.Count; ++i)
        {
            if (data[i].Steam64 == player)
            {
                PlayerData playerData = data[i];
                for (int j = 0; j < playerData.recentFriendlyDamages.Count; j++)
                {
                    StructureDamageData data = playerData.recentFriendlyDamages[j];
                    if (data.instId == instId)
                    {
                        data.broke = true;
                        playerData.recentFriendlyDamages[j] = data;
                    }
                }
                return;
            }
        }
    }
#if USE_VOICE
    public void AddVoiceHistory(ulong player, byte[] voiceData)
    {
        foreach (KeyValuePair<ulong, PlayerData> pkv in data)
        {
            if (pkv.Key == player)
            {
                pkv.Value.voiceHistory.Add(voiceData);
                return;
            }
        }
    }
#endif
    private readonly List<PlayerData> data = new List<PlayerData>(64);
    private class PlayerData
    {
        public readonly ulong Steam64;
        public Player player;
        public bool isOnline;
        public string characterName;
        public string playerName;
        public string nickName;
        public float offlineTime;
        public float onlineTime;
        public Dictionary<Guid, List<float>> soloTime = new Dictionary<Guid, List<float>>(8);
        public List<VehicleLifeData> recentRequests = new List<VehicleLifeData>(8);
        public List<StructureDamageData> recentFriendlyDamages = new List<StructureDamageData>(16);
        public List<KeyValuePair<int, KeyValuePair<string, DateTime>>> chatLogs = new List<KeyValuePair<int, KeyValuePair<string, DateTime>>>(256);
        public List<Teamkill> teamkills = new List<Teamkill>();
        public List<VehicleTeamkill> vehicleTeamkills = new List<VehicleTeamkill>();
        //public List<byte[]> voiceHistory = new List<byte[]>(0);
        
        public void InsertChat(string message)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (message.Length < 1 || message[0] == '/') return;
            if (chatLogs.Count > 0 && chatLogs[0].Value.Key == message && (DateTime.Now - chatLogs[0].Value.Value).TotalSeconds < 10d)
            {
                chatLogs[0] = new KeyValuePair<int, KeyValuePair<string, DateTime>>(chatLogs[0].Key + 1, chatLogs[0].Value);
                return;
            }
            if (chatLogs.Count > 255)
            {
                chatLogs.RemoveAt(chatLogs.Count - 1);
            }
            chatLogs.Insert(0, new KeyValuePair<int, KeyValuePair<string, DateTime>>(1, new KeyValuePair<string, DateTime>(message, DateTime.Now)));
        }

        public PlayerData(ulong steam64) => Steam64 = steam64;
        public Report CustomReport(string message, ulong reporter) =>
            new Report()
            {
                Message = message,
                Reporter = reporter,
                Time = DateTime.Now,
                Violator = Steam64
            };
        private string[] ConvertChatLogs()
        {
            string[] chatlogs = new string[chatLogs.Count];
            int i = 0;
            foreach (KeyValuePair<int, KeyValuePair<string, DateTime>> kvp in chatLogs)
            {
                chatlogs[i] = kvp.Key > 1 ? $"[{kvp.Key}x] [1st {kvp.Value.Value:HH:mm:ss EST}] {kvp.Value.Key}" : $"[{kvp.Value.Value:HH:mm:ss EST}] {kvp.Value.Key}";
                i++;
            }
            return chatlogs;
        }
        public ChatAbuseReport ChatAbuseReport(string message, ulong reporter) =>
            new ChatAbuseReport()
            {
                ChatRecords = ConvertChatLogs(),
                Message = message,
                Reporter = reporter,
                Time = DateTime.Now,
                Violator = Steam64
            };
        public CheatingReport CheatingReport(string message, ulong reporter) =>
            new CheatingReport()
            {
                Message = message,
                Reporter = reporter,
                Time = DateTime.Now,
                Violator = Steam64
            };
        public byte[] ConvertVoiceLogs()
        {
            return new byte[0];
        }
        public VoiceChatAbuseReport VoiceChatAbuseReport(string message, ulong reporter) =>
            new VoiceChatAbuseReport()
            {
#if USE_VOICE
                VoiceRecords = ConvertVoiceLogs(),
#else
                VoiceRecords = new byte[0],
#endif
                Message = message,
                Reporter = reporter,
                Time = DateTime.Now,
                Violator = Steam64
            };
        public Report.VehicleTime[] ConvertSoloData()
        {
            Report.VehicleTime[] vehicles = new Report.VehicleTime[soloTime.Count];
            int i = 0;
            foreach (KeyValuePair<Guid, List<float>> kvp in soloTime)
            {
                vehicles[i] = new Report.VehicleTime()
                {
                    VehicleName = Assets.find<VehicleAsset>(kvp.Key)?.vehicleName ?? kvp.Key.ToString("N"),
                    Time = kvp.Value.Count * 5f,
                    Timestamp = (Time.realtimeSinceStartup - kvp.Value.LastOrDefault() >= 10) ? DateTime.MaxValue : DateTime.MinValue
                };
                i++;
            }
            return vehicles;
        }
        public SoloingReport SoloingReport(string message, ulong reporter) =>
            new SoloingReport()
            {
                Message = message,
                Reporter = reporter,
                Time = DateTime.Now,
                Violator = Steam64,
                Seats = ConvertSoloData()
            };
        public Report.VehicleTime[] ConvertRecentRequestsData()
        {
            Report.VehicleTime[] vehicles = new Report.VehicleTime[recentRequests.Count];
            for (int i = 0; i < recentRequests.Count; i++)
            {
                VehicleLifeData data = recentRequests[i];
                vehicles[i] = new Report.VehicleTime()
                {
                    VehicleName = Assets.find<VehicleAsset>(data.vehicle)?.vehicleName ?? data.vehicle.ToString("N"),
                    Time = data.lifeTime,
                    Timestamp = data.requestTime.FromUnityTime()
                };
            }
            return vehicles;
        }
        public Report.VehicleTeamkill[] ConvertRecentVehicleTeamkillData()
        {
            Report.VehicleTeamkill[] vehicles = new Report.VehicleTeamkill[vehicleTeamkills.Count];
            for (int i = 0; i < vehicleTeamkills.Count; i++)
            {
                VehicleTeamkill data = vehicleTeamkills[i];
                vehicles[i] = new Report.VehicleTeamkill()
                {
                    VehicleName = Assets.find<VehicleAsset>(data.vehicle)?.vehicleName ?? data.vehicle.ToString("N"),
                    Origin = Translation.TranslateEnum(data.origin, 0),
                    Timestamp = data.time.FromUnityTime(),
                    VehicleOwner = data.owner,
                    Weapon = Assets.find<ItemAsset>(data.weapon)?.itemName ?? data.weapon.ToString("N"),
                };
            }
            return vehicles;
        }
        public WastingAssetsReport WastingAssetsReport(string message, ulong reporter) =>
            new WastingAssetsReport()
            {
                Message = message,
                Reporter = reporter,
                Violator = Steam64,
                Time = DateTime.Now,
                RecentRequests = ConvertRecentRequestsData(),
                RecentVehicleTeamkills = ConvertRecentVehicleTeamkillData()
            };
        public Report.Teamkill[] ConvertRecentTeamkills()
        {
            Report.Teamkill[] teamkills = new Report.Teamkill[this.teamkills.Count];
            for (int i = 0; i < this.teamkills.Count; i++)
            {
                Teamkill data = this.teamkills[i];
                teamkills[i] = new Report.Teamkill()
                {
                    Dead = data.dead,
                    DeathType = Translation.TranslateEnum(data.cause, 0),
                    Weapon = Assets.find<ItemAsset>(data.weapon)?.itemName ?? data.weapon.ToString("N"),
                    IsVehicle = false,
                    Timestamp = data.time.FromUnityTime()
                };
            }
            return teamkills;
        }
        public IntentionalTeamkillReport IntentionalTeamkillReport(string message, ulong reporter) =>
            new IntentionalTeamkillReport()
            {
                Message = message,
                Reporter = reporter,
                Violator = Steam64,
                Time = DateTime.Now,
                RecentTeamkills = ConvertRecentTeamkills()
            };

        public GreifingFOBsReport.StructureDamage[] ConvertRecentFOBDamage()
        {
            GreifingFOBsReport.StructureDamage[] damages = new GreifingFOBsReport.StructureDamage[vehicleTeamkills.Count];
            for (int i = 0; i < recentFriendlyDamages.Count; i++)
            {
                StructureDamageData data = recentFriendlyDamages[i];
                damages[i] = new GreifingFOBsReport.StructureDamage()
                {
                    Damage = data.damage,
                    DamageOrigin = Translation.TranslateEnum(data.origin, 0),
                    Structure = Assets.find<ItemAsset>(data.structure)?.itemName ?? data.structure.ToString("N"),
                    Timestamp = data.time.FromUnityTime(),
                    Weapon = Assets.find<ItemAsset>(data.weapon)?.itemName ?? data.weapon.ToString("N")
                };
            }
            return damages;
        }
        public GreifingFOBsReport GreifingFOBsReport(string message, ulong reporter) =>
            new GreifingFOBsReport()
            {
                Message = message,
                Reporter = reporter,
                Violator = Steam64,
                Time = DateTime.Now,
                RecentDamage = ConvertRecentFOBDamage()
            };
    }
    private struct VehicleLifeData
    {
        public Guid vehicle;
        public uint bayInstId;
        public float requestTime;
        public float lifeTime;
        public bool died;
    }

    private struct VehicleTeamkill
    {
        public Guid vehicle;
        public Guid weapon;
        public float time;
        public ulong owner;
        public EDamageOrigin origin;
    }

    private struct Teamkill
    {
        public Guid weapon;
        public ulong dead;
        public EDeathCause cause;
        public float time;
    }

    internal struct StructureDamageData
    {
        public uint instId;
        public Guid structure;
        public float damage;
        public bool broke;
        public float time;
        public Guid weapon;
        public EDamageOrigin origin;
    }
    public static class NetCalls
    {
        /// <summary>T1: report <br>T2: isOnline</br></summary>
        public static readonly NetCallRaw<Report?, bool> SendReportInvocation = new NetCallRaw<Report?, bool>(4000, Report.ReadReport, null, Report.WriteReport!, null, 256);
        public static readonly NetCall<bool, string> ReceiveInvocationResponse = new NetCall<bool, string>(4001, 78);
    }
}
