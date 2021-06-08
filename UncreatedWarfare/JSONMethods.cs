using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Flags;
using System.IO;
using UnityEngine;
using SDG.Unturned;
using FlagData = Uncreated.Warfare.Flags.FlagData;
using Uncreated.Warfare.Teams;
using Flag = Uncreated.Warfare.Flags.Flag;
using Uncreated.Warfare.Stats;

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
    public struct SerializableVector3
    {
        public static readonly SerializableVector3 Zero = new SerializableVector3(0, 0, 0);
        public float x;
        public float y;
        public float z;
        [JsonIgnore]
        public Vector3 Vector3 { 
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
        public override string ToString() => $"({Mathf.RoundToInt(x)}, {Mathf.RoundToInt(y)}, {Mathf.RoundToInt(z)})";
        public SerializableVector3(Vector3 v)
        {
            if(v == default)
            {
                x = 0;
                y = 0;
                z = 0;
            } else
            {
                x = v.x;
                y = v.y;
                z = v.z;
            }
        }
        [JsonConstructor]
        public SerializableVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }
    public struct SerializableTransform
    {
        public static readonly SerializableTransform Zero = new SerializableTransform(SerializableVector3.Zero, SerializableVector3.Zero);
        public SerializableVector3 position;
        public SerializableVector3 euler_angles;
        [JsonIgnore]
        public Quaternion Rotation { get => Quaternion.Euler(euler_angles.Vector3); }
        [JsonIgnore]
        public Vector3 Position { get => position.Vector3; }
        public static bool operator ==(SerializableTransform a, SerializableTransform b) => a.position == b.position && a.euler_angles == b.euler_angles;
        public static bool operator ==(SerializableTransform a, Transform b) => a.position == b.position && a.euler_angles == b.eulerAngles;
        public static bool operator !=(SerializableTransform a, SerializableTransform b) => a.position != b.position || a.euler_angles != b.euler_angles;
        public static bool operator !=(SerializableTransform a, Transform b) => a.position != b.position || a.euler_angles != b.eulerAngles;
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
        public SerializableTransform(Vector3 position, Quaternion rotation)
        {
            this.position = new SerializableVector3(position);
            this.euler_angles = new SerializableVector3(rotation.eulerAngles);
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
        public static List<FlagData> ReadFlags()
        {
            F.CheckDir(Data.FlagStorage, out bool madeFolder);
            if(madeFolder)
            {
                if (!File.Exists(Data.FlagStorage + "flags.json"))
                {
                    SaveFlags(DefaultFlags);
                } else
                {
                    List<FlagData> Flags;
                    using (StreamReader Reader = File.OpenText(Data.FlagStorage + "flags.json"))
                    {
                        Flags = JsonConvert.DeserializeObject<List<FlagData>>(Reader.ReadToEnd());
                        Reader.Close();
                        Reader.Dispose();
                    }
                    return Flags ?? DefaultFlags;
                }
            }
            return DefaultFlags;
        }
        public static void SaveFlags(this List<FlagData> Flags)
        {
            using (StreamWriter TextWriter = File.CreateText(Data.FlagStorage + "flags.json"))
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
        public static void AddFlag(this FlagData flag)
        {
            List<FlagData> Data = ReadFlags();
            Data.Add(flag);
            Data.SaveFlags();
        }
        public static void RemoveFlag(this FlagData flag)
        {
            List<FlagData> Data = ReadFlags();
            Data.RemoveAll(x => x.id == flag.id);
            Data.SaveFlags();
        }
        public static FlagData GetFlagInfo(int id)
        {
            List<FlagData> Data = ReadFlags();
            return Data.FirstOrDefault(x => x.id == id);
        }
        public static Dictionary<string, Color> LoadColors(out Dictionary<string, string> HexValues)
        {
            if (!File.Exists(Data.DataDirectory + "chat_colors.json"))
            {
                using (StreamWriter TextWriter = File.CreateText(Data.DataDirectory + "chat_colors.json"))
                {
                    using (JsonWriter JsonWriter = new JsonTextWriter(TextWriter))
                    {
                        JsonSerializer Serializer = new JsonSerializer() { Formatting = Formatting.Indented };
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
            using (StreamReader Reader = File.OpenText(Data.DataDirectory + "chat_colors.json"))
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
            if (!File.Exists(Data.DataDirectory + "credit_values.json"))
            {
                using (StreamWriter TextWriter = File.CreateText(Data.DataDirectory + "credit_values.json"))
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
            using (StreamReader Reader = File.OpenText(Data.DataDirectory + "credit_values.json"))
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
                    F.LogError(data.key + " is not a valid value for Credit type.");
                }
            }
            return NewCredits;
        }
        public static Dictionary<EXPGainType, int> LoadXP()
        {
            if (!File.Exists(Data.DataDirectory + "xp_values.json"))
            {
                using (StreamWriter TextWriter = File.CreateText(Data.DataDirectory + "xp_values.json"))
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
            using (StreamReader Reader = File.OpenText(Data.DataDirectory + "xp_values.json"))
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
                    F.LogError(data.key + " is not a valid value for XP type");
                }
            }
            return NewXPs;
        }
        public static Dictionary<string, Dictionary<string, string>> LoadTranslations(
            out Dictionary<string, Dictionary<string, string>> deathloc, out Dictionary<string, Dictionary<ELimb, string>> limbloc)
        {
            string[] langDirs = Directory.GetDirectories(Data.LangStorage, "*", SearchOption.TopDirectoryOnly);
            Dictionary<string, Dictionary<string, string>> languages = new Dictionary<string, Dictionary<string, string>>();
            deathloc = new Dictionary<string, Dictionary<string, string>>();
            limbloc = new Dictionary<string, Dictionary<ELimb, string>>();
            F.CheckDir(Data.LangStorage + DefaultLanguage, out bool madeDir);
            if(madeDir)
            {
                if (!File.Exists(Data.LangStorage + DefaultLanguage + @"\localization.json"))
                {
                    using (StreamWriter TextWriter = File.CreateText(Data.LangStorage + DefaultLanguage + @"\localization.json"))
                    {
                        using (JsonWriter JsonWriter = new JsonTextWriter(TextWriter))
                        {
                            JsonSerializer Serializer = new JsonSerializer { Formatting = Formatting.Indented };
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
                }
                if (!File.Exists(Data.LangStorage + DefaultLanguage + @"\deathlocalization.dat"))
                {
                    using (StreamWriter TextWriter = File.CreateText(Data.LangStorage + DefaultLanguage + @"\deathlocalization.dat"))
                    {
                        TextWriter.WriteLine(DeathsTranslationDescription);
                        foreach (KeyValuePair<string, string> dmsg in DefaultDeathTranslations)
                            TextWriter.WriteLine(dmsg.Key + ' ' + dmsg.Value);
                        TextWriter.Close();
                        TextWriter.Dispose();
                    }
                }
                if (!File.Exists(Data.LangStorage + DefaultLanguage + @"\limblocalization.dat"))
                {
                    using (StreamWriter TextWriter = File.CreateText(Data.LangStorage + DefaultLanguage + @"\limblocalization.dat"))
                    {
                        TextWriter.WriteLine(DeathsLimbTranslationsDescription);
                        foreach (KeyValuePair<ELimb, string> dmsg in DefaultLimbTranslations)
                            TextWriter.WriteLine(dmsg.Key.ToString() + ' ' + dmsg.Value);
                        TextWriter.Close();
                        TextWriter.Dispose();
                    }
                }
                foreach (string folder in langDirs)
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(folder);
                    string[] langFiles = Directory.GetFiles(folder, "*", SearchOption.TopDirectoryOnly);
                    foreach (string file in langFiles)
                    {
                        FileInfo info = new FileInfo(file);
                        if (info.Name == "localization.json")
                        {
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
                                    F.LogWarning("\"" + data.key + "\" has a duplicate key in translation file: (" + info.Name + ")!");
                                }
                            }
                            if (!languages.ContainsKey(directoryInfo.Name))
                                languages.Add(directoryInfo.Name, translationDict);
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
                                        F.LogWarning($"Error parsing death translation in \".\\{Data.LangStorage}{directoryInfo.Name}\\{info.Name}\":\n{p}");
                                }
                            }
                            if (!deathloc.ContainsKey(directoryInfo.Name))
                                deathloc.Add(directoryInfo.Name, rtn);
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
                                            F.LogWarning("Invalid line, must match SDG.Unturned.ELimb enumerator list (LEFT|RIGHT)_(ARM|LEG|BACK|FOOT|FRONT|HAND), SPINE, SKULL. Line:\n" + p);
                                    }
                                    else
                                        F.LogWarning($"Error parsing limb translation in \".\\{Data.LangStorage}{directoryInfo.Name}\\{info.Name}\":\n{p}");
                                }
                            }
                            if (!limbloc.ContainsKey(directoryInfo.Name))
                                limbloc.Add(directoryInfo.Name, rtn);
                        }

                    }

                }
                F.Log($"Loaded {languages.Count} languages, default having {(languages.Count > 0 ? languages.ElementAt(0).Value.Count.ToString() : "NO_LANGS_FOUND")} translations.");
            } else
            {
                F.LogError("Failed to load translations, see above.");
                languages.Add(DefaultLanguage, DefaultTranslations);
                limbloc.Add(DefaultLanguage, DefaultLimbTranslations);
                deathloc.Add(DefaultLanguage, DefaultDeathTranslations);
                return languages;
            }
            return languages;
        }
        public static Dictionary<int, Zone> LoadExtraZones()
        {
            F.CheckDir(Data.FlagStorage, out bool madeDir);
            if(madeDir)
            {
                if (!File.Exists(Data.FlagStorage + "extra_zones.json"))
                {
                    using (StreamWriter TextWriter = File.CreateText(Data.FlagStorage + "extra_zones.json"))
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
                    foreach (FlagData zone in DefaultExtraZones)
                        NewDefaultZones.Add(zone.id, Flag.ComplexifyZone(zone));
                    return NewDefaultZones;
                }
                List<FlagData> Zones;
                using (StreamReader Reader = File.OpenText(Data.FlagStorage + "extra_zones.json"))
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
            } else
            {
                F.LogError("Failed to load extra zones, see above. Loading default zones.");
                Dictionary<int, Zone> NewDefaultZones = new Dictionary<int, Zone>();
                foreach (FlagData zone in DefaultExtraZones)
                    NewDefaultZones.Add(zone.id, Flag.ComplexifyZone(zone));
                return NewDefaultZones;
            }
        }
        public static Dictionary<string, Vector3> LoadExtraPoints()
        {
            F.CheckDir(Data.FlagStorage, out bool madeDirs);
            if(madeDirs)
            {
                if (!File.Exists(Data.FlagStorage + "extra_points.json"))
                {
                    using (StreamWriter TextWriter = File.CreateText(Data.FlagStorage + "extra_points.json"))
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
                using (StreamReader Reader = File.OpenText(Data.FlagStorage + "extra_points.json"))
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
            } else
            {
                F.LogError("Failed to load extra points, see above. Loading default points.");
                Dictionary<string, Vector3> NewDefaultPoints = new Dictionary<string, Vector3>();
                foreach (Point3D point in DefaultExtraPoints)
                    NewDefaultPoints.Add(point.name, point.Vector3);
                return NewDefaultPoints;
            }
            
        }
        public static Dictionary<string, MySqlTableLang> LoadTables()
        {
            if (!File.Exists(Data.DataDirectory + "tables.json"))
            {
                using (StreamWriter TextWriter = File.CreateText(Data.DataDirectory + "tables.json"))
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
            using (StreamReader Reader = File.OpenText(Data.DataDirectory + "tables.json"))
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
            if (!File.Exists(Data.DataDirectory + "node-calls.json"))
            {
                
                using (StreamWriter TextWriter = File.CreateText(Data.DataDirectory + "node-calls.json"))
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
            using (StreamReader Reader = File.OpenText(Data.DataDirectory + "node-calls.json"))
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
            if (!File.Exists(Data.LangStorage + "preferences.json"))
            {
                using (StreamWriter TextWriter = File.CreateText(Data.LangStorage + "preferences.json"))
                {
                    TextWriter.Write("[]");
                    TextWriter.Close();
                    TextWriter.Dispose();
                }
                return new Dictionary<ulong, string>();
            }
            List<LangData> Languages;
            using (StreamReader Reader = File.OpenText(Data.LangStorage + "preferences.json"))
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
            using (StreamWriter TextWriter = File.CreateText(Data.LangStorage + "preferences.json"))
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
            if(Data.Languages.ContainsKey(player))
            {
                Data.Languages[player] = language;
                SaveLangs(Data.Languages);
            } else
            {
                Data.Languages.Add(player, language);
                SaveLangs(Data.Languages);
            }
        }
        public static Dictionary<string, LanguageAliasSet> LoadLangAliases()
        {
            if (!File.Exists(Data.LangStorage + "aliases.json"))
            {

                using (StreamWriter TextWriter = File.CreateText(Data.LangStorage + "aliases.json"))
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
            using (StreamReader Reader = File.OpenText(Data.LangStorage + "aliases.json"))
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
