using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BuildBot.ServiceModel.Octopus;
using Discord;
using Octopus.Client;
using Octopus.Client.Model;

namespace BuildBot.Discord.Publishers.Octopus
{
    public sealed class DeployPublisher : IPublisher<Deploy>
    {
        private readonly IDiscordBot _bot;
        private readonly IOctopusClientFactory _octopusClientFactory;
        private readonly OctopusServerEndpoint _octopusServerEndpoint;

        public DeployPublisher(IDiscordBot bot, IOctopusClientFactory octopusClientFactory, OctopusServerEndpoint octopusServerEndpoint)
        {
            this._bot = bot;
            this._octopusClientFactory = octopusClientFactory;
            this._octopusServerEndpoint = octopusServerEndpoint;
        }

        public async Task PublishAsync(Deploy message)
        {
            IOctopusAsyncClient client = await this._octopusClientFactory.CreateAsyncClient(this._octopusServerEndpoint);

            string projectId =
                message.Payload.Event.RelatedDocumentIds.FirstOrDefault(predicate: x => x.StartsWith(value: "Projects-", comparisonType: StringComparison.OrdinalIgnoreCase));
            string releaseId =
                message.Payload.Event.RelatedDocumentIds.FirstOrDefault(predicate: x => x.StartsWith(value: "Releases-", comparisonType: StringComparison.OrdinalIgnoreCase));
            string environmentId =
                message.Payload.Event.RelatedDocumentIds.FirstOrDefault(predicate: x => x.StartsWith(value: "Environments-", comparisonType: StringComparison.OrdinalIgnoreCase));
            string deploymentId =
                message.Payload.Event.RelatedDocumentIds.FirstOrDefault(predicate: x => x.StartsWith(value: "Deployments-", comparisonType: StringComparison.OrdinalIgnoreCase));
            string? tenantId =
                message.Payload.Event.RelatedDocumentIds.FirstOrDefault(predicate: x => x.StartsWith(value: "Tenants-", comparisonType: StringComparison.OrdinalIgnoreCase));

            ProjectResource? project = await client.Repository.Projects.Get(projectId);
            ReleaseResource? release = await client.Repository.Releases.Get(releaseId);
            EnvironmentResource? environment = await client.Repository.Environments.Get(environmentId);

            TenantResource? tenant = null;

            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                tenant = await client.Repository.Tenants.Get(tenantId);
            }

            string projectName = NormalizeProjectName(project: project, projectId: projectId);
            string environmentName = NormaliseEnvironmentName(environment: environment, environmentId: environmentId, out bool releaseNoteWorthy, out string? tenantName);
            string releaseVersion = release != null ? release.Version : releaseId;

            if (tenant != null)
            {
                tenantName = NormaliseTenantName(tenant);
            }

            EmbedBuilder builder = new EmbedBuilder();
            bool succeeded = HasSucceeded(message);

            if (succeeded)
            {
                builder.Color = Color.Green;
                builder.Title = $"{projectName} {releaseVersion} was deployed to {environmentName.ToLowerInvariant()}";

                if (!string.IsNullOrWhiteSpace(tenantName))
                {
                    builder.Title += string.Concat(str0: " (", str1: tenantName, str2: ")");
                }

                string releaseNotes = release?.ReleaseNotes ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(releaseNotes))
                {
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
            }
            else
            {
                builder.Color = Color.Red;
                builder.Title = $"{projectName} {releaseVersion} failed to deploy to {environmentName.ToLowerInvariant()}";

                if (!string.IsNullOrWhiteSpace(tenantName))
                {
                    builder.Title += string.Concat(str0: " (", str1: tenantName, str2: ")");
                }
            }

            if (message.Payload.ServerUri != null && !string.IsNullOrWhiteSpace(deploymentId))
            {
                string url = $"{message.Payload.ServerUri}/app#/{message.Payload.Event.SpaceId}/deployments/{deploymentId}";

                builder.WithUrl(url);
            }

            builder.AddField(name: "Product", value: projectName);
            builder.AddField(name: "Release", value: releaseVersion);
            builder.AddField(name: "Environment", value: environmentName);

            if (!string.IsNullOrWhiteSpace(tenantName))
            {
                builder.AddField(name: "Tenant", value: tenantName);
            }

            await this._bot.PublishAsync(builder);

            if (succeeded && releaseNoteWorthy)
            {
                await this._bot.PublishToReleaseChannelAsync(builder);
            }
        }

        private static string NormalizeProjectName(ProjectResource project, string projectId)
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

            StringBuilder builder = new StringBuilder();
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

        private static string? EnsurePrefixed(string trim)
        {
            const string prefix = "- ";

            if (trim.StartsWith(value: prefix, comparisonType: StringComparison.Ordinal))
            {
                return trim;
            }

            return prefix + trim;
        }

        private static bool HasSucceeded(Deploy message)
        {
            return StringComparer.InvariantCultureIgnoreCase.Equals(x: message.Payload.Event.Category, y: "DeploymentSucceeded");
        }

        private static string NormaliseEnvironmentName(EnvironmentResource environment, string environmentId, out bool isReleaseNoteWorthy, out string? tenantName)
        {
            tenantName = null;
            string name = environment != null ? environment.Name : environmentId;

            string[] releaseChannels = {"Beta", "Showcase", "Live"};

            isReleaseNoteWorthy = releaseChannels.Any(predicate: x => StringComparer.InvariantCultureIgnoreCase.Equals(x: name, y: x));

            if (StringComparer.InvariantCultureIgnoreCase.Equals(x: name, y: "Beta"))
            {
                return "Staging";
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

        private static string Underline(string value)
        {
            return Wrap(value: value, wrapWith: @"__");
        }
    }
}