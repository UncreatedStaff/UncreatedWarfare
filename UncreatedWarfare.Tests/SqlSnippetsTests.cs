using NUnit.Framework;
using System;
using System.Text;
using Uncreated.Warfare.Database.Manual;

namespace Uncreated.Warfare.Tests;
internal class SqlSnippetsTests
{
    [Test]
    [TestCase(0, 1)]
    [TestCase(1, 1)]
    [TestCase(0, 4)]
    [TestCase(4, 4)]
    [TestCase(0, 20)]
    [TestCase(1, 20)]
    [TestCase(9, 20)]
    [TestCase(0, 101)]
    public void TestParameterList(int startIndex, int length)
    {
        string list = MySqlSnippets.ParameterList(startIndex, length);

        StringBuilder sb = new StringBuilder();
        MySqlSnippets.AppendParameterList(sb, startIndex, length);

        Assert.That(list, Is.EqualTo(sb.ToString()));
    }

    [Test]
    public void TestColumnUpdateList([Values(1, 4, 32)] int ct, [Values(0, 1)] int skip, [Values(0, 1, 13)] int startIndex)
    {
        string[] args = new string[ct];
        for (int i = 0; i < args.Length; ++i)
            args[i] = ct.ToString();

        string list = MySqlSnippets.ColumnUpdateList(startIndex, skip, args);

        if (skip >= ct)
            skip = ct - 1;

        StringBuilder slowBuild = new StringBuilder();
        for (int i = skip; i < ct; ++i)
        {
            slowBuild.Append('`').Append(args[i]).Append('`').Append("=@").Append(i - skip + startIndex).Append(',');
        }

        Assert.That(list, Is.EqualTo(slowBuild.ToString(0, slowBuild.Length - 1)));
    }

    [Test]
    public void TestAliasList([Range(1, 13)] int ct)
    {
        string[] args = new string[ct];
        for (int i = 0; i < args.Length; ++i)
            args[i] = ct.ToString();
        
        string list = MySqlSnippets.AliasedColumnList("alias", args);

        StringBuilder slowBuild = new StringBuilder();
        foreach (string arg in args)
        {
            slowBuild.Append("`alias`.`").Append(arg).Append('`').Append(',');
        }

        Assert.That(list, Is.EqualTo(slowBuild.ToString(0, slowBuild.Length - 1)));
    }

    [Test]
    public void TestList([Range(1, 13)] int ct)
    {
        string[] args = new string[ct];
        for (int i = 0; i < args.Length; ++i)
            args[i] = ct.ToString();
        
        string list = MySqlSnippets.ColumnList(args);

        StringBuilder slowBuild = new StringBuilder();
        foreach (string arg in args)
        {
            slowBuild.Append('`').Append(arg).Append('`').Append(',');
        }

        Assert.That(list, Is.EqualTo(slowBuild.ToString(0, slowBuild.Length - 1)));
    }
}
