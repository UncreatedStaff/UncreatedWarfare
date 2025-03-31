using Microsoft.Extensions.Configuration;
using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util.DamageTracking;

namespace Uncreated.Warfare.FOBs;
public class BunkerFob : ResourceFob
{
    public bool IsBuilt { get; private set; }
    public bool HasBeenRebuilt { get; private set; }
    public DamageTracker DamageTracker { get; }
    public CSteamID Creator => Buildable.Owner;
    public override Color32 Color
    {
        get
        {
            if (!IsBuilt)
                return UnityEngine.Color.gray;

            return base.Color;
        }
    }
    public BunkerFob(IServiceProvider serviceProvider, string name, IBuildable buildable) : base(serviceProvider, name, buildable)
    {
        IsBuilt = false;
        HasBeenRebuilt = false;
        DamageTracker = new DamageTracker(name);
    }
    public void MarkBuilt(IBuildable newBuildable)
    {
        IsBuilt = true;
        HasBeenRebuilt = true;
        Buildable = newBuildable;
    }
    public void MarkUnbuilt(IBuildable newBuildable)
    {
        IsBuilt = false;
        Buildable = newBuildable;
    }
    public override bool CheckDeployableToTick(WarfarePlayer player, ChatService chatService, DeploymentTranslations translations, in DeploySettings settings)
    {
        if (!base.CheckDeployableTo(player, chatService, translations, settings))
            return false;

        if (!IsBuilt)
        {
            chatService.Send(player, translations.DeployDestroyed, this);
            return false;
        }

        return true;
    }
    public override TimeSpan GetDelay(WarfarePlayer player)
    {
        return TimeSpan.FromSeconds(FobManager.Configuration.GetValue("FobDeployDelay", 10));
    }
}
