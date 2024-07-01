using System.Collections.Generic;
using System.Xml.Serialization;

namespace ConflictManagementService.Model.TMS
{
    [XmlRoot("TripMovementTemplateList")]
    public class MovementTemplateList
    {
        public MovementTemplateList()
        {
        }
        [XmlArray("MovementTemplates")]
        public List<MovementTemplate> movementTemplates = new();
    }
}
