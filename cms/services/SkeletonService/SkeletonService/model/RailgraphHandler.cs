using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Diagnostics.Debug;
using Serilog;
using RailgraphLib;

namespace SkeletonService.Model
{
    internal class RailgraphHandler
    {
        public RailgraphLib.NetworkCreator? RailGraph => this.railGraph;
        //public RailgraphLib.Core.CoreTopoGraph? CoreTopoGraph => this.coreTopoGraph;
        public RailgraphLib.Interlocking.ILTopoGraph? ILTopoGraph => this.ilTopoGraph;
        public RailgraphLib.Interlocking.ILGraph? ILGraph => this.ilGraph;
        public RailgraphLib.HierarchyObjects.HierarchyRelations? HierarchyRelations { get => this.hierarchyRelations; }

        private RailgraphLib.NetworkCreator? railGraph;
        //private RailgraphLib.Core.CoreGraph? coreGraph;
        //private RailgraphLib.Core.CoreTopoGraph? coreTopoGraph;
        private RailgraphLib.Interlocking.ILGraph? ilGraph;
        private RailgraphLib.Interlocking.ILTopoGraph? ilTopoGraph;
        private RailgraphLib.HierarchyObjects.HierarchyRelations? hierarchyRelations;

        private DataHandler dataHandler;

        private Dictionary<string, RailgraphLib.CoreObj> coreObjectsByName = new();

        public RailgraphHandler(DataHandler dataHandler)
        {
            this.dataHandler = dataHandler;

            CreateRailGraph();
            BuildStationNetwork();

            // Create dictionary of core objects by name
            this.ILGraph.iterateAllCoreObjectsAndCallMethod(BuildCoreObjectByNameDictionary);
        }

