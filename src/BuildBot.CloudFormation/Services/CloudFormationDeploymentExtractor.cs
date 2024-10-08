using System;
using System.Collections.Generic;
using System.Diagnostics;
using BuildBot.CloudFormation.Models;
using BuildBot.CloudFormation.Services.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace BuildBot.CloudFormation.Services;

public sealed class CloudFormationDeploymentExtractor : ICloudFormationDeploymentExtractor
{
    private const string STACK_ID = "StackId";
    private const string STACK_NAME = "StackName";
    private const string RESOURCE_TYPE = "ResourceType";
    private const string LOGICAL_RESOURCE_ID = "LogicalResourceId";
    private const string PHYSICAL_RESOURCE_ID = "PhysicalResourceId";
    private const string RESOURCE_STATUS = "ResourceStatus";

    private static readonly Dictionary<string, bool> Statuses = new(StringComparer.Ordinal)
                                                                {
                                                                    ["CREATE_COMPLETE"] = true,
                                                                    ["CREATE_FAILED"] = false,
                                                                    ["DELETE_COMPLETE"] = true,
                                                                    ["DELETE_FAILED"] = false,
                                                                    ["ROLLBACK_COMPLETE"] = false,
                                                                    ["ROLLBACK_IN_PROGRESS"] = false,
                                                                    ["ROLLBACK_FAILED"] = false,
                                                                    ["UPDATE_COMPLETE"] = true,
                                                                    ["UPDATE_ROLLBACK_COMPLETE"] = false,
                                                                    ["UPDATE_ROLLBACK_FAILED"] = false,
                                                                    ["UPDATE_ROLLBACK_IN_PROGRESS"] = false
                                                                };

    private readonly ILogger<CloudFormationDeploymentExtractor> _logger;

    public CloudFormationDeploymentExtractor(ILogger<CloudFormationDeploymentExtractor> logger)
    {
        this._logger = logger;
    }

    public Deployment? ExtractDeploymentProperties(in CloudFormationMessageReceived notification)
    {
        this.DumpAllProperties(notification);

        if (!IsMatching(properties: notification.Properties, key: RESOURCE_TYPE, value: "AWS::CloudFormation::Stack"))
        {
            this._logger.NotACloudFormationStack();

            return null;
        }

        if (!notification.Properties.TryGetValue(key: STACK_ID, out string? stackId))
        {
            this._logger.NoStackIdFound();

            return null;
        }

        if (!notification.Properties.TryGetValue(key: STACK_NAME, out string? stackName))
        {
            this._logger.NoStackNameFound();

            return null;
        }

        if (!notification.Properties.TryGetValue(key: LOGICAL_RESOURCE_ID, out string? project))
        {
            this._logger.NoLogicalResourceIdFound();

            return null;
        }

        this._logger.LogicalResourceIdFound(project);

        if (!notification.Properties.TryGetValue(key: PHYSICAL_RESOURCE_ID, out string? arn))
        {
            this._logger.NoPhysicalResourceIdFound();

            return null;
        }

        if (!notification.Properties.TryGetValue(key: RESOURCE_STATUS, out string? status))
        {
            this._logger.NoResourceStatusFound();

            return null;
        }

        if (Statuses.TryGetValue(key: status, out bool success))
        {
            this.LogConfiguration(stackName: stackName, project: project, arn: arn, status: status, success: success);

            return new Deployment(StackName: stackName, Project: project, Arn: arn, Status: status, Success: success, StackId: stackId);
        }

        this._logger.UnknownStatus(status);

        return null;
    }

    private void LogConfiguration(string stackName, string project, string arn, string status, bool success)
    {
        this._logger.StackName(stackName);
        this._logger.Project(project);
        this._logger.AwsArn(arn);
        this._logger.ResourceStatus(status: status, success: success);
    }

    [Conditional("DEBUG")]
    private void DumpAllProperties(CloudFormationMessageReceived notification)
    {
        foreach ((string key, string value) in notification.Properties)
        {
            this._logger.Property(key: key, value: value);
        }
    }

    private static bool IsMatching(Dictionary<string, string> properties, string key, string value)
    {
        return properties.TryGetValue(key: key, out string? propertyValue) && StringComparer.Ordinal.Equals(x: propertyValue, y: value);
    }
}