using System.Collections.Generic;
using System.Xml.Serialization;

namespace ConflictManagementService.Model.TMS
{
    public enum eLinkStatus
    {
        [XmlEnum("0")]
        notUsing,
        [XmlEnum("1")]
        waiting,
        [XmlEnum("2")]
        off,
        [XmlEnum("3")]
        initializing,
        [XmlEnum("4")]
        on
    };
    public enum eSpeedOrigin
    {
        [XmlEnum("0")]
        notUsing,
        [XmlEnum("1")]
        unknown,
        [XmlEnum("2")]
        train,
        [XmlEnum("3")]
        estimate,
    }

    public enum eDepotTrainRequestState
    {
        [XmlEnum("0")]
        defaultState,
        [XmlEnum("1")]
        requested,
        [XmlEnum("2")]
        obtainFailed,
        [XmlEnum("3")]
        timetableAllocationFailed,
        [XmlEnum("4")]
        success,
        [XmlEnum("5")]
        unmanaged,
        [XmlEnum("6")]
        requestedForPreparation
    }

    [XmlType("PlTr")]
    public class ATRPlainTrain
    {
        public enum eUpdateReason
        {
            [XmlEnum("0")]
            groupUpdate,
            [XmlEnum("1")]
            normalMove,
            [XmlEnum("2")]
            arriveToTimedObject,
            [XmlEnum("3")]
            departureFromTimedObject,
            [XmlEnum("4")]
            arriveAndDeparture,
            [XmlEnum("5")]
            mappedToSchedule,
            [XmlEnum("6")]
            userReform,
            [XmlEnum("7")]
            userCancel,
            [XmlEnum("8")]
            userAlarm,
            [XmlEnum("9")]
            userMove,
            [XmlEnum("10")]
            unmappedFromSchedule,
            [XmlEnum("11")]
            connectedToTrackedTrain,
            [XmlEnum("12")]
            linkStatusChanged,
            [XmlEnum("13")]
            trainStopped,
            [XmlEnum("14")]
            trainMoving,
            [XmlEnum("15")]
            propertyChanges,
            [XmlEnum("16")]
            clearPosition,
            [XmlEnum("17")]
            pathChanges,
            [XmlEnum("18")]
            connectedActiveCountChanged,
            [XmlEnum("19")]
            connectedToTrackedTrainAndMapped,
        };
        public ATRPlainTrain()
        {
        }
        public string? GUID => tmsPTI?.trainGUID;

        [XmlElement("TmsPTI")]
        public TmsPTI? tmsPTI;
        [XmlAttribute("Active")]
        public bool active;
        [XmlAttribute("dc")]
        public int templateDayCode;
        [XmlAttribute("ActiveC")]
        public int connectedActiveTrainsCount;
        [XmlAttribute("TeGUID")]
        public string? templateGUID;
        [XmlAttribute("Name")]
        public string? strName;
        [XmlAttribute("UIName")]
        public string? strUIName;
        [XmlAttribute("UdR")]
        public eUpdateReason updateReason;
        [XmlAttribute("TrN")]
        public string? strTripName;
        [XmlAttribute("TrNo")]
        public int tripNo;
        //[XmlAttribute("SSt")]
        //public eSchedulingState serviceState;
        [XmlAttribute("CN")]
        public int currentNodeID;
        [XmlAttribute("Ltc")]
        public int latencyFromCurrentNode;
        [XmlAttribute("NN")]
        public int nextNodeID;
        [XmlAttribute("ErP")]
        public int errorPlan;
        [XmlAttribute("ErR")]
        public int errorReg;
        [XmlAttribute("NR")]
        public string? isoNextRegDeparture;
        [XmlAttribute("NP")]
        public string? isoNextPlanDeparture;
        [XmlAttribute("RTT")]
        public int roundTime;
        [XmlAttribute("Spd")]
        public int internalSpeed;   // This is train speedclass
        [XmlAttribute("PCap")]
        public int passengerCapacity;
        [XmlAttribute("LD")]
        public int lastDwell;
        [XmlAttribute("Int")]
        public uint intervalSecs;
        [XmlAttribute("AID")]
        public uint currentTrackAreaID;

        [XmlAttribute("ALARM")]
        public bool alarm;
        [XmlAttribute("LDId")]
        public int lastDepartureID; // ToDo: Should be sent in future
        [XmlAttribute("LID")]
        public uint lineID; // ToDo: Should be sent in future
                            //[XmlElement("RInfo")]
                            //public ReformTrainInfo reformTrainInfo;
        [XmlAttribute("DrInf")]
        public string? driverInfo;
        [XmlAttribute("LnkSt")]
        public eLinkStatus linkStatus;
        [XmlAttribute("CSpd")]
        public double currentSpeed; // Actual moving speed of train (meters per second)
        [XmlAttribute("SpdO")]
        public eSpeedOrigin currentSpeedOrigin;
        [XmlAttribute("DTRS")]
        public eDepotTrainRequestState depotTrainRequestState;
    }
}
