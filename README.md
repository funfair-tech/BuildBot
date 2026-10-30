# BuildBot

FunFair's build notification bot: receives GitHub, Watchtower and CloudFormation webhooks and posts the results to Discord.

## Build Status

| Branch  | Status                                                     |
|---------|------------------------------------------------------------|
| main    | [![Build: .NET Pre-Release][pre-release-img]][pre-release] |
| release | [![Build: .NET Release][release-img]][release]             |

## Overview

BuildBot is an ASP.NET Core service that listens for webhooks from GitHub (pushes and commit statuses), Watchtower (container updates) and CloudFormation (stack deployments via SNS). It turns each notification into a Discord embed and posts it to a single channel webhook, so build, deployment and container activity shows up in one place. GitHub requests are verified against the webhook's HMAC-SHA256 signature when a secret is configured.

## Quick Start

```shell
export GITHUB__WEBHOOKSECRET="<github-webhook-secret>"
export DISCORD__WEBHOOKURL="https://discord.com/api/webhooks/<id>/<token>"
dotnet run --project src/BuildBot/BuildBot.csproj
```

The service listens on port 8080; `curl http://localhost:8080/ping` checks that it is running.

## Installation

BuildBot is not published as a NuGet package. It ships as a container image: the build workflows run `dotnet publish` on `src/BuildBot` and build the image from the [`Dockerfile`][dockerfile], which copies the published `BuildBot` executable and `appsettings.json` onto a chiseled `runtime-deps` base image and exposes port 8080. See [Deployment](#deployment) for how it runs in AWS.

## Usage

Point each webhook source at the matching route:

| Method | Route                    | Source                                              |
|--------|--------------------------|-----------------------------------------------------|
| `GET`  | `/ping`                  | Health check                                        |
| `POST` | `/github/ping`           | GitHub webhook `ping` event                         |
| `POST` | `/github/push`           | GitHub webhook `push` event                         |
| `POST` | `/github/status`         | GitHub webhook `status` event                       |
| `POST` | `/watchtower/deploy`     | Watchtower container update notification            |
| `POST` | `/cloudformation/deploy` | CloudFormation stack event delivered by Amazon SNS  |

Example requests for the health check, GitHub and CloudFormation routes are in [`MinimalApi.http`][minimal-api-http], and [`DiscordWebhook.http`][discord-webhook-http] exercises the Discord webhook that BuildBot posts to.

## Configuration

| Setting                | Environment variable    | Type   | Default | Description                                                                                                                                                                                                                                                                                                                                                  |
|------------------------|-------------------------|--------|---------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `GitHub:WebhookSecret` | `GITHUB__WEBHOOKSECRET` | string | (empty) | Shared secret configured on the GitHub webhook, used to verify the HMAC-SHA256 `X-Hub-Signature-256` header on `/github/*` requests. Requests with a missing or invalid signature are rejected with 401. When empty, signatures are not verified and a warning is logged at startup. In AWS it is read from the Secrets Manager key `GITHUB_WEBHOOK_SECRET`. |
| `Discord:WebhookUrl`   | `DISCORD__WEBHOOKURL`   | URL    | (empty) | Discord channel webhook URL that build, deployment and container messages are posted to. Required: startup fails if it is empty or not an absolute `https` URL. The URL contains the webhook token, so treat it as a secret. In AWS it is read from the Secrets Manager key `DISCORD_WEBHOOK`.                                                               |

## Deployment

BuildBot runs behind the internal load balancer. Webhooks (GitHub, Watchtower and CloudFormation SNS notifications) arrive through an API Gateway HTTP API, which forwards them over a VPC Link to the internal load balancer. The CloudFormation stack owns the API Gateway custom domain and the Route 53 `A` alias record for the API host name.

### Request handling through API Gateway

- Only these routes are exposed: `GET /ping`, `POST /github/ping`, `POST /github/push`, `POST /github/status`, `POST /watchtower/deploy` and `POST /cloudformation/deploy`. Any other route gets API Gateway's own 404 and never reaches BuildBot.
- Payloads over 10 MB are rejected by API Gateway before they reach BuildBot.
- The `$default` stage throttles every route to a burst of 20 and a steady 10 requests per second, and writes a JSON access log line per request (source IP, route, status, integration error) to the `/apigateway/<ApplicationName>/` log group, kept for 30 days. The ECS application log group (`/ecs/<ApplicationName>/`) is also kept for 30 days.
- The real client IP address is in the `X-Client-Ip` header, which API Gateway overwrites so a caller cannot spoof it. `X-Forwarded-For` only holds the VPC Link's private address.
- Authentication for new webhook sources (SNS in #424, Watchtower in #425) must use the `Authorization` header with basic auth. Never use `X-Api-Key`.
- The VPC Link goes `INACTIVE` after 60 days without traffic. If webhooks stop arriving after a long quiet period, check the VPC Link status in the API Gateway console.

### First deploy: DNS cut-over

CloudFormation cannot import an existing Route 53 record set, so the existing `buildbot.funfair.io` record must be deleted by hand immediately before the first deploy that includes the API Gateway. Expect up to about 15 minutes of NXDOMAIN while resolvers' negative caches expire, and webhooks sent in that window may be lost.

1. Find the hosted zone ID and the current record's exact values (a `DELETE` must match the existing record exactly):

   ```shell
   aws route53 list-hosted-zones-by-name --dns-name funfair.io. --max-items 1
   aws route53 list-resource-record-sets --hosted-zone-id <HOSTED_ZONE_ID> --query "ResourceRecordSets[?Name=='buildbot.funfair.io.']"
   ```

   If records of other types (for example `AAAA`) exist for the name, delete them too, as the stack only creates the `A` record.

2. Delete the record, substituting the values returned above:

   ```shell
   aws route53 change-resource-record-sets \
     --hosted-zone-id <HOSTED_ZONE_ID> \
     --change-batch '{
       "Changes": [
         {
           "Action": "DELETE",
           "ResourceRecordSet": {
             "Name": "buildbot.funfair.io.",
             "Type": "A",
             "AliasTarget": {
               "HostedZoneId": "<CURRENT_ALIAS_HOSTED_ZONE_ID>",
               "DNSName": "<CURRENT_ALIAS_DNS_NAME>",
               "EvaluateTargetHealth": <CURRENT_EVALUATE_TARGET_HEALTH>
             }
           }
         }
       ]
     }'
   ```

3. Run the deploy straight away. The stack creates the `A` alias record pointing at the API Gateway custom domain. The record has a `Retain` deletion policy, so deleting or replacing the stack leaves it in place. Keep the output of step 1, so the old record can be restored with an `UPSERT` if the change has to be backed out.

4. If the deploy fails and rolls back, the `Retain` policy also keeps the newly created record, which then points at the API Gateway custom domain that the rollback removed, and a retry fails because the record already exists. Repeat steps 1 and 2 (the alias values are now the API Gateway ones) before retrying the deploy.

## Changelog

View the [changelog][changelog].

## Licence

BuildBot is released under the MIT licence; see [LICENSE][licence].

[changelog]: CHANGELOG.md
[discord-webhook-http]: src/BuildBot/DiscordWebhook.http
[dockerfile]: Dockerfile
[licence]: LICENSE
[minimal-api-http]: src/BuildBot/MinimalApi.http
[pre-release]: https://github.com/funfair-tech/BuildBot/actions/workflows/build-and-publish-pre-release.yml
[pre-release-img]: https://github.com/funfair-tech/BuildBot/actions/workflows/build-and-publish-pre-release.yml/badge.svg
[release]: https://github.com/funfair-tech/BuildBot/actions/workflows/build-and-publish-release.yml
[release-img]: https://github.com/funfair-tech/BuildBot/actions/workflows/build-and-publish-release.yml/badge.svg
