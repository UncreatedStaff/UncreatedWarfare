using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Events.Items;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes.Flags;

namespace Uncreated.Warfare.Singletons;

public interface IUncreatedSingleton
{
    bool LoadAsynchrounously { get; }
    bool AwaitLoad { get; }
    bool IsLoaded { get; }
    bool IsLoading { get; }
    bool IsUnloading { get; }
    Task LoadAsync(CancellationToken token);
    Task UnloadAsync(CancellationToken token);
    void Load();
    void Unload();
}
public interface ILevelStartListener
{
    void OnLevelReady();
}
public interface ILevelStartListenerAsync
{
    Task OnLevelReady(CancellationToken token);
}
public interface IQuestCompletedHandler
{
    /// <returns>Whether the quest was handled and execution should be stopped.</returns>
    void OnQuestCompleted(QuestCompleted e);
}
public interface IQuestCompletedHandlerAsync
{
    /// <returns>Whether the quest was handled and execution should be stopped.</returns>
    Task OnQuestCompleted(QuestCompleted e, CancellationToken token);
}
public interface IQuestCompletedListener
{
    void OnQuestCompleted(QuestCompleted e);
}
public interface IQuestCompletedListenerAsync
{
    Task OnQuestCompleted(QuestCompleted e, CancellationToken token);
}
public interface ITimeSyncListener
{
    void TimeSync(float time);
}
public interface IGameTickListener
{
    void Tick();
}
public interface ITCPConnectedListener
{
    Task OnConnected(CancellationToken token);
}
public interface IDeclareWinListener
{
    void OnWinnerDeclared(ulong winner);
}
public interface IStagingPhaseOverListener
{
    void OnStagingPhaseOver();
}
public interface IDeclareWinListenerAsync
{
    Task OnWinnerDeclared(ulong winner, CancellationToken token);
}
public interface IGameStartListener
{
    void OnGameStarting(bool isOnLoad);
}
public interface IGameStartListenerAsync
{
    Task OnGameStarting(bool isOnLoad, CancellationToken token);
}
public interface IFlagCapturedListener
{
    void OnFlagCaptured(Flag flag, ulong newOwner, ulong oldOwner);
}
public interface ICacheDiscoveredListener
{
    void OnCacheDiscovered(Components.Cache cache);
}
public interface ICacheDestroyedListener
{
    void OnCacheDestroyed(Components.Cache cache);
}
public interface ICraftingSettingsOverride
{
    void OnCraftRequested(CraftRequested e);
}
public interface IFlagNeutralizedListener
{
    void OnFlagNeutralized(Flag flag, ulong newOwner, ulong oldOwner);
}
public interface IPlayerDisconnectListener
{
    void OnPlayerDisconnecting(UCPlayer player);
}
public interface IPlayerConnectListener
{
    void OnPlayerConnecting(UCPlayer player);
}
public interface IPlayerConnectListenerAsync
{
    Task OnPlayerConnecting(UCPlayer player, CancellationToken token);
}
public interface IPlayerPostInitListener
{
    void OnPostPlayerInit(UCPlayer player, bool wasAlreadyOnline);
}
public interface IPlayerPostInitListenerAsync
{
    Task OnPostPlayerInit(UCPlayer player, bool wasAlreadyOnline, CancellationToken token);
}
public interface IJoinedTeamListener
{
    void OnJoinTeam(UCPlayer player, ulong team);
}
public interface IJoinedTeamListenerAsync
{
    Task OnJoinTeamAsync(UCPlayer player, ulong team, CancellationToken token);
}
public interface IPlayerPreInitListener
{
    void OnPrePlayerInit(UCPlayer player, bool wasAlreadyOnline);
}
public interface IPlayerPreInitListenerAsync
{
    Task OnPrePlayerInit(UCPlayer player, bool wasAlreadyOnline, CancellationToken token);
}
public interface IPlayerDeathListener
{
    void OnPlayerDeath(PlayerDied e);
}

public interface IUIListener
{
    void HideUI(UCPlayer player);
    void ShowUI(UCPlayer player);
    void UpdateUI(UCPlayer player);
}
public interface ILanguageChangedListener
{
    void OnLanguageChanged(UCPlayer player);
}

public interface IReloadableSingleton : IUncreatedSingleton
{
    string? ReloadKey { get; }
    void Reload();
    Task ReloadAsync(CancellationToken token);
}