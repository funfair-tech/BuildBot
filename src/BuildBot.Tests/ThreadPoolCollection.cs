namespace BuildBot.Tests;

internal static class ThreadPoolCollection
{
    // Server startup changes the process-wide thread pool minimums, so tests that start it or assert on them must not run in parallel.
    public const string NAME = "Thread pool";
}
