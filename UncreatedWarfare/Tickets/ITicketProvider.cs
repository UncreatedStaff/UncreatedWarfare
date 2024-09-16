namespace Uncreated.Warfare.Tickets;
public interface ITicketProvider
{
    void Load();
    void Unload();
    int GetTeamBleed(ulong team);
    void UpdateUI(UCPlayer player);
    void UpdateUI(ulong team);
    void OnTicketsChanged(ulong team, int oldValue, int newValue, ref bool updateUI);
    void Tick();
    void OnGameStarting(bool isOnLoaded);
}