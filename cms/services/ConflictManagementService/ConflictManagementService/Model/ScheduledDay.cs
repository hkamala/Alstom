using ConflictManagementService.Model.TMS;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ConflictManagementService.Model;

public class ScheduledDay : IEquatable<ScheduledDay?>
{
    public int ScheduledDayCode => scheduledDayCode;
    public ActionTime StartTime => startTime;

    private readonly int scheduledDayCode = -1;
    private readonly ActionTime startTime = new();
    private readonly ScheduledDayItem? scheduledDayTMS;

    public ScheduledDay(ActionTime startTime, ScheduledDayItem scheduledDay)
    {
        scheduledDayCode = scheduledDay.scheduledDayCode;
        this.startTime = startTime;
        scheduledDayTMS = scheduledDay;
    }

    public bool IsValid()
    {
        return scheduledDayCode != -1;
    }

    public bool IsToday()
    {
        return startTime.DateTime.Date == ActionTime.Now.DateTime.Date;
    }
    public bool IsTomorrow()
    {
        return startTime.DateTime.Date == ActionTime.Now.DateTime.Date.AddDays(1);
    }

    public string AsDateString()
    {
        return startTime.AsDateTime().ToString("yyyyMMdd");
    }

    public string AsISODateString()
    {
        return startTime.AsDateTime().ToString("yyyy-MM-dd");
    }

    public override string ToString()
    {
        return "ScheduledDayCode=" + scheduledDayCode + ", StartTime=" + startTime;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ScheduledDay);
    }

    public bool Equals(ScheduledDay? other)
    {
        return other != null && scheduledDayCode == other.scheduledDayCode;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(scheduledDayCode, startTime);
    }

    public static bool operator ==(ScheduledDay? left, ScheduledDay? right)
    {
        return EqualityComparer<ScheduledDay>.Default.Equals(left, right);
    }

    public static bool operator !=(ScheduledDay? left, ScheduledDay? right)
    {
        return !(left == right);
    }
}

////////////////////////////////////////////////////////////////////////////////
// Collections (concurrent ones for automatic thread safety)

public class ScheduledDays : ConcurrentDictionary<int /*scheduled day code*/, ScheduledDay>
{
    public ScheduledDays(int concurrencyLevel, int capacity) : base(concurrencyLevel, capacity)
    {
    }
}