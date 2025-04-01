using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;

namespace Uncreated.Warfare.Discord;

[RpcClass]
public class DiscordUserService
{
    /// <summary>
    /// Check if a user is a member of the Uncreated Warfare guild.
    /// </summary>
    [RpcSend, RpcTimeout(10 * Timeouts.Seconds)]
    public virtual RpcTask<bool> IsMemberOfGuild(ulong discordId) => RpcTask<bool>.NotImplemented;
}