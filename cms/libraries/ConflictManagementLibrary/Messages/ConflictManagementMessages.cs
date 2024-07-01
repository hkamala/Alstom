using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ConflictManagementLibrary.Model.Conflict;
using ConflictManagementLibrary.Model.Movement;
using ConflictManagementLibrary.Model.Trip;

namespace ConflictManagementLibrary.Messages
{
    public static class ConflictManagementMessages
    {
        private const string DateFormat = "yyyy-MM-dd HH:mm:ss.fff";

        [Serializable]
        public class InitializeClient : IMessageJson
        {
            public string ClassName
            {
                get => "InitializeClient";
            }
            public string MessageName
            {
                get => "Initialization Request Message";
            }
            public string MessageNumber
            {
                get => "1000";
            }
            public string MessageDescription
            {
                get => "This message is sent from TSUI upon the startup of the client for CMS to send all trips and conflicts to the TSUI client.";
            }

            private InitializeClient(bool AllTrips = true)
            {

            }

            public InitializeClient()
            {
                
            } 

            public static InitializeClient CreateInstance(bool AllTrips = true)
            {
                return new InitializeClient(AllTrips);
            }
        }
        [Serializable]
        public class ConflictResolutionStatus : IMessageJson
        {
            public string ClassName
            {
                get => "ConflictResolutionStatus";
            }
            public string MessageName
            {
                get => "Conflict Resolution Status Message";
            }
            public string MessageNumber
            {
                get => "1100";
            }
            public string MessageDescription
            {
                get => "This message is sent from CMS upon the startup of the service to report the status for TSUI.";
            }

            private bool conflictResolutionEnabled;
            public bool ConflictResolutionEnabled
            {
                get => this.conflictResolutionEnabled;
                set => this.conflictResolutionEnabled = value;
            }
            private ConflictResolutionStatus(bool conflictResolutionEnabled = false)
            {
                this.ConflictResolutionEnabled = conflictResolutionEnabled;
            }
            public ConflictResolutionStatus()
            {

            }
            public static ConflictResolutionStatus CreateInstance(bool conflictResolutionEnabled = false)
            {
                return new ConflictResolutionStatus();
            }
        }
        [Serializable]
        public class SendAllTrips : IMessageJson
        {
            public string ClassName
            {
                get => "SendAllTrips";
            }
            public string MessageName
            {
                get => "Send All Trips Message";
            }
            public string MessageNumber
            {
                get => "2000";
            }
            public string MessageDescription
            {
                get => "This message is sent from CMS to the TSUI client upon the startup that sends all trips and conflicts to the client.";
            }
            public SendAllTrips()
            {

            }
        }
        [Serializable]
        public class AddNewTrip : IMessageJson
        {
            private Trip? theTrip;
            public string ClassName
            {
                get => "AddTrip";
            }
            public string MessageName
            {
                get => "Add New Trip Message";
            }
            public string MessageNumber
            {
                get => "2001";
            }
            public string MessageDescription
            {
                get => "This message is sent from CMS to the TSUI client upon a new trip being added to the client.";
            }
            public Trip? TheTrip
            {
                get => this.theTrip;
                set => this.theTrip = value;
            }
            private AddNewTrip(Trip theTrip)
            {
                this.theTrip = theTrip;
            }
            public static AddNewTrip CreateInstance(Trip theTrip)
            {
                return new AddNewTrip(theTrip);
            }
            public AddNewTrip()
            {
                
            }
        }
        [Serializable]
        public class DeleteTrip : IMessageJson
        {
            private Trip? theTrip;
            public string ClassName
            {
                get => "DeleteTrip";
            }
            public string MessageName
            {
                get => "Delete Trip Message";
            }
            public string MessageNumber
            {
                get => "2002";
            }
            public string MessageDescription
            {
                get => "This message is sent from CMS to the TSUI client upon trip being deleted to the client.";
            }
            public virtual Trip? TheTrip
            {
                get => this.theTrip;
                set => this.theTrip = value;
            }
            private DeleteTrip(Trip theTrip)
            {
                this.theTrip = theTrip;
            }

