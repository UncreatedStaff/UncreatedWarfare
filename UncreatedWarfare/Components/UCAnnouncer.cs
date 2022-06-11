using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UnityEngine;

namespace Uncreated.Warfare.Components
{
    public class UCAnnouncer : MonoBehaviour
    {
        public Coroutine? coroutine;
        private bool stop = false;

        private float TimeBetweenMessages;
        private readonly Dictionary<string, Dictionary<string, TranslationData>> Messages = new Dictionary<string, Dictionary<string, TranslationData>>(1);
        private readonly List<string> allKeys = new List<string>();
        private IEnumerator<string> Enumerator;
        void Start()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            ReloadConfig();
            stop = false;
            if (Messages.Count > 0)
            {
                allKeys.Clear();
                foreach (KeyValuePair<string, Dictionary<string, TranslationData>> lang in Messages)
                {
                    foreach (KeyValuePair<string, TranslationData> kvp in lang.Value)
                    {
                        if (!allKeys.Contains(kvp.Key))
                            allKeys.Add(kvp.Key);
                    }
                }
                Enumerator = allKeys.GetEnumerator();
                this.coroutine = StartCoroutine(MessageLoop());
            }
            else
            {
                L.LogWarning("Announcer has no messages set up.");
            }
        }
        public void ReloadConfig()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            F.CheckDir(Data.DATA_DIRECTORY, out bool folderExists);
            Messages.Clear();
            if (folderExists)
            {
                string directory = Data.DATA_DIRECTORY + "autobroadcast.json";
                if (!File.Exists(directory))
                {
                    Dictionary<string, TranslationData> enUs = new Dictionary<string, TranslationData>(DefaultMessages.Count);
                    using (FileStream stream = new FileStream(directory, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                    {
                        Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
                        writer.WriteStartObject();
                        writer.WriteProperty(nameof(TimeBetweenMessages), 60f);
                        writer.WritePropertyName("Messages");
                        writer.WriteStartObject();
                        writer.WritePropertyName(JSONMethods.DEFAULT_LANGUAGE);
                        writer.WriteStartObject();
                        foreach (KeyValuePair<string, string> message in DefaultMessages)
                        {
                            writer.WritePropertyName(message.Key);
                            writer.WriteStringValue(message.Value);
                            enUs.Add(message.Key, new TranslationData(message.Value));
                        }
                        writer.WriteEndObject();
                        writer.WriteEndObject();
                        writer.WriteEndObject();
                        writer.Dispose();
                        Messages.Add(JSONMethods.DEFAULT_LANGUAGE, enUs);
                    }
                    return;
                }
                using (FileStream stream = new FileStream(directory, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    long len = stream.Length;
                    if (len > int.MaxValue)
                    {
                        L.LogWarning("File autobroadcast.json is too long to read.");
                        Dictionary<string, TranslationData> enUs = new Dictionary<string, TranslationData>(DefaultMessages.Count);
                        foreach (KeyValuePair<string, string> message in DefaultMessages)
                            enUs.Add(message.Key, new TranslationData(message.Value));
                        Messages.Add(JSONMethods.DEFAULT_LANGUAGE, enUs);
                    }
                    else
                    {
                        byte[] bytes = new byte[len];
                        stream.Read(bytes, 0, (int)len);
                        Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.PropertyName)
                            {
                                string prop = reader.GetString()!;
                                if (reader.Read())
                                {
                                    switch (prop)
                                    {
                                        case nameof(TimeBetweenMessages):
                                            TimeBetweenMessages = (float)reader.GetDouble();
                                            break;
                                        case "Messages":
                                            if (reader.TokenType == JsonTokenType.StartObject)
                                            {
                                                Dictionary<string, TranslationData>? current = new Dictionary<string, TranslationData>();
                                                string? lang = null;
                                                bool i = false;
                                                while (reader.Read())
                                                {
                                                    if (reader.TokenType == JsonTokenType.EndObject)
                                                    {
                                                        if (!i) break;
                                                        i = false;
                                                        if (current != null && lang != null)
                                                        {
                                                            Messages.Add(lang, current);
                                                            current = null;
                                                        }
                                                    }
                                                    if (reader.TokenType == JsonTokenType.PropertyName)
                                                    {
                                                        if (!i)
                                                        {
                                                            lang = reader.GetString()!;
                                                            i = true;
                                                        }
                                                        else
                                                        {
                                                            string key = reader.GetString()!;
                                                            if (reader.Read() && reader.TokenType == JsonTokenType.String)
                                                            {
                                                                string value = reader.GetString()!;
                                                                TranslationData data = new TranslationData(value);
                                                                if (current == null)
                                                                    current = new Dictionary<string, TranslationData>();
                                                                current.Add(key, data);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                L.LogWarning("Failed to read UCAnnouncer config.");
                Dictionary<string, TranslationData> enUs = new Dictionary<string, TranslationData>(DefaultMessages.Count);
                foreach (KeyValuePair<string, string> entry in DefaultMessages)
                    enUs.Add(entry.Key, new TranslationData(entry.Value));
                Messages.Add(JSONMethods.DEFAULT_LANGUAGE, enUs);
            }
        }
        private IEnumerator MessageLoop()
        {
            while (!stop)
            {
                yield return new WaitForSeconds(TimeBetweenMessages);
#if DEBUG
                using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
                if (!Enumerator.MoveNext())
                {
                    Enumerator.Reset();
                    Enumerator.MoveNext();
                }
                if (Provider.clients.Count > 0)
                    Announce(Enumerator.Current);
            }
        }
        private TranslationData GetMessage(string key, string language)
        {
            if (
                (Messages.TryGetValue(language, out Dictionary<string, TranslationData> data) && data.TryGetValue(key, out TranslationData value)) ||
                (Messages.TryGetValue(JSONMethods.DEFAULT_LANGUAGE, out data) && data.TryGetValue(key, out value)) ||
                (Messages.Count > 0 && Messages.ElementAt(0).Value.TryGetValue(key, out value)))
            {
                return value;
            }
            else return TranslationData.GetPlaceholder(key);
        }
        public void Announce(string key)
        {
            if (key == null)
            {
                L.LogWarning($"Message to be broadcasted by announcer was null.");
                return;
            }
            if (key.Length == 0)
            {
                L.LogWarning($"Message to be broadcasted by announcer was empty.");
                return;
            }
            foreach (LanguageSet set in Translation.EnumerateLanguageSets())
            {
                TranslationData tdata = GetMessage(key, set.Language);
                while (set.MoveNext())
                {
                    Chat.SendSingleMessage(tdata.Message, tdata.Color, EChatMode.SAY, null, tdata.Message.Contains("</"), set.Next.Player.channel.owner);
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
            Enumerator?.Reset();
            OnDisable();
            Start();
        }

        private static readonly Dictionary<string, string> DefaultMessages = new Dictionary<string, string>(9)
        {
            { "announce_join_discord", "<color=#b3b3b3>Have you joined our <color=#7483c4>discord</color> yet? Do <color=#ffffff>/discord</color>.</color>" },
            { "announce_deploy_main", "<color=#c2b7a5>You can deploy back to main by doing <color=#ffffff>/deploy main</color>.</color>" },
            { "announce_rank_up", "<color=#92a692>Capture <color=#ffffff>flags</color> to rank up, and earn respect amongst your team.</color>" },
            { "announce_dont_waste_assets", "<color=#c79675>Do not waste vehicles, ammo, build & other assets!</color>" },
            { "announce_coordinate", "<color=#a2a7ba>Winning requires coordination and teamwork. Listen to your superior officers, and communicate!</color>" },
            { "announce_fobs", "<color=#9da6a6>Building <color=#54e3ff>FOBs</color> is vital for advancing operations. Grab a logistics truck and go build one!</color>" },
            { "announce_squads", "<color=#c2b7a5>Join a squad with <color=#ffffff>/squad join</color> or create one with <color=#ffffff>/squad create</color> to gain officer points.</color>" },
            { "announce_ping", "<color=#c2b7a5>Ping locations for your squad by using the <color=#cedcde>POINT</color> gesture in the gestures menu.</color>" },
            { "announce_squad_chat", "<color=#a2a7ba>Use area chat while in a squad to communicate with your <color=#54e3ff>squad</color>, and group chat to communicate with your <color=#54e3ff>team</color>.</color>" }
        };
    }
}
