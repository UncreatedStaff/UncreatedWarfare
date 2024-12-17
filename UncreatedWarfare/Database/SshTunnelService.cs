using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Configuration;
using Renci.SshNet;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Uncreated.Warfare.Database;

// only initialize if library exists
public static class SshTunnelHelper
{
    public static void AddIfAvailable(ContainerBuilder serviceCollection)
    {
        if (Type.GetType("Renci.SshNet.ISshClient, Renci.SshNet, Version=2024.2.0.1", false) == null)
            return;
        
        serviceCollection.RegisterType(GetServiceType())
            .SingleInstance();
    }

    public static UniTask OpenIfAvailableAsync(ILifetimeScope serviceProvider, CancellationToken token)
    {
        if (Type.GetType("Renci.SshNet.ISshClient, Renci.SshNet", false) == null)
            return UniTask.CompletedTask;

        object? service = serviceProvider.ResolveOptional(GetServiceType());
        if (service != null)
        {
            return OpenAsync(service, token);
        }

        return UniTask.CompletedTask;
    }

    private static UniTask OpenAsync(object service, CancellationToken token)
    {
        SshTunnelService sshTunnel = (SshTunnelService)service;

        return sshTunnel.StartAsync(token);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type GetServiceType() => typeof(SshTunnelService);
}

[Ignore]
public class SshTunnelService : IDisposable
{
    private readonly IConfiguration _systemConfig;
    private readonly ILogger<SshTunnelService> _logger;

    private ISshClient? _sshClient;

    public SshTunnelService(IConfiguration systemConfig, ILogger<SshTunnelService> logger)
    {
        _systemConfig = systemConfig.GetSection("database");
        _logger = logger;
    }

    public async UniTask StartAsync(CancellationToken cancellationToken)
    {
        string? sshFilePath = _systemConfig["ssh_key"];
        if (string.IsNullOrEmpty(sshFilePath) || !File.Exists(sshFilePath))
        {
            _logger.LogInformation("Missing SSH key in settings, not using SSH tunnel.");
            return;
        }

        string? username = _systemConfig["ssh_user"];
        string? host = _systemConfig["ssh_host"];
        const ushort port = 22;
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(host))
        {
            _logger.LogWarning("Missing SSH username or host in settings, not using SSH tunnel.");
            return;
        }

        PrivateKeyFile keyFile;
        using (FileStream sshFile = new FileStream(sshFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            keyFile = new PrivateKeyFile(sshFile);
        }

        PrivateKeyAuthenticationMethod method = new PrivateKeyAuthenticationMethod(username, keyFile);
        ConnectionInfo connectionInfo = new ConnectionInfo(host, port, username, method);

        _logger.LogInformation("SSH tunnel opened to {0}@{1}:{2}", username, host, port);

        _sshClient = new SshClient(connectionInfo);
        await _sshClient.ConnectAsync(cancellationToken);

        ForwardedPortLocal fwd = new ForwardedPortLocal("127.0.0.1", 3306, "127.0.0.1", 3306);
        _sshClient.AddForwardedPort(fwd);
        fwd.Start();

        _logger.LogInformation("Forwarded port 3306");
    }

    public void Dispose()
    {
        _sshClient?.Dispose();
    }
}