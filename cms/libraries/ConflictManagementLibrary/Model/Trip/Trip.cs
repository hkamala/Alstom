using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using ConflictManagementLibrary.Helpers;
using ConflictManagementLibrary.Logging;
using ConflictManagementLibrary.Management;
using ConflictManagementLibrary.Model.Conflict;
using ConflictManagementLibrary.Model.Movement;
using ConflictManagementLibrary.Network;
using ConflictManagementLibrary.Model.Reservation;
using ConflictManagementLibrary.Model.Schedule;
using Newtonsoft.Json;
using Platform = ConflictManagementLibrary.Network.Platform;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using RailgraphLib.Interlocking;
using System.Xml.Linq;
using static ConflictManagementLibrary.Management.TrainAutoRoutingManager;
using System.Linq;

namespace ConflictManagementLibrary.Model.Trip
{
    public class Trip
    {
        #region Declarations
        private readonly IMyLogger? theLogger;
        public string? MyGuid = Guid.NewGuid().ToString();
        public string? CtcUid { get; set; }
        public int SerUid { get; set; }
        public string? ScheduledPlanId { get; set; }
        public string? Name { get; set; }
        public string? TripCode { get; set; }
        public int TripId { get; set; }
        public string? SysUid { get; set; }
        public string? ScheduledPlanName { get; set; }
        public int Number { get; set; }
        public string? StartTime { get; set; }
        public string? StartPosition { get; set; }
        public string? EndPosition { get; set; }
        public string Direction { get; set; } = "";
        public TrainType TypeOfTrain { get; set; }
        public TrainSubType SubType { get; set; }
        public string? TrainTypeString { get; set; }
        public int TrainPriority { get; set; }
        public int Length { get; set; }
        public PostFixType Postfix { get; set; }
        public TrainLocation LocationCurrent { get; set; } = new TrainLocation();
        public TrainLocation LocationNext { get; set; } = new TrainLocation();
        public List<Conflict.Conflict> MyConflicts { get; set; } = new List<Conflict.Conflict>();
        public int ConflictCount { get; set; }
        public List<Reservation.Reservation> MyReservations { get; set; } = new List<Reservation.Reservation>();
        public List<TimedLocation> TimedLocations { get; set; } = new List<TimedLocation>();
        public SortedDictionary<int, Reservation.Reservation> TripReservations { get; set; } = new SortedDictionary<int, Reservation.Reservation>();
        public bool IsAllocated;
        public bool IsCompleted;
        [JsonIgnore] public DateTime PlanStartTime;
        [JsonIgnore] public bool CompleteRoutePlanSent;
        [JsonIgnore] public int ScheduledPlanDayCode { get; set; }
        [JsonIgnore] public List<ConflictObject> MyConflictObjects = new List<ConflictObject>();
        [JsonIgnore] public List<TrainPosition> MyTrainPositions = new List<TrainPosition>();
        [JsonIgnore] public List<TrainAutoRoutingManager.RouteMarkInfo> MyRouteMarkingCurrent;

        [JsonIgnore] public string MyTrainSystemUid;
        [JsonIgnore] public string MyTrainObid;
        [JsonIgnore] public DateTime LastPositionUpdate;
        [JsonIgnore] public List<TriggerPoint> MyTriggerPoints = new List<TriggerPoint>();
        [JsonIgnore] public List<SignalPoint> MySignalPoints = new List<SignalPoint>();
        [JsonIgnore] public List<Platform> MyPlatforms = new List<Platform>();
        [JsonIgnore] public List<Path> MyStationNecks = new List<Path>();


        public TrainPosition MyTrainPosition;
        public enum TrainType
        {
            Passenger,
            Freight, 
            Default
        }
        public enum TrainSubType
        {
            //Spare=0,
            //Repair=1,
            //Passenger=2,
            //Local=3,
            //Suburban=4,
            //FastFreight=5,
            //Freight=6,
            //Locomotive=7,
            //Utility=8,
            //Other=9
            Spare = 0,
            Train = 1,
            Repair = 2,
            Passenger = 3,
            Local = 4,
            Suburban = 5,
            FastFreight = 6,
            Freight = 7,
            Nothing = 8,
            Utility = 9,
            None = 10,
            Other = 11,
            Locomotive = 12
        }
        public enum PostFixType
        {
            None,
            G,
            N,
            S,
            M,
            BM,
            A
        }

