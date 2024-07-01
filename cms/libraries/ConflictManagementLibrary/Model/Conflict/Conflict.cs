using System;
using System.Text;
using ConflictManagementLibrary.Helpers;
using ConflictManagementLibrary.Model.Trip;
using ConflictManagementLibrary.Network;
using Newtonsoft.Json;

namespace ConflictManagementLibrary.Model.Conflict
{
    [Serializable]
    public class Conflict
    {
        #region Declarations

        public string MyGuid = Guid.NewGuid().ToString();
        public string MyDescription { get; set; } = null!;
        public ConflictEntity MyEntity { get; set; } = null!;
        public ConflictType.TypeOfConflict MyTypeOfConflict { get; set; }
        public ConflictResolution MyResolution { get; set; } = null!;
        public DateTime MyDateTime { get; set; }
        public DateTime MyDateTimeCreated { get; set; }
        public string MyLocation { get; set; } = null!;
        public string MyLocationDetail { get; set; } = null!;
        public SubtypeOfConflict MySubtypeOfConflict { get; set; } = null!;
        public string MyTripUid { get; set; } = null!;
        public string MyTripStartTime { get; set; } = null!;
        public bool IsRejected { get; set; } = false;
        public bool IsAccepted { get; set; } = false;
        public bool IsResolvable { get; set; }
        public bool IsConflicting { get; set; } = true;
        public bool IsTrainConflict { get; set; } = false;
        public bool IsReservationConflict { get; set; } = false;
        public bool IsPossessionConflict { get; set; } = false;
        public bool IsLengthConflict { get; set; } = false;
        public bool IsStationNeckConflict { get; set; } = false;
        public int MyConflictLength { get; set; } = 0;
        public bool IsInfrastructureConflict { get; set; } = false;
        public string? MyDeviceName { get; set; }
        public string? MyDeviceUid { get; set; }
        public string? MyDeviceKilometerValue { get; set; }
        public string? MyPossessionUid { get; set; }
        public Reservation.Reservation? MyReservation = new Reservation.Reservation();
        public Reservation.Reservation ConflictingReservation = new Reservation.Reservation();
        public string MyPlanLocation { get; set; }
        #endregion

        #region Constructors
        public static Conflict CreateInstance(string theLocation, ConflictEntity theEntity, ConflictType.TypeOfConflict theTypeOfConflict, int theSubTypeIndex, ConflictResolution theResolution, DateTime theDateTime)
        {
            return new Conflict(theLocation, theEntity, theTypeOfConflict, theSubTypeIndex, theResolution, theDateTime);
        }
        private Conflict(string theLocation, ConflictEntity theEntity, ConflictType.TypeOfConflict theTypeOfConflict, int theSubTypeIndex, ConflictResolution theResolution, DateTime theDateTime)
        {
            MyDateTimeCreated = DateTime.Now;
            MyEntity = theEntity;
            MyTypeOfConflict = theTypeOfConflict;
            MyResolution = theResolution;
            MyDateTime = theDateTime;
            MyLocation = theLocation;
            MySubtypeOfConflict = ConflictSubtypes.GetSubtype(theSubTypeIndex);
            IsResolvable = GetIsConflictResolvable();
            IsConflicting = GetIsConflicting();
            IsTrainConflict = true;
        }
        private Conflict(Station theStation, Reservation.Reservation? theReservation, Reservation.Reservation theConflictingReservation, Trip.Trip theTrip,Path route, Path routeConflicting, bool holdTrain = true, bool isStationNeckConflict = true)
        {
            IsReservationConflict = true;
            IsStationNeckConflict = true;
            ConflictingReservation = null;
            MyDateTimeCreated = DateTime.Now;
            this.MyReservation = theReservation;
            this.ConflictingReservation = theConflictingReservation;
            var platformName = theReservation.MyTimedLocation!.Description;
            var stationName = theStation.StationName; // theReservation.MyTimedLocation!.MyStationName;
            var referenceNumber = theReservation.MyLink?.MyReferenceNumber;
            var edgeName = theReservation.MyEdgeName;
            //this.MyLocation = deviceName + " (" + deviceKilometerValue + ")" + " @ " + stationName;
            this.MyLocation = "At Station " + stationName + " between route " + route.MyRouteName + " and route " + routeConflicting.MyRouteName + ")";

            if (theReservation != null)
            {
                this.MyDateTime = theReservation.MyTimedLocation.ArrivalTimeActual;
                this.MyDateTime = theReservation.TimeEnd.Value;

            }
            this.IsConflicting = true;
            this.IsResolvable = true;
            this.MyTripUid = theTrip.TripId.ToString();
            this.MyTripStartTime = theTrip.StartTime!;
            this.MyResolution = ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.HoldOtherTrain);
            if (holdTrain) this.MyResolution = ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.HoldThisTrain);

