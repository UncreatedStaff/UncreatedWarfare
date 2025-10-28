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
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Translations.Util;

namespace Uncreated.Warfare.Fobs.UI;

[UnturnedUI(BasePath = "FobList")]
public class FobHUD : 
    UnturnedUI,
    IEventListener<IPlayerNeedsFobUIUpdateEvent>,
    IEventListener<IFobNeedsUIUpdateEvent>,
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

    private void UpdateForPlayer(WarfarePlayer player)
    {
        if (_isHidden)
        {
            ClearFromPlayer(player.Connection);
            return;
        }

        using IEnumerator<IFob> enumerator = _fobManager.Fobs.Where(f => f.IsVisibleToPlayer(player)).GetEnumerator();
        if (!enumerator.MoveNext())
        {
            ClearFromPlayer(player.Connection);
            return;
        }

        SendToPlayer(player.Connection);

        bool isDone = false;
        bool isFirst = true;
        for (int i = 0; i < Fobs.Length; i++)
        {
            FobElement element = Fobs[i];
            if (isDone || !(isFirst || enumerator.MoveNext()))
            {
                isFirst = false;
                isDone = true;
                element.Root.Hide(player);
                continue;
            }

            isFirst = false;
            IFob fob = enumerator.Current!;

            string fobName = fob.GetUIDisplay(player.Team);
            if (string.IsNullOrEmpty(fobName))
            {
                fobName = TranslationFormattingUtility.Colorize(fob.Name.ToUpper(), fob.GetColor(player.Team));
            }

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

    public void HandleEvent(IPlayerNeedsFobUIUpdateEvent e, IServiceProvider serviceProvider)
    {
        if (e.Player != null)
            UpdateForPlayer(e.Player);
    }

    public void HandleEvent(IFobNeedsUIUpdateEvent e, IServiceProvider serviceProvider)
    {
        if (e.Fob == null)
            return;

        foreach (WarfarePlayer player in _playerService.OnlinePlayers.Where(e.Fob.IsVisibleToPlayer))
        {
            UpdateForPlayer(player);
        }
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
