using DanielWillett.SpeedBytes;
using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Steam.Models;

namespace Uncreated.Warfare.Moderation;

public static class Actors
{
    public static IModerationActor BattlEye => BattlEyeActor.Instance;
    public static IModerationActor AntiCheat => AntiCheatActor.Instance;
    public static IModerationActor Console => ConsoleActor.Instance;

    /// <summary>
    /// Try to create a standard actor from an ID.
    /// </summary>
    /// <remarks>Invalid steam IDs will be assumed to be Discord IDs. IDs 0-2 are reserved for Console, Anti-Cheat, and BattlEye in that order.</remarks>
    public static IModerationActor GetActor(ulong id)
    {
        if (id == 0ul || id == Provider.server.m_SteamID)
            return ConsoleActor.Instance;

        if (id == 1ul)
            return AntiCheatActor.Instance;

        if (id == 2ul)
            return BattlEyeActor.Instance;

        if (new CSteamID(id).GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
            return new DiscordActor(id);

        if (UCWarfare.IsLoaded && UCPlayer.FromID(id) is { IsOnline: true } pl)
            return pl;
            
        return new PlayerActor(id);

    }
}

public interface IModerationActor
{
    bool Async { get; }
    ulong Id { get; }
    ValueTask<string?> GetProfilePictureURL(DatabaseInterface database, AvatarSize size, CancellationToken token = default);
    ValueTask<string> GetDisplayName(DatabaseInterface database, CancellationToken token = default);
}

[JsonConverter(typeof(ActorConverter))]
public class PlayerActor : IModerationActor
{
    public ulong Id { get; }
    bool IModerationActor.Async => true;
    public PlayerActor(ulong id) => Id = id;
    public override string ToString() => Id.ToString(CultureInfo.InvariantCulture);
    public virtual async ValueTask<string> GetDisplayName(DatabaseInterface database, CancellationToken token = default)
    {
        return (await database.GetUsernames(Id, true, token)).PlayerName;
    }
    public virtual async ValueTask<string?> GetProfilePictureURL(DatabaseInterface database, AvatarSize size, CancellationToken token = default)
    {
        if (database.TryGetAvatar(Id, size, out string url))
            return url;
        if (UCWarfare.IsLoaded)
        {
            if (UCPlayer.FromID(Id) is { } pl)
                return await (pl as IModerationActor).GetProfilePictureURL(database, size, token).ConfigureAwait(false);

            PlayerSummary? summary = await database.SteamAPI.GetPlayerSummary(Id, token);
            if (summary == null)
                return null;
            url = size switch
            {
                AvatarSize.Full => summary.AvatarUrlFull,
                AvatarSize.Medium => summary.AvatarUrlMedium,
                _ => summary.AvatarUrlSmall
            };
            if (url != null)
                database.UpdateAvatar(Id, size, url);
            return url;
        }

        return null;
    }
}

[JsonConverter(typeof(ActorConverter))]
public class DiscordActor : IModerationActor
{
    public static Func<ulong, CancellationToken, Task<string>>? GetDiscordDisplayNameOverride = null;
    public static Func<ulong, CancellationToken, Task<string>>? GetDiscordProfilePictureOverride = null;
    public ulong Id { get; }
    bool IModerationActor.Async => true;
    public DiscordActor(ulong id) => Id = id;
    public override string ToString() => "<@" + Id.ToString(CultureInfo.InvariantCulture) + ">";
    public virtual async ValueTask<string> GetDisplayName(DatabaseInterface database, CancellationToken token = default)
    {
        if (GetDiscordDisplayNameOverride != null)
            return await GetDiscordDisplayNameOverride(Id, token).ConfigureAwait(false);

        ulong steam64 = await database.Sql.GetSteam64(Id, token).ConfigureAwait(false);
        if (steam64 != 0)
            return (await database.GetUsernames(steam64, true, token).ConfigureAwait(false)).PlayerName;

        return "<@" + Id + ">";
    }
    public virtual async ValueTask<string?> GetProfilePictureURL(DatabaseInterface database, AvatarSize size, CancellationToken token = default)
    {
        if (GetDiscordProfilePictureOverride != null)
            return await GetDiscordProfilePictureOverride(Id, token).ConfigureAwait(false);

        ulong steam64 = await database.Sql.GetSteam64(Id, token).ConfigureAwait(false);
        if (steam64 == 0) return null;
        if (database.TryGetAvatar(steam64, size, out string url))
            return url;
        if (UCWarfare.IsLoaded)
        {
            if (UCPlayer.FromID(steam64) is { } pl)
                return await (pl as IModerationActor).GetProfilePictureURL(database, size, token).ConfigureAwait(false);

            PlayerSummary? summary = await database.SteamAPI.GetPlayerSummary(steam64, token);
            if (summary == null)
                return null;
            url = size switch
            {
                AvatarSize.Full => summary.AvatarUrlFull,
                AvatarSize.Medium => summary.AvatarUrlMedium,
                _ => summary.AvatarUrlSmall
            };
            if (url != null)
                database.UpdateAvatar(steam64, size, url);
            return url;
        }

        return null;
    }
}

[JsonConverter(typeof(ActorConverter))]
public class ConsoleActor : IModerationActor
{
    public static IModerationActor Instance { get; } = new ConsoleActor();
    public ulong Id => 0;
    bool IModerationActor.Async => false;
    private ConsoleActor() { }
    public override string ToString() => "Console";
    public ValueTask<string> GetDisplayName(DatabaseInterface database, CancellationToken token = default) => new ValueTask<string>("Console");
    public ValueTask<string?> GetProfilePictureURL(DatabaseInterface database, AvatarSize size, CancellationToken token = default)
        => new ValueTask<string?>(UCWarfare.IsLoaded ? "https://i.imgur.com/f2axLoQ.png" /* this image has rounded corners */ : "https://i.imgur.com/NRZFfKN.png");
}

[JsonConverter(typeof(ActorConverter))]
public class AntiCheatActor : IModerationActor
{
    public static IModerationActor Instance { get; } = new AntiCheatActor();
    public ulong Id => 1;
    bool IModerationActor.Async => false;
    private AntiCheatActor() { }
    public override string ToString() => "Anti-Cheat";
    public ValueTask<string> GetDisplayName(DatabaseInterface database, CancellationToken token = default) => new ValueTask<string>("Anti-Cheat");
    public ValueTask<string?> GetProfilePictureURL(DatabaseInterface database, AvatarSize size, CancellationToken token = default) => ConsoleActor.Instance.GetProfilePictureURL(database, size, token);
}

[JsonConverter(typeof(ActorConverter))]
public class BattlEyeActor : IModerationActor
{
    public static IModerationActor Instance { get; } = new BattlEyeActor();
    public ulong Id => 2;
    bool IModerationActor.Async => false;
    private BattlEyeActor() { }
    public override string ToString() => "BattlEye";
    public ValueTask<string> GetDisplayName(DatabaseInterface database, CancellationToken token = default) => new ValueTask<string>("BattlEye");
    public ValueTask<string?> GetProfilePictureURL(DatabaseInterface database, AvatarSize size, CancellationToken token = default) => new ValueTask<string?>("https://i.imgur.com/jasTgpD.jpg");
}
public enum AvatarSize
{
    Full,
    Medium,
    Small
}

public readonly struct RelatedActor : IEquatable<RelatedActor>
{
    public const string RolePrimaryAdmin = "Primary Admin";
    
    public const string RoleRemovingAdmin = "Removing Admin";
    
    public const string RoleReporter = "Reporter";
    
    public const string RoleEditor = "Editor";

    [JsonPropertyName("role")]
    public string Role { get; }

    [JsonPropertyName("admin")]
    public bool Admin { get; }

    [JsonPropertyName("actor")]
    [JsonConverter(typeof(ActorConverter))]
    public IModerationActor Actor { get; }
    
    public RelatedActor() { }
    public RelatedActor(string role, bool admin, IModerationActor actor)
    {
        Role = role;
        Admin = admin;
        Actor = actor;
    }

    // ReSharper disable once UnusedParameter.Local
    public RelatedActor(ByteReader reader, ushort version)
    {
        Role = reader.ReadString();
        Admin = reader.ReadBool();
        Actor = Actors.GetActor(reader.ReadUInt64());
    }
    public void Write(ByteWriter writer)
    {
        writer.Write(Role);
        writer.Write(Admin);
        writer.Write(Actor.Id);
    }

    public bool Equals(RelatedActor other)
    {
        return string.Equals(Role, other.Role, StringComparison.Ordinal) && Admin == other.Admin && (Actor == null && other.Actor == null || Actor != null && other.Actor != null && Actor.Id == other.Actor.Id);
    }
    public override bool Equals(object? obj)
    {
        return obj is RelatedActor other && Equals(other);
    }
    public override int GetHashCode() => HashCode.Combine(Role, Admin, Actor.Id);
    public static bool operator ==(RelatedActor left, RelatedActor right)
    {
        return left.Equals(right);
    }
    public static bool operator !=(RelatedActor left, RelatedActor right)
    {
        return !left.Equals(right);
    }

    public override string ToString() => Actor + " (" + Role + (Admin ? " [A]" : string.Empty) + ")";
}
public sealed class ActorConverter : JsonConverter<IModerationActor>
{
    public override IModerationActor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null!;
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetUInt64(out ulong id))
            throw new JsonException("Reading actor expected type: UInt64, instead of " + reader.TokenType + ".");

        return Actors.GetActor(id);
    }

    public override void Write(Utf8JsonWriter writer, IModerationActor value, JsonSerializerOptions options)
    {
        if (value == null)
            writer.WriteNullValue();
        else
            writer.WriteNumberValue(value.Id);
    }
}