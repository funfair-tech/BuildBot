using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.CloudFormation;

[DebuggerDisplay("{Type} ({MessageId}): {Message}")]
public sealed class SnsMessage
{
    [SuppressMessage(
        category: "Roslynator.Analyzers",
        checkId: "RCS1231: Make parameter ref read-only.",
        Justification = "Serialisation model"
    )]
    [JsonConstructor]
    public SnsMessage(
        string type,
        string messageId,
        string? token,
        string topicArn,
        string subject,
        string message,
        DateTime timestamp,
        string signatureVersion,
        string signature,
        string? signingCertUrl,
        string? subscribeUrl,
        string? unsubscribeUrl
    )
    {
        this.Type = type;
        this.MessageId = messageId;
        this.TopicArn = topicArn;
        this.Subject = subject;
        this.Message = message;
        this.Timestamp = timestamp;
        this.SignatureVersion = signatureVersion;
        this.Signature = signature;
        this.SigningCertUrl = signingCertUrl;
        this.SubscribeUrl = subscribeUrl;
        this.UnsubscribeUrl = unsubscribeUrl;
    }

    [JsonPropertyName("Type")]
    public string Type { get; }

    [JsonPropertyName("MessageId")]
    public string MessageId { get; }

    [JsonPropertyName("Token")]
    public string? Token { get; }

    [JsonPropertyName("TopicArn")]
    public string TopicArn { get; }

    [JsonPropertyName("Subject")]
    public string Subject { get; }

    [JsonPropertyName("Message")]
    public string Message { get; }

    [JsonPropertyName("Timestamp")]
    public DateTime Timestamp { get; }

    [JsonPropertyName("SignatureVersion")]
    public string SignatureVersion { get; }

    [JsonPropertyName("Signature")]
    public string Signature { get; }

    [SuppressMessage(
        category: "Microsoft.Naming",
        checkId: "CA1056: Uri properties should not be strings",
        Justification = "AWS SNS URL"
    )]
    [JsonPropertyName("SigningCertURL")]
    public string? SigningCertUrl { get; }

    [SuppressMessage(
        category: "Microsoft.Naming",
        checkId: "CA1056: Uri properties should not be strings",
        Justification = "AWS SNS URL"
    )]
    [JsonPropertyName("SubscribeURL")]
    public string? SubscribeUrl { get; }

    [SuppressMessage(
        category: "Microsoft.Naming",
        checkId: "CA1056: Uri properties should not be strings",
        Justification = "AWS SNS URL"
    )]
    [JsonPropertyName("UnsubscribeURL")]
    public string? UnsubscribeUrl { get; }
}
