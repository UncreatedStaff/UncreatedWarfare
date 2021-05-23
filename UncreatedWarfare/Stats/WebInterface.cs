using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UncreatedWarfare.Stats
{
    
    public struct Response
    {
        public bool Success;
        public string Reply;

        public Response(bool Success, string Reply)
        {
            this.Success = Success;
            this.Reply = Reply;
        }
    }
    public class WebClientWithTimeout : WebClient
    {
        public TimeSpan Timeout = WebInterface.DefaultTimeout;
        // https://stackoverflow.com/questions/1789627/how-to-change-the-timeout-on-a-net-webclient-object
        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest w = base.GetWebRequest(address);
            w.Timeout = (int)Math.Round(Timeout.TotalMilliseconds);
            return w;
        }
    }
    public class WebInterface : IDisposable
    {
        public static readonly TimeSpan DefaultTimeout = new TimeSpan(0, 0, 5);
        public class Query
        {
            public string URL;
            public string Parameters;
            public string FailureReply;
            public Query(string URL, string Parameters, string failureResponse)
            {
                this.URL = URL;
                this.Parameters = Parameters;
                this.FailureReply = failureResponse;
            }
            public Query(string URL, string function, Dictionary<string, string> Parameters, string failureResponse)
            {
                this.URL = URL;
                this.FailureReply = failureResponse;
                string p = "call=" + function;
                //build url
                for (int i = 0; i < Parameters.Count; i++)
                {
                    p += "&";
                    p += Parameters.Keys.ElementAt(i).EncodeURIComponent();
                    p += "=";
                    p += Parameters.Values.ElementAt(i).EncodeURIComponent();
                }
                this.Parameters = p;
            }
            public delegate void AsyncQueryDelegate(WebClientWithTimeout _client, out Response res);
            public void ExecuteQueryAsync(WebClientWithTimeout _client, out Response r)
            {
                while (_client.IsBusy)
                    Thread.Sleep(1);
                try
                {
                    string url = URL + '?' + Parameters;
                    if (url.Length > 65519)
                    {
                        r.Reply = "TOO LONG";
                        r.Success = false;
                        F.LogError("Web Request Too long: \n" + Parameters.Substring(0, Parameters.Length > 200 ? 200 : Parameters.Length) + "...");
                        return;
                    }
                    F.Log("Starting web request: \"" + url.Substring(0, url.Length > 200 ? 200 : url.Length) + '\"', ConsoleColor.DarkYellow);
                    r.Reply = _client.UploadString(url, "");
                }
                catch (WebException ex)
                {
                    string msg = ex.Message;
                    if (msg.StartsWith("Error: ConnectFailure"))
                        msg = "Could not connect to Node server at \"" + UCWarfare.Config.PlayerStatsSettings.NJS_ServerURL + '\"';
                    r.Reply = ex.Message;
                    r.Success = false;
                    F.LogError("Web Request Error: " + msg);
                    return;
                }
                r.Success = r.Reply != FailureReply && r.Reply != InvalidCallResponse;
            }
        }
        public void LogWarning(ulong Violator, ulong Admin, string ViolatorName, string AdminName, byte WarnedTeam, string reason) =>
            LogWarning(Violator, Admin, ViolatorName, AdminName, WarnedTeam, reason, DateTime.Now);
        public void LogWarning(ulong Violator, ulong Admin, string ViolatorName, string AdminName, byte WarnedTeam, string reason, DateTime WarnTime)
        {
            throw new NotImplementedException();
        }
        public void LogUnban(ulong Pardoned, ulong Pardoner, string PardonedName, string PardonerName) =>
            LogUnban(Pardoned, Pardoner, PardonedName, PardonerName, DateTime.Now);
        public void LogUnban(ulong Pardoned, ulong Pardoner, string PardonedName, string PardonerName, DateTime UnbanTime)
        {
            throw new NotImplementedException();
        }

        public void LogKick(ulong Violator, ulong Kicker, string ViolatorName, string AdminName, byte KickedTeam, string Reason) =>
            LogKick(Violator, Kicker, ViolatorName, AdminName, KickedTeam, Reason, DateTime.Now);
        public void LogKick(ulong Violator, ulong Kicker, string ViolatorName, string AdminName, byte KickedTeam, string Reason, DateTime KickTime)
        {
            throw new NotImplementedException();
        }

        public IAsyncResult LogBan(ulong Violator, ulong Banner, string ViolatorName, string AdminName, byte BannedTeam, string Reason, uint Duration) =>
            LogBan(Violator, Banner, ViolatorName, AdminName, BannedTeam, Reason, Duration, DateTime.Now);


        public IAsyncResult LogBan(ulong Violator, ulong Banner, string ViolatorName, string AdminName, byte BannedTeam, string Reason, uint Duration, DateTime BanTime)
        {
            throw new NotImplementedException();
        }

        public const string InvalidCallResponse = "INVALID CALL";
        public const string PlayerDataSent = "Playerdata sent!";
        public string URL => UCWarfare.I.Configuration.Instance.PlayerStatsSettings.NJS_ServerURL;
        //public DatabaseManager SQL { get => UCWarfare.I.DB; }
        private readonly WebClientWithTimeout _client;
        public WebInterface()
        {
            _client = new WebClientWithTimeout();
            _client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            PingAndSendAsync();
        }
        public IAsyncResult PingAsync()
        {
            return BasicQueryAsync(ECall.PING_SERVER, new Dictionary<string, string> { { "dt", DateTime.UtcNow.ToString("o") } },
                "No time provided.", new AsyncCallback(WebCallbacks.Ping));
        }
        public IAsyncResult PingAndSendAsync()
        {
            return BasicQueryAsync(ECall.PING_SERVER, new Dictionary<string, string> { { "dt", DateTime.UtcNow.ToString("o") } }, 
                "No time provided.", new AsyncCallback(WebCallbacks.PingAndSend));
        }
        public IAsyncResult BasicQueryAsync(ECall function, Dictionary<string, string> data, string failureResponse, AsyncCallback callback) => BasicQueryAsync(Data.NodeCalls[function], data, failureResponse, callback);
        public void BasicQuerySync(ECall function, Dictionary<string, string> data, string failureResponse) => BasicQuerySync(Data.NodeCalls[function], data, failureResponse);
        public IAsyncResult BasicQueryAsync(string function, Dictionary<string, string> data, string failureResponse, AsyncCallback callback)
        {
            Query q = new Query(URL, function, data, failureResponse);
            Query.AsyncQueryDelegate caller = new Query.AsyncQueryDelegate(q.ExecuteQueryAsync);
            return caller.BeginInvoke(_client, out _, callback, caller);
        }
        public void BasicQueryAsync(string data, string failureResponse, AsyncCallback callback)
        {
            Query q = new Query(URL, data, failureResponse);
            Query.AsyncQueryDelegate caller = new Query.AsyncQueryDelegate(q.ExecuteQueryAsync);
            caller.BeginInvoke(_client, out _, callback, caller);
        }
        public Response BasicQuerySync(string data, string failureResponse)
        {
            while (_client.IsBusy)
                Thread.Sleep(1);
            Response r = new Response();
            try
            {
                string url = URL + '?' + data;
                if(url.Length > 65519)
                {
                    r.Reply = "TOO LONG";
                    r.Success = false;
                    F.LogError("Web Request Too long: \n" + data.Substring(0, 200) + "...");
                    return r;
                }
                F.Log("Starting web request: \"" + url.Substring(0, url.Length > 200 ? 200 : url.Length) + '\"', ConsoleColor.DarkYellow);
                r.Reply = _client.UploadString(url, "");
            }
            catch (WebException ex)
            {
                string msg = ex.Message;
                if (msg.StartsWith("Error: ConnectFailure"))
                    msg = "Could not connect to Node server at \"" + UCWarfare.Config.PlayerStatsSettings.NJS_ServerURL + '\"';
                r.Reply = ex.Message;
                r.Success = false;
                F.LogError("Web Request Error: " + msg);
                return r;
            }
            r.Success = r.Reply != failureResponse && r.Reply != InvalidCallResponse;
            return r;
        }
        public Response BasicQuerySync(string function, Dictionary<string, string> data, string failureResponse)
        {
            string Parameters = "call=" + function;
            //build url
            for (int i = 0; i < data.Count; i++)
            {
                Parameters += "&";
                Parameters += data.Keys.ElementAt(i).EncodeURIComponent();
                Parameters += "=";
                Parameters += data.Values.ElementAt(i).EncodeURIComponent();
            }
            while (_client.IsBusy)
                Thread.Sleep(1);
            Response r = new Response();
            try
            {
                string url = URL + '?' + Parameters;
                if (url.Length > 65519)
                {
                    r.Reply = "TOO LONG";
                    r.Success = false;
                    F.LogError("Web Request Too long: \n" + Parameters.Substring(0, Parameters.Length > 100 ? 100 : Parameters.Length) + "..." );
                    return r;
                }
                F.Log("Starting web request: \"" + url.Substring(0, url.Length > 200 ? 200 : url.Length) + '\"', ConsoleColor.DarkYellow);
                r.Reply = _client.UploadString(url, "");
            }
            catch (WebException ex)
            {
                string msg = ex.Message;
                if (msg.StartsWith("Error: ConnectFailure"))
                    msg = "Could not connect to Node server at \"" + UCWarfare.Config.PlayerStatsSettings.NJS_ServerURL + '\"';
                r.Reply = ex.Message;
                r.Success = false;
                F.LogError("Web Request Error: " + msg);
                return r;
            }
            r.Success = r.Reply != failureResponse && r.Reply != InvalidCallResponse;
            return r;
        }
        public void Dispose()
        {
            _client.Dispose();
            GC.SuppressFinalize(this);
        }
        public Dictionary<string, string> GetPlayerListParams()
        {
            string names = string.Empty;
            string ids = string.Empty;
            string teams = string.Empty;
            for (int i = 0; i < Provider.clients.Count; i++)
            {
                if (i != 0)
                {
                    names += ',';
                    ids += ',';
                    teams += ',';
                }
                names += F.GetPlayerOriginalNames(Provider.clients[i]).CharacterName.EncodeURIComponent();
                ids += Provider.clients[i].playerID.steamID.m_SteamID.ToString();
                teams += Provider.clients[i].player.quests.groupID.m_SteamID.ToString();
            }
            Dictionary<string, string> Parameters = new Dictionary<string, string> {
                { "server", "warfare" },
                { "names", "_" + names },
                { "ids", "_" + ids },
                { "teams", "_" + teams },
            };
            return Parameters;
        }
        public IAsyncResult SendPlayerListAsync()
        {
            return BasicQueryAsync(ECall.SEND_PLAYER_LIST, GetPlayerListParams(), "FAILURE", new AsyncCallback(WebCallbacks.SendPlayerList));
        }
        public void SendPlayerJoinedAsync(SteamPlayer player)
        {
            Dictionary<string, string> Parameters = new Dictionary<string, string> {
                { "server", "warfare" },
                { "name", "_" + F.GetPlayerOriginalNames(player).CharacterName.EncodeURIComponent() },
                { "id", "_" + player.playerID.steamID.m_SteamID.ToString().EncodeURIComponent() },
                { "team", "_" + player.player.quests.groupID.m_SteamID.ToString().EncodeURIComponent() }
            };
            BasicQueryAsync(ECall.SEND_PLAYER_JOINED, Parameters, "FAILURE", new AsyncCallback(WebCallbacks.SendPlayerJoin));
        }
        public void SendPlayerLeftAsync(SteamPlayer player)
        {
            Dictionary<string, string> Parameters = new Dictionary<string, string> {
                { "server", "warfare" },
                { "id", "_" + player.playerID.steamID.m_SteamID.ToString().EncodeURIComponent() }
            };
            BasicQueryAsync(ECall.SEND_PLAYER_LEFT, Parameters, "FAILURE", new AsyncCallback(WebCallbacks.SendPlayerLeft));
        }
        public void SendUpdatedUsername(ulong Steam64, FPlayerName NewName)
        {
            Dictionary<string, string> Parameters = new Dictionary<string, string> {
                { "server", "warfare" },
                { "id", "_" + Steam64.ToString().EncodeURIComponent() },
                { "playername", "_" + NewName.PlayerName.EncodeURIComponent() },
                { "charactername", "_" + NewName.CharacterName.EncodeURIComponent() },
                { "nickname", "_" + NewName.NickName.EncodeURIComponent() },
            };
            BasicQueryAsync(ECall.SEND_UPDATED_USERNAME, Parameters, "FAILURE", new AsyncCallback(WebCallbacks.Default));
        }
        public void SendCoroutinePlayerData(List<PlayerStatsCoroutineData> data)
        {
            Dictionary<string, string> Parameters = new Dictionary<string, string>
            {
                { "server", "warfare" },
                { "data", F.QuickSerialize(data) }
            };
            BasicQueryAsync(ECall.SEND_PLAYER_LOCATION_DATA, Parameters, "INVALID DATA", new AsyncCallback(WebCallbacks.SendPlayerLocationData));
        }
        const int StringSendLengthAtATime = 8192;
        public void SendAssetUpdate()
        {
            TimeSpan timeout = _client.Timeout;
            _client.Timeout = new TimeSpan(0, 0, 15);
            DateTime StartTimestamp = DateTime.Now;
            string vehicleData = GetVehicleAssets();
            string itemData = GetItemAssets();
            int VehicleIntervals = (int)Math.Ceiling(vehicleData.Length / (decimal)StringSendLengthAtATime);
            for (int i = 0; i < VehicleIntervals; i ++)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("call=" + Data.NodeCalls[ECall.SEND_VEHICLE_DATA]);
                sb.Append("&server=warfare");
                sb.Append("&index=" + i.ToString());
                sb.Append("&final=" + (i == VehicleIntervals -1 ? "1" : "0"));
                int position = i * StringSendLengthAtATime;
                string data;
                if (position + StringSendLengthAtATime >= vehicleData.Length)
                    data = vehicleData.Substring(position);
                else
                    data = vehicleData.Substring(position, StringSendLengthAtATime);
                sb.Append("&data=" + data.Replace('\u000D', '\0').Replace('\u000A', '\0').EncodeURIComponent());
                sb.Append("&e=1");
                string send = sb.ToString();
                Response r = BasicQuerySync(send, "INVALID DATA");
                if(!r.Success)
                {
                    BasicQuerySync("call=" + Data.NodeCalls[ECall.REPORT_VEHICLE_ERROR] + "&server=warfare", "INVALID DATA");
                    F.LogError("Failed to send vehicle data to server: " + r.Reply);
                    break;
                }
            }
            int ItemIntervals = (int)Math.Ceiling(itemData.Length / (decimal)StringSendLengthAtATime);
            for (int i = 0; i < ItemIntervals; i++)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("call=" + Data.NodeCalls[ECall.SEND_ITEM_DATA]);
                sb.Append("&server=warfare");
                sb.Append("&index=" + i.ToString());
                sb.Append("&final=" + (i == ItemIntervals - 1 ? "1" : "0"));
                int position = i * StringSendLengthAtATime;
                string data;
                if (position + StringSendLengthAtATime >= itemData.Length)
                    data = itemData.Substring(position);
                else
                    data = itemData.Substring(position, StringSendLengthAtATime);
                data = data.EncodeURIComponent();
                sb.Append("&data=" + data.Replace('\u000D', '\0').Replace('\u000A', '\0'));
                sb.Append("&e=1");
                string send = sb.ToString();
                Response r = BasicQuerySync(send, "INVALID DATA");
                if (!r.Success)
                {
                    BasicQuerySync("call=" + Data.NodeCalls[ECall.REPORT_ITEM_ERROR] + "&server=warfare", "INVALID DATA");
                    F.LogError("Failed to send item data to server: " + r.Reply);
                    break;
                }
            }
            _client.Timeout = timeout;
            F.LogWarning("Completed sending assets in " + (DateTime.Now - StartTimestamp).TotalMilliseconds.ToString() + "ms.", ConsoleColor.DarkYellow);
        }
        private string GetVehicleAssets()
        {
            List<VehicleAsset> a = new List<VehicleAsset>(Assets.find(EAssetType.VEHICLE).Cast<VehicleAsset>());
            a.Sort(delegate (VehicleAsset i1, VehicleAsset i2) {
                return i1.id.CompareTo(i2.id);
            });
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            for (int i = 0; i < a.Count; i++)
            {
                if (i != 0) sb.Append(",");
                string name = a[i].vehicleName;
                if (name == null || name == "#NAME") name = "Unknown";
                else name = name.Replace("#", "").Replace("&", " and ").Replace("\"", "\\\"");
                sb.Append($"{{\"id\":{a[i].id},\"name\":\"{name}\",\"rarity\":\"{a[i].rarity}\",\"turrets\":[");
                for(int t = 0; t < a[i].turrets.Length; t++)
                {
                    if (t != 0) sb.Append(",");
                    sb.Append($"{{\"id\":{a[i].turrets[t].itemID},\"seat\":{a[i].turrets[t].seatIndex}}}");
                }
                sb.Append($"]}}");
            }
            sb.Append("]");
            return sb.ToString();
        }
        private string GetItemAssets()
        {
            List<ItemAsset> a = new List<ItemAsset>(Assets.find(EAssetType.ITEM).Cast<ItemAsset>());
            a.Sort(delegate (ItemAsset i1, ItemAsset i2) {
                return i1.id.CompareTo(i2.id);
            });
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            for(int i = 0; i < a.Count; i++)
            {
                if (i != 0) sb.Append(",");
                string name = a[i].itemName;
                if (name == null || name == "#NAME") name = "Unknown";
                else name = name.Replace("#", "").Replace("&", " and ").Replace("\"", "\\\"");
                sb.Append($"{{\"id\":{a[i].id},\"name\":\"{name}\",\"rarity\":\"{a[i].rarity}\",\"type\":\"{a[i].type}\"}}");
            }
            sb.Append("]");
            return sb.ToString();
        }
    }
    public enum EResponseFromAsyncSocketEvent : byte
    {
        NO_DISCORD_ID_BAN,
        NO_STEAM_ID_BAN,
        NO_REASON_BAN,
        NO_ARGS_BAN,
        NO_REASON_OR_DISCORD_ID_BAN
    }
    public enum ECall : byte
    {
        SEND_PLAYER_LIST,
        SEND_PLAYER_JOINED,
        SEND_PLAYER_LEFT,
        GET_PLAYER_LIST,
        GET_USERNAME,
        PING_SERVER,
        SEND_PLAYER_LOCATION_DATA,
        PLAYER_KILLED,
        INVOKE_BAN,
        SEND_VEHICLE_DATA,
        SEND_ITEM_DATA,
        SEND_SKIN_DATA,
        REPORT_VEHICLE_ERROR,
        REPORT_ITEM_ERROR,
        REPORT_SKIN_ERROR,
        SEND_UPDATED_USERNAME
    }
}
