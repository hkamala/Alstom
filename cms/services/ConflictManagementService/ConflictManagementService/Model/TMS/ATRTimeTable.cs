using System.Collections.Generic;
using System.Xml.Serialization;

namespace ConflictManagementService.Model.TMS
{
    [XmlRoot("ATRTimeTable")]
    public class ATRTimeTable
    {
        public ATRTimeTable()
        {
        }
        [XmlElement("TaID")]
        public int trafficAreaID;        // Valid only in Scheduled timetable
        [XmlElement("AaID")]
        public int atrAreaID;            // Valid only in Scheduled timetable
        [XmlElement("dateTime")]
        public string? isoDateTime;
        [XmlElement("StartIndex")]
        public int startIndex;
        [XmlElement("TotalCount")]
        public int totalCount;
        [XmlElement("GraphID")]
        public uint graphID;

        //[XmlArray("Trains")] public ATRGraphTrain [] trains;
        [XmlArray("Trains")]
        public List<TrafficGraphTrainPath>? trains;

        public void AddTrains(ATRTimeTable source)
        {
            if (source.trains != null)
            {
                if (trains == null)
                    trains = new List<TrafficGraphTrainPath>();
                trains.AddRange(source.trains);
            }
        }

        [XmlElement(ElementName = "DayType", IsNullable = true)]
        public uint DayType;
    }
}
