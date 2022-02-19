//#define USE_VOICE
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Networking;
using Uncreated.Networking.Encoding;
using UnityEngine;

namespace Uncreated.Warfare.ReportSystem
{
    public class Reporter : MonoBehaviour
    {
        /// <summary>
        /// T1: report <br>T2: isOnline</br>
        /// </summary>
        public static readonly NetCallRaw<Report, bool> SendReportInvocation = new NetCallRaw<Report, bool>(4000, Report.ReadReport, null, Report.WriteReport, null, 256);
        public static readonly NetCall<bool, string> ReceiveInvocationResponse = new NetCall<bool, string>(4001, 78, true);
        public static readonly Dictionary<EDamageOrigin, string> DmgOriginLocalization = new Dictionary<EDamageOrigin, string>(30)
        {
            { EDamageOrigin.Animal_Attack, "Animal Attack" },
            { EDamageOrigin.Bullet_Explosion, "Bullet Explosion" },
            { EDamageOrigin.Carepackage_Timeout, "Carepackage Expired" },
            { EDamageOrigin.Charge_Explosion, "Explosive Charge" },
            { EDamageOrigin.Charge_Self_Destruct, "Detonated Charge" },
            { EDamageOrigin.Flamable_Zombie_Explosion, "Flamable Zombie Explosion" },
            { EDamageOrigin.Food_Explosion, "Explosive Food" },
            { EDamageOrigin.Grenade_Explosion, "Explosive Grenade" },
            { EDamageOrigin.Horde_Beacon_Self_Destruct, "Horde Beacon Expired" },
            { EDamageOrigin.Kill_Volume, "Map Kill Volume" },
            { EDamageOrigin.Lightning, "Lightning Strike" },
            { EDamageOrigin.Mega_Zombie_Boulder, "Mega Zombie Boulder" },
            { EDamageOrigin.Plant_Harvested, "Plant Harvested" },
            { EDamageOrigin.Punch, "Punch" },
            { EDamageOrigin.Radioactive_Zombie_Explosion, "Radioactive Zombie Explosion" },
            { EDamageOrigin.Rocket_Explosion, "Explosive Rocket" },
            { EDamageOrigin.Sentry, "Sentry" },
            { EDamageOrigin.Trap_Explosion, "Trap Explosion" },
            { EDamageOrigin.Trap_Wear_And_Tear, "Trap Wear & Tear" },
            { EDamageOrigin.Unknown, "Unknown Damage Origin" },
            { EDamageOrigin.Useable_Gun, "Gun" },
            { EDamageOrigin.Useable_Melee, "Melee" },
            { EDamageOrigin.VehicleDecay, "Vehicle Despawn" },
            { EDamageOrigin.Vehicle_Bumper, "Ran Over" },
            { EDamageOrigin.Vehicle_Collision_Self_Damage, "Collision" },
            { EDamageOrigin.Vehicle_Explosion, "Vehicle Explosion" },
            { EDamageOrigin.Zombie_Electric_Shock, "Electric Zombie Shock" },
            { EDamageOrigin.Zombie_Fire_Breath, "Fire Zombie Breath" },
            { EDamageOrigin.Zombie_Stomp, "Zombie Stomp" },
            { EDamageOrigin.Zombie_Swipe, "Zombie Swipe" },
        };
        public static readonly Dictionary<EDeathCause, string> DeathCauseLocalization = new Dictionary<EDeathCause, string>(30);
        public Report CreateReport(ulong reporter, ulong violator, string message)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (data.TryGetValue(violator, out PlayerData pd))
            {
                return pd.CustomReport(message, reporter);
            }
            return null;
        }
        public ChatAbuseReport CreateChatAbuseReport(ulong reporter, ulong violator, string message)
        {
            if (data.TryGetValue(violator, out PlayerData pd))
            {
                return pd.ChatAbuseReport(message, reporter);
            }
            return null;
        }
        public VoiceChatAbuseReport CreateVoiceChatAbuseReport(ulong reporter, ulong violator, string message)
        {
            if (data.TryGetValue(violator, out PlayerData pd))
            {
                return pd.VoiceChatAbuseReport(message, reporter);
            }
            return null;
        }
        public SoloingReport CreateSoloingReport(ulong reporter, ulong violator, string message)
        {
            if (data.TryGetValue(violator, out PlayerData pd))
            {
                return pd.SoloingReport(message, reporter);
            }
            return null;
        }
        public WasteingAssetsReport CreateWasteingAssetsReport(ulong reporter, ulong violator, string message)
        {
            if (data.TryGetValue(violator, out PlayerData pd))
            {
                return pd.WasteingAssetsReport(message, reporter);
            }
            return null;
        }
        public IntentionalTeamkillReport CreateIntentionalTeamkillReport(ulong reporter, ulong violator, string message)
        {
            if (data.TryGetValue(violator, out PlayerData pd))
            {
                return pd.IntentionalTeamkillReport(message, reporter);
            }
            return null;
        }
        public GreifingFOBsReport CreateGreifingFOBsReport(ulong reporter, ulong violator, string message)
        {
            if (data.TryGetValue(violator, out PlayerData pd))
            {
                return pd.GreifingFOBsReport(message, reporter);
            }
            return null;
        }
        private void TickPlayer(PlayerData data, float deltaTime, float time)
        {
            Player player = data.player;
            InteractableVehicle veh = player.movement.getVehicle();
            if (veh != null)
            {
                int passengerCount = 0;
                for (int i = 0; i < veh.passengers.Length; i++)
                {
                    if (veh.passengers[i] != null)
                    {
                        passengerCount++;
                        if (passengerCount > 1)
                            break;
                    }
                }
                float lowerLimit = time - 1200f;
                if (passengerCount > 1)
                {
                    if (!data.soloTime.TryGetValue(veh.asset.GUID, out List<float> times))
                    {
                        times = new List<float>(120) { time };
                        data.soloTime.Add(veh.asset.GUID, times);
                    }
                    // remove times 20 minutes ago or older
                    for (int i = 0; i < times.Count; i++)
                    {
                        if (times[i] < lowerLimit)
                            times.RemoveAt(i--);
                        else break;
                    }
                    times.Add(time);
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
            foreach (KeyValuePair<ulong, PlayerData> player in data.ToList())
            {
                if (!player.Value.isOnline)
                {
                    player.Value.offlineTime += dt;
                    if (player.Value.offlineTime > 600f) // remove from list after 10 minutes
                    {
                        data.Remove(player.Key);
                    }
                }
                else player.Value.onlineTime += dt;
                for (int i = 0; i < player.Value.recentRequests.Count; i++)
                {
                    VehicleLifeData data = player.Value.recentRequests[i];
                    if (!data.died)
                    {
                        data.lifeTime += dt;
                        player.Value.recentRequests[i] = data;
                    }
                }
                if (tickTime)
                {
                    if (player.Value.isOnline)
                    {
                        PlayerData data = player.Value;
                        if (data.player == null)
                        {
                            SteamPlayer pl = PlayerTool.getSteamPlayer(player.Key);
                            if (pl == null)
                            {
                                data.isOnline = false;
                                continue;
                            }
                            else
                            {
                                data.player = pl.player;
                            }
                        }
                        TickPlayer(data, dt2, time);
                    }
                }
            }
            if (tickTime) lastTick = Time.realtimeSinceStartup;
        }
        internal void OnVehicleRequest(ulong player, Guid vehicle, uint bayInstID)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (data.TryGetValue(player, out PlayerData playerData))
            {
                playerData.recentRequests.Add(new VehicleLifeData()
                {
                    bayInstId = bayInstID,
                    died = false,
                    lifeTime = 0,
                    requestTime = Time.realtimeSinceStartup,
                    vehicle = vehicle
                });
            }
        }
        internal void OnVehicleDied(ulong owner, uint bayInstId, ulong killer, Guid vehicle, Guid weapon, EDamageOrigin origin, bool tk)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (bayInstId != uint.MaxValue && data.TryGetValue(owner, out PlayerData playerData))
            {
                for (int i = 0; i < playerData.recentRequests.Count; i++)
                {
                    VehicleLifeData data = playerData.recentRequests[i];
                    if (data.bayInstId == bayInstId && !data.died)
                    {
                        data.died = true;
                        playerData.recentRequests[i] = data;
                        break;
                    }
                }
            }
            if (tk && data.TryGetValue(killer, out playerData))
            {
                playerData.vehicleTeamkills.Add(new VehicleTeamkill()
                {
                    owner = owner,
                    time = Time.realtimeSinceStartup,
                    vehicle = vehicle,
                    weapon = weapon,
                    origin = origin
                });
            }
        }
        internal void OnPlayerJoin(SteamPlayer player)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            foreach (KeyValuePair<ulong, PlayerData> pkv in data)
            {
                if (pkv.Key == player.playerID.steamID.m_SteamID)
                {
                    pkv.Value.isOnline = true;
                    pkv.Value.onlineTime = 0;
                    pkv.Value.characterName = player.playerID.characterName;
                    pkv.Value.playerName = player.playerID.playerName;
                    pkv.Value.nickName = player.playerID.nickName;
                    return;
                }
            }
            data.Add(player.playerID.steamID.m_SteamID, new PlayerData(player.playerID.steamID.m_SteamID) 
            {
                isOnline = true,
                characterName = player.playerID.characterName,
                playerName = player.playerID.playerName,
                nickName = player.playerID.nickName
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
                foreach (KeyValuePair<ulong, PlayerData> current in data.OrderBy(x => x.Value.characterName.Length))
                {
                    if (current.Value.characterName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                        return current.Value.Steam64;
                }
                foreach (KeyValuePair<ulong, PlayerData> current in data.OrderBy(x => x.Value.nickName.Length))
                {
                    if (current.Value.nickName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                        return current.Value.Steam64;
                }
                foreach (KeyValuePair<ulong, PlayerData> current in data.OrderBy(x => x.Value.playerName.Length))
                {
                    if (current.Value.playerName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                        return current.Value.Steam64;
                }
                return 0;
            }
            else if (type == UCPlayer.ENameSearchType.NICK_NAME)
            {
                foreach (KeyValuePair<ulong, PlayerData> current in data.OrderBy(x => x.Value.nickName.Length))
                {
                    if (current.Value.nickName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                        return current.Value.Steam64;
                }
                foreach (KeyValuePair<ulong, PlayerData> current in data.OrderBy(x => x.Value.characterName.Length))
                {
                    if (current.Value.characterName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                        return current.Value.Steam64;
                }
                foreach (KeyValuePair<ulong, PlayerData> current in data.OrderBy(x => x.Value.playerName.Length))
                {
                    if (current.Value.playerName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                        return current.Value.Steam64;
                }
                return 0;
            }
            else if (type == UCPlayer.ENameSearchType.PLAYER_NAME)
            {
                foreach (KeyValuePair<ulong, PlayerData> current in data.OrderBy(x => x.Value.playerName.Length))
                {
                    if (current.Value.playerName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                        return current.Value.Steam64;
                }
                foreach (KeyValuePair<ulong, PlayerData> current in data.OrderBy(x => x.Value.nickName.Length))
                {
                    if (current.Value.nickName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                        return current.Value.Steam64;
                }
                foreach (KeyValuePair<ulong, PlayerData> current in data.OrderBy(x => x.Value.characterName.Length))
                {
                    if (current.Value.characterName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                        return current.Value.Steam64;
                }
                return 0;
            }
            else return RecentPlayerNameCheck(name, UCPlayer.ENameSearchType.CHARACTER_NAME);
        }
        internal void OnTeamkill(ulong killer, Guid weapon, ulong dead, EDeathCause cause)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (data.TryGetValue(killer, out PlayerData playerData))
            {
                playerData.teamkills.Add(new Teamkill()
                {
                    cause = cause,
                    dead = dead,
                    time = Time.realtimeSinceStartup,
                    weapon = weapon
                });
            }
        }
        internal void OnPlayerChat(ulong player, string message)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (data.TryGetValue(player, out PlayerData playerData))
                playerData.InsertChat(message);
        }
        internal void OnDamagedStructure(ulong player, StructureDamageData data)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (this.data.TryGetValue(player, out PlayerData playerData))
            {
                for (int i = 0; i < playerData.recentFriendlyDamages.Count; i++)
                {
                    if (playerData.recentFriendlyDamages[i].instId == data.instId)
                    {
                        if (!playerData.recentFriendlyDamages[i].broke)
                        {
                            data.damage += playerData.recentFriendlyDamages[i].damage;
                            playerData.recentFriendlyDamages[i] = data;
                            return;
                        }
                    }
                }
                playerData.recentFriendlyDamages.Add(data);
            }
        }
        internal void OnDestroyedStructure(ulong player, uint instId)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (this.data.TryGetValue(player, out PlayerData playerData))
            {
                for (int i = 0; i < playerData.recentFriendlyDamages.Count; i++)
                {
                    StructureDamageData data = playerData.recentFriendlyDamages[i];
                    if (data.instId == instId)
                    {
                        data.broke = true;
                        playerData.recentFriendlyDamages[i] = data;
                    }
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
        private readonly Dictionary<ulong, PlayerData> data = new Dictionary<ulong, PlayerData>(64);
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
                        Origin = DmgOriginLocalization.TryGetValue(data.origin, out string orig) ? orig : data.origin.ToString().Replace('_', ' '),
                        Timestamp = data.time.FromUnityTime(),
                        VehicleOwner = data.owner,
                        Weapon = Assets.find<ItemAsset>(data.weapon)?.itemName ?? data.weapon.ToString("N"),
                    };
                }
                return vehicles;
            }
            public WasteingAssetsReport WasteingAssetsReport(string message, ulong reporter) =>
                new WasteingAssetsReport()
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
                Report.Teamkill[] teamkills = new Report.Teamkill[vehicleTeamkills.Count];
                for (int i = 0; i < vehicleTeamkills.Count; i++)
                {
                    Teamkill data = this.teamkills[i];
                    teamkills[i] = new Report.Teamkill()
                    {
                        Dead = data.dead,
                        DeathType = DeathCauseLocalization.TryGetValue(data.cause, out string cause) ? cause : data.cause.ToString().Replace('_', ' '),
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
                for (int i = 0; i < vehicleTeamkills.Count; i++)
                {
                    StructureDamageData data = recentFriendlyDamages[i];
                    damages[i] = new GreifingFOBsReport.StructureDamage()
                    {
                        Damage = data.damage,
                        DamageOrigin = DmgOriginLocalization.TryGetValue(data.origin, out string orig) ? orig : data.origin.ToString().Replace('_', ' '),
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
    }
}
