using HarmonyLib;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Uncreated.Encoding;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Networking.Async;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Singletons;
using Unity.Rendering.HybridV2;
using UnityEngine;

namespace Uncreated.Warfare.Sync;
public class ConfigSync : MonoBehaviour
{
#nullable disable
    public static ConfigSync Instance { get; private set; }
#nullable restore
    public static readonly Dictionary<int, ConfigSyncInst> RegisteredTypes = new Dictionary<int, ConfigSyncInst>(16);
    public static readonly Dictionary<int, object> RegisteredInstances = new Dictionary<int, object>(32);
    private static readonly HarmonyMethod ptfx = new HarmonyMethod(typeof(PatchFactory).GetMethod(nameof(PatchFactory.GetPostfix), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
    private static readonly List<UpdateTracker> _trackers = new List<UpdateTracker>(32);
    private static readonly List<PropertyValue> pending = new List<PropertyValue>(16);
    private static readonly List<PropertyValue> outPending = new List<PropertyValue>(16);
    private static readonly Type rtDynamicMethodType = Type.GetType("System.Reflection.Emit.DynamicMethod+RTDynamicMethod");
    private static readonly InstanceGetter<MethodInfo, MethodInfo>? getDynamicMethodOwner;
    private static bool savePending = false;
    private static bool hasReflected = false;
    [SuppressMessage(Data.SUPPRESS_CATEGORY, Data.SUPPRESS_ID)]
    void Start()
    {
        Instance = this;
    }

    [SuppressMessage(Data.SUPPRESS_CATEGORY, Data.SUPPRESS_ID)]
    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    private bool _sendPending = false;
    [SuppressMessage(Data.SUPPRESS_CATEGORY, Data.SUPPRESS_ID)]
    void LateUpdate()
    {
        if (savePending)
        {
            savePending = false;
            SaveProperties();
        }
        if (_sendPending)
            return;
        if (outPending.Count > 0)
        {
            DateTime now = DateTime.UtcNow;
            for (int i = outPending.Count - 1; i >= 0; --i)
            {
                PropertyValue p = outPending[i];
                if ((now - p.SendTime).TotalMilliseconds > 5000)
                {
                    p.SendTime = default;
                    outPending.RemoveAt(i);
                    pending.Add(p);
                }
            }
        }
        if (pending.Count > 0 && UCWarfare.CanUseNetCall)
        {
            DateTime now = DateTime.UtcNow;
            for (int i = 0; i < pending.Count; ++i)
                pending[i].SendTime = now;

            int ind = outPending.Count;
            if (pending.Count > ushort.MaxValue)
            {
                outPending.AddRange(pending.Take(ushort.MaxValue));
                pending.RemoveRange(0, ushort.MaxValue);
            }
            else
            {
                outPending.AddRange(pending);
                pending.Clear();
            }
            StartCoroutine(Coroutine(ind));
        }
    }
    IEnumerator Coroutine(int index)
    {
        L.LogDebug("Queueing " + (outPending.Count - index) + " properties.");
        _sendPending = true;
        try
        {
            UnityNetTask task = outPending.Count == 1
                ? NetCalls.SendSingleProperty.YieldRequestAck(UCWarfare.I.NetClient!, outPending[index])
                : NetCalls.BulkSendProperties.YieldRequestAck(UCWarfare.I.NetClient!, WriteBulk);
            yield return task;
            if (task.Responded)
            {
                for (int i = index; i < outPending.Count; ++i)
                {
                    PropertyValue p = outPending[i];
                    ConfigSyncInst.Property? prop = GetPropertyData(p.ParentSyncId, p.SyncId, out _);
                    if (prop is not null)
                    {
                        prop.ValuePending = false;
                        prop.LastUpdated = default;
                    }
                    outPending[i].Acknowledged = true;
                }

                L.LogDebug("Received confirmation of " + (outPending.Count - index) + " properties.");
                outPending.RemoveRange(index, outPending.Count - index);
                savePending = true;
            }
            else
            {
                L.LogDebug("Failed to send " + (outPending.Count - index) + " properties.");
            }
        }
        finally
        {
            _sendPending = false;
        }
    }
    private static void WriteBulk(ByteWriter writer)
    {
        writer.Write((ushort)outPending.Count);
        for (int i = 0; i < outPending.Count; ++i)
            outPending[i].Write(writer);
    }
    public static ConfigSyncInst? GetClassData(int syncId) => RegisteredTypes.TryGetValue(syncId, out ConfigSyncInst i) ? i : null;
    public static ConfigSyncInst? GetClassData(Type type) => RegisteredTypes.FirstOrDefault(x => x.Value.Type == type).Value;
    public static ConfigSyncInst.Property? GetPropertyData(Type type, int propertySyncId)
    {
        KeyValuePair<int, ConfigSyncInst> prop = RegisteredTypes.FirstOrDefault(x => x.Value.Type == type);
        if (prop.Value is null)
            return null;
        return prop.Value.SyncedMembers.TryGetValue(propertySyncId, out ConfigSyncInst.Property i) ? i : null;
    }
    public static ConfigSyncInst.Property? GetPropertyData(ConfigSyncInst type, int propertySyncId) => type.SyncedMembers.TryGetValue(propertySyncId, out ConfigSyncInst.Property i) ? i : null;
    public static ConfigSyncInst.Property? GetPropertyData(int classSyncId, int propertySyncId, out ConfigSyncInst? inst)
    {
        if (RegisteredTypes.TryGetValue(classSyncId, out inst))
        {
            return inst.SyncedMembers.TryGetValue(propertySyncId, out ConfigSyncInst.Property? val) ? val : null;
        }
        else
        {
            return null;
        }
    }
    public class ConfigSyncInst
    {
        public readonly int SyncId;
        public readonly Dictionary<int, Property> SyncedMembers = new Dictionary<int, Property>();
        public readonly Type Type;
        public readonly SyncMode Mode;
        public MethodInfo? SaveMethod;
        public ConfigSyncInst(int syncId, Type type, SyncMode mode)
        {
            SyncId = syncId;
            Type = type;
            Mode = mode;
        }
        public class Property
        {
            public readonly InstanceSetter<object?, object?> Setter;
            public readonly InstanceGetter<object?, object?> Getter;
            public readonly PropertyInfo PropertyInfo;
            public readonly MethodInfo SetterMethod;
            public readonly MethodInfo GetterMethod;
            public readonly MethodInfo OnUpdateMethod;
            public readonly FieldInfo BackingField;
            public readonly ByteWriter.Writer<object?> Writer;
            public readonly ByteReader.Reader<object?> Reader;
            public readonly bool CanHaveUpdateTracked;
            public readonly bool Static;
            public readonly string JsonName;
            public object? SavedValue;
            public bool ValuePending;
            public DateTimeOffset LastUpdated = default;
            public readonly int SyncId;
            public Property(InstanceSetter<object?, object?> setter, InstanceGetter<object?, object?> getter, PropertyInfo propertyInfo, MethodInfo setterMethod, MethodInfo getterMethod, FieldInfo backingField, ByteWriter.Writer<object?> writer, ByteReader.Reader<object?> reader, string jsonName, int syncId, MethodInfo onUpdateMethod)
            {
                Setter = setter;
                Getter = getter;
                PropertyInfo = propertyInfo;
                SetterMethod = setterMethod;
                GetterMethod = getterMethod;
                BackingField = backingField;
                Writer = writer;
                Reader = reader;
                CanHaveUpdateTracked = typeof(INotifyValueUpdate).IsAssignableFrom(propertyInfo.PropertyType);
                Static = getterMethod.IsStatic;
                ValuePending = false;
                JsonName = jsonName;
                SyncId = syncId;
                OnUpdateMethod = onUpdateMethod;
            }
        }
    }

    #region IL and Reflection Spaghetti
    public static void Reflect()
    {
        if (hasReflected)
            return;
        Assembly exeAssembly = Assembly.GetExecutingAssembly();
        bool isInternal = Assembly.GetCallingAssembly() == exeAssembly;
        foreach (Type type in exeAssembly.GetTypes())
        {
            if (Attribute.GetCustomAttribute(type, typeof(SyncAttribute)) is SyncAttribute typeSync && typeSync.SyncId != 0 && !RegisteredTypes.ContainsKey(typeSync.SyncId))
            {
                ConfigSyncInst inst = new ConfigSyncInst(typeSync.SyncId, type, typeSync.SyncMode);
                if (isInternal && !string.IsNullOrEmpty(typeSync.SaveMethodOverride))
                {
                    inst.SaveMethod = type.GetMethods(BindingFlags.Instance | BindingFlags.Static |
                                                      BindingFlags.FlattenHierarchy |
                                                      BindingFlags.NonPublic | BindingFlags.Public)
                        .FirstOrDefault(x =>
                            x.Name.Equals(typeSync.SaveMethodOverride, StringComparison.Ordinal) &&
                            x.ReturnType == typeof(void) && x.GetParameters().Length == 0);
                }
                PropertyInfo[] props = type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (PropertyInfo prop in props)
                {
                    if (!NetFactory.IsValidAutoType(prop.PropertyType))
                        throw new InvalidOperationException("Invalid type on property \"" + type.Name + "." +
                                                            prop.Name + "\"; " + prop.PropertyType +
                                                            " can not be serialized.");
                    if (Attribute.GetCustomAttribute(prop, typeof(SyncAttribute)) is SyncAttribute propSync && !inst.SyncedMembers.ContainsKey(propSync.SyncId))
                    {
                        try
                        {
                            MethodInfo setter = prop.GetSetMethod(true);
                            MethodInfo getter = prop.GetGetMethod(true);
                            if (isInternal)
                            {
                                if (propSync.SyncMode == SyncMode.Automatic)
                                {
                                    if (setter == null || getter == null)
                                        throw new InvalidOperationException("Invalid auto-sync property \"" + type.Name + "." + prop.Name +
                                                                            "\"; All synced properties must have a getter and setter.");
                                    PatchProperty(prop);
                                }

                                FieldInfo? backingField = prop.DeclaringType.GetField(
                                    propSync.OverrideFieldName ?? ("<" + prop.Name + ">k__BackingField"),
                                    (getter.IsStatic ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.NonPublic |
                                    BindingFlags.Public);
                                if (backingField is null)
                                {
                                    throw new InvalidOperationException("Invalid auto-sync property \"" + type.Name + "." + prop.Name +
                                                                        "\"; All synced properties must have a backing field. " +
                                                                        "Define a custom backing field with the OverrideFieldName property in the Sync attribute.");
                                }

                                MethodInfo? onUpdateMethod = propSync.OnPullMethod is null
                                    ? null
                                    : prop.DeclaringType.GetMethod(propSync.OnPullMethod,
                                        (getter.IsStatic ? BindingFlags.Static : BindingFlags.Instance) |
                                        BindingFlags.NonPublic |
                                        BindingFlags.Public);
                                if (onUpdateMethod is not null)
                                {
                                    if (onUpdateMethod.ReturnType != typeof(void) || onUpdateMethod.GetParameters().Length != 0)
                                    {
                                        L.LogWarning("Invalid signature on OnPullMethod supplied to property \"" +
                                                     type.Name + "." + prop.Name +
                                                     "\"; It must match the signature: " +
                                                     (getter.IsStatic ? "static " : string.Empty) + "void(). This method will not be called.");
                                        onUpdateMethod = null;
                                    }
                                }
                                else if (propSync.OnPullMethod is not null)
                                {
                                    L.LogWarning("Unable to find OnPullMethod supplied to property \"" +
                                                 type.Name + "." + prop.Name +
                                                 "\"; It must be contained in the declaring type of the property (" + prop.DeclaringType.Name + ").");
                                }
                                GetDynamicAssignments(prop, backingField, out InstanceGetter<object?, object?> getter2, out InstanceSetter<object?, object?> setter2);
                                GetCoderMethods(prop, out ByteWriter.Writer<object?> objWriter, out ByteReader.Reader<object?> objReader);
                                string jsonName;
                                if (Attribute.GetCustomAttribute(prop, typeof(JsonPropertyNameAttribute)) is JsonPropertyNameAttribute propAttr && !string.IsNullOrWhiteSpace(propAttr.Name))
                                    jsonName = propAttr.Name;
                                else
                                    jsonName = prop.Name;
                                ConfigSyncInst.Property property = new ConfigSyncInst.Property(setter2, getter2, prop, setter, getter, backingField, objWriter, objReader, jsonName, propSync.SyncId, onUpdateMethod);
                                inst.SyncedMembers.Add(propSync.SyncId, property);
                            }
                            else
                            {
                                GetCoderMethods(prop, out ByteWriter.Writer<object?> objWriter, out ByteReader.Reader<object?> objReader);
                                inst.SyncedMembers.Add(propSync.SyncId, new ConfigSyncInst.Property(null!, null!, prop, getter, setter, null!, objWriter, objReader, null!, propSync.SyncId, null));
                            }
                        }
                        catch (Exception ex)
                        {
                            L.LogError("Failed to reflect property \"" + type.Name + "." + prop.Name + "\".");
                            L.LogError(ex);
                        }
                    }
                }
                RegisteredTypes.Add(typeSync.SyncId, inst);
            }
        }
        if (isInternal)
            ReadProperties();
        hasReflected = true;
    }
    private static void GetCoderMethods(PropertyInfo prop, out ByteWriter.Writer<object?> objWriter, out ByteReader.Reader<object?> objReader)
    {
        Delegate writer = (Delegate)(prop.PropertyType.IsValueType ? typeof(ByteWriter.WriterHelper<>) : typeof(ByteWriter.NullableWriterHelper<>))
            .MakeGenericType(prop.PropertyType)
            .GetField("Writer")
            .GetValue(null);
        Delegate reader = (Delegate)(prop.PropertyType.IsValueType ? typeof(ByteReader.ReaderHelper<>) : typeof(ByteReader.NullableReaderHelper<>))
            .MakeGenericType(prop.PropertyType)
            .GetField("Reader")
            .GetValue(null);
        GetDynamicCoders(prop.Name, prop.PropertyType, prop.DeclaringType, writer, reader, out objWriter, out objReader);
    }
    public static void GetDynamicCoders(string name, Type propType, Type declType, Delegate writer, Delegate reader, out ByteWriter.Writer<object?> objWriter, out ByteReader.Reader<object?> objReader)
    {
        MethodInfo wtrMthd = writer.Method;
        if (wtrMthd is null || !wtrMthd.IsStatic)
            throw new InvalidOperationException("Writer base method must be an existing static method.");
        DynamicMethod dyn;
        ILGenerator il;
        if (propType.IsValueType)
        {
            MethodInfo rdrMthd = reader.Method;
            if (rdrMthd is null || !rdrMthd.IsStatic)
                throw new InvalidOperationException("Reader base method must be an existing static method.");
            dyn = new DynamicMethod("read_<>" + name,
                MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, typeof(object),
                new Type[] { typeof(ByteReader) }, declType, true);
            dyn.DefineParameter(1, ParameterAttributes.None, "reader");
            il = dyn.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.EmitCall(OpCodes.Call, GetRuntimeMethod(rdrMthd), null);
            if (propType.IsValueType)
                il.Emit(OpCodes.Box, propType);
            il.Emit(OpCodes.Ret);
            objReader = (ByteReader.Reader<object?>)dyn.CreateDelegate(typeof(ByteReader.Reader<object?>));
        }
        else
            objReader = (ByteReader.Reader<object?>)reader;
        dyn = new DynamicMethod("write_<>" + name,
            MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, typeof(void),
            new Type[] { typeof(ByteWriter), typeof(object) }, declType, true);
        dyn.DefineParameter(1, ParameterAttributes.None, "writer");
        dyn.DefineParameter(2, ParameterAttributes.None, "arg1");
        il = dyn.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        if (propType.IsValueType)
        {
            il.Emit(OpCodes.Unbox_Any, propType);
        }
        il.EmitCall(OpCodes.Call, GetRuntimeMethod(wtrMthd), null);
        il.Emit(OpCodes.Ret);
        objWriter = (ByteWriter.Writer<object?>)dyn.CreateDelegate(typeof(ByteWriter.Writer<object?>));
    }
    static ConfigSync()
    {
        if (rtDynamicMethodType == null)
            return;
        DynamicMethod method = new DynamicMethod("getDynamicMethodOwner", MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard, typeof(DynamicMethod), new Type[] { typeof(MethodInfo) }, typeof(ConfigSync), true);
        ILGenerator il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, rtDynamicMethodType.GetField("m_owner", BindingFlags.NonPublic | BindingFlags.Instance));
        il.Emit(OpCodes.Ret);
        getDynamicMethodOwner = (InstanceGetter<MethodInfo, MethodInfo>)method.CreateDelegate(typeof(InstanceGetter<MethodInfo, MethodInfo>));
    }
    private static MethodInfo GetRuntimeMethod(MethodInfo method)
    {
        if (getDynamicMethodOwner != null && method.GetType() == rtDynamicMethodType)
            return getDynamicMethodOwner(method);
        return method;
    }
    private static void GetDynamicAssignments(PropertyInfo prop, FieldInfo backingField, out InstanceGetter<object?, object?> getter, out InstanceSetter<object?, object?> setter)
    {
        DynamicMethod dyn = new DynamicMethod("get_<>" + prop.Name + "Fast",
            MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, typeof(object),
            new Type[] { typeof(object) }, prop.DeclaringType, true);
        dyn.DefineParameter(1, ParameterAttributes.None, "instance");
        ILGenerator il = dyn.GetILGenerator();
        if (backingField.IsStatic)
        {
            il.Emit(OpCodes.Ldsfld, backingField);
        }
        else
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, backingField);
        }
        if (prop.PropertyType.IsValueType)
            il.Emit(OpCodes.Box, prop.PropertyType);
        il.Emit(OpCodes.Ret);

        getter = (InstanceGetter<object?, object?>)dyn.CreateDelegate(typeof(InstanceGetter<object?, object?>));
        dyn = new DynamicMethod("set_<>" + prop.Name + "Fast",
            MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, typeof(void),
            new Type[] { typeof(object), typeof(object) }, prop.DeclaringType, true);
        il = dyn.GetILGenerator();
        if (backingField.IsStatic)
        {
            il.Emit(OpCodes.Ldarg_1);
            if (prop.PropertyType.IsValueType)
                il.Emit(OpCodes.Unbox_Any, prop.PropertyType);
            il.Emit(OpCodes.Stsfld, backingField);
        }
        else
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            if (prop.PropertyType.IsValueType)
                il.Emit(OpCodes.Unbox_Any, prop.PropertyType);
            il.Emit(OpCodes.Stfld, backingField);
        }
        il.Emit(OpCodes.Ret);
        setter = (InstanceSetter<object?, object?>)dyn.CreateDelegate(typeof(InstanceSetter<object?, object?>));
    }
    public static void UnpatchAll()
    {
        foreach (KeyValuePair<int, ConfigSyncInst> c in RegisteredTypes)
        {
            foreach (KeyValuePair<int, ConfigSyncInst.Property> p in c.Value.SyncedMembers)
            {
                UnpatchProperty(p.Value.PropertyInfo);
            }
        }
    }
    private static void PatchProperty(PropertyInfo prop)
    {
        try
        {
            L.LogDebug("Patching " + prop.DeclaringType.Name + "." + prop.Name + " for cross-server syncing.");
            Patches.Patcher.Patch(prop.GetSetMethod(true), postfix: ptfx);
        }
        catch (Exception ex)
        {
            L.LogError("Error patching " + prop.DeclaringType.Name + "." + prop.Name + ":");
            L.LogError(ex);
        }
    }
    private static void UnpatchProperty(PropertyInfo prop)
    {
        MethodInfo setter = prop.GetSetMethod(true);
        Patches.Patcher.Unpatch(setter, HarmonyPatchType.Prefix, Patches.Patcher.Id);
        Patches.Patcher.Unpatch(setter, HarmonyPatchType.Postfix, Patches.Patcher.Id);
    }
    #endregion

    #region Json I/O
    private static readonly ByteReader byteReader = new ByteReader();
    private static readonly ByteWriter byteWriter = new ByteWriter(false, 128);
    private static void SaveProperties()
    {
        F.CheckDir(Data.Paths.Sync, out bool success);
        if (!success)
            return;
        using FileStream str = new FileStream(Data.Paths.ConfigSync, FileMode.Create, FileAccess.Write, FileShare.Read);
        using Utf8JsonWriter writer = new Utf8JsonWriter(str, JsonEx.condensedWriterOptions);
        writer.WriteStartArray();
        foreach (KeyValuePair<int, ConfigSyncInst> @class in RegisteredTypes)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("parent_sync_id");

            writer.WriteNumberValue(@class.Key);
            writer.WritePropertyName("pending_properties");

            writer.WriteStartArray();
            foreach (KeyValuePair<int, ConfigSyncInst.Property> prop in @class.Value.SyncedMembers)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("sync_id");
                writer.WriteNumberValue(prop.Key);

                writer.WritePropertyName("pending");
                writer.WriteBooleanValue(prop.Value.ValuePending);

                writer.WritePropertyName("last_updated");
                writer.WriteStringValue(prop.Value.LastUpdated);

                writer.WritePropertyName("value");
                if (prop.Value.SavedValue == null)
                    writer.WriteNullValue();
                else
                {
                    try
                    {
                        lock (byteWriter)
                        {
                            byteWriter.Flush();
                            prop.Value.Writer(byteWriter, prop.Value.SavedValue);
                            writer.WriteStringValue(Convert.ToBase64String(byteWriter.ToArray()));
                        }
                    }
                    catch (Exception ex)
                    {
                        L.LogError("Error writing value to byte writer");
                        L.LogError(ex);
                        writer.WriteNullValue();
                    }
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.Flush();
    }
    private static void ReadProperties()
    {
        L.LogDebug("Reading");
        if (!File.Exists(Data.Paths.ConfigSync))
            return;
        using FileStream str = new FileStream(Data.Paths.ConfigSync, FileMode.Open, FileAccess.Read, FileShare.Read);
        byte[] bytes = new byte[str.Length];
        if (bytes.Length < 2)
            return;
        str.Read(bytes, 0, bytes.Length);
        Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
            return;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                bool skipped = false;
                int? si = null;
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                        break;
                    if (skipped) continue;
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        string propName = reader.GetString()!;
                        if (!reader.Read()) break;
                        L.LogDebug(propName);
                        if (propName.Equals("parent_sync_id", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!reader.TryGetInt32(out int v))
                                throw new Exception("Failed to read parent sync ID.");
                            else si = v;
                        }
                        else if (propName.Equals("pending_properties", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!si.HasValue)
                                throw new Exception("The property 'parent_sync_id' must come before 'pending_properties'.");
                            ConfigSyncInst? docType = GetClassData(si.Value);
                            if (docType is null)
                            {
                                L.LogWarning("Skipping parent sync ID {#" + si.Value + "}. Doesn't match to any registered sync parent types.");
                                skipped = true;
                                continue;
                            }
                            if (reader.TokenType == JsonTokenType.StartArray)
                            {
                                while (reader.Read())
                                {
                                    if (reader.TokenType == JsonTokenType.EndArray)
                                        break;
                                    if (reader.TokenType == JsonTokenType.StartObject)
                                    {
                                        bool skipped2 = false;
                                        bool objectFound = false;
                                        bool? pending = null;
                                        DateTimeOffset? timestamp = null;
                                        object? obj = null;
                                        ConfigSyncInst.Property? prop = null;
                                        while (reader.Read())
                                        {
                                            if (reader.TokenType == JsonTokenType.EndObject)
                                                break;
                                            if (skipped2) continue;
                                            if (reader.TokenType == JsonTokenType.PropertyName)
                                            {
                                                propName = reader.GetString()!;
                                                if (!reader.Read()) break;
                                                if (propName.Equals("sync_id", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    if (!reader.TryGetInt32(out int v))
                                                        throw new Exception("Failed to read sync ID.");
                                                    prop = GetPropertyData(docType, v);
                                                    if (prop is null)
                                                    {
                                                        L.LogWarning("Skipping sync ID {#" + v + "}. Doesn't match to any registered properties in " + docType.Type.Name + ".");
                                                        skipped2 = true;
                                                        continue;
                                                    }
                                                }
                                                else if (propName.Equals("pending", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    if (reader.TokenType is JsonTokenType.True or JsonTokenType.False)
                                                        pending = reader.TokenType == JsonTokenType.True;
                                                    else throw new Exception("Failed to read timestamp.");
                                                }
                                                else if (propName.Equals("last_updated", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    if (reader.TryGetDateTimeOffset(out DateTimeOffset dto))
                                                        timestamp = dto;
                                                    else throw new Exception("Failed to read timestamp.");
                                                }
                                                else if (propName.Equals("value", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    if (prop == null)
                                                        throw new Exception("sync_id must come before value!");
                                                    objectFound = true;
                                                    if (reader.TokenType == JsonTokenType.Null)
                                                        obj = null;
                                                    else
                                                    {
                                                        lock (byteReader)
                                                        {
                                                            byteReader.LoadNew(Convert.FromBase64String(reader.GetString()));
                                                            obj = prop.Reader(byteReader);
                                                        }
                                                    }
                                                }
                                                if (timestamp.HasValue && prop != null && objectFound && pending.HasValue)
                                                {
                                                    lock (prop)
                                                    {
                                                        prop.SavedValue = obj;
                                                        L.LogDebug(prop.JsonName + ": " + (prop.SavedValue?.ToString() ?? "null"));
                                                        prop.ValuePending = pending.Value;
                                                        prop.LastUpdated = timestamp.Value;
                                                    }
                                                    skipped2 = true;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    #endregion

    public static bool RegisterSingleton(object obj)
    {
        Type type = obj.GetType();
        KeyValuePair<int, ConfigSyncInst> kvp = RegisteredTypes.FirstOrDefault(x => x.Value.Type == type);
        if (kvp.Value is null)
            return false;

        if (RegisteredInstances.TryGetValue(kvp.Key, out object old))
        {
            RegisteredInstances[kvp.Key] = obj;
            L.LogDebug("[CONFIG SYNC] Replaced sync object " + old.ToString() + " with " + obj.ToString() + " (Class ID #" + kvp.Key + ").");
        }
        else
        {
            RegisteredInstances.Add(kvp.Key, obj);
            L.LogDebug("[CONFIG SYNC] Added " + obj.ToString() + " as a sync object (Class ID #" + kvp.Key + ").");
        }
        return true;
    }
    public static void SetPropertySilent(int parentSyncId, int syncId, object? value, bool save = true, bool applyToSave = false, bool pushAsNew = true)
    {
        if (!RegisteredTypes.TryGetValue(parentSyncId, out ConfigSyncInst inst) || !inst.SyncedMembers.TryGetValue(syncId, out ConfigSyncInst.Property prop))
            throw new InvalidOperationException("Property is not registered. Ensure it has the Sync attribute along with its defining class (with a unique ID).");

        SetPropertySilent(inst, prop, value, save, applyToSave, pushAsNew);
    }
    public static void SetPropertySilent(ConfigSyncInst inst, ConfigSyncInst.Property prop, object? value, bool save = true, bool applyToSave = false, bool pushAsNew = true)
    {
        lock (prop)
        {
            object? instance = null;
            if (!prop.Static && !RegisteredInstances.TryGetValue(inst.SyncId, out instance))
                throw new InvalidOperationException("There is not a instance of this ID loaded.");
            if ((value is null && prop.PropertyInfo.PropertyType.IsValueType) || (value is not null && !prop.PropertyInfo.PropertyType.IsAssignableFrom(value.GetType())))
                throw new InvalidOperationException("Value provided: \"" + (value?.ToString() ?? "null") + "\" is not compatible with type " + prop.PropertyInfo.PropertyType.Name + ".");
            prop.Setter(instance, value!);
            if (!applyToSave && !save || prop.SavedValue == null && value == null || value != null && value.Equals(prop.SavedValue))
                return;
            if (applyToSave)
            {
                prop.SavedValue = value;
                prop.ValuePending |= pushAsNew;
                prop.LastUpdated = DateTime.UtcNow;
                ReplicatePropertyValue(inst.SyncId, prop.SyncId, value, prop.LastUpdated);
                SaveProperties();
            }
            if (save)
                TrySaveParent(inst, prop.Static);
        }
    }
    public static void OnInitialSyncRegisteringComplete()
    {
        foreach (ConfigSyncInst inst in RegisteredTypes.Values)
        {
            foreach (ConfigSyncInst.Property property in inst.SyncedMembers.Values)
            {
                object? instance = null;
                if (!property.Static)
                {
                    if (!RegisteredInstances.TryGetValue(inst.SyncId, out instance))
                        continue;
                }
                lock (property)
                {
                    object? value = property.Getter(instance);
                    if (property.SavedValue == null && value == null || value != null && value.Equals(property.SavedValue))
                    {
                        L.LogDebug("Detected no change: " + property.JsonName + " Value: " + (value?.ToString() ?? "null") + ".");
                        continue;
                    }
                    L.LogDebug("Detected change: " + property.JsonName + " (" + (property.SavedValue?.ToString() ?? "null") + " -> " + (value?.ToString() ?? "null") + ").");
                    property.SavedValue = value;
                    property.ValuePending = true;
                    property.LastUpdated = DateTime.UtcNow;
                    ReplicatePropertyValue(inst.SyncId, property.SyncId, value, property.LastUpdated);
                }
            }
        }

        SaveProperties();
    }
    public static void OnUpdated(int parentSyncId, int syncId, object? instance, object? value)
    {
        L.LogDebug("Detected update: " + parentSyncId + " / " + syncId + " to " + (value is null ? "null" : value.ToString()));
        if (Data.IsInitialSyncRegistering)
            return;
        if (RegisteredTypes.TryGetValue(parentSyncId, out ConfigSyncInst inst))
        {
            lock (inst)
            {
                if (inst.SyncedMembers.TryGetValue(syncId, out ConfigSyncInst.Property prop))
                {
                    if (!prop.Static)
                    {
                        if (!RegisteredInstances.TryGetValue(parentSyncId, out object inst2) || inst2 != instance)
                            return;
                    }
                    lock (prop)
                    {
                        L.LogDebug("Updated: " + inst.Type.Name + " / " + prop.PropertyInfo.Name + " to " + (value is null ? "null" : value.ToString()));
                        bool alreadySubbed = false;
                        if (prop.CanHaveUpdateTracked)
                        {
                            for (int i = _trackers.Count - 1; i >= 0; --i)
                            {
                                if (_trackers[i].SyncId == syncId && _trackers[i].ParentSyncId == parentSyncId)
                                {
                                    if (value is not null && ReferenceEquals(_trackers[i].Value, value))
                                    {
                                        alreadySubbed = true;
                                        continue;
                                    }
                                    _trackers[i].Dispose();
                                    _trackers.RemoveAt(i);
                                }
                            }
                            if (!alreadySubbed && value is INotifyValueUpdate nvu)
                            {
                                _trackers.Add(new UpdateTracker(nvu, parentSyncId, syncId, instance));
                            }
                        }
                        prop.LastUpdated = DateTimeOffset.UtcNow;
                        prop.SavedValue = value;
                        prop.ValuePending = true;
                        savePending = true;

                        ReplicatePropertyValue(parentSyncId, syncId, value, prop.LastUpdated);
                    }
                }
            }
        }
    }
    private static void ReplicatePropertyValue(int parentSyncId, int syncId, object? value, DateTimeOffset timestamp)
    {
        if (!UCWarfare.CanUseNetCall)
            return;
        PropertyValue val = new PropertyValue(parentSyncId, syncId, value, timestamp);
        pending.Add(val);
        L.LogDebug("Added " + val.ToString() + " to send after this frame.");
    }
    private static void ApplySyncPacket(SyncPacket packet)
    {
        bool @static = false, instFound = false;
        for (int i = 0; i < packet.PropertyGroups.Count; ++i)
        {
            SyncPacket.PropertyGroup doc = packet.PropertyGroups[i];
            ConfigSyncInst? docType = GetClassData(doc.SyncId);
            if (docType is null)
                throw new Exception("Failed to find class sync info!");
            for (int j = 0; j < doc.Properties.Count; ++j)
            {
                SyncPacket.PropertyGroup.Property propertyData = doc.Properties[j];
                ConfigSyncInst.Property? propType = GetPropertyData(docType, propertyData.Id);
                if (propType is null)
                    throw new Exception("Failed to find property sync info for " + docType.Type.Name + "[" + propertyData.Id + "]!");

                if (propType.LastUpdated > propertyData.Timestamp) // saved value was set later than the value on the server
                    ReplicatePropertyValue(docType.SyncId, propertyData.Id, propType.SavedValue, propType.LastUpdated);
                else
                {
                    SetPropertySilent(docType, propType, propertyData.Value, false, applyToSave: true);
                    TryCallOnUpdated(docType, propType);
                    if (propType.Static)
                        @static = true;
                    else instFound = true;
                }
            }

            if (@static)
                TrySaveParent(docType, true);
            if (instFound)
                TrySaveParent(docType, false);
        }
    }
    private static void ApplyMultipleProperties(PropertyValue[] properties)
    {
        bool @static = false, instFound = false;
        for (int i = 0; i < properties.Length; ++i)
        {
            PropertyValue propertyData = properties[i];
            ConfigSyncInst? docType = GetClassData(propertyData.SyncId);
            if (docType is null)
                throw new Exception("Failed to find class sync info!");
            ConfigSyncInst.Property? propType = GetPropertyData(docType, propertyData.SyncId);
            if (propType is null)
                throw new Exception("Failed to find property sync info for " + docType.Type.Name + "[" + propertyData.SyncId + "]!");

            if (propType.LastUpdated > propertyData.Timestamp) // saved value was set later than the value on the server
                ReplicatePropertyValue(docType.SyncId, propertyData.SyncId, propType.SavedValue, propType.LastUpdated);
            else
            {
                SetPropertySilent(docType, propType, propertyData.Value, false, applyToSave: true);
                TryCallOnUpdated(docType, propType);
                if (propType.Static)
                    @static = true;
                else instFound = true;
            }

            if (@static)
                TrySaveParent(docType, true);
            if (instFound)
                TrySaveParent(docType, false);
        }
    }
    internal static bool TryCallOnUpdated(ConfigSyncInst parent, ConfigSyncInst.Property property)
    {
        if (property.OnUpdateMethod == null)
            return true;
        object? instance = null;
        if (!property.Static)
        {
            if (!RegisteredInstances.TryGetValue(parent.SyncId, out instance))
                return false;
        }
        try
        {
            property.OnUpdateMethod.Invoke(instance, Array.Empty<object>());
            return true;
        }
        catch (Exception ex)
        {
            L.LogError("Error in " + property.PropertyInfo.DeclaringType.Name + "." + property.PropertyInfo.Name + "'s on updated method.");
            L.LogError(ex);
            return false;
        }
    }
    private static void ApplySingleProperty(PropertyValue property)
    {
        ConfigSyncInst.Property? propData = GetPropertyData(property.ParentSyncId, property.SyncId, out ConfigSyncInst? docType);
        if (propData is null || docType is null)
            throw new Exception("Failed to find property sync info for " + (docType?.Type.Name ?? "{" + property.ParentSyncId + "}") + "[" + property.SyncId + "]!");
        if (propData.LastUpdated > property.Timestamp) // saved value was set later than the value on the server
            ReplicatePropertyValue(docType.SyncId, property.SyncId, propData.SavedValue, propData.LastUpdated);
        else
        {
            SetPropertySilent(docType, propData, property.Value, true, applyToSave: true);
            TryCallOnUpdated(docType, propData);
        }
    }
    public static void AddToSyncPacket(int parentSyncId, SyncPacket pending)
    {
        ConfigSyncInst? instance = GetClassData(parentSyncId);
        if (instance is null)
            throw new ArgumentException("Unable to find parent type info.", nameof(parentSyncId));
        SyncPacket.PropertyGroup grp = new SyncPacket.PropertyGroup(instance.SyncId);
        foreach (KeyValuePair<int, ConfigSyncInst.Property> property in instance.SyncedMembers)
        {
            object? inst = null;
            if (!property.Value.Static && !RegisteredInstances.TryGetValue(instance.SyncId, out inst))
                throw new InvalidOperationException("There is not a instance of this ID loaded.");

            grp.Properties.Add(new SyncPacket.PropertyGroup.Property(property.Key, property.Value.Getter.Invoke(inst)));
        }

        pending.PropertyGroups.Add(grp);
    }
    private static void TrySaveParent(ConfigSyncInst inst, bool @static)
    {
        lock (inst)
        {
            object? instance = null;
            if (!@static && !RegisteredInstances.TryGetValue(inst.SyncId, out instance))
                return;
            if (inst.SaveMethod != null)
            {
                if (inst.SaveMethod.IsStatic == @static)
                {
                    inst.SaveMethod.Invoke(instance, Array.Empty<object>());
                }
            }
            else if (instance is ISyncObject sync)
                sync.Save();
        }
    }
    internal static void OnConnected(IConnection connection)
    {
        NetCalls.RequestFullSyncPacket.NetInvoke();
    }
    private class UpdateTracker : IDisposable
    {
        public readonly INotifyValueUpdate Value;
        public readonly int ParentSyncId;
        public readonly int SyncId;
        public readonly object? Instance;
        public UpdateTracker(INotifyValueUpdate value, int parentSyncId, int syncId, object? instance)
        {
            Value = value;
            value.OnUpdate += OnValueUpdated;
            ParentSyncId = parentSyncId;
            SyncId = syncId;
            Instance = instance;
        }
        private void OnValueUpdated(object obj)
        {
            OnUpdated(ParentSyncId, SyncId, Instance, Value);
        }
        public void Dispose()
        {
            Value.OnUpdate -= OnValueUpdated;
        }
    }
    private static class PatchFactory
    {
        private static readonly MethodInfo onUpdated = typeof(ConfigSync).GetMethod(nameof(OnUpdated), BindingFlags.Static | BindingFlags.Public);
        public static MethodInfo GetPostfix(MethodBase method)
        {
            PropertyInfo prop = method.DeclaringType
                .GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .FirstOrDefault(prop => prop.GetSetMethod(true) == method);
            if (prop is null)
                throw new Exception("Non-setter passed to PatchFactory.GetPostfix");
            SyncAttribute attr = (SyncAttribute)Attribute.GetCustomAttribute(prop, typeof(SyncAttribute));
            SyncAttribute typeAttr = (SyncAttribute)Attribute.GetCustomAttribute(prop.DeclaringType, typeof(SyncAttribute));
            if (attr is null || typeAttr is null)
                throw new Exception("Non-sync property passed to PatchFactory.GetPrefix");
            Type type = prop.PropertyType;
            DynamicMethod dyn = new DynamicMethod("OnPost" + prop.Name + "Updated",
                    MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void),
                    method.IsStatic
                        ? new Type[] { prop.PropertyType }
                        : new Type[] { prop.DeclaringType, prop.PropertyType }, prop.DeclaringType, true);

            if (!method.IsStatic)
            {
                dyn.DefineParameter(1, ParameterAttributes.None, "__instance");
                dyn.DefineParameter(2, ParameterAttributes.None, "value");
            }
            else
            {
                dyn.DefineParameter(1, ParameterAttributes.None, "value");
            }
            ILGenerator il = dyn.GetILGenerator();
            il.Emit(OpCodes.Ldc_I4, typeAttr.SyncId);
            il.Emit(OpCodes.Ldc_I4, attr.SyncId);
            if (method.IsStatic)
                il.Emit(OpCodes.Ldnull);
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                if (prop.DeclaringType.IsValueType)
                    il.Emit(OpCodes.Box, prop.DeclaringType);
            }

            il.Emit(method.IsStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1);
            if (prop.PropertyType.IsValueType)
                il.Emit(OpCodes.Box, prop.PropertyType);
            il.Emit(OpCodes.Call, onUpdated);
            il.Emit(OpCodes.Ret);
            return dyn;
        }
    }
    public static class NetCalls
    {
        public static readonly NetCallCustom BulkSendProperties = new NetCallCustom(ReceiveBulkProperties, 256);
        public static readonly NetCallRaw<SyncPacket> SendSyncPacket = new NetCallRaw<SyncPacket>(ReceiveSyncPacket, SyncPacket.ReadPacket, SyncPacket.WritePacket, SyncPacket.CAPACITY);
        public static readonly NetCallRaw<PropertyValue> SendSingleProperty = new NetCallRaw<PropertyValue>(ReceiveSingleProperty, PropertyValue.ReadProperty, PropertyValue.WriteProperty, PropertyValue.CAPACITY);
        public static readonly NetCall RequestFullSyncPacket = new NetCall(ReceiveFullSyncPacketRequest);
        [NetCall(ENetCall.FROM_SERVER, 3002)]
        private static void ReceiveSingleProperty(MessageContext ctx, PropertyValue property)
        {
            try
            {
                ApplySingleProperty(property);
                L.Log("Received and applied single property: " + property.ToString() + ".", ConsoleColor.Magenta);
                ctx.Acknowledge();
            }
            catch (Exception ex)
            {
                L.LogError("Error applying single property: " + property.ToString());
                L.LogError(ex);
            }
        }
        [NetCall(ENetCall.FROM_SERVER, 3005)]
        private static void ReceiveBulkProperties(MessageContext ctx, ByteReader reader)
        {
            try
            {
                int amt = reader.ReadUInt16();
                PropertyValue[] vals = new PropertyValue[amt];
                for (int i = 0; i < amt; ++i)
                {
                    vals[i] = PropertyValue.ReadProperty(reader);
                }
                ApplyMultipleProperties(vals);
                L.Log("Received and applied " + amt + " properties.", ConsoleColor.Magenta);
                ctx.Acknowledge();
            }
            catch (Exception ex)
            {
                L.LogError("Error applying bulk properties");
                L.LogError(ex);
            }
        }

        [NetCall(ENetCall.FROM_SERVER, 3003)]
        private static void ReceiveSyncPacket(MessageContext ctx, SyncPacket packet)
        {
            try
            {
                ApplySyncPacket(packet);
                L.Log("Received and applied sync packet containing " + packet.PropertyGroups.Count + " documents.", ConsoleColor.Magenta);
                ctx.Acknowledge();
            }
            catch (Exception ex)
            {
                L.LogError("Error applying sync packet: " + packet.ToString());
                L.LogError(ex);
            }
        }
        [NetCall(ENetCall.FROM_SERVER, 3004)]
        private static void ReceiveFullSyncPacketRequest(MessageContext ctx)
        {
            SyncPacket packet = new SyncPacket();
            foreach (KeyValuePair<int, ConfigSyncInst> inst in RegisteredTypes)
            {
                if (inst.Value.Mode == SyncMode.Automatic)
                    AddToSyncPacket(inst.Key, packet);
            }
            ctx.Reply(SendSyncPacket, packet);
        }
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SyncAttribute : Attribute
{
    private readonly int syncId;
    private readonly SyncMode syncMode;
    public int SyncId => syncId;
    public SyncMode SyncMode => syncMode;
    public string? OnPullMethod { get; set; }
    public string? OverrideFieldName { get; set; }
    public string? SaveMethodOverride { get; set; }
    public SyncAttribute(ConfigSyncId id) : this((ushort)id) { }
    public SyncAttribute(ushort syncId, SyncMode mode = SyncMode.Automatic)
    {
        this.syncId = syncId;
        this.syncMode = mode;
    }
}
public sealed class PropertyValue : IReadWrite
{
    public const int CAPACITY = 32;
    public int ParentSyncId { get; private set; }
    public int SyncId { get; private set; }
    public object? Value { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public bool Acknowledged = false;
    public DateTime SendTime;
    private PropertyValue() { }
    public PropertyValue(int parentSyncId, int syncId, object? value, DateTimeOffset timestamp)
    {
        ParentSyncId = parentSyncId;
        SyncId = syncId;
        Value = value;
        Timestamp = timestamp;
    }

    public static void WriteProperty(ByteWriter writer, PropertyValue value) => value.Write(writer);
    public void Write(ByteWriter writer)
    {
        ConfigSync.ConfigSyncInst.Property? propData = ConfigSync.GetPropertyData(ParentSyncId, SyncId, out ConfigSync.ConfigSyncInst? docType);
        if (propData is null)
            throw new Exception("Failed to find property sync info for " + (docType?.Type.Name ?? "{" + ParentSyncId + "}") + "[" + SyncId + "]!");
        writer.Write((ushort)ParentSyncId);
        writer.Write((ushort)SyncId);
        writer.Write(Timestamp);
        propData.Writer.Invoke(writer, Value);
    }
    public static PropertyValue ReadProperty(ByteReader reader)
    {
        PropertyValue value = new PropertyValue();
        value.Read(reader);
        return value;
    }
    public void Read(ByteReader reader)
    {
        ParentSyncId = reader.ReadUInt16();
        SyncId = reader.ReadUInt16();
        ConfigSync.ConfigSyncInst.Property? propData = ConfigSync.GetPropertyData(ParentSyncId, SyncId, out ConfigSync.ConfigSyncInst? docType);
        if (propData is null)
            throw new Exception("Failed to find property sync info for " + (docType?.Type.Name ?? "{" + ParentSyncId + "}") + "[" + SyncId + "]!");
        Timestamp = reader.ReadDateTimeOffset();
        Value = propData.Reader.Invoke(reader);
    }
    public override string ToString()
    {
        ConfigSync.ConfigSyncInst.Property? propData = ConfigSync.GetPropertyData(ParentSyncId, SyncId, out ConfigSync.ConfigSyncInst? parent);
        return
            $"Property: {parent?.Type.Name ?? "<unknown-type>"}.{propData?.PropertyInfo.Name ?? "<unknown-property>"}, Timestamp: {Timestamp:G} UTC, " +
            $"Value: {(Value is null ? "{null}" : Translation.ToString(Value, L.DEFAULT, null, null, TranslationFlags.NoColor))}";
    }
}

public sealed class SyncPacket : IReadWrite
{
    public const int CAPACITY = 512;
    public static readonly SyncPacket Empty = new SyncPacket();
    public readonly List<PropertyGroup> PropertyGroups = new List<PropertyGroup>(16);
    public DateTimeOffset Timestamp { get; private set; } = DateTimeOffset.UtcNow;
    public SyncPacket() { }
    public SyncPacket(int parentSyncId)
    {
        ConfigSync.AddToSyncPacket(parentSyncId, this);
    }
    public void Read(ByteReader reader)
    {
        int ct = reader.ReadUInt16();
        for (int i = 0; i < ct; i++)
            PropertyGroups.Add(PropertyGroup.ReadGroup(reader));
        Timestamp = reader.ReadDateTimeOffset();
    }
    public static SyncPacket ReadPacket(ByteReader reader)
    {
        SyncPacket packet = new SyncPacket();
        packet.Read(reader);
        return packet;
    }
    public void Write(ByteWriter writer)
    {
        writer.Write((ushort)PropertyGroups.Count);
        for (int i = 0; i < PropertyGroups.Count; i++)
            PropertyGroups[i].Write(writer);
        writer.Write(Timestamp);
    }
    public static void WritePacket(ByteWriter writer, SyncPacket packet) => packet.Write(writer);
    public static void WriteDocument(ByteWriter writer, PropertyGroup document) => document.Write(writer);
    public class PropertyGroup : IReadWrite
    {
        public int SyncId { get; private set; }
        public readonly List<Property> Properties = new List<Property>(16);
        public DateTimeOffset Timestamp { get; private set; } = DateTimeOffset.UtcNow;
        private PropertyGroup() { }
        public PropertyGroup(int syncId)
        {
            SyncId = syncId;
        }
        public readonly struct Property
        {
            public readonly int Id;
            public readonly object? Value;
            public readonly DateTimeOffset Timestamp;
            public Property(int id, object? value)
            {
                Id = id;
                Value = value;
                Timestamp = default;
            }
            public Property(int id, object? value, DateTimeOffset timestamp)
            {
                Id = id;
                Value = value;
                Timestamp = timestamp;
            }
        }
        public void Write(ByteWriter writer)
        {
            ConfigSync.ConfigSyncInst? docType = ConfigSync.GetClassData(SyncId);
            if (docType is null)
                throw new Exception("Failed to find class sync info!");
            writer.Write((ushort)SyncId);
            writer.Write(Timestamp);
            writer.Write((ushort)Properties.Count);
            for (int i = 0; i < Properties.Count; ++i)
            {
                Property prop = Properties[i];
                ConfigSync.ConfigSyncInst.Property? propData = ConfigSync.GetPropertyData(docType, prop.Id);
                if (propData is null)
                    throw new Exception("Failed to find property sync info for " + docType.Type.Name + "[" + prop.Id + "]!");
                writer.Write((ushort)prop.Id);
                propData.Writer.Invoke(writer, prop.Value!);
            }
        }
        public void Read(ByteReader reader)
        {
            PropertyGroup pgrp = new PropertyGroup();
            SyncId = reader.ReadUInt16();
            Timestamp = reader.ReadDateTimeOffset();
            ConfigSync.ConfigSyncInst? docType = ConfigSync.GetClassData(pgrp.SyncId);
            if (docType is null)
                throw new Exception("Failed to find class sync info for {" + SyncId + "}.");
            Properties.Clear();
            int propCount = reader.ReadUInt16();
            for (int i = 0; i < propCount; ++i)
            {
                int propId = reader.ReadUInt16();
                ConfigSync.ConfigSyncInst.Property? prop = ConfigSync.GetPropertyData(docType, propId);
                if (prop is null)
                    throw new Exception("Failed to find property sync info for " + docType.Type.Name + "[" + propId + "]!");
                pgrp.Properties.Add(new Property(propId, prop.Reader.Invoke(reader)));
            }
        }
        public static PropertyGroup ReadGroup(ByteReader reader)
        {
            PropertyGroup grp = new PropertyGroup();
            grp.Read(reader);
            return grp;
        }
    }
    public override string ToString() => $"Sync packet containing {PropertyGroups.Count} documents. Timestamp: {Timestamp:G} UTC.";
}

public enum SyncMode
{
    Manual,
    Automatic
}
public interface ISyncObject
{
    void Save();
}