namespace SkeletonService.Model;

////////////////////////////////////////////////////////////////////////////////
// Has to be handled in UTC to be consistent with other times!

internal class PurgeTime
{
    private ActionTime lastPurged = ActionTime.Now;

    public PurgeTime()
    {
    }

    public bool IsPurgeTime()
    {
        var now = ActionTime.Now;
        var isPurgeTime = (now - lastPurged).TotalHours >= 1; // 1 hour

        if (isPurgeTime)
            lastPurged = now;

        return isPurgeTime;
    }
}
