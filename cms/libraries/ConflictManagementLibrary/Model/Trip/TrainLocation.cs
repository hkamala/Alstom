using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConflictManagementLibrary.Model.Conflict;

namespace ConflictManagementLibrary.Model.Trip
{
    public class TrainLocation
    {
        public string NameLocation { get; set; }
        public string NameTrack { get; set; }
        public DateTime TimeCurrent { get; set; }
        public DateTime TimeArrival { get; set; }
        public DateTime TimeDeparture { get; set; }

        public string GetCurrentLocation() => $"{NameLocation} {NameTrack} {TimeCurrent:hh:mm}";
        public string GetNextLocation() => $"{NameLocation} {NameTrack} [{TimeArrival:hh:mm}/{TimeDeparture.AddMinutes(5):hh:mm}]";

    }
}
