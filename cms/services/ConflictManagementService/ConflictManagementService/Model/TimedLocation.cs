using static E2KService.ServiceImp;

namespace ConflictManagementService.Model;

////////////////////////////////////////////////////////////////////////////////
public class TimedLocation
{
    public string Description => description;   // Platform name, etc.
    public ElementPosition Pos => pos;
    public ActionTime Arrival => arrival;
    public bool ArrivalOccurred { get => arrivalOccurred; set => arrivalOccurred = value; }
    public ActionTime Departure => departure;
    public bool DepartureOccurred { get => departureOccurred; set => departureOccurred = value; }
    public bool HasStopping => stopping;
    public int Id { get => id; set => id = value; }                 // Optional ID of timed location, platform ID, action, ...
    public int TripId { get => tripId; set => tripId = value; }  // Optional trip ID
    public string TripName { get => tripName; set => tripName = value; }  // Optional trip name

    private readonly ActionTime arrival = new();
    private readonly ActionTime departure = new();
    private bool arrivalOccurred = false;
    private bool departureOccurred = false;
    private readonly string description = "";
    private readonly ElementPosition pos = new();
    private readonly bool stopping = false;
    private int id = 0;
    private int tripId = 0;
    private string tripName = "";

    public TimedLocation()
    {
    }

    public TimedLocation(string description, ElementPosition pos, ActionTime arrival, ActionTime departure, bool stopping)
    {
        this.description = description;
        this.pos = pos;
        this.arrival = arrival;
        this.departure = departure;
        this.stopping = stopping;
    }

    //public TimedLocation(RailgraphLib.HierarchyObjects.Platform platform, ActionTime arrival, ActionTime departure, bool stopping)
    //{
    //    this.description = platform.SysName;
    //    this.pos = Service.RailgraphHandler.GetElementPositionOfPlatform(platform);
    //    this.arrival = arrival;
    //    this.departure = departure;
    //    this.stopping = stopping;
    //}

    public bool IsValid()
    {
        return pos.IsValid();
    }

    public override string ToString()
    {
        return string.Format($"[ description='{Description}' pos={Pos} arrival={Arrival} departure={Departure} stopping={HasStopping} ]");
    }
}

