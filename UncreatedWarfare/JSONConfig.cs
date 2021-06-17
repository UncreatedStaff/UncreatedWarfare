using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare
{
    public class Config<TData> where TData : ConfigData, new()
    {
        public readonly string directory;
        public TData data { get; private set; }

        public Config(string directory)
        {
            this.directory = directory;

            if (!File.Exists(directory))
                LoadDefaults();
            else
                Reload();
        }
        public void Save()
        {
            StreamWriter file = File.CreateText(directory);
            JsonWriter writer = new JsonTextWriter(file);
            JsonSerializer serializer = new JsonSerializer() { Formatting = Formatting.Indented };
            try
            {
                serializer.Serialize(writer, data);
                writer.Close();
            }
            catch (Exception ex)
            {
                writer.Close();
                throw ex;
            }
        }
        public void Reload()
        {
            StreamReader r = File.OpenText(directory);
            try
            {
                string json = r.ReadToEnd();
                data = JsonConvert.DeserializeObject<TData>(json);

                r.Close();
                r.Dispose();
            }
            catch (Exception ex)
            {
                if (r != default)
                {
                    r.Close();
                    r.Dispose();
                }
                throw new JSONSaver<TData>.JSONReadException(r, directory, ex);
            }
        }
        public void LoadDefaults()
        {
            StreamWriter file = File.CreateText(directory);
            JsonWriter writer = new JsonTextWriter(file);
            JsonSerializer serializer = new JsonSerializer() { Formatting = Formatting.Indented };
            try
            {
                data = new TData();
                data.SetDefaults();

                serializer.Serialize(writer, data);
                writer.Close();
            }
            catch (Exception ex)
            {
                writer.Close();
                throw ex;
            }
        }
    }

    public abstract class ConfigData
    {
        public ConfigData() { }
        public abstract void SetDefaults();
    }
}
