using System.Collections.Generic;
using System.Xml.Serialization;

namespace ConflictManagementService.Model.TMS
{
    public enum SchedulingState
    {
        [XmlEnum("0")]
        nullState,
        [XmlEnum("1")]
        plannedToRun,
        [XmlEnum("2")]
        plannedSpare,
        [XmlEnum("3")]
        cancelled,
        [XmlEnum("4")]
        partlyCancelled,
        [XmlEnum("5")]
        finished,
        [XmlEnum("6")]
        finishedDegraded,
        [XmlEnum("7")]
        notValid
    }

    public enum eServiceInfoReason
    {
        [XmlEnum("0")]
        notSet,
        [XmlEnum("1")]
        query,
        [XmlEnum("2")]
        schedulingStateChanged,
        [XmlEnum("3")]
        connectionStateChanged,
        [XmlEnum("4")]
        shortTurnStateChanged,
        [XmlEnum("5")]
        created,
        [XmlEnum("6")]
        deleted,
        [XmlEnum("7")]
        modified,
        [XmlEnum("8")]
        tripStateChanged,
    }
    [XmlType("SrItem")]
    public class ServiceItem
    {
        public ServiceItem()
        {
        }

        [XmlAttribute("serid")]
        public string? serviceID;   // This is GUID! Get ID from TmsPTI!
        [XmlAttribute("TrN")]
        public string? name;
        [XmlAttribute("LId")]
        public int lineID;
        [XmlAttribute("State")]
        public SchedulingState currentState;
        [XmlAttribute("PState")]
        public SchedulingState plannedState;
        [XmlAttribute("PSsecs")]
        public int plannedStartSecs;
        [XmlAttribute("PEsecs")]
        public int plannedEndSecs;
        // Special attribute for scheduled service..		
        [XmlAttribute("Day")]
        public int scheduledDayCode;
        [XmlAttribute("HasTr")]
        public bool hasConnectedTrackedTrains = false;
        [XmlAttribute("ActCount")]
        public int activeTrainsCount;
        [XmlAttribute("ActiveTrainInstance")]
        public bool activeTrainInstance = false;

        [XmlElement("TmsPTI")]
        public TmsPTI? tmsPTI;
        [XmlElement("Reason")]
        public eServiceInfoReason reason;

        [XmlArrayItem(Type = typeof(RunTripAction)),
         XmlArrayItem(Type = typeof(TrainFormationAction)),
         XmlArrayItem(Type = typeof(GlueAction))]
        [XmlArray("Actions")]
        public List<TimetableAction>? serviceActions;
    }
}
