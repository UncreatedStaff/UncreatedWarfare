using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncreatedWarfare.Flags;
using System.IO;
using UnityEngine;
using SDG.Unturned;
using FlagData = UncreatedWarfare.Flags.FlagData;
using UncreatedWarfare.Teams;
using Flag = UncreatedWarfare.Flags.Flag;
using UncreatedWarfare.Stats;

namespace UncreatedWarfare
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
    public struct TeamData
    {
        public ulong team_id;
        public string name;
        public List<ulong> players;
        public float spawnpoint_x;
        public float spawnpoint_y;
        public float spawnpoint_z;
        [JsonConstructor]
        public TeamData(ulong team_id, string name, List<ulong> players, float spawnpoint_x, float spawnpoint_y, float spawnpoint_z)
        {
            this.team_id = team_id;
            this.name = name;
            this.players = players;
            this.spawnpoint_x = spawnpoint_x;
            this.spawnpoint_y = spawnpoint_y;
            this.spawnpoint_z = spawnpoint_z;
        }
    }
    public struct XPData
    {
        public string key;
        public int xp;
        [JsonConstructor]
        public XPData(string key, int xp)
        {
            this.key = key;
            this.xp = xp;
        }
        public XPData(EXPGainType key, int xp)
        {
            this.key = key.ToString();
            this.xp = xp;
        }
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
    public struct CreditsData
    {
        public string key;
        public int credits;
        [JsonConstructor]
        public CreditsData(string key, int xp)
        {
            this.key = key;
            this.credits = xp;
        }
        public CreditsData(ECreditsGainType key, int xp)
        {
            this.key = key.ToString();
            this.credits = xp;
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
    public struct CallData
    {
        public string key;
        public string call;
        [JsonConstructor]
        public CallData(string key, string call)
        {
            this.key = key;
            this.call = call;
        }
        public CallData(ECall key, string call)
        {
            this.key = key.ToString();
            this.call = call;
        }
    }
    public struct LanguageAliasSet
    {
        public string key;
        public string display_name;
        public List<string> values;
        [JsonConstructor]
        public LanguageAliasSet(string key, string display_name, List<string> values)
        {
            this.key = key;
            this.display_name = display_name;
            this.values = values;
        }
    }
    public struct Translation
    {
        public string key;
        public string value;
        [JsonConstructor]
        public Translation(string key, string value)
        {
            this.key = key;
            this.value = value;
        }
    }
    public struct MySqlColumnData
    {
        public string key;
        public string name;
        [JsonConstructor]
        public MySqlColumnData(string key, string name)
        {
            this.key = key;
            this.name = name;
        }
    }
    public struct MySqlTableData
    {
        public string TableName;
        public string key;
        public List<MySqlColumnData> Columns;
        [JsonConstructor]
        public MySqlTableData(string key, string tableName, List<MySqlColumnData> columns)
        {
            this.key = key;
            this.TableName = tableName;
            this.Columns = columns;
        }
    }
    public struct MySqlTableLang
    {
        public string TableName;
        public Dictionary<string, string> Columns;
        public MySqlTableLang(string tableName, Dictionary<string,string> columns)
        {
            this.TableName = tableName;
            this.Columns = columns;
        }
        public override string ToString() => TableName;
    }
    public static partial class JSONMethods
    {
        public const string DefaultLanguage = "en-us";
        public static List<FlagData> ReadFlags(string Preset)
        {
            if(!File.Exists(UCWarfare.FlagStorage + Preset + ".json"))
            {
                SaveFlags(DefaultFlags, Preset);
                return DefaultFlags;
            }
            List<FlagData> Flags;
            using (StreamReader Reader = File.OpenText(UCWarfare.FlagStorage + Preset + ".json"))
            {
                Flags = JsonConvert.DeserializeObject<List<FlagData>>(Reader.ReadToEnd());
                Reader.Close();
                Reader.Dispose();
            }
            return Flags ?? DefaultFlags;
        }
        public static void SaveFlags(this List<FlagData> Flags, string Preset)
        {
            using(StreamWriter TextWriter = File.CreateText(UCWarfare.FlagStorage + Preset + ".json"))
            {
                using (JsonWriter JsonWriter = new JsonTextWriter(TextWriter))
                {
                    JsonSerializer Serializer = new JsonSerializer();
                    Serializer.Formatting = Formatting.Indented;
                    Serializer.Serialize(JsonWriter, Flags);
                    JsonWriter.Close();
                    TextWriter.Close();
                    TextWriter.Dispose();
                }
            }
        }
        public static void AddFlag(this FlagData flag, string Preset)
        {
            List<FlagData> Data = ReadFlags(Preset);
            Data.Add(flag);
            Data.SaveFlags(Preset);
        }
        public static void RemoveFlag(this FlagData flag, string Preset)
        {
            List<FlagData> Data = ReadFlags(Preset);
            Data.RemoveAll(x => x.id == flag.id);
            Data.SaveFlags(Preset);
        }
        public static void ClearPreset(string Preset)
        {
            SaveFlags(new List<FlagData>(), Preset);
        }
        public static FlagData GetFlagInfo(int id, string Preset)
        {
            List<FlagData> Data = ReadFlags(Preset);
            return Data.FirstOrDefault(x => x.id == id);
        }
        public static Dictionary<string, Color> LoadColors(out Dictionary<string, string> HexValues)
        {
            if (!File.Exists(UCWarfare.DataDirectory + "chat_colors.json"))
            {
                using (StreamWriter TextWriter = File.CreateText(UCWarfare.DataDirectory + "chat_colors.json"))
                {
                    using (JsonWriter JsonWriter = new JsonTextWriter(TextWriter))
                    {
                        JsonSerializer Serializer = new JsonSerializer();
                        Serializer.Formatting = Formatting.Indented;
                        Serializer.Serialize(JsonWriter, DefaultColors);
                        JsonWriter.Close();
                        TextWriter.Close();
                        TextWriter.Dispose();
                    }
                }
                Dictionary<string, Color> NewDefaults = new Dictionary<string, Color>();
                Dictionary<string, string> NewDefaultsHex = new Dictionary<string, string>();
                foreach(ColorData data in DefaultColors)
                {
                    NewDefaults.Add(data.key, data.Color);
                    NewDefaultsHex.Add(data.key, data.color_hex);
                }
                HexValues = NewDefaultsHex;
                return NewDefaults;
            }
            List<ColorData> Colors;
            using (StreamReader Reader = File.OpenText(UCWarfare.DataDirectory + "chat_colors.json"))
            {
                Colors = JsonConvert.DeserializeObject<List<ColorData>>(Reader.ReadToEnd());
                Reader.Close();
                Reader.Dispose();
            }
            Dictionary<string, Color> NewColors = new Dictionary<string, Color>();
            Dictionary<string, string> NewColorsHex = new Dictionary<string, string>();
            foreach (ColorData data in Colors ?? DefaultColors)
            {
                NewColors.Add(data.key, data.Color);
                NewColorsHex.Add(data.key, data.color_hex);
            }
            HexValues = NewColorsHex;
            return NewColors;
        }
        public static Dictionary<ECreditsGainType, int> LoadCredits()
        {
            if (!File.Exists(UCWarfare.DataDirectory + "credit_values.json"))
            {
                using (StreamWriter TextWriter = File.CreateText(UCWarfare.DataDirectory + "credit_values.json"))
                {
                    using (JsonWriter JsonWriter = new JsonTextWriter(TextWriter))
                    {
                        JsonSerializer Serializer = new JsonSerializer();
                        Serializer.Formatting = Formatting.Indented;
                        Serializer.Serialize(JsonWriter, DefaultCreditData);
                        JsonWriter.Close();
                        TextWriter.Close();
                        TextWriter.Dispose();
                    }
                }
                Dictionary<ECreditsGainType, int> NewDefaults = new Dictionary<ECreditsGainType, int>();
                foreach (CreditsData data in DefaultCreditData)
                {
                    NewDefaults.Add((ECreditsGainType)Enum.Parse(typeof(ECreditsGainType), data.key), data.credits);
                }
                return NewDefaults;
            }
            List<CreditsData> Credits;
            using (StreamReader Reader = File.OpenText(UCWarfare.DataDirectory + "credit_values.json"))
            {
                Credits = JsonConvert.DeserializeObject<List<CreditsData>>(Reader.ReadToEnd());
                Reader.Close();
                Reader.Dispose();
            }
            Dictionary<ECreditsGainType, int> NewCredits = new Dictionary<ECreditsGainType, int>();
            foreach (CreditsData data in Credits ?? DefaultCreditData)
            {
                try
                {
                    NewCredits.Add((ECreditsGainType)Enum.Parse(typeof(ECreditsGainType), data.key), data.credits);
                }
                catch
                {
                    CommandWindow.LogError(data.key + " is not a valid value for Credit type.");
                }
            }
            return NewCredits;
        }
        public static Dictionary<EXPGainType, int> LoadXP()
        {
            if (!File.Exists(UCWarfare.DataDirectory + "xp_values.json"))
            {
                using (StreamWriter TextWriter = File.CreateText(UCWarfare.DataDirectory + "xp_values.json"))
                {
                    using (JsonWriter JsonWriter = new JsonTextWriter(TextWriter))
                    {
                        JsonSerializer Serializer = new JsonSerializer();
                        Serializer.Formatting = Formatting.Indented;
                        Serializer.Serialize(JsonWriter, DefaultXPData);
                        JsonWriter.Close();
                        TextWriter.Close();
                        TextWriter.Dispose();
                    }
                }
                Dictionary<EXPGainType, int> NewDefaults = new Dictionary<EXPGainType, int>();
                foreach (XPData data in DefaultXPData)
                {
                    NewDefaults.Add((EXPGainType)Enum.Parse(typeof(EXPGainType), data.key), data.xp);
                }
                return NewDefaults;
            }
            List<XPData> XPs;
            using (StreamReader Reader = File.OpenText(UCWarfare.DataDirectory + "xp_values.json"))
            {
                XPs = JsonConvert.DeserializeObject<List<XPData>>(Reader.ReadToEnd());
                Reader.Close();
                Reader.Dispose();
            }
            Dictionary<EXPGainType, int> NewXPs = new Dictionary<EXPGainType, int>();
            foreach (XPData data in XPs ?? DefaultXPData)
            {
                try
                {
                    NewXPs.Add((EXPGainType)Enum.Parse(typeof(EXPGainType), data.key), data.xp);
                } catch
                {
                    CommandWindow.LogError(data.key + " is not a valid value for XP type");
                }
            }
            return NewXPs;
        }
        public static Dictionary<string, Dictionary<string, string>> LoadTranslations()
        {
            string[] langFiles = Directory.GetFiles(UCWarfare.LangStorage, "*.json", SearchOption.TopDirectoryOnly);
            Dictionary<string, Dictionary<string, string>> languages = new Dictionary<string, Dictionary<string, string>>();
            if (!File.Exists(UCWarfare.LangStorage + DefaultLanguage + ".json"))
            {
                using (StreamWriter TextWriter = File.CreateText(UCWarfare.LangStorage + DefaultLanguage + ".json"))
                {
                    using (JsonWriter JsonWriter = new JsonTextWriter(TextWriter))
                    {
                        JsonSerializer Serializer = new JsonSerializer();
                        Serializer.Formatting = Formatting.Indented;
                        List<Translation> t = new List<Translation>();
                        foreach (KeyValuePair<string, string> translation in DefaultTranslations)
                        {
                            t.Add(new Translation(translation.Key, translation.Value));
                        }
                        Serializer.Serialize(JsonWriter, t);
                        JsonWriter.Close();
                        TextWriter.Close();
                        TextWriter.Dispose();
                    }
                }
                languages.Add(DefaultLanguage, DefaultTranslations);
            }
            foreach (string file in langFiles)
            {
                FileInfo info = new FileInfo(file);
                List<Translation> Translations;
                using (StreamReader Reader = File.OpenText(file))
                {
                    Translations = JsonConvert.DeserializeObject<List<Translation>>(Reader.ReadToEnd());
                    Reader.Close();
                    Reader.Dispose();
                }
                if (Translations == null) continue;
                Dictionary<string, string> translationDict = new Dictionary<string, string>();
                foreach (Translation data in Translations)
                {
                    try
                    {
                        translationDict.Add(data.key, data.value);
                    }
                    catch
                    {
                        CommandWindow.LogWarning("\"" + data.key + "\" has a duplicate key in translation file: (" + info.Name + ")!");
                    }
                }
                string langName = info.Name;
                if (langName.Length > 5)
                    langName = langName.Substring(0, langName.Length - 5);
                languages.Add(langName, translationDict);
            }
            return languages;
        }
        public static Dictionary<int, Zone> LoadExtraZones()
        {
            if (!File.Exists(UCWarfare.DataDirectory + "extra_zones.json"))
            {
                using (StreamWriter TextWriter = File.CreateText(UCWarfare.DataDirectory + "extra_zones.json"))
                {
                    using (JsonWriter JsonWriter = new JsonTextWriter(TextWriter))
                    {
                        JsonSerializer Serializer = new JsonSerializer();
                        Serializer.Formatting = Formatting.Indented;
                        Serializer.Serialize(JsonWriter, DefaultExtraZones);
                        JsonWriter.Close();
                        TextWriter.Close();
                        TextWriter.Dispose();
                    }
                }
                Dictionary<int, Zone> NewDefaultZones = new Dictionary<int, Zone>();
                foreach(FlagData zone in DefaultExtraZones)
                    NewDefaultZones.Add(zone.id, Flag.ComplexifyZone(zone));
                return NewDefaultZones;
            }
            List<FlagData> Zones;
            using (StreamReader Reader = File.OpenText(UCWarfare.DataDirectory + "extra_zones.json"))
            {
                Zones = JsonConvert.DeserializeObject<List<FlagData>>(Reader.ReadToEnd());
                Reader.Close();
                Reader.Dispose();
            }
            if (Zones == null)
            {
                Dictionary<int, Zone> NewDefaultZones = new Dictionary<int, Zone>();
                foreach (FlagData zone in DefaultExtraZones)
                    NewDefaultZones.Add(zone.id, Flag.ComplexifyZone(zone));
                return NewDefaultZones;
            }
            Dictionary<int, Zone> NewZones = new Dictionary<int, Zone>();
            foreach (FlagData zone in Zones)
                NewZones.Add(zone.id, Flag.ComplexifyZone(zone));
            return NewZones;
        }
        public static Dictionary<string, Vector3> LoadExtraPoints()
        {
            if (!File.Exists(UCWarfare.DataDirectory + "extra_points.json"))
            {
                using (StreamWriter TextWriter = File.CreateText(UCWarfare.DataDirectory + "extra_points.json"))
                {
                    using (JsonWriter JsonWriter = new JsonTextWriter(TextWriter))
                    {
                        JsonSerializer Serializer = new JsonSerializer();
                        Serializer.Formatting = Formatting.Indented;
                        Serializer.Serialize(JsonWriter, DefaultExtraPoints);
                        JsonWriter.Close();
                        TextWriter.Close();
                        TextWriter.Dispose();
                    }
                }
                Dictionary<string, Vector3> NewDefaultPoints = new Dictionary<string, Vector3>();
                foreach (Point3D point in DefaultExtraPoints)
                    NewDefaultPoints.Add(point.name, point.Vector3);
                return NewDefaultPoints;
            }
            List<Point3D> Points;
            using (StreamReader Reader = File.OpenText(UCWarfare.DataDirectory + "extra_points.json"))
            {
                Points = JsonConvert.DeserializeObject<List<Point3D>>(Reader.ReadToEnd());
                Reader.Close();
                Reader.Dispose();
            }
            if (Points == null)
            {
                Dictionary<string, Vector3> NewDefaultPoints = new Dictionary<string, Vector3>();
                foreach (Point3D point in DefaultExtraPoints)
                    NewDefaultPoints.Add(point.name, point.Vector3);
                return NewDefaultPoints;
            }
            Dictionary<string, Vector3> NewPoints = new Dictionary<string, Vector3>();
            foreach (Point3D point in Points)
                NewPoints.Add(point.name, point.Vector3);
            return NewPoints;
        }
        public static Dictionary<string, MySqlTableLang> LoadTables()
        {
            if (!File.Exists(UCWarfare.DataDirectory + "tables.json"))
            {
                using (StreamWriter TextWriter = File.CreateText(UCWarfare.DataDirectory + "tables.json"))
                {
                    using (JsonWriter JsonWriter = new JsonTextWriter(TextWriter))
                    {
                        JsonSerializer Serializer = new JsonSerializer();
                        Serializer.Formatting = Formatting.Indented;
                        Serializer.Serialize(JsonWriter, DefaultMySQLTableData);
                        JsonWriter.Close();
                        TextWriter.Close();
                        TextWriter.Dispose();
                    }
                }
                Dictionary<string, MySqlTableLang> NewDefaultTables = new Dictionary<string, MySqlTableLang>();
                foreach (MySqlTableData table in DefaultMySQLTableData)
                {
                    Dictionary<string, string> columns = new Dictionary<string, string>();
                    foreach (MySqlColumnData column in table.Columns)
                        columns.Add(column.key, column.name);
                    NewDefaultTables.Add(table.key, new MySqlTableLang(table.TableName, columns));
                }
                return NewDefaultTables;
            }
            List<MySqlTableData> Tables;
            using (StreamReader Reader = File.OpenText(UCWarfare.DataDirectory + "tables.json"))
            {
                Tables = JsonConvert.DeserializeObject<List<MySqlTableData>>(Reader.ReadToEnd());
                Reader.Close();
                Reader.Dispose();
            }
            if (Tables == null)
            {
                Dictionary<string, MySqlTableLang> NewDefaultTables = new Dictionary<string, MySqlTableLang>();
                foreach (MySqlTableData table in DefaultMySQLTableData)
                {
                    Dictionary<string, string> columns = new Dictionary<string, string>();
                    foreach (MySqlColumnData column in table.Columns)
                        columns.Add(column.key, column.name);
                    NewDefaultTables.Add(table.key, new MySqlTableLang(table.TableName, columns));
                }
                return NewDefaultTables;
            }
            Dictionary<string, MySqlTableLang> NewTables = new Dictionary<string, MySqlTableLang>();
            foreach (MySqlTableData table in Tables)
            {
                Dictionary<string, string> columns = new Dictionary<string, string>();
                foreach (MySqlColumnData column in table.Columns)
                    columns.Add(column.key, column.name);
                NewTables.Add(table.key, new MySqlTableLang(table.TableName, columns));
            }
            return NewTables;
        }
        public static Dictionary<ECall, string> LoadCalls()
        {
            if (!File.Exists(UCWarfare.DataDirectory + "node-calls.json"))
            {
                
                using (StreamWriter TextWriter = File.CreateText(UCWarfare.DataDirectory + "node-calls.json"))
                {
                    using (JsonWriter JsonWriter = new JsonTextWriter(TextWriter))
                    {
                        JsonSerializer Serializer = new JsonSerializer();
                        Serializer.Formatting = Formatting.Indented;
                        Serializer.Serialize(JsonWriter, DefaultNodeCalls);
                        JsonWriter.Close();
                        TextWriter.Close();
                        TextWriter.Dispose();
                    }
                }
                Dictionary<ECall, string> DefaultNewCalls = new Dictionary<ECall, string>();
                foreach (CallData call in DefaultNodeCalls)
                {
                    DefaultNewCalls.Add((ECall)Enum.Parse(typeof(ECall), call.key), call.call);
                }
                return DefaultNewCalls;
            }
            List<CallData> Calls;
            using (StreamReader Reader = File.OpenText(UCWarfare.DataDirectory + "node-calls.json"))
            {
                Calls = JsonConvert.DeserializeObject<List<CallData>>(Reader.ReadToEnd());
                Reader.Close();
                Reader.Dispose();
            }
            if (Calls == null)
            {
                Dictionary<ECall, string> DefaultNewCalls = new Dictionary<ECall, string>();
                foreach (CallData call in DefaultNodeCalls)
                {
                    DefaultNewCalls.Add((ECall)Enum.Parse(typeof(ECall), call.key), call.call);
                }
                return DefaultNewCalls;
            }
            Dictionary<ECall, string> NewCalls = new Dictionary<ECall, string>();
            foreach (CallData call in Calls)
            {
                NewCalls.Add((ECall)Enum.Parse(typeof(ECall), call.key), call.call);
            }
            return NewCalls;
        }
        public static Dictionary<ulong, string> LoadLanguagePreferences()
        {
            if (!File.Exists(UCWarfare.LangStorage + "preferences.json"))
            {
                using (StreamWriter TextWriter = File.CreateText(UCWarfare.LangStorage + "preferences.json"))
                {
                    TextWriter.Write("[]");
                    TextWriter.Close();
                    TextWriter.Dispose();
                }
                return new Dictionary<ulong, string>();
            }
            List<LangData> Languages;
            using (StreamReader Reader = File.OpenText(UCWarfare.LangStorage + "preferences.json"))
            {
                Languages = JsonConvert.DeserializeObject<List<LangData>>(Reader.ReadToEnd());
                Reader.Close();
                Reader.Dispose();
            }
            if (Languages == null) return new Dictionary<ulong, string>();
            Dictionary<ulong, string> NewLanguages = new Dictionary<ulong, string>();
            foreach (LangData player in Languages)
                NewLanguages.Add(player.player, player.language);
            return NewLanguages;
        }
        public static void SaveLangs(Dictionary<ulong, string> Languages)
        {
            if (Languages == null) return;
            List<LangData> data = new List<LangData>();
            foreach (KeyValuePair<ulong, string> player in Languages)
                data.Add(new LangData(player.Key, player.Value));
            using (StreamWriter TextWriter = File.CreateText(UCWarfare.LangStorage + "preferences.json"))
            {
                if (data.Count == 0) TextWriter.Write("[]");
                else
                {
                    using (JsonWriter JsonWriter = new JsonTextWriter(TextWriter))
                    {
                        JsonSerializer Serializer = new JsonSerializer();
                        Serializer.Formatting = Formatting.Indented;
                        Serializer.Serialize(JsonWriter, data);
                        JsonWriter.Close();
                        TextWriter.Close();
                        TextWriter.Dispose();
                    }
                }
            }
        }
        public static void SetLanguage(ulong player, string language)
        {
            if(UCWarfare.I.Languages.ContainsKey(player))
            {
                UCWarfare.I.Languages[player] = language;
                SaveLangs(UCWarfare.I.Languages);
            } else
            {
                UCWarfare.I.Languages.Add(player, language);
                SaveLangs(UCWarfare.I.Languages);
            }
        }
        public static Dictionary<string, LanguageAliasSet> LoadLangAliases()
        {
            if (!File.Exists(UCWarfare.LangStorage + "aliases.json"))
            {

                using (StreamWriter TextWriter = File.CreateText(UCWarfare.LangStorage + "aliases.json"))
                {
                    using (JsonWriter JsonWriter = new JsonTextWriter(TextWriter))
                    {
                        JsonSerializer Serializer = new JsonSerializer();
                        Serializer.Formatting = Formatting.Indented;
                        Serializer.Serialize(JsonWriter, DefaultLanguageAliasSets);
                        JsonWriter.Close();
                        TextWriter.Close();
                        TextWriter.Dispose();
                    }
                }
                Dictionary<string, LanguageAliasSet> DefaultNewAliases = new Dictionary<string, LanguageAliasSet>();
                foreach (LanguageAliasSet set in DefaultLanguageAliasSets)
                {
                    DefaultNewAliases.Add(set.key, set);
                }
                return DefaultNewAliases;
            }
            List<LanguageAliasSet> Sets;
            using (StreamReader Reader = File.OpenText(UCWarfare.LangStorage + "aliases.json"))
            {
                Sets = JsonConvert.DeserializeObject<List<LanguageAliasSet>>(Reader.ReadToEnd());
                Reader.Close();
                Reader.Dispose();
            }
            if (Sets == null)
            {
                Dictionary<string, LanguageAliasSet> DefaultNewAliases = new Dictionary<string, LanguageAliasSet>();
                foreach (LanguageAliasSet set in DefaultLanguageAliasSets)
                {
                    DefaultNewAliases.Add(set.key, set);
                }
                return DefaultNewAliases;
            }
            Dictionary<string, LanguageAliasSet> NewAliases = new Dictionary<string, LanguageAliasSet>();
            foreach (LanguageAliasSet set in Sets)
            {
                NewAliases.Add(set.key, set);
            }
            return NewAliases;
        }
    }
    public enum EXPGainType : byte
    {
        CAP_INCREASE,
        WIN,
        KILL,
        DEFENCE_KILL,
        OFFENCE_KILL,
        CAPTURE_KILL,
        CAPTURE,
        HOLDING_POINT
    }
    public enum ECreditsGainType : byte
    {
        CAPTURE,
        WIN
    }
}
