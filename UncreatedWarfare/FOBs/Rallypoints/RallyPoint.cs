using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.FOBs.Rallypoints;
public class RallyPoint : IBuildableFob, IDisposable
{
    private const int BurnRadius = 80;
    private const int DeployTimer = 20;
    private readonly IPlayerService _playerService;
    private readonly ILoopTicker _loopTicker;
    private DateTime _deploymentStarted;

    public IBuildable Buildable { get; protected set; }
    public string Name { get; }
    public Color32 Color { get; }
    public Team Team { get; }
    public Squad Squad { get; }
    public Vector3 SpawnPosition => new Vector3(Buildable.Position.x, Buildable.Position.y + 1, Buildable.Position.z);
    public float Yaw => 0f;
    public bool IsBurned { get; private set; }
    public bool IsDeploying => (DateTime.Now - _deploymentStarted).TotalSeconds <= DeployTimer; // rally points commbine player deployments so that they all teleport at the same time.
    

    public RallyPoint(IBuildable buildable, Squad squad, IServiceProvider serviceProvider)
    {
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
        Buildable = buildable;
        Name = "Rally Point";
        Squad = squad;
        Team = Squad.Team;
        _deploymentStarted = DateTime.MinValue;

        Color = ColorUtility.TryParseHtmlString("#67ff85", out Color color) ? color : UnityEngine.Color.white;

        _loopTicker = serviceProvider.GetRequiredService<ILoopTickerFactory>().CreateTicker(TimeSpan.FromSeconds(1f), true, true, OnTick);
    }

    private void OnTick(ILoopTicker ticker, TimeSpan timeSinceStart, TimeSpan deltaTime)
    {
        if (IsBurned)
            return;

        // todo: should the rallypoint be destroyed here?
        IsBurned = CheckBurned();
    }
    private bool CheckBurned()
    {
        return _playerService.OnlinePlayers.Any(p => p.Team != Team && MathUtility.WithinRange(p.Position, Buildable.Position, BurnRadius));
    }
    public bool IsVibileToPlayer(WarfarePlayer player) => Squad.ContainsPlayer(player);
    public bool CheckDeployableFrom(WarfarePlayer player, ChatService chatService, DeploymentTranslations translations, in DeploySettings settings, IDeployable deployingTo)
    {
        return true;
    }

    public bool CheckDeployableTo(WarfarePlayer player, ChatService chatService, DeploymentTranslations translations, in DeploySettings settings)
    {
        if (IsBurned)
        {
            chatService.Send(player, translations.RallyPointBurned);
            return false;
        }

        return true;
    }

    public bool CheckDeployableToTick(WarfarePlayer player, ChatService chatService, DeploymentTranslations translations, in DeploySettings settings)
    {
        if (IsBurned)
        {
            chatService.Send(player, translations.RallyPointBurned);
            return false;
        }

        return true;
    }

    public int CompareTo(IFob other)
    {
        return ReferenceEquals(other, this) ? 0 : 1;
    }

    public TimeSpan GetDelay(WarfarePlayer player)
    {
        if (!IsDeploying) // no one is deploying, the timer is the full rally delay
        {
            // timer officially starts here
            _deploymentStarted = DateTime.Now;
            return TimeSpan.FromSeconds(DeployTimer);
        }
        // otherwise, it's the time remaining since the deploy was started
        TimeSpan timeSinceDeployStarted = DateTime.Now - _deploymentStarted;
        return TimeSpan.FromSeconds(DeployTimer) - timeSinceDeployStarted;
    }

    public string Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        return formatter.Colorize(Name, Color, parameters.Options);
    }

    bool IDeployable.IsSafeZone => false;

    /// <inheritdoc />
    public void Dispose()
    {
        _loopTicker.Dispose();
    }
}
