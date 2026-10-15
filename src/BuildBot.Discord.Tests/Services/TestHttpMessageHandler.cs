using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BuildBot.Discord.Tests.Services;

internal sealed class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly ConcurrentQueue<Func<HttpResponseMessage>> _responses;
    private readonly ConcurrentQueue<RecordedRequest> _requests = new();

    public TestHttpMessageHandler(params IEnumerable<Func<HttpResponseMessage>> responses)
    {
        this._responses = new(responses);
    }

    public IReadOnlyList<RecordedRequest> Requests => [.. this._requests];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        string body = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);

        this._requests.Enqueue(
            new(
                Method: request.Method,
                RequestUri: request.RequestUri,
                Body: body,
                ContentType: request.Content?.Headers.ContentType?.MediaType
            )
        );

        cancellationToken.ThrowIfCancellationRequested();

        if (!this._responses.TryDequeue(out Func<HttpResponseMessage>? response))
        {
            throw new InvalidOperationException("No more responses configured");
        }

        return response();
    }

    [DebuggerDisplay("{Method} {RequestUri}")]
    internal sealed record RecordedRequest(HttpMethod Method, Uri? RequestUri, string Body, string? ContentType);
}
