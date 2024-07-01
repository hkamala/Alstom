using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConflictManagementLibrary.Logging;
using ConflictManagementLibrary.Model.Trip;
using ConflictManagementLibrary.Network;
using Newtonsoft.Json;

namespace ConflictManagementLibrary.Model.Reservation
{
    [Serializable]
    public class Reservation
    {
        public string MyGuid = DateTime.Now.ToString("yyyyMMddHHmmssffff");//Guid.NewGuid().ToString();
        public string? MyStationName { get; set; }
        public Station? MyStation { get; set; }
        public Node? MyNode { get; set; }
        public string? MyNodeNumber { get; set; }
        public Path? MyPath { get; set; }
        [JsonIgnore] public Link? MyLink { get; set; }
        public string? MyLinkReferenceUid { get; set; }
        public string? MyEdgeName   { get; set; }
        public string? MyEdgeUid { get; set; }
        public string? MyEdgeKilometerValue { get; set; }
        public TimedLocation? MyTimedLocation { get; set; }
        public TimedLocation? MyNextTimedLocation { get; set; }
        public DateTime? TimeBegin { get; set; } = DateTime.Now;
        public DateTime? TimeEnd { get; set; } = DateTime.Now;
        public TimeSpan? TotalReservationTime { get; set; }
        public int? MyTripId { get; set; }
        public string? MyTripCode { get; set; }
        public string? MyTripStartTime { get; set; }
        public string? TotalTimeInSeconds { get; set; }
        public string? MyTripGuid { get; set; }
        public bool HasBeenReleased { get; set; }
        [JsonIgnore] public int? MyMetersPerSecond { get; set; }
        [JsonIgnore] public bool HasBeenUpdated { get; set; }
        [JsonIgnore] public string? ScheduledPlanId { get; set; }
        [JsonIgnore] public string? ScheduledPlanName { get; set; }
        [JsonIgnore] public string? ScheduledPlanDayCode { get; set; }

        public Reservation(string theStation, Node theNode, Path? thePath, Link theLink, TimedLocation theTimedLocation, TimedLocation theNextTimedLocation, Trip.Trip theTrip)
        {
            MyStationName = theStation;
            MyNode = theNode;
            MyPath = thePath;
            MyLink = theLink;
            MyTimedLocation = theTimedLocation;
            MyNextTimedLocation = theNextTimedLocation;
            ScheduledPlanId = theTrip.ScheduledPlanId;
            ScheduledPlanName = theTrip.ScheduledPlanName;

            if (MyTimedLocation != null)
            {
                TimeBegin = MyTimedLocation.ArrivalTimeActual;
            }

            if (MyTimedLocation != null) TimeEnd = MyTimedLocation.DepartureTimeActual;

            if (MyLink != null) TimeEnd = MyNextTimedLocation.ArrivalTimeActual;
            TotalReservationTime = TimeEnd - TimeBegin;
            TotalTimeInSeconds = TotalReservationTime?.TotalSeconds.ToString("00#");
        }

        public Reservation(Link theLink,  DateTime beginTime, DateTime endTime, TimedLocation theTimedLocation, TimedLocation? theNextTimedLocation, string theStation, string theNode, Trip.Trip theTrip)
        {
            MyStationName = theStation;
            MyNodeNumber = theNode;
            TimeBegin = beginTime;
            TimeEnd = endTime;
            TotalReservationTime = TimeEnd - TimeBegin;
            MyLink = theLink;
            MyLinkReferenceUid = MyLink.MyReferenceNumber.ToString();
            MyEdgeName = MyLink.EdgeName;
            MyEdgeUid = MyLink.EdgeId;
            MyEdgeKilometerValue = MyLink.MyEdgeAssociation?.MyKiloMeterValue;
            MyTimedLocation = theTimedLocation;
            MyNextTimedLocation = theNextTimedLocation;
            ScheduledPlanId = theTrip.ScheduledPlanId;
            ScheduledPlanName = theTrip.ScheduledPlanName;
        }
        public Reservation(string theGuid, Link theLink, DateTime beginTime, DateTime endTime, TimedLocation theTimedLocation, TimedLocation? theNextTimedLocation, string theStation, string theNode)
        {
            MyStationName = theStation;
            MyNodeNumber = theNode;
            TimeBegin = beginTime;
            TimeEnd = endTime;
            TotalReservationTime = TimeEnd - TimeBegin;
            MyLink = theLink;
            MyLinkReferenceUid = MyLink.MyReferenceNumber.ToString();
            MyEdgeName = MyLink.EdgeName;
            MyEdgeUid = MyLink.EdgeId;
            MyEdgeKilometerValue = MyLink.MyEdgeAssociation?.MyKiloMeterValue;
            MyTimedLocation = theTimedLocation;
            MyNextTimedLocation = theNextTimedLocation;
        }

        [JsonConstructor]
        public Reservation()
        {
            
        }
        private void CalculateReservationTime()
        {

        }
        private DateTime GetDepartureTime(TimedLocation departingPlatform)
        {
            return departingPlatform.DepartureTimeActual;
        }
        private DateTime GetArrivalTime(TimedLocation arrivingPlatform)
        {
            return arrivingPlatform.ArrivalTimeActual;
        }

    }
}
