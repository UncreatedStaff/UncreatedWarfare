using System;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;

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
    ulong Id { get; }
    ValueTask<string> GetDisplayName(DatabaseInterface database, CancellationToken token = default);
}

public class PlayerActor : IModerationActor
{
    public ulong Id { get; }
    public virtual async ValueTask<string> GetDisplayName(DatabaseInterface database, CancellationToken token = default)
    {
        return (await database.GetUsernames(Id, token)).PlayerName;
    }

    public PlayerActor(ulong id) => Id = id;
}
public class DiscordActor : IModerationActor
{
    public static Func<ulong, CancellationToken, Task<string>>? GetDiscordDisplayNameOverride = null;
    public ulong Id { get; }
    public virtual async ValueTask<string> GetDisplayName(DatabaseInterface database, CancellationToken token = default)
    {
        if (GetDiscordDisplayNameOverride != null)
            return await GetDiscordDisplayNameOverride(Id, token).ConfigureAwait(false);

        ulong steam64 = await database.Sql.GetSteam64(Id, token).ConfigureAwait(false);
        if (steam64 != 0)
            return (await database.GetUsernames(steam64, token).ConfigureAwait(false)).PlayerName;

        return "<@" + Id + ">";
    }

    public DiscordActor(ulong id) => Id = id;
}
public class ConsoleActor : IModerationActor
{
    public static IModerationActor Instance { get; } = new ConsoleActor();
    public ulong Id => 0;
    public ValueTask<string> GetDisplayName(DatabaseInterface database, CancellationToken token = default) => new ValueTask<string>("Console");
    private ConsoleActor() { }
}
public class AntiCheatActor : IModerationActor
{
    public static IModerationActor Instance { get; } = new AntiCheatActor();
    public ulong Id => 1;
    public ValueTask<string> GetDisplayName(DatabaseInterface database, CancellationToken token = default) => new ValueTask<string>("Anti-Cheat");
    private AntiCheatActor() { }
}
public class BattlEyeActor : IModerationActor
{
    public static IModerationActor Instance { get; } = new BattlEyeActor();
    public ulong Id => 2;
    public ValueTask<string> GetDisplayName(DatabaseInterface database, CancellationToken token = default) => new ValueTask<string>("BattlEye");
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