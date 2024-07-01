namespace E2KService.Model;

using System.Collections.Concurrent;

enum RestrictionType { POSSESSION = 1 };

////////////////////////////////////////////////////////////////////////////////

public class TimedLocation
{
    public string Description => description;
    public EdgePosition Pos => pos;
	public ActionTime Arrival => this.arrival;
    public ActionTime Departure => this.departure;
    public bool ArrivalOccurred { get => arrivalOccurred; set => arrivalOccurred = value; }
    public bool DepartureOccurred { get => departureOccurred; set => departureOccurred = value; }
    public int TripId { get => tripId; set => tripId = value; }  // Optional trip ID
    public string TripName { get => tripName; set => tripName = value; }  // Optional trip name

    private readonly ActionTime arrival = new();
    private readonly ActionTime departure = new();
    private bool arrivalOccurred = false;
    private bool departureOccurred = false;
    private int tripId = 0;
    private string tripName = "";
    private readonly string description = "";
    private readonly EdgePosition pos = new();

    public TimedLocation()
    {
    }

    public TimedLocation(string description, EdgePosition pos, ActionTime arrival, ActionTime departure)
    {
        this.description = description;
        this.pos = pos;
        this.arrival = arrival;
        this.departure = departure;
    }

    public bool IsValid()
    {
        return this.pos.IsValid();
    }

    public override string ToString()
    {
        return string.Format("[ description='{0}' pos={1} arrival={2} departure={3} ]", Description, Pos, Arrival, Departure);
    }
}

////////////////////////////////////////////////////////////////////////////////

public class EstimationPlan
{
    // Accessors added later
    public string Obid => obid;
    public string Td => td;
    public List<TimedLocation> TimedLocations => timedLocations;
    public EdgeExtension TrainPath => trainPath;
    public ScheduledPlanKey? ScheduledPlanKey { get => scheduledPlanKey; set => scheduledPlanKey = value; }
    public int TripId { get; set; }

    bool updatedByRefresh = false;
    private readonly string obid = "";
    private readonly string td = "";
    private readonly List<TimedLocation> timedLocations = new();
    private readonly EdgeExtension trainPath = new();
    private ScheduledPlanKey? scheduledPlanKey = null;

    public EstimationPlan()
    {
        this.TripId = 0;
    }

    public EstimationPlan(string obid, string td, List<TimedLocation> timedLocations, EdgeExtension trainPath)
    {
        this.obid = obid;
        this.td = td;
        this.timedLocations = timedLocations;
        this.trainPath = trainPath;

        this.TripId = 0;

        this.updatedByRefresh = true;
    }

    public EstimationPlan(List<TimedLocation> timedLocations)
    {
        this.timedLocations = timedLocations;

        this.TripId = 0;

        this.updatedByRefresh = true;
    }

    public bool IsValid()
    {
        return IsTrainEstimationPlan() || scheduledPlanKey != null;
    }

    public bool IsTrainEstimationPlan()
    {
        return Obid != "" && Td != "";
    }

    public override string ToString()
    {
        string s = string.Format("Obid='{0}' Td='{1}' TimedLocations:", Obid, Td);
        foreach (var timedLocation in TimedLocations)
            s += " " + timedLocation;
        s += " TrainPath: " + TrainPath;
        return s;
    }

    public bool IsUpdatedByRefresh()
    {
        return this.updatedByRefresh;
    }

    public void ClearUpdatedByRefresh()
    {
        this.updatedByRefresh = false;
    }
}

////////////////////////////////////////////////////////////////////////////////

public class Trip
{
    public string Id => this.id;
    public string Name => this.name;
    public int TripNumber => tripNumber;
    public string Description => description;
    public EdgePosition StartPos => startPos;
    public EdgePosition EndPos => endPos;
    public ActionTime StartTime => this.startTime;
    public ActionTime EndTime => this.endTime;
    public List<TimedLocation> TimedLocations => this.timedLocations;
    public bool ActiveTrip => activeTrip;
    public bool Allocated { get; set; }

    private readonly ActionTime startTime = new();
    private readonly ActionTime endTime = new();
    private readonly string description = "";
    private readonly EdgePosition startPos = new();
    private readonly EdgePosition endPos = new();
    private readonly string id = "";
    private readonly string name = "";
    private readonly int tripNumber = 0;
    private readonly List<TimedLocation> timedLocations = new();
    private readonly bool activeTrip = true;

    public Trip()
    {
        this.Allocated = false;
    }

