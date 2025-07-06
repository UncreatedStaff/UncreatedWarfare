using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;

namespace Uncreated.Warfare.Discord;

[GenerateRpcSource]
public partial class DiscordUserService
{
    /// <summary>
    /// Check if a user is a member of the Uncreated Warfare guild.
    /// </summary>
    [RpcSend, RpcTimeout(10 * Timeouts.Seconds)]
    public partial RpcTask<bool> IsMemberOfGuild(ulong discordId);
}