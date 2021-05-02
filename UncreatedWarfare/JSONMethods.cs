﻿using Newtonsoft.Json;
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

namespace UncreatedWarfare
{
    public class ColorData
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
    public class TeamData
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
    public class XPData
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
    public class CreditsData
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
    public class Translation
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
    public static partial class JSONMethods
    {
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
        public static Dictionary<string, string> LoadTranslations(string language = "en-us")
        {
            if (!File.Exists(UCWarfare.LangStorage + language + ".json"))
            {
                using (StreamWriter TextWriter = File.CreateText(UCWarfare.LangStorage + language + ".json"))
                {
                    using (JsonWriter JsonWriter = new JsonTextWriter(TextWriter))
                    {
                        JsonSerializer Serializer = new JsonSerializer();
                        Serializer.Formatting = Formatting.Indented;
                        List<Translation> t = new List<Translation>();
                        foreach(KeyValuePair<string, string> translation in DefaultTranslations)
                        {
                            t.Add(new Translation(translation.Key, translation.Value));
                        }
                        Serializer.Serialize(JsonWriter, t);
                        JsonWriter.Close();
                        TextWriter.Close();
                        TextWriter.Dispose();
                    }
                }
                return DefaultTranslations;
            }
            List<Translation> Translations;
            using (StreamReader Reader = File.OpenText(UCWarfare.LangStorage + language + ".json"))
            {
                Translations = JsonConvert.DeserializeObject<List<Translation>>(Reader.ReadToEnd());
                Reader.Close();
                Reader.Dispose();
            }
            if(Translations == null) return DefaultTranslations;
            Dictionary<string, string> translationDict = new Dictionary<string, string>();
            foreach (Translation data in Translations)
            {
                translationDict.Add(data.key, data.value);
            }
            return translationDict;
        }
        public static List<TeamData> ReadTeams()
        {
            if (!File.Exists(UCWarfare.DataDirectory + "teams.json"))
            {
                SaveTeams(DefaultTeamData);
                return DefaultTeamData;
            }
            List<TeamData> Teams;
            using (StreamReader Reader = File.OpenText(UCWarfare.DataDirectory + "teams.json"))
            {
                Teams = JsonConvert.DeserializeObject<List<TeamData>>(Reader.ReadToEnd());
                Reader.Close();
                Reader.Dispose();
            }
            return Teams ?? DefaultTeamData;
        }
        public static Dictionary<int, Zone> ReadExtraZones()
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
        public static void SaveTeams(List<TeamData> Teams)
        {
            using (StreamWriter TextWriter = File.CreateText(UCWarfare.DataDirectory + "teams.json"))
            {
                using (JsonWriter JsonWriter = new JsonTextWriter(TextWriter))
                {
                    JsonSerializer Serializer = new JsonSerializer();
                    Serializer.Formatting = Formatting.Indented;
                    Serializer.Serialize(JsonWriter, Teams);
                    JsonWriter.Close();
                    TextWriter.Close();
                    TextWriter.Dispose();
                }
            }
        }
        public static void AddTeam(TeamData Team)
        {
            List<TeamData> data = ReadTeams();
            data.Add(Team);
            UCWarfare.I.TeamManager.Teams.Add(new TeamOld(Team));
            SaveTeams(data);
        }
        public static bool RenameTeam(ulong teamID, string newName, out string oldName)
        {
            List<TeamData> data = ReadTeams();
            int team = data.FindIndex(t => t.team_id == teamID);
            if (team != -1)
            {
                oldName = data[team].name;
                data[team].name = newName;
                SaveTeams(data);
                return true;
            }
            else
            {
                oldName = "FAILURE";
                return false;
            }
        }
        public static bool DeleteTeam(ulong teamID, out TeamData teamRemoved)
        {
            List<TeamData> data = ReadTeams();
            int team = data.FindIndex(t => t.team_id == teamID);
            if (team != -1)
            {
                teamRemoved = data[team];
                data.RemoveAt(team);
                SaveTeams(data);
                return true;
            }
            else
            {
                teamRemoved = null;
                return false;
            }
        }
        public static bool AddPlayerToTeam(ulong teamID, ulong playerID)
        {
            List<TeamData> data = ReadTeams();
            int team = data.FindIndex(t => t.team_id == teamID);
            if (team != -1)
            {
                if(!data[team].players.Contains(playerID))
                {
                    data[team].players.Add(playerID);
                    SaveTeams(data);
                }
                return true;
            }
            else return false;
        }
        public static bool RemovePlayerFromTeam(ulong teamID, ulong playerID)
        {
            List<TeamData> data = ReadTeams();
            int team = data.FindIndex(t => t.team_id == teamID);
            if (team != -1)
            {
                if (data[team].players.Contains(playerID))
                {
                    data[team].players.Remove(playerID);
                    SaveTeams(data);
                }
                return true;
            }
            else return false;
        }
    }
}
