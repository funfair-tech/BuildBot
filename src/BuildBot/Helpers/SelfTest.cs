using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BuildBot.Helpers;

public static class SelfTest
{
    private const string SWITCH = "--self-test";

    // Secrets are only supplied when the container is deployed, so the image build has none; nothing is ever sent to it.
    private static readonly IReadOnlyDictionary<string, string?> DeploymentSecretPlaceholders = new Dictionary<
        string,
        string?
    >(StringComparer.Ordinal)
    {
        ["Discord:WebhookUrl"] = "https://discord.invalid/api/webhooks/self-test",
    }.ToFrozenDictionary(StringComparer.Ordinal);

    public static bool IsSelfTest(IReadOnlyList<string> args)
    {
        return args.Contains(value: SWITCH, comparer: StringComparer.Ordinal);
    }

    public static async ValueTask<int> RunAsync(
        IReadOnlyList<string> args,
        string configPath,
        TextWriter output,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await VerifyStartupAsync(args: WithoutSwitch(args), configPath: configPath);

            await WriteLineAsync(output: output, message: "Self-test passed", cancellationToken: cancellationToken);

            return 0;
        }
        catch (Exception exception)
        {
            await WriteLineAsync(
                output: output,
                message: "Self-test failed: " + exception.Message.ReplaceLineEndings(" "),
                cancellationToken: cancellationToken
            );

            return 1;
        }
    }

    private static string[] WithoutSwitch(IReadOnlyList<string> args)
    {
        return [.. args.Where(static arg => !StringComparer.Ordinal.Equals(x: arg, y: SWITCH))];
    }

    private static async ValueTask VerifyStartupAsync(string[] args, string configPath)
    {
        await using (
            WebApplication app = ServerStartup.CreateApp(
                args: args,
                configPath: configPath,
                configurationOverrides: DeploymentSecretPlaceholders,
                validateServices: true
            )
        )
        {
            WebApplication configured = app.ConfigureEndpoints();

            // Resolving the options runs the listener configuration without binding any port.
            _ = configured.Services.GetRequiredService<IOptions<KestrelServerOptions>>().Value;

            LoadCryptography();
        }
    }

    private static void LoadCryptography()
    {
        // Forces the native crypto library (OpenSSL on Linux) to load, as webhook signature checks depend on it.
        _ = HMACSHA256.HashData(key: [1], source: [2]);
    }

    private static ValueTask WriteLineAsync(TextWriter output, string message, CancellationToken cancellationToken)
    {
        return new(output.WriteLineAsync(buffer: message.AsMemory(), cancellationToken: cancellationToken));
    }
}
