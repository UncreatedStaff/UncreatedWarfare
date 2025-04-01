using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Fobs;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Translations.Util;

namespace Uncreated.Warfare.Fobs.UI;

[UnturnedUI(BasePath = "FobList")]
public class FobHUD : 
    UnturnedUI,
    IEventListener<FobRegistered>,
    IEventListener<FobDeregistered>,
    IEventListener<FobBuilt>,
    IEventListener<PlayerTeamChanged>,
    IEventListener<FobDestroyed>,
    IEventListener<FobProxyChanged>,
    IEventListener<FobSuppliesChanged>,
    IHudUIListener
{
    private readonly FobManager _fobManager;
    private readonly IPlayerService _playerService;
    private bool _isHidden;
    public FobElement[] Fobs { get; } = ElementPatterns.CreateArray<FobElement>("Fob_{0}/Fob{1}_{0}", 1, to: 12);

    public FobHUD(IServiceProvider serviceProvider, AssetConfiguration assetConfig, ILoggerFactory loggerFactory)
        : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:FobHUD"), debugLogging: false, staticKey: true)
    {
        _fobManager = serviceProvider.GetRequiredService<FobManager>();
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
    }

    /// <inheritdoc />
    public void Hide(WarfarePlayer? player)
    {
        _isHidden = true;
        if (player != null)
        {
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
            UpdateForPlayer(player);
            return;
        }

        _isHidden = false;

        foreach (WarfarePlayer pl in _playerService.OnlinePlayers)
        {
            UpdateForPlayer(pl);
        }
    }

    private void UpdateRelevantPlayers(IFob fob)
    {
        foreach (WarfarePlayer player in _playerService.OnlinePlayers.Where(fob.IsVibileToPlayer))
        {
            UpdateForPlayer(player);
        }
    }

    private void UpdateForPlayer(WarfarePlayer player)
    {
        if (_isHidden)
        {
            ClearFromPlayer(player.Connection);
            return;
        }

        SendToPlayer(player.Connection);

        using IEnumerator<IFob> enumerator = _fobManager.Fobs.Where(f => f.IsVibileToPlayer(player)).GetEnumerator();
        bool isDone = false;
        for (int i = 0; i < Fobs.Length; i++)
        {
            FobElement element = Fobs[i];
            if (isDone || !enumerator.MoveNext())
            {
                isDone = true;
                element.Root.Hide(player);
                continue;
            }

            IFob fob = enumerator.Current!;

            string fobName = TranslationFormattingUtility.Colorize(fob.Name.ToUpper(), fob.Color);

            element.Root.Show(player);
            element.FobName.SetText(player, fobName);

            if (fob is IResourceFob resourceFob)
            {
                element.BuildCount.Show(player);
                element.AmmoCount.Show(player);
                element.BuildCount.SetText(player, Mathf.CeilToInt(resourceFob.BuildCount).ToString());

                element.AmmoCount.SetText(player, Mathf.CeilToInt(resourceFob.AmmoCount).ToString());
            }
            else
            {
                element.BuildCount.Hide(player);
                element.AmmoCount.Hide(player);
            }
        }
    }

    public void HandleEvent(PlayerTeamChanged e, IServiceProvider serviceProvider)
    {
        UpdateForPlayer(e.Player);
    }

    public void HandleEvent(FobRegistered e, IServiceProvider serviceProvider)
    {
        UpdateRelevantPlayers(e.Fob);
    }

    public void HandleEvent(FobDeregistered e, IServiceProvider serviceProvider)
    {
        UpdateRelevantPlayers(e.Fob);
    }

    public void HandleEvent(FobBuilt e, IServiceProvider serviceProvider)
    {
        UpdateRelevantPlayers(e.Fob);
    }

    public void HandleEvent(FobDestroyed e, IServiceProvider serviceProvider)
    {
        UpdateRelevantPlayers(e.Fob);
    }

    public void HandleEvent(FobProxyChanged e, IServiceProvider serviceProvider)
    {
        UpdateRelevantPlayers(e.Fob);
    }

    public void HandleEvent(FobSuppliesChanged e, IServiceProvider serviceProvider)
    {
        UpdateRelevantPlayers(e.Fob);
    }

#nullable disable
    public class FobElement
    {
        [Pattern("", Root = true, CleanJoin = '_')]
        public UnturnedUIElement Root { get; set; }

        [Pattern("Name", Mode = FormatMode.Format)]
        public UnturnedLabel FobName { get; set; }

        [Pattern("Build", Mode = FormatMode.Format)]
        public UnturnedLabel BuildCount { get; set; }
        [Pattern("Ammo", Mode = FormatMode.Format)]
        public UnturnedLabel AmmoCount { get; set; }
    }
#nullable restore
}
