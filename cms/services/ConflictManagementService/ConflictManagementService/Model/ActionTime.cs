using System;

namespace ConflictManagementService.Model;

////////////////////////////////////////////////////////////////////////////////

public class ActionTime
{
    DateTime utcDateTime;  // This should always have datetime in UTC!

    public static ActionTime Now => new(DateTime.UtcNow);
    public DateTime DateTime => utcDateTime;
    public ActionTime()
    {
        utcDateTime = DateTime.UnixEpoch;   // Invalid time
    }
    public ActionTime(string dateStamp, int hour = 0, int minute = 0, int second = 0)
    {
        if (!InitFromDateStringAndTime(dateStamp, hour, minute, second))
            utcDateTime = DateTime.UnixEpoch;   // Invalid time
    }
    public ActionTime(ActionTime actionTime)
    {
        utcDateTime = actionTime.utcDateTime;
    }
    public ActionTime(DateTime datetime)    // datetime must be UTC time!
    {
        utcDateTime = datetime;
    }

    public bool IsValid() => utcDateTime != DateTime.UnixEpoch;
    internal void SetTimeInvalid() => utcDateTime = DateTime.UnixEpoch;   // Invalid time
    public ulong GetMilliSecondsFromEpoch() => (ulong)(utcDateTime - DateTime.UnixEpoch).TotalMilliseconds;
    public ulong GetTimeStamp() => (ulong)(utcDateTime - DateTime.UnixEpoch).TotalSeconds;

    public static bool operator ==(ActionTime dt, ActionTime dt2) => dt.utcDateTime == dt2.utcDateTime;
    public static bool operator !=(ActionTime dt, ActionTime dt2) => dt.utcDateTime != dt2.utcDateTime;
    public static bool operator <(ActionTime dt, ActionTime dt2) => dt.utcDateTime < dt2.utcDateTime;
    public static bool operator <=(ActionTime dt, ActionTime dt2) => dt.utcDateTime <= dt2.utcDateTime;
    public static bool operator >(ActionTime dt, ActionTime dt2) => dt.utcDateTime > dt2.utcDateTime;
    public static bool operator >=(ActionTime dt, ActionTime dt2) => dt.utcDateTime >= dt2.utcDateTime;
    public override bool Equals(object? obj)
    {
        return obj is ActionTime time && utcDateTime == time.utcDateTime;
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(utcDateTime);
    }
    public DateTime AsDateTime() => utcDateTime;
    public DateTime AsLocalDateTime() => utcDateTime.ToLocalTime();
    public static ActionTime operator +(ActionTime t, TimeSpan timeSpan) => new(t.utcDateTime + timeSpan);
    public static ActionTime operator -(ActionTime t, TimeSpan timeSpan) => new(t.utcDateTime - timeSpan);
    public static TimeSpan operator -(ActionTime t, ActionTime t2) => t.utcDateTime - t2.utcDateTime;
    public override string ToString() => System.Xml.XmlConvert.ToString(utcDateTime, "yyyyMMddTHHmmss");
    public string ToLocalTimeString() => System.Xml.XmlConvert.ToString(utcDateTime.ToLocalTime(), "yyyyMMddTHHmmss");
    public string ToISODateTimeString() => System.Xml.XmlConvert.ToString(utcDateTime, "yyyy-MM-ddTHH:mm:ss.000Z");
    public bool InitFromFormat(string timeStamp, string format)
    {
        try
        {
            utcDateTime = System.Xml.XmlConvert.ToDateTime(timeStamp, format);
        }
        catch
        {
            return false;
        }
        return true;
    }
    public bool InitFromATSDateTimeString(string timeStamp)
    {
        return InitFromFormat(timeStamp, "yyyyMMddTHHmmss");
    }
    public bool InitFromISODateTimeString(string timeStamp)
    {
        return InitFromFormat(timeStamp, "yyyy-MM-ddTHH:mm:ss.000Z");
    }
    public bool InitFromDateStringAndTime(string dateStamp, int hour = 0, int minute = 0, int second = 0)
    {
        try
        {
            if (InitFromFormat(dateStamp, "yyyyMMdd"))
                utcDateTime += new TimeSpan(hour, minute, second);
        }
        catch
        {
            return false;
        }
        return true;
    }

}

