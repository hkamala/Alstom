using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConflictManagementLibrary.Network;

namespace ConflictManagementLibrary.Model.Trip
{
    public class RoutePlanInfo
    {
        public int PlanUid;
        public int TripUid;
        public string TrainObid;
        public string TripOrigin;
        public string TripDestination;
        public string FromPlatform;
        public string ToPlatform;
        public Trip MyTrip;
        public TimedLocation CurrentTimedLocation;
        public Platform CurrentPlatform;
        public bool UseAlternatePath = false;
        public bool HasTripStartedFromOtherPoint = false;
        public bool RemoveActionPointsFromRoutePlan = false;
        private PlatformAlternate thePlatformAlternate;

        public PlatformAlternate MyPlatformAlternate
        {
            get
            {
                return this.thePlatformAlternate;
            }
            set
            {
                this.thePlatformAlternate = value;
            }
        }

        private RoutePlanInfo(int planUid, int tripUid, string trainObid, string tripOrigin, string tripDestination,
            TimedLocation theLocation, Trip theTrip, Platform theCurrentPlatform)
        {
            PlanUid = planUid;
            TripUid = tripUid;
            TrainObid = trainObid;
            TripOrigin = tripOrigin;
            TripDestination = tripDestination;
            FromPlatform = theLocation.MyMovementPlan.FromName;
            ToPlatform = theLocation.MyMovementPlan.ToName;
            MyTrip = theTrip;
            CurrentTimedLocation = theLocation;
            CurrentPlatform = theCurrentPlatform;
        }

        public static RoutePlanInfo CreateInstance(int planUid, int tripUid, string trainObid, string tripOrigin, string tripDestination, TimedLocation theLocation, Trip theTrip, Platform theCurrentPlatform)
        {
            return new RoutePlanInfo(planUid, tripUid, trainObid, tripOrigin, tripDestination, theLocation, theTrip, theCurrentPlatform);
        }

        public void ApplyAlternatePath(PlatformAlternate thePlatformAlternate)
        {
            UseAlternatePath = true;
            MyPlatformAlternate = thePlatformAlternate;
        }
    }
}
