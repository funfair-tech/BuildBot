using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using BuildBot.Json;
using BuildBot.ServiceModel.GitHub;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.Tests;

public sealed class DecodesGithubPush : TestBase
{
    private const string GITHUB_PUSH =
        @"{
  ""ref"": ""refs/heads/main"",
  ""before"": ""1c46b424863be722f3bffa6ffd17545f94fd726e"",
  ""after"": ""07138d7ed444df3063e112183ad665cc0bc28e9a"",
  ""repository"": {
    ""id"": 99678954,
    ""node_id"": ""MDEwOlJlcG9zaXRvcnk5OTY3ODk1NA=="",
    ""name"": ""BuildBot"",
    ""full_name"": ""funfair-tech/BuildBot"",
    ""private"": false,
    ""owner"": {
      ""name"": ""funfair-tech"",
      ""email"": ""info@funfair.eu"",
      ""login"": ""funfair-tech"",
      ""id"": 29401895,
      ""node_id"": ""MDEyOk9yZ2FuaXphdGlvbjI5NDAxODk1"",
      ""avatar_url"": ""https://avatars.githubusercontent.com/u/29401895?v=4"",
      ""gravatar_id"": """",
      ""url"": ""https://api.github.com/users/funfair-tech"",
      ""html_url"": ""https://github.com/funfair-tech"",
      ""followers_url"": ""https://api.github.com/users/funfair-tech/followers"",
      ""following_url"": ""https://api.github.com/users/funfair-tech/following{/other_user}"",
      ""gists_url"": ""https://api.github.com/users/funfair-tech/gists{/gist_id}"",
      ""starred_url"": ""https://api.github.com/users/funfair-tech/starred{/owner}{/repo}"",
      ""subscriptions_url"": ""https://api.github.com/users/funfair-tech/subscriptions"",
      ""organizations_url"": ""https://api.github.com/users/funfair-tech/orgs"",
      ""repos_url"": ""https://api.github.com/users/funfair-tech/repos"",
      ""events_url"": ""https://api.github.com/users/funfair-tech/events{/privacy}"",
      ""received_events_url"": ""https://api.github.com/users/funfair-tech/received_events"",
      ""type"": ""Organization"",
      ""site_admin"": false
    },
    ""html_url"": ""https://github.com/funfair-tech/BuildBot"",
    ""description"": ""Discord Bot for reporting commit, build and deployment status"",
    ""fork"": false,
    ""url"": ""https://github.com/funfair-tech/BuildBot"",
    ""forks_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/forks"",
    ""keys_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/keys{/key_id}"",
    ""collaborators_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/collaborators{/collaborator}"",
    ""teams_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/teams"",
    ""hooks_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/hooks"",
    ""issue_events_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/issues/events{/number}"",
    ""events_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/events"",
    ""assignees_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/assignees{/user}"",
    ""branches_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/branches{/branch}"",
    ""tags_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/tags"",
    ""blobs_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/git/blobs{/sha}"",
    ""git_tags_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/git/tags{/sha}"",
    ""git_refs_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/git/refs{/sha}"",
    ""trees_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/git/trees{/sha}"",
    ""statuses_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/statuses/{sha}"",
    ""languages_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/languages"",
    ""stargazers_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/stargazers"",
    ""contributors_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/contributors"",
    ""subscribers_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/subscribers"",
    ""subscription_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/subscription"",
    ""commits_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/commits{/sha}"",
    ""git_commits_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/git/commits{/sha}"",
    ""comments_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/comments{/number}"",
    ""issue_comment_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/issues/comments{/number}"",
    ""contents_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/contents/{+path}"",
    ""compare_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/compare/{base}...{head}"",
    ""merges_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/merges"",
    ""archive_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/{archive_format}{/ref}"",
    ""downloads_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/downloads"",
    ""issues_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/issues{/number}"",
    ""pulls_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/pulls{/number}"",
    ""milestones_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/milestones{/number}"",
    ""notifications_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/notifications{?since,all,participating}"",
    ""labels_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/labels{/name}"",
    ""releases_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/releases{/id}"",
    ""deployments_url"": ""https://api.github.com/repos/funfair-tech/BuildBot/deployments"",
    ""created_at"": 1502186838,
    ""updated_at"": ""2022-03-27T19:06:14Z"",
    ""pushed_at"": 1662938018,
    ""git_url"": ""git://github.com/funfair-tech/BuildBot.git"",
    ""ssh_url"": ""git@github.com:funfair-tech/BuildBot.git"",
    ""clone_url"": ""https://github.com/funfair-tech/BuildBot.git"",
    ""svn_url"": ""https://github.com/funfair-tech/BuildBot"",
    ""homepage"": """",
    ""size"": 1803,
    ""stargazers_count"": 4,
    ""watchers_count"": 4,
    ""language"": ""C#"",
    ""has_issues"": false,
    ""has_projects"": false,
    ""has_downloads"": true,
    ""has_wiki"": false,
    ""has_pages"": false,
    ""forks_count"": 1,
    ""mirror_url"": null,
    ""archived"": false,
    ""disabled"": false,
    ""open_issues_count"": 0,
    ""license"": {
      ""key"": ""mit"",
      ""name"": ""MIT License"",
      ""spdx_id"": ""MIT"",
      ""url"": ""https://api.github.com/licenses/mit"",
      ""node_id"": ""MDc6TGljZW5zZTEz""
    },
    ""allow_forking"": true,
    ""is_template"": false,
    ""web_commit_signoff_required"": false,
    ""topics"": [
      ""buildbot"",
      ""discord"",
      ""discord-bot""
    ],
    ""visibility"": ""public"",
    ""forks"": 1,
    ""open_issues"": 0,
    ""watchers"": 4,
    ""default_branch"": ""main"",
    ""stargazers"": 4,
    ""master_branch"": ""main"",
    ""organization"": ""funfair-tech""
  },
  ""pusher"": {
    ""name"": ""credfeto"",
    ""email"": ""credfeto@users.noreply.github.com""
  },
  ""organization"": {
    ""login"": ""funfair-tech"",
    ""id"": 29401895,
    ""node_id"": ""MDEyOk9yZ2FuaXphdGlvbjI5NDAxODk1"",
    ""url"": ""https://api.github.com/orgs/funfair-tech"",
    ""repos_url"": ""https://api.github.com/orgs/funfair-tech/repos"",
    ""events_url"": ""https://api.github.com/orgs/funfair-tech/events"",
    ""hooks_url"": ""https://api.github.com/orgs/funfair-tech/hooks"",
    ""issues_url"": ""https://api.github.com/orgs/funfair-tech/issues"",
    ""members_url"": ""https://api.github.com/orgs/funfair-tech/members{/member}"",
    ""public_members_url"": ""https://api.github.com/orgs/funfair-tech/public_members{/member}"",
    ""avatar_url"": ""https://avatars.githubusercontent.com/u/29401895?v=4"",
    ""description"": """"
  },
  ""sender"": {
    ""login"": ""credfeto"",
    ""id"": 1020430,
    ""node_id"": ""MDQ6VXNlcjEwMjA0MzA="",
    ""avatar_url"": ""https://avatars.githubusercontent.com/u/1020430?v=4"",
    ""gravatar_id"": """",
    ""url"": ""https://api.github.com/users/credfeto"",
    ""html_url"": ""https://github.com/credfeto"",
    ""followers_url"": ""https://api.github.com/users/credfeto/followers"",
    ""following_url"": ""https://api.github.com/users/credfeto/following{/other_user}"",
    ""gists_url"": ""https://api.github.com/users/credfeto/gists{/gist_id}"",
    ""starred_url"": ""https://api.github.com/users/credfeto/starred{/owner}{/repo}"",
    ""subscriptions_url"": ""https://api.github.com/users/credfeto/subscriptions"",
    ""organizations_url"": ""https://api.github.com/users/credfeto/orgs"",
    ""repos_url"": ""https://api.github.com/users/credfeto/repos"",
    ""events_url"": ""https://api.github.com/users/credfeto/events{/privacy}"",
    ""received_events_url"": ""https://api.github.com/users/credfeto/received_events"",
    ""type"": ""User"",
    ""site_admin"": false
  },
  ""created"": false,
  ""deleted"": false,
  ""forced"": false,
  ""base_ref"": null,
  ""compare"": ""https://github.com/funfair-tech/BuildBot/compare/1c46b424863b...07138d7ed444"",
  ""commits"": [
    {
      ""id"": ""07138d7ed444df3063e112183ad665cc0bc28e9a"",
      ""tree_id"": ""8d90051423c8ef94a70c7c904f4899d6127b052e"",
      ""distinct"": true,
      ""message"": ""Corrected"",
      ""timestamp"": ""2022-09-12T00:13:35+01:00"",
      ""url"": ""https://github.com/funfair-tech/BuildBot/commit/07138d7ed444df3063e112183ad665cc0bc28e9a"",
      ""author"": {
        ""name"": ""Mark Ridgwell"",
        ""email"": ""credfeto@users.noreply.github.com"",
        ""username"": ""credfeto""
      },
      ""committer"": {
        ""name"": ""Mark Ridgwell"",
        ""email"": ""credfeto@users.noreply.github.com"",
        ""username"": ""credfeto""
      },
      ""added"": [

      ],
      ""removed"": [

      ],
      ""modified"": [
        ""src/BuildBot/Startup.cs""
      ]
    }
  ],
  ""head_commit"": {
    ""id"": ""07138d7ed444df3063e112183ad665cc0bc28e9a"",
    ""tree_id"": ""8d90051423c8ef94a70c7c904f4899d6127b052e"",
    ""distinct"": true,
    ""message"": ""Corrected"",
    ""timestamp"": ""2022-09-12T00:13:35+01:00"",
    ""url"": ""https://github.com/funfair-tech/BuildBot/commit/07138d7ed444df3063e112183ad665cc0bc28e9a"",
    ""author"": {
      ""name"": ""Mark Ridgwell"",
      ""email"": ""credfeto@users.noreply.github.com"",
      ""username"": ""credfeto""
    },
    ""committer"": {
      ""name"": ""Mark Ridgwell"",
      ""email"": ""credfeto@users.noreply.github.com"",
      ""username"": ""credfeto""
    },
    ""added"": [

    ],
    ""removed"": [

    ],
    ""modified"": [
      ""src/BuildBot/Startup.cs""
    ]
  }
}";

    [Fact]
    public void DecodeOpt()
    {
        JsonTypeInfo<Push> pushTypeInfo = AssertReallyNotNull(
            AppSerializationContext.Default.GetTypeInfo(typeof(Push)) as JsonTypeInfo<Push>
        );

        Push packet = JsonSerializer.Deserialize(json: GITHUB_PUSH, jsonTypeInfo: pushTypeInfo);

        Assert.Equal(expected: "refs/heads/main", actual: packet.Ref);
        Assert.NotEmpty(packet.Commits);
    }
}
