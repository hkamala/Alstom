using System.Xml.Serialization;

namespace ConflictManagementService.Model.TMS
{
    [XmlType("Mv")]
    public class MovingAction : TimetableAction
    {
        [XmlAttribute("TTName2")]
        public string? timetableName2;
        [XmlAttribute("NodeID2")]
        public int nodeId2;
        [XmlAttribute("TemplID")]
        public int templateId;

        public MovingAction()
        {
        }
    }
}
