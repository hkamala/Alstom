using System.Collections.Concurrent;

namespace ConflictManagementService.Model;

////////////////////////////////////////////////////////////////////////////////
// Route plan of service (service allocated to train or not)
// TODO: extend considerably later, now contains just TMS messaging based class

public class ScheduledRoutePlan
{
    // Accessors added later

    public ScheduledPlanKey? ScheduledPlanKey => scheduledPlanKey;
    public XSD.RoutePlan.rcsMsg? TMSRoutePlan => tmsRoutePlan;

    private readonly ScheduledPlanKey? scheduledPlanKey = null;
    private XSD.RoutePlan.rcsMsg? tmsRoutePlan;

    public ScheduledRoutePlan()
    {
    }

    public ScheduledRoutePlan(ScheduledPlanKey scheduledPlanKey, XSD.RoutePlan.rcsMsg tmsRoutePlan)
    {
        this.scheduledPlanKey = scheduledPlanKey;
        this.tmsRoutePlan = tmsRoutePlan;
    }

    public bool IsValid()
    {
        return this.scheduledPlanKey != null && this.tmsRoutePlan != null;
    }

    public override string ToString()
    {
        string s = string.Format($"ScheduledPlanKey={ScheduledPlanKey}");
        //TODO: more here?
        return s;
    }

    public void UpdateTMSRoutePlan(XSD.RoutePlan.rcsMsg? tmsRoutePlan)
    {
        this.tmsRoutePlan = tmsRoutePlan;
    }
}

////////////////////////////////////////////////////////////////////////////////
// Collections (concurrent ones for automatic thread safety)

public class ScheduledRoutePlans : ConcurrentDictionary<ScheduledPlanKey, ScheduledRoutePlan>
{
    public ScheduledRoutePlans(int concurrencyLevel, int capacity) : base(concurrencyLevel, capacity)
    {
    }
}
