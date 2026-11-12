using System;
using System.IO;
using System.Threading.Tasks;
using BuildBot.Helpers;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.Tests;

public sealed class SelfTestTests : TestBase
{
    private const string APP_SETTINGS = """
        {
          "Discord": { "WebhookUrl": "" },
          "CloudFormation": { "TopicArn": "", "Aws": { "AccessKeyId": "", "SecretKey": "" } },
          "GitHub": { "WebhookSecret": "" }
        }
        """;

    [Theory]
    [InlineData("--self-test", true)]
    [InlineData("--urls http://localhost --self-test", true)]
    [InlineData("", false)]
    [InlineData("--health-check http://127.0.0.1:8080/ping", false)]
    [InlineData("--SELF-TEST", false)]
    public static void DetectsSelfTestSwitch(string commandLine, bool expected)
    {
        string[] args = commandLine.Split(separator: ' ', options: StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(expected: expected, actual: SelfTest.IsSelfTest(args));
    }

    [Fact]
    public async Task SucceedsWhenStartupCompletesAsync()
    {
        DirectoryInfo configFolder = Directory.CreateTempSubdirectory();

        try
        {
            await File.WriteAllTextAsync(
                path: Path.Combine(path1: configFolder.FullName, path2: "appsettings.json"),
                contents: APP_SETTINGS,
                cancellationToken: this.CancellationToken()
            );

            (int exitCode, string output) = await this.RunSelfTestAsync(configFolder: configFolder);

            Assert.Equal(expected: 0, actual: exitCode);
            Assert.Equal(expected: "Self-test passed", actual: output);
        }
        finally
        {
            configFolder.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task FailsWithOneLineReasonWhenConfigurationIsMissingAsync()
    {
        DirectoryInfo configFolder = Directory.CreateTempSubdirectory();

        try
        {
            (int exitCode, string output) = await this.RunSelfTestAsync(configFolder: configFolder);

            Assert.Equal(expected: 1, actual: exitCode);
            Assert.StartsWith(
                expectedStartString: "Self-test failed: ",
                actualString: output,
                comparisonType: StringComparison.Ordinal
            );
            Assert.DoesNotContain(
                expectedSubstring: "\n",
                actualString: output,
                comparisonType: StringComparison.Ordinal
            );
        }
        finally
        {
            configFolder.Delete(recursive: true);
        }
    }

    private async ValueTask<(int ExitCode, string Output)> RunSelfTestAsync(DirectoryInfo configFolder)
    {
        await using (StringWriter output = new())
        {
            int exitCode = await SelfTest.RunAsync(
                args: ["--self-test"],
                configPath: configFolder.FullName,
                output: output,
                cancellationToken: this.CancellationToken()
            );

            return (exitCode, output.ToString().TrimEnd());
        }
    }
}
