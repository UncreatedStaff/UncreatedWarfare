using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Uncreated.Warfare;

namespace Uncreated
{
    public class Config<TData> : IConfiguration where TData : ConfigData, new()
    {
        readonly string _dir;
        public TData Data { get; private set; }
        public Type Type = typeof(TData);
        public string Directory => _dir;
        public Config(string directory, string filename)
        {
            if (!System.IO.Directory.Exists(directory))
                System.IO.Directory.CreateDirectory(directory);
            this._dir = directory + filename;

            if (!File.Exists(this._dir))
                LoadDefaults();
            else
                Reload();
        }
        public void Save()
        {
            StreamWriter file = File.CreateText(_dir);
            JsonWriter writer = new JsonTextWriter(file);
            JsonSerializer serializer = new JsonSerializer() { Formatting = Formatting.Indented };
            try
            {
                serializer.Serialize(writer, Data);
                writer.Close();
                file.Close();
                file.Dispose();
            }
            catch (Exception ex)
            {
                writer.Close();
                file.Close();
                file.Dispose();
                throw ex;
            }
        }
        public void Reload()
        {
            if (!File.Exists(this._dir))
            {
                LoadDefaults();
                return;
            }
            StreamReader r = File.OpenText(_dir);
            try
            {
                string json = r.ReadToEnd();
                Data = JsonConvert.DeserializeObject<TData>(json);

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
                throw new JSONSaver<TData>.JSONReadException(r, _dir, ex);
            }
        }
        public void TryUpgrade()
        {
            try
            {
                TData defaultConfig = new TData();
                defaultConfig.SetDefaults();
                FieldInfo[] fields = Type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                bool needsSaving = false;
                for (int i = 0; i < fields.Length; i++)
                {
                    try
                    {
                        if (fields[i].IsStatic ||  // if the field is static or it contains [JsonIgnore] in its attributes.
                            fields[i].CustomAttributes.Count(x => x.AttributeType == typeof(JsonIgnoreAttribute) ||
                            x.AttributeType == typeof(PreventUpgradeAttribute)) > 0) continue;
                        object currentvalue = fields[i].GetValue(Data);
                        object defaultvalue = fields[i].GetValue(defaultConfig);
                        if (currentvalue == defaultvalue) continue;
                        else if (currentvalue != fields[i].FieldType.getDefaultValue()) continue;
                        else
                        {
                            fields[i].SetValue(Data, defaultvalue);
                            needsSaving = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        L.LogError($"Error upgrading config for field {fields[i].FieldType.Name}::{fields[i].Name} in {Type.Name} config.");
                        L.LogError(ex);
                    }
                }
                if (needsSaving) Save();
            }
            catch (Exception ex)
            {
                L.LogError("Error upgrading config: " + Type.Name);
                L.LogError(ex);
            }
        }
        public void LoadDefaults()
        {
            StreamWriter file = File.CreateText(_dir);
            JsonWriter writer = new JsonTextWriter(file);
            JsonSerializer serializer = new JsonSerializer() { Formatting = Formatting.Indented };
            try
            {
                Data = new TData();
                Data.SetDefaults();

                serializer.Serialize(writer, Data);
                writer.Close();
                file.Close();
                file.Dispose();
            }
            catch (Exception ex)
            {
                writer.Close();
                file.Close();
                file.Dispose();
                throw ex;
            }
        }
    }

    public abstract class ConfigData
    {
        public ConfigData() => SetDefaults();
        public abstract void SetDefaults();
    }
    public interface IConfiguration
    {
        string Directory { get; }
        void LoadDefaults();
        void Reload();
        void Save();
    }
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class PreventUpgradeAttribute : Attribute
    {
        public PreventUpgradeAttribute() { }
    }
}
