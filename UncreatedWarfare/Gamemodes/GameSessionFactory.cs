using Cysharp.Threading.Tasks;
using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Gamemodes;
public class GameSessionFactory : IHostedService
{
    private readonly WarfareModule _warfare;
    public GameSessionFactory(WarfareModule warfare)
    {
        _warfare = warfare;
    }

    /// <inheritdoc />
    public UniTask StartAsync(CancellationToken token)
    {
        Level.onPostLevelLoaded += OnLevelLoaded;
        return default;
    }

    /// <inheritdoc />
    public async UniTask StopAsync(CancellationToken token)
    {
        Level.onPostLevelLoaded -= OnLevelLoaded;

        if (!_warfare.IsGameSessionActive())
            return;
        
        GameSession session = _warfare.GetActiveGameSession();
        if (session.IsActive)
        {
            await session.EndSessionAsync(CancellationToken.None);
        }

        session.Dispose();
        _warfare.SetActiveGameSession(null);
    }

    private void OnLevelLoaded(int level)
    {
        if (level != Level.BUILD_INDEX_GAME)
            return;

        UniTask.Create(() => StartNextGameSession(_warfare.UnloadToken));
    }

    /// <summary>
    /// Forcibly end the current game session and create a new random game session from the config files.
    /// </summary>
    public async UniTask StartNextGameSession(CancellationToken token = default)
    {
        await UniTask.SwitchToThreadPool();
        
        GameSessionInfo newSession = SelectRandomGameSession();

        await UniTask.SwitchToMainThread(token);

        if (_warfare.IsGameSessionActive())
        {
            GameSession session = _warfare.GetActiveGameSession();
            if (session.IsActive)
            {
                await session.EndSessionAsync(CancellationToken.None);
            }

            session.Dispose();
            _warfare.SetActiveGameSession(null);
        }

        await CreateGameSessionAsync(newSession, token);
    }

    /// <summary>
    /// Actually creates a new game session with <paramref name="sessionInfo"/> as it's startup args.
    /// </summary>
    public async UniTask CreateGameSessionAsync(GameSessionInfo sessionInfo, CancellationToken token = default)
    {
        if (!typeof(GameSession).IsAssignableFrom(sessionInfo.GameSessionType))
        {
            throw new ArgumentException($"Type {Accessor.ExceptionFormatter.Format(sessionInfo.GameSessionType)} is not assignable to GameSession.", nameof(sessionInfo));
        }

        IServiceProvider scopedProvider = await _warfare.CreateScopeAsync(token);
        await UniTask.SwitchToMainThread(token);

        GameSession session = (GameSession)ActivatorUtilities.CreateInstance(scopedProvider, sessionInfo.GameSessionType, [sessionInfo]);
        _warfare.SetActiveGameSession(session);

        using CombinedTokenSources tokens = token.CombineTokensIfNeeded(session.UnloadToken);
        await session.InitializeSessionAsync(token);
        await session.BeginSessionAsync(token);
    }

    /// <summary>
    /// Read all game sessions and select a random one.
    /// </summary>
    /// <remarks>Reading them each time keeps us from having to reload config.</remarks>
    /// <exception cref="InvalidOperationException">No layouts are configured.</exception>
    public GameSessionInfo SelectRandomGameSession()
    {
        List<GameSessionInfo> sessions = GetBaseLayoutFiles()
            .Select(x => ReadGameSessionInfo(x.FullName))
            .Where(x => x != null)
            .ToList()!;

        if (sessions.Count == 0)
        {
            throw new InvalidOperationException("There are no layouts configured.");
        }

        int index = RandomUtility.GetIndex(sessions, x => x.Weight);
        return sessions[index];
    }

    /// <summary>
    /// Read a <see cref="GameSessionInfo"/> from the given file and open a configuration root for the file.
    /// </summary>
    public GameSessionInfo? ReadGameSessionInfo(string file)
    {
        if (!File.Exists(file))
            return null;

        ConfigurationBuilder configBuilder = new ConfigurationBuilder();
        ConfigurationHelper.AddSourceWithMapOverride(configBuilder, file);
        IConfigurationRoot root = configBuilder.Build();

        // read the full type name from the config file
        string? sessionTypeName = root["Type"];
        if (sessionTypeName == null)
        {
            L.LogError($"Layout config file missing \"Type\" config value in \"{file}\".");
            return null;
        }

        Type? sessionType = Type.GetType(sessionTypeName) ?? Assembly.GetExecutingAssembly().GetType(sessionTypeName);
        if (sessionType == null)
        {
            L.LogError($"Unknown session type \"{sessionTypeName}\" in layout config \"{file}\".");
            return null;
        }

        // read the selection weight from the config file
        if (!double.TryParse(root["Weight"], NumberStyles.Number, CultureInfo.InvariantCulture, out double weight))
        {
            weight = 1;
        }

        // read display name
        if (root["Name"] is not { Length: > 0 } displayName)
        {
            displayName = Path.GetFileNameWithoutExtension(file);
        }

        return new GameSessionInfo
        {
            GameSessionType = sessionType,
            Layout = root,
            Weight = weight,
            DisplayName = displayName,
            FilePath = file
        };
    }

