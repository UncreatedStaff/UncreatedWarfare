using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static UncreatedWarfare.JSONMethods;

namespace UncreatedWarfare.Stats
{
    public class WebClientWithTimeout : WebClient
    {
        public readonly TimeSpan Timeout = new TimeSpan(0, 0, 5);
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
        public const string InvalidCallResponse = "INVALID CALL";
        public const string PlayerDataSent = "Playerdata sent!";
        public string URL => UCWarfare.I.Configuration.Instance.PlayerStatsSettings.NJS_ServerURL;
        private WebClientWithTimeout _client;
        public WebInterface()
        {
            _client = new WebClientWithTimeout();
            _client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            if(Ping(out string response))
            {
                CommandWindow.LogWarning("Connected to NodeJS server successfully. Ping: " + response + "ms.");
                if(int.TryParse(response, out int ping) && ping > 300)
                    CommandWindow.LogError(response + "ms seems a bit high, is the connection to the Node server okay?");
            }
            else
                CommandWindow.LogError("Failed to ping NodeJS Server!");
        }
        public bool Ping(out string response) => BasicQuery(ECall.PING_SERVER, new Dictionary<string, string> { { "dt", DateTime.UtcNow.ToString("o") } }, "No time provided.", out response);
        public bool BasicQuery(ECall function, Dictionary<string, string> data, string failureResponse, out string response) => 
            BasicQuery(function.ToString(), data, failureResponse, out response);
        public bool BasicQuery(string function, Dictionary<string, string> data, string failureResponse, out string response)
        {
            while(_client.IsBusy)
                System.Threading.Thread.Sleep(1);
            string Parameters = "call=" + function;
            //build url
            for(int i = 0; i < data.Count; i++)
            {
                Parameters += "&";
                Parameters += data.Keys.ElementAt(i).EncodeURIComponent();
                Parameters += "=";
                Parameters += data.Values.ElementAt(i).EncodeURIComponent();
            }
            //CommandWindow.LogWarning($"QUERYING \"{URL}?{Parameters}\"");
            try
            {
                response = _client.UploadString(URL + '?' + Parameters, Parameters);
            } catch (WebException ex)
            {
                response = ex.Message;
                CommandWindow.LogError("Web Request Error: " + ex.Message);
                return false;
            }
            //CommandWindow.LogWarning($"QUERY MADE TO \"{URL}?{Parameters}\"");
            if (response == failureResponse || response == InvalidCallResponse) return false;
            return true;
        }

        public void Dispose()
        {
            _client.Dispose();
            GC.SuppressFinalize(this);
        }
        public bool SendPlayerList()
        {
            string names = string.Empty;
            string ids = string.Empty;
            string teams = string.Empty;
            for(int i = 0; i < Provider.clients.Count; i++)
            {
                if (i != 0)
                {
                    names += ',';
                    ids += ',';
                    teams += ',';
                }
                names += Provider.clients[i].playerID.playerName.ShortenName().EncodeURIComponent();
                ids += Provider.clients[i].playerID.steamID.m_SteamID.ToString();
                teams += Provider.clients[i].player.quests.groupID.m_SteamID.ToString();
            }
            Dictionary<string, string> Parameters = new Dictionary<string, string> {
                { "server", "warfare" },
                { "names", "_" + names },
                { "ids", "_" + ids },
                { "teams", "_" + teams },
            };
            bool success = BasicQuery(ECall.SEND_PLAYER_LIST, Parameters, "FAILURE", out string response);
            if (response == PlayerDataSent && success) return true;
            else return false;
        }
        public bool SendPlayerJoined(SteamPlayer player)
        {
            Dictionary<string, string> Parameters = new Dictionary<string, string> {
                { "server", "warfare" },
                { "name", "_" + player.playerID.playerName.ShortenName().EncodeURIComponent() },
                { "id", "_" + player.playerID.steamID.m_SteamID.ToString() },
                { "team", "_" + player.player.quests.groupID.m_SteamID.ToString() }
            };
            bool success = BasicQuery(ECall.SEND_PLAYER_JOINED, Parameters, "FAILURE", out string response);
            if (response == PlayerDataSent && success) return true;
            else return false;
        }
        public bool SendPlayerLeft(SteamPlayer player)
        {
            Dictionary<string, string> Parameters = new Dictionary<string, string> {
                { "server", "warfare" },
                { "id", "_" + player.playerID.steamID.m_SteamID.ToString() }
            };
            bool success = BasicQuery(ECall.SEND_PLAYER_LEFT, Parameters, "FAILURE", out string response);
            if (response == PlayerDataSent && success) return true;
            else return false;
        }
    }
}
