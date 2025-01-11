extern alias JetBrains;
using JetBrains::JetBrains.Annotations;

using NUnit.Framework;
using SDG.Unturned;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Quests.Parameters;
using Cysharp.Threading.Tasks;

namespace Uncreated.Warfare.Tests;

#nullable enable
public class QuestParameterParseTests
{
    [SetUp]
    public static void Setup()
    {
        TestHelpers.SetupMainThread();
    }

    [Test]
    public void Int32SelectiveWildcard()
    {
        const string value = "$*";

        Int32ParameterTemplate template = new Int32ParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void Int32InclusiveWildcard()
    {
        const string value = "#*";

        Int32ParameterTemplate template = new Int32ParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = Int32ParameterTemplate.TryParseValue(value, out QuestParameterValue<int>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }
    
    [Test]
    public void Int32ConstantSingleDigit()
    {
        const string value = "1";

        Int32ParameterTemplate template = new Int32ParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = Int32ParameterTemplate.TryParseValue(value, out QuestParameterValue<int>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void Int32ConstantMultiDigit()
    {
        const string value = "20";

        Int32ParameterTemplate template = new Int32ParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = Int32ParameterTemplate.TryParseValue(value, out QuestParameterValue<int>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void Int32SelectiveRangeWithRoundBounded()
    {
        const string value = "$(1:9){5}";

        Int32ParameterTemplate template = new Int32ParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void Int32InclusiveRangeWithRoundBounded()
    {
        const string value = "#(1:9){5}";

        Int32ParameterTemplate template = new Int32ParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = Int32ParameterTemplate.TryParseValue(value, out QuestParameterValue<int>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void Int32SelectiveRangeBounded()
    {
        const string value = "$(1:9)";

        Int32ParameterTemplate template = new Int32ParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void Int32InclusiveRangeBounded()
    {
        const string value = "#(1:9)";

        Int32ParameterTemplate template = new Int32ParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = Int32ParameterTemplate.TryParseValue(value, out QuestParameterValue<int>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void Int32SelectiveRangeWithRoundUpperBounded()
    {
        const string value = "$(:9){5}";

        Int32ParameterTemplate template = new Int32ParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void Int32InclusiveRangeWithRoundUpperBounded()
    {
        const string value = "#(:9){5}";

        Int32ParameterTemplate template = new Int32ParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = Int32ParameterTemplate.TryParseValue(value, out QuestParameterValue<int>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void Int32SelectiveRangeUpperBounded()
    {
        const string value = "$(:9)";

        Int32ParameterTemplate template = new Int32ParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void Int32InclusiveRangeUpperBounded()
    {
        const string value = "#(:9)";

        Int32ParameterTemplate template = new Int32ParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = Int32ParameterTemplate.TryParseValue(value, out QuestParameterValue<int>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void Int32SelectiveRangeWithRoundLowerBounded()
    {
        const string value = "$(1:){5}";

        Int32ParameterTemplate template = new Int32ParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void Int32InclusiveRangeWithRoundLowerBounded()
    {
        const string value = "#(1:){5}";

        Int32ParameterTemplate template = new Int32ParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = Int32ParameterTemplate.TryParseValue(value, out QuestParameterValue<int>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void Int32SelectiveRangeLowerBounded()
    {
        const string value = "$(1:)";

        Int32ParameterTemplate template = new Int32ParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void Int32InclusiveRangeLowerBounded()
    {
        const string value = "#(1:)";

        Int32ParameterTemplate template = new Int32ParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = Int32ParameterTemplate.TryParseValue(value, out QuestParameterValue<int>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void Int32EmptySelectiveSet()
    {
        const string value = "$[]";

        Int32ParameterTemplate template = new Int32ParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void Int32EmptyInclusiveSet()
    {
        const string value = "#[]";

        Int32ParameterTemplate template = new Int32ParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = Int32ParameterTemplate.TryParseValue(value, out QuestParameterValue<int>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void Int32SingleSelectiveSet()
    {
        const string value = "$[1]";

        Int32ParameterTemplate template = new Int32ParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void Int32SingleInclusiveSet()
    {
        const string value = "#[1]";

        Int32ParameterTemplate template = new Int32ParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = Int32ParameterTemplate.TryParseValue(value, out QuestParameterValue<int>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void Int32SelectiveSet()
    {
        const string value = "$[1,2,3,4,56,7,8]";

        Int32ParameterTemplate template = new Int32ParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void Int32InclusiveSet()
    {
        const string value = "#[1,2,3,4,56,7,8]";

        Int32ParameterTemplate template = new Int32ParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = Int32ParameterTemplate.TryParseValue(value, out QuestParameterValue<int>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }


    [Test]
    public void SingleIntSelectiveWildcard()
    {
        const string value = "$*";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleIntInclusiveWildcard()
    {
        const string value = "#*";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = SingleParameterTemplate.TryParseValue(value, out QuestParameterValue<float>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleIntConstant()
    {
        const string value = "1";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = SingleParameterTemplate.TryParseValue(value, out QuestParameterValue<float>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleIntSelectiveRangeWithRoundBounded()
    {
        const string value = "$(1:9){5}";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleIntInclusiveRangeWithRoundBounded()
    {
        const string value = "#(1:9){5}";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = SingleParameterTemplate.TryParseValue(value, out QuestParameterValue<float>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleIntSelectiveRangeBounded()
    {
        const string value = "$(1:9)";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleIntInclusiveRangeBounded()
    {
        const string value = "#(1:9)";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = SingleParameterTemplate.TryParseValue(value, out QuestParameterValue<float>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleIntSelectiveRangeWithRoundUpperBounded()
    {
        const string value = "$(:9){5}";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleIntInclusiveRangeWithRoundUpperBounded()
    {
        const string value = "#(:9){5}";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = SingleParameterTemplate.TryParseValue(value, out QuestParameterValue<float>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleIntSelectiveRangeUpperBounded()
    {
        const string value = "$(:9)";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleIntInclusiveRangeUpperBounded()
    {
        const string value = "#(:9)";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = SingleParameterTemplate.TryParseValue(value, out QuestParameterValue<float>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleIntSelectiveRangeWithRoundLowerBounded()
    {
        const string value = "$(1:){5}";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleIntInclusiveRangeWithRoundLowerBounded()
    {
        const string value = "#(1:){5}";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = SingleParameterTemplate.TryParseValue(value, out QuestParameterValue<float>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleIntSelectiveRangeLowerBounded()
    {
        const string value = "$(1:)";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleIntInclusiveRangeLowerBounded()
    {
        const string value = "#(1:)";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = SingleParameterTemplate.TryParseValue(value, out QuestParameterValue<float>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleIntSingleSelectiveSet()
    {
        const string value = "$[1]";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleIntSingleInclusiveSet()
    {
        const string value = "#[1]";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = SingleParameterTemplate.TryParseValue(value, out QuestParameterValue<float>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleIntSelectiveSet()
    {
        const string value = "$[1,2,3,4,56,7,8]";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleIntInclusiveSet()
    {
        const string value = "#[1,2,3,4,56,7,8]";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = SingleParameterTemplate.TryParseValue(value, out QuestParameterValue<float>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleDecimalConstant()
    {
        const string value = "1.1";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = SingleParameterTemplate.TryParseValue(value, out QuestParameterValue<float>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleDecimalSelectiveRangeWithRoundBounded()
    {
        const string value = "$(1.1:9.9){5}";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleDecimalInclusiveRangeWithRoundBounded()
    {
        const string value = "#(1.1:9.9){5}";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = SingleParameterTemplate.TryParseValue(value, out QuestParameterValue<float>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleDecimalSelectiveRangeBounded()
    {
        const string value = "$(1.1:9.9)";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleDecimalInclusiveRangeBounded()
    {
        const string value = "#(1.1:9.9)";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = SingleParameterTemplate.TryParseValue(value, out QuestParameterValue<float>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleDecimalSelectiveRangeWithRoundUpperBounded()
    {
        const string value = "$(:9.9){5}";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleDecimalInclusiveRangeWithRoundUpperBounded()
    {
        const string value = "#(:9.9){5}";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = SingleParameterTemplate.TryParseValue(value, out QuestParameterValue<float>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleDecimalSelectiveRangeUpperBounded()
    {
        const string value = "$(:9.9)";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleDecimalInclusiveRangeUpperBounded()
    {
        const string value = "#(:9.9)";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = SingleParameterTemplate.TryParseValue(value, out QuestParameterValue<float>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleDecimalSelectiveRangeWithRoundLowerBounded()
    {
        const string value = "$(1.1:){5}";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleDecimalInclusiveRangeWithRoundLowerBounded()
    {
        const string value = "#(1.1:){5}";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = SingleParameterTemplate.TryParseValue(value, out QuestParameterValue<float>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleDecimalSelectiveRangeLowerBounded()
    {
        const string value = "$(1.1:)";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleDecimalInclusiveRangeLowerBounded()
    {
        const string value = "#(1.1:)";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = SingleParameterTemplate.TryParseValue(value, out QuestParameterValue<float>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleEmptySelectiveSet()
    {
        const string value = "$[]";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleEmptyInclusiveSet()
    {
        const string value = "#[]";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = SingleParameterTemplate.TryParseValue(value, out QuestParameterValue<float>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleDecimalSingleSelectiveSet()
    {
        const string value = "$[1.1]";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleDecimalSingleInclusiveSet()
    {
        const string value = "#[1.1]";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = SingleParameterTemplate.TryParseValue(value, out QuestParameterValue<float>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleDecimalSelectiveSet()
    {
        const string value = "$[1.1,2.2,3.3,4.4,56.56,7.7,8.8]";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void SingleDecimalInclusiveSet()
    {
        const string value = "#[1.1,2.2,3.3,4.4,56.56,7.7,8.8]";

        SingleParameterTemplate template = new SingleParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = SingleParameterTemplate.TryParseValue(value, out QuestParameterValue<float>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void StringSelectiveWildcard()
    {
        const string value = "$*";

        Assert.Throws<FormatException>(() =>
        {
            _ = new StringParameterTemplate(value.AsSpan());
        });
    }

    [Test]
    public void StringInclusiveWildcard()
    {
        const string value = "#*";

        StringParameterTemplate template = new StringParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = StringParameterTemplate.TryParseValue(value, out QuestParameterValue<string>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void StringConstant()
    {
        const string value = "t";

        StringParameterTemplate template = new StringParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = StringParameterTemplate.TryParseValue(value, out QuestParameterValue<string>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void StringConstantEmpty()
    {
        const string value = "";

        StringParameterTemplate template = new StringParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = StringParameterTemplate.TryParseValue(value, out QuestParameterValue<string>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void StringEmptySelectiveSet()
    {
        const string value = "$[]";

        StringParameterTemplate template = new StringParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void StringEmptyInclusiveSet()
    {
        const string value = "#[]";

        StringParameterTemplate template = new StringParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = StringParameterTemplate.TryParseValue(value, out QuestParameterValue<string>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void StringSingleSelectiveSet()
    {
        const string value = "$[t]";

        StringParameterTemplate template = new StringParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void StringSingleInclusiveSet()
    {
        const string value = "#[t]";

        StringParameterTemplate template = new StringParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = StringParameterTemplate.TryParseValue(value, out QuestParameterValue<string>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void StringSelectiveSet()
    {
        const string value = @"$[t,te,tes,test,tes\,t,tes\\,t]";

        StringParameterTemplate template = new StringParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void StringInclusiveSet()
    {
        const string value = @"#[t,te,tes,test,tes\,t,tes\\,t]";

        StringParameterTemplate template = new StringParameterTemplate(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = StringParameterTemplate.TryParseValue(value, out QuestParameterValue<string>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void EnumSelectiveWildcard()
    {
        const string value = "$*";

        EnumParameterTemplate<TestEnum> template = new EnumParameterTemplate<TestEnum>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void EnumInclusiveWildcard()
    {
        const string value = "#*";

        EnumParameterTemplate<TestEnum> template = new EnumParameterTemplate<TestEnum>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = EnumParameterTemplate<TestEnum>.TryParseValue(value, out QuestParameterValue<TestEnum>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void EnumConstant()
    {
        const string value = "A";

        EnumParameterTemplate<TestEnum> template = new EnumParameterTemplate<TestEnum>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = EnumParameterTemplate<TestEnum>.TryParseValue(value, out QuestParameterValue<TestEnum>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void EnumSelectiveRangeBounded()
    {
        const string value = "$(A:_)";

        EnumParameterTemplate<TestEnum> template = new EnumParameterTemplate<TestEnum>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void EnumInclusiveRangeBounded()
    {
        const string value = "#(A:_)";

        EnumParameterTemplate<TestEnum> template = new EnumParameterTemplate<TestEnum>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = EnumParameterTemplate<TestEnum>.TryParseValue(value, out QuestParameterValue<TestEnum>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void EnumSelectiveRangeUpperBounded()
    {
        const string value = "$(:_)";

        EnumParameterTemplate<TestEnum> template = new EnumParameterTemplate<TestEnum>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void EnumInclusiveRangeUpperBounded()
    {
        const string value = "#(:_)";

        EnumParameterTemplate<TestEnum> template = new EnumParameterTemplate<TestEnum>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = EnumParameterTemplate<TestEnum>.TryParseValue(value, out QuestParameterValue<TestEnum>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void EnumSelectiveRangeLowerBounded()
    {
        const string value = "$(A:)";

        EnumParameterTemplate<TestEnum> template = new EnumParameterTemplate<TestEnum>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void EnumInclusiveRangeLowerBounded()
    {
        const string value = "#(A:)";

        EnumParameterTemplate<TestEnum> template = new EnumParameterTemplate<TestEnum>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = EnumParameterTemplate<TestEnum>.TryParseValue(value, out QuestParameterValue<TestEnum>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void EnumSelectiveRangeBoundedLonger()
    {
        const string value = "$(Test:WithNumber1)";

        EnumParameterTemplate<TestEnum> template = new EnumParameterTemplate<TestEnum>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void EnumInclusiveRangeBoundedLonger()
    {
        const string value = "#(Test:WithNumber1)";

        EnumParameterTemplate<TestEnum> template = new EnumParameterTemplate<TestEnum>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = EnumParameterTemplate<TestEnum>.TryParseValue(value, out QuestParameterValue<TestEnum>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void EnumSelectiveRangeUpperBoundedLonger()
    {
        const string value = "$(:WithNumber1)";

        EnumParameterTemplate<TestEnum> template = new EnumParameterTemplate<TestEnum>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void EnumInclusiveRangeUpperBoundedLonger()
    {
        const string value = "#(:WithNumber1)";

        EnumParameterTemplate<TestEnum> template = new EnumParameterTemplate<TestEnum>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = EnumParameterTemplate<TestEnum>.TryParseValue(value, out QuestParameterValue<TestEnum>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void EnumSelectiveRangeLowerBoundedLonger()
    {
        const string value = "$(Test:)";

        EnumParameterTemplate<TestEnum> template = new EnumParameterTemplate<TestEnum>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void EnumInclusiveRangeLowerBoundedLonger()
    {
        const string value = "#(Test:)";

        EnumParameterTemplate<TestEnum> template = new EnumParameterTemplate<TestEnum>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = EnumParameterTemplate<TestEnum>.TryParseValue(value, out QuestParameterValue<TestEnum>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void EnumEmptySelectiveSet()
    {
        const string value = "$[]";

        EnumParameterTemplate<TestEnum> template = new EnumParameterTemplate<TestEnum>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void EnumEmptyInclusiveSet()
    {
        const string value = "#[]";

        EnumParameterTemplate<TestEnum> template = new EnumParameterTemplate<TestEnum>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = EnumParameterTemplate<TestEnum>.TryParseValue(value, out QuestParameterValue<TestEnum>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void EnumSingleSelectiveSet()
    {
        const string value = "$[A]";

        EnumParameterTemplate<TestEnum> template = new EnumParameterTemplate<TestEnum>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void EnumSingleInclusiveSet()
    {
        const string value = "#[A]";

        EnumParameterTemplate<TestEnum> template = new EnumParameterTemplate<TestEnum>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = EnumParameterTemplate<TestEnum>.TryParseValue(value, out QuestParameterValue<TestEnum>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void EnumSelectiveSet()
    {
        const string value = "$[A,Test,_,WithNumber1]";

        EnumParameterTemplate<TestEnum> template = new EnumParameterTemplate<TestEnum>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void EnumInclusiveSet()
    {
        const string value = "#[A,Test,_,WithNumber1]";

        EnumParameterTemplate<TestEnum> template = new EnumParameterTemplate<TestEnum>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = EnumParameterTemplate<TestEnum>.TryParseValue(value, out QuestParameterValue<TestEnum>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    private enum TestEnum
    {
        [UsedImplicitly]
        A,

        [UsedImplicitly]
        Test,

        [UsedImplicitly]
        _,

        [UsedImplicitly]
        WithNumber1
    }

    [Test]
    public void AssetSelectiveWildcard()
    {
        const string value = "$*";

        AssetParameterTemplate<Asset> template = new AssetParameterTemplate<Asset>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void AssetInclusiveWildcard()
    {
        const string value = "#*";

        AssetParameterTemplate<Asset> template = new AssetParameterTemplate<Asset>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = AssetParameterTemplate<Asset>.TryParseValue(value, out QuestParameterValue<Guid>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void AssetConstant()
    {
        const string value = "7539a610cb9f4b54bd7bf2b1c4c5d17e";

        AssetParameterTemplate<Asset> template = new AssetParameterTemplate<Asset>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = AssetParameterTemplate<Asset>.TryParseValue(value, out QuestParameterValue<Guid>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void AssetEmptySelectiveSet()
    {
        const string value = "$[]";

        AssetParameterTemplate<Asset> template = new AssetParameterTemplate<Asset>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void AssetEmptyInclusiveSet()
    {
        const string value = "#[]";

        AssetParameterTemplate<Asset> template = new AssetParameterTemplate<Asset>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = AssetParameterTemplate<Asset>.TryParseValue(value, out QuestParameterValue<Guid>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void AssetSingleSelectiveSet()
    {
        const string value = "$[7539a610cb9f4b54bd7bf2b1c4c5d17e]";

        AssetParameterTemplate<Asset> template = new AssetParameterTemplate<Asset>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void AssetSingleInclusiveSet()
    {
        const string value = "#[7539a610cb9f4b54bd7bf2b1c4c5d17e]";

        AssetParameterTemplate<Asset> template = new AssetParameterTemplate<Asset>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = AssetParameterTemplate<Asset>.TryParseValue(value, out QuestParameterValue<Guid>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void AssetSelectiveSet()
    {
        const string value = "$[7539a610cb9f4b54bd7bf2b1c4c5d17e,86a28564d0b24016907cef969ef35a56,dad196400a434b00a2e4a36817981bd2,6f3a8f2e801d483aace9a911377a1581]";

        AssetParameterTemplate<Asset> template = new AssetParameterTemplate<Asset>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));
    }

    [Test]
    public void AssetInclusiveSet()
    {
        const string value = "#[7539a610cb9f4b54bd7bf2b1c4c5d17e,86a28564d0b24016907cef969ef35a56,dad196400a434b00a2e4a36817981bd2,6f3a8f2e801d483aace9a911377a1581]";

        AssetParameterTemplate<Asset> template = new AssetParameterTemplate<Asset>(value.AsSpan());

        Assert.That(template.ToString(), Is.EqualTo(value));

        bool success = AssetParameterTemplate<Asset>.TryParseValue(value, out QuestParameterValue<Guid>? parameterValue);

        Assert.That(success, Is.True);
        Assert.That(parameterValue, Is.Not.Null);
        Assert.That(parameterValue!.ToString(), Is.EqualTo(value));
    }
}
#nullable restore