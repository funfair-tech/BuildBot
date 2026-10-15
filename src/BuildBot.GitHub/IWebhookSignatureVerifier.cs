using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BuildBot.GitHub;

public interface IWebhookSignatureVerifier
{
    ValueTask<WebhookSignatureVerificationResult> VerifyAsync(
        Stream body,
        string signatureHeader,
        CancellationToken cancellationToken
    );
}
