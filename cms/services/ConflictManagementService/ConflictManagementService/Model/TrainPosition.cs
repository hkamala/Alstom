namespace ConflictManagementService.Model;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Serialization;
using TrainData;
using static E2KService.ServiceImp;

public class TrainPosition : IEquatable<TrainPosition?>
{
    public Train? Train => GetTrain();
    public string Obid => obid;
    public ElementExtension? ElementExtension => elementExtension;
    public ActionTime OccurredTime => occurredTime; // UTC!
    public bool IsTerminated => terminated;
    public RailgraphLib.Enums.EDirection Direction => elementExtension?.EndPos.AdditionalPos < elementExtension?.StartPos.AdditionalPos ? RailgraphLib.Enums.EDirection.dOpposite : RailgraphLib.Enums.EDirection.dNominal;

    private string obid = "";
    private ElementExtension? elementExtension = null;
    private ActionTime occurredTime = new();
    private bool terminated = false;
    public List<string> MyCurrentTrackNameList { get; set; }
    public List<uint>? MyCurrentTrackUidList { get; set; }

    ////////////////////////////////////////////////////////////////////////////////

    public TrainPosition()
    {
    }
    // Train moves
    public TrainPosition(Train train, ElementExtension elementExtension, ActionTime occurredTime)
    {
        this.obid = train.Obid;
        this.occurredTime = occurredTime;
        this.elementExtension = elementExtension;
        terminated = false;
    }
    // Train is terminated
    public TrainPosition(Train train, ActionTime occurredTime)
    {
        this.obid = train.Obid;
        this.occurredTime = occurredTime;
        terminated = true;
    }
    public bool IsValid()
    {
        return obid != "" && elementExtension != null && occurredTime.IsValid() && !terminated || obid != "" && occurredTime.IsValid() && terminated;
    }
    public override string ToString()
    {
        return string.Format($"train = '{Train}', elementExtension = '{elementExtension}', time = '{occurredTime}', terminated = {terminated}");
    }

    private Train? GetTrain()
    {
        return Service?.DataHandler?.GetTrain(obid);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as TrainPosition);
    }

    public bool Equals(TrainPosition? other)
    {
        return other is not null &&
            obid == other.obid &&
            elementExtension != null &&
            elementExtension.Equals(other.elementExtension) &&
            terminated == other.terminated;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(obid, elementExtension, terminated);
    }

    public static bool operator ==(TrainPosition? left, TrainPosition? right)
    {
        return left is not null && left.Equals(right);
    }

    public static bool operator !=(TrainPosition? left, TrainPosition? right)
    {
        return !(left == right);
    }
}

////////////////////////////////////////////////////////////////////////////////
// Collections (concurrent ones for automatic thread safety)

public class TrainPositions : ConcurrentDictionary<string /*obid*/, TrainPosition>
{
    public TrainPositions(int concurrencyLevel, int capacity) : base(concurrencyLevel, capacity)
    {
    }
}
