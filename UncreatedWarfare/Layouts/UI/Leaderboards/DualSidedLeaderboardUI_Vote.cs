using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Framework.UI.Presets;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Layouts.UI.Leaderboards;

// code for the 'vote for next layout' screen

partial class DualSidedLeaderboardUI
{
    public readonly VoteButton[] VoteButtons = ElementPatterns.CreateArray<VoteButton>("~/VoteBox/Grid/Vote_{0}", 0, to: 3);

    public readonly LabeledButton VoteCloseButton = new LabeledButton("~/VoteBox/Vote_Close", "./Label");
    public readonly LabeledStateButton VoteClearButton = new LabeledStateButton("~/VoteBox/Vote_Clear", "./Label", "./ButtonState");

    public readonly LabeledButton VoteOpenButton = new LabeledButton("GameInfo/Vote_Open", "./Label");
    public readonly UnturnedLabel VoteNewButton = new UnturnedLabel("GameInfo/Vote_Open/NewLabel");

    public readonly UnturnedLabel VoteTitle = new UnturnedLabel("~/VoteBox/Title");
    public readonly UnturnedUIElement LogicOpenVote = new UnturnedUIElement("~/Logic_OpenVote");
    public readonly UnturnedUIElement LogicCloseVote = new UnturnedUIElement("~/Logic_CloseVote");

    private LayoutInfo[] _voteLayouts = Array.Empty<LayoutInfo>();
    private int[] _layoutVotes;

    public bool IsVotingPeriodOpen { get; set; }

    private LayoutInfo[] ComputeCandidateLayouts()
    {
        List<LayoutInfo> layouts = _layoutFactory.GetBaseLayoutFiles()
            .Select(x => _layoutFactory.ReadLayoutInfo(x.FullName, false))
            .Where(x => x != null && !string.Equals(x.FilePath, _layout.LayoutInfo.FilePath))
            .ToList()!;

        return RandomUtility.GetRandomValues(layouts, x =>
        {
            // scale weight up slowly based on the difference between this layout's name and the candidate's layout name
            double levDistance = StringUtility.LevenshteinDistance(x.DisplayName, _layout.LayoutInfo.DisplayName,
                CultureInfo.InvariantCulture,
                LevenshteinOptions.IgnoreCase | LevenshteinOptions.IgnorePunctuation |
                LevenshteinOptions.IgnoreWhitespace);
            levDistance = Math.Max(0.05, Math.Pow(Math.Abs(levDistance), 1d / 3d));

            return x.Weight * levDistance * (string.Equals(x.Configuration.GamemodeName, _layout.LayoutInfo.Configuration.GamemodeName, StringComparison.OrdinalIgnoreCase) ? 0.2 : 1);
        }, VoteButtons.Length);
    }

    private void SendNoVotes(LanguageSet set)
    {
        // disables the vote button.
        while (set.MoveNext())
        {
            VoteOpenButton.Hide(set.Next);
        }
    }

    private void SendVotes(LanguageSet set)
    {
        int i = 0;
        for (; i < _voteLayouts.Length; ++i)
        {
            VoteButton ui = VoteButtons[i];
            LayoutInfo layout = _voteLayouts[i];

            string name = string.IsNullOrWhiteSpace(layout.Configuration.GamemodeName) || string.IsNullOrWhiteSpace(layout.Configuration.LayoutName)
                ? layout.DisplayName
                : $"{layout.Configuration.GamemodeName}\n<#bbb>{layout.Configuration.LayoutName}</color>";

            string votes = _layoutVotes[i].ToString(set.Culture);

            while (set.MoveNext())
            {
                ui.Button.SetText(set.Next.Connection, name);
                ui.Image.SetImage(set.Next.Connection, layout.Configuration.Image ?? string.Empty);
                ui.Votes.SetText(set.Next.Connection, votes);
                ui.Root.Show(set.Next.Connection);
            }

            set.Reset();
        }

        for (; i < VoteButtons.Length; ++i)
        {
            VoteButton ui = VoteButtons[i];
            while (set.MoveNext())
                ui.Root.Hide(set.Next.Connection);

            set.Reset();
        }
    }

    public void OpenVotes(ITransportConnection connection)
    {
        LogicOpenVote.SetVisibility(connection, _doVote);
    }

