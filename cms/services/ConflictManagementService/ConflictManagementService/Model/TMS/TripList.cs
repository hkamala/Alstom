using System.Collections.Generic;
using System.Xml.Serialization;

namespace ConflictManagementService.Model.TMS
{
    [XmlType("TripList")]
    public class TripList
    {
        public enum TripUpdateData
        {
            [XmlEnum("0")]
            allTripdata,
            [XmlEnum("1")]
            withoutActions,
        }
        public enum TripListType
        {
            [XmlEnum("0")]
            tripsInTrainOrService,
            [XmlEnum("1")]
            templateRequest,
        }
        public TripList()
        {
        }
        [XmlElement("ReqID")]
        public uint ReqID;
        [XmlElement("serid")]
        public string? serviceID;
        [XmlAttribute("updateType")]
        public TripUpdateData updateType;
        [XmlAttribute("listType")]
        public TripListType listType;
        [XmlArray("Trips")]
        public List<TripItem>? trips;
    }
}