            this.MyEntity = new ConflictEntity
            {
                MyDescription = "Conflicting Station Neck with Trip <" + theConflictingReservation.MyTripCode + ">",
                MyEntityType = ConflictEntity.EntityType.Train
            };
            this.MyDescription = theTrip.TripCode + " is station neck conflicting with Trip ID " + theConflictingReservation.MyTripCode;
            this.MyTypeOfConflict = ConflictType.TypeOfConflict.Train;

            var thisTrip = GetTrip(theReservation.MyTripCode!);
            var otherTrip = GetTrip(theConflictingReservation.MyTripCode!);
            if (thisTrip != null && otherTrip != null)
            {
                if (thisTrip.Direction != otherTrip.Direction)
                {
                    if (theReservation.MyLink != null)
                    {
                        this.MySubtypeOfConflict = ConflictSubtypes.GetSubtype(15);
                    }
                    if (theReservation.MyPath != null) this.MySubtypeOfConflict = ConflictSubtypes.GetSubtype(15);
                }
                else
                {
                    if (theReservation.MyLink != null)
                    {
                        this.MySubtypeOfConflict = ConflictSubtypes.GetSubtype(15);
                    }
                    if (theReservation.MyPath != null) this.MySubtypeOfConflict = ConflictSubtypes.GetSubtype(15);
                }
            }
            else
            {
                this.MySubtypeOfConflict = ConflictSubtypes.GetSubtype(15);
            }