    public void CloseVotes(ITransportConnection connection)
    {
        LogicOpenVote.SetVisibility(connection, false);
    }

    private void OnClearVotes(UnturnedButton button, Player unturnedPlayer)
    {
        WarfarePlayer player = _playerService.GetOnlinePlayer(unturnedPlayer);

        ClearPlayerVote(player);
    }

    public void ClearPlayerVote(WarfarePlayer player)
    {
        DualSidedLeaderboardPlayerData data = GetOrCreateData(player.Steam64);
     
        if (player.IsOnline && !player.IsDisconnecting)
        {
            VoteClearButton.Disable(player.Connection);
        }

        if (data.Vote == -1)
        {
            return;
        }

        --_layoutVotes[data.Vote];
        data.Vote = -1;
        UpdateVoteCountLabels();
    }

    public void EndVotingPeriod()
    {
        if (!IsVotingPeriodOpen)
            return;

        IsVotingPeriodOpen = false;
        int maxWinnerCount = -1;
        LayoutInfo? winner = null;
        List<LayoutInfo>? winners = null;

        for (int i = 0; i < _voteLayouts.Length; ++i)
        {
            LayoutInfo info = _voteLayouts[i];
            int votes = _layoutVotes[i];

            if (votes > maxWinnerCount)
            {
                if (winners == null)
                    winner = info;
                else
                {
                    winners.Clear();
                    winners.Add(info);
                }
                maxWinnerCount = votes;
                continue;
            }

            if (votes < maxWinnerCount)
                continue;

            if (winners == null)
            {
                winners = new List<LayoutInfo>(_voteLayouts.Length) { winner! };
                winner = null;
            }
            else
            {
                winners.Add(info);
            }
        }

        if (winners != null)
        {
            winner = winners[RandomUtility.GetIndex(winners, x => x.Weight)];
        }

        if (winner == null || !File.Exists(winner.FilePath))
            return;

        _layoutFactory.NextLayout = new FileInfo(winner.FilePath);
        GetLogger().LogInformation($"Voted for next layout: {winner.DisplayName}.");
    }

    private void OnVoteButtonChosen(UnturnedButton button, Player unturnedPlayer)
    {
        if (!IsVotingPeriodOpen)
        {
            return;
        }

        int index = Array.FindIndex(VoteButtons, b => ReferenceEquals(b.Button.Button, button));
        if (index < 0 || index >= _voteLayouts.Length)
            return;

        WarfarePlayer player = _playerService.GetOnlinePlayer(unturnedPlayer);

        DualSidedLeaderboardPlayerData data = GetOrCreateData(player.Steam64);

        if (data.Vote == index)
        {
            return;
        }

        if (data.Vote >= 0)
            --_layoutVotes[data.Vote];
        else
            VoteClearButton.Enable(player.Connection);

        data.Vote = index;
        ++_layoutVotes[index];
        UpdateVoteCountLabels();
    }

    private static readonly Color32 SelectedColor = new Color32(138, 226, 219, 255);

    private void UpdateVoteCountLabels()
    {
        int max = _layoutVotes.Max();
        for (int vote = 0; vote < _layoutVotes.Length; ++vote)
        {
            int voteCt = _layoutVotes[vote];
            bool isMax = voteCt != 0 && voteCt == max;

            UnturnedLabel oldVoteLabel = VoteButtons[vote].Votes;
            foreach (LanguageSet set in _translationService.SetOf.AllPlayers())
            {
                string str = voteCt.ToString(set.Culture);
                if (isMax)
                    str = TranslationFormattingUtility.Colorize(str, SelectedColor);
                while (set.MoveNext())
                {
                    oldVoteLabel.SetText(set.Next.Connection, str);
                }
            }
        }
    }

    public class VoteButton : PatternRoot
    {
        [Pattern("Background")]
        public required UnturnedImage Image { get; set; }

        [Pattern("Vote_Gamemode_{0}", PresetPaths = [ "./Label" ])]
        public required LabeledButton Button { get; set; }

        [Pattern("Votes")]
        public required UnturnedLabel Label { get; set; }

        [Pattern("Votes", AdditionalPath = "Vote_Gamemode_{0}")]
        public required UnturnedLabel Votes { get; set; }
    }
}