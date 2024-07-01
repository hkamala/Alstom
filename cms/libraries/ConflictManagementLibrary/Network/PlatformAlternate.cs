using ConflictManagementLibrary.Management;
using ConflictManagementLibrary.Model.Movement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ConflictManagementLibrary.Network
{
    public class PlatformAlternate
    {
        public int MyReferenceNumber { get; set; }
        public string MyName { get; set; }
        public int StationId { get; set; }
        public string StationAbbreviation { get; set; }
        public List<RouteAction> MyRouteActions { get; set; }
        public List<RouteAction> MyRouteActionsToOriginalRoute { get; set; }
        [JsonIgnore] public string PlatformReplacementName { get; set; }
        [JsonIgnore] public string PlatformFromName { get; set; }
        [JsonIgnore] public string PlatformToName { get; set; }
        [JsonIgnore] public Track? MyTrackNetwork { get; set; }
        [JsonIgnore] public RailgraphLib.HierarchyObjects.Track? MyTrackHierarchy { get; set; }
        [JsonIgnore] public RailgraphLib.HierarchyObjects.Platform? MyHierarchyPlatform;
        [JsonIgnore] public ElementPosition? MyElementPosition;
        [JsonIgnore] public long MyDistanceInMeters = 0;
        [JsonIgnore] public bool UseAlternateToPaths;
        [JsonIgnore] public bool UseAlternateFromPaths;

 
        public PlatformAlternate()
        {
            MyRouteActionsToOriginalRoute = new List<RouteAction>();
            MyRouteActions = new List<RouteAction>();
        }

        public void AddRouteAction(string routeName, string actionType, string actionLocation, string startTime, string startTimeMinimum)
        {
                var ra = RouteAction.CreateInstance(routeName,actionType, actionLocation, startTime, startTimeMinimum);
                MyRouteActions.Add(ra);
        }
        public void AddReRouteAction(string routeName, string actionType, string actionLocation, string startTime, string startTimeMinimum)
        {
            var ra = RouteAction.CreateInstance(routeName, actionType, actionLocation, startTime, startTimeMinimum);
            MyRouteActionsToOriginalRoute.Add(ra);
        }

    }
}
