using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ConflictManagementLibrary.Logging;
using Microsoft.Extensions.Logging;
using RailgraphLib.HierarchyObjects;
using RailgraphLib.Interlocking;
using Track = RailgraphLib.HierarchyObjects.Track;

namespace ConflictManagementLibrary.Model.Movement
{
    public class RouteActionInfo
    {
        [JsonIgnore] public Route MyRoute { get; set; }
        [JsonIgnore] public string? SignalNameBegin { get; set; }
        [JsonIgnore] public string? SignalNameEnd { get; set; }
        [JsonIgnore] public List<string> TrackNameList = new List<string>();
        [JsonIgnore] private readonly IMyLogger? _theLogger;

        private RouteActionInfo(Route theRoute, IMyLogger? theLogger)
        {
            MyRoute = theRoute;
            this._theLogger = theLogger;
            GetRouteInformation();
        }

        public static RouteActionInfo CreateInstance(Route theRoute, IMyLogger? theLogger)
        {
            return new RouteActionInfo(theRoute, theLogger);
        }

        private void GetRouteInformation()
        {
            try
            {
               RailgraphLib.RailExtension.TrackExtension? te = MyRailGraphManager?.GetTrackExtensionOfRoute(MyRoute);
                if (te != null)
                {
                    var sig1 = MyRailGraphManager?.ILGraph?.getGraphObj(MyRoute.BPID);
                    SignalNameBegin = sig1?.getName();
                    var sig2 = MyRailGraphManager?.ILGraph?.getGraphObj(MyRoute.EPID);
                    SignalNameEnd = sig2?.getName();
                    var trackList = new StringBuilder();
                    foreach (var track in te.getExtensionElementsRaw())
                    {
                        var element = MyRailGraphManager?.ILGraph?.getGraphObj(track);
                        if (element is Track || element is TrackSection)
                        {
                            var trackName = element.getName();
                            TrackNameList.Add(trackName);
                            trackList.Append(" <" + trackName + "> ");
                        }
                    }
                    _theLogger?.LogInfo("New Route Action Info Created...Route Name <" + MyRoute.SysName + "> Begin Signal <" + SignalNameBegin + "> End Signal <" + SignalNameEnd + "> Tracks <" + trackList + ">");
                }
                else
                {
                    _theLogger?.LogInfo("Route Action Info Route Name <" + MyRoute.SysName + "> Not Created");
                }
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }

    }
}
