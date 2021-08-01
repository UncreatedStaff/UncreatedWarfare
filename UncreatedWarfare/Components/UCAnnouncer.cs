using SDG.Framework.Translations;
using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using UnityEngine;

namespace Uncreated.Warfare.Components
{
    public class UCAnnouncer : MonoBehaviour
    {
        public Coroutine coroutine;
        public Config<MessagerConfig> config = new Config<MessagerConfig>(Data.DataDirectory, "autobroadcast.json");
        private bool stop = false;
        private Dictionary<string, Dictionary<string, TranslationData>> Messages;
        private List<string> allKeys = new List<string>();
        private IEnumerator<string> Enumerator;
        void Start()
        {
            stop = false;
            if (config.Data.Messages != null && config.Data.Messages.Count > 0)
            {
                Messages = new Dictionary<string, Dictionary<string, TranslationData>>();
                allKeys.Clear();
                foreach (KeyValuePair<string, Dictionary<string, string>> automessage in config.Data.Messages)
                {
                    Dictionary<string, TranslationData> translations = new Dictionary<string, TranslationData>();
                    foreach (KeyValuePair<string, string> translation in automessage.Value)
                        translations.Add(translation.Key, new TranslationData(translation.Value));
                    Messages.Add(automessage.Key, translations);
                }
                if (Messages.TryGetValue(JSONMethods.DefaultLanguage, out Dictionary<string, TranslationData> d))
                {
                    foreach (KeyValuePair<string, TranslationData> translation in d)
                        allKeys.Add(translation.Key);
                } else if (Messages.Count > 0)
                {
                    d = Messages.ElementAt(0).Value;
                    foreach (KeyValuePair<string, TranslationData> translation in d)
                        allKeys.Add(translation.Key);
                }
                Enumerator = allKeys.GetEnumerator();
                this.coroutine = StartCoroutine(MessageLoop());
            } else
            {
                F.LogWarning("Announcer has no messages set up.");
            }
        }