    public Trip(string id, string name, int tripNumber, string description, EdgePosition startPos, EdgePosition endPos, ActionTime startTime, ActionTime endTime, List<TimedLocation> timedLocations, bool activeTrip)
    {
        this.id = id;
        this.name = name;
        this.tripNumber = tripNumber;
        this.description = description;
        this.startPos = startPos;
        this.endPos = endPos;
        this.startTime = startTime;
        this.endTime = endTime;
        this.timedLocations = timedLocations;
        this.activeTrip = activeTrip;
        this.Allocated = false;
}

public bool IsValid()
    {
        return TripNumber != 0;
    }

    public override string ToString()
    {
        string s = string.Format($"[id = {Id} tripNo='{TripNumber}' description='{Description}' startPos={StartPos} endPos={EndPos} startTime={StartTime} endTime={EndTime} activeTrip={ActiveTrip} TimedLocations:");
        foreach (var timedLocation in TimedLocations)
            s += " " + timedLocation;
        s += "]";
        return s;
    }
}

////////////////////////////////////////////////////////////////////////////////

public class ScheduledPlan
{
    public ScheduledPlanKey Key => GetKey();
    public int DayCode => dayCode;
    public string Id => id;
    public string Name => name;
    public string TrainType => trainType;
    public string Description => description;
    public List<Trip> Trips => trips;
    public bool ActivePlan => activePlan;
    public bool Allocated { get; set; }

    private bool updatedByRefresh = false;
    private int dayCode = 0;
    private readonly string id = "";
    private readonly string name = "";
    private readonly string trainType = "";
    private readonly string description = "";
    private readonly List<Trip> trips = new();
    private readonly bool activePlan = true;

    public ScheduledPlan()
    {
        this.Allocated = false;
    }

    public ScheduledPlan(int dayCode, string id, string name, string trainType, string description, List<Trip> trips, bool activePlan)
    {
        this.dayCode = dayCode;
        this.id = id;
        this.name = name;
        this.trainType = trainType;
        this.description = description;
        this.trips = trips;
        this.activePlan = activePlan;
        this.Allocated = false;

        this.updatedByRefresh = true;
    }

    public bool IsValid()
    {
        return Id != "" && Name != "" && Trips.Count > 0;
    }

    public override string ToString()
    {
        string s = string.Format("id='{0}' name='{1}' trainType='{2}', description='{3}' Trips:", Id, Name, TrainType, Description);
        foreach (var trip in Trips)
            s += " " + trip;
        return s;
    }

    private ScheduledPlanKey GetKey()
    {
        return new ScheduledPlanKey(dayCode, id, name);
    }

    public bool IsUpdatedByRefresh()
    {
        return this.updatedByRefresh;
    }

    public void ClearUpdatedByRefresh()
    {
        this.updatedByRefresh = false;
    }
}

////////////////////////////////////////////////////////////////////////////////

public class ActivationAction : Tuple<ActionTime /*activationactiontime*/, bool /*activated*/, string /*state*/>
{
    public ActivationAction(ActionTime item1, bool item2, string item3) : base(item1, item2, item3)
    {
    }
}
public class ActivationActionVector : List<ActivationAction>
{
    public ActivationActionVector() : base()
    {
    }

    public ActivationActionVector(IEnumerable<ActivationAction> collection) : base(collection)
    {
    }
}

public class Possession
{
    public string ExternalId => externalId;
    public string Description => description;
    public EdgePosition StartPos => startPos;
    public EdgePosition EndPos => endPos; 
    public ActionTime StartTime => this.startTime;
    public ActionTime EndTime => this.endTime;
    public string State => this.state;

    protected string state = "";
    protected bool active = false;
    protected bool startTimeLocked = false;
    protected bool endTimeLocked = false;
    protected bool historic = false;
    protected ActivationActionVector activationActions = new();
    protected bool updatedByRefresh = false;
    private readonly string externalId = "";
    private readonly string description = "";
    private readonly EdgePosition startPos = new();
    private readonly EdgePosition endPos = new();

    private ActionTime startTime = new();
    private ActionTime endTime = new();

    public Possession()
    {
    }

    public Possession(string id, string description, EdgePosition startPos, EdgePosition endPos, ActionTime startTime, ActionTime endTime, bool active, string state)
    {
        this.externalId = id;
        this.description = description;
        this.startPos = startPos;
        this.endPos = endPos;
        this.startTime = startTime;
        this.endTime = endTime;
        this.state = state;

        this.startTimeLocked = false;
        this.endTimeLocked = false;
        this.historic = false;
        this.updatedByRefresh = true;

        // If there is no start time given (ad-hoc possession), set current time as start time
        if (!this.startTime.IsValid())
            this.startTime = ActionTime.Now;

        // Initial active level must also be remembered in this.activationActions, force setting!
        this.active = !active;
        SetActive(active, state);

        // If directly active, lock start time
        if (this.active)
            this.startTimeLocked = true;
    }

