using System.Collections.Generic;

namespace ConflictManagementLibrary.Model.Schedule;

public class ScheduledRoutePlan
{
    public ScheduledPlanKey? ScheduledPlanKey { get; set; }
    public TMSRoutePlan? TMSRoutePlan { get; set; }
}
public class Action
{
    public List<Command>? Command { get; set; }
    public Obj? Obj { get; set; }
    public int ID { get; set; }
    public bool IDSpecified { get; set; }
    public string? SubType { get; set; }
    public int SeqNo { get; set; }
    public bool SeqNoSpecified { get; set; }
    public int TimingMode { get; set; }
    public bool TimingModeSpecified { get; set; }
}

public class Command
{
    public List<object>? Properties { get; set; }
    public bool ttObjIDSpecified { get; set; }
    public string? cmd { get; set; }
    public string? value { get; set; }
}

public class Data
{
    public RoutePlan? RoutePlan { get; set; }
}

public class From
{
    public string PA { get; set; }
    public string RA { get; set; }
    public string PD { get; set; }
    public string RD { get; set; }
    public int ttobjID { get; set; }
    public bool ttobjIDSpecified { get; set; }
    public string ename { get; set; }
}

public class Hdr
{
    public string schema { get; set; }
    public string sender { get; set; }
    public string utc { get; set; }
    public int scnt { get; set; }
}

public class Item
{
    public From From { get; set; }
    public To To { get; set; }
    public Platform Platform { get; set; }
    public List<MasterRoute> MasterRoute { get; set; }
    public string ID { get; set; }
    public int TrID { get; set; }
    public bool TrIDSpecified { get; set; }
}

public class MasterRoute
{
    public List<Action> Actions { get; set; }
    public int ID { get; set; }
    public bool IDSpecified { get; set; }
    public string Type { get; set; }
    public int routingType { get; set; }
    public bool routingTypeSpecified { get; set; }
    public int start { get; set; }
    public bool startSpecified { get; set; }
    public int end { get; set; }
    public bool endSpecified { get; set; }
    public string destination { get; set; }
}

public class Obj
{
    public int tmsID { get; set; }
    public bool tmsIDSpecified { get; set; }
    public string ename { get; set; }
    public int secs { get; set; }
    public bool secsSpecified { get; set; }
    public int par1 { get; set; }
    public bool par1Specified { get; set; }
    public int par2 { get; set; }
    public bool par2Specified { get; set; }
}

public class Platform
{
    public bool stop { get; set; }
    public bool stopSpecified { get; set; }
    public bool passengers { get; set; }
    public bool passengersSpecified { get; set; }
}



public class RoutePlan
{
    public List<Train> Trains { get; set; }
}

public class ScheduledPlanKey
{
    public int ScheduledDayCode { get; set; }
    public string ScheduledPlanName { get; set; }
    public int Item1 { get; set; }
    public string Item2 { get; set; }
}

public class TMSRoutePlan
{
    public Hdr hdr { get; set; }
    public Data data { get; set; }
}

public class To
{
    public string PA { get; set; }
    public string RA { get; set; }
    public string PD { get; set; }
    public string RD { get; set; }
    public int ttobjID { get; set; }
    public bool ttobjIDSpecified { get; set; }
    public string ename { get; set; }
}

public class Train
{
    public string GUID { get; set; }
    public int serid { get; set; }
    public string SerN { get; set; }
    public int TripID { get; set; }
    public string Origin { get; set; }
    public string Destination { get; set; }
    public string FirstPassengerStop { get; set; }
    public string LastPassengerStop { get; set; }
    public string TrackedGUID { get; set; }
    public string CTCID { get; set; }
    public List<Item> Items { get; set; }
}