        ~RailgraphHandler()
        {
            // Close RailGraph
            this.railGraph?.destroy();
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Create RailGraph
        // - Connect to CTC Solid DB
        // - Initialize Armd from DB
        // - Create and initialize RailGraph objects and networks from DB
        // - Connect to CDH shared memory for dynamic states of objects
        private void CreateRailGraph()
        {
            Log.Information($"Connecting to Solid DB");

            RailgraphLib.SolidDB.CSolidEntryPoint solidDB = new();
            if (!solidDB.Connect())
            {
                throw new Exception("Couldn't connect to Solid DB");
            }

            Log.Information($"Initializing Armd");
            RailgraphLib.armd.Armd.init(solidDB);

            Log.Information($"Initializing RailGraph");

            // Hierarchy relation objects (nametype of NAMEPARTS table as parameter)
            Log.Information($"Building hierarchy relations");
            this.hierarchyRelations = RailgraphLib.HierarchyObjects.HierarchyRelations.Instance(solidDB, 6000);

            // Network creator
            this.railGraph = new RailgraphLib.NetworkCreator(solidDB, this.hierarchyRelations);

            // Core graph, objects and topograph. Actually not needed, because core objects and conversions of element extension are done in interlocking topograph
            /*
            Log.Information($"Initializing core graph and topograph");
            this.coreGraph = new RailgraphLib.Core.CoreGraph();
            this.coreGraph.startInit();
            this.coreTopoGraph = new RailgraphLib.Core.CoreTopoGraph(this.coreGraph);
            this.railGraph.add(this.coreTopoGraph);
            */
            // Interlocking graph, objects and topograph
            Log.Information($"Initializing interlocking graph and topograph");
            this.ilGraph = new RailgraphLib.Interlocking.ILGraph(solidDB);
            this.ilGraph.startInit();
            this.ilTopoGraph = new RailgraphLib.Interlocking.ILTopoGraph(this.ilGraph);
            this.railGraph.add(this.ilTopoGraph);

            Log.Information($"Building RailGraph networks and creating objects");
            this.railGraph.create();

            Log.Information($"Closing connection to Solid DB");
            solidDB.CloseConnection();
        }

        private void BuildCoreObjectByNameDictionary(uint coreObjId)
        {
            var coreObj = this.ILTopoGraph.getCoreObj(coreObjId);
            if (coreObj != null)
            {
                this.coreObjectsByName.Add(coreObj.getName(), coreObj);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        public class StationFindCondition : RailgraphLib.FindCondition.FindAllCondition
        {
            public Dictionary<UInt32, Station> stations;
            private UInt32 currentStationId = 0;
            private readonly RailgraphHandler railgraphHandler;

            public StationFindCondition(RailgraphHandler railgraphHandler, ref Dictionary<UInt32, Station> stations, UInt32 from, RailgraphLib.Enums.EDirection eSearchDir) : base(from, 0, eSearchDir)
            {
                this.railgraphHandler = railgraphHandler;
                this.stations = stations;
                var track = (RailgraphLib.Interlocking.Track)railgraphHandler.ILTopoGraph.getGraphObj(from);
                this.currentStationId = track.getLogicalStation();

                setSearchDepth(1000);
            }

            public override EConditionalProceed isConditionFound(UInt32 current, UInt32 previous)
            {
                if (railgraphHandler.ILTopoGraph.getGraphObj(current) is RailgraphLib.Interlocking.Track)
                {
                    var track = (RailgraphLib.Interlocking.Track)railgraphHandler.ILTopoGraph.getGraphObj(current);
                    var stationId = track.getLogicalStation();
                    if (stationId != 0 && stationId != this.currentStationId)
                    {
                        RailgraphLib.HierarchyObjects.Station station = railgraphHandler.HierarchyRelations.GetStationBySysID(stationId);
                        RailgraphLib.HierarchyObjects.Station currentStation = railgraphHandler.HierarchyRelations.GetStationBySysID(this.currentStationId);

                        if (!this.stations.ContainsKey(stationId))
                        {
                            this.stations.Add(stationId, new Station(station));
                        }
                        if (this.m_eSearchDir == RailgraphLib.Enums.EDirection.dNominal)
                        {
                            if (this.stations[stationId].StationsToNominal.Count == 0)
                            {
                                if (this.stations.ContainsKey(this.currentStationId) && !this.stations[this.currentStationId].StationsToNominal.Contains(station))
                                    this.stations[this.currentStationId].StationsToNominal.Add(station);
                                if (this.stations.ContainsKey(stationId) && !this.stations[stationId].StationsToOpposite.Contains(currentStation))
                                    this.stations[stationId].StationsToOpposite.Add(currentStation);
                            }
                        }
                        else
                        {
                            if (this.stations[stationId].StationsToOpposite.Count == 0)
                            {
                                if (this.stations.ContainsKey(this.currentStationId) && !this.stations[this.currentStationId].StationsToOpposite.Contains(station))
                                    this.stations[this.currentStationId].StationsToOpposite.Add(station);
                                if (this.stations.ContainsKey(stationId) && !this.stations[stationId].StationsToNominal.Contains(currentStation))
                                    this.stations[stationId].StationsToNominal.Add(currentStation);
                            }
                        }
                        this.currentStationId = stationId;
                    }
                }

                return EConditionalProceed.cpContinue;
            }
        }

        public void BuildStationNetwork()
        {
            // Create station "network" on simple track layout without station branches, like RigaJunction
            // If more complex track layout with station branches (station has several neighbor stations on same side), code in StationFindCondition must be re-thinked

            Dictionary<UInt32, Station> networkedStations = new();

            // First create initial stations
            foreach (var ctcStation in HierarchyRelations.Stations)
                networkedStations.Add(ctcStation.SysID, new Station(ctcStation));

            // Start track for searches, has to be platform track
            var platform = networkedStations.Values.First().CTCStation.Platforms.First();
            var track = (RailgraphLib.Interlocking.Track)ILTopoGraph.getGraphObj(platform.Tracks.First().SysID);

            StationFindCondition rCondition;
            List<RailgraphLib.FindCondition.FindResult> rFindResultVector;
            RailgraphLib.TopoGraph.ETerminationReason termReason;

            // Traverse everything into nominal direction
            rCondition = new(this, ref networkedStations, track.getId(), RailgraphLib.Enums.EDirection.dNominal);
            rFindResultVector = new();
            termReason = ILTopoGraph.findPath(rCondition, rFindResultVector);
            Assert(termReason != RailgraphLib.TopoGraph.ETerminationReason.ok);     // The search will "fail", because the condition never returns EConditionalProceed.cpFound

            // Traverse everything into opposite direction
            rCondition = new(this, ref networkedStations, track.getId(), RailgraphLib.Enums.EDirection.dOpposite);
            rFindResultVector = new();
            termReason = ILTopoGraph.findPath(rCondition, rFindResultVector);
            Assert(termReason != RailgraphLib.TopoGraph.ETerminationReason.ok);     // The search will "fail", because the condition never returns EConditionalProceed.cpFound

            Log.Information($"Station network resolved from RailGraph");

            // Resolve entry and exit routes to station platforms
            foreach (var station in networkedStations.Values)
                ResolveEntryExitRoutesOfStation(station);

            Log.Information($"Station entry and exit routes resolved from RailGraph");

            // Now stations have all adjacencies and entry/exit routes
            this.dataHandler.CreateStationNetwork(networkedStations);

            Log.Debug($"---------------------------------------");

            foreach (var station in this.dataHandler.Stations.Values)
            {
                Log.Debug($"Station {station.CTCStation.SysName}:");
                Log.Debug($"  Previous stations ({station.StationsToOpposite.Count}):");
                foreach (var stat in station.StationsToOpposite)
                    Log.Debug($"    {stat.SysName}");
                Log.Debug($"  Next stations ({station.StationsToNominal.Count}):");
                foreach (var stat in station.StationsToNominal)
                    Log.Debug($"    {stat.SysName}");
                Log.Debug($"  Platforms and km-values:");
                foreach (var pl in station.CTCStation.Platforms)
                    Log.Debug($"    {pl.SysName}: {GetElementPositionOfPlatform(pl).AdditionalPos} mm");
                Log.Debug($"  Total # of routes ({station.CTCStation.Routes.Count}):");
                Log.Debug($"  Entry routes ({station.PlatformEntryRoutes.Count}):");
                foreach (var routeInfo in station.PlatformEntryRoutes)
                {
                    foreach (var targetPlatform in routeInfo.Value)
                        Log.Debug($"    {routeInfo.Key.SysName} : {targetPlatform.SysName}");
                }
                Log.Debug($"  Exit routes ({station.PlatformExitRoutes.Count}):");
                foreach (var routeInfo in station.PlatformExitRoutes)
                {
                    foreach (var targetPlatform in routeInfo.Value)
                        Log.Debug($"    {routeInfo.Key.SysName} : {targetPlatform.SysName}");
                }
                Log.Debug($"  Other routes ({station.OtherRoutes.Count}):");
                foreach (var route in station.OtherRoutes)
                {
                    Log.Debug($"    {route.SysName}");
                }
                if (station.PlatformEntryRoutes.Keys.Count + station.PlatformExitRoutes.Keys.Count + station.OtherRoutes.Count != station.CTCStation.Routes.Count)
                    Log.Debug($"    ***************************************************************** ERROR IN ROUTES (count does not match)");
            }
        }

        private void ResolveEntryExitRoutesOfStation(Station station)
        {
            RailgraphLib.FindCondition.FindAllCondition rCondition;
            List<RailgraphLib.FindCondition.FindResult> rFindResultVector;
            RailgraphLib.TopoGraph.ETerminationReason termReason;

            foreach (var platform in station.CTCStation.Platforms)
            {
                if (platform.Tracks.Count > 0)
                {
                    var platformTrackId = platform.Tracks.First().SysID;

                    foreach (var route in station.CTCStation.Routes)
                    {
                        var routeDir = route.EDir;

                        // First try to resolve as entry route. If platform track is found inside route path's last edge, route leads to platform
                        rCondition = new(route.BPID, route.EPID, routeDir);
                        rCondition.addViaElements(route.GetPoints().Select(p => (uint)p.SysID).ToList());
                        rFindResultVector = new();
                        termReason = this.ILTopoGraph.findPath(rCondition, rFindResultVector);

                        bool foundRoute = false;

                        if (termReason == RailgraphLib.TopoGraph.ETerminationReason.ok)
                        {
                            foreach (var result in rFindResultVector)
                            {
                                if (result.getResult().Contains(platformTrackId))
                                {
                                    var lastObj = this.ILTopoGraph.getGraphObj(result.getResult().Last());
                                    if (lastObj != null)
                                    {
                                        var coreObj = this.ILTopoGraph.getCoreObj(lastObj.getCoreId());
                                        if (coreObj != null && coreObj.getAssociatedObjects().Contains(platformTrackId))
                                        {
                                            if (!station.PlatformEntryRoutes.ContainsKey(route))
                                            {
                                                List<RailgraphLib.HierarchyObjects.Platform> platforms = new();
                                                station.PlatformEntryRoutes.Add(route, platforms);
                                            }
                                            station.PlatformEntryRoutes[route].Add(platform);
                                            foundRoute = true;
                                            break; // Only one resulting path is needed
                                        }
                                    }
                                }
                            }
                        }

                        if (!foundRoute)
                        {
                            // Route was not entry route, could it be exit route? If route's begin signal is on same edge than platform track, route is taken as exit route
                            var trackObj = this.ILTopoGraph.getGraphObj(platformTrackId);
                            if (trackObj != null)
                            {
                                var coreObj = this.ILTopoGraph.getCoreObj(trackObj.getCoreId());
                                if (coreObj != null && coreObj.getAssociatedObjects().Contains(route.BPID))
                                {
                                    if (!station.PlatformExitRoutes.ContainsKey(route))
                                    {
                                        List<RailgraphLib.HierarchyObjects.Platform> platforms = new();
                                        station.PlatformExitRoutes.Add(route, platforms);
                                    }
                                    station.PlatformExitRoutes[route].Add(platform);
                                }
                            }
                        }
                    }
                }
            }

            // Add other routes (path through station/platforms, etc)
            foreach (var route in station.CTCStation.Routes)
            {
                if (!station.PlatformEntryRoutes.ContainsKey(route) && !station.PlatformExitRoutes.ContainsKey(route))
                    station.OtherRoutes.Add(route);
            }
        }

        public ElementPosition GetElementPositionOfPlatform(RailgraphLib.HierarchyObjects.Platform? platform)
        {
            if (platform != null)
            {
                var trackExtension = new RailgraphLib.RailExtension.TrackExtension(0, 0, platform.Tracks.Select(t => (uint)t.SysID).ToList());
                RailgraphLib.RailExtension.CoreExtension coreExtension = new();

                if (this.RailGraph?.getConverter().convertToCoreExtension(trackExtension, ref coreExtension, this.ILTopoGraph) == RailgraphLib.Enums.EConversionResult.crOk)
                {
                    // Platform should always be only on one edge!
                    if (coreExtension.getExtensionElements().Count == 1)
                    {
                        var edgeId = coreExtension.getExtensionElements().First();
                        var edge = this.ILTopoGraph.getCoreObj(edgeId);
                        if (edge != null)
                        {
                            var centerOffsetOfPlatformTracks = coreExtension.getStartDistance() + (edge.getLength() - coreExtension.getStartDistance() - coreExtension.getEndDistance()) / 2;
                            var kmValue = getKmValue(edge, centerOffsetOfPlatformTracks);

                            return new ElementPosition(edge.getName(), (uint)coreExtension.getStartDistance(), kmValue, platform.SysName);
                        }
                    }
                }
            }

            return new ElementPosition();
        }

        public RailgraphLib.HierarchyObjects.Platform? GetPlatform(string platformName)
        {
            return this.hierarchyRelations?.GetPlatformByName(platformName);
        }

        public RailgraphLib.GraphObj? GetILElementOnElementPosition(ElementPosition pos)
        {
            if (pos != null)
            {
                // Element position is core element position
                if (this.coreObjectsByName.ContainsKey(pos.ElementId))
                {
                    var coreObj = this.coreObjectsByName[pos.ElementId];
                    var coreExtension = new RailgraphLib.RailExtension.CoreExtension((int)pos.Offset, (int)(coreObj.getLength() - pos.Offset), new List<uint>() { coreObj.getId() });
                    RailgraphLib.RailExtension.ElementExtension trackExtension = new RailgraphLib.RailExtension.TrackExtension();

                    this.RailGraph.getConverter().convertToExtension(coreExtension, ref trackExtension, this.ILTopoGraph);

                    for (int i = 0; i < trackExtension.getExtensionElementsRaw().Count; i++)
                    {
                        var ilElement = this.ILTopoGraph.getGraphObj(trackExtension.getExtensionElementsRaw()[i]);
                        if (ilElement != null && (ilElement is RailgraphLib.Interlocking.Track || ilElement is RailgraphLib.Interlocking.PointLeg))
                            return ilElement;
                    }
                }
            }

            return null;
        }

        private int getKmValue(RailgraphLib.CoreObj coreObj, int offset)
        {
            int kmValue = 0;

            // This will work after OFFSETSECTION table has been filled in SA!
            if (!RailgraphLib.KmOffsetSection.convertElementOffsetToSectionValue(coreObj.getId(), offset, ref kmValue))
            {
                kmValue = coreObj.getDistanceFromInitPoint() + offset;  // This is used until the above SA work has been done!
            }

            return kmValue;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // RailGraph testing (various RailgraphLib tests based on RigaJunction rail network, left here for reference)
        ////////////////////////////////////////////////////////////////////////////////
        public void TestRailgraph()
        {
            //TestFindPaths();
            //TestRailgraphRoutes();
            //TestConversions();
            //IterateCoreObjects();
        }

        public class MyFindCondition : RailgraphLib.FindCondition.FindAllCondition
        {
            private List<UInt32>? path = null;
            private int discarded = 0;
            RailgraphHandler railgraphHandler;

            public MyFindCondition(RailgraphHandler railgraphHandler, UInt32 from, UInt32 target, RailgraphLib.Enums.EDirection eSearchDir) : base(from, target, eSearchDir)
            {
                this.railgraphHandler = railgraphHandler;
            }

            public override EConditionalProceed isConditionFound(UInt32 current, UInt32 previous)
            {
                var getGraphName = (UInt32 id) =>
                {
                    var graphObj = this.railgraphHandler.ILTopoGraph.getGraphObj(id);
                    if (graphObj != null)
                        return graphObj.getName();
                    return "";
                };

                var getCoreName = (UInt32 id) =>
                {
                    var graphObj = this.railgraphHandler.ILTopoGraph.getGraphObj(id);
                    if (graphObj != null)
                    {
                        var coreObj = this.railgraphHandler.ILTopoGraph.getCoreObj(graphObj.getCoreId());
                        if (coreObj != null)
                            return coreObj.getName();
                    }
                    return "";
                };

                var addElement = (UInt32 current, UInt32 previous) =>
                {
                    var graphName = getGraphName(current);
                    var currentCoreName = getCoreName(current);
                    var previousCoreName = getCoreName(previous);

                    this.path.Add(current);
                    if (currentCoreName != previousCoreName)
                        Log.Debug($"---> MyFindCondition: searching {previous} -> {current} '{graphName}' on core element '{currentCoreName}'");
                    else
                        Log.Debug($"---> MyFindCondition: searching {previous} -> {current} '{graphName}'");
                };

                if (this.path == null)
                {
                    this.path = new();
                    this.path.Add(current);
                    Log.Debug($"---> MyFindCondition: starting from {current} '{getGraphName(current)}'");
                }
                else if (this.path.Last() == previous)
                {
                    addElement(current, previous);
                }
                else if (this.path.Contains(previous))
                {
                    var count = this.path.Count;
                    var idxPrevious = this.path.IndexOf(previous);

                    var discardedPath = this.path.GetRange(idxPrevious + 1, count - idxPrevious - 1);
                    discardedPath.Reverse();

                    foreach (var item in discardedPath)
                    {
                        Log.Debug($"---> MyFindCondition: discarded {item} '{getGraphName(item)}' on core element '{getCoreName(item)}'");
                    }
                    Log.Debug($"---> MyFindCondition: amount of unsuccessful searches {++this.discarded}");

                    this.path = this.path.GetRange(0, idxPrevious + 1);
                    addElement(current, previous);
                }
                else
                {
                    Log.Debug($"---> MyFindCondition: shouldn't happen {previous} <-> {current}");
                }

                if (current == m_target)
                {
                    Log.Debug($"---> MyFindCondition: FOUND - Final path:");
                    foreach (var item in this.path)
                    {
                        Log.Debug($"---> MyFindCondition: {item} '{getGraphName(item)}' on core element '{getCoreName(item)}'");
                    }
                    //return EConditionalProceed.cpFoundAndContinue;
                    return EConditionalProceed.cpFound;
                }

                return EConditionalProceed.cpContinue;
            }
        }

        private void TestFindPaths()
        {
            RailgraphLib.FindCondition.FindAllCondition rCondition;
            List<RailgraphLib.FindCondition.FindResult> rFindResultVector;
            RailgraphLib.FindCondition.FindLogicalCondition rLogicalCondition;
            RailgraphLib.FindCondition.FindResult rFindResult;
            RailgraphLib.TopoGraph.ETerminationReason termReason;

            // Test findPath() from SIP2_VEC (1630) to SI10_VEC (258698) (begin and end signals of route SIP2_VEC-SI10_VEC)
            // One path should be found
            rCondition = new(1630, 258698, RailgraphLib.Enums.EDirection.dNominal);
            rFindResultVector = new();
            termReason = ILTopoGraph.findPath(rCondition, rFindResultVector);
            Assert(termReason == RailgraphLib.TopoGraph.ETerminationReason.ok && rFindResultVector.Count == 1);

            // Test findPath() from SIP_VEC (1625) to SI10_VEC (258698) (begin and end signals of route SIP_VEC-SI10_VEC)
            // Two paths should be found!
            rCondition = new(1625, 258698, RailgraphLib.Enums.EDirection.dNominal);
            rFindResultVector = new();
            termReason = ILTopoGraph.findPath(rCondition, rFindResultVector);
            Assert(termReason == RailgraphLib.TopoGraph.ETerminationReason.ok && rFindResultVector.Count == 2);

            // Test findLogicalPath() from SIP_VEC (1625) to SI10_VEC (258698) (begin and end signals of route SIP_VEC-SI10_VEC)
            // After "Basic Update" in TrainingSim one result should be found!
            //rLogicalCondition = new(1625, 258698, RailgraphLib.Enums.EDirection.dNominal);
            //rFindResult = new();
            //Assert(ILTopoGraph.findLogicalPath(ref rLogicalCondition, ref rFindResult) && rFindResult.getResult().Count == 21);

            // Test findPath() from SIP2_VEC (1630) to SI10_VEC (258698) (begin and end signals of route SIP2_VEC-SI10_VEC) with own condition
            /*
            MyFindCondition rMyCondition = new(this, 1630, 258698, RailgraphLib.Enums.EDirection.dNominal);
            rMyCondition.setSearchDepth(1000);
            rFindResultVector = new();
            termReason = this.ilTopoGraph.findPath(rMyCondition, rFindResultVector);
            Assert(termReason == RailgraphLib.TopoGraph.ETerminationReason.ok && rFindResultVector.Count == 1);
            */
        }

        private void TestRailgraphRoutes()
        {
            RailgraphLib.FindCondition.FindAllCondition rCondition;
            List<RailgraphLib.FindCondition.FindResult> rFindResultVector;
            RailgraphLib.TopoGraph.ETerminationReason termReason;

            Log.Debug($"============================================================================");
            Log.Debug($"Searching paths of {HierarchyRelations.Routes.Count} routes");
            int found = 0;
            int failed = 0;
            int foundToOtherDirection = 0;
            Dictionary<int, int> amountPaths = new();
            Dictionary<int, int> amountPathsOtherDirection = new();
            List<RailgraphLib.HierarchyObjects.Route> failedRoutes = new();
            foreach (var route in HierarchyRelations.Routes)
            {
                var stationId = route.Station.SysID;
                var stationName = route.Station.SysName;

                rCondition = new(route.BPID, route.EPID, route.EDir);
                rCondition.addViaElements(route.GetPoints().Select(p => (uint)p.SysID).ToList());
                rFindResultVector = new();
                termReason = this.ilTopoGraph.findPath(rCondition, rFindResultVector);
                if (termReason == RailgraphLib.TopoGraph.ETerminationReason.ok)
                {
                    found++;
                    if (amountPaths.ContainsKey(rFindResultVector.Count))
                        amountPaths[rFindResultVector.Count]++;
                    else
                        amountPaths.Add(rFindResultVector.Count, 1);
                    Log.Debug($"      OK ---> Route {route.SysName} on {stationName} - Paths found: {rFindResultVector.Count}");
                }
                else
                {
                    rCondition = new(route.BPID, route.EPID, route.EDir == RailgraphLib.Enums.EDirection.dNominal ? RailgraphLib.Enums.EDirection.dOpposite : RailgraphLib.Enums.EDirection.dNominal);
                    rCondition.addViaElements(route.GetPoints().Select(p => (uint)p.SysID).ToList());
                    rFindResultVector = new();
                    termReason = this.ilTopoGraph.findPath(rCondition, rFindResultVector);
                    if (termReason == RailgraphLib.TopoGraph.ETerminationReason.ok)
                    {
                        foundToOtherDirection++;
                        Log.Debug($"DB ERROR ---> Route {route.SysName} on {stationName} - Found to other direction. Paths found: {rFindResultVector.Count}");
                        if (amountPathsOtherDirection.ContainsKey(rFindResultVector.Count))
                            amountPathsOtherDirection[rFindResultVector.Count]++;
                        else
                            amountPathsOtherDirection.Add(rFindResultVector.Count, 1);
                    }
                    else
                    {
                        failed++;
                        failedRoutes.Add(route);
                        Log.Debug($"    FAIL ---> Route {route.SysName} on {stationName} - No paths found: {route.BPID} -> {route.EPID} ({route.EDir})");
                    }
                }
            }

            // Report of search
            Log.Debug($"Found paths for {found} routes");
            Log.Debug($"Found paths to other direction for {foundToOtherDirection} routes (route direction error in DB)");
            Log.Debug($"Failed to find paths for {failed} routes");
            int total = 0, count = 0;
            Log.Debug($"Amount of paths in found routes:");
            foreach (var paths in amountPaths)
            {
                Log.Debug($"  {paths.Key} paths : {paths.Value} routes");
                count += paths.Key * paths.Value;
            }
            total += count;
            count = 0;
            Log.Debug($"Amount of paths in routes to other direction:");
            foreach (var paths in amountPathsOtherDirection)
            {
                Log.Debug($"  {paths.Key} paths : {paths.Value} routes");
                count += paths.Key * paths.Value;
            }
            total += count;
            Log.Debug($"Failed routes:");
            foreach (var route in failedRoutes)
                Log.Debug($"  {route.SysName} ({route.SysID}): {route.BPID} -> {route.EPID} ({route.EDir})");
            Log.Debug($"Total number of paths found: {total}");
            Log.Debug($"============================================================================");
        }

        private void TestConversions()
        {
            int startDistance = 100000;
            int endDistance = 200000;
            var trackList = new List<uint>() { 1833, 1048, 1326, 1050, 1053, 1329, 1051, 1056, 1331, 1054, 1836, 1881, 1860 };

            var trackExtension = new RailgraphLib.RailExtension.TrackExtension(startDistance, endDistance, trackList);
            RailgraphLib.RailExtension.CoreExtension coreExtension = new();
            RailgraphLib.RailExtension.ElementExtension trackExtension2 = new RailgraphLib.RailExtension.TrackExtension();

            this.RailGraph.getConverter().convertToCoreExtension(trackExtension, ref coreExtension, this.ILTopoGraph);
            this.RailGraph.getConverter().convertToExtension(coreExtension, ref trackExtension2, this.ILTopoGraph);

            Assert(trackExtension.getStartDistance() == trackExtension2.getStartDistance());
            Assert(trackExtension.getEndDistance() == trackExtension2.getEndDistance());
            Assert(trackExtension.getExtensionElementsRaw().Count == trackExtension2.getExtensionElementsRaw().Count);
            for (int i = 0; i < trackExtension.getExtensionElementsRaw().Count; i++)
                Assert(trackExtension.getExtensionElementsRaw()[i] == trackExtension2.getExtensionElementsRaw()[i]);
        }

        private void LogCoreObjInfo(uint coreObjId)
        {
            var coreObj = this.ILTopoGraph.getCoreObj(coreObjId);
            if (coreObj != null)
            {
                Log.Information($"    \"{coreObj.getName()}\"");
            }
        }

        public void IterateCoreObjects()
        {
            Log.Information($"Core objects:");
            this.ILGraph.iterateAllCoreObjectsAndCallMethod(LogCoreObjInfo);
        }
    }
}