    // This constructor is for setting raw data without any logic applied (load from persistent storage)
    public Possession(string id, string description, EdgePosition startPos, EdgePosition endPos, ActionTime startTime, ActionTime endTime, bool active, bool startTimeLocked, bool endTimeLocked, bool historic, ActivationActionVector actions)
    {
        // Resolve external ID
        try
        {
            this.externalId = id[..id.IndexOf("-[-*OPER*-]-")];
        }
        catch
        {
            this.externalId = id;
        }

        this.description = description;
        this.startPos = startPos;
        this.endPos = endPos;
        this.startTime = startTime;
        this.endTime = endTime;

        this.active = active;
        this.startTimeLocked = startTimeLocked;
        this.endTimeLocked = endTimeLocked;
        this.historic = historic;
        this.activationActions = actions;
        this.updatedByRefresh = true;
    }

    public bool IsValid()
    {
        return ExternalId != "" && StartPos.IsValid() && EndPos.IsValid() && EndTime.IsValid();
    }

    public override string ToString()
    {
        string s = string.Format("[externalId='{0}' id='{1}' description='{2}' startPos={3} endPos={4} startTime={5} endTime={6} active={7} state={8} startTimeLocked={9} endTimeLocked={10} historic={11} action count={12}]",
                                 ExternalId, GetId(), Description, StartPos, EndPos, StartTime, EndTime, this.active, this.state, this.startTimeLocked, this.endTimeLocked, this.historic, this.activationActions.Count);
        return s;
    }

    // This is dynamic operational ID, which is based on external ID, position and historic state, and which must be used for identification
    // By using the input flag, previous ID before history, can be obtained (not very clever way, but needed for simplicity, when possession is deleted and it moves into history)
    public string GetId(bool givePreviousId = false)
    {
        string s = ExternalId + "-[-*OPER*-]-" + StartPos.GetEdgePosIdentifier() + "-" + EndPos.GetEdgePosIdentifier();
        if (this.historic && !givePreviousId)
            s += "-" + StartTime + "-" + EndTime;
        return s;
    }

    public void Merge(Possession other)
    {
        // Merge data from other/old (ie. existing and with same identifier) possession to this (new) one
        if (GetId() == other.GetId() && !other.IsHistoric())
        {
            // ID and positions are the same and both are existing possessions
            // Description may have been changed, so do not copy from other one

            // If start time was locked before, use that one as current
            if (other.StartTime.IsValid() && other.IsStartTimeLocked())
            {
                this.startTime = other.startTime;
                this.startTimeLocked = true;
            }

            // End time may have been changed, so don't change that

            // Previous activation changes must be remembered
            ActivationActionVector? ourActions = this.activationActions;

            this.activationActions = new ActivationActionVector(other.activationActions); // TODO: check that list is really cloned!

            // Is there change in active state between new and old? If not, do not add first one of new actions
            if (this.activationActions.Count > 0 && ourActions.Count > 0)
            {
                var lastOldAction = this.activationActions.Last();
                var firstNewAction = ourActions.First();

                if (lastOldAction.Item2 == firstNewAction.Item2)
                    ourActions.RemoveAt(0);
            }
            this.activationActions.AddRange(ourActions);  // .insert(this.activationActions.end(), ourActions.begin(), ourActions.end());

            this.updatedByRefresh = true;
        }
    }

    public bool IsActive()
    {
        return this.active;
    }
    public bool IsStartTimeLocked()
    {
        return this.startTimeLocked;
    }
    public void SetStartTimeLocked()
    {
        if (!this.historic)
        {
            if (!this.startTimeLocked)
            {
                this.startTime = ActionTime.Now;
            }
            this.startTimeLocked = true;
        }
    }
    public bool IsEndTimeLocked()
    {
        return this.endTimeLocked;
    }
    public void SetEndTimeLocked()
    {
        if (!this.historic)
        {
            if (!this.endTimeLocked)
            {
                this.endTime = ActionTime.Now;
            }
            this.endTimeLocked = true;
        }
    }
    public bool IsHistoric()
    {
        return this.historic;
    }
    public void SetHistoric()
    {
        SetStartTimeLocked();
        SetEndTimeLocked();
        this.active = false;
        this.historic = true;
    }
    public ActivationActionVector GetActivationActions()
    {
        return this.activationActions;
    }
    public void SetActivationActions(ActivationActionVector actions)
    {
        if (!this.historic)
            this.activationActions = actions;
    }
    public bool IsUpdatedByRefresh()
    {
        return this.updatedByRefresh;
    }
    public void ClearUpdatedByRefresh()
    {
        this.updatedByRefresh = false;
    }
    private void SetActive(bool active, string state)
    {
        if (!this.historic)
        {
            bool addAction;

            // Is there change in active state?
            if (this.activationActions.Count > 0)
            {
                var lastAction = this.activationActions.Last();
                addAction = active != lastAction.Item2 || state != lastAction.Item3;
            }
            else
                addAction = active != this.active;

            this.active = active;

            if (this.active && !this.startTimeLocked)
                SetStartTimeLocked();

            if (addAction)
                this.activationActions.Add(new(ActionTime.Now, active, state)); // TODO: Time should be got somewhere!
        }
	}

