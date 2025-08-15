using System;

namespace Uncreated.Warfare.Layouts.Seeding;

public class SeedingModePlayerVoteManager : IPlayerVoteManager
{
    public bool IsVoting => throw new NotImplementedException();

    public DateTime VoteStart => throw new NotImplementedException();

    public DateTime VoteEnd => throw new NotImplementedException();

    public void StartVote(in VoteSettings settings, Action<VoteResult> callback)
    {
        throw new NotImplementedException();
    }

    public PlayerVoteState GetVoteState(CSteamID player)
    {
        throw new NotImplementedException();
    }
}