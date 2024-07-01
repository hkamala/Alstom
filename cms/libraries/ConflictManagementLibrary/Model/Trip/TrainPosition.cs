using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ConflictManagementLibrary.Model.Schedule;

public class TrainPosition
{
    public TrainInfo Train { get; set; }
    public string Obid { get; set; }
    public ElementExtension? ElementExtension { get; set; }
    public OccurredTime? OccurredTime { get; set; }
    public bool IsTerminated { get; set; }
    public int Direction { get; set; }
    public List<string>? MyCurrentTrackNameList { get; set; }
    public List<uint>? MyCurrentTrackUidList { get; set; }
}

public class TrainInfo
{
    public string Obid { get; set; }
    public string Guid { get; set; }
    public string CtcId { get; set; }
    public string Td { get; set; }
    public int Sysid { get; set; }
    public int CtcType { get; set; }
    public string Postfix { get; set; }
    public string AllocatedTrainGuid { get; set; }
    public string TrainType { get; set; }
    public int DefaultLength { get; set; }
    
    [JsonIgnore]
    public int PreviousLength { get; set; }


}

public class ElementExtension
{
    public Startpos StartPos { get; set; }
    public EndPos EndPos { get; set; }
    public List<string>? Elements { get; set; }
}

public class StartPos
{
    public string ElementId { get; set; }
    public int Offset { get; set; }
    public int AdditionalPos { get; set; }
    public string? AdditionalName { get; set; }
}

public class EndPos
{
    public string ElementId { get; set; }
    public int Offset { get; set; }
    public int AdditionalPos { get; set; }
    public string? AdditionalName { get; set; }
}

public class OccurredTime
{
    public DateTime DateTime { get; set; }
}
