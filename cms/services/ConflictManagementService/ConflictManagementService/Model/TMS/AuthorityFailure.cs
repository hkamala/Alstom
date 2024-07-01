using System.Xml.Serialization;

namespace ConflictManagementService.Model.TMS
{
    [XmlRoot("AuthorityFailure")]
    public class AuthorityFailure
	{
		[XmlElement("RequestSchema")]
		public string requestSchema = "";
		[XmlElement("RequestCounter")]
		public uint requestCounter = 0;
		[XmlElement("RequestHandler")]
		public string requestHandler = "";
	}
}
