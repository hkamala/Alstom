namespace E2KService.Model;

////////////////////////////////////////////////////////////////////////////////
// Has to be handled in UTC to be consistent with other times!

internal class PurgeTime
{
	private DateTime lastPurged = DateTime.UtcNow;

	public PurgeTime()
	{
	}

	public bool IsPurgeTime()
	{
		var now = DateTime.UtcNow;
		var isPurgeTime = (now - this.lastPurged).TotalSeconds >= 60; // 1 minute
		
		if (isPurgeTime)
			this.lastPurged = now;

		return isPurgeTime;
	}
}