	public string ToJsonStrings(out string activationActions)
	{
		// Possession
		var stream = new MemoryStream();
		var writer = new System.Text.Json.Utf8JsonWriter(stream, new System.Text.Json.JsonWriterOptions() { Indented = false });

		writer.WriteStartObject();

		writer.WriteString("description", this.Description);
		writer.WriteString("starttime", this.StartTime.GetMilliSecondsFromEpoch().ToString());
		writer.WriteString("endtime", this.EndTime.GetMilliSecondsFromEpoch().ToString());
		writer.WriteString("starttimelocked", this.startTimeLocked.ToString());
		writer.WriteString("endtimelocked", this.endTimeLocked.ToString());

        var addPosition = (string nodeName, EdgePosition pos) =>
        {
            writer.WriteStartObject(nodeName);
            writer.WriteString("edgeid", pos.EdgeId);
            writer.WriteString("offset", pos.Offset.ToString());
            writer.WriteString("fromvertexid", pos.FromVertexId);
            writer.WriteString("addpos", pos.AdditionalPos.ToString());
            writer.WriteString("addname", pos.AdditionalName);
            writer.WriteEndObject();
        };

		addPosition("startposition", this.StartPos);
		addPosition("endposition", this.EndPos);

        writer.WriteEndObject();
        writer.Flush();
        
        string restriction = System.Text.Encoding.UTF8.GetString(stream.ToArray());

        // Activation actions
		// Cassandra does not understand JSON array properly, can not use it. Had to change restrictions-table and replace list with map
		// And even map is impossible to generate with Utf8JsonWriter so that Cassandra would understand it as a map
		// Do it by hand...
        activationActions = "{";
        foreach (var activationAction in this.activationActions)
        {
            if (activationActions != "{")
                activationActions += ",";
			activationActions += "\"" + activationAction.Item1.GetMilliSecondsFromEpoch().ToString();
			activationActions += "\":";
            activationActions += "\"" + activationAction.Item3 + "\"";
        }
        activationActions += "}";

        return restriction;
	}
}

////////////////////////////////////////////////////////////////////////////////
// Collections (concurrent ones for automatic thread safety)
public class ScheduledPlanKey : Tuple<int, string, string>, IEquatable<ScheduledPlanKey?>
{
    public int ScheduledDayCode => Item1;
    public string ScheduledPlanId => Item2;
    public string ScheduledPlanName => Item3;

    public ScheduledPlanKey(int scheduledDayCode, string scheduledPlanId, string scheduledPlanName) : base(scheduledDayCode, scheduledPlanId, scheduledPlanName)
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
               ScheduledPlanId == other.ScheduledPlanId &&
               ScheduledPlanName == other.ScheduledPlanName;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), ScheduledDayCode, ScheduledPlanId, ScheduledPlanName);
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

public class TrainEstimationPlans : ConcurrentDictionary<string /*obid*/, EstimationPlan>
{
    public TrainEstimationPlans(int concurrencyLevel, int capacity) : base(concurrencyLevel, capacity)
    {
    }
}

public class EstimationPlans : ConcurrentDictionary<ScheduledPlanKey, EstimationPlan>
{
    public EstimationPlans(int concurrencyLevel, int capacity) : base(concurrencyLevel, capacity)
    {
    }
}

public class ScheduledPlans : ConcurrentDictionary<ScheduledPlanKey, ScheduledPlan>
{
    public ScheduledPlans(int concurrencyLevel, int capacity) : base(concurrencyLevel, capacity)
    {
    }
}

public class Possessions : ConcurrentDictionary<string /*possession ID*/, Possession>
{
    public Possessions(int concurrencyLevel, int capacity) : base(concurrencyLevel, capacity)
    {
    }
}
