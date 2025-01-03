using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Event listener args which handles <see cref="DamageTool.damagePlayerRequested"/>.
/// </summary>
/// <remarks>Does not support async event handlers.</remarks>
public class DamagePlayerRequested : CancellablePlayerEvent
{
    private DamagePlayerParameters _parameters;
    private readonly IPlayerService _playerService;
    private WarfarePlayer? _instigator;

    /// <summary>
    /// Mutable properties for how players are damaged.
    /// </summary>
    public ref DamagePlayerParameters Parameters => ref _parameters;

    /// <summary>
    /// If this request injured the player.
    /// </summary>
    public bool IsInjure { get; set; }

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

    public DamagePlayerRequested(in DamagePlayerParameters parameters, IPlayerService playerService)
    {
        _parameters = parameters;
        _playerService = playerService;
    }
}
