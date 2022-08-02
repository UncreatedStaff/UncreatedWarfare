using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Players;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Teams;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;

public class ReloadCommand : Command
{
    public const string SYNTAX = "/reload [help|module]";
    public const string HELP = "Reload certain parts of UCWarfare.";
    public static event VoidDelegate OnTranslationsReloaded;
    public static event VoidDelegate OnFlagsReloaded;

    public const string RELOAD_ALL_PERMISSION = "uc.reload.all";

    public static Dictionary<string, IConfiguration> ReloadableConfigs = new Dictionary<string, IConfiguration>();

    public ReloadCommand() : base("reload", EAdminType.ADMIN, 1)
    {

    }
    public override void Execute(CommandInteraction ctx)
    {
        if (!ctx.IsConsole && !ctx.Caller.IsAdmin)
            ctx.AssertOnDuty();

        if (!ctx.TryGet(0, out string module))
            throw ctx.SendCorrectUsage(SYNTAX);

        if (module.Equals("help", StringComparison.OrdinalIgnoreCase))
            throw ctx.SendNotImplemented();

        if (module.Equals("translations", StringComparison.OrdinalIgnoreCase) || module.Equals("lang", StringComparison.OrdinalIgnoreCase))
        {
            ReloadTranslations();
            ctx.Reply(T.ReloadedTranslations);
            ctx.LogAction(EActionLogType.RELOAD_COMPONENT, "TRANSLATIONS");
        }
        else if (module.Equals("flags", StringComparison.OrdinalIgnoreCase))
        {
            if (Data.Is<IFlagRotation>())
            {
                ReloadFlags();
                ctx.Reply(T.ReloadedFlags);
                ctx.LogAction(EActionLogType.RELOAD_COMPONENT, "FLAGS");
            }
            else ctx.Reply(T.ReloadFlagsInvalidGamemode);
        }
        else if (module.Equals("permissions", StringComparison.OrdinalIgnoreCase))
        {
            ReloadPermissions();
            ctx.Reply(T.ReloadedPermissions);
            ctx.LogAction(EActionLogType.RELOAD_COMPONENT, "PERMISSIONS");
        }
        else if (module.Equals("colors", StringComparison.OrdinalIgnoreCase))
        {
            ReloadColors();
            ctx.Reply(T.ReloadedGeneric, "colors");
            ctx.LogAction(EActionLogType.RELOAD_COMPONENT, "COLORS");
        }
        else if (module.Equals("tcp", StringComparison.OrdinalIgnoreCase))
        {
            ReloadTCPServer();
            ctx.Reply(T.ReloadedTCP);
            ctx.LogAction(EActionLogType.RELOAD_COMPONENT, "TCP SERVER");
        }
        else if (module.Equals("sql", StringComparison.OrdinalIgnoreCase))
        {
            ReloadSQLServer();
            ctx.Reply(T.ReloadedSQL);
            ctx.LogAction(EActionLogType.RELOAD_COMPONENT, "MYSQL CONNECTION");
        }
        else if (module.Equals("teams", StringComparison.OrdinalIgnoreCase))
        {
            TeamManager.SetupConfig();
            ctx.Reply(T.ReloadedGeneric, "teams and factions");
            ctx.LogAction(EActionLogType.RELOAD_COMPONENT, "TEAMS & FACTIONS");
        }
        else if (module.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            ReloadTranslations();
            ReloadFlags();
            ReloadTCPServer();
            ReloadSQLServer();
            ReloadKits();
            foreach (KeyValuePair<string, IConfiguration> config in ReloadableConfigs)
                config.Value.Reload();

            ctx.Reply(T.ReloadedAll);
            ctx.LogAction(EActionLogType.RELOAD_COMPONENT, "ALL");
        }
        else
        {
            module = module.ToLowerInvariant();
            if (ReloadableConfigs.TryGetValue(module, out IConfiguration config))
            {
                config.Reload();
                ctx.Reply(T.ReloadedGeneric, module.ToProperCase());
                ctx.LogAction(EActionLogType.RELOAD_COMPONENT, module.ToUpperInvariant());
            }
            else
            {
                IReloadableSingleton? reloadable = Data.Singletons.ReloadSingleton(module);
                if (reloadable is null) goto notFound;
                ctx.Reply(T.ReloadedGeneric, module.ToProperCase());
                ctx.LogAction(EActionLogType.RELOAD_COMPONENT, module.ToUpperInvariant());
            }
        }
        return;
    notFound:
        ctx.SendCorrectUsage(SYNTAX);
    }

