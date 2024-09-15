using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Handles before a player is injured.
/// </summary>
public class InjurePlayerRequested : CancellablePlayerEvent
{
    private DamagePlayerParameters _parameters;
    private readonly IPlayerService _playerService;
    private WarfarePlayer? _instigator;

    /// <summary>
    /// Mutable properties for how players are damaged to get injured.
    /// </summary>
    public ref DamagePlayerParameters Parameters => ref _parameters;

    /// <summary>
    /// The player that instigated the damage.
    /// </summary>
    public WarfarePlayer? Instigator
    {
        get
        {
            if (_parameters.killer.GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
                return null;

            if (_instigator == null || !_instigator.Equals(_parameters.killer))
                _instigator = _playerService.GetOnlinePlayerOrNullThreadSafe(_parameters.killer);

            return _instigator;
        }
        set
        {
            _parameters.killer = value?.Steam64 ?? CSteamID.Nil;
            _instigator = value;
        }
    }
    public InjurePlayerRequested(in DamagePlayerParameters parameters, IPlayerService playerService)
    {
        _parameters = parameters;
        _playerService = playerService;
    }
}