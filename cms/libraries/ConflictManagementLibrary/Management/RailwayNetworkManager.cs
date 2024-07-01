using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
//using System.Runtime.InteropServices.WindowsRuntime;
//using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Xml;
//using Amqp.Framing;
using ConflictManagementLibrary.Helpers;
using ConflictManagementLibrary.Logging;
using ConflictManagementLibrary.Model.Movement;
using ConflictManagementLibrary.Network;
using Newtonsoft.Json;
using NLog.Targets;
using RailgraphLib.HierarchyObjects;
using JPath = System.IO;
using Path = ConflictManagementLibrary.Network.Path;
using Platform = ConflictManagementLibrary.Network.Platform;
using Route = ConflictManagementLibrary.Network.Route;
using Station = ConflictManagementLibrary.Network.Station;


namespace ConflictManagementLibrary.Management;

public class RailwayNetworkManager
{
    #region Declarations
    public List<Station> MyStations { get; set; } = new List<Station>();
    private readonly IMyLogger? theLogger;
    private const string Filename = "RigaJunction.json";
    private const string FolderName = "Data";
    private const string FileNameMovementPlans = "MovementPlans.json";
    public bool IsInitialized;
    public List<MovementPlan> MyMovementPlans = new List<MovementPlan>();
    public List<MovementTemplate> MyMovementTemplates = new List<MovementTemplate>();
    public List<Path> MyStationNecks = new List<Path>();


    #endregion

    #region Constructor
    public RailwayNetworkManager(IMyLogger? theLogger)
    {
        this.theLogger = theLogger;
        InitializeRailwayNetwork();
        InitializeRailGraphManager();
        InitializeRouteActionInformation();
    }
    #endregion

