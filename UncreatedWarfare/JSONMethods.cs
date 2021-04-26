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
    public static class JSONMethods
    {
        public static readonly List<FlagData> DefaultFlags = new List<FlagData>
        {
            new FlagData(1, "AmmoHill", -83, 76, 280, 80, 80),
            new FlagData(2, "Hilltop", 239, 66, 460, 100, 100),
            new FlagData(3, "Papanov", 709, 76, 709, 130, 130),
            new FlagData(4, "Verto", 573, 45, 433, 120, 120),
            new FlagData(5, "Hill123", 593, 73, 121, 80, 80),
            new FlagData(6, "Hill13", 316, 68, -15, 90, 90),
            new FlagData(7, "Mining", 49, 41, -200, 110, 110)
        };
        public static readonly List<ColorData> DefaultColors = new List<ColorData>
        {
            new ColorData("default", "ffffff"),

            // Team Colors
            new ColorData("team_1_color", "4785ff"),
            new ColorData("team_2_color", "f53b3b"),
            new ColorData("neutral_color", "c2c2c2"),

            // Team 1 Circle
            new ColorData("capturing_team_1", "4785ff"),
            new ColorData("losing_team_1", "f53b3b"),
            new ColorData("clearing_team_1", "4785ff"),
            new ColorData("contested_team_1", "ffff1a"),
            new ColorData("secured_team_1", "00ff00"),
            new ColorData("nocap_team_1", "ff0000"),

            // Team 1 Background Circle
            new ColorData("capturing_team_1_bkgr", "002266"),
            new ColorData("losing_team_1_bkgr", "610505"),
            new ColorData("clearing_team_1_bkgr", "002266"),
            new ColorData("contested_team_1_bkgr", "666600"),
            new ColorData("secured_team_1_bkgr", "006600"),
            new ColorData("nocap_team_1_bkgr", "660000"),

            // Team 1 Words
            new ColorData("capturing_team_1_words", "4785ff"),
            new ColorData("losing_team_1_words", "f53b3b"),
            new ColorData("clearing_team_1_words", "4785ff"),
            new ColorData("contested_team_1_words", "ffff1a"),
            new ColorData("secured_team_1_words", "00ff00"),
            new ColorData("nocap_team_1_words", "ff0000"),
            new ColorData("entered_cap_radius", "e6e3d5"),

            // Team 2 Circle
            new ColorData("capturing_team_2", "f53b3b"),
            new ColorData("losing_team_2", "4785ff"),
            new ColorData("clearing_team_2", "f53b3b"),
            new ColorData("contested_team_2", "ffff1a"),
            new ColorData("secured_team_2", "00ff00"),
            new ColorData("nocap_team_2", "ff0000"),

            // Team 2 Background Circle
            new ColorData("capturing_team_2_bkgr", "610505"),
            new ColorData("losing_team_2_bkgr", "002266"),
            new ColorData("clearing_team_2_bkgr", "610505"),
            new ColorData("contested_team_2_bkgr", "666600"),
            new ColorData("secured_team_2_bkgr", "006600"),
            new ColorData("nocap_team_2_bkgr", "660000"),

            // Team 2 Words
            new ColorData("capturing_team_2_words", "f53b3b"),
            new ColorData("losing_team_2_words", "4785ff"),
            new ColorData("clearing_team_2_words", "f53b3b"),
            new ColorData("contested_team_2_words", "ffff1a"),
            new ColorData("secured_team_2_words", "00ff00"),
            new ColorData("nocap_team_2_words", "ff0000"),

            // Flag Chats
            new ColorData("entered_cap_radius_team_1", "e6e3d5"),
            new ColorData("entered_cap_radius_team_2", "e6e3d5"),
            new ColorData("left_cap_radius_team_1", "e6e3d5"),
            new ColorData("left_cap_radius_team_2", "e6e3d5"),

            // Team 1 Chat
            new ColorData("capturing_team_1_chat", "e6e3d5"),
            new ColorData("losing_team_1_chat", "e6e3d5"),
            new ColorData("clearing_team_1_chat", "e6e3d5"),
            new ColorData("contested_team_1_chat", "e6e3d5"),
            new ColorData("secured_team_1_chat", "e6e3d5"),
            new ColorData("nocap_team_1_chat", "e6e3d5"),

            // Team 2 Chat
            new ColorData("capturing_team_2_chat", "e6e3d5"),
            new ColorData("losing_team_2_chat", "e6e3d5"),
            new ColorData("clearing_team_2_chat", "e6e3d5"),
            new ColorData("contested_team_2_chat", "e6e3d5"),
            new ColorData("secured_team_2_chat", "e6e3d5"),
            new ColorData("nocap_team_2_chat", "e6e3d5"),

            // Other Flag Chats
            new ColorData("flag_neutralized", "e6e3d5")

        };
        public static readonly List<XPData> DefaultXPData = new List<XPData>
        {
            new XPData(EXPGainType.OFFENCE_KILL, 30),
            new XPData(EXPGainType.DEFENCE_KILL, 15),
            new XPData(EXPGainType.CAPTURE, 500),
            new XPData(EXPGainType.WIN, 800),
            new XPData(EXPGainType.CAPTURE_KILL, 25),
            new XPData(EXPGainType.KILL, 10),
            new XPData(EXPGainType.CAP_INCREASE, 30),
            new XPData(EXPGainType.HOLDING_POINT, 10)
        };
        public static readonly List<CreditsData> DefaultCreditData = new List<CreditsData>
        {
            new CreditsData(ECreditsGainType.CAPTURE, 250),
            new CreditsData(ECreditsGainType.WIN, 600)
        };
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
            return Flags ?? new List<FlagData>();
        }
        public static void SaveFlags(this List<FlagData> Flags, string Preset)
        {
            using(StreamWriter TextWriter = File.CreateText(UCWarfare.FlagStorage + Preset + ".json"))
            {
                using (JsonWriter JsonWriter = new JsonTextWriter(TextWriter))
                {
                    JsonSerializer Serializer = new JsonSerializer();
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
            Dictionary<EXPGainType, int> NewCredits = new Dictionary<EXPGainType, int>();
            foreach (XPData data in XPs ?? DefaultXPData)
            {
                try
                {
                    NewCredits.Add((EXPGainType)Enum.Parse(typeof(EXPGainType), data.key), data.xp);
                } catch
                {
                    CommandWindow.LogError(data.key + " is not a valid value for XP type");
                }
            }
            return NewCredits;
        }
    }
}
