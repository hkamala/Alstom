using static E2KService.ServiceImp;

namespace SkeletonService.Model;

////////////////////////////////////////////////////////////////////////////////

public class TimedLocation
{
    public string Description => description;   // Platform name, etc.
    public ElementPosition Pos => pos;
    public ActionTime Arrival => arrival;
    public ActionTime Departure => departure;
    public bool HasStopping => stopping;

    private readonly ActionTime arrival = new();
    private readonly ActionTime departure = new();
    private readonly string description = "";
    private readonly ElementPosition pos = new();
    private readonly bool stopping = false;

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

    public TimedLocation(RailgraphLib.HierarchyObjects.Platform platform, ActionTime arrival, ActionTime departure, bool stopping)
    {
        this.description = platform.SysName;
        var pos = Service?.RailgraphHandler?.GetElementPositionOfPlatform(platform);
        this.pos = pos == null ? new ElementPosition() : pos;
        this.arrival = arrival;
        this.departure = departure;
        this.stopping = stopping;
    }

    public bool IsValid()
    {
        return pos.IsValid();
    }

    public override string ToString()
    {
        return string.Format($"[ description='{Description}' pos={Pos} arrival={Arrival} departure={Departure} stopping={HasStopping} ]");
    }
}

