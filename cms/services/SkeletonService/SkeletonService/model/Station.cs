namespace SkeletonService.Model;

// We don't have a way to create or extend RailGraph's station, so we now use our own one
public class Station
{
    public enum Priority { NoPriority = 0, NominalPriority = 1, OppositePriority = 2 };

    public RailgraphLib.HierarchyObjects.Station CTCStation => ctcStation;
    public string StationId { get => this.stationId; set => this.stationId = value; }
    public Priority StationPriority { get => this.stationPriority; set => this.stationPriority = value; }

    public List<RailgraphLib.HierarchyObjects.Station> StationsToNominal = new();
    public List<RailgraphLib.HierarchyObjects.Station> StationsToOpposite = new();

    // Route can lead to several platforms (on same edge!)
    public Dictionary<RailgraphLib.HierarchyObjects.Route, List<RailgraphLib.HierarchyObjects.Platform>> PlatformEntryRoutes = new();
    public Dictionary<RailgraphLib.HierarchyObjects.Route, List<RailgraphLib.HierarchyObjects.Platform>> PlatformExitRoutes = new();
    public List<RailgraphLib.HierarchyObjects.Route> OtherRoutes = new();

    private string stationId = string.Empty;
    private RailgraphLib.HierarchyObjects.Station ctcStation;
    private Priority stationPriority = Priority.NoPriority;

    public Station(RailgraphLib.HierarchyObjects.Station ctcStation)
    {
        this.ctcStation = ctcStation;
    }
    public override string ToString()
    {
        return "Station ID=" + this.stationId;
    }
}

