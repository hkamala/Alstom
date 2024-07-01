using System.Collections.Generic;
using System.Xml.Serialization;

namespace ConflictManagementService.Model.TMS
{
    public enum eTripUserType
    {
        [XmlEnum("0")]
        notValid,
        [XmlEnum("1")]
        system,
        [XmlEnum("2")]
        user,
        [XmlEnum("3")]
        system_and_user
    }

    [XmlType("TrItem")]
    public class TripItem
    {
        public TripItem()
        {
        }
        [XmlAttribute("trid")]
        public int tripID;
        [XmlAttribute("trdbid")]
        public int tripDBID;
        [XmlAttribute("TrN")]
        public string? name;
        [XmlAttribute("ttypeid")]
        public int tripTypeID;
        [XmlAttribute("PSsecs")]
        public int plannedStartSecs;
        [XmlAttribute("PEsecs")]
        public int plannedEndSecs;
        [XmlAttribute("TrSt")]
        public SchedulingState tripState;
        [XmlAttribute("TrId")]
        public string trainID;              // Used to send trip modifications
        [XmlAttribute("SerId")]
        public int serviceID;
        [XmlArrayItem(Type = typeof(MovingAction)),
         XmlArrayItem(Type = typeof(StopAction)),
         XmlArrayItem(Type = typeof(PassAction))]
        [XmlArray("Actions")]
        public List<TimetableAction>? tripActions;
    }
}
