using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.SQL;

namespace Uncreated.Warfare.Moderation;
public interface IModerationActor
{
    ulong Id { get; }
    ValueTask<string> GetDisplayName(CancellationToken token = default);
}
public interface IPlayerActor : IModerationActor
{
    bool FromDiscord { get; }
}

public class WarfarePlayerActor : IPlayerActor
{
    public ulong Id { get; set; }
    public bool FromDiscord { get; set; }
    public virtual async ValueTask<string> GetDisplayName(CancellationToken token = default)
    {
        return (await F.GetPlayerOriginalNamesAsync(Id, token).ConfigureAwait(false)).PlayerName;
    }
}
public class ConsoleActor : IModerationActor
{
    public ulong Id => 0;
    public ValueTask<string> GetDisplayName(CancellationToken token = default) => new ValueTask<string>("Console");
}
public class AntiCheatActor : IModerationActor
{
    public ulong Id => 1;
    public ValueTask<string> GetDisplayName(CancellationToken token = default) => new ValueTask<string>("Anti-Cheat");
}
public class BattlEyeActor : IModerationActor
{
    public ulong Id => 2;
    public ValueTask<string> GetDisplayName(CancellationToken token = default) => new ValueTask<string>("BattlEye");
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