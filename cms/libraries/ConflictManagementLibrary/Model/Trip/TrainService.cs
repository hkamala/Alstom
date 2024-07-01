using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConflictManagementLibrary.Model.Trip
{
    public class TrainService
    {
        public string SystemGuid { get; set; } = Guid.NewGuid().ToString();
        public int ScheduledDayCode { get; set; } 
        public string ScheduledPlanName { get; set; } = "";
        public int LineId { get; set; } 
        public int TrainTypeId { get; set; }
        public SortedDictionary<int, Trip> Trips { get; set; } = new SortedDictionary<int, Trip>();
        public DateTime StartTime { get; set; }
        public List<Trip> TripList = new List<Trip>();
        public string GetServiceInformation()
        {
            var t = this;
            var s = new StringBuilder();
            s.AppendLine("\n\nTrain Service");
            s.AppendLine("Code < " + t.ScheduledDayCode + "Name <" + t.ScheduledPlanName + "> Direction<" + t.ScheduledDayCode + ">");
            
            return s.ToString();
        }


    }
}