        [JsonIgnore] public TimedLocation LastTimedLocation;
        [JsonIgnore] public int MyAverageMetersPerSecond;
        #endregion
        #region Constructor
        public Trip( IMyLogger? theLogger)
        {
            this.theLogger = theLogger;
        }
        public void CreateReservations(List<MovementPlan> GlobalMovementPlans, List<Station> Stations)
        {
            try
            {

                var index = 0;
                //Get the movement plan between time locations
                foreach (var tl in TimedLocations)
                {
                    if (index < TimedLocations.Count - 1)
                    {
                        var nextLocation = TimedLocations[index + 1];
                        tl.MyMovementPlan = GetMovementPlan(GlobalMovementPlans, tl.Description, nextLocation.Description);
                        if (tl.MyMovementPlan == null)
                        {
                            GlobalDeclarations.MyLogger.LogException("Movement Plan not found for Trip <" + TripCode + "> From Location <" + tl.Description + "> To Location <" + nextLocation.Description + ">");
                        }
                    }

                    index++;
                }

                //For each route action in movement plan make a reservation on the path and the link to the next node
                var startIndex = 1;
                index = 0;
                foreach (var tl in TimedLocations)
                {
                    if (tl.MyMovementPlan != null)
                    {
                        foreach (var ra in tl.MyMovementPlan.MyRouteActions)
                        {
                            var path = GetPath(Stations, ra.RouteName);
                            if (path != null)
                            {
                                var nextLocation = TimedLocations[index + 1];
                                var stn = GetStation(Stations, path.MyStationName);
                                if (stn == null)
                                {
                                    Console.Beep();
                                };
                                var node = GetNode(stn, path);
                                if (node == null) continue;
                                var reservePath = new Reservation.Reservation(stn.StationName, node, path, null, tl, nextLocation, this);
                                reservePath.MyTripId = this.Number;
                                reservePath.MyTripCode = this.TripCode;
                                reservePath.MyTripStartTime = this.StartTime;
                                AddReservationToLists(stn, reservePath, startIndex);
                                DoConflictCheck(stn, reservePath,true);
                                startIndex++;

                                var link = GetNextLink(this.Direction, path);
                                if (IsLinkReserved(link)) continue;
                                var reserveLink = new Reservation.Reservation(stn.StationName, node, null, link, tl, nextLocation, this);
                                reserveLink.MyTripId = this.Number;
                                reserveLink.MyTripCode = this.TripCode;
                                reserveLink.MyTripStartTime = this.StartTime;
                                reserveLink.MyTripGuid = this.MyGuid;
                                AddReservationToLists(stn, reserveLink, startIndex);
                                DoConflictCheck(stn, reserveLink,true);
                                startIndex++;
                            }
                        }
                    }
                    index++;
                }
            }
            catch (Exception e)
            {
                GlobalDeclarations.MyLogger.LogException(e.ToString());
            }

        }
        public void CreateTripReservations(List<MovementPlan> GlobalMovementPlans, List<Station> Stations)
        {
            try
            {

                var index = 0;
                //Get the movement plan between time locations
                foreach (var tl in TimedLocations)
                {
                    if (index < TimedLocations.Count - 1)
                    {
                        var nextLocation = TimedLocations[index + 1];
                        tl.MyMovementPlan = GetMovementPlan(GlobalMovementPlans, tl.Description, nextLocation.Description);
                        if (tl.MyMovementPlan == null)
                        {
                            GlobalDeclarations.MyLogger.LogException("Movement Plan not found for Trip <" + TripCode + "> From Location <" + tl.Description + "> To Location <" + nextLocation.Description + ">");
                        }
                    }

                    index++;
                }

                //For each route action in movement plan make a reservation on the path and the link to the next node
                var startIndex = 1;
                index = 0;

                foreach (var tl in TimedLocations)
                {
                    var nextTimeLocation = TimedLocations[index + 1];
                    if (nextTimeLocation.SystemGuid == TimedLocations[TimedLocations.Count - 1].SystemGuid) { break; }
                    var totalTripTime = GetTimeLocationTimeSpan(tl, nextTimeLocation);
                    var totalLinks = GetLinkList(tl, Stations);
                    var totalTripDistance = GetTotalDistanceBetweenTimedLocations(totalLinks);
                    var MetersPerSecond = totalTripDistance / totalTripTime.TotalSeconds;
                    var beginTime = tl.DepartureTimeActual;
                    var endTime = tl.ArrivalTimeActual;
                    foreach (var link in totalLinks)
                    {
                        if (link.MyEdgeAssociation == null)
                        {
                            Console.Beep();
                            continue;
                        }
                        var i = totalLinks.IndexOf(link);
                        if (i == 0) 
                        {
                            var dis = link.MyEdgeAssociation!.MyDistanceInMeters / 2;
                            var totalSeconds = dis / MetersPerSecond;
                            if (totalSeconds < 0)
                            {
                                Console.Beep();
                            }

                            endTime = beginTime.AddSeconds(totalSeconds);
                        }
                        else if (i == totalLinks.Count - 1)
                        {
                            var dis = link.MyEdgeAssociation!.MyDistanceInMeters / 2;
                            var totalSeconds = dis / MetersPerSecond;
                            if (totalSeconds < 0)
                            {
                                Console.Beep();
                            }

                            endTime = beginTime.AddSeconds(totalSeconds);
                        }
                        else
                        {
                            var dis = link.MyEdgeAssociation!.MyDistanceInMeters;
                            var totalSeconds = dis / MetersPerSecond;
                            if (totalSeconds < 0)
                            {
                                Console.Beep();
                            }
                            endTime = beginTime.AddSeconds(totalSeconds);
                        }

                        var stn = GetStation(Stations, link);
                        if (stn == null) continue;
                        var node = GetNode(Stations, link);
                        var stnName = stn.StationName;
                        var nodeNumber = node?.MyReferenceNumber.ToString();
                        var reserveLink = new Reservation.Reservation(link, beginTime, endTime, tl, nextTimeLocation, stnName, nodeNumber, this);
                        reserveLink.MyTripId = this.Number;
                        reserveLink.MyTripCode = this.TripCode;
                        reserveLink.MyTripStartTime = this.StartTime;
                        AddReservationToLists(stn, reserveLink, startIndex);
                        DoConflictCheck(stn, reserveLink,false,false);
                        startIndex++;
                        beginTime = endTime;
                    }
                    index += 1;
                }
            }
            catch (Exception e)
            {
                GlobalDeclarations.MyLogger.LogException(e.ToString());
            }

        }
        private static MovementPlan CreateMovementPlan(ScheduledPlan thePlan, string beginPlatform, string endPlatform)
        {
            var theMovementPlan = new MovementPlan();
            try
            {
                var enumerable = thePlan.ScheduledRoutePlan?.TMSRoutePlan?.data.RoutePlan?.Trains[0].Items;
                if (enumerable != null)
                    foreach (var item in enumerable)
                    {
                        if (item.From.ename == beginPlatform && item.To.ename == endPlatform)
                        {
                            theMovementPlan.Description = beginPlatform + " to " + endPlatform;
                            theMovementPlan.FromName = beginPlatform;
                            theMovementPlan.ToName = endPlatform;
                            foreach (var action in item.MasterRoute[0].Actions)
                            {
                                RouteAction theRoute = new RouteAction();
                                theRoute.ActionLocation = action.Obj.ename;
                                theRoute.ActionType = "ROUTE_AP_TIMING";
                                theRoute.RouteName = action.Command?[0].value;
                                theRoute.GetRouteInfo();
                                theMovementPlan.MyRouteActions.Add(theRoute);
                            }
                        }
                    }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }

            return theMovementPlan;
        }
        public void CreateTriggerPoints()
        {
            string excep = "";
            try
            {
                foreach (var tl in TimedLocations)
                {
                    if (tl.MyMovementPlan  == null) continue;
                    foreach (var ra in tl.MyMovementPlan.MyRouteActions)
                    {
                        excep = ra.ActionLocation;
                        var ilElement = MyRailGraphManager?.ILGraph?.getGraphObj(ra.ActionLocation);
                        if (ilElement == null) continue;
                        var triggerPoint = TriggerPoint.CreateInstance(ra.ActionLocation, ilElement, ra.RouteName);
                        MyTriggerPoints.Add(triggerPoint);
                        MyLogger?.LogInfo("CreateTriggerPoints:Trigger Point Created <" + ra.ActionLocation +"> for Trip <" + TripCode + ">");
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }
        public void CreateSignalPoints()
        {
            try
            {
                foreach (var tl in TimedLocations)
                {
                    if (tl.MyMovementPlan == null) continue;
                    foreach (var ra in tl.MyMovementPlan.MyRouteActions)
                    {
                        var signalNames = ra.RouteName?.Split("-");
                        if (signalNames != null && signalNames.Length > 0)
                        {
                            var theSignalName = signalNames[0];
                            var signal = MyRailGraphManager?.ILGraph?.getGraphObj(theSignalName);
                            if (signal == null || signal is not SignalOptical) continue;

                            var theSignalPoint = SignalPoint.CreateInstance(theSignalName, signal);
                            if (theSignalPoint != null)
                            {
                                MySignalPoints.Add(theSignalPoint);
                                MyLogger?.LogInfo("CreateSignalPoints:Signal Point Created <" + ra.RouteName + "> for Trip <" + TripCode + ">");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }
        public void CreatePlatformPoints()
        {
            try
            {
                foreach (var loc in this.TimedLocations)
                {
                    if (loc == null || !loc.HasStopping) continue;
                    var platform = MyRailwayNetworkManager?.FindPlatform(loc.Description);
                    if (platform == null) continue;
                    MyPlatforms.Add(platform);
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }
        public void DoIdentifyStationNecks()
        {
            try
            {
                foreach (var tl in TimedLocations)
                {
                    foreach (var ra in tl.MyMovementPlan.MyRouteActions)
                    {
                        if (IsStationNeck(ra.RouteName))
                        {
                          var path =  MyRailwayNetworkManager?.GetPath(ra.RouteName);
                          if (path != null)
                          {
                              MyStationNecks.Add(path);
                              var otherPath = MyRailwayNetworkManager?.GetPath(path.MyConflictingStationNeckPath);
                              if (otherPath != null)
                              {
                                  MyStationNecks.Add(otherPath);
                              }
                          }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }

        private bool IsStationNeck(string routeName)
        {
            try
            {
                foreach (var sn in MyRailwayNetworkManager.MyStationNecks)
                {
                    if (sn.MyRouteName == routeName) return true;
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
            return false;
        }
        public void CreateTripReservationsFromPlan(ScheduledPlan thePlan, List<Station> Stations)
        {
            try
            {
                var lastLocation = false;
                var index = 0;
                //Get the movement plan between time locations
                foreach (var tl in TimedLocations)
                {
                    if (index < TimedLocations.Count - 1)
                    {
                        var nextLocation = TimedLocations[index + 1];
                        tl.MyMovementPlan = CreateMovementPlan(thePlan, tl.Description, nextLocation.Description);
                        if (tl.MyMovementPlan == null)
                        {
                            MyLogger?.LogException("Movement Plan not found for Trip <" + TripCode + "> From Location <" + tl.Description + "> To Location <" + nextLocation.Description + ">");
                        }
                    }
                    index++;
                }

                //For each route action in movement plan make a reservation on the path and the link to the next node
                var startIndex = 1;
                index = 0;
                foreach (var tl in TimedLocations)
                {
                    var nextTimeLocation = TimedLocations[index + 1];
                    if (nextTimeLocation.SystemGuid == TimedLocations[TimedLocations.Count - 1].SystemGuid)
                    {
                        lastLocation = true;
                    }
                    var totalTripTime = GetTimeLocationTimeSpan(tl, nextTimeLocation);
                    var totalLinks = GetLinkList(tl, Stations);
                    tl.MyReservationLinks = totalLinks;
                    var totalTripDistance = GetTotalDistanceBetweenTimedLocations(totalLinks);
                    var metersPerSecond = totalTripDistance / totalTripTime.TotalSeconds;
                    tl.MyMetersPerSecondToNextLocation = metersPerSecond;
                    tl.MyTotalTripDistanceToNextLocation = totalTripDistance;
                    tl.MyNextTimedLocation = nextTimeLocation;
                    var beginTime = tl.DepartureTimeActual;
                    var endTime = tl.ArrivalTimeActual;
                    foreach (var link in totalLinks)
                    {
                        if (link.MyEdgeAssociation == null)
                        {
                            continue;
                        }
                        var i = totalLinks.IndexOf(link);
                        if (i == 0)
                        {
                            var dis = link.MyEdgeAssociation!.MyDistanceInMeters / 2;
                            var totalSeconds = dis / metersPerSecond;
                            endTime = beginTime.AddSeconds(totalSeconds);
                        }
                        else if (i == totalLinks.Count - 1)
                        {
                            var dis = link.MyEdgeAssociation!.MyDistanceInMeters / 2;
                            var totalSeconds = dis / metersPerSecond;
                            endTime = beginTime.AddSeconds(totalSeconds);
                        }
                        else
                        {
                            var dis = link.MyEdgeAssociation!.MyDistanceInMeters;
                            var totalSeconds = dis / metersPerSecond;
                            endTime = beginTime.AddSeconds(totalSeconds);
                        }

                        var stn = GetStation(Stations, link);
                        if (stn == null)
                        {
                            MyLogger?.LogException("Station not found for Trip <" + TripCode + "> From Location <" + tl.Description + "> To Location <" + nextTimeLocation.Description + "> Link Ref < " + link.MyReferenceNumber + ">");
                        }
                        else
                        {
                            var node = GetNode(Stations, link);
                            var stnName = stn.StationName;
                            var nodeNumber = node?.MyReferenceNumber.ToString();
                            var reserveLink = new Reservation.Reservation(link, beginTime, endTime, tl, nextTimeLocation, stnName, nodeNumber, this);
                            reserveLink.MyTripId = this.Number;
                            reserveLink.MyTripCode = this.TripCode;
                            reserveLink.MyTripStartTime = this.StartTime;
                            reserveLink.MyMetersPerSecond = Convert.ToInt32(metersPerSecond);
                            AddReservationToLists(stn, reserveLink, startIndex);
                            if (MyEnableAutomaticConflictResolution) DoConflictCheck(stn, reserveLink,false,false);
                            startIndex++;
                            beginTime = endTime;
                        }
                    }

                    if (lastLocation)
                    {
                        CalculateAverageMetersPerSecond();
                        break;
                    }
                    index += 1;
                }
            }
            catch (Exception e)
            {
                GlobalDeclarations.MyLogger?.LogException(e.ToString());
            }
        }
        public void UpdateConflicts()
        {
            try
            {
                var stations = MyRailwayNetworkManager?.MyStations;

                foreach (var reservation in MyReservations)
                {
                    var stn = GetStation(stations, reservation.MyStationName);
                    if (stn != null)
                    {
                        DoConflictCheck(stn, reservation,false,false);
                    }
                }
            }
            catch (Exception e)
            {
                GlobalDeclarations.MyLogger?.LogException(e.ToString());
            }
        }
        public void UpdateTripReservations(bool updateReservation = true)
        {
            try
            {
                var index = 0;

                var stations = MyRailwayNetworkManager?.MyStations;
                //For each route action in movement plan make a reservation on the path and the link to the next node
                index = 0;
                foreach (var tl in TimedLocations)
                {
                    var nextTimeLocation = TimedLocations[index + 1];
                    if (nextTimeLocation.SystemGuid == TimedLocations[TimedLocations.Count - 1].SystemGuid)
                    {
                        break;
                    }
                    var totalTripTime = GetTimeLocationTimeSpan(tl, nextTimeLocation);
                    //var totalLinks = GetLinkList(tl, stations);
                    var totalLinks = tl.MyReservationLinks;
                    if (tl.MyReservationLinks == null)
                    {
                        
                        continue;
                    }
                    var totalTripDistance = GetTotalDistanceBetweenTimedLocations(totalLinks);
                    var MetersPerSecond = totalTripDistance / totalTripTime.TotalSeconds;
                    //var MetersPerSecond = this.MyAverageMetersPerSecond;
                    var beginTime = tl.DepartureTimeAdjusted;
                    var endTime = tl.ArrivalTimeAdjusted;
                    foreach (var link in totalLinks)
                    {
                        if (link.MyEdgeAssociation == null)
                        {
                            continue;
                        }
                        var i = totalLinks.IndexOf(link);
                        if (i == 0)
                        {
                            var dis = link.MyEdgeAssociation!.MyDistanceInMeters / 2;
                            var totalSeconds = dis / MetersPerSecond;
                            endTime = beginTime.AddSeconds(totalSeconds);
                        }
                        else if (i == totalLinks.Count - 1)
                        {
                            var dis = link.MyEdgeAssociation!.MyDistanceInMeters / 2;
                            var totalSeconds = dis / MetersPerSecond;
                            endTime = beginTime.AddSeconds(totalSeconds);
                        }
                        else
                        {
                            var dis = link.MyEdgeAssociation!.MyDistanceInMeters;
                            var totalSeconds = dis / MetersPerSecond;
                            endTime = beginTime.AddSeconds(totalSeconds);
                        }

                        var stn = GetStation(stations, link);
                        if (stn == null)
                        {
                            MyLogger?.LogException("Station not found for Trip <" + TripCode + "> From Location <" + tl.Description + "> To Location <" + nextTimeLocation.Description + "> Link Ref < " + link.MyReferenceNumber + ">");
                        }
                        else
                        {
                            var node = GetNode(stations, link);
                            var stnName = stn.StationName;
                            var nodeNumber = node?.MyReferenceNumber.ToString();
                            //Find Reservation and update
                            var reservation = GetReservation(link.MyReferenceNumber);
                            reservation.HasBeenUpdated = true;
                            reservation.TimeBegin = beginTime;
                            reservation.TimeEnd = endTime;
                            if (MyEnableAutomaticConflictResolution) DoConflictCheck(stn, reservation, true, true);
                            beginTime = endTime;
                        }
                    }
                    index += 1;
                }

                foreach (var r in MyReservations)
                {
                    r.HasBeenUpdated = false;
                }
            }
            catch (Exception e)
            {
                GlobalDeclarations.MyLogger?.LogException(e.ToString());
            }

        }
        private Reservation.Reservation GetReservation(int theReference)
        {
            try
            {
                foreach (var r in MyReservations)
                {
                    if (r.MyLink?.MyReferenceNumber == theReference && !r.HasBeenUpdated) return r;
                }
            }
            catch (Exception e)
            {
                GlobalDeclarations.MyLogger?.LogException(e.ToString());
            }

            return null;
        }
        public void CleanUp()
        {
            try
            {
                this.IsAllocated = false;
                this.MyTrainPosition = null;

                this.LastTimedLocation = null;
                this.LocationCurrent = null;
                this.MyRouteMarkingCurrent = new List<RouteMarkInfo>();

                foreach (var tl in TimedLocations)
                {
                    tl.HasArrivedToPlatform = false;
                    tl.HasDepartedFromPlatform = false;
                    tl.RouteIsExecutedForTrip = false;
                    tl.HasSentRouteMarker = false;
                    tl.IsPastTriggerPoint = false;
                    if (tl.MyMovementPlan == null) continue;
                    foreach (var ra in tl.MyMovementPlan.MyRouteActions)
                    {
                        ra.HasBeenSentToRos=false;
                        ra.HasConfirmationRouteClearedFromRos = false;
                        ra.HasSignalBeenCleared = false;
                        ra.IsOnTriggerPoint = false;
                        ra.IsTrainPastStartOfRoute = false;
                        tl.RouteIsAlreadyAvailableForTrip = false;
                        tl.UseAlternatePlatform = false;
                        tl.RemoveActionPointsFromRoutePlan = false;
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }
        public void UpdateTripReservations()
        {
            try
            {
                var lastLocation = false;
                var index = 0;

                var stations = MyRailwayNetworkManager?.MyStations;
                //For each route action in movement plan make a reservation on the path and the link to the next node
                var startIndex = 1;
                index = 0;
                foreach (var tl in TimedLocations)
                {
                    var nextTimeLocation = TimedLocations[index + 1];
                    if (nextTimeLocation.SystemGuid == TimedLocations[TimedLocations.Count - 1].SystemGuid)
                    {
                        lastLocation = true;
                    }
                    var totalTripTime = GetTimeLocationTimeSpan(tl, nextTimeLocation);
                    var totalLinks = GetLinkList(tl, stations);
                    var totalTripDistance = GetTotalDistanceBetweenTimedLocations(totalLinks);
                    var MetersPerSecond = totalTripDistance / totalTripTime.TotalSeconds;
                    var beginTime = tl.DepartureTimeAdjusted;
                    var endTime = tl.ArrivalTimeAdjusted;
                    foreach (var link in totalLinks)
                    {
                        if (link.MyEdgeAssociation == null)
                        {
                            continue;
                        }
                        var i = totalLinks.IndexOf(link);
                        if (i == 0)
                        {
                            var dis = link.MyEdgeAssociation!.MyDistanceInMeters / 2;
                            var totalSeconds = dis / MetersPerSecond;
                            if (totalSeconds < 0)
                            {
                                Console.Beep();
                            }

                            endTime = beginTime.AddSeconds(totalSeconds);
                        }
                        else if (i == totalLinks.Count - 1)
                        {
                            var dis = link.MyEdgeAssociation!.MyDistanceInMeters / 2;
                            var totalSeconds = dis / MetersPerSecond;
                            if (totalSeconds < 0)
                            {
                                Console.Beep();
                            }

                            endTime = beginTime.AddSeconds(totalSeconds);
                        }
                        else
                        {
                            var dis = link.MyEdgeAssociation!.MyDistanceInMeters;
                            var totalSeconds = dis / MetersPerSecond;
                            if (totalSeconds < 0)
                            {
                                Console.Beep();
                            }
                            endTime = beginTime.AddSeconds(totalSeconds);
                        }

                        var stn = GetStation(stations, link);
                        if (stn == null)
                        {
                            MyLogger?.LogException("Station not found for Trip <" + TripCode + "> From Location <" + tl.Description + "> To Location <" + nextTimeLocation.Description + "> Link Ref < " + link.MyReferenceNumber + ">");
                        }
                        else
                        {
                            var node = GetNode(stations, link);
                            var stnName = stn.StationName;
                            var nodeNumber = node?.MyReferenceNumber.ToString();
                            var reserveLink = new Reservation.Reservation(link, beginTime, endTime, tl, nextTimeLocation, stnName, nodeNumber, this);
                            reserveLink.MyTripId = this.Number;
                            reserveLink.MyTripCode = this.TripCode;
                            reserveLink.MyTripStartTime = this.StartTime;
                            reserveLink.MyMetersPerSecond = Convert.ToInt32(MetersPerSecond);
                            AddReservationToLists(stn, reserveLink, startIndex);
                            if (MyEnableAutomaticConflictResolution) DoConflictCheck(stn, reserveLink,true,true);
                            startIndex++;
                            beginTime = endTime;
                        }
                    }

                    if (lastLocation)
                    {
                        CalculateAverageMetersPerSecond();
                        break;
                    }
                    index += 1;
                }
            }
            catch (Exception e)
            {
                GlobalDeclarations.MyLogger?.LogException(e.ToString());
            }

        }
        private void CalculateAverageMetersPerSecond()
        {
            try
            {
                var totalMetersPerSecond = 0;
                foreach (var reservation in MyReservations)
                {
                    totalMetersPerSecond += Convert.ToInt32(reservation.MyMetersPerSecond);
                }

                MyAverageMetersPerSecond = totalMetersPerSecond / MyReservations.Count;
            }
            catch (Exception e)
            {
                GlobalDeclarations.MyLogger?.LogException(e.ToString());
            }
        }
        private TimeSpan GetTimeLocationTimeSpan(TimedLocation beginLocation, TimedLocation endLocation)
        {
            TimeSpan time; //= endLocation.ArrivalTime - beginLocation.DepartureTime;
            if (endLocation.ArrivalTimePlan > beginLocation.DepartureTimePlan)
                time = endLocation.ArrivalTimePlan - beginLocation.DepartureTimePlan;
            else
            {
                time = beginLocation.DepartureTimePlan - endLocation.ArrivalTimePlan;
            }
            if (time < TimeSpan.Zero)
            {
                //Console.Beep();
            }
            return time;
        }
        private List<Link> GetLinkList(TimedLocation? beginLocation, List<Station> Stations)
        {
            List<Link> theLinkList = new List<Link>();
            if (beginLocation?.MyMovementPlan == null) return theLinkList;
            theLogger?.LogInfo("Get Link List from this location <" + beginLocation.Description +">");
            foreach (var ra in beginLocation.MyMovementPlan.MyRouteActions)
            {
                theLogger?.LogInfo("Route Name to get Previous and Next Links <" + ra.RouteName + ">");
                var path = GetPath(Stations, ra.RouteName);
                if (path != null)
                {
                    theLogger?.LogInfo("Path Description <" + path.GetPathDescription() + "> Path Direction <" + path.MyDirection +">");

                    var nextLink = GetNextLink(this.Direction, path);
                    var previousLink = GetPreviousLink(this.Direction, path);
                    if (previousLink != null)
                    {
                        ra.BeginLinkUid = previousLink.MyReferenceNumber.ToString();
                    }
                    if (nextLink != null)
                    {
                        ra.EndLinkUid = nextLink.MyReferenceNumber.ToString();
                    }
                  
                    AddLink(previousLink, theLinkList);
                    AddLink(nextLink, theLinkList);
                }
            }

            return theLinkList;
        }
        private int GetTotalDistanceBetweenTimedLocations(List<Link> theLinkList)
        {
            var totalDistance = 0;

            foreach (var link in theLinkList)
            {
                var i = theLinkList.IndexOf(link);
                if (i == 0)
                {
                   if (link.MyEdgeAssociation != null) totalDistance += link.MyEdgeAssociation!.MyDistanceInMeters / 2;
                }
                else if (i == theLinkList.Count-1)
                {
                    if (link.MyEdgeAssociation != null) totalDistance += link.MyEdgeAssociation!.MyDistanceInMeters / 2;
                }
                else
                {
                    if (link.MyEdgeAssociation != null)  totalDistance += link.MyEdgeAssociation!.MyDistanceInMeters;
                }
            }

            return totalDistance;
        }
        private void AddLink(Link? theLink, List<Link> theLinkList)
        {
            if (theLink == null) return;
            foreach (var l in theLinkList)
            {
                if (theLink.MyReferenceNumber == l.MyReferenceNumber)
                {
                    theLogger?.LogInfo("Link Founded In List <" +l.MyReferenceNumber + "><" + l.MyDescription +">");
                    return;
                }
            }
            theLogger?.LogInfo("Link Added to List <" + theLink.MyReferenceNumber + "><" + theLink.MyDescription + ">");

            theLinkList.Add(theLink);
        }
        #endregion
        #region Trip Train Positions
        public bool AddPosition(TrainPosition? theTrainPosition)
        {
            try
            {
                lock (MyTrainPositions)
                {
                    if (MyTrainPositions.Any(pos => pos.ElementExtension?.StartPos.Offset ==
                                                    theTrainPosition?.ElementExtension?.StartPos.Offset))
                    {
                        return false;
                    }

                    if (theTrainPosition != null)
                    {
                        MyTrainPositions.Add(theTrainPosition);
                        theLogger?.LogInfo("Train Position <" + theTrainPosition?.ElementExtension?.StartPos.AdditionalName +"> added to trip");
                    }
                }
            }
            catch (Exception e)
            {
                theLogger?.LogException(e.ToString());
            }

            return true;
        }
        public bool DoesPositionExist(string theTrackName)
        {
            try
            {
                lock (MyTrainPositions)
                {
                    if (MyTrainPositions.Any(pos => pos.ElementExtension?.StartPos.ElementId == theTrackName))
                    {
                        return true;
                    }
                }

            }
            catch (Exception e)
            {
                theLogger?.LogException(e.ToString());
            }

            return false;
        }
        #endregion
        #region Trip Methods
        public Conflict.Conflict FindConflict(string theGuid)
        {
            try
            {
                foreach (var conflict in MyConflicts)
                {
                    if (conflict.MyGuid == theGuid) return conflict;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return null!;
        }
        private bool IsLinkReserved(Link theLink)
        {
            try
            {
                foreach (var r in MyReservations)
                {
                    if (r.MyLink != null)
                    {
                        if (r.MyLink.MyReferenceNumber == theLink.MyReferenceNumber) return true;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return false;

        }
        private void AddReservationToLists(Station? theStation, Reservation.Reservation theReservation, int startIndex)
        {
            if (theReservation == null)
            {
                return;
            }
            this.MyReservations.Add(theReservation);
            //this.TripReservations.Add(startIndex, theReservation);
            theStation!.MyReservations.Add(theReservation);
        }
        public void DoConflictCheckForPlatform()
        {
            try
            {
                foreach (var tl in TimedLocations)
                {
                    var typeIndex = 5;
                    var stationName = "UNKNOWN";
                    var theConflictDescription = "Platform Does Not Exist @ <" + tl.Description + "> <" + stationName + ">";

                    if (tl.MyPlatform != null) continue;
                    var conflict = Conflict.Conflict.CreateInstanceFromNoPlatform(this, tl, typeIndex,
                        ConflictEntity.EntityType.Station, ConflictType.TypeOfConflict.Station, theConflictDescription,
                        stationName, tl.Description);
                    this.MyConflicts.Add(conflict);
                    lock (TripList)
                    {
                        MyTrainSchedulerManager?.ProduceMessage2003(this);
                        MyLogger?.LogInfo("DoConflictCheckForPlatform:Trip <" + this.TripCode + "> <" + tl.Description + ">");
                    }
                    var message = "Conflict Detected for Trip <" + this.TripCode + "> " + theConflictDescription;
                    MyAutoRoutingManager?.CreateEvent(message, "PLATFORM DOES NOT EXIST", TrainAutoRoutingManager.AlertLevel.WARN, true);
                }
            }
            catch (Exception e)
            {
                theLogger?.LogException(e.ToString());
            }
        }
        public Reservation.Reservation CheckReservationConflictFromTrips(Reservation.Reservation reservationToCheck)
        {
            try
            {
                lock (GlobalDeclarations.TripList)
                {
                    foreach (var trip in TripList)
                    {
                        foreach (var r in trip.MyReservations)
                        {
                            if (reservationToCheck.MyLink == null || r.MyLink == null) continue;
                            if (reservationToCheck.MyTripCode == r.MyTripCode) continue;
                            if (reservationToCheck.ScheduledPlanName == r.ScheduledPlanName) continue;
                            if (reservationToCheck.MyLink.MyReferenceNumber != r.MyLink.MyReferenceNumber) continue;
                            if (reservationToCheck.TimeBegin >= r.TimeBegin && reservationToCheck.TimeBegin <= r.TimeEnd)
                            {
                                return r;
                            }
                            if (reservationToCheck.TimeEnd >= r.TimeBegin && reservationToCheck.TimeEnd <= r.TimeEnd)
                            {
                                return r;
                            }

                            if (reservationToCheck.MyPath == null || r.MyPath == null) continue;
                            if (reservationToCheck.MyPath.MyReferenceNumber != r.MyPath.MyReferenceNumber) continue;
                            if (reservationToCheck.TimeBegin >= r.TimeBegin && reservationToCheck.TimeBegin <= r.TimeEnd)
                            {
                                return r;
                            }
                            if (reservationToCheck.TimeEnd >= r.TimeBegin && reservationToCheck.TimeEnd <= r.TimeEnd)
                            {
                                return r;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return null;
            }

            return null;
        }
        private void DoConflictCheck(Station theStation, Reservation.Reservation theReservation, bool createEvent = true)
        {
            try
            {
                var conflictingReservations = theStation.CheckReservationConflict(theReservation, true);
                foreach (var r in conflictingReservations)
                {
                    string eventMessage = "NOTHING";
                    if (r != null && !DoesConflictExist(r))
                    {
                        var conflict = Conflict.Conflict.CreateInstanceFromReservationConflict(theStation, theReservation, r, this);
                        var otherTrip = GlobalDeclarations.FindTrip(r.MyTripCode, r.MyTripStartTime);
                        if (otherTrip == null)
                        {
                            if (DoesConflictExist(conflict)) continue;
                            MyConflicts.Add(conflict);
                            var message = "Conflict Added for Trip <" + this.TripCode + "> at <" + theReservation.MyEdgeName + "> " + conflict.MyDescription;
                            CreateEventForConflict(message,conflict,true,createEvent);
                            continue;
                        }
                        var conflictOther = Conflict.Conflict.CreateInstanceFromReservationConflict(theStation, r, theReservation, this, false);
                        var otherStartTime = Convert.ToDateTime(otherTrip.StartTime);
                        var thisStartTime = Convert.ToDateTime(this.StartTime);

                        if (otherTrip.TrainPriority == this.TrainPriority)
                        {
                            if (thisStartTime < otherStartTime)
                            {
                                if (!otherTrip.DoesConflictExist(conflictOther))
                                {
                                    otherTrip.MyConflicts.Add(conflictOther);
                                    theLogger?.LogInfo("StartTime:Conflict Added for Trip <" + otherTrip.TripCode + "> at <" + conflictOther.MyReservation.MyEdgeName + ">");
                                    var message = "Conflict Added for Trip <" + otherTrip.TripCode + "> at <" + conflictOther.MyReservation.MyEdgeName + ">";
                                    CreateEventForConflict(message, conflictOther, true, createEvent);
                                    continue;
                                }
                            }
                            else
                            {
                                if (!DoesConflictExist(conflict))
                                {
                                    MyConflicts.Add(conflict);
                                    theLogger?.LogInfo("Other StatTime:Conflict Added for Trip <" + otherTrip.TripCode + "> at <" + conflictOther.MyReservation.MyEdgeName + ">");
                                    var message = "Conflict Added for Trip <" + this.TripCode + "> at <" + theReservation.MyEdgeName + ">";
                                    CreateEventForConflict(message, conflictOther, true, createEvent);
                                    continue;
                                }
                            }
                        }

                        if (this.TrainPriority < otherTrip.TrainPriority)
                        {
                            if (otherTrip.DoesConflictExist(conflictOther)) continue;
                            otherTrip.MyConflicts.Add(conflict);
                            var message = "Conflict Added for Trip <" + otherTrip.TripCode + "> AT <" + conflictOther.MyReservation.MyEdgeName + "> <" + conflictOther.MyDescription;
                            CreateEventForConflict(message, conflictOther, true, createEvent);
                        }
                        else
                        {
                            if (DoesConflictExist(conflict)) continue;
                            MyConflicts.Add(conflict);
                            var message = "Conflict Added for Trip <" + this.TripCode + "> at <" + theReservation.MyEdgeName + ">";
                            CreateEventForConflict(message, conflictOther, true, createEvent);
                        }
                    }
                    else if (r != null)
                    {
                        theLogger?.LogInfo("Conflict Exists on Reservation <" + r.MyLink.MyReferenceNumber + ">");
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }

        public void DoStationNeckConflictCheck()
        {
            try
            {
                Path pathToLeft = null;
                Path pathToRight = null;
                if (MyStationNecks.Count <= 1)
                {
                    return;
                }
                foreach (var sn in MyStationNecks)
                {
                    if (sn.MyDirection == "R") pathToRight = sn;
                    if (sn.MyDirection == "L") pathToLeft = sn;
                }
                var doUpdate = false;
                var stn = MyRailwayNetworkManager.FindStation(pathToRight.MyStationName);
                if (stn == null) return;
                if (pathToRight != null && pathToLeft != null)
                {
                    foreach (var tl in TimedLocations)
                    {
                        if (tl.MyMovementPlan == null) continue;
                        foreach (var ra in tl.MyMovementPlan.MyRouteActions)
                        {
                            if (ra.RouteName == pathToLeft.MyRouteName )
                            {
                                foreach (var r in stn.MyReservations)
                                {
                                    if (r.MyLink?.MyReferenceNumber == pathToRight.MyConnectionLeft )
                                    {
                                        var conflictingReservation = stn.CheckStationNeckReservationConflict(r, pathToRight, pathToLeft, this);
                                        if (conflictingReservation != null)
                                        {
                                            var tripFound = FindTrip(r.MyTripCode, r.ScheduledPlanId);
                                            if (tripFound == null)continue;
                                            var conflict = Conflict.Conflict.CreateInstanceStationNeckConflict(stn, r, conflictingReservation, tripFound, pathToRight, pathToLeft);
                                            if (!tripFound.DoesStationNeckConflictExist(conflict))
                                            {
                                                tripFound.MyConflicts.Add(conflict);
                                                doUpdate = true;
                                                MyLogger?.LogInfo("DoStationNeckConflictCheck:Station Neck Conflict:" + conflict.MyDescription);
                                                MyTrainSchedulerManager?.ProduceMessage2003(tripFound);
                                            }
                                        }

                                    }
                                }
                            }
                        }
                    }
                }
                //if (doUpdate) MyTrainSchedulerManager?.ProduceMessage2003(this);

            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }

        private Trip FindTrip(string tripCode, string dayCode)
        {
            try
            {
                foreach (var trip in GlobalDeclarations.TripList)
                {
                    if (trip.TripCode == tripCode && trip.ScheduledPlanId.ToString() == dayCode)
                        return trip;
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }

            return null;
        }
        private bool DoReservationTimesOverlap(Reservation.Reservation toLeftReservation, Reservation.Reservation toRightReservation)
        {
            try
            {

            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }

            return false;
        }
        private void CreateEventForConflict(string theMessage, Conflict.Conflict theConflict, bool logEvent = true, bool createEvent = false)
        {
            try
            {
                if (createEvent) MyAutoRoutingManager?.CreateEvent(theMessage, "CONFLICT ADDED", TrainAutoRoutingManager.AlertLevel.WARN, true);
                if (logEvent) MyLogger?.LogInfo(theMessage);
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
    ;       }
        }
        private void DoConflictCheck(Station theStation, Reservation.Reservation theReservation, bool sendTripUpdateMessage = false, bool createEvent = false)
        {
            var conflictingReservation = theStation.CheckReservationConflict(theReservation);
            //if (conflictingReservation == null) conflictingReservation = CheckReservationConflictFromTrips(theReservation);

            if (conflictingReservation != null && !DoesConflictExist(conflictingReservation))
            {
                var conflict = Conflict.Conflict.CreateInstanceFromReservationConflict(theStation, theReservation, conflictingReservation, this);

                //need to create other conflict and send update
                var otherTrip = GlobalDeclarations.FindTrip(conflictingReservation.MyTripCode, conflictingReservation.MyTripStartTime);
                var conflictOther = Conflict.Conflict.CreateInstanceFromReservationConflict(theStation, conflictingReservation, theReservation, this, false);
                //if (otherTrip == null)
                //{
                //    MyConflicts.Add(conflict);
                //    return;
                //}
                if (otherTrip == null)
                {
                    theLogger?.LogException("Other Trip In DoConflictCheck Was Not Found");
                    return;
                }
                DateTime otherStartTime;
                DateTime thisStartTime;
                try
                {
                    otherStartTime = Convert.ToDateTime(otherTrip.StartTime);
                }
                catch (Exception e)
                {
                    otherStartTime = Convert.ToDateTime(FormatIntoProperTime(otherTrip.StartTime));
                    theLogger?.LogException(e.ToString());
                }

                try
                {
                    thisStartTime = Convert.ToDateTime(this.StartTime);
                }
                catch (Exception e)
                {
                    thisStartTime = Convert.ToDateTime(FormatIntoProperTime(this.StartTime));
                    theLogger?.LogException(e.ToString());
                }

                //var testTime = DateTime.Parse(otherTrip.StartTime.Replace("24", "2024"));
                //var otherStartTime = Convert.ToDateTime(otherTrip.StartTime);
                //var thisStartTime = Convert.ToDateTime(this.StartTime);

                //if (otherTrip.TrainPriority == this.TrainPriority)
                //{
                //    if (thisStartTime < otherStartTime)
                //    {
                //        if (!otherTrip.DoesConflictExist(conflictOther))
                //        {
                //            otherTrip.MyConflicts.Add(conflictOther);
                //            theLogger?.LogInfo("StartTime:Conflict Added for MockTrip <" + otherTrip.TripCode +"> at <" + conflictOther.MyReservation.MyEdgeName +">");
                //            if (!createEvent) return;
                //            var message = "Conflict Added for MockTrip <" + otherTrip.TripCode + "> at <" + conflictOther.MyReservation.MyEdgeName + ">";
                //            MyAutoRoutingManager?.CreateEvent(message, "CONFLICT ADDED", TrainAutoRoutingManager.AlertLevel.WARN, true);
                //            MyTrainSchedulerManager?.ProduceMessage2003(otherTrip);
                //        }
                //    }
                //    else
                //    {
                //        if (!DoesConflictExist(conflict))
                //        {
                //            //otherTrip.MyConflicts.Add(conflictOther);
                //            MyConflicts.Add(conflict);
                //            theLogger?.LogInfo("Other StatTime:Conflict Added for MockTrip <" + otherTrip.TripCode + "> at <" + conflictOther.MyReservation.MyEdgeName + ">");
                //            if (!createEvent) return;
                //            var message = "Conflict Added for MockTrip <" + this.TripCode + "> at <" + theReservation.MyEdgeName + ">";
                //            MyAutoRoutingManager?.CreateEvent(message, "CONFLICT ADDED", TrainAutoRoutingManager.AlertLevel.WARN, true);
                //            MyTrainSchedulerManager?.ProduceMessage2003(this);
                //        }
                //    }
                //    return;
                //}

                //if (this.TrainPriority < otherTrip.TrainPriority)
                //{
                //    if (!otherTrip.DoesConflictExist(conflictOther))
                //    {
                //        otherTrip.MyConflicts.Add(conflict);
                //        if (createEvent)
                //        {
                //            var message = "Conflict Added for MockTrip <" + otherTrip.TripCode + "> AT <" + conflict.MyReservation.MyEdgeName + "> <" + conflict.MyDescription;
                //            MyAutoRoutingManager?.CreateEvent(message, "CONFLICT ADDED", TrainAutoRoutingManager.AlertLevel.INFORMATION, true);
                //            MyTrainSchedulerManager?.ProduceMessage2003(otherTrip);
                //        }
                //    }
                //}
                //else
                //{
                    if (DoesConflictExist(conflict)) return;
                    MyConflicts.Add(conflict);
                    if (createEvent)
                    {
                        var message = "Conflict Added for Trip <" + this.TripCode + "> at <" + theReservation.MyEdgeName + "> " + conflict.MyDescription;
                        MyAutoRoutingManager?.CreateEvent(message, "CONFLICT ADDED", TrainAutoRoutingManager.AlertLevel.INFORMATION, true);
                        //MyTrainSchedulerManager?.ProduceMessage2003(this);
                    }
                //}

                if (sendTripUpdateMessage)
                {
                    //MyTrainSchedulerManager?.ProduceMessage2003(otherTrip);
                   // MyTrainSchedulerManager?.ProduceMessage2003(this);
                }
            }
            else if (conflictingReservation != null)
            {
                theLogger?.LogInfo("Conflict Exists on Reservation <" + conflictingReservation.MyLink.MyReferenceNumber +">0");
            }
        }
        private bool DoesConflictExist(Conflict.Conflict theConflict)
        {
            try
            {
                foreach (var c in MyConflicts)
                {
                    if (TripCode == "2001" && theConflict.IsStationNeckConflict)
                    {
                        Console.Beep();
                    }

                    if (c.MyDescription == theConflict.MyDescription && c.MyLocation == theConflict.MyLocation)
                    {
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                theLogger?.LogException(e.ToString());
            }
            return false;
        }
        public bool DoesStationNeckConflictExist(Conflict.Conflict theConflict)
        {
            try
            {
                foreach (var c in MyConflicts)
                {
                    if (c.IsStationNeckConflict)
                    {
                        if (c.MyDescription == theConflict.MyDescription && c.MyLocation == theConflict.MyLocation)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                theLogger?.LogException(e.ToString());
            }
            return false;
        }

        private bool DoesConflictExist(Reservation.Reservation theReservation)
        {
            try
            {
                foreach (var c in MyConflicts)
                {
                    if (c.ConflictingReservation.MyGuid == theReservation.MyGuid) return true;
                }
            }
            catch (Exception e)
            {
                theLogger?.LogException(e.ToString());
            }
            return false;
        }
        public bool DoesConflictExist(string theDeviceName, string theDeviceUid, int subTypeIndex)
        {
            try
            {
                foreach (var c in MyConflicts)
                {
                    if (c.IsInfrastructureConflict && c.MyEntity.MyName == theDeviceName && c.MyEntity.MyUid == theDeviceUid && c.MyEntity.MySubTypeIndex == subTypeIndex) return true;
                }
            }
            catch (Exception e)
            {
                theLogger?.LogException(e.ToString());
            }
            return false;
        }
        private Link GetNextLink(string direction, Path thePath)
        {
            if (direction == "R") return thePath.MyLinkRight;

            return thePath.MyLinkLeft;
        }
        private Link GetPreviousLink(string direction, Path thePath)
        {
            if (direction == "L") return thePath.MyLinkRight;

            return thePath.MyLinkLeft;
        }
        private Node? GetNode(Station theStation, Path thePath)
        {
            foreach (var node in theStation.MyNodes)
            {
                if (thePath.MyNodeId == node.MyReferenceNumber) return node;
            }
            return null;
        }
        private Station? GetStation(List<Station> Stations, string stationName)
        {
            foreach (var stn in Stations)
            {
                if (stn.MyReferenceNumber == Convert.ToInt32(stationName)) return stn;
            }
            return null;
        }
        private Station? GetStation(List<Station> Stations, Link theLink)
        {
            foreach (var stn in Stations)
            {
                foreach (var node in stn.MyNodes)
                {
                    foreach (var left in node.MyLeftLinks)
                    {
                        if (left.MyReferenceNumber == theLink.MyReferenceNumber) return stn;
                    }
                }
            }
            return null;
        }
        private Node? GetNode(List<Station> Stations, Link theLink)
        {
            foreach (var stn in Stations)
            {
                foreach (var node in stn.MyNodes)
                {
                    foreach (var left in node.MyLeftLinks)
                    {
                        if (left.MyReferenceNumber == theLink.MyReferenceNumber) return node;
                    }
                }
            }
            return null;
        }
        private Path GetPath(List<Station> Stations, string routeName)
        {
            foreach (var stn in Stations)
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
        private MovementPlan GetMovementPlan(List<MovementPlan> GlobalMovementPlans, string beginPlatform, string endPlatform)
        {
            foreach (var mp in GlobalMovementPlans)
            {
                if (mp.FromName == beginPlatform && mp.ToName == endPlatform) return mp;
            }

            return null;
        }
        public string GetTripInformation()
        {
            var t = this;
            var s = new StringBuilder();
            s.AppendLine("\nTrip (" + TripCode + ") (" + Number + ")");
            s.AppendLine("Name<" + t.Name + "> Direction<" + t.Direction + ">");
            s.AppendLine("\tTimed Locations");

            foreach (var tl in TimedLocations)
            {
                s.AppendLine("\tDescription<" + tl.Description + "> Arrival<" + tl.ArrivalTimeActual.ToString("MM/dd/yyyy HH:mm") + ">> Depart<" + tl.DepartureTimeActual.ToString("MM/dd/yyyy HH:mm"));
                s.AppendLine("\t\tMovement Plan");
                s.AppendLine("\t\tDescription<" + tl.MyMovementPlan?.Description + "> From Platform<" + tl.MyMovementPlan?.FromName + ">> To Platform<" + tl.MyMovementPlan?.ToName);
                s.AppendLine("\t\t\tRoute Action(s)");
                if (tl.MyMovementPlan?.MyRouteActions != null)
                    foreach (var ra in tl.MyMovementPlan?.MyRouteActions)
                    {
                        s.AppendLine("\t\t\tRoute Name<" + ra.RouteName + "> Location<" + ra.ActionLocation +
                                     "> Type<" + ra.ActionType);
                    }
            }
            return s.ToString();
        }
        public void CreateLengthConflict(Platform thePlatform, TimedLocation theTimeLocation)
        {
            try
            {
                //var currentTrainLength = this.MyTrainPosition.Train.DefaultLength;
                var kilometerValue = MyRailGraphManager?.GetElementKilometerValueOfTrack(thePlatform.MyHierarchyTrack.SysID);

                var conflict = Conflict.Conflict.CreateInstanceFromLength(this,thePlatform, theTimeLocation, kilometerValue.ToString());
                MyConflicts.Add(conflict);
                MyTrainSchedulerManager?.ProduceMessage2003(this);
                theLogger?.LogInfo("CreateLengthConflict: Trip Uid <" + TripCode + "> Platform Name <" + thePlatform.MyName + "> Platform Length <" + thePlatform.MyDistanceInMeters + "> Train Length <" + Length + ">");

            }
            catch (Exception e)
            {
                theLogger?.LogException(e.ToString());
            }
        }
        public void DeleteLengthConflict(Platform thePlatform)
        {
            try
            {
                foreach (var conflict in MyConflicts)
                {
                    if (!conflict.IsLengthConflict || conflict.MyDeviceName != thePlatform.MyName) continue;
                    MyConflicts.Remove(conflict);
                    MyTrainSchedulerManager?.ProduceMessage2003(this);
                    break;
                }
            }
            catch (Exception e)
            {
                theLogger?.LogException(e.ToString());
            }
        }
        public bool CheckIfLengthConflictExists(Platform thePlatform, int lengthToCheck)
        {
            try
            {
                foreach (var conflict in MyConflicts)
                {
                    if (conflict.IsLengthConflict && conflict.MyDeviceName == thePlatform.MyName)
                    {
                        if (conflict.MyConflictLength != lengthToCheck)
                        {
                            conflict.MyConflictLength = lengthToCheck;
                            if (thePlatform.MyDistanceInMeters < lengthToCheck)
                            {
                                //Update Conflict and send trip update
                                conflict.MyDescription = "Train Length <" + this.Length + "> Conflicting With Platform <" + thePlatform.MyName + "> <" + thePlatform.MyDistanceInMeters + ">";
                                conflict.MyEntity.MyDescription = "Train Length <" + this.Length + "> Conflicting With Platform <" + thePlatform.MyName + "> <" + thePlatform.MyDistanceInMeters + ">";
                                MyTrainSchedulerManager?.ProduceMessage2003(this);
                                return true;
                            }
                            //if (thePlatform.MyDistanceInMeters >= lengthToCheck)
                            //{
                            //    //remove conflict and send trip update
                            //    MyConflicts.Remove(conflict);
                            //    break;
                            //}
                        }
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                theLogger?.LogException(e.ToString());
            }

            return false;
        }
        public void SetTrainType(int theType)
        {
            try
            {
                TrainTypeString = Enum.GetName(typeof(TrainSubType), theType);
                SubType = (TrainSubType)theType;
                TrainPriority = theType;
            }
            catch (Exception e)
            {
                theLogger?.LogException(e.ToString());
            }
        }
        private string FormatIntoProperTime(string? dateTimeString)
        {
            var outPutDate = DateTime.Now;
            try
            {
                DateTime.TryParseExact(dateTimeString, "dd/MM/yy HH:mm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out outPutDate);
            }
            catch (Exception e)
            {
                theLogger?.LogException(e.ToString());
            }
            return outPutDate.ToString("MM/dd/yyyy HH:mm");
        }
        #endregion
    }
}

