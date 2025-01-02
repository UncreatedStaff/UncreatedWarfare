using DanielWillett.ModularRpcs.Routing;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Networking;

namespace Uncreated.Warfare.Commands;

[Command("homebase"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugHomebaseCommand : ICommand;

[Command("reload", "reconnect"), SubCommandOf(typeof(DebugHomebaseCommand))]
internal sealed class DebugHomebaseReloadCommand : IExecutableCommand
{
    private readonly HomebaseConnector _connector;
    private readonly IRpcConnectionLifetime _lifetime;
    public required CommandContext Context { get; init; }

    public DebugHomebaseReloadCommand(HomebaseConnector connector, IRpcConnectionLifetime lifetime)
    {
        _connector = connector;
        _lifetime = lifetime;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        await _connector.StopAsync(token);

        Context.ReplyString("Reconnecting...", "ccffcc");

        await _connector.ConnectAsync(CancellationToken.None).ConfigureAwait(false);

        int ct = _lifetime.ForEachRemoteConnection(_ => false);

        Context.ReplyString(ct > 0 ? "Successfully connected to homebase server." : "Failed to connect to homebase server.",
                            ct > 0 ? "ccffcc" : "ffcc99"
        );
    }
}