using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Data;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Players;

public abstract class VoteUIDisplay<TData> : UnturnedUI, IVoteDisplay where TData : VoteUIDisplayData
{
    protected readonly IPlayerVoteManager VoteManager;
    protected readonly ITranslationService TranslationService;

    private Func<WarfarePlayer, bool>? _playerSelector;
    protected VoteSettings VoteSettings;

    protected bool HasVote { get; private set; }

    private readonly Func<CSteamID, TData> _getData;

    protected VoteUIDisplay(IServiceProvider serviceProvider, IPlayerVoteManager voteManager, string uiConfig)
        : base(
            serviceProvider.GetRequiredService<ILoggerFactory>(),
            serviceProvider.GetRequiredService<AssetConfiguration>().GetAssetLink<EffectAsset>(uiConfig),
            staticKey: true
        )
    {
        VoteManager = voteManager;

        TranslationService = serviceProvider.GetRequiredService<ITranslationService>();

        _getData = CreateData;
    }

    protected abstract TData CreateData(CSteamID arg);

    protected abstract void SendToPlayers(LanguageSet set);

    protected virtual void OnCleared(WarfarePlayer player, TData data) { }

    protected TData GetOrAddData(CSteamID id)
    {
        return GetOrAddData(id, _getData);
    }

    public void VoteStarted(in VoteSettings settings, Func<WarfarePlayer, bool>? playerSelector)
    {
        HasVote = true;
        VoteSettings = settings;
        _playerSelector = playerSelector;

        foreach (LanguageSet set in TranslationService.SetOf.PlayersWhere(playerSelector))
        {
            SendToPlayers(set);
        }
    }

    public void VoteFinished(IVoteResult result)
    {
        HasVote = false;
        foreach (LanguageSet set in TranslationService.SetOf.PlayersWhere(PlayerIsVoting()))
        {
            while (set.MoveNext())
            {
                ClearFromPlayer(set.Next.Connection);
                if (GetData<TData>(set.Next.Steam64) is { } data)
                    data.HasVoteUI = false;
            }
        }
    }

    public void PlayerJoinedVote(WarfarePlayer player)
    {
        if (_playerSelector != null && !_playerSelector(player))
            throw new InvalidOperationException("Player does not meet player selector requirements.");

        SendToPlayers(new LanguageSet(player));
    }

    public void PlayerLeftVote(WarfarePlayer player)
    {
        if (GetData<TData>(player.Steam64) is not { HasVoteUI: true } data)
            return;
        
        data.HasVoteUI = false;
        if (player.IsDisconnecting || !player.IsOnline)
            return;

        OnCleared(player, data);
        ClearFromPlayer(player.Connection);
    }

    public void PlayerVoteUpdated(CSteamID playerId, PlayerVoteState newVote, PlayerVoteState oldVote)
    {
        foreach (LanguageSet set in TranslationService.SetOf.PlayersWhere(PlayerIsVoting()))
        {
            SendToPlayers(set);
        }
    }

    protected Func<WarfarePlayer, bool> PlayerIsVoting()
    {
        Func<WarfarePlayer, bool> playerSelector;
        if (_playerSelector != null)
        {
            playerSelector = player =>
            {
                if (!_playerSelector(player))
                    return false;

                return GetData<TData>(player.Steam64) is { HasVoteUI: true };
            };
        }
        else
        {
            playerSelector = player => GetData<TData>(player.Steam64) is { HasVoteUI: true };
        }

        return playerSelector;
    }
}

public class VoteUIDisplayData : IUnturnedUIData
{
    public bool HasVoteUI { get; set; }

    public CSteamID Player { get; }
    public UnturnedUI Owner { get; }

    public VoteUIDisplayData(CSteamID player, UnturnedUI owner)
    {
        Player = player;
        Owner = owner;
    }

    UnturnedUIElement? IUnturnedUIData.Element => null;
}