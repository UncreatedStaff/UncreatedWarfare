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
    private void UpdateForTeam(Team team)
    {
        List<IFob> friendlyFobs = _fobManager.Fobs.Where(f => f.Team == team).ToList();
        for (int i = 0; i < Fobs.Length; i++)
        {
            FobElement element = Fobs[i];
            if (i < friendlyFobs.Count)
            {
                IFob fob = friendlyFobs[i];

                UpdateForPlayers(element, team, fob);
            }
            else
                UpdateForPlayers(element, team, null);
        }
    }
    private void UpdateForPlayers(FobElement element, Team team, IFob? fob)
    {
        if (fob != null)
        {
            foreach (WarfarePlayer player in _playerService.OnlinePlayersOnTeam(team))
            {
                SendToPlayer(player.Connection); // todo: maybe only send the UI when a player joins a team?
                string fobName = TranslationFormattingUtility.Colorize(fob.Name.ToUpper(), fob.Color);
                
                element.Root.Show(player);
                element.FobName.SetText(player, fobName);

                if (fob is IResourceFob resourceFob)
                {
                    element.BuildCount.Show(player);
                    element.AmmoCount.Show(player);
                    element.BuildCount.SetText(player, resourceFob.BuildCount.ToString());
                    element.AmmoCount.SetText(player, resourceFob.AmmoCount.ToString());
                }
            }
        }
        else
        {
            foreach (WarfarePlayer player in _playerService.OnlinePlayersOnTeam(team))
            {
                element.Root.Hide(player);
            }
        }
    }

    public void HandleEvent(FobRegistered e, IServiceProvider serviceProvider)
    {
        Console.WriteLine("sending FOB UI for team: " + e.Fob.Team);
        UpdateForTeam(e.Fob.Team);
    }

    public void HandleEvent(FobDeregistered e, IServiceProvider serviceProvider)
    {
        UpdateForTeam(e.Fob.Team);
    }

    public void HandleEvent(FobBuilt e, IServiceProvider serviceProvider)
    {
        UpdateForTeam(e.Fob.Team);
    }

    public void HandleEvent(FobDestroyed e, IServiceProvider serviceProvider)
    {
        UpdateForTeam(e.Fob.Team);
    }

    public void HandleEvent(FobProxyChanged e, IServiceProvider serviceProvider)
    {
        UpdateForTeam(e.Fob.Team);
    }

    public void HandleEvent(FobSuppliesChanged e, IServiceProvider serviceProvider)
    {
        UpdateForTeam(e.Fob.Team);
    }

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
}
