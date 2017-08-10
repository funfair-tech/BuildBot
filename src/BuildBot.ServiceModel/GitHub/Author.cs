using System.Runtime.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    [DataContract]
    public class Author
    {
        [DataMember(Name = "login")]
        public string Login { get; set; }
    }
}
