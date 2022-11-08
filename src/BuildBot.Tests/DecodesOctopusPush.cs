using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using BuildBot.Json;
using BuildBot.ServiceModel.Octopus;
using FunFair.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace BuildBot.Tests;

public sealed class DecodesOctopusPush : LoggingTestBase
{
    private const string OCTOPUS_PUSH = @"{
  ""Timestamp"": ""2022-02-11T15:48:01.796+00:00"",
  ""EventType"": ""SubscriptionPayload"",
  ""Payload"": {
    ""ServerUri"": ""https://octopus.funfair.io"",
    ""ServerAuditUri"": ""https://octopus.funfair.io/app#/Spaces-1/configuration/audit?eventCategories=DeploymentFailed&eventCategories=DeploymentSucceeded&from=2022-02-11T15%3a47%3a30.%2b00%3a00&to=2022-02-11T16%3a48%3a01.%2b01%3a00"",
    ""BatchProcessingDate"": ""2022-02-11T16:48:01.723+01:00"",
    ""Subscription"": {
      ""Id"": ""Subscriptions-2"",
      ""Name"": ""BuildBot"",
      ""Type"": ""Event"",
      ""IsDisabled"": false,
      ""EventNotificationSubscription"": {
        ""Filter"": {
          ""Users"": [],
          ""Projects"": [],
          ""ProjectGroups"": [],
          ""Environments"": [],
          ""EventGroups"": [],
          ""EventCategories"": [
            ""DeploymentFailed"",
            ""DeploymentSucceeded""
          ],
          ""EventAgents"": [],
          ""Tenants"": [],
          ""Tags"": [],
          ""DocumentTypes"": []
        },
        ""EmailTeams"": [],
        ""EmailFrequencyPeriod"": ""01:00:00"",
        ""EmailPriority"": ""Normal"",
        ""EmailDigestLastProcessed"": null,
        ""EmailDigestLastProcessedEventAutoId"": null,
        ""EmailShowDatesInTimeZoneId"": ""GMT Standard Time"",
        ""WebhookURI"": ""https://buildbot.funfair.io/octopus/deploy"",
        ""WebhookTeams"": [],
        ""WebhookTimeout"": ""00:00:59"",
        ""WebhookHeaderKey"": null,
        ""WebhookHeaderValue"": null,
        ""WebhookLastProcessed"": ""2022-02-11T15:47:30.680+00:00"",
        ""WebhookLastProcessedEventAutoId"": 1006581
      },
      ""SpaceId"": ""Spaces-1"",
      ""Links"": {
        ""Self"": ""/api/Spaces-1/subscriptions/Subscriptions-2""
      }
    },
    ""Event"": {
      ""Id"": ""Events-693825"",
      ""RelatedDocumentIds"": [
        ""Deployments-84781"",
        ""Projects-825"",
        ""Releases-81092"",
        ""Environments-2"",
        ""ServerTasks-197738"",
        ""Channels-1070""
      ],
      ""Category"": ""DeploymentSucceeded"",
      ""UserId"": ""users-system"",
      ""Username"": ""system"",
      ""IsService"": false,
      ""IdentityEstablishedWith"": """",
      ""UserAgent"": ""Server"",
      ""Occurred"": ""2022-02-11T15:47:36.034+00:00"",
      ""Message"": ""Deploy to Staging (#2) succeeded for Ethereum-Gas-Server release 0.0.1.39 to Staging"",
      ""MessageHtml"": ""<a href='#/deployments/Deployments-84781'>Deploy to Staging (#2)</a> succeeded for <a href='#/projects/Projects-825'>Ethereum-Gas-Server</a> release <a href='#/releases/Releases-81092'>0.0.1.39</a> to <a href='#/environments/Environments-2'>Staging</a>"",
      ""MessageReferences"": [
        {
          ""ReferencedDocumentId"": ""Deployments-84781"",
          ""StartIndex"": 0,
          ""Length"": 22
        },
        {
          ""ReferencedDocumentId"": ""Projects-825"",
          ""StartIndex"": 37,
          ""Length"": 19
        },
        {
          ""ReferencedDocumentId"": ""Releases-81092"",
          ""StartIndex"": 65,
          ""Length"": 8
        },
        {
          ""ReferencedDocumentId"": ""Environments-2"",
          ""StartIndex"": 77,
          ""Length"": 7
        }
      ],
      ""Comments"": null,
      ""Details"": null,
      ""ChangeDetails"": {
        ""DocumentContext"": null,
        ""Differences"": null
      },
      ""SpaceId"": ""Spaces-1"",
      ""Links"": {
        ""Self"": ""/api/events/Events-693825""
      }
    },
    ""BatchId"": ""8cd8baf9-2d1d-414b-988b-b30a7f380377"",
    ""TotalEventsInBatch"": 1,
    ""EventNumberInBatch"": 1
  }
}";

    public DecodesOctopusPush(ITestOutputHelper output)
        : base(output)
    {
    }

    private static JsonSerializerOptions SerializerOptions { get; } = new()
                                                                      {
                                                                          DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                                                                          PropertyNameCaseInsensitive = false,
                                                                          PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                                                          WriteIndented = true
                                                                      };

    private static JsonSerializerOptions SerializerOptionsWithContext { get; } = JsonSerialiser.Configure(new());

    [Fact]
    [SuppressMessage(category: "Philips.CodeAnalysis.DuplicateCodeAnalyzer", checkId: "PH2071:DuplicateCodeDetection", Justification = "Test code")]
    public void Decode()
    {
        Deploy packet = AssertReallyNotNull(JsonSerializer.Deserialize<Deploy>(json: OCTOPUS_PUSH, options: SerializerOptions));

        Assert.Equal(expected: "SubscriptionPayload", actual: packet.EventType);
        Assert.Equal(expected: "https://octopus.funfair.io", actual: packet.Payload?.ServerUri);
        Assert.Equal(expected: "Spaces-1", actual: packet.Payload?.Event?.SpaceId);
        Assert.Equal(new[]
                     {
                         "Deployments-84781",
                         "Projects-825",
                         "Releases-81092",
                         "Environments-2",
                         "ServerTasks-197738",
                         "Channels-1070"
                     },
                     actual: packet.Payload?.Event?.RelatedDocumentIds);

        Assert.Equal(expected: "DeploymentSucceeded", actual: packet.Payload?.Event?.Category);
    }

    [Fact]
    public void DecodeOpt()
    {
        Deploy packet = AssertReallyNotNull(JsonSerializer.Deserialize<Deploy>(json: OCTOPUS_PUSH, options: SerializerOptionsWithContext));

        Assert.Equal(expected: "SubscriptionPayload", actual: packet.EventType);
        Assert.Equal(expected: "https://octopus.funfair.io", actual: packet.Payload?.ServerUri);
        Assert.Equal(expected: "Spaces-1", actual: packet.Payload?.Event?.SpaceId);
        Assert.Equal(new[]
                     {
                         "Deployments-84781",
                         "Projects-825",
                         "Releases-81092",
                         "Environments-2",
                         "ServerTasks-197738",
                         "Channels-1070"
                     },
                     actual: packet.Payload?.Event?.RelatedDocumentIds);

        Assert.Equal(expected: "DeploymentSucceeded", actual: packet.Payload?.Event?.Category);
    }
}