using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace ConflictManagementService.Model.TMS
{
    public class PlanDetails
    {
        [XmlAttribute("min")]
        public int minSecs;
        [XmlAttribute("plan")]
        public int planSecs;
        [XmlAttribute("id")]
        public int actionId;
        [XmlAttribute("trid")]
        public string? tripid; // Well on glue action this will be zero for now ???

        public PlanDetails()
        {
        }
        public PlanDetails(PlanDetails rhs)
        {
            minSecs = rhs.minSecs;
            planSecs = rhs.planSecs;
        }
    }

    [XmlType("TCD")]
    public class TripChange
    {
        [XmlElement("A")]
        public PlanDetails? arrive;
        [XmlElement("G")]
        public PlanDetails? glue;
        [XmlElement("D")]
        public PlanDetails? departure;

        public TripChange()
        {
        }
        public bool HasArriveActionForTrip(string tripID, int id)
        {
            if (arrive != null && arrive.tripid == tripID && arrive.actionId == id)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool HasSeparateArriveAction(out int id)
        {
            if (arrive != null && arrive.actionId != 0)
            {
                id = arrive.actionId;
                return true;
            }
            else
            {
                id = 0;
                return false;
            }
        }
        public TripChange(TripChange rhs)
        {
            if (rhs.arrive != null)
                arrive = new PlanDetails(rhs.arrive);

            if (rhs.glue != null)
                glue = new PlanDetails(rhs.glue);

            if (rhs.departure != null)
                departure = new PlanDetails(rhs.departure);
        }
    }

    public enum ScheduleGroupType
    {
        [XmlEnum("0")]
        unknown,
        [XmlEnum("1")]
        glueGroup,
        [XmlEnum("2")]
        tripGroup,
        [XmlEnum("3")]
        formationGroup
    }
    public enum ScheduleGroupTiming
    {
        [XmlEnum("0")] none,
        [XmlEnum("1")] keepAllAlways,
        [XmlEnum("2")] keepAllAlwaysOnTimeTable,
        [XmlEnum("3")] keepStartAlways,
        [XmlEnum("4")] keepStartOnTimetable,
        [XmlEnum("5")] releaseStartEarly,

    };
    public interface ITimetableEvent
    {
        bool IsPass();

        bool IsStopPatternChange();

        int DepartureSpeedClass { get; }
    }
    public class TrainScheduleEvent : ITimetableEvent
    {
        public TrainScheduleEvent()
        {
        }

        public TrainScheduleEvent(TrainScheduleEvent item)
        {
            eventFlags = item.eventFlags;
            nodeID = item.nodeID;
            arriveSecs = item.arriveSecs;
            departureSecs = item.departureSecs;
            diffToTimeTables = item.diffToTimeTables;
            departureSpeedClass = item.departureSpeedClass;
            lastActionID = item.lastActionID;
            //lastTripID = item.lastTripID;
            routeStatus = item.routeStatus;

            //TODO linePathFragment = item.linePathFragment;

            // Do deep copy
            if (item.tripChangeData != null)
                tripChangeData = new TripChange(item.tripChangeData);

        }
        public bool IsHistoryItem()
        {
            return (eventFlags & GRAPH_TRAIN_HISTORY_ITEM) == GRAPH_TRAIN_HISTORY_ITEM;
        }
        public bool IsTrainDiffValid()
        {
            return (eventFlags & GRAPH_TRAIN_DIFF_VALID) == GRAPH_TRAIN_DIFF_VALID;
        }
        public bool IsWaitingDeparture()
        {
            return (eventFlags & GRAPH_TRAIN_WAITING_DEPARTURE) == GRAPH_TRAIN_WAITING_DEPARTURE;
        }
        public void ClearIsWaitingDeparture()
        {
            eventFlags = (ushort)(eventFlags ^ GRAPH_TRAIN_WAITING_DEPARTURE);
        }
        public bool IsUserRegulated()
        {
            return (eventFlags & GRAPH_TRAIN_USER_REGULATED_DEPARTURE) == GRAPH_TRAIN_USER_REGULATED_DEPARTURE;
        }
        public bool RegulatorChangedSpeed()
        {
            return (eventFlags & GRAPH_TRAIN_REGULATOR_SPEED_CHANGE) == GRAPH_TRAIN_REGULATOR_SPEED_CHANGE;
        }
        public bool IsStopPatternChange()
        {
            return (eventFlags & GRAPH_TRAIN_REGULATOR_STOP_PATTERN_CHANGE) == GRAPH_TRAIN_REGULATOR_STOP_PATTERN_CHANGE;
        }
        public bool IsPass()
        {
            return (eventFlags & GRAPH_PASS_EVENT) == GRAPH_PASS_EVENT;
        }
        public int DepartureSpeedClass { get => departureSpeedClass; }
        public bool HasArriveActionForTrip(string tripID, int id)
        {
            if (tripChangeData != null)
            {
                return tripChangeData.HasArriveActionForTrip(tripID, id);
            }
            return false;
        }
        public int GetArriveActionID()
        {
            if (tripChangeData != null && tripChangeData.HasSeparateArriveAction(out int id))
            {
                return id;
            }
            else
            {
                return lastActionID;
            }
        }
        public void UpdateFlags(ushort mask, ushort flags)
        {
            ushort setFlags = (ushort)(mask & flags);

            eventFlags = (ushort)(eventFlags | setFlags);

            ushort clearFlags = (ushort)(mask & ~flags);

            eventFlags = (ushort)(eventFlags ^ clearFlags);

        }
        // We are sending > 250 MB in my env with 4 days open so now every attribute is basically a single character...
        [XmlAttribute("F")]
        public ushort eventFlags;
        [XmlAttribute("N")]
        public uint nodeID;
        [XmlAttribute("A")]
        public int arriveSecs;
        [XmlAttribute("D")]
        public int departureSecs;
        [XmlAttribute("T")]
        public int tailDepartureSecs;
        [XmlAttribute("E")] // (E)rror
        public int diffToTimeTables;
        [XmlAttribute("S")]
        public int departureSpeedClass;
        [XmlAttribute("Tck")]
        public string? ticketGUID;
        [XmlAttribute("Id")]
        public int lastActionID;
        [XmlAttribute("RS")]
        public int routeStatus; // 0=no route, 1=route, 2=fleeting route, 3=shunting route
        [XmlElement("TCD")]
        public TripChange? tripChangeData; // Null if no change..
                                           //[XmlElement("Path")]
                                           //TODO: public LinePathFragment linePathFragment;

        public const ushort GRAPH_TRAIN_ARRIVEVALID = 0x01;
        public const ushort GRAPH_TRAIN_DEPARTUREVALID = 0x02;
        public const ushort GRAPH_TRAIN_HISTORY_ITEM = 0x04;
        public const ushort GRAPH_TRAIN_RUNNING_LATE = 0x08;
        public const ushort GRAPH_TRAIN_DIFF_VALID = 0x10;
        public const ushort GRAPH_PASS_EVENT = 0x20;
        public const ushort GRAPH_PATH_IN_CONFLICT = 0x40;
        public const ushort GRAPH_TRAIN_WAITING_DEPARTURE = 0x80;
        public const ushort GRAPH_TRAIN_USER_REGULATED_DEPARTURE = 0x0100;
        public const ushort GRAPH_TRAIN_REGULATOR_SPEED_CHANGE = 0x0200;
        public const ushort GRAPH_TRAIN_REGULATOR_STOP_PATTERN_CHANGE = 0x0400; // should we have separate for user ??
        public const ushort GRAPH_TRAIN_ENDPOINT = 0x0800;


        //public Conflict relConf = null;
        //public Conflict stopConf = null;

        [XmlIgnore]
        public int MinimumGluedDwell
        {
            get
            {
                return tripChangeData != null ?
                    (tripChangeData.arrive != null ? tripChangeData.arrive.planSecs : 0) +
                    (tripChangeData.departure != null ? tripChangeData.departure.planSecs : 0) +
                    (tripChangeData.glue != null ? tripChangeData.glue.minSecs : 0)
                        : 0;
            }
        }
    }
    [XmlType("Grp")]
    public class TrainScheduleEventGroup // ScheduleItemGroupIF in tshcduler server 
    {
        [XmlAttribute("gt")]
        public ScheduleGroupType groupType;
        [XmlAttribute("id")]
        public string? id;
        [XmlAttribute("serid")]
        public string? primaryserviceID;
        [XmlAttribute("trid")]
        public string? primaryTripID;

        [XmlArray("Evts")]
        [XmlArrayItem("E", typeof(TrainScheduleEvent))]
        public List<TrainScheduleEvent>? events;
        //[XmlIgnore]
        //TODO public TSUITrip SharedTripData { get; set; }
    }
    /// <summary>
    /// ATRGraphTrain is a total mess, this comes from year 1970 :) when we had 2 sparate client programs, c++ ATRClient managing traingraph,
    /// and C# TrainlistClient (=now this TSUI) for everything else. Now still in 2019 traingraph and PAS uses ATRGraphTrain, 
    /// since we all the time release some XXXX and ther eis never time to make a single "train" based on combination of SccheduledTrain and ATRGraphTrain  
    /// </summary>
    [XmlType("Tg")]
    public class TrafficGraphTrainPath
    {
        public TrafficGraphTrainPath()
        {
        }
        public bool IsRegulatedTrain()
        {
            return (trainTypeFlags & GRAPH_TRAIN_REGULATED_TRAIN) == GRAPH_TRAIN_REGULATED_TRAIN;
        }
        public bool IsRealTrain()
        {
            return (trainTypeFlags & GRAPH_TRAIN_REAL_TRAIN) == GRAPH_TRAIN_REAL_TRAIN;
        }
        public bool IsVirtualTrain()
        {
            return (trainTypeFlags & GRAPH_TRAIN_VIRTUAL_TRAIN) == GRAPH_TRAIN_VIRTUAL_TRAIN;
        }
        public bool IsPlannedTrain()
        {
            return (trainTypeFlags & GRAPH_TRAIN_PLANNED_TRAIN) == GRAPH_TRAIN_PLANNED_TRAIN;
        }
        [XmlIgnore]
        public int DayCode
        {
            get { if (tmsPTI != null) return tmsPTI.serviceDayCode; else return 0; }
        }

        [XmlElement("TmsPTI")]
        public TmsPTI? tmsPTI;
        [XmlElement("Name")]
        public string? trainName;
        [XmlElement("ttF")]
        public ushort trainTypeFlags;
        [XmlElement("DayType")]
        public int DayType;
        [XmlArray("Grps")]
        public List<TrainScheduleEventGroup>? groups;

        [XmlArray("DutyIDS")]
        public int[]? relatedDutyIDs;
        [XmlIgnore]
        public DateTime origoTime; // This is used inside TSUI and contains origotime where 'Evts' seconds are calculated from

        public const ushort GRAPH_TRAIN_REGULATED_TRAIN = 0x01;
        public const ushort GRAPH_TRAIN_REAL_TRAIN = 0x02;
        public const ushort GRAPH_TRAIN_VIRTUAL_TRAIN = 0x04;
        public const ushort GRAPH_TRAIN_PLANNED_TRAIN = 0x08;
    }
}
