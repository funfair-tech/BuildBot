using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BuildBot.CloudFormation.Tests.Publishers;

internal sealed class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _responseMessage;

    public TestHttpMessageHandler(HttpResponseMessage responseMessage)
    {
        this._responseMessage = responseMessage;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        return Task.FromResult(this._responseMessage);
    }
}
