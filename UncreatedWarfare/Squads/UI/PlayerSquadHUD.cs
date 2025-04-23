using System;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Kits;
using Uncreated.Warfare.Events.Models.Squads;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;

namespace Uncreated.Warfare.Squads.UI;

[UnturnedUI(BasePath = "PlayerSquad")]
public class PlayerSquadHUD : UnturnedUI,
    IEventListener<SquadMemberJoined>,
    IEventListener<SquadMemberLeft>,
    IEventListener<SquadLeaderUpdated>,
    IEventListener<PlayerKitChanged>,
    IHudUIListener
{
    private readonly IPlayerService _playerService;
    public UnturnedLabel SquadName { get; } = new UnturnedLabel("PlayerSquadName");
    public UnturnedLabel SquadNumber { get; } = new UnturnedLabel("PlayerSquadName/PlayerSquadNumber");
    public UnturnedLabel[] SquadMembers { get; } = ElementPatterns.CreateArray<UnturnedLabel>("PlayerSquadMember_{0}", 1, to: 6);

    private bool _isHidden;

    public PlayerSquadHUD(AssetConfiguration assetConfig, ILoggerFactory loggerFactory, IPlayerService playerService)
        : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:PlayerSquadHUD"), staticKey: true)
    {
        _playerService = playerService;
    }

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    public void HandleEvent(SquadMemberJoined e, IServiceProvider serviceProvider)
    {
        SendToPlayer(e.Player.Connection);
        UpdateForSquad(e.Squad);
    }

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    public void HandleEvent(SquadMemberLeft e, IServiceProvider serviceProvider)
    {
        ClearFromPlayer(e.Player.Connection);
        UpdateForSquad(e.Squad);
    }

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    public void HandleEvent(SquadLeaderUpdated e, IServiceProvider serviceProvider)
    {
        UpdateForSquad(e.Squad);
    }

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    public void HandleEvent(PlayerKitChanged e, IServiceProvider serviceProvider)
    {
        Squad? squad = e.Player.GetSquad();
        if (squad != null)
            UpdateForSquad(squad);
    }
    private void UpdateForSquad(Squad squad)
    {
        foreach (WarfarePlayer member in squad.Members)
        {
            UpdateForPlayer(member, squad);
        }
    }
    private void UpdateForPlayer(WarfarePlayer player, Squad squad)
    {
        if (!player.IsOnline)
            return;

        if (_isHidden)
        {
            ClearFromPlayer(player.Connection);
            return;
        }

        SquadName.SetText(player, $"{squad.Name}  {squad.Members.Count}/{Squad.MaxMembers}");
        SquadNumber.SetText(player, squad.TeamIdentificationNumber.ToString());
        for (int i = 0; i < SquadMembers.Length; i++)
        {
            UnturnedLabel element = SquadMembers[i];
            if (i < squad.Members.Count)
            {
                WarfarePlayer member = squad.Members[i];
                Class kitClass = member.Component<KitPlayerComponent>().ActiveClass;
                string memberName = $"<mspace=20>{kitClass.GetIconString()}</mspace>  {member.Names.PlayerName}";

                element.Show(player);
                element.SetText(player, memberName);
            }
            else
                element.Hide(player);
        }
    }

    /// <inheritdoc />
    public void Hide(WarfarePlayer? player)
    {
        if (player != null)
        {
            ClearFromPlayer(player.Connection);
            return;
        }

        _isHidden = true;
        ClearFromAllPlayers();
    }

    /// <inheritdoc />
    public void Restore(WarfarePlayer? player)
    {
        if (player != null)
        {
            Squad? squad = player.GetSquad();
            if (squad == null)
                ClearFromPlayer(player.Connection);
            else
                UpdateForPlayer(player, squad);
            return;
        }

        _isHidden = false;
        foreach (WarfarePlayer pl in _playerService.OnlinePlayers)
        {
            Restore(pl);
        }
    }
}
