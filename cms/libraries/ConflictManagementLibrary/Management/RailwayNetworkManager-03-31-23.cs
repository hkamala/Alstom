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
using Amqp.Framing;
using ConflictManagementLibrary.Logging;
using ConflictManagementLibrary.Model.Movement;
using ConflictManagementLibrary.Network;
using Newtonsoft.Json;
using JPath = System.IO;
using Path = ConflictManagementLibrary.Network.Path;


namespace ConflictManagementLibrary.Management
{
    public class RailwayNetworkManager
    {
        public List<Station> MyStations { get; set; } = new List<Station>();
        private readonly IMyLogger theLogger;
        private const string Filename = "RigaJunction.json";
        private const string FolderName = "Data";
        private const string FileNameMovementPlans = "MovementPlans.json";

        public List<MovementPlan> MyMovementPlans = new List<MovementPlan>();
        public RailwayNetworkManager(IMyLogger theLogger)
        {
            this.theLogger = theLogger;
            InitializeRailwayNetwork();
        }
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

                filePath = JPath.Path.Combine(dir, FolderName, FileNameMovementPlans);
                using (var r = new StreamReader(filePath))
                {
                    var json = r.ReadToEnd();
                    MyMovementPlans = JsonConvert.DeserializeObject<List<MovementPlan>>(json);
                }

                if (MyMovementPlans != null)
                    foreach (var mp in MyMovementPlans)
                    {
                        mp.CheckForDuplicateRouteActions();
                        theLogger.LogInfo(mp.GetMovementPlanInformation());
                    }
            }

            catch (Exception e)
            {
                theLogger?.LogException(e);
            }
        }
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
        public Station FindStationById(int id) => MyStations.FirstOrDefault(s => s.MyReferenceNumber== id);
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
        private Link FindLinkFromPath(string theDirection, Path thePath)
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
        private Path FindPathInNodeFromLink(Link theLink, Node theNode, string theDirection, bool ignoreDiverging)
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
                        if (path.MyDirection == theDirection && theLink.MyReferenceNumber == path.MyLinkRight.MyReferenceNumber )
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
    }
}