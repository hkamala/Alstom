using System.Collections.Generic;
using System.Xml.Serialization;

namespace ConflictManagementService.Model.TMS
{
    [XmlType("Station")]
    public class Station
    {
        [XmlElement("Id")]
        public int id;
        [XmlElement("Name")]
        public string? name;
        [XmlElement("SchN")]
        public string? scheduleName;
        [XmlArray("Platforms")]
        public List<Platform>? platforms;
    }

    [XmlType("Group")]
    public class Group
    {
        [XmlElement("Id")]
        public int id;
        [XmlElement("Name")]
        public string? name;
        [XmlElement("SchN")]
        public string? scheduleName;
        [XmlArray("Platforms")]
        public List<Platform>? platforms;
    }

    [XmlType("Platform")]
    public class Platform
    {
        [XmlElement("NId")]
        public int nodeId; // Node id, from arealocations
        [XmlElement("Id")]
        public int id; // TTobjid, from TTObjects
        [XmlElement("Name")]
        public string? name;
        [XmlElement("SchN")]
        public string? scheduleName;
        [XmlElement("DAID")]
        public uint defaultActionID;
        [XmlElement("DADes")]
        public string? defaultActionDescription;
        [XmlArray("Lines")]
        [XmlArrayItem("L", typeof(TrafficLine))]
        public List<TrafficLine>? lines;
    }
    public class TrafficLine
    {
        [XmlAttribute("id")]
        public int lineID;
        [XmlAttribute("seqno")]
        public int locationSeqno;
    }

    [XmlRoot("StationList")]
    public class StationList
    {
        [XmlElement("ReqID")]
        public uint ReqID;
        [XmlElement("LiID")]
        public int lineId;
        [XmlArray("Stations")]
        public List<Station>? stations;
        [XmlArray("Groups")]
        public List<Group>? groups;
    }
}
