using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SubtitleDownloader.Implementations.OpenSubtitles
{
    [XmlRoot("Configuration")]
    [DataContract]
    public class OpenSubtitlesConfiguration
    {
        [XmlElement("Username")]
        [DataMember(Name="username")]
        public string Username  = "";

        [XmlElement("Password")] 
        [DataMember(Name="password")]
        public string Password = "";

        [XmlElement("Language")]
        public string Language = "";
    }
}
