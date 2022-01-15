﻿using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;
using FlagData = Uncreated.Warfare.Gamemodes.Flags.FlagData;

namespace Uncreated.Warfare
{
    public struct ColorData
    {
        public string key;
        public string color_hex;
        [JsonIgnore]
        public Color Color { get => color_hex.Hex(); }
        [JsonConstructor]
        public ColorData(string key, string color_hex)
        {
            this.key = key;
            this.color_hex = color_hex;
        }
    }
    public struct TranslationData
    {
        public static TranslationData Nil => new TranslationData() { Color = Color.white, Message = "default", Original = "<color=#ffffff>default</color>", UseColor = true };
        public string Message;
        public string Original;
        public Color Color;
        public bool UseColor;
        public TranslationData(string Original)
        {
            this.Original = Original;
            this.Color = GetColorFromMessage(Original, out Message, out UseColor);
        }
        public static Color GetColorFromMessage(string Original, out string InnerText, out bool found)
        {
            if (Original.Length < 23)
            {
                InnerText = Original;
                found = false;
                return UCWarfare.GetColor("default");
            }
            if (Original.StartsWith("<color=#") && Original[8] != '{' && Original.EndsWith("</color>"))
            {
                IEnumerator<char> characters = Original.Skip(8).GetEnumerator();
                int start = 8;
                int length = 0;
                while (characters.MoveNext())
                {
                    if (characters.Current == '>') break; // keep moving until the ending > is found.
                    length++;
                }
                characters.Dispose();
                int msgStart = start + length + 1;
                InnerText = Original.Substring(msgStart, Original.Length - msgStart - 8);
                found = true;
                return Original.Substring(start, length).Hex();
            }
            else
            {
                InnerText = Original;
                found = false;
                return UCWarfare.GetColor("default");
            }
        }
        public override string ToString() =>
            $"Original: {Original}, Inner text: {Message}, {(UseColor ? $"Color: {Color} ({ColorUtility.ToHtmlStringRGBA(Color)}." : "Unable to find color.")}";
    }
    public struct Point3D
    {
        public string name;
        public float x;
        public float y;
        public float z;
        [JsonIgnore]
        public Vector3 Vector3 { get => new Vector3(x, y, z); }
        [JsonConstructor]
        public Point3D(string name, float x, float y, float z)
        {
            this.name = name;
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }
    public interface IJsonReadWrite
    {
        void WriteJson(Utf8JsonWriter writer);
        void ReadJson(ref Utf8JsonReader reader);
    }

