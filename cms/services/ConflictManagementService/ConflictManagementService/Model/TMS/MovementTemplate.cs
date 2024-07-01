using System.Collections.Generic;
using System.Xml.Serialization;

namespace ConflictManagementService.Model.TMS
{
    [XmlType("MovementTemplate")]
    public class MovementTemplate
    {
        public MovementTemplate()
        {
        }

        [XmlAttribute("TrID")]
        public int movementTemplateID;
        [XmlAttribute("Desc")]
        public string? movementTemplateDescription;
        [XmlAttribute("FromStationID")]
        public int fromStationID;
        [XmlAttribute("FromStation")]
        public string? fromStationName;
        [XmlAttribute("FromID")]
        public int fromID;
        [XmlAttribute("From")]
        public string? fromName;
        [XmlAttribute("ToStationID")]
        public int toStationID;
        [XmlAttribute("ToStation")]
        public string? toStationName;
        [XmlAttribute("ToID")]
        public int toID;
        [XmlAttribute("To")]
        public string? toName;
    }
}