            MyLocationDetail = GetConflictLocationDetails(theStation, theReservation,false,true);
        }
        internal static Conflict CreateInstanceStationNeckConflict(Station theStation, Reservation.Reservation? theReservation, Reservation.Reservation theConflictingReservation, Trip.Trip theTrip, Path route, Path routeConflicting, bool holdTrain = true, bool isStationNeckConflict = true)
        {
            return new Conflict(theStation, theReservation, theConflictingReservation, theTrip, route, routeConflicting, holdTrain, isStationNeckConflict);
        }
        private Conflict(Station theStation, Reservation.Reservation? theReservation, Reservation.Reservation theConflictingReservation, Trip.Trip theTrip, bool holdTrain = true)
        {
            IsReservationConflict = true;
            MyDateTimeCreated = DateTime.Now;
            this.MyReservation = theReservation;
            this.ConflictingReservation = theConflictingReservation;
            var platformName = theReservation.MyTimedLocation!.Description;
            var stationName = theReservation.MyTimedLocation!.MyStationName;
            var referenceNumber = theReservation.MyLink?.MyReferenceNumber;
            var edgeName = theReservation.MyEdgeName;
            this.MyLocation = stationName + "@" + platformName + " (" + referenceNumber + ") (" + edgeName + ")";

            if (theReservation != null)
            {
                this.MyDateTime = theReservation.MyTimedLocation.ArrivalTimeActual;
                this.MyDateTime = theReservation.TimeEnd.Value;

            }
            this.IsConflicting = true;
            this.IsResolvable = true;
            this.MyTripUid = theTrip.TripId.ToString();
            this.MyTripStartTime = theTrip.StartTime!;
            this.MyResolution = ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.HoldOtherTrain);
            if (holdTrain) this.MyResolution = ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.HoldThisTrain);

            this.MyEntity = new ConflictEntity
            {
                MyDescription = "Conflicting Reservation with Trip <" + theConflictingReservation.MyTripCode + ">",
                MyEntityType = ConflictEntity.EntityType.Train
            };
            this.MyDescription = theTrip.TripCode + " is conflicting with Trip ID " + theConflictingReservation.MyTripCode;
            this.MyTypeOfConflict = ConflictType.TypeOfConflict.Train;

            var thisTrip = GetTrip(theReservation.MyTripCode!);
            var otherTrip = GetTrip(theConflictingReservation.MyTripCode!);
            if (thisTrip != null && otherTrip != null)
            {
                if (thisTrip.Direction != otherTrip.Direction)
                {
                    if (theReservation.MyLink != null)
                    {
                        this.MySubtypeOfConflict = ConflictSubtypes.GetSubtype(theReservation.MyLink.MyPlatforms.Count > 0 ? 13 : 2);
                    }
                    if (theReservation.MyPath != null) this.MySubtypeOfConflict = ConflictSubtypes.GetSubtype(0);
                }
                else
                {
                    if (theReservation.MyLink != null)
                    {
                        this.MySubtypeOfConflict = ConflictSubtypes.GetSubtype(theReservation.MyLink.MyPlatforms.Count > 0 ? 14 : 3);
                    }
                    if (theReservation.MyPath != null) this.MySubtypeOfConflict = ConflictSubtypes.GetSubtype(1);
                }
            }
            else
            {
                this.MySubtypeOfConflict = ConflictSubtypes.GetSubtype(2);
            }

            MyLocationDetail = GetConflictLocationDetails(theStation, theReservation);
        }
        public static Conflict CreateInstanceFromReservationConflict(Station theStation, Reservation.Reservation theReservation, Reservation.Reservation theConflictingReservation, Trip.Trip theTrip, bool holdTrain = true)
        {
            return new Conflict(theStation, theReservation, theConflictingReservation, theTrip, holdTrain);
        }
        public static Conflict CreateInstanceFromLength(Trip.Trip theTrip, Platform thePlatform, TimedLocation theTimeLocation, string deviceKilometerValue)
        {
            return new Conflict(theTrip, thePlatform, theTimeLocation,deviceKilometerValue);
        }
        private Conflict(Trip.Trip theTrip, Platform thePlatform, TimedLocation theTimeLocation, string deviceKilometerValue)
        {
            MyDateTimeCreated = DateTime.Now;
            IsLengthConflict = true;
            IsInfrastructureConflict = true;
            ConflictingReservation = null;

            var platformName = thePlatform.MyName;
            var stationName = theTimeLocation.MyStationName;
            var edgeName = theTimeLocation.Position.ElementId;
            MyDeviceName = platformName;
            //this.MyLocation = stationName + "@" + platformName+ " (" + edgeName + ")"; 
            this.MyLocation = platformName + " (" + deviceKilometerValue + ")" + " @ " + stationName;

            this.IsConflicting = true;
            this.MyTripUid = theTrip.TripId.ToString();
            this.MyTripStartTime = theTrip.StartTime!;
            this.MyDateTime = theTimeLocation.ArrivalTimeActual;
            this.MyResolution = ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.Manual);
            this.MyEntity = new ConflictEntity
            {
                MyDescription = "Train Length <" + theTrip.Length + "> Conflicting With Platform <" + thePlatform.MyName + "> <" + thePlatform.MyDistanceInMeters +">",
                MyEntityType = ConflictEntity.EntityType.Track
            };
            this.MyDescription = "Train Length <" + theTrip.Length + "> Conflicting With Platform <" + thePlatform.MyName + "> <" + thePlatform.MyDistanceInMeters + ">";
            this.MyTypeOfConflict = ConflictType.TypeOfConflict.Track;
            this.MySubtypeOfConflict = ConflictSubtypes.GetSubtype(4);
            MyLocationDetail = platformName + " @ " + stationName + " <" + MySubtypeOfConflict.MyDescription + ">";

        }
        public static Conflict CreateInstanceFromInfrastructure(Trip.Trip theTrip, TimedLocation theTimeLocation, int indexSubType, ConflictEntity.EntityType theEntityType, ConflictType.TypeOfConflict theTypeOfConflict, string theConflictDescription, string deviceName, string deviceUid, string stationName, string deviceKilometerValue)
        {
            return new Conflict(theTrip, theTimeLocation, indexSubType, theEntityType, theTypeOfConflict, theConflictDescription, deviceName, deviceUid, stationName, deviceKilometerValue);
        }
        private Conflict(Trip.Trip theTrip, TimedLocation theTimeLocation, int indexSubType, ConflictEntity.EntityType theEntityType, ConflictType.TypeOfConflict theTypeOfConflict, string theConflictDescription, string deviceName, string deviceUid, string stationName, string deviceKilometerValue)
        {
            MyDateTimeCreated = DateTime.Now;
            IsInfrastructureConflict = true;
            IsResolvable = false;
            this.MyLocation = deviceName + " (" + deviceKilometerValue + ")" + " @ " + stationName ;
            this.IsConflicting = true;
            this.MyTripUid = theTrip.TripId.ToString();
            this.MyTripStartTime = theTrip.StartTime!;
            this.MyDateTime = theTimeLocation.ArrivalTimeActual;
            this.MyResolution = ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.Manual);
            this.MyEntity = new ConflictEntity
            {
                MyDescription = theConflictDescription, //"Train Length Conflicting With Platform <" + thePlatform.MyName + ">",
                MyEntityType = theEntityType,
                MyName = deviceName,
                MyUid = deviceUid,
                MySubTypeIndex = indexSubType
            };
            this.MyDescription = theConflictDescription; //"Train Length Conflicting With Platform <" + thePlatform.MyName + ">";
            this.MyTypeOfConflict = theTypeOfConflict;
            this.MySubtypeOfConflict = ConflictSubtypes.GetSubtype(indexSubType);
            MyLocationDetail = deviceName + " @ " + stationName + " <" + MySubtypeOfConflict.MyDescription +">";

        }
        private Conflict(Trip.Trip theTrip, TimedLocation theTimeLocation, int indexSubType, ConflictEntity.EntityType theEntityType, ConflictType.TypeOfConflict theTypeOfConflict, string theConflictDescription, string stationName, string thePlatformDescription)
        {
            MyDateTimeCreated = DateTime.Now;
            IsInfrastructureConflict = true;
            IsResolvable = false;
            ConflictingReservation = null;
            this.MyLocation = thePlatformDescription  + " @ " + stationName;
            this.IsConflicting = true;
            this.MyTripUid = theTrip.TripId.ToString();
            this.MyTripStartTime = theTrip.StartTime!;
            this.MyDateTime = theTimeLocation.ArrivalTimeActual;
            this.MyResolution = ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.Manual);
            this.MyEntity = new ConflictEntity
            {
                MyDescription = theConflictDescription, //"Train Length Conflicting With Platform <" + thePlatform.MyName + ">",
                MyEntityType = theEntityType,
                MyName = thePlatformDescription,
                MyUid = string.Empty,
                MySubTypeIndex = indexSubType
            };
            this.MyDescription = theConflictDescription; //"Train Length Conflicting With Platform <" + thePlatform.MyName + ">";
            this.MyTypeOfConflict = theTypeOfConflict;
            this.MySubtypeOfConflict = ConflictSubtypes.GetSubtype(indexSubType);
            MyLocationDetail = thePlatformDescription + " @ " + stationName + " <" + MySubtypeOfConflict.MyDescription + ">";

        }
        internal static Conflict CreateInstanceFromNoPlatform(Trip.Trip theTrip, TimedLocation theTimeLocation, int indexSubType, ConflictEntity.EntityType theEntityType, ConflictType.TypeOfConflict theTypeOfConflict, string theConflictDescription, string stationName, string thePlatformDescription)
        {
            return new Conflict(theTrip, theTimeLocation, indexSubType, theEntityType, theTypeOfConflict, theConflictDescription, stationName, thePlatformDescription);
        }

        [JsonConstructor]
        public Conflict()
        {

        }
        #endregion

        #region Functions
        private bool GetIsConflictResolvable()
        {
            if (MyResolution.MyTypeOfResolution == ConflictResolution.TypeOfResolution.Manual) return false;
            return true;
        }
        private bool GetIsConflicting()
        {
            if (MyDateTime < DateTime.Now) return false;
            return true;
        }
        private Trip.Trip GetTrip(string tripCode)
        {
            foreach (var trip in GlobalDeclarations.TripList)
            {
                if (trip.TripCode == tripCode) return trip;
            }
            return null!;
        }
        private string GetConflictLocationDetails(Station theStation, Reservation.Reservation theReservation, bool stationOnlyConflict = false, bool stationNeckConflict = false)
        {
            var info = new StringBuilder();

            if (theReservation.MyLink != null & !stationOnlyConflict & !stationNeckConflict)
            {
                if (theReservation.MyLink.MyPlatforms.Count > 0)
                {
                    info.Append("Platform <" + theReservation.MyTimedLocation.Description + ">");
                }
                else
                {
                    var nodeLeft = GlobalDeclarations.MyRailwayNetworkManager.FindNode(theReservation.MyLink.MyConnectionLeft);
                    var nodeRight = GlobalDeclarations.MyRailwayNetworkManager.FindNode(theReservation.MyLink.MyConnectionRight);

                    if (nodeLeft != null && nodeRight != null)
                    {
                        info.Append("Open Line Between <" + nodeLeft.MyStation.Abbreviation + " and " + nodeRight.MyStation.Abbreviation + ">");
                    }
                    else
                    {
                        info.Append("Open Line <" + theReservation.MyLink.MyReferenceNumber + ">"); //"<" + DateTime.Now.ToString("dd-MM-yy HH:mm:ss:.fff"));
                    }
                }
            }
            else
            {
                if (stationOnlyConflict)
                {
                    info.Append("Station <" + theStation.StationName + "><" + DateTime.Now.ToString("dd-MM-yy HH:mm:ss:.fff"));
                }
                else if (stationNeckConflict)
                    info.Append("Station Neck <" + theStation.StationName + "> <" + ConflictingReservation.MyTripCode +">");

            }
            return info.ToString();
        }
        #endregion
    }
}
