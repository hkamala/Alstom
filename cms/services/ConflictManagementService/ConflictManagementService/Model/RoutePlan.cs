using System.Collections.Concurrent;
using System.Text;
using ConflictManagementLibrary.Network;
using Newtonsoft.Json;
using RoutePlanLib;

namespace ConflictManagementService.Model;

////////////////////////////////////////////////////////////////////////////////
// Route plan of train allocated to timetable
// TODO: extend considerably later, now contains just TMS messaging based class and info, if already sent to ROS!

public class RoutePlan
{
    // Accessors added later
    public string Guid => guid;
    public XSD.RoutePlan.rcsMsg? TMSRoutePlan { get => tmsRoutePlan; set => tmsRoutePlan = value; }//TMSRoutePlan => tmsRoutePlan;
    public bool SentToROS { get => sentToROS; set => sentToROS = value; }   // Needed only for RoutePlanService kind of functionality with late timetable allocation!

    private readonly string guid = "";
    private XSD.RoutePlan.rcsMsg? tmsRoutePlan;
    bool sentToROS = false;

    public RoutePlan()
    {
    }

    public RoutePlan(string guid, XSD.RoutePlan.rcsMsg tmsRoutePlan)
    {
        this.guid = guid;
        ////var str = JsonConvert.SerializeObject(tmsRoutePlan);
        //this.tmsRoutePlan = tmsRoutePlan; //JsonConvert.DeserializeObject<XSD.RoutePlan.rcsMsg>(str);
        var str = XmlSerialization.SerializeObject(tmsRoutePlan, out string error);
        string _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
        if (str.StartsWith(_byteOrderMarkUtf8))
        {
            str = str.Remove(0, _byteOrderMarkUtf8.Length);
        }
        this.tmsRoutePlan = XmlSerialization.DeserializeObjectFromString<XSD.RoutePlan.rcsMsg>(str, out string desError);
    }

    public bool IsValid()
    {
        return Guid != "" && TMSRoutePlan != null;
    }

    public override string ToString()
    {
        string s = string.Format($"Guid='{Guid}'");
        //TODO: more here?
        return s;
    }

    public void UpdateTMSRoutePlan(XSD.RoutePlan.rcsMsg? tmsRoutePlan)
    {
        this.tmsRoutePlan = tmsRoutePlan;
        SentToROS = false;
    }
}

////////////////////////////////////////////////////////////////////////////////
// Collections (concurrent ones for automatic thread safety)

public class TrainRoutePlans : ConcurrentDictionary<string /*guid*/, RoutePlan>
{
    public TrainRoutePlans(int concurrencyLevel, int capacity) : base(concurrencyLevel, capacity)
    {
    }
}
