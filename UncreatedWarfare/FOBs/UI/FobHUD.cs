using Microsoft.Extensions.DependencyInjection;
using Stripe;
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
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Translations.Util;

namespace Uncreated.Warfare.Fobs.UI;

[UnturnedUI(BasePath = "FobList")]
public class FobHUD : 
    UnturnedUI,
    IEventListener<FobRegistered>,
    IEventListener<FobDeregistered>,
    IEventListener<FobBuilt>,
    IEventListener<FobDestroyed>,
    IEventListener<FobProxyChanged>,
    IEventListener<FobSuppliesChanged>
{
    private readonly FobManager _fobManager;
    private readonly IPlayerService _playerService;
    public FobElement[] Fobs { get; } = ElementPatterns.CreateArray<FobElement>("Fob_{0}/Fob{1}_{0}", 1, to: 12);

    public FobHUD(IServiceProvider serviceProvider, AssetConfiguration assetConfig, ILoggerFactory loggerFactory)
        : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:FobHUD"), /* todo turn off */ debugLogging: true, staticKey: true)
    {
        _fobManager = serviceProvider.GetRequiredService<FobManager>();
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
    }
    private void UpdateRelevantPlayers(IFob fob)
    {
        foreach (WarfarePlayer player in _playerService.OnlinePlayers.Where(p => fob.IsVibileToPlayer(p)))
        {
            UpdateForPlayer(player);
        }
    }
    private void UpdateForPlayer(WarfarePlayer player)
    {
        List<IFob> visibleFobs = _fobManager.Fobs.Where(f => f.IsVibileToPlayer(player)).ToList();
        for (int i = 0; i < Fobs.Length; i++)
        {
            FobElement element = Fobs[i];
            if (i >= visibleFobs.Count)
            {
                element.Root.Hide(player);
                continue;
            }

            IFob fob = visibleFobs[i];

            SendToPlayer(player.Connection); // todo: maybe only send the UI when a player joins a team?
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
