using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Revives;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;

namespace Uncreated.Warfare.Gamemodes
{
    /// <summary>Gamemode with 2 teams</summary>
    public abstract class TeamGamemode : Gamemode, ITeams//, IStructureSaving, IFOBs, IKitRequests, IRevives, ISquads, IImplementsLeaderboard
    {
        protected TeamManager _teamManager;
        public TeamManager TeamManager { get => _teamManager; }
        protected JoinManager _joinManager;
        public JoinManager JoinManager { get => _joinManager; }

        public virtual bool UseJoinUI { get => true; }
        public virtual bool EnableAMC { get => true; }

        protected TeamGamemode(string Name, float EventLoopSpeed) : base(Name, EventLoopSpeed)
        {

        }

        public override void Init()
        {
            if (UseJoinUI)
            {
                _joinManager = gameObject.AddComponent<JoinManager>();
            }
            base.Init();
        }
        public override void OnLevelLoaded()
        {
            _teamManager = new TeamManager();
            base.OnLevelLoaded();
        }
        public override void Dispose()
        {
            base.Dispose();
            _joinManager?.Dispose();
        }

    }
}
