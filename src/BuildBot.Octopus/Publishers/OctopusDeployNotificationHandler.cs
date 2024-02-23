using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Models;
using BuildBot.Octopus.Models;
using BuildBot.ServiceModel.Octopus;
using Discord;
using Mediator;
using Microsoft.Extensions.Logging;
using Octopus.Client;
using Octopus.Client.Model;
using RegexExpressions = BuildBot.Octopus.Helpers.RegexExpressions;

namespace BuildBot.Octopus.Publishers;

public sealed class OctopusDeployNotificationHandler : INotificationHandler<OctopusDeploy>
{
    private static readonly IReadOnlyList<string> ReleaseChannels =
    [
        "Staging",
        "Showcase",
        "Live"
    ];

    private readonly ILogger<OctopusDeployNotificationHandler> _logger;
    private readonly IMediator _mediator;

    private readonly IOctopusClientFactory _octopusClientFactory;
    private readonly OctopusServerEndpoint _octopusServerEndpoint;

    public OctopusDeployNotificationHandler(IMediator mediator,
                                            IOctopusClientFactory octopusClientFactory,
                                            OctopusServerEndpoint octopusServerEndpoint,
                                            ILogger<OctopusDeployNotificationHandler> logger)
    {
        this._mediator = mediator;
        this._octopusClientFactory = octopusClientFactory;
        this._octopusServerEndpoint = octopusServerEndpoint;
        this._logger = logger;
    }

    public ValueTask Handle(OctopusDeploy notification, CancellationToken cancellationToken)
    {
        this._logger.LogDebug($"Octopus: [{notification.Model.EventType}]");

        return this.PublishAsync(message: notification.Model, cancellationToken: cancellationToken);
    }

    private ValueTask PublishAsync(Deploy message, CancellationToken cancellationToken)
    {
        // TODO... should this queue the message and then process it in a background task?
        DeployPayload? payload = message.Payload;

        return payload is null
            ? ValueTask.CompletedTask
            : this.PublishPayloadAsync(payload: payload.Value, cancellationToken: cancellationToken);
    }

    private async ValueTask PublishPayloadAsync(DeployPayload payload, CancellationToken cancellationToken)
    {
        IOctopusAsyncClient client = await this._octopusClientFactory.CreateAsyncClient(this._octopusServerEndpoint);

        string serverUri = payload.ServerUri;
        string spaceId = payload.Event?.SpaceId ?? "Spaces-1";
        string? projectId = FindDocumentId(payload: payload, documentPrefix: "Projects-");
        string? releaseId = FindDocumentId(payload: payload, documentPrefix: "Releases-");
        string? environmentId = FindDocumentId(payload: payload, documentPrefix: "Environments-");
        string? deploymentId = FindDocumentId(payload: payload, documentPrefix: "Deployments-");
        string? tenantId = FindDocumentId(payload: payload, documentPrefix: "Tenants-");

        IOctopusAsyncRepository repo = client.Repository;

        ProjectResource? project = await this.GetProjectAsync(repo: repo, projectId: projectId, cancellationToken: cancellationToken);
        ReleaseResource? release = await this.GetReleaseAsync(repo: repo, releaseId: releaseId, cancellationToken: cancellationToken);
        EnvironmentResource? environment = await this.GetEnvironmentAsync(repo: repo, environmentId: environmentId, cancellationToken: cancellationToken);
        TenantResource? tenant = await this.GetTenantAsync(client: client, tenantId: tenantId, cancellationToken: cancellationToken);

        string? projectName = NormalizeProjectName(project: project, projectId: projectId);
        string? environmentName = NormaliseEnvironmentName(environment: environment, environmentId: environmentId, out bool releaseNoteWorthy, out string? tenantName);
        string? releaseVersion = GetReleaseVersion(release: release, releaseId: releaseId);

        if (tenant is not null)
        {
            tenantName = NormaliseTenantName(tenant);
        }

        if (projectName is null || releaseVersion is null || environmentName is null)
        {
            // no idea what we're releasing if it has none of the above.
            return;
        }

        bool succeeded = HasSucceeded(payload);

        EmbedBuilder builder = BuildMessage(projectName: projectName, releaseVersion: releaseVersion, environmentName: environmentName, tenantName: tenantName, release: release, succeeded: succeeded);

        AddDeploymentId(serverUri: serverUri, deploymentId: deploymentId, builder: builder, spaceId: spaceId);

        AddDeploymentDetails(builder: builder, projectName: projectName, releaseVersion: releaseVersion, environmentName: environmentName, tenantName: tenantName);

        await this._mediator.Publish(new BotMessage(builder), cancellationToken: cancellationToken);

        this._logger.LogDebug($"{projectName}: {releaseVersion} Build successful: {succeeded} Noteworthy: {releaseNoteWorthy}");

        await this.PublishToReleaseChannelAsync(succeeded: succeeded, releaseNoteWorthy: releaseNoteWorthy, builder: builder, cancellationToken: cancellationToken);
    }