    public struct SerializableVector3 : IJsonReadWrite
    {
        public static readonly SerializableVector3 Zero = new SerializableVector3(0, 0, 0);
        public float x;
        public float y;
        public float z;
        [JsonIgnore]
        public Vector3 Vector3
        {
            get => new Vector3(x, y, z);
            set
            {
                if (value == default)
                {
                    x = 0; y = 0; z = 0;
                }
                else
                {
                    x = value.x; y = value.y; z = value.z;
                }
            }
        }
        public static bool operator ==(SerializableVector3 a, SerializableVector3 b) => a.x == b.x && a.y == b.y && a.z == b.z;
        public static bool operator ==(SerializableVector3 a, Vector3 b) => a.x == b.x && a.y == b.y && a.z == b.z;
        public static bool operator !=(SerializableVector3 a, SerializableVector3 b) => a.x != b.x || a.y != b.y || a.z != b.z;
        public static bool operator !=(SerializableVector3 a, Vector3 b) => a.x != b.x || a.y != b.y || a.z != b.z;
        public override bool Equals(object obj)
        {
            if (obj == default) return false;
            if (obj is SerializableVector3 v3)
                return x == v3.x && y == v3.y && z == v3.z;
            else if (obj is Vector3 uv3)
                return x == uv3.x && y == uv3.y && z == uv3.z;
            else return false;
        }
        public override int GetHashCode()
        {
            int hashCode = 373119288;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            hashCode = hashCode * -1521134295 + z.GetHashCode();
            return hashCode;
        }
        public override string ToString() => $"({Mathf.RoundToInt(x).ToString(Data.Locale)}, {Mathf.RoundToInt(y).ToString(Data.Locale)}, {Mathf.RoundToInt(z).ToString(Data.Locale)})";
        public SerializableVector3(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }
        [JsonConstructor]
        public SerializableVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public void WriteJson(Utf8JsonWriter writer)
        {
            writer.WriteProperty(nameof(x), this.x);
            writer.WriteProperty(nameof(y), this.x);
            writer.WriteProperty(nameof(z), this.x);
        }
        public void ReadJson(ref Utf8JsonReader reader)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string val = reader.GetString();
                    if (val != null && reader.Read())
                    {
                        switch (val)
                        {
                            case nameof(x):
                                x = (float)reader.GetDecimal();
                                break;
                            case nameof(y):
                                y = (float)reader.GetDecimal();
                                break;
                            case nameof(z):
                                z = (float)reader.GetDecimal();
                                break;
                        }
                    }
                }
                else if (reader.TokenType == JsonTokenType.EndObject) return;
            }
        }
    }
    public struct SerializableTransform : IJsonReadWrite
    {
        public static readonly SerializableTransform Zero = new SerializableTransform(SerializableVector3.Zero, SerializableVector3.Zero);
        public SerializableVector3 position;
        public SerializableVector3 euler_angles;
        [JsonIgnore]
        public Quaternion Rotation { get => Quaternion.Euler(euler_angles.Vector3); }
        [JsonIgnore]
        public Vector3 Position { get => position.Vector3; }
        public static bool operator ==(SerializableTransform a, SerializableTransform b) => a.position == b.position && a.euler_angles == b.euler_angles;
        public static bool operator !=(SerializableTransform a, SerializableTransform b) => a.position != b.position || a.euler_angles != b.euler_angles;
        public static bool operator ==(SerializableTransform a, Transform b) => a.position == b.position && a.euler_angles == b.rotation.eulerAngles;
        public static bool operator !=(SerializableTransform a, Transform b) => a.position != b.position || a.euler_angles != b.rotation.eulerAngles;
        public override bool Equals(object obj)
        {
            if (obj == default) return false;
            if (obj is SerializableTransform t)
                return position == t.position && euler_angles == t.euler_angles;
            else if (obj is Transform ut)
                return position == ut.position && euler_angles == ut.eulerAngles;
            else return false;
        }
        public override string ToString() => position.ToString();
        public override int GetHashCode()
        {
            int hashCode = -1079335343;
            hashCode = hashCode * -1521134295 + position.GetHashCode();
            hashCode = hashCode * -1521134295 + euler_angles.GetHashCode();
            return hashCode;
        }
        [JsonConstructor]
        public SerializableTransform(SerializableVector3 position, SerializableVector3 euler_angles)
        {
            this.position = position;
            this.euler_angles = euler_angles;
        }
        public SerializableTransform(Transform transform)
        {
            this.position = new SerializableVector3(transform.position);
            this.euler_angles = new SerializableVector3(transform.rotation.eulerAngles);
        }
        public SerializableTransform(Vector3 position, Vector3 eulerAngles)
        {
            this.position = new SerializableVector3(position);
            this.euler_angles = new SerializableVector3(eulerAngles);
        }
        public SerializableTransform(float posx, float posy, float posz, float rotx, float roty, float rotz)
        {
            this.position = new SerializableVector3(posx, posy, posz);
            this.euler_angles = new SerializableVector3(rotx, roty, rotz);
        }
        public SerializableTransform(Vector3 position, Quaternion rotation)
        {
            this.position = new SerializableVector3(position);
            this.euler_angles = new SerializableVector3(rotation.eulerAngles);
        }
        public void WriteJson(Utf8JsonWriter writer)
        {
            writer.WriteProperty(nameof(position), position);
            writer.WriteProperty(nameof(euler_angles), euler_angles);
        }
        public void ReadJson(ref Utf8JsonReader reader)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string val = reader.GetString();
                    if (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
                    {
                        switch (val)
                        {
                            case nameof(position):
                                position = new SerializableVector3();
                                position.ReadJson(ref reader);
                                break;
                            case nameof(euler_angles):
                                euler_angles = new SerializableVector3();
                                euler_angles.ReadJson(ref reader);
                                break;
                        }
                    }
                }
                else if (reader.TokenType == JsonTokenType.EndObject)
                    return;
            }
        }
    }
    public struct LangData
    {
        public ulong player;
        public string language;
        [JsonConstructor]
        public LangData(ulong player, string language)
        {
            this.player = player;
            this.language = language;
        }
    }
    public struct LanguageAliasSet : IJsonReadWrite
    {
        public string key;
        public string display_name;
        public string[] values;
        [JsonConstructor]
        public LanguageAliasSet(string key, string display_name, string[] values)
        {
            this.key = key;
            this.display_name = display_name;
            this.values = values;
        }
        public void ReadJson(ref Utf8JsonReader reader)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) return;
                else if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string prop = reader.GetString();
                    if (!reader.Read()) continue;
                    if (prop == nameof(key))
                        this.key = reader.GetString();
                    else if (prop == nameof(display_name))
                        this.display_name = reader.GetString();
                    else if (prop == nameof(values) && reader.TokenType == JsonTokenType.StartArray)
                    {
                        List<string> tlist = new List<string>(24);
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            if (reader.TokenType == JsonTokenType.String)
                            {
                                tlist.Add(reader.GetString());
                            }
                        }
                        this.values = tlist.ToArray();
                    }
                }
            }
        }
        public void WriteJson(Utf8JsonWriter writer)
        {
            writer.WriteProperty(nameof(key), key);
            writer.WriteProperty(nameof(display_name), display_name);
            writer.WriteStartArray();
            for (int i = 0; i < values.Length; i++)
            {
                writer.WriteStringValue(values[i]);
            }
            writer.WriteEndArray();
        }
    }
    
    public static partial class JSONMethods
    {
        public static readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions() { WriteIndented = true };
        public const string DefaultLanguage = "en-us";
        public static Dictionary<string, Color> LoadColors(out Dictionary<string, string> HexValues)
        {
            if (!File.Exists(Data.DATA_DIRECTORY + "chat_colors.json"))
            {
                Dictionary<string, Color> NewDefaults = new Dictionary<string, Color>(DefaultColors.Count);
                using (FileStream stream = new FileStream(Data.DATA_DIRECTORY + "chat_colors.json", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                {
                    Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
                    writer.WriteStartObject();
                    foreach (KeyValuePair<string, string> color in DefaultColors)
                    {
                        writer.WritePropertyName(color.Key);
                        writer.WriteStringValue(color.Value);
                        NewDefaults.Add(color.Key, color.Value.Hex());
                    }
                    writer.WriteEndObject();
                    writer.Dispose();
                    stream.Close();
                    stream.Dispose();
                }
                HexValues = DefaultColors;
                return NewDefaults;
            }
            using (FileStream stream = new FileStream(Data.DATA_DIRECTORY + "chat_colors.json", FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                long len = stream.Length;
                if (len > int.MaxValue)
                {
                    L.LogError("chat_colors.json is too long to read.");
                    Dictionary<string, Color> NewDefaults = new Dictionary<string, Color>(DefaultColors.Count);
                    foreach (KeyValuePair<string, string> color in DefaultColors)
                    {
                        NewDefaults.Add(color.Key, color.Value.Hex());
                    }
                    HexValues = DefaultColors;
                    return NewDefaults;
                }
                else
                {
                    Dictionary<string, Color> converted = new Dictionary<string, Color>(DefaultColors.Count);
                    Dictionary<string, string> read = new Dictionary<string, string>(DefaultColors.Count);
                    byte[] bytes = new byte[len];
                    stream.Read(bytes, 0, (int)len);
                    Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.StartObject) continue;
                        else if (reader.TokenType == JsonTokenType.EndObject) break;
                        else if (reader.TokenType == JsonTokenType.PropertyName)
                        {
                            string key = reader.GetString();
                            if (reader.Read() && reader.TokenType == JsonTokenType.String)
                            {
                                string color = reader.GetString();
                                string value = reader.GetString();
                                if (read.ContainsKey(key))
                                    L.LogWarning("Duplicate color key \"" + key + "\" in chat_colors.json");
                                else
                                {
                                    read.Add(key, color);
                                    converted.Add(key, color.Hex());
                                }
                            }
                        }
                    }
                    HexValues = read;
                    return converted;
                }
            }
        }
        public static Dictionary<string, Dictionary<string, TranslationData>> LoadTranslations(
            out Dictionary<string, Dictionary<string, string>> deathloc, out Dictionary<string, Dictionary<ELimb, string>> limbloc)
        {
            string[] langDirs = Directory.GetDirectories(Data.LangStorage, "*", SearchOption.TopDirectoryOnly);
            Dictionary<string, Dictionary<string, TranslationData>> languages = new Dictionary<string, Dictionary<string, TranslationData>>();
            deathloc = new Dictionary<string, Dictionary<string, string>>();
            limbloc = new Dictionary<string, Dictionary<ELimb, string>>();
            F.CheckDir(Data.LangStorage + DefaultLanguage, out bool folderIsThere);
            if (folderIsThere)
            {
                if (!File.Exists(Data.LangStorage + DefaultLanguage + @"\localization.json"))
                {
                    Dictionary<string, TranslationData> defaultLocal = new Dictionary<string, TranslationData>(DefaultTranslations.Count);
                    using (FileStream stream = new FileStream(Data.LangStorage + DefaultLanguage + @"\localization.json", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                    {
                        Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
                        writer.WriteStartObject();
                        foreach (KeyValuePair<string, string> translation in DefaultTranslations)
                        {
                            writer.WritePropertyName(translation.Key);
                            writer.WriteStringValue(translation.Value);
                            defaultLocal.Add(translation.Key, new TranslationData(translation.Value));
                        }
                        writer.WriteEndObject();
                        writer.Dispose();
                        stream.Close();
                        stream.Dispose();
                    }

                    languages.Add(DefaultLanguage, defaultLocal);
                }
                if (!File.Exists(Data.LangStorage + DefaultLanguage + @"\deathlocalization.dat"))
                {
                    Dictionary<string, string> defaultDeathLocal = new Dictionary<string, string>(DefaultDeathTranslations.Count);
                    using (StreamWriter TextWriter = File.CreateText(Data.LangStorage + DefaultLanguage + @"\deathlocalization.dat"))
                    {
                        TextWriter.WriteLine(DeathsTranslationDescription);
                        foreach (KeyValuePair<string, string> dmsg in DefaultDeathTranslations)
                        {
                            defaultDeathLocal.Add(dmsg.Key, dmsg.Value);
                            TextWriter.WriteLine(dmsg.Key + ' ' + dmsg.Value);
                        }
                        TextWriter.Close();
                        TextWriter.Dispose();
                    }

                    deathloc.Add(DefaultLanguage, defaultDeathLocal);
                }
                if (!File.Exists(Data.LangStorage + DefaultLanguage + @"\limblocalization.dat"))
                {
                    Dictionary<ELimb, string> defaultLimbLocal = new Dictionary<ELimb, string>(DefaultLimbTranslations.Count);
                    using (StreamWriter TextWriter = File.CreateText(Data.LangStorage + DefaultLanguage + @"\limblocalization.dat"))
                    {
                        TextWriter.WriteLine(DeathsLimbTranslationsDescription);
                        foreach (KeyValuePair<ELimb, string> dmsg in DefaultLimbTranslations)
                        {
                            TextWriter.WriteLine(dmsg.Key.ToString() + ' ' + dmsg.Value);
                            defaultLimbLocal.Add(dmsg.Key, dmsg.Value);
                        }
                        TextWriter.Close();
                        TextWriter.Dispose();
                    }

                    limbloc.Add(DefaultLanguage, defaultLimbLocal);
                }
                foreach (string folder in langDirs)
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(folder);
                    string lang = directoryInfo.Name;
                    FileInfo[] langFiles = directoryInfo.GetFiles("*", SearchOption.TopDirectoryOnly);
                    foreach (FileInfo info in langFiles)
                    {
                        if (info.Name == "localization.json")
                        {
                            using (FileStream stream = new FileStream(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                long len = stream.Length;
                                if (len > int.MaxValue)
                                {
                                    L.LogError(info.FullName + " is too long to read.");
                                    if (lang == DefaultLanguage && !languages.ContainsKey(DefaultLanguage))
                                    {
                                        Dictionary<string, TranslationData> defaultLocal = new Dictionary<string, TranslationData>(DefaultTranslations.Count);
                                        foreach (KeyValuePair<string, string> translation in DefaultTranslations)
                                        {
                                            defaultLocal.Add(translation.Key, new TranslationData(translation.Value));
                                        }
                                        languages.Add(DefaultLanguage, defaultLocal);
                                    }
                                }
                                else
                                {
                                    Dictionary<string, TranslationData> local = new Dictionary<string, TranslationData>(DefaultTranslations.Count);
                                    byte[] bytes = new byte[len];
                                    stream.Read(bytes, 0, (int)len);
                                    Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
                                    while (reader.Read())
                                    {
                                        if (reader.TokenType == JsonTokenType.StartObject) continue;
                                        else if (reader.TokenType == JsonTokenType.EndObject) break;
                                        else if (reader.TokenType == JsonTokenType.PropertyName)
                                        {
                                            string key = reader.GetString();
                                            if (reader.Read() && reader.TokenType == JsonTokenType.String)
                                            {
                                                string value = reader.GetString();
                                                if (local.ContainsKey(key))
                                                    L.LogWarning("Duplicate key \"" + key + "\" in localization file for " + lang);
                                                else
                                                    local.Add(key, new TranslationData(value));
                                            }
                                        }
                                    }
                                    if (!languages.ContainsKey(lang))
                                        languages.Add(lang, local);
                                }
                            }
                        }
                        else if (info.Name == "deathlocalization.dat")
                        {
                            StringReader reader = new StringReader(File.ReadAllText(info.FullName));
                            Dictionary<string, string> rtn = new Dictionary<string, string>();
                            while (true)
                            {
                                string p = reader.ReadLine();
                                if (p == null)
                                    break;
                                if (p != DeathsTranslationDescription)
                                {
                                    string[] data = p.Split(' ');
                                    if (data.Length > 1)
                                        rtn.Add(data[0], string.Join(" ", data, 1, data.Length - 1));
                                    else
                                        L.LogWarning($"Error parsing death translation in \".\\{Data.LangStorage}{lang}\\{info.Name}\":\n{p}");
                                }
                            }
                            if (!deathloc.ContainsKey(lang))
                                deathloc.Add(lang, rtn);
                        }
                        else if (info.Name == "limblocalization.dat")
                        {
                            StringReader reader = new StringReader(File.ReadAllText(info.FullName));
                            Dictionary<ELimb, string> rtn = new Dictionary<ELimb, string>();
                            while (true)
                            {
                                string p = reader.ReadLine();
                                if (p == null)
                                    break;
                                if (p != DeathsLimbTranslationsDescription)
                                {
                                    string[] data = p.Split(' ');
                                    if (data.Length > 1)
                                    {
                                        if (Enum.TryParse(data[0], out ELimb result))
                                            rtn.Add(result, string.Join(" ", data, 1, data.Length - 1));
                                        else
                                            L.LogWarning("Invalid line, must match SDG.Unturned.ELimb enumerator list (LEFT|RIGHT)_(ARM|LEG|BACK|FOOT|FRONT|HAND), SPINE, SKULL. Line:\n" + p);
                                    }
                                    else
                                        L.LogWarning($"Error parsing limb translation in \".\\{Data.LangStorage}{lang}\\{info.Name}\":\n{p}");
                                }
                            }
                            if (!limbloc.ContainsKey(lang))
                                limbloc.Add(lang, rtn);
                        }

                    }

                }
                L.Log($"Loaded {Math.Max(Math.Max(languages.Count, deathloc.Count), limbloc.Count)} languages ({deathloc.Count} death files, {limbloc.Count} limb files, {languages.Count} localization files), default having {(languages.TryGetValue(DefaultLanguage, out Dictionary<string, TranslationData> d) ? d.Count.ToString(Data.Locale) : "0")} translations.");
            }
            else
            {
                L.LogError("Failed to load translations, see above.");
                languages.Add(DefaultLanguage, ConvertTranslations(DefaultTranslations, DefaultLanguage));
                limbloc.Add(DefaultLanguage, DefaultLimbTranslations);
                deathloc.Add(DefaultLanguage, DefaultDeathTranslations);
                return languages;
            }
            return languages;
        }
        public static Dictionary<string, TranslationData> ConvertTranslations(Dictionary<string, string> input, string language = null)
        {
            Dictionary<string, TranslationData> rtn = new Dictionary<string, TranslationData>(input.Count);
            IEnumerator<KeyValuePair<string, string>> enumerator = input.GetEnumerator();
            string current = string.Empty;
            try
            {
                while (enumerator.MoveNext())
                {
                    current = enumerator.Current.Key;
                    rtn.Add(current, new TranslationData(enumerator.Current.Value));
                }
            }
            catch (Exception ex)
            {
                L.LogError($"Error converting translation {current} for language {language ?? "unknown"}: ");
                L.LogError(ex);
            }
            finally
            {
                enumerator.Dispose();
            }
            return rtn;
        }
        public static Dictionary<int, Zone> LoadExtraZones()
        {
            F.CheckDir(Data.FlagStorage, out bool dirExists);
            if (dirExists)
            {
                if (!File.Exists(Data.FlagStorage + "extra_zones.json"))
                {
                    Dictionary<int, Zone> defaultXtraZones = new Dictionary<int, Zone>(DefaultExtraZones.Count);
                    using (FileStream stream = new FileStream(Data.FlagStorage + "extra_zones.json", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                    {
                        Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
                        writer.WriteStartArray();
                        for (int i = 0; i < DefaultExtraZones.Count; i++)
                        {
                            FlagData zone = DefaultExtraZones[i];
                            writer.WriteStartObject();
                            zone.WriteFlagData(writer);
                            writer.WriteEndObject();
                            defaultXtraZones.Add(zone.id, Flag.ComplexifyZone(zone));
                        }
                        writer.WriteEndArray();
                        writer.Dispose();
                        stream.Close();
                        stream.Dispose();
                    }
                    return defaultXtraZones;
                }
                using (FileStream stream = new FileStream(Data.FlagStorage + "extra_zones.json", FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    long len = stream.Length;
                    if (len > int.MaxValue)
                    {
                        L.LogError("extra_zones.json is too long to read.");
                        Dictionary<int, Zone> defaultXtraZones = new Dictionary<int, Zone>(DefaultExtraZones.Count);
                        for (int i = 0; i < DefaultExtraZones.Count; i++)
                        {
                            FlagData zone = DefaultExtraZones[i];
                            defaultXtraZones.Add(zone.id, Flag.ComplexifyZone(zone));
                        }
                        return defaultXtraZones;
                    }
                    else
                    {
                        Dictionary<int, Zone> xtraZones = new Dictionary<int, Zone>(DefaultExtraZones.Count);
                        byte[] bytes = new byte[len];
                        stream.Read(bytes, 0, (int)len);
                        Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.StartArray) continue;
                            else if (reader.TokenType == JsonTokenType.EndArray) break;
                            else if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                FlagData data = FlagData.ReadFlagData(ref reader);
                                xtraZones.Add(data.id, Flag.ComplexifyZone(data));
                            }
                        }

                        return xtraZones;
                    }
                }
            }
            else
            {
                L.LogError("Failed to load extra zones, see above. Loading default zones.");
                Dictionary<int, Zone> defaultXtraZones = new Dictionary<int, Zone>(DefaultExtraZones.Count);
                for (int i = 0; i < DefaultExtraZones.Count; i++)
                {
                    FlagData zone = DefaultExtraZones[i];
                    defaultXtraZones.Add(zone.id, Flag.ComplexifyZone(zone));
                }
                return defaultXtraZones;
            }
        }
        public static Dictionary<string, Vector3> LoadExtraPoints()
        {
            F.CheckDir(Data.FlagStorage, out bool dirExists);
            if (dirExists)
            {
                if (!File.Exists(Data.FlagStorage + "extra_points.json"))
                {
                    Dictionary<string, Vector3> defaultXtraPoints = new Dictionary<string, Vector3>(DefaultExtraPoints.Count);
                    using (FileStream stream = new FileStream(Data.FlagStorage + "extra_points.json", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                    {
                        Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
                        writer.WriteStartObject();
                        for (int i = 0; i < DefaultExtraPoints.Count; i++)
                        {
                            Point3D point = DefaultExtraPoints[i];
                            writer.WritePropertyName(point.name);
                            writer.WriteStartObject();
                            writer.WriteProperty("x", point.x);
                            writer.WriteProperty("y", point.y);
                            writer.WriteProperty("z", point.z);
                            writer.WriteEndObject();
                            defaultXtraPoints.Add(point.name, point.Vector3);
                        }
                        writer.WriteEndObject();
                        writer.Dispose();
                        stream.Close();
                        stream.Dispose();
                    }
                    return defaultXtraPoints;
                }
                using (FileStream stream = new FileStream(Data.FlagStorage + "extra_points.json", FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    long len = stream.Length;
                    if (len > int.MaxValue)
                    {
                        L.LogError("extra_points.json is too long to read.");
                        Dictionary<string, Vector3> defaultXtraPoints = new Dictionary<string, Vector3>(DefaultExtraPoints.Count);
                        for (int i = 0; i < DefaultExtraPoints.Count; i++)
                        {
                            Point3D point = DefaultExtraPoints[i];
                            defaultXtraPoints.Add(point.name, point.Vector3);
                        }
                        return defaultXtraPoints;
                    }
                    else
                    {
                        Dictionary<string, Vector3> xtraPoints = new Dictionary<string, Vector3>(DefaultExtraPoints.Count);
                        byte[] bytes = new byte[len];
                        stream.Read(bytes, 0, (int)len);
                        Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.StartObject) continue;
                            else if (reader.TokenType == JsonTokenType.EndObject) break;
                            else if (reader.TokenType == JsonTokenType.PropertyName)
                            {
                                string key = reader.GetString();
                                if (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
                                {
                                    float x = 0f;
                                    float y = 0f;
                                    float z = 0f;
                                    while (reader.Read())
                                    {
                                        if (reader.TokenType == JsonTokenType.EndObject) break;
                                        else if (reader.TokenType == JsonTokenType.PropertyName)
                                        {
                                            string prop = reader.GetString();
                                            switch (prop)
                                            {
                                                case "x":
                                                    x = (float)reader.GetDouble();
                                                    break;
                                                case "y":
                                                    y = (float)reader.GetDouble();
                                                    break;
                                                case "z":
                                                    z = (float)reader.GetDouble();
                                                    break;
                                            }
                                        }
                                    }
                                    xtraPoints.Add(key, new Vector3(x, y, z));
                                }
                            }
                        }

                        return xtraPoints;
                    }
                }
            }
            else
            {
                L.LogError("Failed to load extra points, see above. Loading default points.");
                Dictionary<string, Vector3> defaultXtraPoints = new Dictionary<string, Vector3>(DefaultExtraPoints.Count);
                for (int i = 0; i < DefaultExtraPoints.Count; i++)
                {
                    Point3D point = DefaultExtraPoints[i];
                    defaultXtraPoints.Add(point.name, point.Vector3);
                }
                return defaultXtraPoints;
            }
        }
        public static Dictionary<ulong, string> LoadLanguagePreferences()
        {
            F.CheckDir(Data.LangStorage, out bool dirExists);
            if (dirExists)
            {
                if (!File.Exists(Data.LangStorage + "preferences.json"))
                {
                    using (FileStream stream = new FileStream(Data.LangStorage + "preferences.json", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                    {
                        byte[] utf8 = System.Text.Encoding.UTF8.GetBytes("[]");
                        stream.Write(utf8, 0, utf8.Length);
                        stream.Close();
                        stream.Dispose();
                    }
                    return new Dictionary<ulong, string>();
                }
                using (FileStream stream = new FileStream(Data.LangStorage + "preferences.json", FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    long len = stream.Length;
                    if (len > int.MaxValue)
                    {
                        L.LogError("Language preferences at preferences.json is too long to read.");
                        return new Dictionary<ulong, string>();
                    }
                    else
                    {
                        Dictionary<ulong, string> prefs = new Dictionary<ulong, string>(48);
                        byte[] bytes = new byte[len];
                        stream.Read(bytes, 0, (int)len);
                        Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.StartObject) continue;
                            else if (reader.TokenType == JsonTokenType.EndObject) break;
                            else if (reader.TokenType == JsonTokenType.PropertyName)
                            {
                                string input = reader.GetString();
                                if (!ulong.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out ulong steam64))
                                {
                                    L.LogWarning("Invalid Steam64 ID: \"" + input + "\" in Lang\\preferences.json");
                                }
                                else if (reader.Read() && reader.TokenType == JsonTokenType.String)
                                {
                                    string language = reader.GetString();
                                    prefs.Add(steam64, language);
                                }
                            }
                        }

                        return prefs;
                    }
                }
            }
            else
            {
                L.LogError("Failed to load language preferences, see above.");
                return new Dictionary<ulong, string>();
            }
        }
        public static void SaveLangs(Dictionary<ulong, string> languages)
        {
            if (languages == null) return;
            using (FileStream stream = new FileStream(Data.LangStorage + "preferences.json", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
                writer.WriteStartObject();
                foreach (KeyValuePair<ulong, string> languagePref in languages)
                {
                    writer.WritePropertyName(languagePref.Key.ToString(Data.Locale));
                    writer.WriteStringValue(languagePref.Value);
                }
                writer.WriteEndObject();
                writer.Dispose();
                stream.Close();
                stream.Dispose();
            }
        }
        public static void SetLanguage(ulong player, string language)
        {
            if (Data.Languages.ContainsKey(player))
            {
                Data.Languages[player] = language;
                SaveLangs(Data.Languages);
            }
            else
            {
                Data.Languages.Add(player, language);
                SaveLangs(Data.Languages);
            }
        }
        public static Dictionary<string, LanguageAliasSet> LoadLangAliases()
        {
            F.CheckDir(Data.LangStorage, out bool dirExists);
            if (dirExists)
            {
                if (!File.Exists(Data.LangStorage + "aliases.json"))
                {
                    Dictionary<string, LanguageAliasSet> defaultLanguageAliasSets = new Dictionary<string, LanguageAliasSet>(DefaultLanguageAliasSets.Count);
                    using (FileStream stream = new FileStream(Data.LangStorage + "aliases.json", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                    {
                        Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
                        writer.WriteStartArray();
                        for (int i = 0; i < DefaultLanguageAliasSets.Count; i++)
                        {
                            LanguageAliasSet set = DefaultLanguageAliasSets[i];
                            writer.WriteStartObject();
                            set.WriteJson(writer);
                            writer.WriteEndObject();
                            defaultLanguageAliasSets.Add(set.key, set);
                        }
                        writer.WriteEndArray();
                        writer.Dispose();
                        stream.Close();
                        stream.Dispose();
                    }
                    return defaultLanguageAliasSets;
                }
                using (FileStream stream = new FileStream(Data.LangStorage + "aliases.json", FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    long len = stream.Length;
                    if (len > int.MaxValue)
                    {
                        L.LogError("Language alias sets at aliases.json is too long to read.");
                        Dictionary<string, LanguageAliasSet> defaultLanguageAliasSets = new Dictionary<string, LanguageAliasSet>(DefaultLanguageAliasSets.Count);
                        for (int i = 0; i < DefaultLanguageAliasSets.Count; i++)
                        {
                            LanguageAliasSet set = DefaultLanguageAliasSets[i];
                            defaultLanguageAliasSets.Add(set.key, set);
                        }
                        return defaultLanguageAliasSets;
                    }
                    else
                    {
                        Dictionary<string, LanguageAliasSet> languageAliasSets = new Dictionary<string, LanguageAliasSet>(DefaultLanguageAliasSets.Count);
                        byte[] bytes = new byte[len];
                        stream.Read(bytes, 0, (int)len);
                        Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.StartArray) continue;
                            else if (reader.TokenType == JsonTokenType.EndArray) break;
                            else if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                LanguageAliasSet set = new LanguageAliasSet();
                                set.ReadJson(ref reader);
                                languageAliasSets.Add(set.key, set);
                            }
                        }

                        return languageAliasSets;
                    }
                }
            }
            else
            {
                L.LogError("Failed to load language aliases, see above. Loading default language aliases.");
                Dictionary<string, LanguageAliasSet> defaultLanguageAliasSets = new Dictionary<string, LanguageAliasSet>(DefaultLanguageAliasSets.Count);
                for (int i = 0; i < DefaultLanguageAliasSets.Count; i++)
                {
                    LanguageAliasSet set = DefaultLanguageAliasSets[i];
                    defaultLanguageAliasSets.Add(set.key, set);
                }
                return defaultLanguageAliasSets;
            }
            }
    }
}
