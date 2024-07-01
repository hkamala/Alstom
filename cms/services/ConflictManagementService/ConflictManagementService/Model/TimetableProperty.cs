using System.Collections.Concurrent;
using System;
namespace ConflictManagementService.Model;
using System.Collections.Generic;
////////////////////////////////////////////////////////////////////////////////

// public class ServiceProperty
// {
// }

public class TripProperty
{
    public TripPropertyKey Key => new(ScheduledDayCode, ServiceName, TripCode);
    public int ScheduledDayCode => scheduledDayCode;
    public string ServiceName => serviceName;
    public string TripCode => tripCode;
    public int TrainLength { get => trainLength; set => trainLength = value; }      // This is in millimeters
    public int DelaySeconds { get => delaySeconds; set => delaySeconds = value; }   // Now we only support one delay for trip, although DB supports more!

    private int scheduledDayCode = 0;
    private string serviceName = "";
    private string tripCode = "";
    private int trainLength = 0;
    private int delaySeconds = 0;

    public static TripPropertyKey CreateKey(int scheduledDayCode, string serviceName, string tripCode)
    {
        return new TripPropertyKey(scheduledDayCode, serviceName, tripCode);
    }

    public TripProperty(int scheduledDayCode, string serviceName, string tripCode)
    {
        this.scheduledDayCode = scheduledDayCode;
        this.serviceName = serviceName;
        this.tripCode = tripCode;
    }

    public TripProperty(TripPropertyKey key)
    {
        (this.scheduledDayCode, this.serviceName, this.tripCode) = key;
    }

    public bool IsValid()
    {
        return ScheduledDayCode != 0 && ServiceName != "" && TripCode != "";
    }

    public override string ToString()
    {
        return $"[scheduledDayCode={ScheduledDayCode} serviceName='{ServiceName}' tripCode='{TripCode}' trainLength={TrainLength} delaySeconds={DelaySeconds}]";
    }
}

////////////////////////////////////////////////////////////////////////////////
// Collections

public class TripPropertyKey : Tuple<int, string, string>, IEquatable<TripPropertyKey?>
{
    public int ScheduledDayCode => Item1;
    public string ServiceName => Item2;
    public string TripCode => Item3;

    public TripPropertyKey(int scheduledDayCode, string serviceName, string tripCode) : base(scheduledDayCode, serviceName, tripCode)
    {
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as TripPropertyKey);
    }

    public bool Equals(TripPropertyKey? other)
    {
        return other is not null &&
               base.Equals(other) &&
               ScheduledDayCode == other.ScheduledDayCode &&
               ServiceName == other.ServiceName &&
               TripCode == other.TripCode;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), ScheduledDayCode, ServiceName, TripCode);
    }

    public static bool operator ==(TripPropertyKey? left, TripPropertyKey? right)
    {
        return EqualityComparer<TripPropertyKey>.Default.Equals(left, right);
    }

    public static bool operator !=(TripPropertyKey? left, TripPropertyKey? right)
    {
        return !(left == right);
    }
}


public class TripProperties : Dictionary<TripPropertyKey, TripProperty>
{
}

