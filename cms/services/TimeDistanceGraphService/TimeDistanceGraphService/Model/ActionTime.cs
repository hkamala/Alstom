namespace E2KService.Model;

////////////////////////////////////////////////////////////////////////////////

public class ActionTime
{
	DateTime utcDateTime;  // This should always have datetime in UTC!

	public static ActionTime Now => new(DateTime.UtcNow);
	public DateTime DateTime => this.utcDateTime;
	public ActionTime()
	{
		this.utcDateTime = DateTime.UnixEpoch;   // Invalid time
	}
	private ActionTime(DateTime datetime)
	{
		this.utcDateTime = datetime;
	}

    public bool IsValid() => this.utcDateTime != DateTime.UnixEpoch;
    internal void SetTimeInvalid() => this.utcDateTime = DateTime.UnixEpoch;   // Invalid time
    public ulong GetMilliSecondsFromEpoch() => (ulong)(this.utcDateTime - DateTime.UnixEpoch).TotalMilliseconds;
	public ulong GetTimeStamp() => (ulong)(this.utcDateTime - DateTime.UnixEpoch).TotalSeconds;
	
	//public static bool operator ==(ActionTime dt, ActionTime dt2) => dt == dt2;
	//public static bool operator !=(ActionTime dt, ActionTime dt2) => dt != dt2;

	public static ActionTime operator +(ActionTime t, TimeSpan timeSpan) => new(t.utcDateTime + timeSpan);
    public static TimeSpan operator -(ActionTime t, ActionTime t2) => t.utcDateTime - t2.utcDateTime;
    public override string ToString() => System.Xml.XmlConvert.ToString(this.utcDateTime, "yyyyMMddTHHmmss");
	public bool InitFromFormat(string timeStamp, string format)
	{
		try
		{
			this.utcDateTime = System.Xml.XmlConvert.ToDateTime(timeStamp, format);
		}
		catch
		{
			return false;
		}
		return true;
	}
	public bool InitFromISOString(string timeStamp)
    {
		return InitFromFormat(timeStamp, "yyyyMMddTHHmmss");
    }
	public bool InitFromDateStringAndTime(string dateStamp, int hour = 0, int minute = 0, int second = 0)
	{
		try
		{
			if (InitFromFormat(dateStamp, "yyyyMMdd"))
				this.utcDateTime += new TimeSpan(hour, minute, second);
		}
		catch
		{
			return false;
		}
		return true;
	}
}

