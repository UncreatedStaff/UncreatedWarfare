using System;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Squads;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Squads.UI;

[UnturnedUI(BasePath = "PlayerSquad")]
public class PlayerSquadHUD : UnturnedUI, IEventListener<SquadMemberJoined>, IEventListener<SquadMemberLeft>
{
    public UnturnedLabel SquadName { get; } = new UnturnedLabel("PlayerSquadName");
    public UnturnedLabel[] SquadMembers { get; } = ElementPatterns.CreateArray<UnturnedLabel>("PlayerSquadMember_{0}", 1, to: 6);

    public PlayerSquadHUD(AssetConfiguration assetConfig, ILoggerFactory loggerFactory)
        : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:PlayerSquadHUD"), staticKey: true) { }
    public void HandleEvent(SquadMemberJoined e, IServiceProvider serviceProvider)
    {
        SendToPlayer(e.Player.Connection);
        UpdateForSquad(e.Squad);
    }
    public void HandleEvent(SquadMemberLeft e, IServiceProvider serviceProvider)
    {
        ClearFromPlayer(e.Player.Connection);
        UpdateForSquad(e.Squad);
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
        SquadName.SetText(player, $"{squad.Name.ToUpper()}  {squad.Members.Count}/{Squad.MaxMembers}");
        for (int i = 0; i < SquadMembers.Length; i++)
        {
            UnturnedLabel element = SquadMembers[i];
            if (i < squad.Members.Count)
            {
                WarfarePlayer member = squad.Members[i];
                Class kitClass = member.Component<KitPlayerComponent>().ActiveClass;
                string memberName = $"{kitClass.GetIcon()}  {member.Names.PlayerName}";

                element.Show(player);
                element.SetText(player, memberName);
            }
            else
                element.Hide(player);
        }
    }
}
