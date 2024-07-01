namespace SkeletonService.Model;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

public class TrainPosition : IEquatable<TrainPosition?>
{
    public Train? Train => train;
    public ElementExtension? ElementExtension => elementExtension;
    public ActionTime OccurredTime => occurredTime; // UTC!
    public bool IsTerminated => terminated;

    private Train? train = null;
    private ElementExtension? elementExtension = null;
    private ActionTime occurredTime = new();
    private bool terminated = false;

    ////////////////////////////////////////////////////////////////////////////////

    public TrainPosition()
    {
    }
    // Train moves
    public TrainPosition(Train train, ElementExtension elementExtension, ActionTime occurredTime)
    {
        this.train = train;
        this.occurredTime = occurredTime;
        this.elementExtension = elementExtension;
        terminated = false;
    }
    // Train is terminated
    public TrainPosition(Train train, ActionTime occurredTime)
    {
        this.train = train;
        this.occurredTime = occurredTime;
        terminated = true;
    }
    public bool IsValid()
    {
        return train != null && elementExtension != null && occurredTime.IsValid() && !terminated || train != null && occurredTime.IsValid() && terminated;
    }
    public override string ToString()
    {
        return string.Format($"train = '{train}', elementExtension = '{elementExtension}', time = '{occurredTime}', terminated = {terminated}");
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as TrainPosition);
    }

    public bool Equals(TrainPosition? other)
    {
        return other is not null &&
               EqualityComparer<ElementExtension?>.Default.Equals(ElementExtension, other.ElementExtension) &&
               IsTerminated == other.IsTerminated;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ElementExtension, IsTerminated);
    }

    public static bool operator ==(TrainPosition? left, TrainPosition? right)
    {
        return EqualityComparer<TrainPosition>.Default.Equals(left, right);
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
