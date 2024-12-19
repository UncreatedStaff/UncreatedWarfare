using SDG.NetPak;
using System;
using System.Reflection;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Players.Extensions;

/// <summary>
/// Creates a <see cref="ServerInvocationContext"/> for a player that is meant to be used for RequestXxx methods.
/// </summary>
public static class PlayerServerInvocationContextExtensions
{
    /// <summary>
    /// Creates a <see cref="ServerInvocationContext"/> for a player that is meant to be used for RequestXxx methods.
    /// </summary>
    public static ref readonly ServerInvocationContext CreateServerInvocationContext(this WarfarePlayer player)
    {
        GameThread.AssertCurrent();

        ServerInvocationContextComponent comp = player.Component<ServerInvocationContextComponent>();
        if (comp.Failed)
            throw new InvalidOperationException("Unable to find ServerInvocationContext constructor.");

        return ref comp.InvocationContext;
    }

    [PlayerComponent]
    private sealed class ServerInvocationContextComponent : IPlayerComponent
    {
        private static readonly NetPakReader Reader;

        private static readonly ConstructorInfo? CreateInvocationContextCtor = typeof(ServerInvocationContext).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any,
            [
                typeof(ServerInvocationContext.EOrigin), typeof(SteamPlayer), typeof(NetPakReader),
                typeof(ServerMethodInfo)
            ], null);

        static ServerInvocationContextComponent()
        {
            Reader = new NetPakReader();
            Reader.SetBuffer(Array.Empty<byte>());
        }

        internal ServerInvocationContext InvocationContext;
        internal bool Failed;

        public required WarfarePlayer Player { get; set; }

        public void Init(IServiceProvider serviceProvider, bool isOnJoin)
        {
            if (!isOnJoin)
                return;

            if (CreateInvocationContextCtor == null)
            {
                Failed = true;
                return;
            }

            InvocationContext = (ServerInvocationContext)CreateInvocationContextCtor
                .Invoke([ServerInvocationContext.EOrigin.Obsolete, Player.SteamPlayer, Reader, new ServerMethodInfo()]);
        }
    }
}
