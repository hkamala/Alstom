using System.Collections.Generic;
using System.Xml.Serialization;

namespace ConflictManagementService.Model.TMS
{
    [XmlType("ServiceList")]
    public class ServiceList
    {
        public ServiceList()
        {
        }
        [XmlElement(ElementName = "ReqID")]
        public uint ReqID;
        [XmlArray("Services")]
        public List<ServiceItem>? services;
    }
}
