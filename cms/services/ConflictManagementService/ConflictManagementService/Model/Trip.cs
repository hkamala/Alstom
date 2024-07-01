using System.Collections.Concurrent;
using System.Collections.Generic;
using static E2KService.ServiceImp;

namespace ConflictManagementService.Model;

////////////////////////////////////////////////////////////////////////////////
public class Trip
{
    public int ScheduledDayCode => scheduledDayCode;
    public int ScheduledPlanId => scheduledPlanId;
    public string ScheduledPlanName => scheduledPlanName;
    public int Id => id;
    public string Name => name;
    public int TripNumber => tripNumber;
    public string TripCode => tripCode;
    public ElementPosition StartPos => startPos;
    public ElementPosition EndPos => endPos;
    public ActionTime StartTime => startTime;
    public ActionTime EndTime => endTime;
    public List<TimedLocation> TimedLocations => timedLocations;
    public bool IsSpareTrip => isSpareTrip;

    public int DelaySeconds => GetDelaySeconds();
    public int TrainLength => GetTrainLength();

    private int scheduledDayCode = 0;
    private int scheduledPlanId = 0;
    private string scheduledPlanName = "";
    private readonly int id = 0;
    private readonly string name = "";
    private readonly int tripNumber = 0;
    private readonly ActionTime startTime = new();
    private readonly ActionTime endTime = new();
    private readonly string tripCode = "";
    private readonly ElementPosition startPos = new();
    private readonly ElementPosition endPos = new();
    private readonly List<TimedLocation> timedLocations = new();
    private readonly bool isSpareTrip = false;

    public Trip()
    {
    }

    public Trip(int scheduledDayCode, int scheduledPlanId, string scheduledPlanName, int id, string name, int tripNumber, string tripCode, ElementPosition startPos, ElementPosition endPos, ActionTime startTime, ActionTime endTime, List<TimedLocation> timedLocations, bool isSpareTrip)
    {
        this.scheduledDayCode = scheduledDayCode;
        this.scheduledPlanId = scheduledPlanId;
        this.scheduledPlanName = scheduledPlanName;
        this.id = id;
        this.name = name;
        this.tripNumber = tripNumber;
        this.tripCode = tripCode;
        this.startPos = startPos;
        this.endPos = endPos;
        this.startTime = startTime;
        this.endTime = endTime;
        this.timedLocations = timedLocations;
        this.isSpareTrip = isSpareTrip;
    }

    private int GetDelaySeconds()
    {
        var property = Service?.DataHandler?.GetTripProperty(GetTripPropertyKey());
        if (property != null)
            return property.DelaySeconds;

        return 0;
    }

    private int GetTrainLength()
    {
        var property = Service?.DataHandler?.GetTripProperty(GetTripPropertyKey());
        if (property != null)
            return property.TrainLength;

        return 0;
    }

    public TripPropertyKey GetTripPropertyKey()
    {
        return TripProperty.CreateKey(ScheduledDayCode, ScheduledPlanName, TripCode);
    }

    public bool IsValid()
    {
        return TripNumber != 0;
    }

    public override string ToString()
    {
        string s = string.Format($"[tripNo={TripNumber} id='{Id}' name='{Name}' tripCode='{TripCode}' startPos={StartPos} endPos={EndPos} startTime={StartTime} endTime={EndTime} TimedLocations:");
        foreach (var timedLocation in TimedLocations)
            s += " " + timedLocation;
        s += "]";
        return s;
    }
}

////////////////////////////////////////////////////////////////////////////////
// Collections (concurrent ones for automatic thread safety)

public class Trips : ConcurrentDictionary<int /*trip ID*/, Trip>
{
    public Trips(int concurrencyLevel, int capacity) : base(concurrencyLevel, capacity)
    {
    }
}

