using System.Collections.Concurrent;
using System.Collections.Generic;
using System;
namespace ConflictManagementService.Model;
enum RestrictionType { POSSESSION = 1 };

public class Possession
{
    public string Id => id;
    public string Description => description;
    public ElementPosition StartPos => startPos;
    public ElementPosition EndPos => endPos;
    public ActionTime StartTime => startTime;
    public ActionTime EndTime => endTime;
    public string State => state;

    bool updatedByRefresh = false;

    private readonly string id = "";
    private readonly string description = "";
    private readonly ElementPosition startPos = new();
    private readonly ElementPosition endPos = new();
    private ActionTime startTime = new();
    private ActionTime endTime = new();
    private string state = "";

    public Possession()
    {
    }

    public Possession(string id, string description, ElementPosition startPos, ElementPosition endPos, ActionTime startTime, ActionTime endTime, string state)
    {
        this.id = id;
        this.description = description;
        this.startPos = startPos;
        this.endPos = endPos;
        this.startTime = startTime;
        this.endTime = endTime;

        // If there is no start time given (ad-hoc possession), set current time as start time
        if (!this.startTime.IsValid())
            this.startTime = ActionTime.Now;

        this.state = state;

        updatedByRefresh = true;
    }

    public bool IsValid()
    {
        return Id != "" && StartPos.IsValid() && EndPos.IsValid() && StartTime.IsValid() && EndTime.IsValid();
    }

    public override string ToString()
    {
        return string.Format($"[id='{Id}' description='{Description}' startPos={StartPos} endPos={EndPos} startTime={StartTime} endTime={EndTime} state={State}]");
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

public class ActivationAction : Tuple<ActionTime /*activationactiontime*/, bool /*activated*/>
{
    public ActivationAction(ActionTime item1, bool item2) : base(item1, item2)
    {
    }
}
public class ActivationActionVector : List<ActivationAction>
{
    public ActivationActionVector() : base()
    {
    }

    public ActivationActionVector(IEnumerable<ActivationAction> collection) : base(collection)
    {
    }
}

public class Possessions : ConcurrentDictionary<string /*possession ID*/, Possession>
{
    public Possessions(int concurrencyLevel, int capacity) : base(concurrencyLevel, capacity)
    {
    }
}
