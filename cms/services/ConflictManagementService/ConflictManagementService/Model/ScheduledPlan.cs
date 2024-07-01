using ConflictManagementService.Model.TMS;
using System.Collections.Concurrent;
using System.IO;
using Newtonsoft.Json;
using ConflictManagementLibrary.Model.Schedule;
using ConflictManagementLibrary.Model.Trip;
using System.Collections.Generic;
using System;
using System.Linq;

namespace ConflictManagementService.Model;

using static E2KService.ServiceImp;

////////////////////////////////////////////////////////////////////////////////

public class ScheduledPlan
{
    public ScheduledPlanKey Key => new(ScheduledDayCode, Name);
    public int ScheduledDayCode => scheduledDayCode;
    public int Id => id;
    public string Name => name;
    public int LineId => lineId;
    public int TrainTypeId => trainTypeId; // These are not constant values, can be defined in TMS DB, information is in DataHandler's TrainTypes property

    public ElementPosition StartSite => startSite;
    public ElementPosition EndSite => endSite;
    public ActionTime StartTime => startTime;
    public ActionTime EndTime => endTime;
    public SortedDictionary<int, Trip> Trips => trips;
    public bool IsSparePlan => sparePlan;

    public ScheduledRoutePlan? ScheduledRoutePlan => Service.DataHandler.GetScheduledRoutePlan(Key);

    private int scheduledDayCode = 0;
    private readonly int id = 0;
    private readonly string name = "";
    private readonly int lineId = 0;
    private int trainTypeId = 0;
    private readonly ElementPosition startSite = new();
    private readonly ElementPosition endSite = new();
    private ActionTime startTime = new();
    private ActionTime endTime = new();
    private SortedDictionary<int, Trip> trips = new();
    private TmsPTI? tmsPTI = new();
    private List<TimetableAction> serviceActions = new();
    private readonly bool sparePlan = false;

    private bool updatedByRefresh = false;

    public static ScheduledPlanKey CreateKey(int scheduledDayCode, string scheduledPlanName)
    {
        return new ScheduledPlanKey(scheduledDayCode, scheduledPlanName);
    }

    public ScheduledPlan()
    {
    }

    public ScheduledPlan(int scheduledDayCode, int lineId, ServiceNodeDataItem service)
    {
        this.scheduledDayCode = scheduledDayCode;
        this.lineId = lineId;
        id = service.serviceID;
        name = service.name != null ? service.name : "";
        startSite = Service.DataHandler.GetElementPositionOfPlatform(service.startSite);
        endSite = Service.DataHandler.GetElementPositionOfPlatform(service.endSite);

        //TODO: How about other service states, how do they affect? Should some state prevent scheduled plan creation?
        sparePlan = service.serviceStatus == SchedulingState.plannedSpare;

        updatedByRefresh = true;

    }
    public void SerializeRoutePlan()
    {

        var str = JsonConvert.SerializeObject(ScheduledRoutePlan);
        var filename = $"RoutePlan_{DateTime.Now:MMddyyyyhhmmssfff}.json";
        var curDir = Environment.CurrentDirectory;
        const string folder = "Data";
        if (!Directory.Exists(Path.Combine(curDir, folder)))
        {
            Directory.CreateDirectory(Path.Combine(curDir, folder));
        }

        var fullpath = Path.Combine(curDir, folder, filename);
        File.WriteAllText(fullpath, str);


    }



    public bool Merge(ScheduledDay scheduledDay, ServiceItem? serviceItem)
    {
        // ServiceItem's "serviceID" is GUID! While tmsPTI's "serviceGUID" is serviceId integer as string, or empty

        int serviceId = 0;
        var serviceGUID = serviceItem?.tmsPTI?.serviceGUID;
        if (serviceGUID != null && serviceGUID != "")
            serviceId = int.Parse(serviceGUID);

        if (scheduledDay != null && serviceItem != null && serviceId == id && serviceItem.name == name)
        {
            startTime = new(scheduledDay.AsDateString(), 0, 0, serviceItem.plannedStartSecs);
            endTime = new(scheduledDay.AsDateString(), 0, 0, serviceItem.plannedEndSecs);

            tmsPTI = serviceItem.tmsPTI;

            if (tmsPTI != null)
                trainTypeId = tmsPTI.trainTypeID;

            if (serviceItem.serviceActions != null)
                serviceActions = serviceItem.serviceActions.ToList();

            return startTime.IsValid() && endTime.IsValid();
        }

        return false;
    }

