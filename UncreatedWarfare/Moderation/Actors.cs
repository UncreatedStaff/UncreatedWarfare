using SDG.Unturned;
using System;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Networking;
using UnturnedWorkshopAnalyst.Models;

namespace Uncreated.Warfare.Moderation;

public static class Actors
{
    public static IModerationActor GetActor(ulong id)
    {
        if (id == 0ul)
            return ConsoleActor.Instance;

        if (id == 1ul)
            return AntiCheatActor.Instance;

        if (id == 2ul)
            return BattlEyeActor.Instance;

        if (Util.IsValidSteam64Id(id))
            return new PlayerActor(id);

        return new DiscordActor(id);
    }
}
public interface IModerationActor
{
    bool Async { get; }
    ulong Id { get; }
    ValueTask<string?> GetProfilePictureURL(DatabaseInterface database, CancellationToken token = default);
    ValueTask<string> GetDisplayName(DatabaseInterface database, CancellationToken token = default);
}

public class PlayerActor : IModerationActor
{
    public ulong Id { get; }
    bool IModerationActor.Async => true;
    public virtual async ValueTask<string> GetDisplayName(DatabaseInterface database, CancellationToken token = default)
    {
        return (await database.GetUsernames(Id, token)).PlayerName;
    }
    public virtual async ValueTask<string?> GetProfilePictureURL(DatabaseInterface database, CancellationToken token = default)
    {
        if (database.IconUrlCache.TryGetValue(Id, out string url))
            return url;
        if (UCWarfare.IsLoaded)
        {
            Wrapper<PlayerSummary[]> summaries = new Wrapper<PlayerSummary[]>();
            UCWarfare.I.StartCoroutine(SteamAPI.GetPlayerSummaries(new ulong[] { Id }, summaries));
            DateTime timeout = DateTime.Now;
            // todo figure this out better... this really sucks ngl
            while ((DateTime.Now - timeout).TotalSeconds < 5)
            {
                await Task.Delay(25, token).ConfigureAwait(false);
                if (summaries.Value != null)
                    return summaries.Value.Length == 0 ? null : summaries.Value[0]?.ProfileUrl;
            }
        }

        return null;
    }

    public PlayerActor(ulong id) => Id = id;
}
public class DiscordActor : IModerationActor
{
    public static Func<ulong, CancellationToken, Task<string>>? GetDiscordDisplayNameOverride = null;
    public static Func<ulong, CancellationToken, Task<string>>? GetDiscordProfilePictureOverride = null;
    public ulong Id { get; }
    bool IModerationActor.Async => true;
    public virtual async ValueTask<string> GetDisplayName(DatabaseInterface database, CancellationToken token = default)
    {
        if (GetDiscordDisplayNameOverride != null)
            return await GetDiscordDisplayNameOverride(Id, token).ConfigureAwait(false);

        ulong steam64 = await database.Sql.GetSteam64(Id, token).ConfigureAwait(false);
        if (steam64 != 0)
            return (await database.GetUsernames(steam64, token).ConfigureAwait(false)).PlayerName;

        return "<@" + Id + ">";
    }
    public virtual async ValueTask<string?> GetProfilePictureURL(DatabaseInterface database, CancellationToken token = default)
    {
        if (GetDiscordProfilePictureOverride != null)
            return await GetDiscordProfilePictureOverride(Id, token).ConfigureAwait(false);

        ulong steam64 = await database.Sql.GetSteam64(Id, token).ConfigureAwait(false);
        if (steam64 == 0) return null;
        if (database.IconUrlCache.TryGetValue(steam64, out string url))
            return url;
        if (UCWarfare.IsLoaded)
        {
            Wrapper<PlayerSummary[]> summaries = new Wrapper<PlayerSummary[]>();
            UCWarfare.I.StartCoroutine(SteamAPI.GetPlayerSummaries(new ulong[] { steam64 }, summaries));
            DateTime timeout = DateTime.Now;
            // todo figure this out better... this really sucks ngl
            while ((DateTime.Now - timeout).TotalSeconds < 5)
            {
                await Task.Delay(25, token).ConfigureAwait(false);
                if (summaries.Value != null)
                    return summaries.Value.Length == 0 ? null : summaries.Value[0]?.ProfileUrl;
            }
        }

        return null;
    }

    public DiscordActor(ulong id) => Id = id;
}
public class ConsoleActor : IModerationActor
{
    public static IModerationActor Instance { get; } = new ConsoleActor();
    public ulong Id => 0;
    bool IModerationActor.Async => false;
    public ValueTask<string> GetDisplayName(DatabaseInterface database, CancellationToken token = default) => new ValueTask<string>("Console");
    public ValueTask<string?> GetProfilePictureURL(DatabaseInterface database, CancellationToken token = default)
        => UCWarfare.IsLoaded ? new ValueTask<string?>(Provider.configData.Browser.Icon) : new ValueTask<string?>("https://i.imgur.com/NRZFfKN.png");
    private ConsoleActor() { }
}
public class AntiCheatActor : IModerationActor
{
    public static IModerationActor Instance { get; } = new AntiCheatActor();
    public ulong Id => 1;
    bool IModerationActor.Async => false;
    public ValueTask<string> GetDisplayName(DatabaseInterface database, CancellationToken token = default) => new ValueTask<string>("Anti-Cheat");
    public ValueTask<string?> GetProfilePictureURL(DatabaseInterface database, CancellationToken token = default) => ConsoleActor.Instance.GetProfilePictureURL(database, token);
    private AntiCheatActor() { }
}
public class BattlEyeActor : IModerationActor
{
    public static IModerationActor Instance { get; } = new BattlEyeActor();
    public ulong Id => 2;
    bool IModerationActor.Async => false;
    public ValueTask<string> GetDisplayName(DatabaseInterface database, CancellationToken token = default) => new ValueTask<string>("BattlEye");
    public ValueTask<string?> GetProfilePictureURL(DatabaseInterface database, CancellationToken token = default) => new ValueTask<string?>("https://i.imgur.com/jasTgpD.jpg");
    private BattlEyeActor() { }
}

public readonly struct RelatedActor
{
    public const string RolePrimaryAdmin = "Primary Admin";
    public string Role { get; }
    public bool AsAdmin { get; }
    public IModerationActor Actor { get; }
    public RelatedActor(string role, bool asAdmin, IModerationActor actor)
    {
        Role = role;
        AsAdmin = asAdmin;
        Actor = actor;
    }
}