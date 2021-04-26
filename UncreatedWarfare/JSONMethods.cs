using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncreatedWarfare.Flags;
using System.IO;
using UnityEngine;

namespace UncreatedWarfare
{
    public class ColorData
    {
        public string key;
        public string color_hex;
        [JsonIgnore]
        public Color color { get => color_hex.Hex(); }
        [JsonConstructor]
        public ColorData(string key, string color_hex)
        {
            this.key = key;
            this.color_hex = color_hex;
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
            new ColorData("capturing_team_1", "f53b3b"),
            new ColorData("losing_team_1", "4785ff"),
            new ColorData("clearing_team_1", "f53b3b"),
            new ColorData("contested_team_1", "ffff1a"),
            new ColorData("secured_team_1", "00ff00"),
            new ColorData("nocap_team_1", "ff0000"),

            // Team 2 Background Circle
            new ColorData("capturing_team_1_bkgr", "610505"),
            new ColorData("losing_team_1_bkgr", "002266"),
            new ColorData("clearing_team_1_bkgr", "610505"),
            new ColorData("contested_team_1_bkgr", "666600"),
            new ColorData("secured_team_1_bkgr", "006600"),
            new ColorData("nocap_team_1_bkgr", "660000"),

            // Team 2 Words
            new ColorData("capturing_team_1_words", "f53b3b"),
            new ColorData("losing_team_1_words", "4785ff"),
            new ColorData("clearing_team_1_words", "f53b3b"),
            new ColorData("contested_team_1_words", "ffff1a"),
            new ColorData("secured_team_1_words", "00ff00"),
            new ColorData("nocap_team_1_words", "ff0000"),

            // Flag Chats
            new ColorData("entered_cap_radius", "e6e3d5"),

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
                    NewDefaults.Add(data.key, data.color);
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
            if (Colors == null)
            {
                foreach (ColorData data in DefaultColors)
                {
                    NewColors.Add(data.key, data.color);
                    NewColorsHex.Add(data.key, data.color_hex);
                }
            } else
            {
                foreach (ColorData data in Colors)
                {
                    NewColors.Add(data.key, data.color);
                    NewColorsHex.Add(data.key, data.color_hex);
                }
            }
            HexValues = NewColorsHex;
            return NewColors;
        }
    }
}
