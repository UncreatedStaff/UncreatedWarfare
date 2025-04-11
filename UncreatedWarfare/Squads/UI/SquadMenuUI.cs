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
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Kits;
using Uncreated.Warfare.Events.Models.Objects;
using Uncreated.Warfare.Events.Models.Squads;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Squads.UI;

[UnturnedUI(BasePath = "SquadMenuBox")]
public class SquadMenuUI : 
    UnturnedUI,
    IEventListener<SquadCreated>,
    IEventListener<SquadDisbanded>,
    IEventListener<SquadMemberJoined>,
    IEventListener<SquadMemberLeft>,
    IEventListener<SquadLockUpdated>,
    IEventListener<SquadLeaderUpdated>,
    IEventListener<NpcEventTriggered>,
    IEventListener<PlayerKitChanged>
{
    private readonly SquadManager _squadManager;
    private readonly IPlayerService _playerService;
    private readonly SquadTranslations _translations;
    private readonly ChatService _chatService;
    private readonly Func<CSteamID, SquadMenuUIPlayerData> _getData;

    public LabeledButton CloseMenuButton { get; } = new LabeledButton("SquadMenuCloseButton");
    public LabeledStateButton CreateSquadButton { get; } = new LabeledStateButton("CreateSquadButton", "./Label", "./ButtonState");
    public PlaceholderTextBox CreateSquadInput { get; } = new PlaceholderTextBox("CreateSquadInput", "./Viewport/Placeholder") { UseData = true };
    public UnturnedTextBox CreateSquadFeedback { get; } = new UnturnedTextBox("CreateSquadFeedback");
    public UnturnedLabel SquadsTitle { get; } = new UnturnedLabel("JoinSquadHeader");
    public SquadMenuElement[] Squads { get; } = ElementPatterns.CreateArray<SquadMenuElement>("ScrollView/Viewport/Content/Squad_{0}", 1, to: SquadManager.MaxSquadCount);
    public MySquadMenu MySquad { get; } = new MySquadMenu();

    public SquadMenuUI(IServiceProvider serviceProvider, AssetConfiguration assetConfig, ILoggerFactory loggerFactory)
        : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:SquadMenuHUD"), debugLogging: false, staticKey: true)
    {
        _squadManager = serviceProvider.GetRequiredService<SquadManager>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<SquadTranslations>>().Value;
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
        _chatService = serviceProvider.GetRequiredService<ChatService>();
        CreateSquadButton.OnClicked += SquadButtonClicked;
        CloseMenuButton.OnClicked += CloseButtonClicked;

        _getData = steam64 => new SquadMenuUIPlayerData
        {
            Player = steam64,
            Owner = this
        };

        ElementPatterns.SubscribeAll(MySquad.Members.Skip(1).Select(x => x.KickButton), KickMemberClicked);
        ElementPatterns.SubscribeAll(MySquad.Members.Skip(1).Select(x => x.PromoteButton), PromoteMemberClicked);

        MySquad.LeaveButton.OnClicked += JoinLeaveButtonClicked;
        MySquad.ToggleLockedButton.OnToggleUpdated += SquadLockedToggleUpdated;
        ElementPatterns.SubscribeAll(Squads.Select(e => e.SquadJoinLeaveButton), JoinLeaveButtonClicked);
    }

    private bool _isLocking;

    private void SquadLockedToggleUpdated(UnturnedToggle toggle, Player player, bool isLocked)
    {
        WarfarePlayer warfarePlayer = _playerService.GetOnlinePlayer(player);

        if (!warfarePlayer.IsSquadLeader())
        {
            return;
        }

        Squad squad = warfarePlayer.GetSquad()!;
        _isLocking = true;
        try
        {
            if (isLocked)
                squad.LockSquad(warfarePlayer);
            else
                squad.UnlockSquad(warfarePlayer);
        }
        finally
        {
            _isLocking = false;
        }
    }

    private void PromoteMemberClicked(UnturnedButton button, Player player)
    {
        int index = Array.FindLastIndex(MySquad.Members, x => ReferenceEquals(x.PromoteButton, button));
        if (index <= 0)
            return;

        WarfarePlayer warfarePlayer = _playerService.GetOnlinePlayer(player);
        if (!warfarePlayer.IsSquadLeader())
            return;

        Squad squad = warfarePlayer.GetSquad()!;
        if (index >= squad.Members.Count)
            return;

        WarfarePlayer member = squad.Members[index];
        squad.PromoteMember(member);
        _chatService.Send(member, _translations.SquadPromoted, squad);
    }

    private void KickMemberClicked(UnturnedButton button, Player player)
    {
        int index = Array.FindLastIndex(MySquad.Members, x => ReferenceEquals(x.KickButton, button));
        if (index <= 0)
            return;

        WarfarePlayer warfarePlayer = _playerService.GetOnlinePlayer(player);
        if (!warfarePlayer.IsSquadLeader())
            return;

        Squad squad = warfarePlayer.GetSquad()!;
        if (index >= squad.Members.Count)
            return;

        WarfarePlayer member = squad.Members[index];
        squad.RemoveMember(member);
        _chatService.Send(member, _translations.SquadKicked, squad);
    }

    private void JoinLeaveButtonClicked(UnturnedButton button, Player player)
    {
        WarfarePlayer warfarePlayer = _playerService.GetOnlinePlayer(player);

        if (ReferenceEquals(button, MySquad.LeaveButton.Button) && warfarePlayer.GetSquad() is { } mySquad && mySquad.Leader.Equals(player))
        {
            // promote other player to leader first
            if (mySquad.Members.Count > 1)
            {
                WarfarePlayer newLeader = mySquad.Members.Skip(1).Aggregate((x, best) => x.CachedPoints.XP > best.CachedPoints.XP ? x : best);
                mySquad.PromoteMember(newLeader);
                _chatService.Send(newLeader, _translations.SquadPromoted, mySquad);
            }
            mySquad.RemoveMember(warfarePlayer);
            return;
        }

        int index = Array.FindIndex(Squads, e => ReferenceEquals(e.SquadJoinLeaveButton.Button, button));

        Squad? squad = GetSquadAtIndex(warfarePlayer, index);
        if (squad == null)
            return;

        if (squad.ContainsPlayer(warfarePlayer))
            squad.RemoveMember(warfarePlayer);
        else
            squad.TryAddMember(warfarePlayer);
    }

    private void CloseButtonClicked(UnturnedButton button, Player player)
    {
        CloseUI(_playerService.GetOnlinePlayer(player));
    }

    private void SquadButtonClicked(UnturnedButton button, Player player)
    {
        WarfarePlayer squadleader = _playerService.GetOnlinePlayer(player);

        string? squadName = CreateSquadInput.TextBox.GetOrAddData(player).Text;
        if (string.IsNullOrWhiteSpace(squadName))
        {
            squadName = $"{squadleader.Names.CharacterName}'s Squad";
        }
        else if (!PassesSquadNameFilter(squadName))
        {
            CreateSquadFeedback.SetText(squadleader, _translations.SquadNameFilterViolated.Translate(squadleader));
            return;
        }
        
        int numberOfExistingSquads = _squadManager.Squads.Count(s => s.Team == squadleader.Team);
        int numberOfTeammates = _playerService.OnlinePlayers.Count(p => p.Team == squadleader.Team);
        
        int maxAllowedSquads = Mathf.CeilToInt((float)numberOfTeammates / Squad.MaxMembers) + 1;
        if (numberOfExistingSquads > maxAllowedSquads)
        {
            CreateSquadFeedback.SetText(squadleader, _translations.SquadLimitReached.Translate(squadleader));
            return;
        }

        squadName = squadName.TruncateWithEllipses(32);

        _squadManager.CreateSquad(squadleader, squadName);
        CreateSquadFeedback.SetText(player, string.Empty);
    }

    private static bool PassesSquadNameFilter(string squadName)
    {
        // todo: better verifications
        return ChatFilterHelper.GetChatFilterViolation(squadName) == null;
    }

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    public void HandleEvent(SquadCreated e, IServiceProvider serviceProvider)
    {
        UpdateForViewingPlayers(e.Squad);
    }

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    public void HandleEvent(SquadLockUpdated e, IServiceProvider serviceProvider)
    {
        if (_isLocking)
            UpdateForViewingPlayersExceptOwner(e.Squad);
        else
            UpdateForViewingPlayers(e.Squad);
    }

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    public void HandleEvent(SquadDisbanded e, IServiceProvider serviceProvider)
    {
        foreach (WarfarePlayer player in ViewingPlayersOnTeam(e.Squad.Team))
        {
            UpdateForPlayer(player);
        }
    }

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    public void HandleEvent(SquadMemberJoined e, IServiceProvider serviceProvider)
    {
        UpdateForViewingPlayers(e.Squad);
    }

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    public void HandleEvent(SquadMemberLeft e, IServiceProvider serviceProvider)
    {
        UpdateForViewingPlayers(e.Squad);
    }

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    public void HandleEvent(SquadLeaderUpdated e, IServiceProvider serviceProvider)
    {
        UpdateForViewingPlayers(e.Squad);
    }

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    void IEventListener<PlayerKitChanged>.HandleEvent(PlayerKitChanged e, IServiceProvider serviceProvider)
    {
        Squad? squad = e.Player.GetSquad();
        if (squad == null)
            return;

        WarfarePlayer leader = squad.Leader;
        if (leader.IsInSquad() && GetOrAddData(leader).IsViewing)
            SendMySquadDetail(leader);

        UpdateForViewingPlayersExceptOwner(squad);
    }

    [EventListener(RequireActiveLayout = true)]
    void IEventListener<NpcEventTriggered>.HandleEvent(NpcEventTriggered e, IServiceProvider serviceProvider)
    {
        if (!e.Id.Equals("Uncreated.Warfare.Squads.OpenMenu", StringComparison.Ordinal) || e.Player == null)
            return;

        OpenUI(e.Player);
        e.Consume();
    }

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    public IEnumerable<WarfarePlayer> ViewingPlayersOnTeam(Team team) => _playerService.OnlinePlayers.Where(p => GetOrAddData(p).IsViewing && p.Team == team);

    private SquadMenuUIPlayerData GetOrAddData(WarfarePlayer player)
    {
        return GetOrAddData(player.Steam64, _getData);
    }

    public void OpenUI(WarfarePlayer player)
    {
        SquadMenuUIPlayerData data = GetOrAddData(player);
        if (!data.IsViewing)
        {
            data.IsViewing = true;
            SendToPlayer(player.Connection);
            CreateSquadInput.SetPlaceholder(player, _translations.SquadSquadNamePlaceholder.Translate(player));
            CreateSquadInput.TextBox.UpdateFromData(player.UnturnedPlayer);
            CreateSquadButton.SetText(player, _translations.SquadButtonCreate.Translate(player));
            MySquad.LeaveButton.SetText(player, _translations.SquadButtonLeave.Translate(player));
            SquadsTitle.SetText(player, _translations.SquadsTitle.Translate(player));
            ModalHandle.TryGetModalHandle(player, ref data.Modal);
        }

        UpdateForPlayer(player);
    }

    public void CloseUI(WarfarePlayer player)
    {
        SquadMenuUIPlayerData data = GetOrAddData(player);
        data.IsViewing = false;
        ClearFromPlayer(player.Connection);
        data.Modal.Dispose();
    }

    private void UpdateForViewingPlayers(Squad squad)
    {
        if (squad.Members.Count == 0)
            return;
        
        foreach (WarfarePlayer player in ViewingPlayersOnTeam(squad.Team))
        {
            UpdateForPlayer(player);
        }
    }
    private void UpdateForViewingPlayersExceptOwner(Squad squad)
    {
        if (squad.Members.Count == 0)
            return;

        WarfarePlayer owner = squad.Leader;
        foreach (WarfarePlayer player in ViewingPlayersOnTeam(squad.Team))
        {
            if (!player.Equals(owner))
            {
                UpdateForPlayer(player);
            }
        }
    }

    private Squad? GetSquadAtIndex(WarfarePlayer player, int index)
    {
        return GetVisibleSquadList(player).Skip(index).FirstOrDefault();
    }

    private IEnumerable<Squad> GetVisibleSquadList(WarfarePlayer player)
    {
        return _squadManager.Squads
            .Where(s => s.Team == player.Team)
            .OrderByDescending(x => x.ContainsPlayer(player))
            .ThenBy(x => x.TeamIdentificationNumber);
    }

    private void UpdateForPlayer(WarfarePlayer player)
    {
        List<Squad> friendlySquads = GetVisibleSquadList(player).ToList();

        if (player.IsSquadLeader())
        {
            SendMySquadDetail(player);
            friendlySquads.RemoveAt(0);
        }
        else
        {
            MySquad.Root.Hide(player);
        }

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

        CreateSquadButton.SetState(player.Connection, !player.IsInSquad());
    }

    private void SendMySquadDetail(WarfarePlayer player)
    {
        Squad squad = player.GetSquad()!;

        MySquad.Name.SetText(player, squad.Name);
        MySquad.Number.SetText(player, squad.TeamIdentificationNumber.ToString(player.Locale.CultureInfo));
        MySquad.MemberCount.SetText(player, $"{squad.Members.Count.ToString(player.Locale.CultureInfo)}/{Squad.MaxMembers.ToString(player.Locale.CultureInfo)}");
        MySquad.ToggleLockedButton.Set(player.UnturnedPlayer, squad.IsLocked);

        int i = 0;
        int ct = Math.Min(MySquad.Members.Length, squad.Members.Count);
        for (; i < ct; ++i)
        {
            MySquadMember ui = MySquad.Members[i];
            WarfarePlayer member = squad.Members[i];
            ui.Show(player);

            Class cl = member.Component<KitPlayerComponent>().ActiveClass;
            ui.Name.SetText(player, $"<mspace=20>{cl.GetIconString()}</mspace>  {member.Names.CharacterName}");
            ui.Avatar.SetImage(player, member.SteamSummary.AvatarUrlSmall);
        }

        for (; i < MySquad.Members.Length; ++i)
        {
            MySquad.Members[i].Hide(player);
        }

        MySquad.Root.Show(player);
    }

    private void SendSquadDetail(SquadMenuElement element, Squad squad, WarfarePlayer player)
    {
        element.SquadName.SetText(player, squad.Name);
        element.SquadNumber.SetText(player, squad.TeamIdentificationNumber.ToString());
        element.MemberCount.SetText(player, $"{squad.Members.Count.ToString(player.Locale.CultureInfo)}/{Squad.MaxMembers.ToString(player.Locale.CultureInfo)}");
        element.LockIcon.SetVisibility(player, squad.IsLocked);

        if (squad.ContainsPlayer(player))
        {
            element.SquadJoinLeaveButton.Enable(player);
            element.SquadJoinLeaveButton.SetText(player, _translations.SquadButtonLeave.Translate(player));
        }
        else
        {
            element.SquadJoinLeaveButton.SetState(player, !player.IsInSquad() && squad.CanJoinSquad(player));
            element.SquadJoinLeaveButton.SetText(player, _translations.SquadButtonJoin.Translate(player));
        }

        for (int j = 0; j < element.MemberNames.Length; j++)
        {
            var memberElement = element.MemberNames[j];
            if (j < squad.Members.Count)
            {
                WarfarePlayer member = squad.Members[j];
                Class kitClass = member.Component<KitPlayerComponent>().ActiveClass;
                string memberName = $"<mspace=20>{kitClass.GetIconString()}</mspace> {member.Names.PlayerName}";
                if (j == 0)
                    memberName = _translations.SquadLeader.Translate(memberName, player);

                memberElement.SetText(player, memberName);
                memberElement.Show(player);
            }
            else
                memberElement.Hide(player);
        }

        element.Root.Show(player);
    }
#nullable disable

    public class MySquadMenu
    {
        public UnturnedUIElement Root { get; } = new UnturnedUIElement("ScrollView/Viewport/Content/MySquad");
        public UnturnedLabel Number { get; } = new UnturnedLabel("ScrollView/Viewport/Content/MySquad/Number");
        public UnturnedLabel Name { get; } = new UnturnedLabel("ScrollView/Viewport/Content/MySquad/Name");
        public UnturnedLabel MemberCount { get; } = new UnturnedLabel("ScrollView/Viewport/Content/MySquad/MemberCount");
        public LabeledButton LeaveButton { get; } = new LabeledButton("ScrollView/Viewport/Content/MySquad/SquadMenuHUD_LeaveMySquadButton");
        public LabeledUnturnedToggle ToggleLockedButton { get; } = new LabeledUnturnedToggle(true, "ScrollView/Viewport/Content/MySquad/SquadMenuHUD_ToggleLockedMySquadButton", "./ToggleState", "./LockLabel", null);
        public UnturnedLabel LockButtonDescription { get; } = new UnturnedLabel("ScrollView/Viewport/Content/MySquad/SquadMenuHUD_ToggleLockedMySquadButton/LockLabelDescription");
        public MySquadMember[] Members { get; } = ElementPatterns.CreateArray<MySquadMember>("ScrollView/Viewport/Content/MySquad/SquadMember_{0}", 1, to: Squad.MaxMembers);
    }

    public class MySquadMember : PatternRoot
    {
        [Pattern("Name")]
        public UnturnedLabel Name { get; set; }

        [Pattern("Avatar", AdditionalPath = "AvatarMask")]
        public UnturnedImage Avatar { get; set; }

        [Pattern("SquadMenuHUD_KickMySquadMember_{0}")]
        public UnturnedButton KickButton { get; set; }

        [Pattern("SquadMenuHUD_PromoteMySquadMember_{0}")]
        public UnturnedButton PromoteButton { get; set; }
    }

    public class SquadMenuElement : PatternRoot
    {
        [Pattern("Number")]
        public UnturnedLabel SquadNumber { get; set; }
        
        [Pattern("Name")]
        public UnturnedLabel SquadName { get; set; }

        [Pattern("MemberCount")]
        public UnturnedLabel MemberCount { get; set; }

        [Pattern("Lock")]
        public UnturnedUIElement LockIcon { get; set; }

        [Pattern("SquadMenuHUD_JoinLeaveButton_{0}", PresetPaths = [ "./Label", "./ButtonState" ])]
        public LabeledStateButton SquadJoinLeaveButton { get; set; }

        [ArrayPattern(1, To = 6)]
        [Pattern("SquadMember_{0}")]
        public UnturnedLabel[] MemberNames { get; set; }
    }
    public class SquadMenuUIPlayerData : IUnturnedUIData
    {
        internal ModalHandle Modal;
        public required CSteamID Player { get; init; }

        public required UnturnedUI Owner { get; init; }

        public UnturnedUIElement Element => null;
        public bool IsViewing { get; set; }
    }
}
