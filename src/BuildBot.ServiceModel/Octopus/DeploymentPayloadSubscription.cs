namespace BuildBot.ServiceModel.Octopus
{
    public sealed class DeploymentPayloadSubscription
    {
        public string Id { get; set; } = default!;

        public string Name { get; set; } = default!;

        public int Type { get; set; }

        public bool IsDisabled { get; set; }
    }
}