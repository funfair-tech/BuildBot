using System;
using System.IO;
using System.Threading.Tasks;
using BuildBot.Helpers;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.Tests;

public sealed class ApplicationConfigLocatorTests : TestBase
{
    [Fact]
    public async Task UsesTheBaseDirectoryWhenItHoldsTheAppSettingsAsync()
    {
        using (TestConfigFolder configFolder = new())
        {
            await configFolder.WriteAppSettingsAsync(
                discordWebhookUrl: "",
                cancellationToken: this.CancellationToken()
            );

            string path = ApplicationConfigLocator.LookupConfigurationFilesPath(
                configFolder.FullName + Path.DirectorySeparatorChar
            );

            Assert.Equal(expected: configFolder.FullName, actual: path);
        }
    }

    [Fact]
    public void FallsBackToTheCurrentDirectoryWhenTheBaseDirectoryHasNoAppSettings()
    {
        using (TestConfigFolder configFolder = new())
        {
            string path = ApplicationConfigLocator.LookupConfigurationFilesPath(
                configFolder.FullName + Path.DirectorySeparatorChar
            );

            Assert.Equal(expected: Environment.CurrentDirectory, actual: path);
        }
    }

    [Fact]
    public void FallsBackToTheCurrentDirectoryWhenTheBaseDirectoryHasNoParent()
    {
        string path = ApplicationConfigLocator.LookupConfigurationFilesPath(Path.DirectorySeparatorChar.ToString());

        Assert.Equal(expected: Environment.CurrentDirectory, actual: path);
    }
}
