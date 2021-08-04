using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Vehicles;

namespace Uncreated
{
    public abstract class JSONSaver<T> where T : new()
    {
        protected static string directory;
        public static readonly Type Type = typeof(T);
        private static readonly FieldInfo[] fields = Type.GetFields();
        public static List<T> ActiveObjects = new List<T>();
        public JSONSaver(string _directory)
        {
            directory = _directory;
            CreateFileIfNotExists(LoadDefaults());
            Reload();
            TryUpgrade();
        }
        public static void Save() => OverwriteSavedList(ActiveObjects);
        public static void Reload() => ActiveObjects = GetExistingObjects();
        public static T ReloadSingle(string defaults, Func<T> ifFail)
        {
            Reload();
            if (ActiveObjects.Count > 0) return ActiveObjects[0];
            else
            {
                CreateFileIfNotExists(defaults);
                Reload();
                if (ActiveObjects.Count > 0) return ActiveObjects[0];
                else 
                {
                    return ifFail.Invoke();
                }
            }
        }
        protected abstract string LoadDefaults();
        protected static void CreateFileIfNotExists(string text = "[]")
        {
            if (!File.Exists(directory))
            {
                StreamWriter creator = File.CreateText(directory);
                creator.WriteLine(text);
                creator.Close();
                creator.Dispose();
            }
        }
        protected static T AddObjectToSave(T item, bool save = true)
        {
            if (item.Equals(default)) return default;
            ActiveObjects.Add(item);
            if (save) Save();
            return item;
        }
        protected static void RemoveWhere(Predicate<T> match, bool save = true)
        {
            if (match == default) return;
            ActiveObjects.RemoveAll(match);
            if(save) Save();
        }
        protected static void RemoveAllObjectsFromSave(bool save = true)
        {
            ActiveObjects.Clear();
            if (save) Save();
        }
        protected static void OverwriteSavedList(List<T> newList)
        {
            ActiveObjects = newList;
            StreamWriter file = File.CreateText(directory);
            JsonWriter writer = new JsonTextWriter(file);
            JsonSerializer serializer = new JsonSerializer() { Formatting = Formatting.Indented, Culture = Data.Locale };
            try
            {
                serializer.Serialize(writer, newList);
                writer.Close();
            }
            catch (Exception ex)
            {
                writer.Close();
                throw ex;
            }
        }
        protected static List<T> GetExistingObjects(bool readFile = false)
        {
            if(readFile || ActiveObjects == default || ActiveObjects.Count == 0)
            {
                StreamReader r = File.OpenText(directory);
                try
                {
                    string json = r.ReadToEnd();
                    var list = JsonConvert.DeserializeObject<List<T>>(json, new JsonSerializerSettings() { Culture = Data.Locale });

                    r.Close();
                    r.Dispose();
                    return list;
                }
                catch (Exception ex)
                {
                    if (r != default)
                    {
                        r.Close();
                        r.Dispose();
                    }
                    throw new JSONReadException(r, directory, ex);
                }
            } else return ActiveObjects;
        }
        protected static List<T> GetObjectsWhere(Func<T, bool> predicate, bool readFile = false) => GetExistingObjects(readFile).Where(predicate).ToList();
        protected static T GetObject(Func<T, bool> predicate, bool readFile = false) => GetExistingObjects(readFile).FirstOrDefault(predicate);
        protected static bool ObjectExists(Func<T, bool> match, out T item, bool readFile = false)
        {
            item = GetObject(match);
            return item != null;
        }
        /// <summary>reason [ 0: success, 1: no field, 2: invalid field, 3: non-saveable property ]</summary>
        private static FieldInfo GetField(string property, out byte reason)
        {
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].Name == property) // case sensitive search
                {
                    if (ValidateField(fields[i], out reason))
                    {
                        return fields[i];
                    }
                }
            }
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].Name.ToLower() == property.ToLower()) // case insensitive search if case sensitive search netted no results
                {
                    if (ValidateField(fields[i], out reason))
                    {
                        return fields[i];
                    }
                }
            }
            reason = 1;
            return default;
        }
        private static object ParseInput(string input, Type type, out bool parsed)
        {
            if(input == default || type == default)
            {
                parsed = false;
                return default;
            }
            if(type == typeof(object))
            {
                parsed = true;
                return input;
            }
            if(type == typeof(string)) 
            {
                parsed = true;
                return input;
            }
            if(type == typeof(bool))
            {
                string lowercase = input.ToLower();
                if (lowercase == "true")
                {
                    parsed = true;
                    return true;
                } else if (lowercase == "false")
                {
                    parsed = true;
                    return false;
                } else
                {
                    parsed = false;
                    return default;
                }
            }
            if(type == typeof(char))
            {
                if(input.Length == 1)
                {
                    parsed = true;
                    return input[0];
                }
            }
            if (type.IsEnum)
            {
                try
                {
                    object output = Enum.Parse(type, input, true);
                    if (output == default)
                    {
                        parsed = false;
                        return default;
                    }
                    parsed = true;
                    return output;
                }
                catch (ArgumentNullException)
                {
                    parsed = false;
                    return default;
                }
                catch (ArgumentException)
                {
                    parsed = false;
                    return default;
                }
            }
            if (!type.IsPrimitive)
            {
                F.LogError("Can not parse non-primitive types except for strings and enums.");
                parsed = false;
                return default;
            }

            if (type == typeof(int))
            {
                if (int.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out int result))
                {
                    parsed = true;
                    return result;
                }
            } 
            else if (type == typeof(ushort))
            {
                if (ushort.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out ushort result))
                {
                    parsed = true;
                    return result;
                }
            } 
            else if (type == typeof(ulong))
            {
                if (ulong.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out ulong result))
                {
                    parsed = true;
                    return result;
                }
            }
            else if (type == typeof(float))
            {
                if (float.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out float result))
                {
                    parsed = true;
                    return result;
                }
            }
            else if (type == typeof(decimal))
            {
                if (decimal.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out decimal result))
                {
                    parsed = true;
                    return result;
                }
            }
            else if (type == typeof(double))
            {
                if (double.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out double result))
                {
                    parsed = true;
                    return result;
                }
            }
            else if (type == typeof(byte))
            {
                if(byte.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out byte result))
                {
                    parsed = true;
                    return result;
                }
            } 
            else if (type == typeof(sbyte))
            {
                if (sbyte.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out sbyte result))
                {
                    parsed = true;
                    return result;
                }
            } 
            else if (type == typeof(short))
            {
                if (short.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out short result))
                {
                    parsed = true;
                    return result;
                }
            } 
            else if (type == typeof(uint))
            {
                if (uint.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out uint result))
                {
                    parsed = true;
                    return result;
                }
            } 
            else if (type == typeof(long))
            {
                if (long.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out long result))
                {
                    parsed = true;
                    return result;
                }
            }
            parsed = false;
            return default;
        }
        /// <summary>Fields must be instanced, non-readonly, and have the <see cref="JsonSettable"/> attribute to be set.</summary>
        public static T SetProperty(T obj, string property, string value, out bool set, out bool parsed, out bool found, out bool allowedToChange)
        {
            FieldInfo field = GetField(property, out byte reason);
            if(reason != 0)
            {
                if(reason == 1 || reason == 2)
                {
                    set = false;
                    parsed = false;
                    found = false;
                    allowedToChange = false;
                    return obj;
                }
                else if (reason == 3)
                {
                    set = false;
                    parsed = false;
                    found = true;
                    allowedToChange = false;
                    return obj;
                }
            }
            found = true;
            allowedToChange = true;
            object parsedValue = ParseInput(value, field.FieldType, out parsed);
            if(parsed)
            {
                if (field != default)
                {
                    try
                    {
                        field.SetValue(obj, parsedValue);
                        set = true;
                        Save();
                        return obj;
                    }
                    catch (FieldAccessException ex)
                    {
                        F.LogError(ex);
                        set = false;
                        return obj;
                    }
                    catch (TargetException ex)
                    {
                        F.LogError(ex);
                        set = false;
                        return obj;
                    }
                    catch (ArgumentException ex)
                    {
                        F.LogError(ex);
                        set = false;
                        return obj;
                    }
                }
                else
                {
                    set = false;
                    return obj;
                }
            } else
            {
                set = false;
                return obj;
            }
        }
        /// <summary>reason [ 0: success, 1: no field, 2: invalid field, 3: non-saveable property ]</summary>
        private static bool ValidateField(FieldInfo field, out byte reason)
        {
            if (field == default)
            {
                F.LogError(Type.Name + " saver: field not found.");
                reason = 1;
                return false;
            }
            if (field.IsStatic)
            {
                F.LogError(Type.Name + " saver tried to save to a static property.");
                reason = 2;
                return false;
            }
            if (field.IsInitOnly)
            {
                F.LogError(Type.Name + " saver tried to save to a readonly property.");
                reason = 2;
                return false;
            }
            IEnumerator<CustomAttributeData> attributes = field.CustomAttributes.GetEnumerator();
            bool settable = false;
            while (attributes.MoveNext())
            {
                if (attributes.Current.AttributeType == typeof(JsonSettable))
                {
                    settable = true;
                    break;
                }
            }
            attributes.Dispose();
            if (!settable)
            {
                F.LogError(Type.Name + " saver tried to save to a non json-savable property.");
                reason = 3;
                return false;
            }
            reason = 0;
            return true;
        }
        /// <summary>Fields must be instanced, non-readonly, and have the <see cref="JsonSettable"/> attribute to be set.</summary>
        public static bool SetProperty(Func<T, bool> selector, string property, string value, out bool foundObject, out bool setSuccessfully, out bool parsed, out bool found, out bool allowedToChange)
        {
            if(ObjectExists(selector, out T selected))
            {
                foundObject = true;
                SetProperty(selected, property, value, out setSuccessfully, out parsed, out found, out allowedToChange);
                return setSuccessfully;
            } else
            {
                foundObject = false;
                setSuccessfully = false;
                parsed = false;
                found = false;
                allowedToChange = false;
                return false;
            }
        }
        public static bool SetProperty<V>(Func<T, bool> selector, string property, V value, out bool foundObject, out bool setSuccessfully, out bool foundproperty, out bool allowedToChange)
        {
            if (ObjectExists(selector, out T selected))
            {
                foundObject = true;
                SetProperty(selected, property, value, out setSuccessfully, out foundproperty, out allowedToChange);
                return setSuccessfully;
            }
            else
            {
                foundObject = false;
                setSuccessfully = false;
                foundproperty = false;
                allowedToChange = false;
                return false;
            }
        }
        public static T SetProperty<V>(T obj, string property, V value, out bool success, out bool found, out bool allowedToChange)
        {
            FieldInfo field = GetField(property, out byte reason);
            if (reason != 0)
            {
                if (reason == 1 || reason == 2)
                {
                    found = false;
                    allowedToChange = false;
                    success = false;
                    return obj;
                }
                else if (reason == 3)
                { 
                    found = true;
                    allowedToChange = false;
                    success = false;
                    return obj;
                }
            }
            found = true;
            allowedToChange = true;
            if (field != default)
            {
                if (field.FieldType.IsAssignableFrom(typeof(V)))
                {
                    try
                    {
                        field.SetValue(obj, value);
                        success = true;
                        Save();
                        return obj;
                    }
                    catch (FieldAccessException ex)
                    {
                        F.LogError(ex);
                        success = false;
                        return obj;
                    }
                    catch (TargetException ex)
                    {
                        F.LogError(ex);
                        success = false;
                        return obj;
                    }
                    catch (ArgumentException ex)
                    {
                        F.LogError(ex);
                        success = false;
                        return obj;
                    }
                } else
                {
                    success = false;
                    return obj;
                }
            }
            else
            {
                success = false;
                return obj;
            }
        }
        public static void UpdateObjectsWhere(Func<T, bool> selector, Action<T> operation, bool save = true)
        {
            IEnumerator<T> results = ActiveObjects.Where(selector).GetEnumerator();
            while(results.MoveNext())
                operation.Invoke(results.Current);
            results.Dispose();
            if (save) Save();
        }
        public static void WriteSingleObject(T item)
        {
            StreamWriter file = File.CreateText(directory);
            JsonWriter writer = new JsonTextWriter(file);
            JsonSerializer serializer = new JsonSerializer() { Formatting = Formatting.Indented };
            try
            {
                serializer.Serialize(writer, new List<T>() { item });
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
        public static bool IsPropertyValid<TEnum>(object name, out TEnum property) where TEnum : struct, Enum
        {
            if (Enum.TryParse<TEnum>(name.ToString(), out var p))
            {
                property = p;
                return true;
            }
            property = p;
            return false;
        }
        public void TryUpgrade()
        {
            if (ActiveObjects == default) throw new NullReferenceException("Error upgrading in JsonSaver: Not yet loaded.");
            try
            {
                bool needsSaving = false;
                for (int t = 0; t < ActiveObjects.Count; t++)
                {
                    T defaultConfig = new T();
                    if (ActiveObjects[t] == null)
                    {
                        ActiveObjects[t] = defaultConfig;
                        needsSaving = true;
                        continue;
                    }
                    FieldInfo[] fields = Type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (fields[i].IsStatic ||  // if the field is static or it contains [JsonIgnore] in its attributes.
                            fields[i].CustomAttributes.Count(x => x.AttributeType == typeof(JsonIgnoreAttribute)) > 0) continue;
                        object currentvalue = fields[i].GetValue(ActiveObjects[t]);
                        object defaultvalue = fields[i].GetValue(defaultConfig);
                        if (currentvalue == defaultvalue) continue;
                        else if (currentvalue != fields[i].FieldType.getDefaultValue()) continue;
                        else
                        {
                            fields[i].SetValue(ActiveObjects[t], defaultvalue);
                            needsSaving = true;
                        }
                    }
                }
                if (needsSaving) Save();
            }
            catch (Exception ex)
            {
                F.LogError("Error upgrading in JsonSaver:");
                F.LogError(ex);
            }
        }
        protected class TypeArgumentException : Exception
        {
            public TypeArgumentException() { }

            public TypeArgumentException(MethodBase currentMethod, object badObject, Exception inner)
                : base(string.Format("Arguments of {0} should match the JSONSaver's specified type: {1}. The object you gave it was of type: {2}", 
                    currentMethod.Name, typeof(T).Name, badObject.GetType().Name), inner)
            {
                
            }
        }
        public class JSONReadException : Exception
        {
            public JSONReadException() { }

            public JSONReadException(StreamReader reader, string directory, Exception inner)
                : base(string.Format("Could not deserialize data from {0} because the data was corrupted.", directory), inner)
            {
                reader.Close();
                reader.Dispose();
            }
        }
    }
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class JsonSettable : Attribute
    {
        public JsonSettable() { }
    }
}
