using System.Collections.Generic;
using System.Xml.Serialization;

namespace ConflictManagementService.Model.TMS
{
    /// <summary>
    /// Description of TripTemplateList.
    /// </summary>
    public class TripTemplateList
    {
        [XmlElement(ElementName = "ReqID")]
        public uint ReqID;
        [XmlArray("TripTemplates")]
        public List<TripTemplateItem>? tripTemplates;
    }
    [XmlType("TripTemplate")]
    public class TripTemplateItem
    {
        [XmlAttribute(AttributeName = "trid")]
        public string? TripGUID { get; set; }

        [XmlAttribute(AttributeName = "trdbid")]
        public uint ID { get; set; }

        [XmlAttribute(AttributeName = "ttypeid")]
        public int tripTypeID { get; set; }

        [XmlAttribute(AttributeName = "TrN")]
        public string? Description { get; set; }

        [XmlElement(ElementName = "FromId")]
        public uint FromId { get; set; }

        [XmlElement(ElementName = "ToId")]
        public uint ToId { get; set; }

        //[XmlArray("Evts")]
        //public List<TrainScheduleEvent> events;
    }
}
