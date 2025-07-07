using System;
using System.Buffers;
using BuildBot.ServiceModel.GitHub;

namespace BuildBot.GitHub.Helpers;

public static class PackageUpdateDetector
{
    private static readonly SearchValues<string> PackageUpdatePrefixes = SearchValues.Create([
                                                                                                 "FF-1429",
                                                                                                 "[FF-1429]",
                                                                                                 "Dependencies",
                                                                                                 "[Dependencies]"
                                                                                             ],
                                                                                             comparisonType: StringComparison.Ordinal);

    public static bool IsPackageUpdate(Commit c)
    {
        return IsPackageUpdate(c.Message);
    }

    public static bool IsPackageUpdate(in ReadOnlySpan<char> message)
    {
        return message.IndexOfAny(PackageUpdatePrefixes) == 0;
    }
}