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

        //[JsonIgnore]
        public bool IsLengthConflict { get; set; } = false;
        //[JsonIgnore]
        public bool IsInfrastructureConflict { get; set; } = false;
        public string? MyDeviceName { get; set; }
        public string? MyDeviceUid { get; set; }
        public string? MyDeviceKilometerValue { get; set; }

        public Reservation.Reservation? MyReservation = null!;
        public Reservation.Reservation ConflictingReservation = null!;

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

        private Conflict(Station theStation, Reservation.Reservation? theReservation,
            Reservation.Reservation theConflictingReservation, Trip.Trip theTrip, bool holdTrain = true)
        {
            MyDateTimeCreated = DateTime.Now;
            this.MyReservation = theReservation;
            this.ConflictingReservation = theConflictingReservation;
            this.MyLocation = theStation.StationName + "@" + theReservation.MyTimedLocation!.Description + " (" +
                              theReservation.MyLink?.MyReferenceNumber + ") (" + theReservation.MyEdgeName + ")";

            if (theReservation != null)
            {
                this.MyDateTime = theReservation.MyTimedLocation.ArrivalTimePlan;
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
        public static Conflict CreateInstanceFromLength(Trip.Trip theTrip, Platform thePlatform, TimedLocation theTimeLocation)
        {
            return new Conflict(theTrip, thePlatform, theTimeLocation);
        }
        private Conflict(Trip.Trip theTrip, Platform thePlatform, TimedLocation theTimeLocation)
        {
            MyDateTimeCreated = DateTime.Now;
            IsLengthConflict = true;
            this.MyLocation = thePlatform.StationAbbreviation + "@" + thePlatform.MyName + " (" + theTimeLocation.Position.ElementId + ")";
            this.IsConflicting = true;
            this.MyTripUid = theTrip.TripId.ToString();
            this.MyTripStartTime = theTrip.StartTime!;
            this.MyDateTime = theTimeLocation.ArrivalTimePlan;
            this.MyResolution = ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.Manual);
            this.MyEntity = new ConflictEntity
            {
                MyDescription = "Train Length Conflicting With Platform <" + thePlatform.MyName + ">",
                MyEntityType = ConflictEntity.EntityType.Track
            };
            this.MyDescription = "Train Length Conflicting With Platform <" + thePlatform.MyName + ">";
            this.MyTypeOfConflict = ConflictType.TypeOfConflict.Track;
            this.MySubtypeOfConflict = ConflictSubtypes.GetSubtype(4);

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
            this.MyLocation = deviceName + " @ " + stationName ;
            this.IsConflicting = true;
            this.MyTripUid = theTrip.TripId.ToString();
            this.MyTripStartTime = theTrip.StartTime!;
            this.MyDateTime = theTimeLocation.ArrivalTimePlan;
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
        private string GetConflictLocationDetails(Station theStation, Reservation.Reservation theReservation)
        {
            var info = new StringBuilder();

            if (theReservation.MyLink != null)
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
                        info.Append("Open Line <" + theReservation.MyLink.MyReferenceNumber + ">");
                    }

                }
            }
            else
            {
                info.Append("Station <" + theStation.StationName + ">");

            }

            return info.ToString();
        }
        #endregion
    }
}