            public static DeleteTrip CreateInstance(Trip theTrip)
            {
                return new DeleteTrip(theTrip);
            }
            public DeleteTrip()
            {

            }
        }
        [Serializable]
        public class UpdateTrip : IMessageJson
        {
            private Trip? theTrip;
            public string ClassName
            {
                get => "UpdateTrip";
            }
            public string MessageName
            {
                get => "Update Trip Message";
            }
            public string MessageNumber
            {
                get => "2003";
            }
            public string MessageDescription
            {
                get => "This message is sent from CMS to the TSUI client upon trip being updated to the client.";
            }
            public virtual Trip? TheTrip
            {
                get => this.theTrip;
                set => this.theTrip = value;
            }
            private UpdateTrip(Trip theTrip)
            {
                this.theTrip = theTrip;
            }
            public static UpdateTrip CreateInstance(Trip theTrip)
            {
                return new UpdateTrip(theTrip);
            }
            public UpdateTrip()
            {

            }
        }
        [Serializable]
        public class TripAllocated : IMessageJson
        {
            private Trip? theTrip;
            public string ClassName
            {
                get => "TripAllocated";
            }
            public string MessageName
            {
                get => "Trip Allocated Message";
            }
            public string MessageNumber
            {
                get => "2004";
            }
            public string MessageDescription
            {
                get => "This message is sent from CMS service to TSUI that shows the trip has been allocated to a train.";
            }
            public virtual Trip? TheTrip
            {
                get => this.theTrip;
                set => this.theTrip = value;
            }
            private TripAllocated(Trip theTrip)
            {
                this.theTrip = theTrip;
            }
            public static TripAllocated CreateInstance(Trip theTrip)
            {
                return new TripAllocated(theTrip);
            }
            public TripAllocated()
            {

            }

        }
        [Serializable]
        public class PublishForecast : IMessageJson
        {
            private Forecast? _theForecast;
            public string ClassName
            {
                get => "PublishForecast";
            }
            public string MessageName
            {
                get => "Publish Forecast Message";
            }
            public string MessageNumber
            {
                get => "2005";
            }
            public string MessageDescription
            {
                get => "This message is sent from CMS service to TSUI that publishes an update forecast to a trip.";
            }
            public virtual Forecast? TheForecast
            {
                get => this._theForecast;
                set => this._theForecast = value;
            }
            private PublishForecast(Forecast theForecast)
            {
                this._theForecast = theForecast;
            }
            public static PublishForecast CreateInstance(Forecast theForecast)
            {
                return new PublishForecast(theForecast);
            }
            public PublishForecast()
            {

            }
        }
        [Serializable]
        public class ResolutionAccept : IMessageJson
        {
            private Conflict? theConflict;

            public string ClassName
            {
                get => "ResolutionAccept";
            }

            public string MessageName
            {
                get => "Resolution Accept Message";
            }

            public string MessageNumber
            {
                get => "1001";
            }
            public string MessageDescription
            {
                get => "This message is sent from TSUI to CMS service upon conflict being accepted by the client.";
            }

            public virtual Conflict? MyConflict
            {
                get => this.theConflict;
                set => this.theConflict = value;
            }

            private ResolutionAccept(Conflict theConflict)
            {
                this.theConflict = theConflict;
            }

            public static ResolutionAccept CreateInstance(Conflict theConflict)
            {
                return new ResolutionAccept(theConflict);
            }

            public ResolutionAccept()
            {

            }
        }
        [Serializable]
        public class ResolutionReject : IMessageJson
        {
            private Conflict? theConflict;

            public string ClassName
            {
                get => "ResolutionReject";
            }
            public string MessageName
            {
                get => "Resolution Reject Message";
            }
            public string MessageNumber
            {
                get => "1002";
            }
            public string MessageDescription
            {
                get => "This message is sent from TSUI to CMS service upon conflict being rejected by the client.";
            }

            public virtual Conflict? MyConflict
            {
                get => this.theConflict;
                set => this.theConflict = value;
            }

            private ResolutionReject(Conflict theConflict)
            {
                this.theConflict = theConflict;
            }

            public static ResolutionReject CreateInstance(Conflict theConflict)
            {
                return new ResolutionReject(theConflict);
            }

            public ResolutionReject()
            {

            }
        }
        [Serializable]
        public class SendRoutePlanRequest : IMessageJson
        {
  

            private string? thePlatformPath;
            private string? thetripUid;
            private string? thestartTime;
            private string? thetimedLocationGuid;
            private string? platformFrom;
            private string? platformTo;

            public string ClassName
            {
                get => "SendRoutePlanRequest";
            }
            public string MessageName
            {
                get => "Send Route Plan Request Message";
            }
            public string MessageNumber
            {
                get => "1200";
            }
            public string MessageDescription
            {
                get => "This message is sent from TSUI to CMS service to request a portion of the route plan be sent to ROS.";
            }

            public string? MyPlatformPath
            {
                get => this.thePlatformPath;
                set => this.thePlatformPath = value;
            }
            public string? TripUid
            {
                get => this.thetripUid;
                set => this.thetripUid = value;
            }

            public string? StartTime
            {
                get => this.thestartTime;
                set => this.thestartTime = value;
            }

