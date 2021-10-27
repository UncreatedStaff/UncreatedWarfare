using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Revives;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;

namespace Uncreated.Warfare.Gamemodes
{
    /// <summary>Gamemode with 2 teams</summary>
    public abstract class TeamGamemode : Gamemode
    {
        public TeamManager TeamManager;
        public JoinManager JoinManager;

        public virtual bool UseJoinUI { get => true; }
        public virtual bool EnableAMC { get => true; }

        protected TeamGamemode(string Name, float EventLoopSpeed) : base(Name, EventLoopSpeed)
        {

        }

        public override void Init()
        {
            if (UseJoinUI)
            {
                JoinManager = gameObject.AddComponent<JoinManager>();
                JoinManager.Initialize();
            }
            base.Init();
        }
        public override void OnLevelLoaded()
        {
            TeamManager = new TeamManager();
            base.OnLevelLoaded();
        }
        public override void Dispose()
        {
            base.Dispose();
            JoinManager?.Dispose();
        }

    }
}
