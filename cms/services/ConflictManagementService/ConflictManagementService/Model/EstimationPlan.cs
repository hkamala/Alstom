using ConflictManagementService.Model.TMS;
using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
namespace ConflictManagementService.Model;

////////////////////////////////////////////////////////////////////////////////
// EstimationPlan (Forecast) named after BHP concept

public class EstimationPlan
{
    // Accessors added later
    public string Obid => obid;
    public string Td => td;
    public List<TimedLocation> TimedLocations => timedLocations;
    public ElementExtension TrainPath => trainPath;
    public ScheduledPlanKey? ScheduledPlanKey { get => scheduledPlanKey; set => scheduledPlanKey = value; }

    bool updatedByRefresh = false;
    private readonly string obid = "";
    private readonly string td = "";
    private readonly List<TimedLocation> timedLocations = new();
    private ElementExtension trainPath = new();
    private ScheduledPlanKey? scheduledPlanKey = null;

    public EstimationPlan()
    {
    }

    public EstimationPlan(string obid, string td, List<TimedLocation> timedLocations, ElementExtension trainPath)
    {
        this.obid = obid;
        this.td = td;
        this.timedLocations = timedLocations;
        this.trainPath = trainPath;

        updatedByRefresh = true;
    }

    public EstimationPlan(List<TimedLocation> timedLocations)
    {
        this.timedLocations = timedLocations;

        updatedByRefresh = true;
    }

    public bool IsValid()
    {
        return IsTrainEstimationPlan() || scheduledPlanKey != null;
    }

    public bool IsTrainEstimationPlan()
    {
        return Obid != "" && Td != "";
    }

    public override string ToString()
    {
        string s = string.Format("Obid='{0}' Td='{1}' TimedLocations:", Obid, Td);
        foreach (var timedLocation in TimedLocations)
            s += " " + timedLocation;
        s += " TrainPath: " + TrainPath;
        return s;
    }

    public void UpdateTrainPath(ElementExtension trainPath)
    {
        this.trainPath = trainPath;
    }

    public bool IsUpdatedByRefresh()
    {
        return updatedByRefresh;
    }

    public void ClearUpdatedByRefresh()
    {
        updatedByRefresh = false;
    }
}

////////////////////////////////////////////////////////////////////////////////
// Collections (concurrent ones for automatic thread safety)

public class TrainEstimationPlans : ConcurrentDictionary<string /*obid*/, EstimationPlan>
{
    public TrainEstimationPlans(int concurrencyLevel, int capacity) : base(concurrencyLevel, capacity)
    {
    }
}

public class EstimationPlans : ConcurrentDictionary<ScheduledPlanKey, EstimationPlan>
{
    public EstimationPlans(int concurrencyLevel, int capacity) : base(concurrencyLevel, capacity)
    {
    }
}
