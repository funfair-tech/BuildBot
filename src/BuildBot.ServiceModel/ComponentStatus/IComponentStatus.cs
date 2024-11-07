namespace BuildBot.ServiceModel.ComponentStatus;

public interface IComponentStatus
{
    ServiceStatus GetStatus();
}