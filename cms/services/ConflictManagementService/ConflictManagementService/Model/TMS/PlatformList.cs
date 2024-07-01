using System.Collections.Generic;
using System.Xml.Serialization;

namespace ConflictManagementService.Model.TMS
{
    [XmlType("PlatformList")]
    public class PlatformList
    {
        public PlatformList()
        {
        }
        [XmlElement(ElementName = "ReqID")]
        public uint ReqID;
        [XmlArray("Platforms")]
        public List<PlatformItem>? platforms;
    }
}