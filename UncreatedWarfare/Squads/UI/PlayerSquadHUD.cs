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
    private readonly HudManager _hudManager;
    public UnturnedLabel SquadName { get; } = new UnturnedLabel("PlayerSquadName");
    public UnturnedLabel SquadNumber { get; } = new UnturnedLabel("PlayerSquadName/PlayerSquadNumber");
    public UnturnedLabel[] SquadMembers { get; } = ElementPatterns.CreateArray<UnturnedLabel>("PlayerSquadMember_{0}", 1, to: 6);

    public PlayerSquadHUD(
        AssetConfiguration assetConfig,
        ILoggerFactory loggerFactory,
        IPlayerService playerService,
        HudManager hudManager)
        : base(
            loggerFactory,
            assetConfig.GetAssetLink<EffectAsset>("UI:PlayerSquadHUD"),
            staticKey: true
        )
    {
        _playerService = playerService;
        _hudManager = hudManager;
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
        if (squad == null)
            return;

        int index = squad.GetMemberIndex(e.Player);
        if (index < 0 || index >= SquadMembers.Length)
            return;

        string memberName = GetMemberNameText(e.Player);
        foreach (WarfarePlayer player in squad.Members)
        {
            SquadMembers[index].SetText(player, memberName);
        }
    }

    private void UpdateForSquad(Squad squad)
    {
        UpdateForPlayers(squad);
    }

    private readonly string?[] _memberTextBuffer = new string?[Squad.MaxMembers];

    private void UpdateForPlayer(Squad squad, WarfarePlayer player)
    {
        int index = squad.GetMemberIndex(player);
        if (index < 0)
            ClearFromPlayer(player.Connection);
        else
            UpdateForPlayers(squad, index);
    }

    private void UpdateForPlayers(Squad squad, int playerIndex = -1)
    {
        // update for one or more players at once.
        // playerIndex = -1 means the whole squad, otherwise update for a specific player in the squad

        WarfarePlayer? player = playerIndex < 0 ? null : squad.Members[playerIndex];
        if (player != null ? _hudManager.IsHidden(player) : _hudManager.IsHiddenForAllPlayers)
            return;

        string squadName = $"{squad.Name}  {squad.Members.Count}/{Squad.MaxMembers}";
        string idNumber = squad.TeamIdentificationNumber.ToString();
        int member = 0;

        for (; member < squad.Members.Count; ++member)
            _memberTextBuffer[member] = GetMemberNameText(squad.Members[member]);

        for (; member < _memberTextBuffer.Length; ++member)
            _memberTextBuffer[member] = null;

        int max = playerIndex >= 0 ? playerIndex + 1 : squad.Members.Count;
        for (int m = Math.Max(0, playerIndex); m < max; ++m)
        {
            player = squad.Members[m];

            if (playerIndex < 0 && _hudManager.IsHidden(player))
                continue;

            SquadName.SetText(player, squadName);
            SquadNumber.SetText(player, idNumber);
            for (int i = 0; i < SquadMembers.Length; i++)
            {
                UnturnedLabel element = SquadMembers[i];
                string? memberName = _memberTextBuffer[i];
                if (memberName != null)
                {
                    element.Show(player);
                    element.SetText(player, memberName);
                }
                else
                    element.Hide(player);
            }
        }
    }

    private static string GetMemberNameText(WarfarePlayer member)
    {
        Class kitClass = member.Component<KitPlayerComponent>().GetActiveEffectiveKit()?.Class ?? Class.None;
        return $"<mspace=20>{kitClass.GetIconString()}</mspace>  {member.Names.PlayerName}";
    }

    /// <inheritdoc />
    public void Hide(WarfarePlayer? player)
    {
        if (player != null)
        {
            if (player.IsInSquad())
                ClearFromPlayer(player.Connection);
            return;
        }

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
            {
                SendToPlayer(player.Connection);
                UpdateForPlayer(squad, player);
            }
            return;
        }

        foreach (WarfarePlayer pl in _playerService.OnlinePlayers)
        {
            Restore(pl);
        }
    }
}