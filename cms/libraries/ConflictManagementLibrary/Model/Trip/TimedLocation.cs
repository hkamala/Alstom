using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConflictManagementLibrary.Model.Movement;
using ConflictManagementLibrary.Model.Schedule;
using ConflictManagementLibrary.Network;
using Newtonsoft.Json;

namespace ConflictManagementLibrary.Model.Trip
{
    public class TimedLocation
    {
        #region Declarations
        public string SystemGuid { get; set; } = Guid.NewGuid().ToString();
        public int Id { get; set; }                 
        public int TripId { get; set; }  
        public string Description { get; set; } = null!;
        public DateTime ArrivalTimePlan { get; set; }
        public DateTime DepartureTimePlan { get; set; }
        public DateTime ArrivalTimeAdjusted { get; set; }
        public DateTime DepartureTimeAdjusted { get; set; }
        public DateTime ArrivalTimeActual { get; set; }
        public DateTime DepartureTimeActual { get; set; }
        public bool HasStopping { get; set; }
        public TimedLocationPosition Position { get; set; } = null!;
        public MovementPlan MyMovementPlan { get; set; } = null!;
        public bool RouteIsExecutedForTrip { get; set; }
        public bool RouteIsAlreadyAvailableForTrip { get; set; }
        public string MyStationName { get; set; }

        [JsonIgnore] public ConflictManagementLibrary.Network.Platform MyPlatform { get; set; }
        [JsonIgnore] public bool HasSentRouteMarker;
        [JsonIgnore] public bool HasArrivedToPlatform { get; set; }
        [JsonIgnore] public bool HasDepartedFromPlatform { get; set; }
        [JsonIgnore] public bool IsPastTriggerPoint { get; set; }
        [JsonIgnore] public RoutePlanInfo MyRoutePlan { get; set; }
        [JsonIgnore] public bool UseAlternatePlatform { get; set; }
        [JsonIgnore] public bool RemoveActionPointsFromRoutePlan { get; set; }
        [JsonIgnore] public double MyMetersPerSecondToNextLocation { get; set; }
        [JsonIgnore] public int MyTotalTripDistanceToNextLocation { get; set; }
        [JsonIgnore] public TimedLocation MyNextTimedLocation { get; set; }
        [JsonIgnore] public List<Link> MyReservationLinks { get; set; }

        #endregion

        #region Constructor
        [JsonConstructor]
        public TimedLocation()
        {
        }
        #endregion

        public void InitializeTime()
        {
            ArrivalTimeActual = ArrivalTimePlan;
            DepartureTimeActual = DepartureTimePlan;
            ArrivalTimeAdjusted = ArrivalTimePlan;
            DepartureTimeAdjusted = DepartureTimePlan;

        }
    }
    public class TimedLocationPosition
    {
        public string ElementId { get; set; }
        public string Offset { get; set; }
        public string AdditionalPos { get; set; }
        public string AdditionalName { get; set; }
        private TimedLocationPosition(string elementId, string offset, string additionalPos, string additionalName)
        {
            ElementId = elementId;
            Offset = offset;
            AdditionalPos = additionalPos;
            AdditionalName = additionalName;
        }
        public static TimedLocationPosition CreateInstance(string elementId, string offset, string additionalPos, string additionalName)
        {
            return new TimedLocationPosition(elementId, offset, additionalPos, additionalName);
        }
        public TimedLocationPosition()
        {
            
        }
    }
}
