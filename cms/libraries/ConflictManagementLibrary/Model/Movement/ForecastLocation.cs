using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConflictManagementLibrary.Model.Trip;
using Newtonsoft.Json;

namespace ConflictManagementLibrary.Model.Movement
{
    public class ForecastLocation
    {
        public string Description { get; set; } = null!;
        public DateTime ArrivalTimePlan { get; set; }
        public DateTime DepartureTimePlan { get; set; }
        public DateTime ArrivalTimeAdjusted { get; set; }
        public DateTime DepartureTimeAdjusted { get; set; }
        public DateTime ArrivalTimeActual { get; set; }
        public DateTime DepartureTimeActual { get; set; }

        public static ForecastLocation CreateInstance(TimedLocation theLocation)
        {
            return new ForecastLocation(theLocation);
        }
        private ForecastLocation(TimedLocation theLocation)
        {
            Description = theLocation.Description;
            ArrivalTimePlan = theLocation.ArrivalTimePlan;
            ArrivalTimeActual = theLocation.ArrivalTimeActual;
            ArrivalTimeAdjusted = theLocation.ArrivalTimeAdjusted;
            DepartureTimePlan = theLocation.DepartureTimePlan;
            DepartureTimeActual = theLocation.DepartureTimeActual;
            DepartureTimeAdjusted = theLocation.DepartureTimeAdjusted;
        }

        [JsonConstructor]
        public ForecastLocation()
        {
            
        }
    }
}
