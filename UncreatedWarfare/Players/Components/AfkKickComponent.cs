using Microsoft.Extensions.DependencyInjection;
using SDG.Framework.Utilities;
using System;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Translations;
using MathUtility = Uncreated.Warfare.Util.MathUtility;

namespace Uncreated.Warfare.Players.Components;

[PlayerComponent]
internal sealed class AfkKickComponent : IPlayerComponent, IDisposable
{
    public const float AfkTimeMaxSeconds = 6 * 60;
    public const float AfkWarningSeconds = 5 * 60;

    private bool _hasSentWarning;

    public static readonly PermissionLeaf PermissionIgnoredAfkKick = new PermissionLeaf("warfare::features.afk");

    private Vector3 _lastPos;

    private float _lastAfk;

#nullable disable

    private UserPermissionStore _userPermissionStore;
    private PlayersTranslations _translations;
    private ChatService _chatService;
    private ILogger<AfkKickComponent> _logger;

    public WarfarePlayer Player { get; private set; }

#nullable restore


    public void Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        _userPermissionStore = serviceProvider.GetRequiredService<UserPermissionStore>();
        _chatService = serviceProvider.GetRequiredService<ChatService>();
        _lastPos = Player.Position;
        _lastAfk = -1;
        _hasSentWarning = false;

        _translations = serviceProvider.GetRequiredService<TranslationInjection<PlayersTranslations>>().Value;
        _logger = serviceProvider.GetRequiredService<ILogger<AfkKickComponent>>();

        if (isOnJoin)
            TimeUtility.updated += OnUpdate;
    }

    private void OnUpdate()
    {
        Vector3 pos = Player.Position;
        float rt = Time.realtimeSinceStartup;
        if (_lastAfk < 0 || MathUtility.SquaredDistance(in pos, in _lastPos, true) > 0.0005 || Player.IsOnDuty)
        {
            _lastPos = pos;
            _lastAfk = rt;
            _hasSentWarning = false;
            return;
        }

        if (rt - _lastAfk <= AfkTimeMaxSeconds)
        {
            if (rt - _lastAfk > AfkWarningSeconds && !_hasSentWarning)
            {
                _hasSentWarning = true;
                _chatService.Send(Player, _translations.AfkKickWarning, TimeSpan.FromSeconds(AfkTimeMaxSeconds - (rt - _lastAfk)));
            }

            return;
        }

        _lastAfk = rt;
        UniTask.Create(async () =>
        {
            if (await _userPermissionStore.HasPermissionAsync(Player, PermissionIgnoredAfkKick, Player.DisconnectToken))
            {
                return;
            }

            if (Player.IsOnline)
            {
                _logger.LogInformation("Player {0} kicked for being AFK for too long.", Player);
                Provider.kick(Player.Steam64, _translations.AfkKickMessage.Translate(Player));
            }
        });
    }

    void IDisposable.Dispose()
    {
        TimeUtility.updated -= OnUpdate;
    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }
}