namespace BuildBot.ServiceModel.Octopus
{
    public sealed class DeployMessageReference
    {
        public string ReferencedDocumentId { get; set; } = default!;

        public int StartIndex { get; set; }

        public int Length { get; set; }
    }
}