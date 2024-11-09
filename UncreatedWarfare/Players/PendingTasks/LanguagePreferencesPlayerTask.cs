using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Translations.Languages;

namespace Uncreated.Warfare.Players.PendingTasks;

/// <summary>
/// Downloads the player's language preferences.
/// </summary>
[PlayerTask]
internal class LanguagePreferencesPlayerTask(ILanguageDataStore languageDataStore) : IPlayerPendingTask
{
    private LanguagePreferences? _preferences;

    public async Task<bool> RunAsync(PlayerPending e, CancellationToken token)
    {
        _preferences = await languageDataStore.GetLanguagePreferences(e.Steam64.m_SteamID, token);
        return true;
    }

    public void Apply(WarfarePlayer player)
    {
        player.Locale.Preferences = _preferences!;
    }

    bool IPlayerPendingTask.CanReject => false;
}