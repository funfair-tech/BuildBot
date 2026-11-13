using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BuildBot.Tests;

internal sealed class TestConfigFolder : IDisposable
{
    public const string DISCORD_WEBHOOK_URL = "https://discord.invalid/api/webhooks/test";

    private readonly DirectoryInfo _folder;

    public TestConfigFolder()
    {
        this._folder = Directory.CreateTempSubdirectory();
    }

    public string FullName => this._folder.FullName;

    public void Dispose()
    {
        this._folder.Delete(recursive: true);
    }

    public Task WriteAppSettingsAsync(string discordWebhookUrl, CancellationToken cancellationToken)
    {
        return File.WriteAllTextAsync(
            path: Path.Combine(path1: this.FullName, path2: "appsettings.json"),
            contents: $$"""
            {
              "Discord": { "WebhookUrl": "{{discordWebhookUrl}}" },
              "CloudFormation": { "TopicArn": "", "Aws": { "AccessKeyId": "", "SecretKey": "" } },
              "GitHub": { "WebhookSecret": "" }
            }
            """,
            cancellationToken: cancellationToken
        );
    }
}
