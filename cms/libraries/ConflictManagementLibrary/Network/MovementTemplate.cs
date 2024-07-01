using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConflictManagementLibrary.Network
{
    public class MovementTemplate
    {
        public int movementTemplateID;
        public string? movementTemplateDescription;
        public int fromStationID;
        public string? fromStationName;
        public int fromID;
        public string? fromName;
        public int toStationID;
        public string? toStationName;
        public int toID;
        public string? toName;

        public MovementTemplate()
        {
        }
    }
}