    public Trip Add(ScheduledDay scheduledDay, TripItem tripItem)
    {
        if (scheduledDay != null && tripItem != null)
        {
            // Get trip code of this trip
            string tripCode = "";
            int tripNumber = 0;
            foreach (var serviceAction in serviceActions)
            {
                var rta = serviceAction as RunTripAction;
                if (rta != null)
                {
                    if (tripItem.tripID == rta.tripID)
                    {
                        tripCode = rta.tripCode != null ? rta.tripCode : "";
                        tripNumber = rta.tripNo;
                        break;
                    }
                }
            }

            ActionTime tripStartTime = new(scheduledDay.AsDateString(), 0, 0, tripItem.plannedStartSecs);
            ActionTime tripEndTime = new(scheduledDay.AsDateString(), 0, 0, tripItem.plannedEndSecs);

            ElementPosition tripStartPos = new ElementPosition();
            ElementPosition tripEndPos = new ElementPosition();

            List<TimedLocation> timedLocations = new();

            ActionTime tripActionTime = new ActionTime(tripStartTime);

            bool isSpareTrip = tripItem.tripState == SchedulingState.plannedSpare;

            if (tripItem.tripActions != null)
            {
                foreach (var action in tripItem.tripActions)
                {
                    if (action != null)
                    {
                        if (!tripStartPos.IsValid())
                            tripStartPos = Service.DataHandler.GetElementPositionOfPlatform(action.timetableName);

                        tripEndPos = Service.DataHandler.GetElementPositionOfPlatform(action.timetableName);

                        if (action is MovingAction)
                        {
                            var a = action as MovingAction;
                            tripActionTime += new TimeSpan(0, 0, 0, action.plannedSecs);
                            tripEndPos = Service.DataHandler.GetElementPositionOfPlatform(a?.timetableName2);
                        }
                        if (action is StopAction || action is PassAction)
                        {
                            var id = action.actionId;   // TMS forecast functions seem to use action ID when searching for scheduled location, or if it fails, the platform schedule name

                            ElementPosition pos = Service.DataHandler.GetElementPositionOfPlatform(action.timetableName);

                            ActionTime arrival = new ActionTime(tripActionTime);
                            tripActionTime += new TimeSpan(0, 0, 0, action.plannedSecs);
                            ActionTime departure = new ActionTime(tripActionTime);

                            var timedLocation = new TimedLocation(pos.AdditionalName, pos, arrival, departure, action is StopAction) { Id = id, TripId = tripItem.tripID };

                            if (timedLocation.IsValid())
                            {
                                timedLocations.Add(timedLocation);
                            }
                        }
                    }
                }
            }

            // If trip is an update to existing trip, replace the existing one with new one
            foreach (var existingTrip in this.trips.Values)
            {
                if (existingTrip.Id == tripItem.tripID && existingTrip.TripCode == tripCode)
                {
                    tripNumber = existingTrip.TripNumber;
                    break;
                }
            }

            if (tripNumber == 0)
                tripNumber = this.trips.Count + 1;  // MockTrip numbers start from 1
            string name = tripItem.name != null ? tripItem.name : "";

            Trip trip = new(ScheduledDayCode, Id, Name, tripItem.tripID, name, tripNumber, tripCode, tripStartPos, tripEndPos, tripStartTime, tripEndTime, timedLocations, isSpareTrip);

            if (trip.IsValid())
            {
                if (this.trips.ContainsKey(tripNumber))
                    this.trips[tripNumber] = trip;
                else
                    this.trips.Add(tripNumber, trip);
            }

            return trip;
        }

        return new Trip();
    }

    public void DeleteTrips()
    {
        this.trips.Clear();
    }

    public bool IsValid()
    {
        return ScheduledDayCode != 0 && Id != 0 && Name != "" && startTime.IsValid() && endTime.IsValid() && this.trips.Count > 0;
    }

    public Trip? GetTripByTripId(int tripId)
    {
        foreach (var tripItem in this.trips)
        {
            if (tripItem.Value.Id == tripId)
                return tripItem.Value;
        }

        return null;
    }

    public bool HasTripWithTripId(int tripId)
    {
        return GetTripNumberByTripId(tripId) > 0;
    }

    public int GetTripNumberByTripId(int tripId)
    {
        foreach (var tripItem in this.trips)
        {
            if (tripItem.Value.Id == tripId)
                return tripItem.Key;
        }
        return 0;
    }

    public int GetTripIdByTripCode(string tripCode)
    {
        foreach (var trip in this.trips.Values)
        {
            if (trip.TripCode == tripCode)
                return trip.Id;
        }
        return 0;
    }

    public override string ToString()
    {
        string s = string.Format($"scheduledDayCode={ScheduledDayCode} id='{Id}' name='{Name}' startSite='{StartSite}' endSite='{EndSite}' Trips:");
        foreach (var trip in this.trips.Values)
            s += " " + trip;
        return s;
    }

    public bool IsUpdatedByRefresh()
    {
        return updatedByRefresh;
    }

    public void ClearUpdatedByRefresh()
    {
        updatedByRefresh = false;
    }
}

////////////////////////////////////////////////////////////////////////////////
// Collections (concurrent ones for automatic thread safety)

public class ScheduledPlanKey : Tuple<int, string>, IEquatable<ScheduledPlanKey?>
{
    public int ScheduledDayCode => Item1;
    public string ScheduledPlanName => Item2;

    public ScheduledPlanKey(int scheduledDayCode, string scheduledPlanName) : base(scheduledDayCode, scheduledPlanName)
    {
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ScheduledPlanKey);
    }

    public bool Equals(ScheduledPlanKey? other)
    {
        return other is not null &&
               base.Equals(other) &&
               ScheduledDayCode == other.ScheduledDayCode &&
               ScheduledPlanName == other.ScheduledPlanName;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), ScheduledDayCode, ScheduledPlanName);
    }

    public static bool operator ==(ScheduledPlanKey? left, ScheduledPlanKey? right)
    {
        return EqualityComparer<ScheduledPlanKey>.Default.Equals(left, right);
    }

    public static bool operator !=(ScheduledPlanKey? left, ScheduledPlanKey? right)
    {
        return !(left == right);
    }
}

public class ScheduledPlans : ConcurrentDictionary<ScheduledPlanKey, ScheduledPlan>
{
    public ScheduledPlans(int concurrencyLevel, int capacity) : base(concurrencyLevel, capacity)
    {
    }
}