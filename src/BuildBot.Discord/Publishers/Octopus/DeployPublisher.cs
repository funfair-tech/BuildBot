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
    public class DeployPublisher : IPublisher<Deploy>
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

            string projectId = message.Payload.Event.RelatedDocumentIds.FirstOrDefault(predicate: x => x.StartsWith(value: "Projects-", StringComparison.OrdinalIgnoreCase));
            string releaseId = message.Payload.Event.RelatedDocumentIds.FirstOrDefault(predicate: x => x.StartsWith(value: "Releases-", StringComparison.OrdinalIgnoreCase));
            string environmentId =
                message.Payload.Event.RelatedDocumentIds.FirstOrDefault(predicate: x => x.StartsWith(value: "Environments-", StringComparison.OrdinalIgnoreCase));
            string deploymentId = message.Payload.Event.RelatedDocumentIds.FirstOrDefault(predicate: x => x.StartsWith(value: "Deployments-", StringComparison.OrdinalIgnoreCase));
            string? tenantId = message.Payload.Event.RelatedDocumentIds.FirstOrDefault(predicate: x => x.StartsWith(value: "Tenants-", StringComparison.OrdinalIgnoreCase));

            ProjectResource? project = await client.Repository.Projects.Get(projectId);
            ReleaseResource? release = await client.Repository.Releases.Get(releaseId);
            EnvironmentResource? environment = await client.Repository.Environments.Get(environmentId);

            TenantResource? tenant = null;

            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                tenant = await client.Repository.Tenants.Get(tenantId);
            }

            string projectName = project != null ? project.Name : projectId;
            string environmentName = NormaliseEnvironmentName(environment, environmentId, out bool releaseNoteWorthy);
            string releaseVersion = release != null ? release.Version : releaseId;

            EmbedBuilder builder = new EmbedBuilder();
            bool succeeded = HasSucceeded(message);

            if (succeeded)
            {
                builder.Color = Color.Green;
                builder.Title = $"{projectName} {releaseVersion} was deployed to {environmentName.ToLowerInvariant()}";

                if (tenant != null)
                {
                    builder.Title += string.Concat(str0: " (", tenant.Name, str2: ")");
                }

                string releaseNotes = release?.ReleaseNotes ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(releaseNotes))
                {
                    string reformatted = ReformatReleaseNotes(releaseNotes);

                    if (reformatted.Length > 2048)
                    {
                        reformatted = reformatted.Substring(startIndex: 0, length: 2048)
                                                 .Trim();
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

                if (tenant != null)
                {
                    builder.Title += string.Concat(str0: " (", tenant.Name, str2: ")");
                }
            }

            if (message.Payload.ServerUri != null && !string.IsNullOrWhiteSpace(deploymentId))
            {
                string url = $"{message.Payload.ServerUri}/app#/{message.Payload.Event.SpaceId}/deployments/{deploymentId}";

                builder.WithUrl(url);
            }

            builder.AddField(name: "Product", projectName);
            builder.AddField(name: "Release", releaseVersion);
            builder.AddField(name: "Environment", environmentName);

            if (tenant != null)
            {
                builder.AddField(name: "Tenant", tenant.Name);
            }

            await this._bot.PublishAsync(builder);

            if (succeeded && releaseNoteWorthy)
            {
                await this._bot.PublishToReleaseChannelAsync(builder);
            }
        }

        private static string ReformatReleaseNotes(string releaseNotes)
        {
            static string MakeUpperCase(Match match)
            {
                return Bold(match.ToString());
            }

            StringBuilder builder = new StringBuilder();
            string[] text = releaseNotes.Split(separator: '\n');

            for (int lineIndex = 0; lineIndex < text.Length; ++lineIndex)
            {
                string line = text[lineIndex];

                if (line.StartsWith(value: "### ", StringComparison.Ordinal))
                {
                    if (IsLastLine(text, lineIndex))
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

                string detail = Regex.Replace(line, pattern: "(ff\\-\\d+)", MakeUpperCase, RegexOptions.IgnoreCase)
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

            if (trim.StartsWith(prefix, StringComparison.Ordinal))
            {
                return trim;
            }

            return prefix + trim;
        }

        private static bool HasSucceeded(Deploy message)
        {
            return StringComparer.InvariantCultureIgnoreCase.Equals(message.Payload.Event.Category, y: "DeploymentSucceeded");
        }

        private static string NormaliseEnvironmentName(EnvironmentResource environment, string environmentId, out bool isReleaseNoteWorthy)
        {
            string name = environment != null ? environment.Name : environmentId;

            string[] releaseChannels = {"Beta", "Showcase", "Live"};

            isReleaseNoteWorthy = releaseChannels.Any(predicate: x => StringComparer.InvariantCultureIgnoreCase.Equals(name, x));

            if (StringComparer.InvariantCultureIgnoreCase.Equals(name, y: "Beta"))
            {
                return "Staging";
            }

            return name;
        }

        private static string Wrap(string value, string wrapWith)
        {
            return string.Concat(wrapWith, value, wrapWith);
        }

        private static string Bold(string value)
        {
            return Wrap(value, wrapWith: @"**");
        }

        private static string Underline(string value)
        {
            return Wrap(value, wrapWith: @"__");
        }
    }
}