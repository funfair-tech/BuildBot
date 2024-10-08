using System.Threading;
using System.Threading.Tasks;

namespace BuildBot.CloudFormation;

public interface IAwsCloudFormation
{
    ValueTask<StackDetails?> GetStackDetailsAsync(Deployment deployment, CancellationToken cancellationToken);
}