using System;
using System.Collections.Generic;
using BuildBot.CloudFormation.Models;
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
        this._logger.LogWarning(message: "CLOUDFORMATION: Received message from {TopicArn} with {MessageId} at {Timestamp}", notification.TopicArn, notification.MessageId, notification.Timestamp);

        this.DumpAllProperties(notification);

        if (!IsMatching(properties: notification.Properties, key: RESOURCE_TYPE, value: "AWS::CloudFormation::Stack"))
        {
            this._logger.LogWarning(message: "CLOUDFORMATION: Not a cloud formation stack");

            return null;
        }

        if (!notification.Properties.TryGetValue(key: STACK_ID, out string? stackId))
        {
            this._logger.LogWarning(message: "CLOUDFORMATION: No stack id found");

            return null;
        }

        if (!notification.Properties.TryGetValue(key: STACK_NAME, out string? stackName))
        {
            this._logger.LogWarning(message: "CLOUDFORMATION: No stack name found");

            return null;
        }

        if (!notification.Properties.TryGetValue(key: LOGICAL_RESOURCE_ID, out string? project))
        {
            this._logger.LogWarning(message: "CLOUDFORMATION: No logical resource id found");

            return null;
        }

        this._logger.LogWarning(message: "CLOUDFORMATION: LogicalResourceId: {LogicalResourceId}", project);

        if (!notification.Properties.TryGetValue(key: PHYSICAL_RESOURCE_ID, out string? arn))
        {
            this._logger.LogWarning(message: "CLOUDFORMATION: No physical resource id found");

            return null;
        }

        if (!notification.Properties.TryGetValue(key: RESOURCE_STATUS, out string? status))
        {
            this._logger.LogWarning(message: "CLOUDFORMATION: No resource status found");

            return null;
        }

        if (Statuses.TryGetValue(key: status, out bool success))
        {
            this.LogConfiguration(stackName: stackName, project: project, arn: arn, success: success);

            return new Deployment(StackName: stackName, Project: project, Arn: arn, Status: status, Success: success, StackId: stackId);
        }

        this._logger.LogWarning(message: "CLOUDFORMATION: Unknown status");

        return null;
    }

    private void LogConfiguration(string stackName, string project, string arn, bool success)
    {
        this._logger.LogWarning(message: "CLOUDFORMATION: Stack Name: {StackName}", stackName);
        this._logger.LogWarning(message: "CLOUDFORMATION: Project: {Project}", project);
        this._logger.LogWarning(message: "CLOUDFORMATION: Arn: {Arn}", arn);
        this._logger.LogWarning(message: "CLOUDFORMATION: Status: {ResourceStatus}", arn);
        this._logger.LogWarning(message: success
                                    ? "CLOUDFORMATION: SUCCEEDED!!!"
                                    : "CLOUDFORMATION: FAILED!!!");
    }

    private void DumpAllProperties(CloudFormationMessageReceived notification)
    {
        foreach ((string key, string value) in notification.Properties)
        {
            this._logger.LogWarning(message: "CLOUDFORMATION: Property: {Key} = {Value}", key, value);
        }
    }

    private static bool IsMatching(Dictionary<string, string> properties, string key, string value)
    {
        return properties.TryGetValue(key: key, out string? propertyValue) && StringComparer.Ordinal.Equals(x: propertyValue, y: value);
    }
}