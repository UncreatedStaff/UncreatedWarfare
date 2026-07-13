using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Players.Permissions;

namespace Uncreated.Warfare.Tweaks;

/// <summary>
/// Enforces the test:join_permission config, not allowing players without a specific permission to join.
/// </summary>
internal sealed class JoinPermissionTweak : IAsyncEventListener<PlayerPending>, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly IDisposable _changeToken;

    private PermissionLeaf _joinPermission;

    public JoinPermissionTweak(IConfiguration configuration)
    {
        _configuration = configuration;
        _changeToken = ChangeToken.OnChange(_configuration.GetReloadToken, OnConfigUpdated);
        OnConfigUpdated();
    }

    private void OnConfigUpdated()
    {
        string? p = _configuration["tests:join_permission"];
        _joinPermission = string.IsNullOrWhiteSpace(p) ? default : new PermissionLeaf(p);
    }

    UniTask IAsyncEventListener<PlayerPending>.HandleEventAsync(PlayerPending e, IServiceProvider serviceProvider, CancellationToken token)
    {
        PermissionLeaf permission = _joinPermission;
        if (!permission.Valid)
        {
            return UniTask.CompletedTask;
        }

        return Core(e, permission, serviceProvider, token);

        static async UniTask Core(PlayerPending e, PermissionLeaf permission, IServiceProvider serviceProvider, CancellationToken token)
        {
            UserPermissionStore permStore = serviceProvider.GetRequiredService<UserPermissionStore>();
            if (!await permStore.HasPermissionAsync(e, permission, token))
            {
                e.RejectReason = "The server is currently restricted to specific players. Try again later.";
                e.Cancel();
                permStore.ClearCachedPermissions(e.Steam64.m_SteamID);
            }
        }
    }

    public void Dispose()
    {
        _changeToken.Dispose();
    }
}