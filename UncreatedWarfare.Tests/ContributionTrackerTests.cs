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

        tracker.RecordWork(new CSteamID(1), false, 10f);

        Assert.That(tracker.ContributorCount, Is.EqualTo(1));
        Assert.That(tracker.TotalWorkDone, Is.EqualTo(10f));

        Assert.That(tracker.GetContribution(new CSteamID(2), false, out float total), Is.EqualTo(0f));
        Assert.That(total, Is.EqualTo(10f));

        Assert.That(tracker.GetContribution(new CSteamID(1), false, out total), Is.EqualTo(10f));
        Assert.That(total, Is.EqualTo(10f));

        Assert.That(tracker.GetContributionPercentage(new CSteamID(2), false), Is.EqualTo(0f));
        Assert.That(tracker.GetContributionPercentage(new CSteamID(1), false), Is.EqualTo(1f));
    }

    [Test]
    public void TwoContributors()
    {
        PlayerContributionTracker tracker = new PlayerContributionTracker();

        tracker.RecordWork(new CSteamID(1), false, 10f);
        tracker.RecordWork(new CSteamID(2), false, 30f);

        Assert.That(tracker.ContributorCount, Is.EqualTo(2));
        Assert.That(tracker.TotalWorkDone, Is.EqualTo(40f));

        Assert.That(tracker.GetContribution(new CSteamID(3), false, out float total), Is.EqualTo(0f));
        Assert.That(total, Is.EqualTo(40f));

        Assert.That(tracker.GetContribution(new CSteamID(1), false, out total), Is.EqualTo(10f));
        Assert.That(total, Is.EqualTo(40f));

        Assert.That(tracker.GetContribution(new CSteamID(2), false, out total), Is.EqualTo(30f));
        Assert.That(total, Is.EqualTo(40f));

        Assert.That(tracker.GetContributionPercentage(new CSteamID(3), false), Is.EqualTo(0f));
        Assert.That(tracker.GetContributionPercentage(new CSteamID(1), false), Is.EqualTo(0.25f));
        Assert.That(tracker.GetContributionPercentage(new CSteamID(2), false), Is.EqualTo(0.75f));
    }

    [Test]
    public void MoreContributors()
    {
        PlayerContributionTracker tracker = new PlayerContributionTracker();

        tracker.RecordWork(new CSteamID(1), false, 10f);
        tracker.RecordWork(new CSteamID(2), false, 30f);
        tracker.RecordWork(new CSteamID(3), false, 60f);

        Assert.That(tracker.ContributorCount, Is.EqualTo(3));
        Assert.That(tracker.TotalWorkDone, Is.EqualTo(100f));

        Assert.That(tracker.GetContribution(new CSteamID(4), false, out float total), Is.EqualTo(0f));
        Assert.That(total, Is.EqualTo(100f));

        Assert.That(tracker.GetContribution(new CSteamID(1), false, out total), Is.EqualTo(10f));
        Assert.That(total, Is.EqualTo(100f));

        Assert.That(tracker.GetContribution(new CSteamID(2), false, out total), Is.EqualTo(30f));
        Assert.That(total, Is.EqualTo(100f));

        Assert.That(tracker.GetContribution(new CSteamID(3), false, out total), Is.EqualTo(60f));
        Assert.That(total, Is.EqualTo(100f));

        Assert.That(tracker.GetContributionPercentage(new CSteamID(4), false), Is.EqualTo(0f));
        Assert.That(tracker.GetContributionPercentage(new CSteamID(1), false), Is.EqualTo(0.1f));
        Assert.That(tracker.GetContributionPercentage(new CSteamID(2), false), Is.EqualTo(0.3f));
        Assert.That(tracker.GetContributionPercentage(new CSteamID(3), false), Is.EqualTo(0.6f));
    }

    [Test]
    public void UpdateWork()
    {
        PlayerContributionTracker tracker = new PlayerContributionTracker();

        tracker.RecordWork(new CSteamID(1), false, 10f);
        tracker.RecordWork(new CSteamID(1), false, 10f);

        Assert.That(tracker.ContributorCount, Is.EqualTo(1));
        Assert.That(tracker.TotalWorkDone, Is.EqualTo(20f));

        Assert.That(tracker.GetContribution(new CSteamID(2), false, out float total), Is.EqualTo(0f));
        Assert.That(total, Is.EqualTo(20f));

        Assert.That(tracker.GetContribution(new CSteamID(1), false, out total), Is.EqualTo(20f));
        Assert.That(total, Is.EqualTo(20f));

        Assert.That(tracker.GetContributionPercentage(new CSteamID(2), false), Is.EqualTo(0f));
        Assert.That(tracker.GetContributionPercentage(new CSteamID(1), false), Is.EqualTo(1f));
    }

    [Test]
    public void UpdateWithTime()
    {
        DateTime minDt = new DateTime(1000, 1, 1);
        DateTime maxDt = new DateTime(1000, 1, 2);
        PlayerContributionTracker tracker = new PlayerContributionTracker();

        tracker.RecordWork(new CSteamID(1), false, 10f, minDt);
        tracker.RecordWork(new CSteamID(1), false, 10f, maxDt);

        Assert.That(tracker.ContributorCount, Is.EqualTo(1));
        Assert.That(tracker.TotalWorkDone, Is.EqualTo(20f));

        DateTime threshold = minDt.Add(TimeSpan.FromSeconds(1));
        Assert.That(tracker.GetContribution(new CSteamID(2), false, threshold, out float total), Is.EqualTo(0f));
        Assert.That(total, Is.EqualTo(20f));

        Assert.That(tracker.GetContribution(new CSteamID(1), false, threshold, out total), Is.EqualTo(20f));
        Assert.That(total, Is.EqualTo(20f));

        Assert.That(tracker.GetContributionPercentage(new CSteamID(2), false, threshold), Is.EqualTo(0f));
        Assert.That(tracker.GetContributionPercentage(new CSteamID(1), false, threshold), Is.EqualTo(1f));
    }

    [Test]
    public void UpdateWithTimeTwoPlayers()
    {
        DateTime minDt = new DateTime(1000, 1, 1);
        DateTime maxDt = new DateTime(1000, 1, 2);
        PlayerContributionTracker tracker = new PlayerContributionTracker();

        tracker.RecordWork(new CSteamID(1), false, 10f, minDt);
        tracker.RecordWork(new CSteamID(2), false, 10f, maxDt);

        Assert.That(tracker.ContributorCount, Is.EqualTo(2));
        Assert.That(tracker.TotalWorkDone, Is.EqualTo(20f));

        DateTime threshold = minDt.Add(TimeSpan.FromSeconds(1));
        Assert.That(tracker.GetContribution(new CSteamID(2), false, threshold, out float total), Is.EqualTo(10f));
        Assert.That(total, Is.EqualTo(10f));

        Assert.That(tracker.GetContribution(new CSteamID(1), false, threshold, out total), Is.EqualTo(0f));
        Assert.That(total, Is.EqualTo(10f));

        Assert.That(tracker.GetContributionPercentage(new CSteamID(2), false, threshold), Is.EqualTo(1f));
        Assert.That(tracker.GetContributionPercentage(new CSteamID(1), false, threshold), Is.EqualTo(0f));
    }
}
