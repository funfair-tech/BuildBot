using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BuildBot.ServiceModel.Octopus;
using Discord;
using Microsoft.Extensions.Logging;
using Octopus.Client;
using Octopus.Client.Model;

namespace BuildBot.Discord.Publishers.Octopus;

public sealed class DeployPublisher : IPublisher<Deploy>
{
    private static readonly IReadOnlyList<string> ReleaseChannels = new[]
                                                                    {
                                                                        "Staging",
                                                                        "Showcase",
                                                                        "Live"
                                                                    };

    private readonly IDiscordBot _bot;
    private readonly ILogger<DeployPublisher> _logger;
    private readonly IOctopusClientFactory _octopusClientFactory;
    private readonly OctopusServerEndpoint _octopusServerEndpoint;

    public DeployPublisher(IDiscordBot bot, IOctopusClientFactory octopusClientFactory, OctopusServerEndpoint octopusServerEndpoint, ILogger<DeployPublisher> logger)
    {
        this._bot = bot ?? throw new ArgumentNullException(nameof(bot));
        this._octopusClientFactory = octopusClientFactory ?? throw new ArgumentNullException(nameof(octopusClientFactory));
        this._octopusServerEndpoint = octopusServerEndpoint ?? throw new ArgumentNullException(nameof(octopusServerEndpoint));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task PublishAsync(Deploy message)
    {
        DeployPayload? payload = message.Payload;

        if (payload == null)
        {
            return Task.CompletedTask;
        }

        return this.PublishPayloadAsync(payload);
    }

    private async Task PublishPayloadAsync(DeployPayload payload)
    {
        IOctopusAsyncClient client = await this._octopusClientFactory.CreateAsyncClient(this._octopusServerEndpoint);

        string serverUri = payload.ServerUri;
        string spaceId = payload.Event?.SpaceId ?? "Spaces-1";
        string? projectId = FindDocumentId(payload: payload, documentPrefix: "Projects-");
        string? releaseId = FindDocumentId(payload: payload, documentPrefix: "Releases-");
        string? environmentId = FindDocumentId(payload: payload, documentPrefix: "Environments-");
        string? deploymentId = FindDocumentId(payload: payload, documentPrefix: "Deployments-");
        string? tenantId = FindDocumentId(payload: payload, documentPrefix: "Tenants-");

        ProjectResource? project = await client.Repository.Projects.Get(projectId);
        ReleaseResource? release = await client.Repository.Releases.Get(releaseId);
        EnvironmentResource? environment = await client.Repository.Environments.Get(environmentId);

        TenantResource? tenant = null;

        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            tenant = await client.Repository.Tenants.Get(tenantId);
        }

        string? projectName = NormalizeProjectName(project: project, projectId: projectId);
        string? environmentName = NormaliseEnvironmentName(environment: environment, environmentId: environmentId, out bool releaseNoteWorthy, out string? tenantName);
        string? releaseVersion = release != null
            ? release.Version
            : releaseId;

        if (tenant != null)
        {
            tenantName = NormaliseTenantName(tenant);
        }

        if (projectName == null || releaseVersion == null || environmentName == null)
        {
            // no idea what we're releasing if it has none of the above.
            return;
        }

        bool succeeded = HasSucceeded(payload);

        EmbedBuilder builder = BuildMessage(projectName: projectName, releaseVersion: releaseVersion, environmentName: environmentName, tenantName: tenantName, release: release, succeeded: succeeded);

        AddDeploymentId(serverUri: serverUri, deploymentId: deploymentId, builder: builder, spaceId: spaceId);

        AddDeploymentDetails(builder: builder, projectName: projectName, releaseVersion: releaseVersion, environmentName: environmentName, tenantName: tenantName);

        await this._bot.PublishAsync(builder);

        this._logger.LogDebug($"{projectName}: {releaseVersion} Build successful: {succeeded} Noteworthy: {releaseNoteWorthy}");

        if (succeeded && releaseNoteWorthy)
        {
            await this._bot.PublishToReleaseChannelAsync(builder);
        }
    }

    private static void AddDeploymentId(string serverUri, string spaceId, string? deploymentId, EmbedBuilder builder)
    {
        if (string.IsNullOrWhiteSpace(deploymentId))
        {
            return;
        }

        string url = $"{serverUri}/app#/{spaceId}/deployments/{deploymentId}";

        builder.WithUrl(url);
    }

    private static EmbedBuilder BuildMessage(string projectName, string releaseVersion, string environmentName, string? tenantName, ReleaseResource? release, bool succeeded)
    {
        EmbedBuilder builder = new();

        if (succeeded)
        {
            BuildSuccessfulDeployment(builder: builder, projectName: projectName, releaseVersion: releaseVersion, environmentName: environmentName, tenantName: tenantName, release: release);
        }
        else
        {
            BuildFailedDeployment(builder: builder, projectName: projectName, releaseVersion: releaseVersion, environmentName: environmentName, tenantName: tenantName);
        }

        return builder;
    }

    private static void AddDeploymentDetails(EmbedBuilder builder, string projectName, string releaseVersion, string environmentName, string? tenantName)
    {
        builder.AddField(name: "Product", value: projectName);
        builder.AddField(name: "Release", value: releaseVersion);
        builder.AddField(name: "Environment", value: environmentName);

        if (!string.IsNullOrWhiteSpace(tenantName))
        {
            builder.AddField(name: "Tenant", value: tenantName);
        }
    }

