using NUnit.Framework;
using Steamworks;
using System;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Tests;

internal class ContributionTrackerTests
{
    [Test]
    public void SingleContributor()
    {
        PlayerContributionTracker tracker = new PlayerContributionTracker();

        tracker.RecordWork(new CSteamID(1), 10f);

        Assert.That(tracker.ContributorCount, Is.EqualTo(1));
        Assert.That(tracker.TotalWorkDone, Is.EqualTo(10f));

        Assert.That(tracker.GetContribution(new CSteamID(2), out float total), Is.EqualTo(0f));
        Assert.That(total, Is.EqualTo(10f));

        Assert.That(tracker.GetContribution(new CSteamID(1), out total), Is.EqualTo(10f));
        Assert.That(total, Is.EqualTo(10f));

        Assert.That(tracker.GetContributionPercentage(new CSteamID(2)), Is.EqualTo(0f));
        Assert.That(tracker.GetContributionPercentage(new CSteamID(1)), Is.EqualTo(1f));
    }

    [Test]
    public void TwoContributors()
    {
        PlayerContributionTracker tracker = new PlayerContributionTracker();

        tracker.RecordWork(new CSteamID(1), 10f);
        tracker.RecordWork(new CSteamID(2), 30f);

        Assert.That(tracker.ContributorCount, Is.EqualTo(2));
        Assert.That(tracker.TotalWorkDone, Is.EqualTo(40f));

        Assert.That(tracker.GetContribution(new CSteamID(3), out float total), Is.EqualTo(0f));
        Assert.That(total, Is.EqualTo(40f));

        Assert.That(tracker.GetContribution(new CSteamID(1), out total), Is.EqualTo(10f));
        Assert.That(total, Is.EqualTo(40f));

        Assert.That(tracker.GetContribution(new CSteamID(2), out total), Is.EqualTo(30f));
        Assert.That(total, Is.EqualTo(40f));

        Assert.That(tracker.GetContributionPercentage(new CSteamID(3)), Is.EqualTo(0f));
        Assert.That(tracker.GetContributionPercentage(new CSteamID(1)), Is.EqualTo(0.25f));
        Assert.That(tracker.GetContributionPercentage(new CSteamID(2)), Is.EqualTo(0.75f));
    }

    [Test]
    public void MoreContributors()
    {
        PlayerContributionTracker tracker = new PlayerContributionTracker();

        tracker.RecordWork(new CSteamID(1), 10f);
        tracker.RecordWork(new CSteamID(2), 30f);
        tracker.RecordWork(new CSteamID(3), 60f);

        Assert.That(tracker.ContributorCount, Is.EqualTo(3));
        Assert.That(tracker.TotalWorkDone, Is.EqualTo(100f));

        Assert.That(tracker.GetContribution(new CSteamID(4), out float total), Is.EqualTo(0f));
        Assert.That(total, Is.EqualTo(100f));

        Assert.That(tracker.GetContribution(new CSteamID(1), out total), Is.EqualTo(10f));
        Assert.That(total, Is.EqualTo(100f));

        Assert.That(tracker.GetContribution(new CSteamID(2), out total), Is.EqualTo(30f));
        Assert.That(total, Is.EqualTo(100f));

        Assert.That(tracker.GetContribution(new CSteamID(3), out total), Is.EqualTo(60f));
        Assert.That(total, Is.EqualTo(100f));

        Assert.That(tracker.GetContributionPercentage(new CSteamID(4)), Is.EqualTo(0f));
        Assert.That(tracker.GetContributionPercentage(new CSteamID(1)), Is.EqualTo(0.1f));
        Assert.That(tracker.GetContributionPercentage(new CSteamID(2)), Is.EqualTo(0.3f));
        Assert.That(tracker.GetContributionPercentage(new CSteamID(3)), Is.EqualTo(0.6f));
    }

    [Test]
    public void UpdateWork()
    {
        PlayerContributionTracker tracker = new PlayerContributionTracker();

        tracker.RecordWork(new CSteamID(1), 10f);
        tracker.RecordWork(new CSteamID(1), 10f);

        Assert.That(tracker.ContributorCount, Is.EqualTo(1));
        Assert.That(tracker.TotalWorkDone, Is.EqualTo(20f));

        Assert.That(tracker.GetContribution(new CSteamID(2), out float total), Is.EqualTo(0f));
        Assert.That(total, Is.EqualTo(20f));

        Assert.That(tracker.GetContribution(new CSteamID(1), out total), Is.EqualTo(20f));
        Assert.That(total, Is.EqualTo(20f));

        Assert.That(tracker.GetContributionPercentage(new CSteamID(2)), Is.EqualTo(0f));
        Assert.That(tracker.GetContributionPercentage(new CSteamID(1)), Is.EqualTo(1f));
    }

    [Test]
    public void UpdateWithTime()
    {
        DateTime minDt = new DateTime(1000, 1, 1);
        DateTime maxDt = new DateTime(1000, 1, 2);
        PlayerContributionTracker tracker = new PlayerContributionTracker();

        tracker.RecordWork(new CSteamID(1), 10f, minDt);
        tracker.RecordWork(new CSteamID(1), 10f, maxDt);

        Assert.That(tracker.ContributorCount, Is.EqualTo(1));
        Assert.That(tracker.TotalWorkDone, Is.EqualTo(20f));

        DateTime threshold = minDt.Add(TimeSpan.FromSeconds(1));
        Assert.That(tracker.GetContribution(new CSteamID(2), threshold, out float total), Is.EqualTo(0f));
        Assert.That(total, Is.EqualTo(20f));

        Assert.That(tracker.GetContribution(new CSteamID(1), threshold, out total), Is.EqualTo(20f));
        Assert.That(total, Is.EqualTo(20f));

        Assert.That(tracker.GetContributionPercentage(new CSteamID(2), threshold), Is.EqualTo(0f));
        Assert.That(tracker.GetContributionPercentage(new CSteamID(1), threshold), Is.EqualTo(1f));
    }

    [Test]
    public void UpdateWithTimeTwoPlayers()
    {
        DateTime minDt = new DateTime(1000, 1, 1);
        DateTime maxDt = new DateTime(1000, 1, 2);
        PlayerContributionTracker tracker = new PlayerContributionTracker();

        tracker.RecordWork(new CSteamID(1), 10f, minDt);
        tracker.RecordWork(new CSteamID(2), 10f, maxDt);

        Assert.That(tracker.ContributorCount, Is.EqualTo(2));
        Assert.That(tracker.TotalWorkDone, Is.EqualTo(20f));

        DateTime threshold = minDt.Add(TimeSpan.FromSeconds(1));
        Assert.That(tracker.GetContribution(new CSteamID(2), threshold, out float total), Is.EqualTo(10f));
        Assert.That(total, Is.EqualTo(10f));

        Assert.That(tracker.GetContribution(new CSteamID(1), threshold, out total), Is.EqualTo(0f));
        Assert.That(total, Is.EqualTo(10f));

        Assert.That(tracker.GetContributionPercentage(new CSteamID(2), threshold), Is.EqualTo(1f));
        Assert.That(tracker.GetContributionPercentage(new CSteamID(1), threshold), Is.EqualTo(0f));
    }
}
