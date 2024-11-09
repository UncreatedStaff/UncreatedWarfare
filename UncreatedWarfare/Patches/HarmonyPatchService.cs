#if DEBUG
#define REFLECTION_TOOLS_ENABLE_HARMONY_LOG
#endif
using DanielWillett.ReflectionTools;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Plugins;
using Uncreated.Warfare.Util;
using Module = SDG.Framework.Modules.Module;

namespace Uncreated.Warfare.Patches;

[Priority(int.MaxValue)]
public class HarmonyPatchService
{
    private List<Assembly>? _patchableAssemblies;
    private List<IHarmonyPatch>? _appliedPatches;

    private readonly ILogger<HarmonyPatchService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILoggerFactory _loggerFactory;
    public HarmonyLib.Harmony Patcher { get; set; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public HarmonyPatchService(Module module, WarfarePluginLoader pluginLoader, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
    {
        _logger = loggerFactory.CreateLogger<HarmonyPatchService>();
        _loggerFactory = loggerFactory;
        _serviceProvider = serviceProvider;
        Patcher = new HarmonyLib.Harmony("network.uncreated.warfare");

        // add all module dependencies, all plugin dlls, and the module itself.
        _patchableAssemblies = new List<Assembly>(module.assemblies);

        _patchableAssemblies.AddRange(pluginLoader.Plugins.Select(x => x.LoadedAssembly));

        _patchableAssemblies.Add(Assembly.GetExecutingAssembly());

        for (int i = _patchableAssemblies.Count - 1; i >= 0; --i)
        {
            Assembly asm = _patchableAssemblies[i];
            for (int j = i - 1; j >= 0; --j)
            {
                if (asm == _patchableAssemblies[j])
                    _patchableAssemblies.RemoveAt(j);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ApplyAllPatches()
    {
        GameThread.AssertCurrent();

        if (_patchableAssemblies is not { Count: > 0 })
            return;

        HarmonyLog.ResetConditional(Path.Combine(UnturnedPaths.RootDirectory.FullName, "Logs", "harmony.log"));

        Assembly asm = Assembly.GetExecutingAssembly();

        _appliedPatches = new List<IHarmonyPatch>(64);

        foreach (Assembly patchableAssembly in _patchableAssemblies)
        {
            foreach (Type type in Accessor.GetTypesSafe(patchableAssembly))
            {
                // not visible or abstract/interface or not IHarmonyPatch
                if (!(type.IsPublic || type.Assembly == asm && type.GetVisibility() is MemberVisibility.Internal or MemberVisibility.ProtectedInternal)
                    || type.IsAbstract
                    || !typeof(IHarmonyPatch).IsAssignableFrom(type))
                {
                    continue;
                }

                IHarmonyPatch patch = (IHarmonyPatch)ActivatorUtilities.CreateInstance(_serviceProvider, type);
                try
                {
                    patch.Patch(_loggerFactory.CreateLogger(type), Patcher);
                    _appliedPatches.Add(patch);

                    _logger.LogDebug("Applied harmony patch: {0}.", type);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to patch {0}.", type);
                    try
                    {
                        if (patch is IDisposable disposable)
                            disposable.Dispose();
                    }
                    catch (Exception ex2)
                    {
                        _logger.LogError(ex2, "Failed to dispose {0}.", type);
                    }
                }
            }
        }

        _patchableAssemblies.Clear();
        _patchableAssemblies = null;
    }

    public void RemoveAllPatches()
    {
        GameThread.AssertCurrent();

        if (_appliedPatches == null)
            return;

        foreach (IHarmonyPatch patch in _appliedPatches)
        {
            Type patchType = patch.GetType();
            try
            {
                patch.Unpatch(_loggerFactory.CreateLogger(patchType), Patcher);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispose {0}.", patchType);
            }
        }
        
        foreach (IHarmonyPatch patch in _appliedPatches)
        {
            if (patch is not IDisposable disposable)
                continue;

            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispose {0}.", patch.GetType());
            }
        }

        _logger.LogDebug("Unapplied {0} harmony patches.", _appliedPatches.Count);
        _appliedPatches.Clear();
        _appliedPatches = null;
    }
}
