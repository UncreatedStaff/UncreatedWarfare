using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Layouts;
public class LayoutFactory : IHostedService
{
    private readonly WarfareModule _warfare;
    public LayoutFactory(WarfareModule warfare)
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

        if (!_warfare.IsLayoutActive())
            return;
        
        Layout layout = _warfare.GetActiveLayout();
        if (layout.IsActive)
        {
            await layout.EndLayoutAsync(CancellationToken.None);
        }

        layout.Dispose();
        _warfare.SetActiveLayout(null);
    }

    private void OnLevelLoaded(int level)
    {
        if (level != Level.BUILD_INDEX_GAME)
            return;

        UniTask.Create(async () =>
        {
            try
            {
                await StartNextLayout(_warfare.UnloadToken);
            }
            catch (Exception ex)
            {
                L.LogError("Error starting layout.");
                L.LogError(ex);
                _ = _warfare.ShutdownAsync();
            }
        });
    }

    /// <summary>
    /// Forcibly end the current layout and create a new random layout from the config files.
    /// </summary>
    public async UniTask StartNextLayout(CancellationToken token = default)
    {
        await UniTask.SwitchToThreadPool();
        
        LayoutInfo newLayout = SelectRandomLayouts();

        await UniTask.SwitchToMainThread(token);

        if (_warfare.IsLayoutActive())
        {
            Layout oldLayout = _warfare.GetActiveLayout();
            if (oldLayout.IsActive)
            {
                await oldLayout.EndLayoutAsync(CancellationToken.None);
            }

            oldLayout.Dispose();
            _warfare.SetActiveLayout(null);
        }
        else
        {
            // invoke ILevelHostedService
            await _warfare.InvokeLevelLoaded(CancellationToken.None);
        }

        await CreateLayoutAsync(newLayout, CancellationToken.None);
    }

    /// <summary>
    /// Actually creates a new layout with <paramref name="layoutInfo"/> as it's startup args.
    /// </summary>
    public async UniTask CreateLayoutAsync(LayoutInfo layoutInfo, CancellationToken token = default)
    {
        if (!typeof(Layout).IsAssignableFrom(layoutInfo.LayoutType))
        {
            throw new ArgumentException($"Type {Accessor.ExceptionFormatter.Format(layoutInfo.LayoutType)} is not assignable to Layout.", nameof(layoutInfo));
        }

        ILifetimeScope scopedProvider = await _warfare.CreateScopeAsync(null, token);
        await UniTask.SwitchToMainThread(token);

        Layout layout = (Layout)Activator.CreateInstance(layoutInfo.LayoutType, [ scopedProvider, layoutInfo ]);
        _warfare.SetActiveLayout(layout);

        using CombinedTokenSources tokens = token.CombineTokensIfNeeded(layout.UnloadToken);
        await layout.InitializeLayoutAsync(token);
        await layout.BeginLayoutAsync(token);
    }

    /// <summary>
    /// Read all layouts and select a random one.
    /// </summary>
    /// <remarks>Reading them each time keeps us from having to reload config.</remarks>
    /// <exception cref="InvalidOperationException">No layouts are configured.</exception>
    public LayoutInfo SelectRandomLayouts()
    {
        List<LayoutInfo> layouts = GetBaseLayoutFiles()
            .Select(x => ReadLayoutInfo(x.FullName))
            .Where(x => x != null)
            .ToList()!;

        if (layouts.Count == 0)
        {
            throw new InvalidOperationException("There are no layouts configured.");
        }

        int index = RandomUtility.GetIndex(layouts, x => x.Weight);
        return layouts[index];
    }

    /// <summary>
    /// Read a <see cref="LayoutInfo"/> from the given file and open a configuration root for the file.
    /// </summary>
    public LayoutInfo? ReadLayoutInfo(string file)
    {
        if (!File.Exists(file))
            return null;

        ConfigurationBuilder configBuilder = new ConfigurationBuilder();
        ConfigurationHelper.AddSourceWithMapOverride(configBuilder, file);
        IConfigurationRoot root = configBuilder.Build();

        // read the full type name from the config file
        string? layoutTypeName = root["Type"];
        if (layoutTypeName == null)
        {
            L.LogDebug($"Layout config file missing \"Type\" config value in \"{file}\".");
            return null;
        }

        Type? layoutType = ContextualTypeResolver.ResolveType(layoutTypeName, typeof(Layout));
        if (layoutType == null)
        {
            L.LogError($"Unknown layout type \"{layoutTypeName}\" in layout config \"{file}\".");
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

        return new LayoutInfo
        {
            LayoutType = layoutType,
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
    /// Host a new layout, starting all <see cref="ILayoutHostedService"/> services. Should only be called from <see cref="Layout.BeginLayoutAsync"/>.
    /// </summary>
    /// <exception cref="OperationCanceledException"/>
    /// <exception cref="Exception"/>
    internal async UniTask HostLayoutAsync(Layout layout, CancellationToken token)
    {
        // start any services implementing ILayoutHostedService
        List<ILayoutHostedService> hostedServices = layout.ServiceProvider
            .Resolve<IEnumerable<ILayoutHostedService>>()
            .OrderByDescending(x => x.GetType().GetPriority())
            .ToList();

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
                L.LogWarning($"Layout {layout} canceled.");
                errIndex = i;
                thrownException = ex;
            }
            catch (Exception ex)
            {
                L.LogError($"Error hosting service {Accessor.Formatter.Format(hostedServices[i].GetType())} in layout {layout}.");
                errIndex = i;
                thrownException = ex;
            }
        }

        // handles if a service failed to start up, unloads the services that did start up and ends the layout.
        if (errIndex == -1)
            return;
        
        await UniTask.SwitchToMainThread();

        if (!layout.UnloadedHostedServices)
        {
            UniTask[] tasks = new UniTask[errIndex];
            for (int i = errIndex - 1; i >= 0; --i)
            {
                ILayoutHostedService hostedService = hostedServices[i];
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

            layout.UnloadedHostedServices = true;

            try
            {
                await UniTask.WhenAll(tasks);
            }
            catch
            {
                await UniTask.SwitchToMainThread();
                L.LogError($"Errors encountered while ending layout {layout}:");
                FormattingUtility.PrintTaskErrors(tasks, hostedServices);
            }
        }

        await layout.EndLayoutAsync(CancellationToken.None);

        if (thrownException is OperationCanceledException && token.IsCancellationRequested)
            ExceptionDispatchInfo.Capture(thrownException).Throw();

        throw new Exception($"Failed to load layout {layout}.", thrownException);
    }

    /// <summary>
    /// Stop hosting a layout, stopping all <see cref="ILayoutHostedService"/> services. Should only be called from <see cref="Layout.EndLayoutAsync"/>.
    /// </summary>
    internal async UniTask UnhostLayoutAsync(Layout layout, CancellationToken token)
    {
        if (layout.UnloadedHostedServices)
            return;

        // stop any services implementing ILayoutHostedService
        List<ILayoutHostedService> hostedServices = layout.ServiceProvider
            .Resolve<IEnumerable<ILayoutHostedService>>()
            .OrderByDescending(x => x.GetType().GetPriority())
            .ToList();

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

        layout.UnloadedHostedServices = true;

        try
        {
            await UniTask.WhenAll(tasks);
        }
        catch
        {
            L.LogError($"Errors encountered while ending layout {layout}:");
            FormattingUtility.PrintTaskErrors(tasks, hostedServices);
        }
    }
}
