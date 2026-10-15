# BuildBot

FunFair's Build Discord Bot

## Build Status

| Branch | Status                                                                                                                                                                                                                             |
|--------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| main   | [![Build: .NET Pre-Release](https://github.com/funfair-tech/BuildBot/actions/workflows/build-and-publish-pre-release.yml/badge.svg)](https://github.com/funfair-tech/BuildBot/actions/workflows/build-and-publish-pre-release.yml) |

## Configuration

| Setting                | Environment variable    | Type   | Default | Description                                                                                                                                                                                                                                                                                                                                                  |
|------------------------|-------------------------|--------|---------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `GitHub:WebhookSecret` | `GITHUB__WEBHOOKSECRET` | string | (empty) | Shared secret configured on the GitHub webhook, used to verify the HMAC-SHA256 `X-Hub-Signature-256` header on `/github/*` requests. Requests with a missing or invalid signature are rejected with 401. When empty, signatures are not verified and a warning is logged at startup. In AWS it is read from the Secrets Manager key `GITHUB_WEBHOOK_SECRET`. |

## Changelog

View [changelog](CHANGELOG.md)
