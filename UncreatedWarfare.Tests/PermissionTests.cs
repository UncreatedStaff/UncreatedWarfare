using DanielWillett.SpeedBytes;
using DanielWillett.SpeedBytes.Formatting;
using NUnit.Framework;
using System;
using Uncreated.Warfare.Players.Permissions;

namespace Uncreated.Warfare.Tests;

public class PermissionTests
{
    [SetUp]
    public static void Setup()
    {
        TestHelpers.SetupMainThread();
    }

    [Test]
    [TestCase("warfare::context.test")]
    [TestCase("unturned::context.test.test2")]
    public void ParseBasicPermissionLeaf(string leafStr)
    {
        PermissionLeaf leaf = PermissionLeaf.Parse(leafStr);

        Assert.AreEqual(leafStr, leaf.ToString());
    }

    [Test]
    [TestCase("context.test")]
    [TestCase("plugin2::*")]
    [TestCase("plugin5::context.test")]
    [TestCase("warfare::context.*")]
    [TestCase("plugin4::context.test::test2")]
    [TestCase("warfare::*")]
    [TestCase("warfare::")]
    [TestCase("::context.test")]
    [TestCase("")]
    [TestCase("*")]
    [TestCase(null)]
    [TestCase("::")]
    public void FailParseBasicPermissionLeaf(string leafStr)
    {
        Assert.Throws<FormatException>(() =>
        {
            PermissionLeaf leaf = PermissionLeaf.Parse(leafStr);

            Console.WriteLine(leaf.ToString());
        });
    }

    [Test]
    [TestCase("warfare::context.test")]
    [TestCase("unturned::context.test.test2")]
    [TestCase("warfare::context.test")]
    [TestCase("unturned::*")]
    [TestCase("+warfare::context.test")]
    [TestCase("+unturned::context.test.test2")]
    [TestCase("+warfare::context.test")]
    [TestCase("+unturned::*")]
    [TestCase("-warfare::context.test")]
    [TestCase("-unturned::context.test.test2")]
    [TestCase("-unturned::*")]
    public void ParseBasicPermissionBranch(string branchStr)
    {
        PermissionBranch branch = PermissionBranch.Parse(branchStr);
        Console.WriteLine($"{branchStr} -> {branch} (lvl {branch.WildcardLevel}).");

        if (branchStr.Length > 0 && branchStr[0] == '+')
            branchStr = branchStr[1..];

        Assert.AreEqual(branchStr, branch.ToString());
    }

    [Test]
    [TestCase("context.test")]
    [TestCase("plugin5::context.test")]
    [TestCase("plugin4::context.test::test2")]
    [TestCase("plugin::5::context.test:test2")]
    [TestCase("warfare::")]
    [TestCase("::context.test")]
    [TestCase("")]
    [TestCase(null)]
    [TestCase("::")]
    public void FailParseBasicPermissionBranch(string branchStr)
    {
        Assert.Throws<FormatException>(() =>
        {
            PermissionBranch branch = PermissionBranch.Parse(branchStr);

            Console.WriteLine(branch);
        });
    }

    [Test]
    [TestCase("warfare::context.test", "warfare::context.test")]
    [TestCase("warfare::context.*", "warfare::context.test")]
    [TestCase("warfare::context.*", "warfare::context")]
    [TestCase("warfare::*", "warfare::context.test")]
    [TestCase("*", "warfare::context.test")]
    [TestCase("unturned::context.test.test.test.test.test.test.*", "unturned::context.test.test.test.test.test.test.leaf")]
    [TestCase("unturned::context.test", "unturned::context.test")]
    [TestCase("unturned::context.*", "unturned::context.test")]
    [TestCase("unturned::*", "unturned::context.test")]
    [TestCase("*", "unturned::context.test")]
    public void ContainsLeafTests(string branchStr, string leafStr)
    {
        PermissionBranch branch = PermissionBranch.Parse(branchStr);
        PermissionLeaf leaf = PermissionLeaf.Parse(leafStr);

        Assert.IsTrue(branch.Valid);
        Assert.IsTrue(leaf.Valid);

        Assert.IsTrue(branch.Contains(leaf));
    }

    [Test]
    [TestCase("warfare::context.test", "unturned::context.test")]
    [TestCase("warfare::context.test", "warfare::context.test.test2")]
    [TestCase("unturned::context.test", "warfare::context.test")]
    [TestCase("unturned::context.test", "unturned::context.test.test2")]
    [TestCase("unturned::*", "warfare::context.test.test2")]
    public void NotContainsLeafTests(string branchStr, string leafStr)
    {
        PermissionBranch branch = PermissionBranch.Parse(branchStr);
        PermissionLeaf leaf = PermissionLeaf.Parse(leafStr);

        Assert.IsTrue(branch.Valid);
        Assert.IsTrue(leaf.Valid);

        Assert.IsFalse(branch.Contains(leaf));
    }

    [Test]
    [TestCase("warfare::context.test")]
    [TestCase("unturned::context.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test")]
    public void TestLeafIO(string leafStr)
    {
        PermissionLeaf leaf = PermissionLeaf.Parse(leafStr);
        Console.WriteLine(leaf.ToString());
        Assert.IsTrue(leaf.Valid);

        ByteWriter writer = new ByteWriter(64);
        ByteReader reader = new ByteReader();

        PermissionLeaf.Write(writer, leaf);

        reader.LoadNew(writer.ToArray());
        Console.WriteLine(Environment.NewLine + ByteFormatter.FormatBinary(reader.InternalBuffer!, ByteStringFormat.Base16 | ByteStringFormat.Columns8));

        Assert.AreEqual(leaf, PermissionLeaf.Read(reader));
    }

    [Test]
    [TestCase("warfare::context.test")]
    [TestCase("unturned::context.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test")]
    [TestCase("warfare::context.*")]
    [TestCase("unturned::context.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.*")]
    [TestCase("*")]
    [TestCase("unturned::*")]
    [TestCase("-warfare::context.test")]
    [TestCase("-unturned::context.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test")]
    [TestCase("-warfare::context.*")]
    [TestCase("-unturned::context.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.*")]
    [TestCase("-*")]
    [TestCase("-unturned::*")]
    [TestCase("+warfare::context.test")]
    [TestCase("+unturned::context.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test")]
    [TestCase("+warfare::context.*")]
    [TestCase("+unturned::context.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.test.*")]
    [TestCase("+*")]
    [TestCase("+unturned::*")]
    public void TestBranchIO(string branchStr)
    {
        PermissionBranch branch = PermissionBranch.Parse(branchStr);
        Console.WriteLine(branch.ToString());
        Assert.IsTrue(branch.Valid);

        ByteWriter writer = new ByteWriter(64);
        ByteReader reader = new ByteReader();

        PermissionBranch.Write(writer, branch);

        reader.LoadNew(writer.ToArray());
        Console.WriteLine(Environment.NewLine + ByteFormatter.FormatBinary(reader.InternalBuffer!, ByteStringFormat.Base16 | ByteStringFormat.Columns8));

        Assert.AreEqual(branch, PermissionBranch.Read(reader));
    }
}
