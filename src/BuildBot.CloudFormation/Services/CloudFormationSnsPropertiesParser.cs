using System;
using System.Collections.Generic;
using System.Linq;
using BuildBot.ServiceModel.CloudFormation;

namespace BuildBot.CloudFormation.Services;

public sealed class CloudFormationSnsPropertiesParser : ICloudFormationSnsPropertiesParser
{
    public Dictionary<string, string> SplitMessageToDictionary(SnsMessage message)
    {
        return message.Message.Split("\n")
                      .Where(line => !string.IsNullOrWhiteSpace(line))
                      .Select(SplitLineToKeyAndValue)
                      .ToDictionary(keySelector: key => key.key, elementSelector: value => value.value, comparer: StringComparer.Ordinal);
    }

    private static (string key, string value) SplitLineToKeyAndValue(string m)
    {
        const string resourceValueStart = "='";
        const string resourceValueEnd = "'";

        int i = m.IndexOf(value: resourceValueStart, comparisonType: StringComparison.Ordinal);

        if (i < resourceValueStart.Length)
        {
            return (key: m, string.Empty);
        }

        string key = m[..i];
        int start = i + resourceValueStart.Length;
        int length = m.Length - start - resourceValueEnd.Length;

        string value = m.Substring(startIndex: start, length: length);

        return (key, value);
    }
}