using DanielWillett.ReflectionTools;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Players.Unlocks;

/// <summary>
/// Generic read-only requirement to unlock something.
/// </summary>
public abstract class UnlockRequirement : ICloneable
{
    public uint PrimaryKey { get; set; }

    private static readonly Dictionary<Type, string[]> LegacyInfo = new Dictionary<Type, string[]>()
    {
        { typeof(LevelUnlockRequirement), [ "unlock_level"               ] },
        { typeof(RankUnlockRequirement),  [ "unlock_rank"                ] },
        { typeof(QuestUnlockRequirement), [ "unlock_presets", "quest_id" ] }
    };

    /// <summary>
    /// If a player passes the requirements. This check can do some caching, mainly for signs.
    /// </summary>
    public abstract bool CanAccessFast(WarfarePlayer player);

    /// <summary>
    /// Full check if a player passes the requirements.
    /// </summary>
    public virtual async UniTask<bool> CanAccessAsync(WarfarePlayer player, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        return CanAccessFast(player);
    }

    /// <summary>
    /// Get the text that shows on a sign when the player is missing the requirement.
    /// </summary>
    public abstract string GetSignText(WarfarePlayer player);

    /// <summary>
    /// Read from JSON in the newer format, which does store the type.
    /// </summary>
    protected abstract bool ReadFromJson(ILogger? logger, ref Utf8JsonReader reader);

    /// <summary>
    /// For older formats that don't store type.
    /// </summary>
    protected virtual void ReadLegacyProperty(ILogger? logger, ref Utf8JsonReader reader, string property) { }

    /// <summary>
    /// Write all properties (not the type) to a JSON writer.
    /// </summary>
    protected abstract void WriteToJson(Utf8JsonWriter writer);

    /// <inheritdoc />
    public abstract object Clone();

    // todo this is not a good way to handle this
    public virtual Exception RequestKitFailureToMeet(CommandContext ctx, Kit kit)
    {
        L.LogWarning("Unhandled kit requirement type: " + GetType().Name);
        return ctx.SendUnknownError();
    }
    public virtual Exception RequestVehicleFailureToMeet(CommandContext ctx, WarfareVehicleInfo data)
    {
        L.LogWarning("Unhandled vehicle requirement type: " + GetType().Name);
        return ctx.SendUnknownError();
    }
    public virtual Exception RequestTraitFailureToMeet(CommandContext ctx, TraitData trait)
    {
        L.LogWarning("Unhandled trait requirement type: " + GetType().Name);
        return ctx.SendUnknownError();
    }

    /// <summary>
    /// Write a <see cref="UnlockRequirement"/> to JSON.
    /// </summary>
    public static void Write(Utf8JsonWriter writer, UnlockRequirement requirement)
    {
        requirement.WriteToJson(writer);
    }

    /// <summary>
    /// Convert a <see cref="UnlockRequirement"/> to a JSON string.
    /// </summary>
    /// <param name="condensed">Don't add new lines and spacing for visibility.</param>
    public string ToJson(bool condensed = true)
    {
        using MemoryStream stream = new MemoryStream(32);
        using Utf8JsonWriter writer = new Utf8JsonWriter(stream, condensed ? ConfigurationSettings.JsonCondensedWriterOptions : ConfigurationSettings.JsonWriterOptions);

        WriteToJson(writer);

        writer.Flush();
        stream.TryGetBuffer(out ArraySegment<byte> buffer);
        return Encoding.UTF8.GetString(buffer);
    }

    /// <summary>
    /// Write an <see cref="UnlockRequirement"/> from JSON.
    /// </summary>
    public static UnlockRequirement? Read(ILogger? logger, IServiceProvider serviceProvider, ref Utf8JsonReader reader)
    {
        Utf8JsonReader typeSearcher = reader;

        if (!JsonUtility.SkipToProperty(ref typeSearcher, "type"))
        {
            // read from legacy property lists (LegacyInfo)
            if (reader.TokenType != JsonTokenType.PropertyName && !reader.Read())
            {
                logger?.LogError("JSON data ended before hitting a property.");
                return null;
            }

            UnlockRequirement? obj = null;
            do
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                    break;

                string property = reader.GetString()!;
                if (!reader.Read())
                {
                    break;
                }

                if (obj != null)
                {
                    obj.ReadLegacyProperty(logger, ref reader, property);
                    continue;
                }

                foreach (KeyValuePair<Type, string[]> propertyList in LegacyInfo)
                {
                    if (Array.IndexOf(propertyList.Value, property) == -1)
                        continue;

                    try
                    {
                        obj = (UnlockRequirement)ActivatorUtilities.CreateInstance(serviceProvider, propertyList.Key);
                    }
                    catch (InvalidOperationException ex)
                    {
                        logger?.LogError(ex, "Unable to create an instance of {0}.", Accessor.Formatter.Format(propertyList.Key));
                        return null;
                    }

                    break;
                }

                obj?.ReadLegacyProperty(logger, ref reader, property);
            } while (reader.Read());

            if (obj == null)
                logger?.LogError("No 'type' property present in JSON data.");

            return obj;
        }

        string? typeName = typeSearcher.GetString();
        if (typeName == null)
        {
            logger?.LogError("Empty type name in unlock requirement.");
            return null;
        }

        Type? type = Type.GetType(typeName, false, false) ?? typeof(WarfareModule).Assembly.GetType(typeName, false, false);
        if (type == null || type.IsAbstract || !type.IsSubclassOf(typeof(UnlockRequirement)))
        {
            logger?.LogError("Unknown type name {0} in unlock requirement.", typeName);
            return null;
        }

        try
        {
            UnlockRequirement req = (UnlockRequirement)ActivatorUtilities.CreateInstance(serviceProvider, type);
            if (req.ReadFromJson(logger, ref reader))
                return req;

            if (req is not IDisposable d)
                return null;
            
            try
            {
                d.Dispose();
            }
            catch (Exception ex)
            {
                logger?.LogInformation(ex, "Failed to dispose of {0} after failed reading.", Accessor.Formatter.Format(type));
            }

            return null;
        }
        catch (InvalidOperationException ex)
        {
            logger?.LogError(ex, "Unable to create an instance of {0}.", Accessor.Formatter.Format(type));
            return null;
        }
    }
}