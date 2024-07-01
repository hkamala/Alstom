using System;
using System.Collections.Generic;

namespace ConflictManagementLibrary.Model.Schedule
{
    public class ScheduledPlan
    {
        public Key Key { get; set; }
        public int ScheduledDayCode { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public int LineId { get; set; }
        public int TrainTypeId { get; set; }
        public Startsite StartSite { get; set; }
        public Endsite EndSite { get; set; }
        public Starttime StartTime { get; set; }
        public Endtime EndTime { get; set; }
        public SortedDictionary<int, PlannedTrip> Trips { get; set; }
        public bool IsSparePlan { get; set; }
        public ScheduledRoutePlan? ScheduledRoutePlan { get; set; }
    }

    public class Key
    {
        public int ScheduledDayCode { get; set; }
        public string ScheduledPlanName { get; set; }
        public int Item1 { get; set; }
        public string Item2 { get; set; }
    }

    public class Startsite
    {
        public string ElementId { get; set; }
        public int Offset { get; set; }
        public int AdditionalPos { get; set; }
        public string AdditionalName { get; set; }
    }

    public class Endsite
    {
        public string ElementId { get; set; }
        public int Offset { get; set; }
        public int AdditionalPos { get; set; }
        public string AdditionalName { get; set; }
    }

    public class Starttime
    {
        public DateTime DateTime { get; set; }
    }

    public class Endtime
    {
        public DateTime DateTime { get; set; }
    }


    public class PlannedTrip
    {
        public int ScheduledDayCode { get; set; }
        public string ScheduledPlanId { get; set; }
        public string ScheduledPlanName { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public int TripNumber { get; set; }
        public string TripCode { get; set; }
        public Startpos StartPos { get; set; }
        public Endpos EndPos { get; set; }
        public Starttime StartTime { get; set; }
        public Endtime EndTime { get; set; }
        public List<Timedlocation> TimedLocations { get; set; } = new List<Timedlocation>();
        public int DelaySeconds { get; set; }
        public int TrainLength { get; set; }
    }

    public class Startpos
    {
        public string ElementId { get; set; }
        public int Offset { get; set; }
        public int AdditionalPos { get; set; }
        public string AdditionalName { get; set; }
    }

    public class Endpos
    {
        public string ElementId { get; set; }
        public int Offset { get; set; }
        public int AdditionalPos { get; set; }
        public string AdditionalName { get; set; }
    }

    public class Timedlocation
    {
        public string Description { get; set; }
        public int Id { get; set; }                 // Optional ID of timed location, platform ID, action, ...
        public int TripId { get; set; }  // Optional trip ID
        public string TripName { get; set; }  // Optional trip name
        public Pos Pos { get; set; }
        public Arrival Arrival { get; set; }
        public Departure Departure { get; set; }
        public bool HasStopping { get; set; }
    }

    public class Pos
    {
        public string ElementId { get; set; }
        public int Offset { get; set; }
        public int AdditionalPos { get; set; }
        public string AdditionalName { get; set; }
    }

    public class Arrival
    {
        public DateTime DateTime { get; set; }
    }

    public class Departure
    {
        public DateTime DateTime { get; set; }
    }



}