using Stripe;
using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Events.Models.Fobs;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.FOBs;
public class BuildableFob : BasePlayableFob
{
    public bool IsBuilt { get; private set; }
    public bool HasBeenRebuilt { get; private set; }
    public override Color32 Color
    {
        get
        {
            if (!IsBuilt)
                return UnityEngine.Color.gray;

            return base.Color;
        }
    }
    public BuildableFob(IServiceProvider serviceProvider, string name, IBuildable buildable) : base(serviceProvider, name, buildable)
    {
        IsBuilt = false;
        HasBeenRebuilt = false;
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
}
