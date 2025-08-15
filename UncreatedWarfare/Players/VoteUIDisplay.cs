using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Data;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Players;

[UnturnedUI(BasePath = "Canvas")]
public abstract class VoteUIDisplay : UnturnedUI, IVoteDisplay
{
    private readonly IPlayerVoteManager _voteManager;
    private readonly ITranslationService _translationService;
    private VoteSettings _voteSettings;
    private bool _hasVote;

    private readonly Func<CSteamID, Data> _getData;

    public UnturnedLabel TitleLabel { get; } = new UnturnedLabel("Title");

    protected VoteUIDisplay(IServiceProvider serviceProvider, IPlayerVoteManager voteManager)
        : base(
            serviceProvider.GetRequiredService<ILoggerFactory>(),
            serviceProvider.GetRequiredService<AssetConfiguration>().GetAssetLink<EffectAsset>("UI:VoteUI"),
            staticKey: true
        )
    {
        _voteManager = voteManager;

        _translationService = serviceProvider.GetRequiredService<ITranslationService>();

        _getData = id => new Data(id, this);
    }

    protected abstract string TranslateTitle(in LanguageSet langSet);

    protected virtual void SendToPlayers(LanguageSet set)
    {
        string label = TranslateTitle(in set);
        
        while (set.MoveNext())
        {
            Data data = GetOrAddData(set.Next.Steam64);
            if (!data.HasVoteUI)
            {
                SendToPlayer(set.Next.Connection, label);
                data.HasVoteUI = true;
            }
            else
            {
                TitleLabel.SetText(set.Next, label);
            }
        }
    }

    protected Data GetOrAddData(CSteamID id)
    {
        return GetOrAddData(id, _getData);
    }

    public void VoteStarted(in VoteSettings settings, Func<WarfarePlayer, bool>? playerSelector)
    {
        _hasVote = true;
        _voteSettings = settings;

        foreach (LanguageSet set in _translationService.SetOf.PlayersWhere(playerSelector))
        {
            SendToPlayers(set);
        }
    }

    public void VoteFinished(IVoteResult result)
    {
        _hasVote = false;
    }

    public void PlayerJoinedVote(WarfarePlayer player)
    {
        SendToPlayers(new LanguageSet(player));
    }

    public void PlayerLeftVote(WarfarePlayer player)
    {
        if (GetData<Data>(player.Steam64) is not { HasVoteUI: true } data)
            return;
        
        ClearFromPlayer(player.Connection);
        data.HasVoteUI = false;
    }

    public void PlayerVoteUpdated(CSteamID playerId, PlayerVoteState newVote, PlayerVoteState oldVote)
    {
        
    }

    protected class Data : IUnturnedUIData
    {
        public bool HasVoteUI { get; set; }

        public CSteamID Player { get; }
        public UnturnedUI Owner { get; }
        UnturnedUIElement? IUnturnedUIData.Element => null;

        public Data(CSteamID player, UnturnedUI owner)
        {
            Player = player;
            Owner = owner;
        }
    }
}