            public string? TimedLocationGuid
            {
                get => this.thetimedLocationGuid;
                set => this.thetimedLocationGuid = value;
            }
            public string? PlatformFrom
            {
                get => this.platformFrom;
                set => this.platformFrom = value;
            }
            public string? PlatformTo
            {
                get => this.platformTo;
                set => this.platformTo = value;
            }

            private SendRoutePlanRequest(string? platformPath, string? tripUid, string? startTime, string? timedLocationGuid, string? platformFrom, string? platformTo)
            {
                TripUid = tripUid;
                StartTime = startTime;
                TimedLocationGuid = timedLocationGuid;
                MyPlatformPath = platformPath;
                PlatformFrom = platformFrom;
                PlatformTo = platformTo;
            }

            public static SendRoutePlanRequest CreateInstance(string? platformPath, string? tripUid, string? startTime, string? timedLocationGuid, string? platformFrom, string? platformTo)
            {
                return new SendRoutePlanRequest(platformPath,tripUid,startTime,timedLocationGuid, platformFrom, platformTo);
            }

            public SendRoutePlanRequest()
            {

            }
        }
        [Serializable]
        public class OperatorDeleteTrip : IMessageJson
        {

            public string ClassName
            {
                get => "OperatorDeleteTrip";
            }
            public string MessageName
            {
                get => "Operator Delete Trip Message";
            }
            public string MessageNumber
            {
                get => "1003";
            }
            public string MessageDescription
            {
                get => "This message is sent from TSUI to CMS service to delete a trip by the client.";
            }

            private string? theTripUid = null!;
            private string? theStartTime = null!;

            public string? MyTripUid
            {
                get => this.theTripUid;
                set => this.theTripUid = value;
            }
            public string? MyStartTime
            {
                get => this.theStartTime;
                set => this.theStartTime = value;
            }

            private OperatorDeleteTrip(string tripUid = "")
            {

            }

            public static OperatorDeleteTrip CreateInstance()
            {
                return new OperatorDeleteTrip();
            }

            public OperatorDeleteTrip()
            {

            }
        }
        [Serializable]
        public class OperatorConflictManagementControl : IMessageJson
        {
            public bool TurnOff { get; set; }
            public string ClassName
            {
                get => "OperatorConflictManagementControl";
            }
            public string MessageName
            { 
                get => "Operator Conflict Management Control Message";
            }
            public string MessageNumber
            {
                get => "1004";
            }
            public string MessageDescription
            {
                get => "This message is sent from TSUI to CMS service to control the conflict management automatic resolution by the client.";
            }
            private OperatorConflictManagementControl(bool turnOff = false)
            {
                TurnOff = turnOff;
            }
            public static OperatorConflictManagementControl CreateInstance(bool turnOff = false)
            {
                return new OperatorConflictManagementControl(turnOff);
            }

            public OperatorConflictManagementControl()
            {

            }
        }
        [Serializable]
        public class CmsEventMessage : IMessageJson
        {
            public string ClassName
            {
                get => "CmsEventMessage";
            }

            public string MessageName
            {
                get => "Event Message from CMS Message";
            }
            public string MessageNumber
            {
                get => "1005";
            }
            public string MessageDescription
            {
                get => "This message is sent from CMS to TSUI to notify the operator about a CMS event.";
            }
            public static CmsEventMessage CreateInstance(EventMessage thEventMessage)
            {
                return new CmsEventMessage(thEventMessage);
            }

            private EventMessage? _theEventMessage;
            public EventMessage? MyEventMessage
            {
                get => this._theEventMessage;
                set => this._theEventMessage = value;
            }

            private CmsEventMessage(EventMessage theEventMessage)
            {
                MyEventMessage = theEventMessage;
            }
            [JsonConstructor]
            public CmsEventMessage()
            {

            }
        }
        [Serializable]
        public class SerializeTrip : IMessageJson
        {
            public string? theTripCode { get; set; }
            public string? theTripUid { get; set; }

            public string ClassName
            {
                get => "SerializeTrip";
            }
            public string MessageName
            {
                get => "Serialize Trip Message";
            }
            public string MessageNumber
            {
                get => "1006";
            }
            public string MessageDescription
            {
                get => "This message is sent from TSUI to CMS to serialize a trip for debugging.";
            }

            private SerializeTrip(string? tripCode, string? tripUid )
            {
                theTripCode = tripCode;
                theTripUid = tripUid;
            }

            public SerializeTrip()
            {

            }

            public static SerializeTrip CreateInstance(string? tripCode, string? tripUid)
            {
                return new SerializeTrip(tripCode, tripUid);
            }
        }
    }
}
