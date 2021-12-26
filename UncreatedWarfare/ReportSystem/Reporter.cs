//#define USE_VOICE
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Networking;
using Uncreated.Networking.Encoding;
using UnityEngine;

namespace Uncreated.Warfare.ReportSystem
{
    public class Reporter : MonoBehaviour
    {
        public static readonly NetCallRaw<Report> SendReportInvocation = new NetCallRaw<Report>(4000, Report.ReadReport, Report.WriteReport);
        public static void SendReport(Report report) => SendReportInvocation.NetInvoke(report);
        public static bool CreateCustomReport(ulong reporter, ulong violator, string message)
        {
            if (Data.NetClient.connection.IsActive)
            {
                SendReport(new Report()
                {
                    Message = message,
                    Reporter = reporter,
                    Violator = violator,
                    Time = DateTime.Now
                });
                return true;
            }
            return false;
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
                        times = new List<float>(120);
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
            }
        }
        float lastTick = 0;
        private void Update()
        {
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
                    if (!player.Value.recentRequests[i].died)
                        player.Value.recentRequests[i].addTime(dt);
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
            if (data.TryGetValue(player, out PlayerData playerData))
            {
                for (int i = 0; i < playerData.recentRequests.Count; i++)
                {
                    if (playerData.recentRequests[i].bayInstId == bayInstID && !playerData.recentRequests[i].died)
                    {
                        playerData.recentRequests[i].setDead(true);
                        break;
                    }
                }
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
        internal void OnVehicleDied(ulong owner, uint bayInstId)
        {
            if (data.TryGetValue(owner, out PlayerData playerData))
            {
                for (int i = 0; i < playerData.recentRequests.Count; i++)
                {
                    if (playerData.recentRequests[i].bayInstId == bayInstId && !playerData.recentRequests[i].died)
                    {
                        playerData.recentRequests[i].setDead(true);
                        break;
                    }
                }
            }
        }
        internal void OnPlayerJoin(ulong player)
        {
            foreach (KeyValuePair<ulong, PlayerData> pkv in data)
            {
                if (pkv.Key == player)
                {
                    pkv.Value.isOnline = true;
                    pkv.Value.onlineTime = 0;
                    return;
                }
            }
            data.Add(player, new PlayerData(player) { isOnline = true });
        }
        internal void OnPlayerChat(ulong player, string message)
        {
            if (data.TryGetValue(player, out PlayerData playerData))
                playerData.InsertChat(message);
        }
        internal void OnDamagedStructure(ulong player, StructureDamageData data)
        {
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
            if (this.data.TryGetValue(player, out PlayerData playerData))
            {
                for (int i = 0; i < playerData.recentFriendlyDamages.Count; i++)
                {
                    if (playerData.recentFriendlyDamages[i].instId == instId)
                    {
                        playerData.recentFriendlyDamages[i].setBroke(true);
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
            public float offlineTime;
            public float onlineTime;
            public Dictionary<Guid, List<float>> soloTime = new Dictionary<Guid, List<float>>(8);
            public List<VehicleLifeData> recentRequests = new List<VehicleLifeData>(8);
            public List<StructureDamageData> recentFriendlyDamages = new List<StructureDamageData>(16);
            public List<KeyValuePair<int, string>> chatLogs = new List<KeyValuePair<int, string>>(256);
#if USE_VOICE
            public List<byte[]> voiceHistory = new List<byte[]>(0);
#endif
            public void InsertChat(string message)
            {
                if (message.Length < 1 || message[0] == '/') return;
                if (chatLogs.Count > 0 && chatLogs[0].Value == message)
                {
                    chatLogs[0] = new KeyValuePair<int, string>(chatLogs[0].Key + 1, chatLogs[0].Value);
                    return;
                }
                if (chatLogs.Count > 255)
                {
                    chatLogs.RemoveAt(chatLogs.Count - 1);
                }
                chatLogs.Insert(0, new KeyValuePair<int, string>(1, message));
            }
            public PlayerData(ulong steam64) => Steam64 = steam64;
            
        }
        private struct VehicleLifeData
        {
            public Guid vehicle;
            public uint bayInstId;
            public float requestTime;
            public float lifeTime;
            public bool died;
            public void setDead(bool val) => died = val;
            public void addTime(float time) => lifeTime += time;
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
            public void setBroke(bool state) => this.broke = state;
        }
    }
}
