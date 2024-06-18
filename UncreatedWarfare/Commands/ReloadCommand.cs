using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Commands;

[Command("reload")]
[HelpMetadata(nameof(GetHelpMetadata))]
public class ReloadCommand : IExecutableCommand
{
    private readonly UserPermissionStore _permissions;
    public static event VoidDelegate OnTranslationsReloaded;
    public static event VoidDelegate OnFlagsReloaded;

    public static Dictionary<string, IConfigurationHolder> ReloadableConfigs = new Dictionary<string, IConfigurationHolder>(StringComparer.InvariantCultureIgnoreCase);

    private static readonly PermissionLeaf PermissionReloadTranslations = new PermissionLeaf("commands.reload.translations", unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionReloadFlags        = new PermissionLeaf("commands.reload.flags",        unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionReloadPermissions  = new PermissionLeaf("commands.reload.permissions",  unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionReloadColors       = new PermissionLeaf("commands.reload.colors",       unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionReloadHomebase     = new PermissionLeaf("commands.reload.homebase",     unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionReloadDatabase     = new PermissionLeaf("commands.reload.database",     unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionReloadTeams        = new PermissionLeaf("commands.reload.teams",        unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionReloadModules      = new PermissionLeaf("commands.reload.module",       unturned: false, warfare: true);

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    public ReloadCommand(UserPermissionStore permissions)
    {
        _permissions = permissions;
    }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Reload a part of Uncreated Warfare.",
            Parameters =
            [
                new CommandParameter("Translations")
                {
                    Permission = PermissionReloadTranslations,
                    Aliases = [ "lang" ],
                },
                new CommandParameter("Flags")
                {
                    Permission = PermissionReloadFlags
                },
                new CommandParameter("Permissions")
                {
                    Permission = PermissionReloadPermissions,
                },
                new CommandParameter("Colors")
                {
                    Permission = PermissionReloadColors,
                },
                new CommandParameter("Homebase")
                {
                    Permission = PermissionReloadHomebase,
                    Aliases = [ "tcp" ]
                },
                new CommandParameter("SQL")
                {
                    Permission = PermissionReloadDatabase
                },
                new CommandParameter("Teams")
                {
                    Aliases = [ "factions" ],
                    Permission = PermissionReloadTeams
                },
                new CommandParameter("Module", typeof(string))
                {
                    Description = "Reload a module with a reload key.",
                    Permission = PermissionReloadModules,
                }
            ]
        };
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Context.Caller.IsSuperUser)
            Context.AssertOnDuty();

        if (!Context.TryGet(0, out string module))
            throw Context.SendCorrectUsage("/reload <module>");

        if (module.Equals("help", StringComparison.InvariantCultureIgnoreCase))
            throw Context.SendCorrectUsage("/reload <module> - Reload a part of Uncreated Warfare.");

        if (module.Equals("translations", StringComparison.InvariantCultureIgnoreCase) || module.Equals("lang", StringComparison.InvariantCultureIgnoreCase))
        {
            await Context.AssertPermissions(PermissionReloadTranslations, token);

            await ReloadTranslations(token).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(token);
            Context.Reply(T.ReloadedTranslations, Localization.TotalDefaultTranslations);
            Context.LogAction(ActionLogType.ReloadComponent, "TRANSLATIONS");
        }
        else if (module.Equals("flags", StringComparison.InvariantCultureIgnoreCase))
        {
            await Context.AssertPermissions(PermissionReloadFlags, token);
            await UniTask.SwitchToMainThread(token);

            ReloadFlags();
            Context.Reply(T.ReloadedFlags);
            Context.LogAction(ActionLogType.ReloadComponent, "FLAGS");
        }
        else if (module.Equals("permissions", StringComparison.InvariantCultureIgnoreCase))
        {
            await Context.AssertPermissions(PermissionReloadPermissions, token);
            await UniTask.SwitchToMainThread(token);

            _permissions.ClearCachedPermissions(0ul);
            Context.Reply(T.ReloadedPermissions);
            Context.LogAction(ActionLogType.ReloadComponent, "PERMISSIONS");
        }
        else if (module.Equals("colors", StringComparison.InvariantCultureIgnoreCase))
        {
            await Context.AssertPermissions(PermissionReloadColors, token);
            await UniTask.SwitchToMainThread(token);

            ReloadColors();
            Context.Reply(T.ReloadedGeneric, "colors");
            Context.LogAction(ActionLogType.ReloadComponent, "COLORS");
        }
        else if (module.Equals("homebase", StringComparison.InvariantCultureIgnoreCase)
                 || module.Equals("tcp", StringComparison.InvariantCultureIgnoreCase))
        {
            await Context.AssertPermissions(PermissionReloadHomebase, token);
            await UniTask.SwitchToMainThread(token);

            await ReloadHomebase(token);
            Context.LogAction(ActionLogType.ReloadComponent, "TCP SERVER");
        }
        else if (module.Equals("sql", StringComparison.InvariantCultureIgnoreCase))
        {
            await Context.AssertPermissions(PermissionReloadDatabase, token);
            await UniTask.SwitchToMainThread(token);

            await ReloadSQLServer(token);
            Context.LogAction(ActionLogType.ReloadComponent, "MYSQL CONNECTION");
        }
        else if (module.Equals("teams", StringComparison.InvariantCultureIgnoreCase) || module.Equals("factions", StringComparison.InvariantCultureIgnoreCase))
        {
            await Context.AssertPermissions(PermissionReloadTeams, token);
            await UniTask.SwitchToMainThread(token);

            await TeamManager.ReloadFactions(token).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(token);
            TeamManager.SetupConfig();
            Context.Reply(T.ReloadedGeneric, "teams and factions");
            Context.LogAction(ActionLogType.ReloadComponent, "TEAMS & FACTIONS");
        }
        else
        {
            module = module.ToLowerInvariant();

            await Context.AssertPermissions(new PermissionLeaf("commands.reload.module." + module), token);
            await UniTask.SwitchToMainThread(token);

            if (ReloadableConfigs.TryGetValue(module, out IConfigurationHolder config))
            {
                config.Reload();
                Context.Reply(T.ReloadedGeneric, module);
                Context.LogAction(ActionLogType.ReloadComponent, module.ToUpperInvariant());
            }
            else
            {
                IReloadableSingleton? reloadable = await Data.Singletons.ReloadSingletonAsync(module, token).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(token);

                if (reloadable is null)
                    throw Context.SendCorrectUsage("/reload <module>");

                Context.Reply(T.ReloadedGeneric, module);
                Context.LogAction(ActionLogType.ReloadComponent, module.ToUpperInvariant());
            }
        }
    }

    private static void ReloadColors()
    {
        Data.Colors = JSONMethods.LoadColors(out Data.ColorsHex);
        Translation.OnColorsReloaded();
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            UCWarfare.I.UpdateLangs(PlayerManager.OnlinePlayers[i], false);
    }
    internal static async Task ReloadTranslations(CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);
        try
        {
            await Data.ReloadLanguageDataStore(false, token).ConfigureAwait(false);
            
            Data.Colors = JSONMethods.LoadColors(out Data.ColorsHex);
            Deaths.Localization.Reload();
            Localization.ReadEnumTranslations(Data.TranslatableEnumTypes);
            await Translation.ReadTranslations(token);
            foreach (UCPlayer player in PlayerManager.OnlinePlayers.ToArray())
                player.Locale.Preferences = await Data.LanguageDataStore.GetLanguagePreferences(player.Steam64, token).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(token);
            OnTranslationsReloaded?.Invoke();
        }
        catch (Exception ex)
        {
            L.LogError("Execption when reloading translations.");
            L.LogError(ex);
        }
    }
    internal static void ReloadGamemodeConfig()
    {
        Gamemode.ConfigObj.Reload();
    }
    internal static void ReloadFlags()
    {
        try
        {
            Gamemode.ConfigObj.Reload();
            if (Data.Gamemode is FlagGamemode flaggm)
                flaggm.LoadAllFlags();
            else
            {
                ZoneList? zl = Data.Singletons.GetSingleton<ZoneList>();
                if (zl != null)
                {
                    UCWarfare.RunTask(async () =>
                    {
                        await zl.DownloadAll();
                        await UCWarfare.ToUpdate();
                        TeamManager.OnReloadFlags();
                    }, ctx: "Reload flags");
                }
            }
            Data.ExtraPoints = JSONMethods.LoadExtraPoints();
            OnFlagsReloaded?.Invoke();
        }
        catch (Exception ex)
        {
            L.LogError("Execption when reloading flags.");
            L.LogError(ex);
        }
    }
    internal static void ReloadAllConfigFiles()
    {
        try
        {
            _ = Data.Singletons.ReloadSingletonAsync("announcer");
            IEnumerable<FieldInfo> objects = typeof(Data).GetFields(BindingFlags.Static | BindingFlags.Public).Where(x => x.FieldType.IsClass);
            foreach (FieldInfo obj in objects)
            {
                try
                {
                    object o = obj.GetValue(null);
                    IEnumerable<FieldInfo> configfields = o.GetType()
                        .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                   BindingFlags.Static).Where(x =>
                            x.FieldType.GetInterfaces().Contains(typeof(IConfigurationHolder)));
                    foreach (FieldInfo config in configfields)
                    {
                        IConfigurationHolder c;
                        if (config.IsStatic)
                        {
                            c = (IConfigurationHolder)config.GetValue(null);
                        }
                        else
                        {
                            c = (IConfigurationHolder)config.GetValue(o);
                        }

                        c.Reload();
                    }
                }
                catch (Exception ex)
                {
                    L.LogError("Error reloading config file.");
                    L.LogError(ex);
                }
            }
        }
        catch (Exception ex)
        {
            L.LogError("Failed to find all objects in type " + nameof(Data));
            L.LogError(ex);
        }
    }
    internal async UniTask ReloadHomebase(CancellationToken token)
    {
        if (Data.RpcConnection is { IsClosed: false })
        {
            await Data.RpcConnection.CloseAsync(token);
        }

        if (await HomebaseConnector.ConnectAsync(CancellationToken.None))
        {
            Context.Reply(T.ReloadedTCP);
        }
        else
        {
            Context.SendUnknownError();
        }
    }
    internal async UniTask ReloadSQLServer(CancellationToken token = default)
    {
        L.Log("Reloading SQL...");
        List<SemaphoreSlim> players = PlayerManager.GetAllSemaphores();
        try
        {
            List<Task> tasks = new List<Task>(players.Count);
            for (int i = 0; i < players.Count; ++i)
                tasks.Add(players[i].WaitAsync(token));
            await Task.WhenAll(tasks).ConfigureAwait(false);
            await UCWarfare.ToUpdate();
            // not async intentionally, we want it to do all this within a frame
            Data.DatabaseManager.CloseAsync(token).Wait(token);
            Data.DatabaseManager.Dispose();
            Data.DatabaseManager = new WarfareSQL(UCWarfare.Config.SQL);
            if (Data.DatabaseManager.OpenAsync(token).Result)
                L.Log("Local database reopened");
            else
                L.LogError("Local database failed to reopen.");
            if (Data.RemoteSQL != null)
            {
                await UniTask.SwitchToMainThread();
                Data.RemoteSQL.CloseAsync(token).Wait(token);
                Data.RemoteSQL.Dispose();
                if (UCWarfare.Config.RemoteSQL != null)
                {
                    Data.RemoteSQL = new WarfareSQL(UCWarfare.Config.RemoteSQL);
                    if (Data.RemoteSQL.OpenAsync(token).Result)
                        L.Log("Remote database reopened");
                    else
                        L.LogError("Remote database failed to reopen.");
                }
            }
            Context?.Reply(T.ReloadedSQL);
        }
        catch (Exception ex)
        {
            L.LogError("Failed to reload SQL.");
            L.LogError(ex);
            Context?.Reply(T.UnknownError);
        }
        finally
        {
            for (int i = 0; i < players.Count; ++i)
                players[i].Release();
            L.Log("Reload operation complete.");
        }
    }
    internal static void DeregisterConfigForReload(string reloadKey)
    {
        ReloadableConfigs.Remove(reloadKey);
    }
    internal static bool RegisterConfigForReload<TData>(IConfigurationHolder<TData> config) where TData : JSONConfigData, new()
    {
        if (config is null) return false;
        if (ReloadableConfigs.TryGetValue(config.ReloadKey!, out IConfigurationHolder config2))
            return ReferenceEquals(config, config2);
        ReloadableConfigs.Add(config.ReloadKey!, config);
        return true;
    }
}