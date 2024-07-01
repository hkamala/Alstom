using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Diagnostics.Debug;
using Serilog;
using RailgraphLib;
using RailgraphLib.Interlocking;
using RailgraphLib.Enums;
using Cassandra.DataStax.Graph;
using RailgraphLib.armd;
using RailgraphLib.FindCondition;
using System.Data.Odbc;
using RailgraphLib.RailExtension;

namespace ConflictManagementService.Model
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

            Log.Information($"Connecting to Solid DB");
            RailgraphLib.SolidDB.CSolidEntryPoint solidDB = new();
            if (!solidDB.Connect())
            {
                throw new Exception("Couldn't connect to Solid DB");
            }

            CreateRailGraph(solidDB);
            BuildStationNetwork(solidDB);

            Log.Information($"Closing connection to Solid DB");
            solidDB.CloseConnection();

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
        private void CreateRailGraph(RailgraphLib.SolidDB.CSolidEntryPoint solidDB)
        {
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

                setSearchDepth(1000);   // This is enough in RigaJunction, but maybe not in larger networks
            }

            public override EConditionalProceed isConditionFound(UInt32 current, UInt32 previous)
            {
                if (railgraphHandler.ILTopoGraph?.getGraphObj(current) is RailgraphLib.Interlocking.Track)
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

        public void BuildStationNetwork(RailgraphLib.SolidDB.CSolidEntryPoint solidDB)
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

            // Resolve control areas for stations
            if (ResolveControlAreasForStations(solidDB, networkedStations))
                Log.Information($"Control areas for stations resolved");

            // Now stations have all adjacencies and entry/exit routes
            this.dataHandler.CreateStationNetwork(networkedStations);

            Log.Debug($"---------------------------------------");

            foreach (var station in this.dataHandler.Stations.Values)
            {
                Log.Debug($"Station {station.CTCStation.SysName} (control area SYSID {station.ControlAreaSysId}):");
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

        private bool ResolveControlAreasForStations(RailgraphLib.SolidDB.CSolidEntryPoint solidDB, Dictionary<uint, Station> networkedStations)
        {
            try
            {
                using (OdbcCommand cmd = solidDB.OdbcConnection!.CreateCommand())
                {
                    cmd.CommandText = $"select h.masterId, h.sysid from hierarchy h "
                    + $"left join sysobj s on s.sysid = h.masterid "
                    + $"left join sysobj s2 on s2.sysid = h.sysid "
                    + $"where h.hierarchytype = 2 and s.objtypeno = 130 and s2.objtypeno = 93 and h.versionno = {solidDB.DbVersion}";

                    OdbcDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        var stationId = (uint) reader.GetInt32(0);
                        var controlAreaId = (uint) reader.GetInt32(1);

                        if (networkedStations.Keys.Contains(stationId))
                        {
                            ////////////////////////////////////////////////////////////////////////////////////
                            /// WARNING! HARD-CODING FOR RIGA JUNCTION MANGALI STATION, WHICH CONTAINS TWO CONTROL AREAS
                            /// ONLY AREA A IS SET TO STATION BECAUSE IT IS IN TMS CONTROLLED AREA
                            if (controlAreaId == 9007)
                                continue;
                            ////////////////////////////////////////////////////////////////////////////////////

                            networkedStations[stationId].ControlAreaSysId = controlAreaId;
                        }
                    }

                    reader.Close();
                }
            }
            catch (OdbcException)
            {
                Log.Error($"Resolving control areas for stations failed");
                return false;
            }
            catch (Exception)
            {
                Log.Error($"Resolving control areas for stations failed");
                return false;
            }

            return true;
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
                            uint centerOffsetOfPlatformTracks = (uint) (coreExtension.getStartDistance() + (edge.getLength() - coreExtension.getStartDistance() - coreExtension.getEndDistance()) / 2);
                            var kmValue = GetKmValue(edge, centerOffsetOfPlatformTracks);

                            return new ElementPosition(edge.getName(), (uint)coreExtension.getStartDistance(), kmValue, platform.SysName);
                        }
                    }
                }
            }

            return new ElementPosition();
        }

        public RailgraphLib.RailExtension.TrackExtension? GetTrackExtensionOfRoute(RailgraphLib.HierarchyObjects.Route route)
        {
            RailgraphLib.RailExtension.TrackExtension? trackExtension = null;

            try
            {
                RailgraphLib.FindCondition.FindAllCondition rCondition;
                List<RailgraphLib.FindCondition.FindResult> rFindResultVector;
                RailgraphLib.TopoGraph.ETerminationReason termReason;

                var stationId = route.Station.SysID;
                var stationName = route.Station.SysName;

                List<uint> viaElements = route.GetPoints().Select(p => (uint)p.SysID).ToList();
                viaElements.AddRange(route.GetFocusObjects());

                rCondition = new(route.BPID, route.EPID, route.EDir);
                rCondition.addViaElements(viaElements);
                rFindResultVector = new();
                termReason = this.ilTopoGraph.findPath(rCondition, rFindResultVector);
                if (termReason == RailgraphLib.TopoGraph.ETerminationReason.ok && rFindResultVector.Count > 0)
                {
                    // Take first result (there may be others, because via elements are points/switches and there may not be focus objects defined)
                    //TODO: later: bool endDirChanges = rFindResultVector.First().getDirectionChangeCountInPath() % 2 == 0 ? false : true;
                    trackExtension = new RailgraphLib.RailExtension.TrackExtension(0, 0, rFindResultVector.First().getResult().ToList(), route.EDir, route.EDir);
                }
                else
                {
                    // Route may sometimes be defined into wrong direction...
                    RailgraphLib.Enums.EDirection searchDir = route.EDir == RailgraphLib.Enums.EDirection.dNominal ? RailgraphLib.Enums.EDirection.dOpposite : RailgraphLib.Enums.EDirection.dNominal;

                    rCondition = new(route.BPID, route.EPID, searchDir);
                    rCondition.addViaElements(viaElements);
                    rFindResultVector = new();
                    termReason = this.ilTopoGraph.findPath(rCondition, rFindResultVector);
                    if (termReason == RailgraphLib.TopoGraph.ETerminationReason.ok && rFindResultVector.Count > 0)
                    {
                        // Take first result (there may be others, because via elements are points/switches and there may not be focus objects defined)
                        //TODO: later: bool endDirChanges = rFindResultVector.First().getDirectionChangeCountInPath() % 2 == 0 ? false : true;
                        trackExtension = new RailgraphLib.RailExtension.TrackExtension(0, 0, rFindResultVector.First().getResult().ToList(), searchDir, searchDir);
                    }
                }
            }
            catch (Exception)
            {
            }

            return trackExtension;
        }
     
        public RailgraphLib.HierarchyObjects.Platform? GetPlatform(string platformName)
        {
            return this.hierarchyRelations?.GetPlatformByName(platformName);
        }

        public RailgraphLib.CoreObj? GetCoreObjByName(string coreObjName)
        {
            if (this.coreObjectsByName.ContainsKey(coreObjName))
                return this.coreObjectsByName[coreObjName];

            return null;
        }

        public RailgraphLib.GraphObj? GetILElementOnElementPosition(ElementPosition pos)
        {
            if (pos != null)
            {
                try
                {
                    // Element position is core element position
                    var coreObj = GetCoreObjByName(pos.ElementId);
                    if (coreObj != null)
                    {
                        var coreExtension = new RailgraphLib.RailExtension.CoreExtension((int)pos.Offset, (int)(coreObj.getLength() - pos.Offset), new List<uint>() { coreObj.getId() });
                        RailgraphLib.RailExtension.ElementExtension trackExtension = new RailgraphLib.RailExtension.TrackExtension();

                        this.RailGraph.getConverter().convertToExtension(coreExtension, ref trackExtension, this.ILTopoGraph);

                        for (int i = 0; i < trackExtension.getExtensionElementsRaw().Count; i++)
                        {
                            var ilElement = this.ILTopoGraph.getGraphObj(trackExtension.getExtensionElementsRaw()[i]);
                            if (ilElement != null && (ilElement is RailgraphLib.Interlocking.Track || ilElement is RailgraphLib.Interlocking.PointLeg || ilElement is RailgraphLib.Interlocking.Point))
                                return ilElement;
                        }
                    }
                }
                catch(Exception) { }
            }

            return null;
        }

        public RailgraphLib.Enums.EDirection GetElementExtensionDirection(ElementExtension extension)
        {
            RailgraphLib.Enums.EDirection dir = RailgraphLib.Enums.EDirection.dUnknown;

            try
            {
                RailgraphLib.FindCondition.FindAllCondition rCondition;
                List<RailgraphLib.FindCondition.FindResult> rFindResultVector;
                RailgraphLib.TopoGraph.ETerminationReason termReason;

                var startIlElement = GetILElementOnElementPosition(extension.StartPos)?.getId();
                var endIlElement = GetILElementOnElementPosition(extension.EndPos)?.getId();

                if (startIlElement != null && endIlElement != null)
                {
                    rCondition = new((uint)startIlElement, (uint)endIlElement, RailgraphLib.Enums.EDirection.dNominal);
                    rFindResultVector = new();
                    termReason = this.ILTopoGraph.findPath(rCondition, rFindResultVector);
                    if (termReason == RailgraphLib.TopoGraph.ETerminationReason.ok && rFindResultVector.Count >= 1)
                    {
                        dir = RailgraphLib.Enums.EDirection.dNominal;
                    }
                    else
                    {
                        rCondition = new((uint)startIlElement, (uint)endIlElement, RailgraphLib.Enums.EDirection.dOpposite);
                        rFindResultVector = new();
                        termReason = ILTopoGraph.findPath(rCondition, rFindResultVector);
                        if (termReason == RailgraphLib.TopoGraph.ETerminationReason.ok && rFindResultVector.Count >= 1)
                            dir = RailgraphLib.Enums.EDirection.dOpposite;
                    }
                }
            }
            catch(Exception) { }

            return dir;
        }

        public RailgraphLib.RailExtension.CoreExtension? CreateCoreExtension(ElementExtension extension)
        {
            RailgraphLib.RailExtension.CoreExtension? coreExtension = null;

            try
            {
                RailgraphLib.Enums.EDirection dir = GetElementExtensionDirection(extension);
                if (dir == RailgraphLib.Enums.EDirection.dNominal || dir == RailgraphLib.Enums.EDirection.dOpposite)
                {
                    RailgraphLib.CoreObj? startCoreObj = GetCoreObjByName(extension.StartPos.ElementId);
                    RailgraphLib.CoreObj? endCoreObj = GetCoreObjByName(extension.EndPos.ElementId);

                    if (startCoreObj != null && endCoreObj != null)
                    {
                        List<uint> elements = new();
                        foreach (var elementName in extension.Elements)
                        {
                            var obj = GetCoreObjByName(elementName);
                            if (obj != null)
                                elements.Add(obj.getId());
                        }

                        if (dir == RailgraphLib.Enums.EDirection.dNominal)
                            coreExtension = new RailgraphLib.RailExtension.CoreExtension((int)extension.StartPos.Offset, (int)(endCoreObj.getLength() - extension.EndPos.Offset), elements);
                        else
                            coreExtension = new RailgraphLib.RailExtension.CoreExtension((int)(startCoreObj.getLength() - extension.StartPos.Offset), (int)extension.EndPos.Offset, elements);
                    }
                }
            }
            catch(Exception) { }

            return coreExtension;
        }

        public RailgraphLib.RailExtension.TrackExtension? CreateTrackExtension(ElementExtension extension)
        {
            RailgraphLib.RailExtension.ElementExtension? ext = null;
            try
            {
                RailgraphLib.RailExtension.CoreExtension? coreExtension = CreateCoreExtension(extension);
                if (coreExtension != null)
                {
                    ext = new RailgraphLib.RailExtension.TrackExtension();
                    this.RailGraph.getConverter().convertToExtension(coreExtension, ref ext, this.ILTopoGraph);
                }
            }
            catch(Exception) { }

            return ext == null ? null : new RailgraphLib.RailExtension.TrackExtension(ext.getStartDistance(), ext.getEndDistance(), ext.getExtensionElements().ToList(), ext.getStartDirection(), ext.getEndDirection());
        }

        public RailgraphLib.RailExtension.CoreExtension? GetCoreExtensionOfRoute(RailgraphLib.HierarchyObjects.Route route)
        {
            RailgraphLib.RailExtension.CoreExtension? coreExtension = null;

            var trackExtension = GetTrackExtensionOfRoute(route);
            if (trackExtension != null)
            {
                coreExtension = new();
                this.RailGraph!.getConverter().convertToCoreExtension(trackExtension, ref coreExtension, this.ILTopoGraph);
            }

            return coreExtension;
        }

        public List<RailgraphLib.RailExtension.CoreExtension> GetCoreExtensionsOfRouteTracks(RailgraphLib.HierarchyObjects.Route route)
        {
            List<RailgraphLib.RailExtension.CoreExtension> coreExtensions = new();

            var trackExtension = GetTrackExtensionOfRoute(route);
            if (trackExtension != null)
            {
                foreach (var elementId in trackExtension.getExtensionElements())
                { 
                    if (ILTopoGraph?.getGraphObj(elementId) is TrackSection track)
                    {
                        RailgraphLib.RailExtension.TrackExtension oneTrackExtension = new(0, 0, new List<uint> {elementId}, trackExtension.getStartDirection(), trackExtension.getEndDirection());
                        RailgraphLib.RailExtension.CoreExtension coreExtension = new();
                        this.RailGraph!.getConverter().convertToCoreExtension(oneTrackExtension, ref coreExtension, this.ILTopoGraph);

                        coreExtensions.Add(coreExtension);
                    }
                }
            }

            return coreExtensions;
        }

        public ElementPosition CreateElementPosition(string element, uint offset, bool offsetToNominal)
        {
            ElementPosition position = new();

            var coreObj = GetCoreObjByName(element);

            if (coreObj != null)
            {
                uint offsetOnCore = (uint) (offsetToNominal ? offset : coreObj.getLength() - offset);
                if (offsetOnCore >= 0)
                {
                    var kmValue = GetKmValue(coreObj, offsetOnCore);
                    position = new ElementPosition(element, offsetOnCore, kmValue);
                }
            }

            return position;
        }

        private int GetKmValue(RailgraphLib.CoreObj coreObj, uint offset)
        {
            int kmValue = 0;

            // This will work after OFFSETSECTION table has been filled in SA!
            if (!RailgraphLib.KmOffsetSection.convertElementOffsetToSectionValue(coreObj.getId(), (int) offset, ref kmValue))
            {
                kmValue = (int) (coreObj.getDistanceFromInitPoint() + offset);  // This is used until the above SA work has been done!
            }

            return kmValue;
        }

        public bool isPointOutOfControl(RailgraphLib.Interlocking.Point point)
        {
            if (!ArmdPredefinedIf.isARMDValueInUse(ArmdPredefinedIf.getSwitchDirMask()))
                return false;

            return (ILGraph.getDynamicBits(point.getId()) & ArmdPredefinedIf.getSwitchDirMask()) == ArmdPredefinedIf.getSwitchDirUnknownBits();
        }
        
        public bool isPointFalseOccupied(RailgraphLib.Interlocking.Point point)
        {
            var mask = Armd.getArmdObj("DynSwitchFalseOccupiedMask");
            if (!ArmdPredefinedIf.isARMDValueInUse(mask))
                return false;

            var setBits = Armd.getArmdObj("DynSwitchFalseOccupiedSet");
            return (ILGraph.getDynamicBits(point.getId()) & mask) == setBits;
        }

        public bool isTrackOutOfControl(RailgraphLib.Interlocking.Track track)
        {
            if (!ArmdPredefinedIf.isARMDValueInUse(ArmdPredefinedIf.getTSOccupationMask()))
                return false;

            return (ILGraph.getDynamicBits(track.getId()) & ArmdPredefinedIf.getTSOccupationMask()) == ArmdPredefinedIf.getTSOccupationUnknownBits();
        }

        public bool isTrackFalseOccupied(RailgraphLib.Interlocking.Track track)
        {
            var mask = Armd.getArmdObj("DynTSFalseOccupiedMask");
            if (!ArmdPredefinedIf.isARMDValueInUse(mask))
                return false;

            var setBits = Armd.getArmdObj("DynTSFalseOccupiedSet");
            return (ILGraph.getDynamicBits(track.getId()) & mask) == setBits;
        }

        // Point false occupied : bit 14 -> DynSwitchFalseOccupiedMask, DynSwitchFalseOccupiedSet, DynSwitchFalseOccupiedClear
        // Track false occupied : bit 16 -> DynTSFalseOccupiedMask, DynTSFalseOccupiedSet, DynTSFalseOccupiedClear

        ////////////////////////////////////////////////////////////////////////////////
        // RailGraph testing
        ////////////////////////////////////////////////////////////////////////////////
        public void TestRailgraph()
        {
            //TestFindPaths();
            //TestBlockings();
            //TestRouteTracks();
            //TestRailgraphRoutes();
            //TestConversions();
            //IterateCoreObjects();
            
            //foreach (var station in this.dataHandler.Stations.Values)
            //{
            //    bool raEnabled = IsAutomaticRoutingEnabledOnStation(station);
            //    Log.Information($"{station.CTCStation.SysName}: RA enabled: {raEnabled}");
            //}

            //var signal = ILGraph.getGraphObj("SIN1_VEC") as SignalOptical;
            //if (signal != null)
            //{
            //    var aspectState = signal.getAspectState();
            //}
        }

        public bool IsAutomaticRoutingEnabledOnStation(Station station)
        {
            var sharedMemory = RailgraphLib.SharedMemory.SharedMemory.Inst();
            ulong dynBits = 0;

            if (station.ControlAreaSysId != 0)
            {
                sharedMemory.GetDynBitsAsNumber(station.ControlAreaSysId, ref dynBits);

                var mask = Armd.getArmdObj("DynCtrlAreaRAMask");
                if (ArmdPredefinedIf.isARMDValueInUse(mask))
                {
                    var raEnabledBits = Armd.getArmdObj("DynCtrlAreaRAEnabled");
                    return (dynBits & mask) == raEnabledBits;
                }
            }

            return true;    // Stations without control areas (or if control area dynamic state is not in use) are consired RA enabled!
        }

        public void TestBlockings()
        {
            Log.Information($"TestBlockings:");

            string[] tracks = { "TSPPP_MAN", "TCCHAP_SKU" }; // TSPPP_MAN, TCCHAP_SKU: tracks: blocking and false occupancy set with track itself
            foreach (var track in tracks)
            {
                var t = this.ILGraph?.getGraphObj(track);
                bool blocked = ((RailgraphLib.Interlocking.Track)t).isBlocked();
                Log.Information($"###-----> Track {t.getName()} ({t.getId()}) blocked: {blocked}");
                bool falseOccupied = isTrackFalseOccupied((RailgraphLib.Interlocking.Track)t);
                Log.Information($"###-----> Track {t.getName()} ({t.getId()}) false occupied: {falseOccupied}");
                bool outOfControl = isTrackOutOfControl((RailgraphLib.Interlocking.Track)t);
                Log.Information($"###-----> Track {t.getName()} ({t.getId()}) out of control: {outOfControl}");
            }

            string[] points = { "PT68_MAN", "PT1_SAU" }; // PT68_MAN: point and point legs: blocking and false occupancy set with section TS68_MAN, PT1_SAU all set with point itself
            foreach (var point in points)
            {
                var t = this.ILGraph?.getGraphObj(point);
                bool blocked = ((RailgraphLib.Interlocking.Point)t).isBlocked();
                Log.Information($"###-----> Point {t.getName()} ({t.getId()}) blocked: {blocked}");
                bool falseOccupied = isPointFalseOccupied((RailgraphLib.Interlocking.Point)t);
                Log.Information($"###-----> Point {t.getName()} ({t.getId()}) false occupied: {falseOccupied}");
                bool outOfControl = isPointOutOfControl((RailgraphLib.Interlocking.Point)t);
                Log.Information($"###-----> Point {t.getName()} ({t.getId()}) out of control: {outOfControl}");
                bool operationBlocked = ((RailgraphLib.Interlocking.Point)t).isOperationBlocked();
                var manualState = ((RailgraphLib.Interlocking.Point)t).getManuallyLockedState();
                var position = ((RailgraphLib.Interlocking.Point)t).getPosition();
                Log.Information($"###-----> Point {t.getName()} ({t.getId()}) operation blocked: {operationBlocked}, manual state: {manualState}, position: {position}");

                string[] legs = { "_PL0", "_PL1", "_PL2" };
                foreach (string leg in legs)
                {
                    t = this.ILGraph?.getGraphObj(point+leg);
                    blocked = ((RailgraphLib.Interlocking.PointLeg)t).isBlocked();
                    Log.Information($"###-----> Point leg {t.getName()} ({t.getId()}) blocked: {blocked}");
                    falseOccupied = isTrackFalseOccupied((RailgraphLib.Interlocking.PointLeg)t);
                    Log.Information($"###-----> Point leg {t.getName()} ({t.getId()}) false occupied: {falseOccupied}");
                    outOfControl = isTrackOutOfControl((RailgraphLib.Interlocking.PointLeg)t);
                    Log.Information($"###-----> Point leg {t.getName()} ({t.getId()}) out of control: {outOfControl}");
                }
            }
        }

        private void TestRouteTracks()
        {
            Log.Information($"TestRouteTracks:");

            var Name = (uint elementId) => { return this.ILGraph?.getGraphObj(elementId).getName(); };

            foreach (var route in HierarchyRelations.Routes)
            {
                RailgraphLib.RailExtension.TrackExtension? te = GetTrackExtensionOfRoute(route);
                if (te != null)
                {
                    string tracks = "";
                    foreach (var track in te.getExtensionElementsRaw())
                    {
                        var element = this.ILGraph?.getGraphObj(track);
                        if (element is Track)
                            tracks += " " + element.getName();
                    }
                    Log.Information($"    Route {route.SysName} from signal {Name(route.BPID)} to signal {Name(route.EPID)} contains tracks:{tracks}");
                }
                else
                    Log.Information($"    Route {route.SysName} from signal {Name(route.BPID)} to signal {Name(route.EPID)}: tracks were not found!");
            }
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

            rCondition = new(1127, 1891, RailgraphLib.Enums.EDirection.dNominal);
            rFindResultVector = new();
            termReason = ILTopoGraph.findPath(rCondition, rFindResultVector);
            Assert(termReason == RailgraphLib.TopoGraph.ETerminationReason.ok && rFindResultVector.Count == 1);

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

        private void TestPointPositionsOnRoute()
        {
            foreach (var route in HierarchyRelations.Routes)
            {
                if (route.SysName == "SIP2_CAR-SI4_CAR")
                {
                    RailgraphLib.Interlocking.Point? point = GetFirstPointInWrongPositionOnRoute(route);
                    List<RailgraphLib.Interlocking.Point> points = GetAllPointsInWrongPositionOnRoute(route);
                }
            }
        }

        // Collect path of search until success or failure
        public class MyLogicalFindCondition : RailgraphLib.FindCondition.FindLogicalCondition
        {
            public List<UInt32> Path = new List<UInt32>();

            public MyLogicalFindCondition(RailgraphLib.HierarchyObjects.Route route) : base(route.BPID, route.EPID, route.EDir)
            {
            }

            override public EConditionalProceed isConditionFound(IReadOnlyList<UInt32> rPath)
            {
                Path = rPath.ToList();
                return base.isConditionFound(rPath);
            }
        };

        private RailgraphLib.Interlocking.Point? GetFirstPointInWrongPositionOnRoute(RailgraphLib.HierarchyObjects.Route route)
        {
            // Returns first point that is in wrong position on route
            FindLogicalCondition rMyLogicalFindCondition = new MyLogicalFindCondition(route);
            FindResult rFindResult = new();
            if ((bool) !ILTopoGraph?.findLogicalPath(ref rMyLogicalFindCondition, ref rFindResult))
            {
                // Last object must be point leg
                UInt32 pointLegID = ((MyLogicalFindCondition)rMyLogicalFindCondition).Path.LastOrDefault();
                if (ILTopoGraph?.getGraphObj(pointLegID) is PointLeg pointLeg)
                    return pointLeg.getPoint();
                else
                    ; // Don't log anything in here!
            }
            return null;
        }

        private List<RailgraphLib.Interlocking.Point> GetAllPointsInWrongPositionOnRoute(RailgraphLib.HierarchyObjects.Route route)
        {
            List<RailgraphLib.Interlocking.Point> points = new();
            var trackExtension = GetTrackExtensionOfRoute(route);
            if (trackExtension != null)
            {
                PointLeg? firstLeg = null;
                RailgraphLib.Interlocking.Point? point = null;

                foreach (var elementId in trackExtension.getExtensionElements())
                {
                    if (ILTopoGraph?.getGraphObj(elementId) is PointLeg pointLeg)
                    {
                        if (firstLeg == null)
                            firstLeg = pointLeg;
                        else
                        {
                            if (point != null && point.getRoutePosition(firstLeg.getId(), pointLeg.getId()) != point.getPosition())
                                points.Add(point);
                            firstLeg = null;
                            point = null;
                        }
                    }
                    else if (ILTopoGraph?.getGraphObj(elementId) is RailgraphLib.Interlocking.Point p && firstLeg != null)
                    {
                        point = p;
                    }
                    else
                    {
                        firstLeg = null;
                        point = null;
                    }
                }
            }
            return points;
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

        private void LogTSUIConfigInfo(uint coreObjId)
        {
            var coreObj = this.ILTopoGraph.getCoreObj(coreObjId);
            if (coreObj != null)
            {
                Log.Information($"    <GraphEdge Name=\"{coreObj.getName()}\" EdgeID=\"0\" Style=\"ET\"/>");
            }
        }

        public void IterateCoreObjects()
        {
            // Create info about TSUI distance graph core objects (to be added to TSUI config file)
            Log.Information($"<GraphEdges>");
            this.ILGraph.iterateAllCoreObjectsAndCallMethod(LogTSUIConfigInfo);
            Log.Information($"</GraphEdges>");
        }
    }
}
