using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using ConflictManagementLibrary.Logging;
using ConflictManagementLibrary.Network;
using Newtonsoft.Json;
using RailgraphLib;
using RailgraphLib.Enums;
using Track = RailgraphLib.Interlocking.Track;

namespace ConflictManagementLibrary.Management
{
    public  class RailGraphManager
    {
        private readonly IMyLogger theLogger;
        private readonly RailwayNetworkManager theRailwayNetworkManager;
        public RailgraphLib.NetworkCreator? RailGraph => this.railGraph;
        public RailgraphLib.Interlocking.ILTopoGraph? ILTopoGraph => this.ilTopoGraph;
        public RailgraphLib.Interlocking.ILGraph? ILGraph => this.ilGraph;
        public RailgraphLib.HierarchyObjects.HierarchyRelations? HierarchyRelations { get => this.hierarchyRelations; }
        private RailgraphLib.NetworkCreator? railGraph;
        private RailgraphLib.Interlocking.ILGraph? ilGraph;
        private RailgraphLib.Interlocking.ILTopoGraph? ilTopoGraph;
        private RailgraphLib.HierarchyObjects.HierarchyRelations? hierarchyRelations;
        private Dictionary<string, RailgraphLib.CoreObj> coreObjectsByName = new();
        private readonly Dictionary<uint, RailgraphLib.GraphObj> trackObjectsByUid = new();
        private readonly Dictionary<uint, RailgraphLib.CoreObj> edgeObjectsByUid = new();
        public bool RailGraphInitialized;
        public static RailGraphManager CreateInstance(IMyLogger theLogger, RailwayNetworkManager theRailwayNetworkManager)
        {
            return new RailGraphManager(theLogger, theRailwayNetworkManager);
        }
        private RailGraphManager(IMyLogger theLogger, RailwayNetworkManager theRailwayNetworkManager)
        {
            this.theLogger = theLogger;
            this.theRailwayNetworkManager = theRailwayNetworkManager;
            RailGraphInitialized = CreateRailGraph();
            InitializeRailwayNetworkToRailGraph();
        }
        ~RailGraphManager()
        {
            // Close RailGraph
            this.railGraph?.destroy();
        }
        private bool CreateRailGraph()
        {
            try
            {
                RailgraphLib.SolidDB.CSolidEntryPoint solidDB = new();
                if (!solidDB.Connect())
                {
                   theLogger.LogInfo("Couldn't connect to Solid DB");
                   return false;
                }

                RailgraphLib.armd.Armd.init(solidDB);
                this.hierarchyRelations = RailgraphLib.HierarchyObjects.HierarchyRelations.Instance(solidDB, 6000);

                // Network creator
                this.railGraph = new RailgraphLib.NetworkCreator(solidDB, this.hierarchyRelations);

                // Interlocking graph, objects and topograph
                this.ilGraph = new RailgraphLib.Interlocking.ILGraph(solidDB);
                this.ilGraph.startInit();
                this.ilTopoGraph = new RailgraphLib.Interlocking.ILTopoGraph(this.ilGraph);
                this.railGraph.add(this.ilTopoGraph);

                this.railGraph.create();

                solidDB.CloseConnection();

            }
            catch (Exception e)
            {
                theLogger.LogException(e.ToString());
                return false;
            }

            return true;
        }
        private void InitializeTrackObjects()
        {
            this.ILGraph?.iterateAllObjectsAndCallMethod(LogTrackObjectInfo);
            ILGraph?.iterateAllObjectsAndCallMethod(AddToTrackDictionary);
        }
        private void InitializeCoreObjects()
        {
            this.ILGraph?.iterateAllCoreObjectsAndCallMethod(LogCoreObjectInformation);
        }
        private void AssociateTracksToLinks()
        {
            try
            {
                foreach (var stn in theRailwayNetworkManager.MyStations)
                {
                    foreach (var node in stn.MyNodes)
                    {
                        foreach (var link in node.MyLeftLinks)
                        {
                            var theObjects = link.MyEdgeAssociation?.MyEdge?.getAssociatedObjects();
                            if (theObjects == null) continue;
                            foreach (var uid in theObjects)
                            {
                                try
                                {
                                    var track = GetTrack(uid);
                                    if (track == null) continue;
                                    var trk = ConflictManagementLibrary.Network.Track.CreateInstance(track.getId(), track.getName(), (RailgraphLib.Interlocking.Track)track);
                                    trk.MyLink = link;
                                    link.MyTracks.Add(trk);
                                    if (trk.MyTrackAssociation?.MyInterlockingTrack != null)
                                    {
                                        trk.MyTrackAssociation.MyDistanceInMeters = trk.MyTrackAssociation.MyInterlockingTrack.getLength() / 1000;
                                        var name = trk.MyTrackAssociation?.MyInterlockingTrack.getName();
                                        var id = trk.MyTrackAssociation?.MyInterlockingTrack.getId();
                                        var dis = trk.MyTrackAssociation?.MyDistanceInMeters;
                                        theLogger.LogInfo("ILTrack <" + name + "><" + id + "><" + dis + "> associated with link <" + link.MyDescription + ">< an Edge " + link.EdgeName + ">");
                                    }
                                }
                                catch (Exception e)
                                {
                                    theLogger.LogException(e.ToString());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                theLogger.LogException(e.ToString());
            }
        }
        private void AssociateEdgesToLinks()
        {
            try
            {
                foreach (var stn in theRailwayNetworkManager.MyStations)
                {
                    foreach (var node in stn.MyNodes)
                    {
                        foreach (var link in node.MyLeftLinks)
                        {
                            try
                            {
                                if (string.IsNullOrEmpty(link.EdgeId)) continue;
                                link.MyEdgeAssociation = LinkEdgeAssociation.CreateInstance(link.EdgeName!, link.EdgeId!);
                                link.MyEdgeAssociation.MyEdge = (Edge)GetEdge(Convert.ToUInt32(link.EdgeId))!;
                                link.MyEdgeAssociation.MyDistanceInMeters = link.MyEdgeAssociation.MyEdge.getLength() / 1000;
                                var name = link.MyEdgeAssociation.MyEdge.getName();
                                var id = link.MyEdgeAssociation.MyEdge.getId();
                                var dis = link.MyEdgeAssociation.MyDistanceInMeters;
                                theLogger.LogInfo("Edge <" + name + "><" + id + "><" + dis + "> associated with link <" + link.MyDescription + ">");
                            }
                            catch (Exception e)
                            {
                                theLogger.LogInfo(link.EdgeId);
                                theLogger.LogException(e.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                theLogger.LogException(e.ToString());
            }
        }
        private void InitializeRailwayNetworkToRailGraph()
        {
            try
            {
                InitializeTrackObjects();
                InitializeCoreObjects();
                AssociateEdgesToLinks();
                AssociateTracksToLinks();
                AssociateHierarchyTracksToNetworkTracks();
                AssociateHierarchyPlatformsToNetworkPlatforms();
            }
            catch (Exception e)
            {
                theLogger.LogException(e.ToString());
            }
        }
        private void AssociateHierarchyPlatformsToNetworkPlatforms()
        {
            try
            {
                foreach (var platform in HierarchyRelations?.Platforms!)
                {
                    var plt = theRailwayNetworkManager.FindPlatform(platform.SysName);
                    if (plt != null)
                    {
                        plt.MyHierarchyPlatform = platform;
                        plt.MyElementPosition = GetElementPositionOfPlatform(platform);
                        var track = platform.Tracks[0];
                        if (track != null) plt.MyTrack = GetNetworkTrack(track.SysID);
                        theLogger.LogInfo("Platform <" + plt.MyName + "><" + plt.MyElementPosition.AdditionalPos / 1000 + ">");
                    }
                }
            }
            catch (Exception e)
            {
                theLogger.LogException(e.ToString());
            }
        }
        private void AssociateHierarchyTracksToNetworkTracks()
        {
            try
            {
                foreach (var trk in HierarchyRelations?.Tracks!)
                {
                    foreach (var stn in theRailwayNetworkManager.MyStations)
                    {
                        foreach (var node in stn.MyNodes)
                        {
                            foreach (var link in node.MyLeftLinks)
                            {
                                try
                                {
                                    foreach (var track in link.MyTracks)
                                    {
                                        if (track?.MyTrackAssociation == null || track.MyTrackAssociation.TrackUid != trk.SysID) continue;
                                        track.MyTrackAssociation.MyHierarchyTrack = trk;
                                        theLogger.LogInfo("Hierarchy Track <" + trk.SysID + "><" + trk.SysName + " associated with network track <" + track.MyUid + ">");
                                        break;
                                    }
                                }
                                catch (Exception e)
                                {
                                    theLogger.LogInfo(link.EdgeId);
                                    theLogger.LogException(e.ToString());
                                }
                            }
                        }
                    }

                }
            }
            catch (Exception e)
            {
                theLogger.LogException(e.ToString());
            }
        }
        private Network.Track GetNetworkTrack(uint trackUid)
        {
            try
            {
                    foreach (var stn in theRailwayNetworkManager.MyStations)
                    {
                        foreach (var node in stn.MyNodes)
                        {
                            foreach (var link in node.MyLeftLinks)
                            {
                                try
                                {
                                    foreach (var track in link.MyTracks)
                                    {
                                        if (track.MyUid == trackUid) return track;
                                    }
                                }
                                catch (Exception e)
                                {
                                    theLogger.LogInfo(link.EdgeId);
                                    theLogger.LogException(e.ToString());
                                }
                            }
                        }
                    }

            }
            catch (Exception e)
            {
                theLogger.LogException(e.ToString());
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
        private GraphObj? GetTrack(uint theUid)
        {
            try
            {
                foreach (var track in trackObjectsByUid.Where(track => track.Key == theUid))
                {
                    return track.Value;
                }
            }
            catch (Exception e)
            {
                theLogger.LogException(e.ToString());
            }
            return null;
        }
        private CoreObj? GetEdge(uint theUid)
        {
            try
            {
                foreach (var edge in edgeObjectsByUid.Where(edge => edge.Key == theUid))
                {
                    return edge.Value;
                }
            }
            catch (Exception e)
            {
                theLogger.LogException(e.ToString());
            }

            return null;
        }
        private void AddToTrackDictionary(uint ObjId)
        {
            var coreObj = ILTopoGraph?.getGraphObj(ObjId);
            if (coreObj != null && coreObj.getClassType() == CLASS_TYPE.CLASS_TRACK)
            {
                trackObjectsByUid.Add(coreObj.getId(),coreObj);
            }
        }
        public void AddToEdgeDictionary(uint ObjId)
        {
            var coreObj = this.ILTopoGraph?.getCoreObj(ObjId);
            if (coreObj == null || coreObj?.getClassType() != CLASS_TYPE.CLASS_EDGE) return;
            edgeObjectsByUid.Add(coreObj!.getId(), coreObj);
        }
        private void LogCoreObjectInformation(uint coreObjId)
        {
            var coreObj = ILTopoGraph?.getCoreObj(coreObjId);
            if (coreObj == null) return;
            theLogger.LogInfo($"    <Core Object Name=\"{coreObj.getName()}\" EdgeID=\"{coreObj.getId()}\" Style=\"ET\"/>");
            AddToEdgeDictionary(coreObjId);
        }
        private void LogTrackObjectInfo(uint ObjId)
        {
            var coreObj = ILTopoGraph?.getGraphObj(ObjId);
            if (coreObj != null && coreObj.getClassType() == CLASS_TYPE.CLASS_TRACK)
            {
                theLogger.LogInfo($"    <Track Name=\"{coreObj.getName()}\" UID=\"{coreObj.getId()}\" Distance=\"{coreObj.getLength() / 1000}\"/>");
            }
        }
        public int GetEdgeLength(uint ObjId)
        {
            var coreObj = this.ILTopoGraph?.getCoreObj(ObjId);
            if (coreObj != null)
            {
                return coreObj.getLength() / 1000;
            }

            return 0;
        }
        public int GetTrackLength(uint ObjId)
        {
            var coreObj = this.ILTopoGraph?.getCoreObj(ObjId);
            if (coreObj != null)
            {
                return coreObj.getLength() / 1000;
            }
            return 0;
        }
        public List<GraphObj> GetEdgeTrackAssociations(CoreObj theEdge)
        {
            var theList = new List<GraphObj>();
            try
            {
                foreach (var objUid in theEdge.getAssociatedObjects())
                {
                    var obj = GetTrackObject(objUid);
                    if (obj != null) theList.Add(obj);
                }
            }
            catch (Exception e)
            {
               theLogger.LogException(e.ToString());
            }
            return theList;
        }
        private GraphObj GetTrackObject(uint theUid)
        {
            foreach (var (key, value) in trackObjectsByUid)
            {
                if (key == theUid) return value;
            }
            return null!;
        }

        #region Test Methods
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
            //Assert(termReason == RailgraphLib.TopoGraph.ETerminationReason.ok && rFindResultVector.Count == 1);

            // Test findPath() from SIP_VEC (1625) to SI10_VEC (258698) (begin and end signals of route SIP_VEC-SI10_VEC)
            // Two paths should be found!
            rCondition = new(1625, 258698, RailgraphLib.Enums.EDirection.dNominal);
            rFindResultVector = new();
            termReason = ILTopoGraph.findPath(rCondition, rFindResultVector);
            //Assert(termReason == RailgraphLib.TopoGraph.ETerminationReason.ok && rFindResultVector.Count == 2);

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
        private void TestRailGraphRoutes()
        {
            RailgraphLib.FindCondition.FindAllCondition rCondition;
            List<RailgraphLib.FindCondition.FindResult> rFindResultVector;
            RailgraphLib.TopoGraph.ETerminationReason termReason;

            //Log.Debug($"============================================================================");
            //Log.Debug($"Searching paths of {HierarchyRelations.Routes.Count} routes");
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
                    //Log.Debug($"      OK ---> Route {route.SysName} on {stationName} - Paths found: {rFindResultVector.Count}");
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
                        //Log.Debug($"DB ERROR ---> Route {route.SysName} on {stationName} - Found to other direction. Paths found: {rFindResultVector.Count}");
                        if (amountPathsOtherDirection.ContainsKey(rFindResultVector.Count))
                            amountPathsOtherDirection[rFindResultVector.Count]++;
                        else
                            amountPathsOtherDirection.Add(rFindResultVector.Count, 1);
                    }
                    else
                    {
                        failed++;
                        failedRoutes.Add(route);
                       //Log.Debug($"    FAIL ---> Route {route.SysName} on {stationName} - No paths found: {route.BPID} -> {route.EPID} ({route.EDir})");
                    }
                }
            }

            // Report of search
            //Log.Debug($"Found paths for {found} routes");
            //Log.Debug($"Found paths to other direction for {foundToOtherDirection} routes (route direction error in DB)");
            //Log.Debug($"Failed to find paths for {failed} routes");
            int total = 0, count = 0;
            //Log.Debug($"Amount of paths in found routes:");
            foreach (var paths in amountPaths)
            {
                //Log.Debug($"  {paths.Key} paths : {paths.Value} routes");
                count += paths.Key * paths.Value;
            }
            total += count;
            count = 0;
           // Log.Debug($"Amount of paths in routes to other direction:");
            foreach (var paths in amountPathsOtherDirection)
            {
                //Log.Debug($"  {paths.Key} paths : {paths.Value} routes");
                count += paths.Key * paths.Value;
            }
            total += count;
            //Log.Debug($"Failed routes:");
            foreach (var route in failedRoutes)
            {

            }
            //    Log.Debug($"  {route.SysName} ({route.SysID}): {route.BPID} -> {route.EPID} ({route.EDir})");
            //Log.Debug($"Total number of paths found: {total}");
            //Log.Debug($"============================================================================");
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

            //Assert(trackExtension.getStartDistance() == trackExtension2.getStartDistance());
            //Assert(trackExtension.getEndDistance() == trackExtension2.getEndDistance());
            //Assert(trackExtension.getExtensionElementsRaw().Count == trackExtension2.getExtensionElementsRaw().Count);
            for (int i = 0; i < trackExtension.getExtensionElementsRaw().Count; i++)
            {

            }
                //Assert(trackExtension.getExtensionElementsRaw()[i] == trackExtension2.getExtensionElementsRaw()[i]);
        }
        private void LogObjectConfigInfo(uint ObjId)
        {
            var coreObj = ILTopoGraph?.getGraphObj(ObjId);
            if (coreObj != null)
            {
                theLogger.LogInfo($"    <Graphic Object Name=\"{coreObj.getName()}\" UID=\"{coreObj.getId()}\" Style=\"ET\"/>");
            }
        }
        private void IterateAllObjects()
        {
            this.ILGraph?.iterateAllObjectsAndCallMethod(LogObjectConfigInfo);
        }

        #endregion

    }
    public class ElementPosition : IEquatable<ElementPosition?>
    {
        public string ElementId => elementId;
        public uint Offset => offset;
        public long AdditionalPos => additionalPos;
        public string AdditionalName => additionalName;     // Platform, timing point etc.

        private readonly string elementId = "";
        private readonly uint offset = 0;
        private readonly string additionalName = "";
        private readonly long additionalPos = 0;

        public ElementPosition()
        {
        }

        public ElementPosition(string elementId, uint offset, long additionalPos, string additionalName = "")
        {
            this.elementId = elementId;
            this.offset = offset;
            this.additionalPos = additionalPos;
            this.additionalName = additionalName;
        }

        public bool IsValid()
        {
            return ElementId != "";
        }

        public override string ToString()
        {
            return string.Format($"ElementId = '{ElementId}', Offset = {Offset}, AdditionalPos = {AdditionalPos}, AdditionalName = '{AdditionalName}'");
        }

        public string GetEdgePosIdentifier()
        {
            return string.Format($"{ElementId}({AdditionalPos})");
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as ElementPosition);
        }

        public bool Equals(ElementPosition? other)
        {
            return other is not null &&
                   ElementId == other.ElementId &&
                   Offset == other.Offset &&
                   AdditionalPos == other.AdditionalPos;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ElementId, Offset, AdditionalPos);
        }

        public static bool operator ==(ElementPosition? left, ElementPosition? right)
        {
            return EqualityComparer<ElementPosition>.Default.Equals(left, right);
        }

        public static bool operator !=(ElementPosition? left, ElementPosition? right)
        {
            return !(left == right);
        }
    }

    ////////////////////////////////////////////////////////////////////////////////

    public class ElementExtension : IEquatable<ElementExtension?>
    {
        public ElementPosition StartPos => startPos;
        public ElementPosition EndPos => endPos;
        public List<string> Elements => elements;

        private ElementPosition startPos = new();
        private ElementPosition endPos = new();
        private List<string> elements = new();

        public ElementExtension()
        {
        }

        public ElementExtension(ElementPosition startPos, ElementPosition endPos, List<string> elements)
        {
            this.startPos = startPos;
            this.endPos = endPos;
            this.elements = elements;

            if (!IsValid())
                throw new Exception(string.Format($"Invalid element extension: {this}"));
        }

        public bool IsValid()
        {
            return StartPos.IsValid() && EndPos.IsValid() && Elements.Count != 0 && Elements.First() == StartPos.ElementId && Elements.Last() == EndPos.ElementId;
        }

        public override string ToString()
        {
            string s = string.Format($"Start: [{StartPos}] End: [{EndPos}] Elements:");
            foreach (var edge in Elements)
                s += " " + edge;
            return s;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as ElementExtension);
        }

        public bool Equals(ElementExtension? other)
        {
            return other is not null &&
                   EqualityComparer<ElementPosition>.Default.Equals(StartPos, other.StartPos) &&
                   EqualityComparer<ElementPosition>.Default.Equals(EndPos, other.EndPos) &&
                   EqualityComparer<List<string>>.Default.Equals(Elements, other.Elements);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StartPos, EndPos, Elements);
        }

        public static bool operator ==(ElementExtension? left, ElementExtension? right)
        {
            return EqualityComparer<ElementExtension>.Default.Equals(left, right);
        }

        public static bool operator !=(ElementExtension? left, ElementExtension? right)
        {
            return !(left == right);
        }
    }

}
