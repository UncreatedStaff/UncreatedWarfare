using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Data;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Framework.UI.Presets;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Squads;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Squads.UI;

[UnturnedUI(BasePath = "SquadMenuBox")]
internal class SquadMenuUI : 
    UnturnedUI,
    IEventListener<SquadCreated>,
    IEventListener<SquadDisbanded>,
    IEventListener<SquadMemberJoined>,
    IEventListener<SquadMemberLeft>
{
    private readonly SquadManager _squadManager;
    private readonly IPlayerService _playerService;
    public LabeledButton CloseMenuButton { get; } = new LabeledButton("SquadMenuCloseButton");
    public LabeledStateButton CreateSquadButton { get; } = new LabeledStateButton("CreateSquadButton", "./Label", "./ButtonState");
    public UnturnedTextBox CreateSquadInput { get; } = new UnturnedTextBox("CreateSquadInput") { UseData = true };
    public SquadMenuElement[] Squads { get; } = ElementPatterns.CreateArray<SquadMenuElement>("ScrollView/Viewport/Content/Squad_{0}/Squad{1}_{0}", 1, to: SquadManager.MaxSquadCount);
    
    public SquadMenuUI(IServiceProvider serviceProvider, AssetConfiguration assetConfig, ILoggerFactory loggerFactory)
        : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:SquadMenuHUD"), /* todo turn off */ debugLogging: true, staticKey: true)
    {
        _squadManager = serviceProvider.GetRequiredService<SquadManager>();
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
        CreateSquadButton.OnClicked += CreateSquadButton_OnClicked;
        CloseMenuButton.OnClicked += CloseMenuButton_OnClicked;
        ElementPatterns.SubscribeAll(Squads.Select(e => e.SquadJoinLeaveButton), SquadJoinLeaveButton_OnClicked);
    }

    private void SquadJoinLeaveButton_OnClicked(UnturnedButton button, Player player)
    {
        WarfarePlayer warfarePlayer = _playerService.GetOnlinePlayer(player);

        int index = Array.FindIndex(Squads, e => e.SquadJoinLeaveButton.Button == button);
        List<Squad> friendlySquads = _squadManager.Squads.Where(s => s.Team == warfarePlayer.Team).ToList();

        if (index >= friendlySquads.Count)
            return;

        Squad squad = friendlySquads[index];

        if (squad.ContainsPlayer(warfarePlayer))
            squad.RemoveMember(warfarePlayer);
        else
            squad.AddMember(warfarePlayer);
    }

    private void CloseMenuButton_OnClicked(UnturnedButton button, Player player)
    {
        CloseUI(_playerService.GetOnlinePlayer(player));
    }

    private void CreateSquadButton_OnClicked(UnturnedButton button, Player player)
    {
        WarfarePlayer squadleader = _playerService.GetOnlinePlayer(player);
        UnturnedTextBoxData textBoxData = CreateSquadInput.GetOrAddData(player);

        string? squadName = textBoxData.Text;
        if (string.IsNullOrWhiteSpace(squadName))
            squadName = $"{player.channel.owner.playerID.playerName}'s Squad";

        _squadManager.CreateSquad(squadleader, squadName);
    }
    public void HandleEvent(SquadCreated e, IServiceProvider serviceProvider)
    {
        UpdateForViewingPlayers(e.Squad.Team);
    }
    public void HandleEvent(SquadLockUpdated e, IServiceProvider serviceProvider)
    {
        UpdateForViewingPlayers(e.Squad.Team);
    }
    public void HandleEvent(SquadDisbanded e, IServiceProvider serviceProvider)
    {
        UpdateForViewingPlayers(e.Squad.Team);
    }

    public void HandleEvent(SquadMemberJoined e, IServiceProvider serviceProvider)
    {
        UpdateForViewingPlayers(e.Squad.Team);
    }

    public void HandleEvent(SquadMemberLeft e, IServiceProvider serviceProvider)
    {
        UpdateForViewingPlayers(e.Squad.Team);
    }
    public IEnumerable<WarfarePlayer> ViewingPlayersOnTeam(Team team) => _playerService.OnlinePlayers.Where(p =>
    {
        PlayerViewingState data = GetOrAddData(p.Steam64, steam64 => new PlayerViewingState
        {
            Player = steam64,
            Owner = this
        });
        return data != null && data.IsViewing && p.Team == team;
    });
    public void OpenUI(WarfarePlayer player)
    {
        PlayerViewingState data = GetOrAddData(player.Steam64, steam64 => new PlayerViewingState
        {
            Player = steam64,
            Owner = this
        });
        data.IsViewing = true;
        SendToPlayer(player.Connection);
        CreateSquadInput.UpdateFromData(player.UnturnedPlayer);
        ModalHandle.TryGetModalHandle(player, ref data.Modal);
        UpdateForPlayer(player);
    }
    public void CloseUI(WarfarePlayer player)
    {
        PlayerViewingState data = GetOrAddData(player.Steam64, steam64 => new PlayerViewingState
        {
            Player = steam64,
            Owner = this
        });
        data.IsViewing = false;
        ClearFromPlayer(player.Connection);
        data.Modal.Dispose();
    }
    private void UpdateForViewingPlayers(Team team)
    {
        foreach (WarfarePlayer player in ViewingPlayersOnTeam(team))
        {
            UpdateForPlayer(player);
        }
    }
    private void UpdateForPlayer(WarfarePlayer player)
    {
        List<Squad> friendlySquads = _squadManager.Squads.Where(s => s.Team == player.Team).ToList();
        for (int i = 0; i < Squads.Length; i++)
        {
            SquadMenuElement element = Squads[i];
            if (i < friendlySquads.Count)
            {
                Squad squad = friendlySquads[i];
                SendSquadDetail(element, squad, player);
            }
            else
                element.Root.Hide(player);
        }
        bool notYetInSquad = player.Component<SquadPlayerComponent>().Squad == null;
        CreateSquadButton.SetState(player.Connection, notYetInSquad);
    }
    private void SendSquadDetail(SquadMenuElement element, Squad squad, WarfarePlayer player)
    {
        element.Root.Show(player);
        element.SquadName.SetText(player, squad.Name);
        element.SquadNumber.SetText(player, squad.TeamIdentificationNumber.ToString());
        element.MemberCount.SetText(player, $"{squad.Members.Count}/{Squad.MaxMembers}");

        element.SquadJoinLeaveButton.Enable(player);
        if (squad.ContainsPlayer(player))
            element.SquadJoinLeaveButton.SetText(player.Connection, "Leave");
        else if (player.Component<SquadPlayerComponent>().Squad != null)
            element.SquadJoinLeaveButton.Disable(player);
        else
            element.SquadJoinLeaveButton.SetText(player.Connection, "Join");

        for (int j = 0; j < element.MemberNames.Length; j++)
        {
            var memberElement = element.MemberNames[j];
            if (j < squad.Members.Count)
            {
                memberElement.Show(player);

                WarfarePlayer member = squad.Members[j];
                Class kitClass = member.Component<KitPlayerComponent>().ActiveClass;
                string memberName = $"{kitClass.GetIcon()}  {member.Names.PlayerName}";
                if (j == 0)
                    memberName = "Leader: " + memberName;

                memberElement.SetText(player, memberName);
            }
            else
                memberElement.Hide(player);
        }
    }
#nullable disable
    public class SquadMenuElement
    {
        [Pattern("", Root = true, CleanJoin = '_')]
        public UnturnedUIElement Root { get; set; }

        [Pattern("Number", Mode = FormatMode.Format)]
        public UnturnedLabel SquadNumber { get; set; }
        
        [Pattern("Name", Mode = FormatMode.Format)]
        public UnturnedLabel SquadName { get; set; }

        [Pattern("MemberCount", Mode = FormatMode.Format)]
        public UnturnedLabel MemberCount { get; set; }

        [Pattern("JoinLeaveButton", PresetPaths = ["./Label", "./ButtonState"], Mode = FormatMode.Format)]
        public LabeledStateButton SquadJoinLeaveButton { get; set; }

        [ArrayPattern(1, To = 6)]
        [Pattern("SquadMember_{0}", Mode = FormatMode.Replace)]
        public UnturnedLabel[] MemberNames { get; set; }
    }
    public class PlayerViewingState : IUnturnedUIData
    {
        internal ModalHandle Modal;
        public required CSteamID Player { get; init; }

        public required UnturnedUI Owner { get; init; }

        public UnturnedUIElement Element => null;
        public bool IsViewing { get; set; }
    }
}