    private static void BuildFailedDeployment(EmbedBuilder builder, string projectName, string releaseVersion, string environmentName, string? tenantName)
    {
        builder.Color = Color.Red;
        builder.Title = $"{projectName} {releaseVersion} failed to deploy to {environmentName.ToLowerInvariant()}";

        if (!string.IsNullOrWhiteSpace(tenantName))
        {
            builder.Title += string.Concat(str0: " (", str1: tenantName, str2: ")");
        }
    }

    private static void BuildSuccessfulDeployment(EmbedBuilder builder, string projectName, string releaseVersion, string environmentName, string? tenantName, ReleaseResource? release)
    {
        builder.Color = Color.Green;
        builder.Title = $"{projectName} {releaseVersion} was deployed to {environmentName.ToLowerInvariant()}";

        if (!string.IsNullOrWhiteSpace(tenantName))
        {
            builder.Title += string.Concat(str0: " (", str1: tenantName, str2: ")");
        }

        string? releaseNotes = release?.ReleaseNotes;

        if (string.IsNullOrWhiteSpace(releaseNotes))
        {
            return;
        }

        string reformatted = ReformatReleaseNotes(releaseNotes);

        if (reformatted.Length > 2048)
        {
            reformatted = reformatted.Substring(startIndex: 0, length: 2048)
                                     .Trim();
            builder.AddField(name: "WARNING", value: "Release notes truncated as too long");
        }

        if (!string.IsNullOrWhiteSpace(reformatted))
        {
            builder.Description = reformatted;
        }
    }

    private static string? FindDocumentId(DeployPayload payload, string documentPrefix)
    {
        IReadOnlyList<string>? relatedDocumentIds = payload.Event?.RelatedDocumentIds;

        return relatedDocumentIds?.FirstOrDefault(x => x.StartsWith(value: documentPrefix, comparisonType: StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeProjectName(ProjectResource? project, string? projectId)
    {
        if (project == null)
        {
            return projectId;
        }

        if (!string.IsNullOrWhiteSpace(project.Description))
        {
            return project.Description;
        }

        return project.Name;
    }

    private static string NormaliseTenantName(TenantResource tenant)
    {
        if (!string.IsNullOrWhiteSpace(tenant.Description))
        {
            return tenant.Description;
        }

        if (StringComparer.InvariantCultureIgnoreCase.Equals(x: tenant.Name, y: "CasinoFair"))
        {
            return "TTM";
        }

        return tenant.Name;
    }

    private static string ReformatReleaseNotes(string releaseNotes)
    {
        static string MakeUpperCase(Match match)
        {
            return Bold(match.ToString()
                             .ToUpperInvariant());
        }

        StringBuilder builder = new();
        string[] text = releaseNotes.Split(separator: '\n');

        for (int lineIndex = 0; lineIndex < text.Length; ++lineIndex)
        {
            string line = text[lineIndex];

            if (line.StartsWith(value: "### ", comparisonType: StringComparison.Ordinal))
            {
                if (IsLastLine(text: text, lineIndex: lineIndex))
                {
                    continue;
                }

                builder.AppendLine();
                string replacement = Bold(line.Substring(startIndex: 4)
                                              .Trim())
                    .ToUpperInvariant();

                builder.AppendLine(replacement);

                continue;
            }

            string detail = Regex.Replace(input: line, pattern: "(ff\\-\\d+)", evaluator: MakeUpperCase, options: RegexOptions.IgnoreCase)
                                 .Trim();

            if (string.IsNullOrWhiteSpace(detail))
            {
                continue;
            }

            builder.AppendLine(EnsurePrefixed(detail));
        }

        return builder.ToString()
                      .Trim();
    }

    private static bool IsLastLine(in string[] text, int lineIndex)
    {
        for (int subsequentLine = lineIndex + 1; subsequentLine < text.Length; ++subsequentLine)
        {
            if (!string.IsNullOrWhiteSpace(text[subsequentLine]))
            {
                return false;
            }
        }

        return true;
    }

    private static string EnsurePrefixed(string trim)
    {
        const string prefix = "- ";

        if (trim.StartsWith(value: prefix, comparisonType: StringComparison.Ordinal))
        {
            return trim;
        }

        return prefix + trim;
    }

    private static bool HasSucceeded(DeployPayload payload)
    {
        return StringComparer.InvariantCultureIgnoreCase.Equals(x: payload.Event?.Category, y: "DeploymentSucceeded");
    }

    private static string? NormaliseEnvironmentName(EnvironmentResource? environment, string? environmentId, out bool isReleaseNoteWorthy, out string? tenantName)
    {
        tenantName = null;
        string? name = environment != null
            ? environment.Name
            : environmentId;

        isReleaseNoteWorthy = ReleaseChannels.Any(predicate: x => StringComparer.InvariantCultureIgnoreCase.Equals(x: name, y: x));

        if (StringComparer.InvariantCultureIgnoreCase.Equals(x: name, y: "Build"))
        {
            return "Build";
        }

        if (StringComparer.InvariantCultureIgnoreCase.Equals(x: name, y: "Showcase"))
        {
            tenantName = "Showcase";

            return "Live";
        }

        return name;
    }

    private static string Wrap(string value, string wrapWith)
    {
        return string.Concat(str0: wrapWith, str1: value, str2: wrapWith);
    }

    private static string Bold(string value)
    {
        return Wrap(value: value, wrapWith: @"**");
    }

    [SuppressMessage(category: "ReSharper", checkId: "UnusedMember.Local", Justification = "For expanson")]
    private static string Underline(string value)
    {
        return Wrap(value: value, wrapWith: @"__");
    }
}