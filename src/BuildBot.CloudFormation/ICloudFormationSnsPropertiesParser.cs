using System.Collections.Generic;
using BuildBot.ServiceModel.CloudFormation;

namespace BuildBot.CloudFormation;

public interface ICloudFormationSnsPropertiesParser
{
    Dictionary<string, string> SplitMessageToDictionary(SnsMessage message);
}