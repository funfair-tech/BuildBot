using System;
using System.Collections.Generic;
using BuildBot.CloudFormation.Services;
using BuildBot.ServiceModel.CloudFormation;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.CloudFormation.Tests.Services;

public sealed class CloudFormationSnsPropertiesParserTests : TestBase
{
    private readonly ICloudFormationSnsPropertiesParser _cloudFormationSnsPropertiesParser;

    public CloudFormationSnsPropertiesParserTests()
    {
        this._cloudFormationSnsPropertiesParser = new CloudFormationSnsPropertiesParser();
    }

    [Fact]
    public void Parse()
    {
        SnsMessage message = new(type: "Notification",
                                 messageId: "arn:aws:sns:eu-west-1:1234:Test",
                                 token: "Test",
                                 topicArn: "Test",
                                 subject: "Example",
                                 message:
                                 "StackId='arn:aws:cloudformation:eu-west-1:117769150821:stack/CoinBot/bfa40f80-7041-11ef-bbf4-0ac3d395d6cf'\nTimestamp='2024-10-07T11:57:10.021Z'\nEventId='485cc630-84a3-11ef-a635-0a14bd780565'\nLogicalResourceId='CoinBot'\nNamespace='117769150821'\nPhysicalResourceId='arn:aws:cloudformation:eu-west-1:117769150821:stack/CoinBot/bfa40f80-7041-11ef-bbf4-0ac3d395d6cf'\nPrincipalId='AIDARW24WUFS3BSXPHXDN'\nResourceProperties='null'\nResourceStatus='UPDATE_COMPLETE'\nResourceStatusReason=''\nDetailedStatus=''\nResourceType='AWS::CloudFormation::Stack'\nStackName='CoinBot'\nClientRequestToken='null'\n",
                                 new(year: 2024, month: 1, day: 1, hour: 1, minute: 1, second: 1, kind: DateTimeKind.Utc),
                                 signatureVersion: "1",
                                 signature:
                                 "EXAMPLElDMXvB8r9R83tGoNn0ecwd5UjllzsvSvbItzfaMpN2nk5HVSw7XnOn/49IkxDKz8YrlH2qJXj2iZB0Zo2O71c4qQk1fMUDi3LGpij7RCW7AW9vYYsSqIKRnFS94ilu7NFhUzLiieYr4BKHpdTmdD6c0esKEYBpabxDSc=",
                                 signingCertUrl: "https://sns.us-west-2.amazonaws.com/SimpleNotificationService-f3ecfb7224c7233fe7bb5f59f96de52f.pem",
                                 subscribeUrl: null,
                                 unsubscribeUrl: null);

        Dictionary<string, string> result = this._cloudFormationSnsPropertiesParser.SplitMessageToDictionary(message);

        Assert.Contains(expected: "StackId", collection: result.Keys);
        Assert.Contains(expected: "Timestamp", collection: result.Keys);
        Assert.Contains(expected: "EventId", collection: result.Keys);
    }
}