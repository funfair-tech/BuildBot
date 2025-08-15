using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.CloudFormation;

/*
 * {
     "Type" : "Notification",
     "MessageId" : "da41e39f-ea4d-435a-b922-c6aae3915ebe",
     "TopicArn" : "arn:aws:sns:us-west-2:123456789012:MyTopic",
     "Subject" : "test",
     "Message" : "test message",
     "Timestamp" : "2012-04-25T21:49:25.719Z",
     "SignatureVersion" : "1",
     "Signature" : "EXAMPLElDMXvB8r9R83tGoNn0ecwd5UjllzsvSvbItzfaMpN2nk5HVSw7XnOn/49IkxDKz8YrlH2qJXj2iZB0Zo2O71c4qQk1fMUDi3LGpij7RCW7AW9vYYsSqIKRnFS94ilu7NFhUzLiieYr4BKHpdTmdD6c0esKEYBpabxDSc=",
     "SigningCertURL" : "https://sns.us-west-2.amazonaws.com/SimpleNotificationService-f3ecfb7224c7233fe7bb5f59f96de52f.pem",
      "UnsubscribeURL" : "https://sns.us-west-2.amazonaws.com/?Action=Unsubscribe&SubscriptionArn=arn:aws:sns:us-west-2:123456789012:MyTopic:2bcfbf39-05c3-41de-beaa-fcfcc21c8f55"
   }
 */
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
