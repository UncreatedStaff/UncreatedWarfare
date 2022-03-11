using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Uncreated.Warfare;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace Uncreated
{
    public abstract class JSONSaver<T> : List<T> where T : new()
    {
        protected static string directory;
        public static readonly Type Type = typeof(T);
        private static readonly FieldInfo[] fields = Type.GetFields();
        private static readonly SemaphoreSlim _threadLocker = new SemaphoreSlim(1, 5);

        public static JSONSaver<T> ActiveObjects;
        //public static List<T> ActiveObjects = new List<T>();
        private static CustomSerializer _serializer;
        private static bool useSerializer;
        private static CustomDeserializer _deserializer;
        private static bool useDeserializer;
        public delegate void CustomSerializer(T obj, Utf8JsonWriter writer);
        public delegate T CustomDeserializer(ref Utf8JsonReader reader);

        public JSONSaver(string _directory) : base()
        {
            ActiveObjects = this;
            directory = _directory;
            useSerializer = false;
            useDeserializer = false;
            CreateFileIfNotExists(LoadDefaults());
            Reload();
            TryUpgrade();
        }

        public JSONSaver(string _directory, CustomSerializer serializer, CustomDeserializer deserializer) : base()
        {
            ActiveObjects = this;
            directory = _directory;
            _serializer = serializer;
            useSerializer = serializer != null;
            _deserializer = deserializer;
            useDeserializer = deserializer != null;
            CreateFileIfNotExists(LoadDefaults());
            Reload();
            TryUpgrade();
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
            if (item == null) throw new ArgumentNullException(nameof(item));
            ActiveObjects.Add(item);
            if (save) Save();
            return item;
        }
        protected static void RemoveWhere(Predicate<T> match, bool save = true)
        {
            if (match == default) return;
            ActiveObjects.RemoveAll(match);
            if (save) Save();
        }
        protected static void RemoveAllObjectsFromSave(bool save = true)
        {
            ActiveObjects.Clear();
            if (save) Save();
        }
        //private static readonly JsonSerializer serializer = new JsonSerializer() { Formatting = Formatting.Indented, Culture = Data.Locale };
        
        public static void Save()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking("JsonSaver Save -> " + directory);
#endif
            _threadLocker.Wait();
            if (useSerializer)
            {
                try
                {
                    if (!File.Exists(directory))
                        File.Create(directory)?.Close();

                    using (FileStream rs = new FileStream(directory, FileMode.Truncate, FileAccess.Write, FileShare.None))
                    {
                        Utf8JsonWriter writer = new Utf8JsonWriter(rs, JsonEx.writerOptions);
                        writer.WriteStartArray();
                        for (int i = 0; i < ActiveObjects.Count; i++)
                        {
                            writer.WriteStartObject();
                            _serializer.Invoke(ActiveObjects[i], writer);
                            writer.WriteEndObject();
                        }

                        writer.WriteEndArray();
                        writer.Dispose();
                        rs.Close();
                        rs.Dispose();
                    }
                    _threadLocker.Release();
                    return;
                }
                catch (Exception ex)
                {
                    L.LogError("Failed to run custom serializer for " + typeof(T).Name);
                    L.LogError(ex);
                }
            }
            try
            {
                using (StreamWriter file = File.CreateText(directory))
                {
                    file.Write(JsonSerializer.Serialize(ActiveObjects as List<T>, JsonEx.serializerSettings));
                }
            }
            catch (Exception ex)
            {
                L.LogError("Failed to run automatic serializer for " + typeof(T).Name);
                L.LogError(ex);
            }
            _threadLocker.Release();
        }
        public static void Reload()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking("JsonSaver Reload -> " + directory);
#endif
            _threadLocker.Wait();
            if (!File.Exists(directory))
                CreateFileIfNotExists(ActiveObjects.LoadDefaults());
            if (useDeserializer)
            {
                FileStream? rs = null;
                try
                {
                    using (rs = new FileStream(directory, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        long len = rs.Length;
                        if (len > int.MaxValue)
                        {
                            L.LogError("File " + directory + " is too large.");
                            return;
                        }
                        byte[] buffer = new byte[len];
                        rs.Read(buffer, 0, (int)len);
                        Utf8JsonReader reader = new Utf8JsonReader(buffer.AsSpan(), JsonEx.readerOptions);
                        if (reader.Read() && reader.TokenType == JsonTokenType.StartArray)
                        {
                            ActiveObjects.Clear();
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                            {
                                if (reader.TokenType == JsonTokenType.StartObject)
                                {
                                    T next = _deserializer.Invoke(ref reader);
                                    ActiveObjects.Add(next);
                                    while (reader.TokenType != JsonTokenType.EndObject && reader.Read()) ;
                                }
                            }
                        }
                        rs.Close();
                        rs.Dispose();
                    }
                    _threadLocker.Release();
                    return;
                }
                catch (Exception e)
                {
                    L.LogError("Failed to run custom deserializer for " + Type.Name);
                    L.LogError(e);
                    if (rs != null)
                    {
                        rs.Close();
                        rs.Dispose();
                    }
                }
            }
            bool clsd = false;
            StreamReader? r = null;
            try
            {
                r = File.OpenText(directory);
                string json = r.ReadToEnd();
                r.Close();
                r.Dispose();
                clsd = true;
                _threadLocker.Release();
                T[]? vals = JsonSerializer.Deserialize<T[]>(json, JsonEx.serializerSettings);
                if (vals != null)
                {
                    ActiveObjects.Clear();
                    ActiveObjects.AddRange(vals);
                }
            }
            catch (Exception ex)
            {
                if (r != null && !clsd)
                {
                    r.Close();
                    r.Dispose();
                }

                L.LogError("Failed to auto-deserialize " + Type.Name);
                L.LogError(ex);
                _threadLocker.Release();
            }
        }
        protected static List<T> GetObjectsWhereAsList(Func<T, bool> predicate) => ActiveObjects.Where(predicate).ToList();
        protected static IEnumerable<T> GetObjectsWhere(Func<T, bool> predicate) => ActiveObjects.Where(predicate);
        protected static T GetObject(Func<T, bool> predicate, bool readFile = false) => ActiveObjects.FirstOrDefault(predicate);
        protected static bool ObjectExists(Func<T, bool> match, out T item)
        {
            item = GetObject(match);
            return item != null;
        }
        /// <summary>reason [ 0: success, 1: no field, 2: invalid field, 3: non-saveable property ]</summary>
        private static FieldInfo? GetField(string property, out byte reason)
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
        private static object? ParseInput(string input, Type type, out bool parsed)
        {
            if (input == default || type == default)
            {
                parsed = false;
                return default;
            }
            if (type == typeof(object))
            {
                parsed = true;
                return input;
            }
            if (type == typeof(string))
            {
                parsed = true;
                return input;
            }
            if (type == typeof(bool))
            {
                string lowercase = input.ToLower();
                if (lowercase == "true")
                {
                    parsed = true;
                    return true;
                }
                else if (lowercase == "false")
                {
                    parsed = true;
                    return false;
                }
                else
                {
                    parsed = false;
                    return default;
                }
            }
            if (type == typeof(char))
            {
                if (input.Length == 1)
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
                L.LogError("Can not parse non-primitive types except for strings and enums.");
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
                if (byte.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out byte result))
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
            FieldInfo? field = GetField(property, out byte reason);
            if (reason != 0)
            {
                if (reason == 1 || reason == 2)
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
            object? parsedValue;
            if (field == null)
            {
                parsed = false;
                parsedValue = null;
            }
            else parsedValue = ParseInput(value, field.FieldType, out parsed);
            if (parsed)
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
                        L.LogError(ex);
                        set = false;
                        return obj;
                    }
                    catch (TargetException ex)
                    {
                        L.LogError(ex);
                        set = false;
                        return obj;
                    }
                    catch (ArgumentException ex)
                    {
                        L.LogError(ex);
                        set = false;
                        return obj;
                    }
                }
                else
                {
                    set = false;
                    return obj;
                }
            }
            else
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
                L.LogError(Type.Name + " saver: field not found.");
                reason = 1;
                return false;
            }
            if (field.IsStatic)
            {
                L.LogError(Type.Name + " saver tried to save to a static property.");
                reason = 2;
                return false;
            }
            if (field.IsInitOnly)
            {
                L.LogError(Type.Name + " saver tried to save to a readonly property.");
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
                L.LogError(Type.Name + " saver tried to save to a non json-savable property.");
                reason = 3;
                return false;
            }
            reason = 0;
            return true;
        }
        /// <summary>Fields must be instanced, non-readonly, and have the <see cref="JsonSettable"/> attribute to be set.</summary>
        public static bool SetProperty(Func<T, bool> selector, string property, string value, out bool foundObject, out bool setSuccessfully, out bool parsed, out bool found, out bool allowedToChange)
        {
            if (ObjectExists(selector, out T selected))
            {
                foundObject = true;
                SetProperty(selected, property, value, out setSuccessfully, out parsed, out found, out allowedToChange);
                return setSuccessfully;
            }
            else
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
            FieldInfo? field = GetField(property, out byte reason);
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
                        L.LogError(ex);
                        success = false;
                        return obj;
                    }
                    catch (TargetException ex)
                    {
                        L.LogError(ex);
                        success = false;
                        return obj;
                    }
                    catch (ArgumentException ex)
                    {
                        L.LogError(ex);
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
            else
            {
                success = false;
                return obj;
            }
        }
        public static void UpdateObjectsWhere(Func<T, bool> selector, Action<T> operation, bool save = true)
        {
            IEnumerator<T> results = ActiveObjects.Where(selector).GetEnumerator();
            while (results.MoveNext())
                operation.Invoke(results.Current);
            results.Dispose();
            if (save) Save();
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
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking("JsonSaver TryUpgrade -> " + directory);
#endif
            try
            {
                bool needsSaving = false;
                T defaultConfig = new T();
                for (int t = 0; t < Count; t++)
                {
                    T th = this[t];
                    if (th == null)
                    {
                        th = new T();
                        needsSaving = true;
                        continue;
                    }
                    FieldInfo[] fields = Type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (fields[i].IsStatic ||  // if the field is static or it contains [JsonIgnore] in its attributes.
                            fields[i].CustomAttributes.Count(x => x.AttributeType == typeof(JsonIgnoreAttribute)) > 0) continue;
                        object currentvalue = fields[i].GetValue(th);
                        object defaultvalue = fields[i].GetValue(defaultConfig);
                        if (currentvalue == defaultvalue) continue;
                        else if (currentvalue != fields[i].FieldType.getDefaultValue()) continue;
                        else
                        {
                            fields[i].SetValue(th, defaultvalue);
                            needsSaving = true;
                        }
                    }
                }
                if (needsSaving) Save();
            }
            catch (Exception ex)
            {
                L.LogError("Error upgrading in JsonSaver:");
                L.LogError(ex);
            }
        }
        protected sealed class TypeArgumentException : Exception
        {
            public TypeArgumentException() { }

            public TypeArgumentException(MethodBase currentMethod, object badObject, Exception inner)
                : base(string.Format("Arguments of {0} should match the JSONSaver's specified type: {1}. The object you gave it was of type: {2}",
                    currentMethod.Name, typeof(T).Name, badObject.GetType().Name), inner)
            {

            }
        }
        public sealed class JSONReadException : Exception
        {
            public JSONReadException() { }

            public JSONReadException(string directory, Exception inner)
                : base(string.Format("Could not deserialize data from {0} because the data was corrupted.", directory), inner)
            {
            }
        }
    }
    public static class JsonEx
    {
        public static readonly JsonSerializerOptions serializerSettings = new JsonSerializerOptions()
        {
            WriteIndented = true, 
            IncludeFields = true, 
            AllowTrailingCommas = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };
        public static readonly JsonWriterOptions writerOptions = new JsonWriterOptions() { Indented = true };
        public static readonly JsonReaderOptions readerOptions = new JsonReaderOptions() { AllowTrailingCommas = true };
        public static void WriteProperty(this Utf8JsonWriter writer, string propertyName, bool value)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteBooleanValue(value);
        }
        public static void WriteProperty(this Utf8JsonWriter writer, string propertyName, byte value)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteNumberValue(value);
        }
        public static void WriteProperty(this Utf8JsonWriter writer, string propertyName, byte[] value)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteBase64StringValue(new ReadOnlySpan<byte>(value));
        }
        public static void WriteProperty(this Utf8JsonWriter writer, string propertyName, ReadOnlySpan<byte> value)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteBase64StringValue(value);
        }
        public static void WriteProperty(this Utf8JsonWriter writer, string propertyName, char value)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteStringValue(new string(new char[1] { value }));
        }
        public static void WriteProperty(this Utf8JsonWriter writer, string propertyName, decimal value)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteNumberValue(value);
        }
        public static void WriteProperty(this Utf8JsonWriter writer, string propertyName, double value)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteNumberValue(value);
        }
        public static void WriteProperty(this Utf8JsonWriter writer, string propertyName, float value)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteNumberValue(value);
        }
        public static void WriteProperty(this Utf8JsonWriter writer, string propertyName, int value)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteNumberValue(value);
        }
        public static void WriteProperty(this Utf8JsonWriter writer, string propertyName, long value)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteNumberValue(value);
        }
        public static void WriteProperty(this Utf8JsonWriter writer, string propertyName, sbyte value)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteNumberValue(value);
        }
        public static void WriteProperty(this Utf8JsonWriter writer, string propertyName, short value)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteNumberValue(value);
        }
        public static void WriteProperty(this Utf8JsonWriter writer, string propertyName, string value)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteStringValue(value);
        }
        public static void WriteProperty(this Utf8JsonWriter writer, string propertyName, DateTime value)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteStringValue(value);
        }
        public static void WriteProperty(this Utf8JsonWriter writer, string propertyName, DateTimeOffset value)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteStringValue(value);
        }
        public static void WriteProperty(this Utf8JsonWriter writer, string propertyName, Guid value)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteStringValue(value);
        }
        public static void WriteProperty(this Utf8JsonWriter writer, string propertyName, TimeSpan value)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteStringValue(value.ToString());
        }
        public static void WriteProperty(this Utf8JsonWriter writer, string propertyName, uint value)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteNumberValue(value);
        }
        public static void WriteProperty(this Utf8JsonWriter writer, string propertyName, ulong value)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteNumberValue(value);
        }
        public static void WriteProperty(this Utf8JsonWriter writer, string propertyName, ushort value)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteNumberValue(value);
        }
        public static void WriteProperty(this Utf8JsonWriter writer, string propertyName, IJsonReadWrite value)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteStartObject();
            value.WriteJson(writer);
            writer.WriteEndObject();
        }
        public static void WriteArrayProperty(this Utf8JsonWriter writer, string propertyName, IJsonReadWrite[] values)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteStartArray();
            for (int i = 0; i < values.Length; i++)
            {
                writer.WriteStartObject();
                values[i].WriteJson(writer);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
        public static void WriteArrayProperty(this Utf8JsonWriter writer, string propertyName, IEnumerable<IJsonReadWrite> values)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteStartArray();
            foreach (IJsonReadWrite jwr in values)
            {
                writer.WriteStartObject();
                jwr.WriteJson(writer);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
    }
}
