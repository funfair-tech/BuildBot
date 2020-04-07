﻿using System;
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

            ProjectResource? project = await client.Repository.Projects.Get(projectId);
            ReleaseResource? release = await client.Repository.Releases.Get(releaseId);
            EnvironmentResource? environment = await client.Repository.Environments.Get(environmentId);

            string projectName = project != null ? project.Name : projectId;
            string environmentName = NormaliseEnvironmentName(environment, environmentId, out bool releaseNoteWorthy);
            string releaseVersion = release != null ? release.Version : releaseId;

            EmbedBuilder builder = new EmbedBuilder();
            bool succeeded = HasSucceeded(message);

            if (succeeded)
            {
                builder.Color = Color.Green;
                builder.Title = $"{projectName} {releaseVersion} was deployed to {environmentName.ToLowerInvariant()}";

                string releaseNotes = release?.ReleaseNotes ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(releaseNotes))
                {
                    builder.Description = ReformatReleaseNotes(releaseNotes);
                }
            }
            else
            {
                builder.Color = Color.Red;
                builder.Title = $"{projectName} {releaseVersion} failed to deploy to {environmentName.ToLowerInvariant()}";
            }

            builder.WithUrl(message.Payload.ServerAuditUri);

            builder.AddField(name: "Product", projectName);
            builder.AddField(name: "Release", releaseVersion);
            builder.AddField(name: "Environment", environmentName);

            builder.AddField(name: "Should be in release channel", succeeded && releaseNoteWorthy);

            await this._bot.PublishAsync(builder);
        }

        private static string ReformatReleaseNotes(string releaseNotes)
        {
            StringBuilder builder = new StringBuilder();
            string[] text = releaseNotes.Split(Environment.NewLine);

            foreach (string line in text)
            {
                if (line.StartsWith(value: "### ", StringComparison.Ordinal))
                {
                    string replacement = "**__" + line.Substring(startIndex: 4)
                                                      .Trim() + "__**";
                    builder.AppendLine(replacement);

                    continue;
                }

                builder.AppendLine(Regex.Replace(line, pattern: "(ff\\-\\d+)", replacement: "_$1:_", RegexOptions.IgnoreCase)
                                        .Trim());
            }

            return builder.ToString();
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
    }
}