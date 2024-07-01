using System.Xml.Serialization;

namespace ConflictManagementService.Model.TMS
{
    [XmlRoot("TSUIHelloRequest")]
    public class TSUIHelloRequest
    {
        [XmlElement(ElementName = "SubID")]
        public string? subID;
        [XmlElement(ElementName = "ServiceStateChanged")]
        public bool serviceStateChanged;

    }
}