    #region Methods Initialization
    private void InitializeRailwayNetwork()
    {
        try
        {
            var dir = Environment.CurrentDirectory;
            var filePath = JPath.Path.Combine(dir, FolderName, Filename);

            using (var r = new StreamReader(filePath))
            {
                var json = r.ReadToEnd();
                MyStations = JsonConvert.DeserializeObject<List<Station>>(json);
            }

            if (MyStations != null)
                foreach (var stn in MyStations)
                {
                    foreach (var node in stn.MyNodes)
                    {
                        node.MyStation = stn;
                    }
                    theLogger.LogInfo(stn.GetStationInformation());
                }

            BuildLeftLinkReferences();
            CheckForNullRightPathLinks();
        }

        catch (Exception e)
        {
            theLogger?.LogException(e);
        }
    }
    private void CheckForNullRightPathLinks()
    {
        try
        {
            foreach (var stn in MyStations)
            {
                foreach (var node in stn.MyNodes)
                {
                    foreach (var path in node.MyPaths)
                    {
                        if (path.MyLinkRight == null)
                        {
                            path.MyLinkRight = FindPathLink(path.MyConnectionRight, node);
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            theLogger?.LogException(e);
        }
    }
    private Link FindPathLink(int theReference, Node theNode)
    {
        foreach (var link in theNode.MyRightLinks)
        {
            if (link.MyReferenceNumber == theReference) return link;
        }
        return null;
    }
    private void InitializeRailGraphManager()
    {
        GlobalDeclarations.MyRailGraphManager = RailGraphManager.CreateInstance(theLogger, this);
        IsInitialized = (GlobalDeclarations.MyRailGraphManager!.InitializeRailGraph());
    }
    private void InitializeRouteActionInformation()
    {
        try
        {
            foreach (var route in MyRailGraphManager?.HierarchyRelations?.Routes!)
            {
                var routeActionInfo = RouteActionInfo.CreateInstance(route, theLogger);
                if (routeActionInfo != null) MyRouteActionInfoList.Add(routeActionInfo);
            }
            foreach (var stn in MyStations)
            {
                foreach (var node in stn.MyNodes)
                {
                    foreach (var path in node.MyPaths)
                    {
                        if (path.IsStationNeck) MyStationNecks.Add(path);
                    }
                }
            }
        }
        catch (Exception e)
        {
            theLogger?.LogException(e);
        }
    }
    #endregion

    #region Movement Templates
    public void AddMovementTemplate(string theTemplate)
    {
        try
        {
            var movementTemplate = DeserializeMyObject<MovementTemplate>(MyLogger, theTemplate);
            MyMovementTemplates.Add(movementTemplate);
            SerializeMovementTemplate(movementTemplate);
        }
        catch (Exception e)
        {
            theLogger?.LogException(e);
        }
    }
    private void SerializeMovementTemplate(MovementTemplate theTemplate)
    {
        try
        {
            var str = JsonConvert.SerializeObject(theTemplate);
            var filename = $"Template-" + theTemplate.fromName + "-" + theTemplate.toName + $"-{DateTime.Now:MMddyyyyhhmmssfff}.json";
            var curDir = Environment.CurrentDirectory;
            const string folder = @"Data\SerializeData\MovementTemplates";
            if (!Directory.Exists(System.IO.Path.Combine(curDir, folder)))
            {
                Directory.CreateDirectory(System.IO.Path.Combine(curDir, folder));
            }

            var fullpath = System.IO.Path.Combine(curDir, folder, filename);
            File.WriteAllText(fullpath, str);

        }
        catch (Exception e)
        {
            theLogger?.LogException(e);
        }
    }
    #endregion

    #region Functions
    public Station FindStation(string theName)
    {
        try
        {
            foreach (var stn in MyStations)
            {
                if (stn.MyReferenceNumber.ToString() == theName) return stn;
            }
        }
        catch (Exception e)
        {
            theLogger.LogException(e.ToString());
        }

        return null;
    }
    public Station FindStationByAbbreviation(string abbreviation) => MyStations.FirstOrDefault(s => string.Equals(s.Abbreviation.Trim(), abbreviation.Trim(), StringComparison.CurrentCultureIgnoreCase));
    public Station FindStationById(int id) => MyStations.FirstOrDefault(s => s.MyReferenceNumber == id);
    public Route FindPathToPlatform(Platform BeginPlatform, Platform EndPlatform, string theDirection)
    {
        try
        {
            var beginLink = FindLinkFromPlatform(BeginPlatform);
            var endLink = FindLinkFromPlatform(EndPlatform);
            return FindPathBetweenPlatforms(beginLink, endLink, theDirection);

        }
        catch (Exception e)
        {
            theLogger.LogException(e.ToString());
        }
        return null;
    }
    public Platform FindPlatform(string theName)
    {
        try
        {
            foreach (var stn in MyStations)
            {
                foreach (var nde in stn.MyNodes)
                {
                    foreach (var ll in nde.MyLeftLinks)
                    {
                        foreach (var pt in ll.MyPlatforms)
                        {
                            if (pt.MyName == theName) return pt;
                        }
                    }

                    foreach (var rr in nde.MyRightLinks)
                    {
                        foreach (var pt in rr.MyPlatforms)
                        {
                            if (pt.MyName == theName) return pt;
                        }
                    }

                    foreach (var path in nde.MyPaths)
                    {
                        foreach (var pt in path.MyPlatforms)
                        {
                            if (pt.MyName == theName) return pt;

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
    public Node FindNode(int theConnection)
    {
        return MyStations.SelectMany(stn => stn.MyNodes).FirstOrDefault(nde => nde.MyReferenceNumber == theConnection);
    }
    private Node FindNode(string theDirection, Link theLink)
    {
        try
        {
            foreach (var stn in MyStations)
            {
                foreach (var nde in stn.MyNodes)
                {
                    switch (theDirection)
                    {
                        case "R":
                            {
                                if (nde.MyLeftLinks.Any(ll => ll.MyReferenceNumber == theLink.MyReferenceNumber))
                                {
                                    return nde;
                                }

                                break;
                            }
                        case "L":
                            {
                                if (nde.MyRightLinks.Any(rr => rr.MyReferenceNumber == theLink.MyReferenceNumber))
                                {
                                    return nde;
                                }

                                break;
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
    private Node FindNextNode(string theDirection, Path thePath)
    {
        try
        {
            foreach (var stn in MyStations)
            {
                foreach (var nde in stn.MyNodes)
                {
                    switch (theDirection)
                    {
                        case "R":
                            {
                                foreach (var rl in nde.MyRightLinks)
                                {
                                    if (rl.MyReferenceNumber == thePath.MyLinkRight.MyReferenceNumber)
                                    {
                                        return FindNode(theDirection, rl);
                                    }
                                }

                                break;
                            }
                        default:
                            {
                                foreach (var ll in nde.MyLeftLinks)
                                {
                                    if (ll.MyReferenceNumber == thePath.MyLinkLeft.MyReferenceNumber)
                                    {
                                        return FindNode(theDirection, ll);
                                    }
                                }

                                break;
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
    private Node FindNodeInReverse(string theDirection, Link theLink)
    {
        try
        {
            foreach (var stn in MyStations)
            {
                foreach (var nde in stn.MyNodes)
                {
                    switch (theDirection)
                    {
                        case "L":
                            {
                                if (nde.MyLeftLinks.Any(ll => ll.MyReferenceNumber == theLink.MyReferenceNumber))
                                {
                                    return nde;
                                }

                                break;
                            }
                        case "R":
                            {
                                if (nde.MyRightLinks.Any(rr => rr.MyReferenceNumber == theLink.MyReferenceNumber))
                                {
                                    return nde;
                                }

                                break;
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
    private Node FindNodeInReverse(string theDirection, Path thePath)
    {
        try
        {
            foreach (var stn in MyStations)
            {
                foreach (var nde in stn.MyNodes)
                {
                    switch (theDirection)
                    {
                        case "L":
                            {
                                foreach (var rl in nde.MyRightLinks)
                                {
                                    if (rl.MyReferenceNumber == thePath.MyLinkRight.MyReferenceNumber) return nde;
                                }

                                break;
                            }
                        default:
                            {
                                foreach (var ll in nde.MyLeftLinks)
                                {
                                    if (ll.MyReferenceNumber == thePath.MyLinkLeft.MyReferenceNumber) return nde;
                                }

                                break;
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
    private Link FindLinkFromPlatform(Platform thePlatform)
    {
        try
        {
            foreach (var stn in MyStations)
            {
                if (thePlatform.StationId == stn.MyReferenceNumber)
                {
                    foreach (var nde in stn.MyNodes)
                    {
                        foreach (var rl in nde.MyRightLinks)
                        {
                            foreach (var pt in rl.MyPlatforms)
                            {
                                if (pt.MyReferenceNumber == thePlatform.MyReferenceNumber)
                                {
                                    return rl;
                                }
                            }
                        }

                        foreach (var ll in nde.MyLeftLinks)
                        {
                            foreach (var pt in ll.MyPlatforms)
                            {
                                if (pt.MyReferenceNumber == thePlatform.MyReferenceNumber)
                                {
                                    return ll;
                                }
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
        return null;
    }
    private Route FindPathBetweenPlatforms(Link BeginLink, Link EndLink, string theDirection)
    {
        try
        {
            Node nextNode = null;
            //Create a route to store the paths
            var theRoute = Route.CreateInstance();

            //Find the begin node of the starting platform
            var beginNode = FindNode(theDirection, BeginLink);

            //Find the end node of the ending platform
            var endNode = FindNodeInReverse(theDirection, EndLink);

            //if either node is null then stop
            if (beginNode == null || endNode == null) return null;

#if DEBUG
            theLogger.LogInfo($"beginNode: {beginNode.MyReferenceNumber}, endNode: {endNode.MyReferenceNumber}");
#endif


            //Until we get the route plan this is an alternative
            var nextLink = BeginLink;
            nextNode = beginNode;
            //Find the initial path inside the node that is not diverging but straight path
            var thePath = FindPathInNodeFromLink(nextLink, nextNode, theDirection, false) ?? FindPathInNodeFromLink(BeginLink, beginNode, theDirection, true);

            var counter = 0;
            while (true)
            {

                //If path is found, find the next node from the path's link. If direction right then use right connection, and vice versa
                if (thePath != null) nextNode = FindNextNode(theDirection, thePath);

                if (nextNode != null && thePath != null)
                {
                    theRoute.MyPaths.Add(thePath);
#if DEBUG
                    theLogger.LogInfo($"nextNode: {nextNode.MyReferenceNumber}, thePath.MyReferenceNumber: {thePath.MyReferenceNumber}");
#endif
                    if (nextNode.Equals(endNode))
                    {
                        thePath = FindPathInNodeFromLinkReverse(EndLink, endNode, theDirection, true);
                        if (thePath != null)
                        {
                            theRoute.MyPaths.Add(thePath);
                            return theRoute;
                        }
                    }
                    else
                    {
                        if (thePath != null)
                        {
                            nextLink = FindLinkFromPath(theDirection, thePath);
                            nextNode = FindNextNodeFromLink(theDirection, nextLink);
                            thePath = FindPathInNodeFromLink(nextLink, nextNode, theDirection, false) ?? FindPathInNodeFromLink(BeginLink, beginNode, theDirection, true);
                        }
                    }
                }
                counter += 1;
                if (counter > 50) return null;
            }
        }
        catch (Exception e)
        {
            theLogger.LogException(e.ToString());
        }
        return null;
    }
    private Node FindNextNodeFromLink(string theDirection, Link theLink)
    {
        try
        {
            return FindNode(theDirection == "R" ? theLink.MyConnectionRight : theLink.MyConnectionLeft);
        }
        catch (Exception e)
        {
            theLogger.LogException(e.ToString());
        }
        return null;
    }
    private Node FindNextNodeFromPath(string theDirection, Path thePath)
    {
        try
        {
            return theDirection == "R" && thePath.MyDirection == theDirection
                ? FindNode(thePath.MyLinkRight.MyConnectionRight)
                : FindNode(thePath.MyLinkLeft.MyConnectionLeft);
        }
        catch (Exception e)
        {
            theLogger.LogException(e.ToString());
        }

        return null;
    }
    public Path GetPath(string routeName)
    {
        foreach (var stn in MyStations)
        {
            foreach (var node in stn.MyNodes)
            {
                foreach (var path in node.MyPaths)
                {
                    if (path.MyRouteName == routeName) return path;
                }
            }
        }

        return null;
    }
    public Link? FindLinkFromPath(string theDirection, Path thePath)
    {
        try
        {
            return theDirection == "R" && thePath.MyDirection == theDirection ? thePath.MyLinkRight : thePath.MyLinkLeft;
        }
        catch (Exception e)
        {
            theLogger.LogException(e.ToString());
        }
        return null;
    }
    private Path? FindPathInNodeFromLink(Link theLink, Node theNode, string theDirection, bool ignoreDiverging)
    {
        try
        {
            foreach (var path in theNode.MyPaths)
            {
                if (theDirection == "R" && path.MyDirection == theDirection)
                {
                    if (theLink.MyReferenceNumber == path.MyLinkLeft.MyReferenceNumber)
                    {
                        if (ignoreDiverging)
                        {
                            return path;
                        }
                        if (!path.IsDiverging)
                        {
                            return path;
                        }
                    }
                }
                else
                {
                    if (path.MyDirection == theDirection && theLink.MyReferenceNumber == path.MyLinkRight.MyReferenceNumber)
                    {
                        if (ignoreDiverging) return path;
                        if (!path.IsDiverging) return path;
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
    private Path FindPathInNodeFromLinkReverse(Link theLink, Node theNode, string theDirection, bool ignoreDiverging)
    {
        try
        {
            foreach (var path in theNode.MyPaths)
            {
                if (theDirection == "L" && path.MyDirection == theDirection)
                {
                    if (path.MyLinkLeft != null)
                    {
                        if (theLink.MyReferenceNumber == path.MyLinkLeft.MyReferenceNumber)
                        {
                            if (ignoreDiverging)
                            {
                                return path;
                            }

                            if (!path.IsDiverging)
                            {
                                return path;
                            }
                        }

                    }
                }
                else
                {
                    if (path.MyLinkRight != null)
                    {
                        if (theLink.MyReferenceNumber == path.MyLinkRight.MyReferenceNumber && path.MyDirection == theDirection)
                        {
                            if (ignoreDiverging)
                            {
                                return path;
                            }

                            if (!path.IsDiverging)
                            {
                                return path;
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
        return null;
    }
    private void BuildLeftLinkReferences()
    {
        try
        {
            foreach (var stn in MyStations)
            {
                foreach (var node in stn.MyNodes)
                {
                    foreach (var rl in node.MyRightLinks)
                    {
                        var nodeToright = FindNode(rl.MyConnectionRight);
                        if (nodeToright != null)
                        {
                            nodeToright.MyLeftLinks.Add(rl);
                        }
                    }

                    foreach (var path in node.MyPaths)
                    {
                        if (path.MyLinkLeft == null)
                        {
                            var leftLink = GetLink(path.MyConnectionLeft);
                            path.MyLinkLeft = leftLink;
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

    public Link GetLink(int theUid)
    {
        foreach (var stn in MyStations)
        {
            foreach (var node in stn.MyNodes)
            {
                foreach (var rl in node.MyRightLinks)
                {
                    if (rl.MyReferenceNumber == theUid) return rl;
                }
                foreach (var rl in node.MyLeftLinks)
                {
                    if (rl.MyReferenceNumber == theUid) return rl;
                }

            }

        }

        return null;
    }
    #endregion

}
public record LinksData 
{
    public string StationName { get; set; }
    public List<Link> MyLeftLinks { get; set; } = new();
    public List<Link> MyRightLinks { get; set; } = new();
}