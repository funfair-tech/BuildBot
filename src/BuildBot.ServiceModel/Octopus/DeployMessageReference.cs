namespace BuildBot.ServiceModel.Octopus
{
#nullable disable
    public sealed class DeployMessageReference
    {
        public string ReferencedDocumentId { get; set; }

        public int StartIndex { get; set; }

        public int Length { get; set; }
    }
}