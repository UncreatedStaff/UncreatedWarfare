using Microsoft.Extensions.DependencyInjection;
using SDG.NetTransport;
using System;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Layouts.Seeding;

[UnturnedUI(BasePath = "Container/Box")]
internal class SeedingVoteHud : VoteUIDisplay<VoteUIDisplayData>
{
    private readonly string _layout;
    private readonly SeedingTranslations _translations;
    private readonly HudManager? _hudManager;

    public UnturnedLabel Title { get; } = new UnturnedLabel("Title");
    public UnturnedLabel Info { get; } = new UnturnedLabel("Info");
    public UnturnedLabel Yes { get; } = new UnturnedLabel("Yes");
    public UnturnedLabel No { get; } = new UnturnedLabel("No");

    /// <inheritdoc />
    public SeedingVoteHud(
        IServiceProvider serviceProvider,
        IPlayerVoteManager voteManager,
        string layout,
        TranslationInjection<SeedingTranslations> translations) : base(serviceProvider, voteManager, "UI:SeedingVoteHUD")
    {
        _layout = layout;
        _hudManager = serviceProvider.GetService<HudManager>();
        _translations = translations.Value;
    }

    /// <inheritdoc />
    protected override void SendToPlayers(LanguageSet set)
    {
        string title = _translations.SeedingVoteTitle.Translate(in set);

        string? voteNoUnselected = null, voteNoSelected = null;
        string? voteYesUnselected = null, voteYesSelected = null;

        string noVotes = VoteManager.GetVoteCount(PlayerVoteState.No).ToString(set.Culture);
        string yesVotes = VoteManager.GetVoteCount(PlayerVoteState.Yes).ToString(set.Culture);

        while (set.MoveNext())
        {
            VoteUIDisplayData data = GetOrAddData(set.Next.Steam64);
            PlayerVoteState voteState = VoteManager.GetVoteState(set.Next.Steam64);

            string voteNoThisPlayer, voteYesThisPlayer;

            if (voteState == PlayerVoteState.Yes)
                voteYesThisPlayer = voteYesSelected ??= _translations.SeedingVoteYes.Translate($"<#ccff99>{noVotes}</color>", in set);
            else
                voteYesThisPlayer = voteYesUnselected ??= _translations.SeedingVoteYes.Translate($"<#ababab>{noVotes}</color>", in set);

            if (voteState == PlayerVoteState.No)
                voteNoThisPlayer = voteNoSelected ??= _translations.SeedingVoteNo.Translate($"<#ccff99>{yesVotes}</color>", in set);
            else
                voteNoThisPlayer = voteNoUnselected ??= _translations.SeedingVoteNo.Translate($"<#ababab>{yesVotes}</color>", in set);

            if (!data.HasVoteUI)
            {
                SendToPlayer(set.Next.Connection, title, _layout, voteYesThisPlayer, voteNoThisPlayer);
                data.HasVoteUI = true;
                _hudManager?.SetIsPluginVoting(set.Next, true);
            }
            else
            {
                ITransportConnection c = set.Next.Connection;
                Title.SetText(c, title);
                Info.SetText(c, _layout);
                Yes.SetText(c, voteYesThisPlayer);
                No.SetText(c, voteNoThisPlayer);
            }
        }
    }

    /// <inheritdoc />
    protected override void OnCleared(WarfarePlayer player, VoteUIDisplayData data)
    {
        _hudManager?.SetIsPluginVoting(player, false);
    }

    /// <inheritdoc />
    protected override VoteUIDisplayData CreateData(CSteamID arg)
    {
        return new VoteUIDisplayData(arg, this);
    }
}