    /// <summary>
    /// Get all base config files in the layout folder.
    /// </summary>
    public List<FileInfo> GetBaseLayoutFiles()
    {
        DirectoryInfo layoutDirectory = new DirectoryInfo(Path.Join(_warfare.HomeDirectory, "Layouts"));

        // get all folders or yaml files.
        List<FileSystemInfo> layouts = layoutDirectory
            .GetFileSystemInfos("*", SearchOption.TopDirectoryOnly)
            .Where(x => x is DirectoryInfo || Path.GetExtension(x.FullName).Equals(".yml", StringComparison.OrdinalIgnoreCase))
            .ToList();

        List<FileInfo> baseLayoutConfigs = new List<FileInfo>(layouts.Count);
        foreach (FileSystemInfo layout in layouts)
        {
            switch (layout)
            {
                case FileInfo { Length: > 0 } yamlFile:
                    baseLayoutConfigs.Add(yamlFile);
                    break;

                case DirectoryInfo dir:
                    FileInfo[] files = dir.GetFiles("*.yml", SearchOption.TopDirectoryOnly);
                    if (files.Length == 0)
                        break;

                    // find file with least periods in it's name to get which one doesn't have a map-specific config.
                    baseLayoutConfigs.Add(files.Aggregate((least, next) => next.Name.Count(x => x == '.') < least.Name.Count(x => x == '.') ? next : least));
                    break;
            }
        }

        return baseLayoutConfigs;
    }

    /// <summary>
    /// Host a new session, starting all <see cref="ISessionHostedService"/> services. Should only be called from <see cref="GameSession.BeginSessionAsync"/>.
    /// </summary>
    /// <exception cref="OperationCanceledException"/>
    /// <exception cref="Exception"/>
    internal async UniTask HostSessionAsync(GameSession session, CancellationToken token)
    {
        // start any services implementing ISessionHostedService
        List<ISessionHostedService> hostedServices = session.ServiceProvider.GetServices<ISessionHostedService>().ToList();
        Exception? thrownException = null;
        int errIndex = -1;
        for (int i = 0; i < hostedServices.Count; ++i)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                await UniTask.SwitchToMainThread(token);
                await hostedServices[i].StartAsync(token);
            }
            catch (OperationCanceledException ex) when (token.IsCancellationRequested)
            {
                L.LogWarning($"Layout {session} canceled.");
                errIndex = i;
                thrownException = ex;
            }
            catch (Exception ex)
            {
                L.LogError($"Error hosting service {Accessor.Formatter.Format(hostedServices[i].GetType())} in layout {session}.");
                errIndex = i;
                thrownException = ex;
            }
        }

        // handles if a service failed to start up, unloads the services that did start up and ends the session.
        if (errIndex == -1)
            return;
        
        await UniTask.SwitchToMainThread();

        if (!session.UnloadedHostedServices)
        {
            UniTask[] tasks = new UniTask[errIndex];
            for (int i = errIndex - 1; i >= 0; --i)
            {
                ISessionHostedService hostedService = hostedServices[i];
                try
                {
                    tasks[i] = hostedService.StopAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    L.LogError($"Error stopping service {Accessor.Formatter.Format(hostedService.GetType())}.");
                    L.LogError(ex);
                }
            }

            session.UnloadedHostedServices = true;

            try
            {
                await UniTask.WhenAll(tasks);
            }
            catch
            {
                await UniTask.SwitchToMainThread();
                L.LogError($"Errors encountered while ending layout {session}:");
                FormattingUtility.PrintTaskErrors(tasks, hostedServices);
            }
        }

        await session.EndSessionAsync(CancellationToken.None);

        if (thrownException is OperationCanceledException && token.IsCancellationRequested)
            ExceptionDispatchInfo.Capture(thrownException).Throw();

        throw new Exception($"Failed to load layout {session}.", thrownException);
    }

    /// <summary>
    /// Stop hosting a session, stopping all <see cref="ISessionHostedService"/> services. Should only be called from <see cref="GameSession.EndSessionAsync"/>.
    /// </summary>
    internal async UniTask UnhostSessionAsync(GameSession session, CancellationToken token)
    {
        if (session.UnloadedHostedServices)
            return;

        // stop any services implementing ISessionHostedService
        List<ISessionHostedService> hostedServices = session.ServiceProvider.GetServices<ISessionHostedService>().ToList();
        UniTask[] tasks = new UniTask[hostedServices.Count];
        for (int i = 0; i < tasks.Length; ++i)
        {
            try
            {
                tasks[i] = hostedServices[i].StopAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                tasks[i] = UniTask.FromException(ex);
            }
        }

        session.UnloadedHostedServices = true;

        try
        {
            await UniTask.WhenAll(tasks);
        }
        catch
        {
            L.LogError($"Errors encountered while ending layout {session}:");
            FormattingUtility.PrintTaskErrors(tasks, hostedServices);
        }
    }
}
