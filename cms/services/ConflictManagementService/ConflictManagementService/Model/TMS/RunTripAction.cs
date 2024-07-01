using System.Xml.Serialization;

namespace ConflictManagementService.Model.TMS
{
    [XmlType("Rt")]
    public class RunTripAction : TimetableAction
    {
        public RunTripAction()
        {
        }
        [XmlAttribute("trid")]
        public int tripID;
        [XmlAttribute("ttrid")]
        public string? templateTripID;
        [XmlAttribute("TrN")]
        public string? tripName;
        [XmlAttribute("TrC")]
        public string? tripCode;
        [XmlAttribute("PSsecs")]
        public int plannedStartSecs;
        [XmlAttribute("PEsecs")]
        public int plannedEndSecs;
        [XmlAttribute("TrNo")]
        public int tripNo;
    }
}