    private async ValueTask<TenantResource?> GetTenantAsync(IOctopusAsyncClient client, string? tenantId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return null;
        }

        try
        {
            return await client.Repository.Tenants.Get(idOrHref: tenantId, cancellationToken: cancellationToken);
        }
        catch (Exception exception)
        {
            this._logger.LogError(new(exception.HResult), exception: exception, $"Failed to get tenant {tenantId}: {exception.Message}");

            return null;
        }
    }

    private async ValueTask<EnvironmentResource?> GetEnvironmentAsync(IOctopusAsyncRepository repo, string? environmentId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(environmentId))
        {
            return null;
        }

        try
        {
            return await repo.Environments.Get(idOrHref: environmentId, cancellationToken: cancellationToken);
        }
        catch (Exception exception)
        {
            this._logger.LogError(new(exception.HResult), exception: exception, $"Failed to get environment {environmentId}: {exception.Message}");

            return null;
        }
    }

    private async ValueTask<ReleaseResource?> GetReleaseAsync(IOctopusAsyncRepository repo, string? releaseId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(releaseId))
        {
            return null;
        }

        try
        {
            return await repo.Releases.Get(idOrHref: releaseId, cancellationToken: cancellationToken);
        }
        catch (Exception exception)
        {
            this._logger.LogError(new(exception.HResult), exception: exception, $"Failed to get release {releaseId}: {exception.Message}");

            return null;
        }
    }

    private async ValueTask<ProjectResource?> GetProjectAsync(IOctopusAsyncRepository repo, string? projectId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(projectId))
        {
            return null;
        }

        try
        {
            return await repo.Projects.Get(idOrHref: projectId, cancellationToken: cancellationToken);
        }
        catch (Exception exception)
        {
            this._logger.LogError(new(exception.HResult), exception: exception, $"Failed to get project {projectId}: {exception.Message}");

            return null;
        }
    }

    private async Task PublishToReleaseChannelAsync(bool succeeded, bool releaseNoteWorthy, EmbedBuilder builder, CancellationToken cancellationToken)
    {
        if (succeeded && releaseNoteWorthy)
        {
            await this._mediator.Publish(new BotReleaseMessage(builder), cancellationToken: cancellationToken);
        }
    }

    private static string? GetReleaseVersion(ReleaseResource? release, string? releaseId)
    {
        return release is not null
            ? release.Version
            : releaseId;
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
        builder.AddField(name: "Product", value: projectName)
               .AddField(name: "Release", value: releaseVersion)
               .AddField(name: "Environment", value: environmentName);

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

    private static string? FindDocumentId(in DeployPayload payload, string documentPrefix)
    {
        IReadOnlyList<string>? relatedDocumentIds = payload.Event?.RelatedDocumentIds;

        return relatedDocumentIds?.FirstOrDefault(x => x.StartsWith(value: documentPrefix, comparisonType: StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeProjectName(ProjectResource? project, string? projectId)
    {
        if (project is null)
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

                builder = builder.AppendLine()
                                 .AppendLine(MakeCodeBold(line));

                continue;
            }

            string detail = BuildDetail(line);

            if (string.IsNullOrWhiteSpace(detail))
            {
                continue;
            }

            builder = builder.AppendLine(EnsurePrefixed(detail));
        }

        return builder.ToString()
                      .Trim();
    }

    private static string BuildDetail(string line)
    {
        return RegexExpressions.BuildNumber()
                               .Replace(input: line, evaluator: MakeUpperCase)
                               .Trim();

        static string MakeUpperCase(Match match)
        {
            return Bold(match.ToString()
                             .ToUpperInvariant());
        }
    }

    private static string MakeCodeBold(string line)
    {
        return Bold(line.Substring(startIndex: 4)
                        .Trim())
            .ToUpperInvariant();
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

    private static bool HasSucceeded(in DeployPayload payload)
    {
        return StringComparer.InvariantCultureIgnoreCase.Equals(x: payload.Event?.Category, y: "DeploymentSucceeded");
    }

    private static string? NormaliseEnvironmentName(EnvironmentResource? environment, string? environmentId, out bool isReleaseNoteWorthy, out string? tenantName)
    {
        tenantName = null;
        string? name = environment is not null
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
        return Wrap(value: value, wrapWith: "**");
    }

    [SuppressMessage(category: "ReSharper", checkId: "UnusedMember.Local", Justification = "For expanson")]
    private static string Underline(string value)
    {
        return Wrap(value: value, wrapWith: "__");
    }
}