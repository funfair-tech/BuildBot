using System;
using System.Buffers;

namespace BuildBot.GitHub.Helpers;

public static class MainBranchDetector
{
    private static readonly SearchValues<string> MainBranches = SearchValues.Create(["main", "master"], comparisonType: StringComparison.Ordinal);

    public static bool IsRepoMainBranch(string branch)
    {
        return MainBranches.Contains(value: branch);
    }
}