using System.Xml.Serialization;

namespace ConflictManagementService.Model.TMS
{
    [XmlType("PlItem")]
    public class PlatformItem
    {
        public PlatformItem()
        {
        }
        [XmlAttribute("TrId")]
        public string? trainID;
        [XmlAttribute("serid")]
        public string? serviceID;
        [XmlAttribute("ASsecs")]
        public int arrivalTimeSecs;
        [XmlAttribute("DSsecs")]
        public int departureTimeSecs;
        [XmlAttribute("DtTT")]
        public int diffToTimeTable;
        [XmlAttribute("PlN")]
        public string? platformName;
        [XmlAttribute("AcN")]
        public string? actionName;
        [XmlAttribute("AcId")]
        public int actionID;
        [XmlAttribute("trid")]
        public string? tripID;
        public int EventSeconds { get { if (arrivalTimeSecs > departureTimeSecs) return arrivalTimeSecs; return departureTimeSecs; } }
    }
}