        private IEnumerator MessageLoop()
        {
            while (!stop)
            {
                yield return new WaitForSeconds(config.Data.TimeBetweenMessages);
                if (!Enumerator.MoveNext())
                {
                    Enumerator.Reset();
                    Enumerator.MoveNext();
                }
                if (Provider.clients.Count > 0)
                    Announce(Enumerator.Current);
            }
        }
        public void Announce(string key)
        {
            foreach (SteamPlayer steamplayer in Provider.clients)
            {
                ulong player = steamplayer.playerID.steamID.m_SteamID;
                if (key == null)
                {
                    F.LogWarning($"Message to be broadcasted by announcer was null ({key})");
                    return;
                }
                if (key.Length == 0)
                {
                    F.LogWarning($"Message to be broadcasted by announcer was empty ({key})");
                    return;
                }
                if (player == 0)
                {
                    if (!Messages.TryGetValue(JSONMethods.DefaultLanguage, out Dictionary<string, TranslationData> data))
                    {
                        if (Messages.Count > 0)
                        {
                            if (Messages.ElementAt(0).Value.TryGetValue(key, out TranslationData translation))
                            {
                                F.SendSingleMessage(translation.Message, translation.Color, EChatMode.SAY, null, translation.Message.Contains("</"), steamplayer);
                            }
                            else
                            {
                                F.SendSingleMessage(key, UCWarfare.GetColor("default"), EChatMode.SAY, null, false, steamplayer);
                            }
                        }
                        else
                        {
                            F.SendSingleMessage(key, UCWarfare.GetColor("default"), EChatMode.SAY, null, false, steamplayer);
                        }
                    }
                    else
                    {
                        if (data.TryGetValue(key, out TranslationData translation))
                        {
                            F.SendSingleMessage(translation.Message, translation.Color, EChatMode.SAY, null, translation.Message.Contains("</"), steamplayer);
                        }
                        else
                        {
                            F.SendSingleMessage(key, UCWarfare.GetColor("default"), EChatMode.SAY, null, false, steamplayer);
                        }
                    }
                }
                else
                {
                    if (Data.Languages.TryGetValue(player, out string lang))
                    {
                        if (!Messages.TryGetValue(lang, out Dictionary<string, TranslationData> data2) || !data2.ContainsKey(key))
                            lang = JSONMethods.DefaultLanguage;
                    }
                    else lang = JSONMethods.DefaultLanguage;
                    if (!Messages.TryGetValue(lang, out Dictionary<string, TranslationData> data))
                    {
                        if (Messages.Count > 0)
                        {
                            if (Messages.ElementAt(0).Value.TryGetValue(key, out TranslationData translation))
                            {
                                F.SendSingleMessage(translation.Message, translation.Color, EChatMode.SAY, null, translation.Message.Contains("</"), steamplayer);
                            }
                            else
                            {
                                F.SendSingleMessage(key, UCWarfare.GetColor("default"), EChatMode.SAY, null, false, steamplayer);
                            }
                        }
                        else
                        {
                            F.SendSingleMessage(key, UCWarfare.GetColor("default"), EChatMode.SAY, null, false, steamplayer);
                        }
                    }
                    else if (data.TryGetValue(key, out TranslationData translation))
                    {
                        F.SendSingleMessage(translation.Message, translation.Color, EChatMode.SAY, null, translation.Message.Contains("</"), steamplayer);
                    }
                    else
                    {
                        F.SendSingleMessage(key, UCWarfare.GetColor("default"), EChatMode.SAY, null, false, steamplayer);
                    }
                }
            }
        }
        void OnDisable()
        {
            stop = true;
            if (coroutine != null)
                StopCoroutine(coroutine);
            coroutine = null;
        }
        public void Reload()
        {
            config.Reload();
            Enumerator?.Reset();
            OnDisable();
            Start();
        }
        public class MessagerConfig : ConfigData
        {
            public float TimeBetweenMessages;
            public Dictionary<string, Dictionary<string, string>> Messages;
            public override void SetDefaults()
            {
                TimeBetweenMessages = 120; // 2 minutes
                try
                {
                    Messages = new Dictionary<string, Dictionary<string, string>>
                    {
                        { "en-us",
                            new Dictionary<string, string> {
                                { "announce_join_discord", "<color=#b3b3b3>Have you joined our <color=#7483c4>discord</color> yet? Do <color=#ffffff>/discord</color>.</color>" },
                                { "announce_deploy_main", "<color=#c2b7a5>You can deploy back to main by doing <color=#ffffff>/deploy main</color>.</color>" },
                                { "announce_rank_up", "<color=#92a692>Capture <color=#ffffff>flags</color> to rank up, and earn respect amongst your team.</color>" },
                                { "announce_dont_waste_assets", "<color=#c79675>Do not waste vehicles, ammo, build & other assets!</color>" },
                                { "announce_coordinate", "<color=#a2a7ba>Winning requires coordination and teamwork. Listen to your superior officers, and communicate!</color>" },
                                { "announce_fobs", "<color=#9da6a6>Building <color=#54e3ff>FOBs</color> is vital for advancing operations. Grab a logistics truck and go build one!</color>" },
                                { "announce_squads", "<color=#c2b7a5>Join a squad with <color=#ffffff>/squad join</color> or create one with <color=#ffffff>/squad create</color> to gain officer points.</color>" },
                                { "announce_ping", "<color=#c2b7a5>Ping locations for your squad by using the <color=#cedcde>POINT</color> gesture in the gestures menu.</color>" },
                                { "announce_squad_chat", "<color=#a2a7ba>Use area chat while in a squad to communicate with your <color=#54e3ff>squad</color>, and group chat to communicate with your <color=#54e3ff>team</color>.</color>" }
                            }
                        }
                    };
                }
                catch (Exception ex)
                {
                    string[] stuff = ex.Message.Split(':');
                    string badKey = "unknown";
                    if (stuff.Length >= 2) badKey = stuff[1].Trim();
                    F.LogError("\"" + badKey + "\" has a duplicate key in message announcer default translations, unable to load them. Unloading...");
                    F.LogError(ex);
                    if (ex.InnerException != default)
                        F.LogError(ex.InnerException);
                }
            }
        }
    }
}