    private void ReloadColors()
    {
        Data.Colors = JSONMethods.LoadColors(out Data.ColorsHex);
        Translation.OnColorsReloaded();
    }
    public static void ReloadPermissions()
    {
        PermissionSaver.Instance.Read();
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            EAdminType old = pl.PermissionLevel;
            pl.ResetPermissionLevel();
            EAdminType @new = pl.PermissionLevel;
            if (old == @new) continue;
            if (@new is EAdminType.MEMBER or EAdminType.ADMIN_OFF_DUTY or EAdminType.TRIAL_ADMIN_OFF_DUTY)
            {
                switch (old)
                {
                    case EAdminType.ADMIN_ON_DUTY:
                        DutyCommand.AdminOnToOff(pl);
                        break;
                    case EAdminType.TRIAL_ADMIN_ON_DUTY:
                        DutyCommand.InternOnToOff(pl);
                        break;
                }
            }
            else if (@new is EAdminType.TRIAL_ADMIN_ON_DUTY)
            {
                DutyCommand.InternOffToOn(pl);
            }
            else if (@new is EAdminType.ADMIN_ON_DUTY)
            {
                DutyCommand.AdminOffToOn(pl);
            }
        }
    }
    internal static void ReloadTranslations()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        try
        {
            Data.LanguageAliases = JSONMethods.LoadLangAliases();
            Data.Languages = JSONMethods.LoadLanguagePreferences();
            Data.Colors = JSONMethods.LoadColors(out Data.ColorsHex);
            Data.Localization = JSONMethods.LoadTranslations();
            Translation.ReadTranslations();
            Deaths.Localization.Reload();
            Localization.ReadEnumTranslations(Data.TranslatableEnumTypes);
            if (OnTranslationsReloaded != null)
                OnTranslationsReloaded.Invoke();
        }
        catch (Exception ex)
        {
            L.LogError("Execption when reloading translations.");
            L.LogError(ex);
        }
    }
    internal static void ReloadGamemodeConfig()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Gamemode.ConfigObj.Reload();
    }
    internal static void ReloadFlags()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        try
        {
            Gamemode.ConfigObj.Reload();
            if (Data.Gamemode is FlagGamemode flaggm)
                flaggm.LoadAllFlags();
            else
                Data.ZoneProvider.Reload();
            Data.ExtraPoints = JSONMethods.LoadExtraPoints();
            TeamManager.OnReloadFlags();
            if (OnFlagsReloaded != null)
                OnFlagsReloaded.Invoke();
        }
        catch (Exception ex)
        {
            L.LogError("Execption when reloading flags.");
            L.LogError(ex);
        }
    }
    internal static void ReloadKits()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        KitManager manager = SingletonEx.AssertAndGet<KitManager>();
        Task.Run(async () =>
        {
            await manager.ReloadKits();
            await UCWarfare.ToUpdate();
            if (RequestSigns.Loaded)
            {
                RequestSigns.UpdateAllSigns();
            }
            if (!KitManager.KitExists(TeamManager.Team1UnarmedKit, out _))
                L.LogError("Team 1's unarmed kit, \"" + TeamManager.Team1UnarmedKit + "\", was not found, it should be added to \"" + Data.Paths.KitsStorage + "kits.json\".");
            if (!KitManager.KitExists(TeamManager.Team2UnarmedKit, out _))
                L.LogError("Team 2's unarmed kit, \"" + TeamManager.Team2UnarmedKit + "\", was not found, it should be added to \"" + Data.Paths.KitsStorage + "kits.json\".");
            if (!KitManager.KitExists(TeamManager.DefaultKit, out _))
                L.LogError("The default kit, \"" + TeamManager.DefaultKit + "\", was not found, it should be added to \"" + Data.Paths.KitsStorage + "kits.json\".");
        });
    }
    internal static void ReloadAllConfigFiles()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        try
        {
            Data.Singletons.ReloadSingleton("announcer");
            IEnumerable<FieldInfo> objects = typeof(Data).GetFields(BindingFlags.Static | BindingFlags.Public).Where(x => x.FieldType.IsClass);
            foreach (FieldInfo obj in objects)
            {
                try
                {
                    object o = obj.GetValue(null);
                    IEnumerable<FieldInfo> configfields = o.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).
                        Where(x => x.FieldType.GetInterfaces().Contains(typeof(IConfiguration)));
                    foreach (FieldInfo config in configfields)
                    {
                        IConfiguration c;
                        if (config.IsStatic)
                        {
                            c = (IConfiguration)config.GetValue(null);
                        }
                        else
                        {
                            c = (IConfiguration)config.GetValue(o);
                        }
                        c.Reload();
                    }
                }
                catch (Exception) { }
            }
            // FIX: Invocations
            //Invocations.Warfare.SendRankInfo.NetInvoke(XPManager.config.Data.Ranks, OfficerManager.config.Data.OfficerRanks, OfficerManager.config.Data.FirstStarPoints, OfficerManager.config.Data.PointsIncreasePerStar);
        }
        catch (Exception ex)
        {
            L.LogError("Failed to find all objects in type " + typeof(Data).Name);
            L.LogError(ex);
        }
    }
    internal static void ReloadTCPServer()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Data.ReloadTCP();
    }
    internal static void ReloadSQLServer()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Data.DatabaseManager.Close();
        Data.DatabaseManager.Open();
    }
    internal static void DeregisterConfigForReload(string reloadKey)
    {
        ReloadableConfigs.Remove(reloadKey);
    }
    internal static bool RegisterConfigForRelaod<TData>(Config<TData> config) where TData : ConfigData, new()
    {
        if (config is null) return false;
        if (ReloadableConfigs.TryGetValue(config.ReloadKey!, out IConfiguration config2))
            return ReferenceEquals(config, config2);
        ReloadableConfigs.Add(config.ReloadKey!, config);
        return true;
    }
}