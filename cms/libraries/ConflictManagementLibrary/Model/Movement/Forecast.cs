using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ConflictManagementLibrary.Model.Movement
{
    public class Forecast
    {
       public string? TripUid { get; set; } = null!;
       public string TripDayCode { get; set;} = null!;
       public bool UseLocalTime { get; set; }
       public List<ForecastLocation> ForecastLocations { get; set; } = null!;
       [JsonIgnore] public Trip.Trip TheTrip { get; } = null!;

       public static Forecast CreateInstance(Trip.Trip theTrip)
       {
           return new Forecast(theTrip);
       }
        private Forecast(Trip.Trip theTrip)
        {
            TheTrip = theTrip;
            UseLocalTime = true;
            TripUid = theTrip.TripCode;
            TripDayCode = theTrip.TripId.ToString();
            ForecastLocations = new List<ForecastLocation>();
            ApplyTimedLocations(theTrip);
        }

        private void ApplyTimedLocations(Trip.Trip theTrip)
        {
            foreach (var tl in theTrip.TimedLocations)
            {
                ForecastLocations.Add(ForecastLocation.CreateInstance(tl));
            }
        }
        [JsonConstructor]
        public Forecast()
        {
            
        }
    }
}
