using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using ConflictManagementLibrary.Logging;
using ConflictManagementLibrary.Model.Possession;
using ConflictManagementLibrary.Network;
using Newtonsoft.Json;
using RailgraphLib;
using RailgraphLib.Enums;
using RailgraphLib.FindCondition;
using RailgraphLib.Interlocking;
using Track = RailgraphLib.Interlocking.Track;

namespace ConflictManagementLibrary.Management;

public class RailGraphManager
{
    private readonly IMyLogger? theLogger;
    private readonly RailwayNetworkManager? theRailwayNetworkManager;
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
    public List<Network.Track> MyTracks = new List<Network.Track>();
    public static RailGraphManager? CreateInstance(IMyLogger? theLogger, RailwayNetworkManager? theRailwayNetworkManager, bool initManually = true)
    {
        return new RailGraphManager(theLogger, theRailwayNetworkManager, initManually);
    }
    private RailGraphManager(IMyLogger? theLogger, RailwayNetworkManager? theRailwayNetworkManager, bool initManually = true)
    {
        this.theLogger = theLogger;
        this.theRailwayNetworkManager = theRailwayNetworkManager;
        if (!initManually) InitializeRailGraph();
    }

    public bool InitializeRailGraph()
    {
        try
        {
            RailGraphInitialized = CreateRailGraph();
            if (!RailGraphInitialized) { return false; }
            InitializeRailwayNetworkToRailGraph();

            return true;
        }
        catch (Exception e)
        {
            theLogger.LogException(e.ToString());
            return false;
        }
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
    private void BuildCoreObjectByNameDictionary(uint coreObjId)
    {
        var coreObj = ilTopoGraph?.getCoreObj(coreObjId);
        if (coreObj != null)
        {
            this.coreObjectsByName.Add(coreObj.getName(), coreObj);
            MyLogger?.LogInfo("CoreName <"+ coreObj.getName() +"> <" +coreObj.getId() + ">");
        }
    }
    private void InitializeTrackObjects()
    {
        this.ILGraph?.iterateAllObjectsAndCallMethod(LogTrackObjectInfo);
        ILGraph?.iterateAllObjectsAndCallMethod(AddToTrackDictionary);
    }
    private void InitializeCoreObjects()
    {
        this.ILGraph?.iterateAllCoreObjectsAndCallMethod(LogCoreObjectInformation);
        this.ilGraph?.iterateAllCoreObjectsAndCallMethod(BuildCoreObjectByNameDictionary);

    }
    private void AssociateTracksToLinks()
    {
        try
        {
            foreach (var stn in theRailwayNetworkManager.MyStations)
            {
                foreach (var node in stn.MyNodes)
                {
                    foreach (var link in node.MyRightLinks)
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
                                MyTracks.Add(trk);
                                trk.MyLink = link;
                                AddTrack(trk,link.MyTracks);
                                //link.MyTracks.Add(trk);
                                if (trk.MyTrackAssociation?.MyInterlockingTrack != null)
                                {
                                    trk.MyTrackAssociation.MyDistanceInMeters =
                                        trk.MyTrackAssociation.MyInterlockingTrack.getLength() / 1000;
                                    var name = trk.MyTrackAssociation?.MyInterlockingTrack.getName();
                                    var id = trk.MyTrackAssociation?.MyInterlockingTrack.getId();
                                    var dis = trk.MyTrackAssociation?.MyDistanceInMeters;
                                    theLogger.LogInfo("ILTrack <" + name + "><" + id + "><" + dis +
                                                      "> associated with link <" + link.MyDescription + ">< an Edge " +
                                                      link.EdgeName + ">");
                                }
                            }
                            catch (Exception e)
                            {
                                theLogger?.LogException(e.ToString());
                            }
                        }
                    }

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
                                var trk = ConflictManagementLibrary.Network.Track.CreateInstance(track.getId(),
                                    track.getName(), (RailgraphLib.Interlocking.Track)track);
                                trk.MyLink = link;
                                AddTrack(trk, link.MyTracks);
                                //link.MyTracks.Add(trk);
                                if (trk.MyTrackAssociation?.MyInterlockingTrack != null)
                                {
                                    trk.MyTrackAssociation.MyDistanceInMeters =
                                        trk.MyTrackAssociation.MyInterlockingTrack.getLength() / 1000;
                                    var name = trk.MyTrackAssociation?.MyInterlockingTrack.getName();
                                    var id = trk.MyTrackAssociation?.MyInterlockingTrack.getId();
                                    var dis = trk.MyTrackAssociation?.MyDistanceInMeters;
                                    theLogger.LogInfo("ILTrack <" + name + "><" + id + "><" + dis +
                                                      "> associated with link <" + link.MyDescription + ">< an Edge " +
                                                      link.EdgeName + ">");
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
    private void AddTrack(Network.Track theTrack, List<Network.Track> theList)
    {
        if (theTrack == null) return;
        foreach (var t in theList)
        {
            if (theTrack.MyUid == t.MyUid)
            {
                theLogger?.LogInfo("Track Founded In List <" + theTrack.MyUid + "><" + theTrack.MyName + ">");
                return;
            }
        }
        //theLogger?.LogInfo("Link Added to List <" + theLink.MyReferenceNumber + "><" + theLink.MyDescription + ">");

        theList.Add(theTrack);
    }
    private void AssociateEdgesToLinks()
    {
        try
        {
            IEnumerable<Station> tempStations = new List<Station>(theRailwayNetworkManager.MyStations);
            var edgeassoDict = new Dictionary<string, LinkEdgeAssociation>();
            var edgeList = new List<LinkEdgeAssociation>();

            foreach (var station in tempStations)
            {
                foreach (var node in station.MyNodes)
                {
                    //foreach (var link in node.MyLeftLinks
                    foreach (var link in node.MyRightLinks)
                    {
                        if (string.IsNullOrEmpty(link.EdgeId))
                        {
                            continue;
                        }

                        var kmValue = GetElementKilometerValueOfEdge(link.EdgeId);
                        var linkEdgeAssociation = LinkEdgeAssociation.CreateInstance(link.EdgeName!, link.EdgeId!, kmValue.ToString());
                        linkEdgeAssociation.MyEdge = (Edge)GetEdge(Convert.ToUInt32(link.EdgeId))!;
                        linkEdgeAssociation.MyDistanceInMeters = linkEdgeAssociation.MyEdge?.getLength() / 1000 ?? 0;
                        link.MyEdgeAssociation = linkEdgeAssociation;
                        if (linkEdgeAssociation.EdgeName == "E_PT1_PT5_5S_CAR")
                        {
                            linkEdgeAssociation.MyDistanceInMeters = 10000;
                        }

                        edgeList.Add(linkEdgeAssociation);
                    }
                }
            }
            foreach (var stn in theRailwayNetworkManager.MyStations)
            {
                foreach (var node in stn.MyNodes)
                {
                    foreach (var link in node.MyLeftLinks)
                    {
                        try
                        {
                            var edgeAssociation = GetEdgeAssociation(edgeList, link.EdgeName);
                            if (edgeAssociation == null)
                            {
                                Console.Beep();
                            }
                        


                            link.MyEdgeAssociation = edgeAssociation;
                            var name = link.MyEdgeAssociation?.MyEdge?.getName();
                            var id = link.MyEdgeAssociation?.MyEdge?.getId();
                            var dis = link.MyEdgeAssociation?.MyDistanceInMeters;
                            theLogger.LogInfo($"Edge <{name}><{id}><{dis}> associated with link <{link.MyDescription}> Link Guid<{link.MySystemGuid}> Left Link");

                            foreach (var item in node.MyPaths)
                            {
                                if (item?.MyLinkLeft != null && item.MyLinkLeft.EdgeId.Equals(link.EdgeId) && item.MyLinkLeft.EdgeName.Equals(link.EdgeName) && item.MyLinkLeft.MyReferenceNumber.Equals(link.MyReferenceNumber))
                                {
                                    item.MyLinkLeft.MySystemGuid = link.MySystemGuid;
                                    item.MyLinkLeft.MyEdgeAssociation ??= link.MyEdgeAssociation;
                                }

                            }
                        }
                        catch (Exception e)
                        {
                            theLogger.LogInfo(link.EdgeId);
                            theLogger.LogException(e.ToString());
                        }
                    }
                    foreach (var link in node.MyRightLinks)
                    {
                        try
                        {
                            var edgeAssociation = GetEdgeAssociation(edgeList, link.EdgeName);
                            if (edgeAssociation == null)
                            {
                                theLogger.LogException("Edge not Found On Link " + link.MyReferenceNumber + "<" + link.MyDescription + ">");
                            }

                            link.MyEdgeAssociation = edgeAssociation;
                            var name = link.MyEdgeAssociation?.MyEdge?.getName();
                            var id = link.MyEdgeAssociation?.MyEdge?.getId();
                            var dis = link.MyEdgeAssociation?.MyDistanceInMeters;
                            theLogger.LogInfo($"Edge <{name}><{id}><{dis}> associated with link <{link.MyDescription}> Link Guid<{link.MySystemGuid}> Right Link");
                            foreach (var item in node.MyPaths)
                            {
                                if (item?.MyLinkRight != null && item.MyLinkRight.EdgeId.Equals(link.EdgeId) && item.MyLinkRight.EdgeName.Equals(link.EdgeName) && item.MyLinkRight.MyReferenceNumber.Equals(link.MyReferenceNumber))
                                {
                                    item.MyLinkRight.MySystemGuid = link.MySystemGuid;
                                    item.MyLinkRight.MyEdgeAssociation ??= link.MyEdgeAssociation;
                                }
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
            if (HierarchyRelations == null)
            {
                return;
            }
            foreach (var platform in HierarchyRelations?.Platforms!)
            {
                var plt = theRailwayNetworkManager.FindPlatform(platform.SysName);
                if (plt != null)
                {
                    plt.MyHierarchyPlatform = platform;
                    plt.MyElementPosition = GetElementPositionOfPlatform(platform);
                    var track = platform.Tracks[0];
                    //theLogger?.LogInfo("Platform <" + plt.MyName + "> Number Of Platform Tracks <" + platform.Tracks.Count + ">");

                    plt.MyTrackUId = track.SysID;
                    plt.MyHierarchyTrack = track;
                    //if (track != null) plt.MyTrackNetwork = GetNetworkTrack(track.SysID);
                    var platformTrack = ilTopoGraph?.getGraphObj(track.SysID);
                    if (platformTrack != null)
                    {
                        plt.MyDistanceInMeters = platformTrack.getLength() / 1000;

                    }
                    //plt.MyDistanceInMeters = plt.MyElementPosition.AdditionalPos / 1000;
                    theLogger?.LogInfo("Platform <" + plt.MyName + "> Track Length <" + plt.MyDistanceInMeters + ">");
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
            if (HierarchyRelations == null)
            {
                return;
            }
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
                                    //theLogger.LogInfo("Hierarchy Track <" + trk.SysID + "><" + trk.SysName + " associated with network track <" + track.MyUid + ">");
                                    break;
                                }
                            }
                            catch (Exception e)
                            {
                                theLogger.LogInfo(link.EdgeId);
                                theLogger.LogException(e.ToString());
                            }
                        }
                        foreach (var link in node.MyRightLinks)
                        {
                            try
                            {
                                foreach (var track in link.MyTracks)
                                {
                                    if (track?.MyTrackAssociation == null || track.MyTrackAssociation.TrackUid != trk.SysID) continue;
                                    track.MyTrackAssociation.MyHierarchyTrack = trk;
                                    //theLogger.LogInfo("Hierarchy Track <" + trk.SysID + "><" + trk.SysName + " associated with network track <" + track.MyUid + ">");
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
                                if (track.MyUid == trackUid)
                                {
                                    return track;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            theLogger.LogInfo(link.EdgeId);
                            theLogger.LogException(e.ToString());
                        }
                    }
                    foreach (var link in node.MyRightLinks)
                    {
                        try
                        {
                            foreach (var track in link.MyTracks)
                            {
                                if (track.MyUid == trackUid)
                                {
                                    return track;
                                }
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
    public int getKmValue(RailgraphLib.CoreObj coreObj, int offset)
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
    public ElementPosition GetElementPositionOfEdge(string theEdgeUid)
    {
        RailgraphLib.RailExtension.CoreExtension coreExtension = new();

            //if (coreExtension.getExtensionElements().Count == 1)
            //{
                var edgeUid = Convert.ToUInt32(theEdgeUid);
                var edge = this.ILTopoGraph?.getCoreObj(edgeUid);
                if (edge != null)
                {
                    var centerOffsetOfEdge = coreExtension.getStartDistance() + (edge.getLength() - coreExtension.getStartDistance() - coreExtension.getEndDistance()) / 2;
                    var kmValue = getKmValue(edge, centerOffsetOfEdge);

                    return new ElementPosition(edge.getName(), (uint)coreExtension.getStartDistance(), kmValue, edge.getName());
                }
            //}

        return new ElementPosition();
    }
    public int GetElementKilometerValueOfEdge(string theEdgeUid)
    {
        RailgraphLib.RailExtension.CoreExtension coreExtension = new();

        //if (coreExtension.getExtensionElements().Count == 1)
        //{
        var edgeUid = Convert.ToUInt32(theEdgeUid);
        var edge = this.ILTopoGraph?.getCoreObj(edgeUid);
        if (edge != null)
        {
            var centerOffsetOfEdge = coreExtension.getStartDistance() + (edge.getLength() - coreExtension.getStartDistance() - coreExtension.getEndDistance()) / 2;
            return getKmValue(edge, centerOffsetOfEdge);
        }
        //}
        return 0;
    }
    public int GetElementKilometerValueOfTrack(uint theUid)
    {
        //RailgraphLib.RailExtension.CoreExtension coreExtension = new();

        ////if (coreExtension.getExtensionElements().Count == 1)
        ////{
        //var edgeUid = Convert.ToUInt32(theUid);
        //var edge = this.ILTopoGraph?.getCoreObj(edgeUid);
        //var track = ilTopoGraph.getGraphObj(edgeUid);
        //var location = track.getDistanceFromInitPoint();
        //if (edge != null)
        //{
        //    var centerOffsetOfEdge = coreExtension.getStartDistance() + (track.getLength() - coreExtension.getStartDistance() - coreExtension.getEndDistance()) / 2;
        //    return getKmValue(edge, centerOffsetOfEdge);
        //}
        ////}
      
        var track = ilTopoGraph?.getGraphObj(theUid);
        if (track != null)
            return track.getDistanceFromInitPoint();

        return 0;
    }
    public int GetElementKilometerValueOfPoint(uint theUid)
    {
        //RailgraphLib.RailExtension.CoreExtension coreExtension = new();

        ////if (coreExtension.getExtensionElements().Count == 1)
        ////{
        //var edgeUid = Convert.ToUInt32(theUid);
        //var edge = this.ILTopoGraph?.getCoreObj(edgeUid);
        //var point = ilGraph.getILGraphObj(edgeUid);
        //if (edge != null)
        //{
        //    var centerOffsetOfEdge = coreExtension.getStartDistance() + (edge.getLength() - coreExtension.getStartDistance() - coreExtension.getEndDistance()) / 2;
        //    return getKmValue(edge, centerOffsetOfEdge);
        //}
        ////}
        //return 0;
        var point = ilTopoGraph?.getGraphObj(theUid);
        if (point != null)
            return point.getDistanceFromInitPoint();

        return 0;

    }
    public GraphObj? GetTrack(uint theUid)
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
    public GraphObj? GetTrack(string theName)
    {
        try
        {
            foreach (var track in trackObjectsByUid.Where(track => track.Value.getName() == theName))
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
    public CoreObj? GetEdge(uint theUid)
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
    public bool DoesRouteExist(string routeName)
    {
        try
        {
            foreach (var r in HierarchyRelations.Routes)
            {
                if (r.SysName == routeName) return true;
            }
        }
        catch (Exception e)
        {
            theLogger.LogException(e.ToString());
        }
        return false;
    }
    private LinkEdgeAssociation? GetEdgeAssociation(List<LinkEdgeAssociation> theList, string theName)
    {
        foreach (var le in theList)
        {
            if (le.EdgeName == theName) return le;
        }

        return null;
    }
    private void AddToTrackDictionary(uint ObjId)
    {
        var coreObj = ILTopoGraph?.getGraphObj(ObjId);
        if (coreObj != null && coreObj.getClassType() == CLASS_TYPE.CLASS_TRACK)
        {
            trackObjectsByUid.Add(coreObj.getId(), coreObj);
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
        //theLogger.LogInfo($"    <Core Object Name=\"{coreObj.getName()}\" EdgeID=\"{coreObj.getId()}\" Style=\"ET\"/>");
        AddToEdgeDictionary(coreObjId);
    }
    private void LogTrackObjectInfo(uint ObjId)
    {
        var coreObj = ILTopoGraph?.getGraphObj(ObjId);
        if (coreObj != null && coreObj.getClassType() == CLASS_TYPE.CLASS_TRACK)
        {
            //theLogger.LogInfo($"    <Track Name=\"{coreObj.getName()}\" UID=\"{coreObj.getId()}\" Distance=\"{coreObj.getLength() / 1000}\"/>");
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
    public RailgraphLib.Interlocking.Point? GetFirstPointInWrongPositionOnRoute(RailgraphLib.HierarchyObjects.Route route)
    {
        // Returns first point that is in wrong position on route
        FindLogicalCondition rMyLogicalFindCondition = new MyLogicalFindCondition(route);
        FindResult rFindResult = new();
        if (((bool)!ILTopoGraph?.findLogicalPath(ref rMyLogicalFindCondition, ref rFindResult))!)
        {
            // Last object must be point leg
            var pointLegId = ((MyLogicalFindCondition)rMyLogicalFindCondition).Path.LastOrDefault();
            if (ILTopoGraph?.getGraphObj(pointLegId) is PointLeg pointLeg)
                return pointLeg.getPoint();
            else
                ; // Don't log anything in here!
        }
        return null;
    }
    public List<RailgraphLib.Interlocking.Point> GetAllPointsInWrongPositionOnRoute(RailgraphLib.HierarchyObjects.Route route)
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
    private class MyLogicalFindCondition : RailgraphLib.FindCondition.FindLogicalCondition
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
    public ElementPosition CreateElementPosition(string element, uint offset, bool offsetToNominal)
    {
        ElementPosition position = new();

        var coreObj = GetCoreObjByName(element);

        if (coreObj != null)
        {
            uint offsetOnCore = (uint)(offsetToNominal ? offset : coreObj.getLength() - offset);
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
        if (!RailgraphLib.KmOffsetSection.convertElementOffsetToSectionValue(coreObj.getId(), (int)offset, ref kmValue))
        {
            kmValue = (int)(coreObj.getDistanceFromInitPoint() + offset);  // This is used until the above SA work has been done!
        }

        return kmValue;
    }


    #region Test Methods

    private void TestPointKmValue(string theUid)
    {
        //var dis = GetElementKilometerValueOfTrack(theUid);
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
   
    //private void TestRailGraphRoutes()
    //{
    //    RailgraphLib.FindCondition.FindAllCondition rCondition;
    //    List<RailgraphLib.FindCondition.FindResult> rFindResultVector;
    //    RailgraphLib.TopoGraph.ETerminationReason termReason;

    //    //Log.Debug($"============================================================================");
    //    //Log.Debug($"Searching paths of {HierarchyRelations.Routes.Count} routes");
    //    int found = 0;
    //    int failed = 0;
    //    int foundToOtherDirection = 0;
    //    Dictionary<int, int> amountPaths = new();
    //    Dictionary<int, int> amountPathsOtherDirection = new();
    //    List<RailgraphLib.HierarchyObjects.Route> failedRoutes = new();
    //    foreach (var route in HierarchyRelations.Routes)
    //    {
    //        var stationId = route.Station.SysID;
    //        var stationName = route.Station.SysName;

    //        rCondition = new(route.BPID, route.EPID, route.EDir);
    //        rCondition.addViaElements(route.GetPoints().Select(p => (uint)p.SysID).ToList());
    //        rFindResultVector = new();
    //        termReason = this.ilTopoGraph.findPath(rCondition, rFindResultVector);
    //        if (termReason == RailgraphLib.TopoGraph.ETerminationReason.ok)
    //        {
    //            found++;
    //            if (amountPaths.ContainsKey(rFindResultVector.Count))
    //                amountPaths[rFindResultVector.Count]++;
    //            else
    //                amountPaths.Add(rFindResultVector.Count, 1);
    //            //Log.Debug($"      OK ---> Route {route.SysName} on {stationName} - Paths found: {rFindResultVector.Count}");
    //        }
    //        else
    //        {
    //            rCondition = new(route.BPID, route.EPID, route.EDir == RailgraphLib.Enums.EDirection.dNominal ? RailgraphLib.Enums.EDirection.dOpposite : RailgraphLib.Enums.EDirection.dNominal);
    //            rCondition.addViaElements(route.GetPoints().Select(p => (uint)p.SysID).ToList());
    //            rFindResultVector = new();
    //            termReason = this.ilTopoGraph.findPath(rCondition, rFindResultVector);
    //            if (termReason == RailgraphLib.TopoGraph.ETerminationReason.ok)
    //            {
    //                foundToOtherDirection++;
    //                //Log.Debug($"DB ERROR ---> Route {route.SysName} on {stationName} - Found to other direction. Paths found: {rFindResultVector.Count}");
    //                if (amountPathsOtherDirection.ContainsKey(rFindResultVector.Count))
    //                    amountPathsOtherDirection[rFindResultVector.Count]++;
    //                else
    //                    amountPathsOtherDirection.Add(rFindResultVector.Count, 1);
    //            }
    //            else
    //            {
    //                failed++;
    //                failedRoutes.Add(route);
    //                //Log.Debug($"    FAIL ---> Route {route.SysName} on {stationName} - No paths found: {route.BPID} -> {route.EPID} ({route.EDir})");
    //            }
    //        }
    //    }

    //    // Report of search
    //    //Log.Debug($"Found paths for {found} routes");
    //    //Log.Debug($"Found paths to other direction for {foundToOtherDirection} routes (route direction error in DB)");
    //    //Log.Debug($"Failed to find paths for {failed} routes");
    //    int total = 0, count = 0;
    //    //Log.Debug($"Amount of paths in found routes:");
    //    foreach (var paths in amountPaths)
    //    {
    //        //Log.Debug($"  {paths.Key} paths : {paths.Value} routes");
    //        count += paths.Key * paths.Value;
    //    }
    //    total += count;
    //    count = 0;
    //    // Log.Debug($"Amount of paths in routes to other direction:");
    //    foreach (var paths in amountPathsOtherDirection)
    //    {
    //        //Log.Debug($"  {paths.Key} paths : {paths.Value} routes");
    //        count += paths.Key * paths.Value;
    //    }
    //    total += count;
    //    //Log.Debug($"Failed routes:");
    //    foreach (var route in failedRoutes)
    //    {

    //    }
    //    //    Log.Debug($"  {route.SysName} ({route.SysID}): {route.BPID} -> {route.EPID} ({route.EDir})");
    //    //Log.Debug($"Total number of paths found: {total}");
    //    //Log.Debug($"============================================================================");
    //}
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
        catch (Exception) { }

        return ext == null ? null : new RailgraphLib.RailExtension.TrackExtension(ext.getStartDistance(), ext.getEndDistance(), ext.getExtensionElements().ToList(), ext.getStartDirection(), ext.getEndDirection());
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
        catch (Exception) { }

        return coreExtension;
    }

    public RailgraphLib.CoreObj? GetCoreObjByName(string coreObjName)
        {
            if (this.coreObjectsByName.ContainsKey(coreObjName))
                return this.coreObjectsByName[coreObjName];

            return null;
        }
    public RailgraphLib.GraphObj? GetILElementFromOffset(string elementId, int theoffset)
    {
            try
            {
                // Element position is core element position
                var coreObj = GetCoreObjByName(elementId);
                if (coreObj != null)
                {
                    var coreExtension = new RailgraphLib.RailExtension.CoreExtension(theoffset, (int)(coreObj.getLength() - theoffset), new List<uint>() { coreObj.getId() });
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
            catch (Exception) { }
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
    public RailgraphLib.GraphObj? GetILElementOnElementPositionModel(Model.Possession.ElementPosition pos)
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
            catch (Exception) { }
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

            rCondition = new(route.BPID, route.EPID, route.EDir);
            rCondition.addViaElements(route.GetPoints().Select(p => (uint)p.SysID).ToList());
            rFindResultVector = new();
            termReason = this.ilTopoGraph.findPath(rCondition, rFindResultVector);
            if (termReason == RailgraphLib.TopoGraph.ETerminationReason.ok && rFindResultVector.Count > 0)
            {
                // Take first result (there may be others, because via elements are points/switches)
                //TODO: later: bool endDirChanges = rFindResultVector.First().getDirectionChangeCountInPath() % 2 == 0 ? false : true;
                trackExtension = new RailgraphLib.RailExtension.TrackExtension(0, 0, rFindResultVector.First().getResult().ToList(), route.EDir, route.EDir);
            }
            else
            {
                // Route may sometimes be defined into wrong direction...
                RailgraphLib.Enums.EDirection searchDir = route.EDir == RailgraphLib.Enums.EDirection.dNominal ? RailgraphLib.Enums.EDirection.dOpposite : RailgraphLib.Enums.EDirection.dNominal;

                rCondition = new(route.BPID, route.EPID, searchDir);
                rCondition.addViaElements(route.GetPoints().Select(p => (uint)p.SysID).ToList());
                rFindResultVector = new();
                termReason = this.ilTopoGraph.findPath(rCondition, rFindResultVector);
                if (termReason == RailgraphLib.TopoGraph.ETerminationReason.ok && rFindResultVector.Count > 0)
                {
                    // Take first result (there may be others, because via elements are points/switches)
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

public record LinkEdgeMapClass
{
    public string LinkGuid { get; set; }
    public LinkEdgeAssociation EdgeAssociation { get; set; }
}