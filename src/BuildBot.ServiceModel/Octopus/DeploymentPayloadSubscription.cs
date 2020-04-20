namespace BuildBot.ServiceModel.Octopus
{
#nullable disable
    public sealed class DeploymentPayloadSubscription
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public int Type { get; set; }

        public bool IsDisabled { get; set; }
    }
}