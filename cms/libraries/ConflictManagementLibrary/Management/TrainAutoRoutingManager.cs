using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Xml.Linq;
using ConflictManagementLibrary.Helpers;
using ConflictManagementLibrary.Logging;
using ConflictManagementLibrary.Model.Conflict;
using ConflictManagementLibrary.Model.Movement;
using ConflictManagementLibrary.Model.Possession;
using ConflictManagementLibrary.Model.Reservation;
using ConflictManagementLibrary.Model.Schedule;
using ConflictManagementLibrary.Model.Trip;
using Newtonsoft.Json;
using RailgraphLib;
using RailgraphLib.HierarchyObjects;
using RailgraphLib.Interlocking;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using Platform = ConflictManagementLibrary.Model.Schedule.Platform;
using Point = RailgraphLib.Interlocking.Point;
using Track = ConflictManagementLibrary.Network.Track;
namespace ConflictManagementLibrary.Management
{
    public class TrainAutoRoutingManager
    {
        #region Delegates
        public void LinkDelegate(RouteAvailableDelegate? isRouteAvailable, RouteExecuteDelegate doExecuteRoute, RouteExecutedDelegate isRouteExecuted, SendCompleteRoutePlan isSendCompleteRoutePlan, SendRouteMarkerDelegate doExecuteRouteMarkerUpdate, SendUpdatedForecastDelegate doExecuteForecastUpdate)
        {
            this.IsRouteAvailable = isRouteAvailable ?? throw new ArgumentNullException(nameof(isRouteAvailable));
            this.DoExecuteRoute = doExecuteRoute ?? throw new ArgumentNullException(nameof(doExecuteRoute));
            this.IsRouteExecuted = isRouteExecuted ?? throw new ArgumentNullException(nameof(isRouteExecuted));
            this.DoSendCompleteRoutePlan = isSendCompleteRoutePlan ?? throw new ArgumentNullException(nameof(isSendCompleteRoutePlan));
            this.DoSendRouteMarkerUpdate = doExecuteRouteMarkerUpdate ?? throw new ArgumentNullException(nameof(doExecuteRouteMarkerUpdate));
            this.DoSendForecastUpdate = doExecuteForecastUpdate ?? throw new ArgumentNullException(nameof(doExecuteForecastUpdate));

        }

        #region Is Route Available Delegate
        public delegate int RouteAvailableDelegate(string originalRouteName, string replaceRouteName, int planUid, int tripUid, string trainObid, RoutePlanInfo theRoutePlan);
        public RouteAvailableDelegate? IsRouteAvailable;
        #endregion
        #region Route Execute Delegate
        public delegate bool RouteExecuteDelegate(RoutePlanInfo theRoute);
        public RouteExecuteDelegate? DoExecuteRoute;
        #endregion

        #region Route Execute Delegate
        public delegate bool SendCompleteRoutePlan(RoutePlanInfo theRoute);
        public SendCompleteRoutePlan? DoSendCompleteRoutePlan;
        #endregion

        #region Is Route Executed Delegate
        public delegate bool RouteExecutedDelegate(string routeName, int planUid);
        public RouteExecutedDelegate? IsRouteExecuted;
        #endregion

        #region Send Route Marker
        public delegate bool SendRouteMarkerDelegate(string trainObid, List<Tuple<DateTime, string?>> markings);
        public SendRouteMarkerDelegate? DoSendRouteMarkerUpdate;
        #endregion

        #region  Send Forecast
        public delegate bool SendUpdatedForecastDelegate(Forecast theForecast);
        public SendUpdatedForecastDelegate? DoSendForecastUpdate;
        #endregion

        #endregion

        #region Declarations
        public IMyLogger MyLogger { get; }
        public List<Trip> MyTripsAllocated = new List<Trip>();
        public ConcurrentBag<PreTestResult> MyPreTestResults = new ConcurrentBag<PreTestResult>();
        public static List<TrainTypeDetails> MyTrainTypes = new List<TrainTypeDetails>();

        #endregion

        #region Constructor
        private TrainAutoRoutingManager(IMyLogger theLogger)
        {
            MyLogger = theLogger;

            if (MyEnableAutomaticRoutingSettingFlag)
            {
                var doStartAllocatedTrainCheck = new Thread(DoAllocatedTrainPositionCheck);
                doStartAllocatedTrainCheck.Start();
            }

            var doStartAllocatedTrainConflictCheck = new Thread(DoCheckForInfrastructureConflictsForAllocatedTrains);
            doStartAllocatedTrainConflictCheck.Start();

            var doStartAllocatedTrainConflictRemovalCheck = new Thread(DoCheckForInfrastructureRemovalOfConflictsForAllocatedTrains);
            doStartAllocatedTrainConflictRemovalCheck.Start();
            
            if (MyEnableAutomaticRoutingSettingFlag)
            {
                var doForceRouteExecutionOnAllocatedTrains = new Thread(DoForceRouteExecutionOnAllocatedTrains);
                doForceRouteExecutionOnAllocatedTrains.Start();

                var doStartTriggerPointCheck = new Thread(PerformTriggerPointStatusUpdates);
                doStartTriggerPointCheck.Start();

                var doCheckForRouteMarkingUpdatesForAllocatedTrains = new Thread(DoCheckForRouteMarkingUpdatesForAllocatedTrains);
                doCheckForRouteMarkingUpdatesForAllocatedTrains.Start();
            }
            var doTripDwellUpdateToForecast = new Thread(PerformTripDwellUpdateToForecast);
            doTripDwellUpdateToForecast.Start();

            var doProjectedTimeToNextTimedLocation = new Thread(PerformProjectedTimeToNextTimedLocation);
            doProjectedTimeToNextTimedLocation.Start();

            var doMonitorRouteClearanceForAllocatedTrains = new Thread(PerformMonitorRouteClearanceForAllocatedTrains);
            doMonitorRouteClearanceForAllocatedTrains.Start();

        }
        public static TrainAutoRoutingManager CreateInstance(IMyLogger theLogger)
        {
            return new TrainAutoRoutingManager(theLogger);
        }
        #endregion

        #region Datahandler

        public void AddTrainTypes(string theTypes)
        {
            try
            {
                MyTrainTypes = DeserializeMyObject<List<TrainTypeDetails>>(MyLogger, theTypes);
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
        }

        #endregion

        #region Methods

        #region Train Allocation
        public void PerformProjectedTimeToNextTimedLocation()
        {
            try
            {
                Thread.Sleep(45000);
                while (true)
                {
                    try
                    {
                        List<Trip> tempTripList;
                        lock (MyTripsAllocated)
                        {
                            //Create temp list of allocated trains
                            tempTripList = new List<Trip>(MyTripsAllocated);
                        }
                        
                        foreach (var trip in tempTripList)
                        {
                            TimedLocation theLastDepartedTimedLocation = null;
                            foreach (var loc in trip.TimedLocations)
                            {
                                if (loc.HasArrivedToPlatform && loc.HasDepartedFromPlatform)
                                {
                                    theLastDepartedTimedLocation = loc;
                                }
                            }
                            if (trip.MyTrainPosition == null) continue;
                            if (theLastDepartedTimedLocation == null) continue;
                            var tl = theLastDepartedTimedLocation;
                            if (tl.HasArrivedToPlatform && tl.HasDepartedFromPlatform)
                            {
                                var headEndPosition = trip.MyTrainPosition.ElementExtension.StartPos.AdditionalPos;
                                var platform = MyRailwayNetworkManager.FindPlatform(tl.MyNextTimedLocation.Description);
                                if (platform == null) continue;
                                var nextPlatformCenterPosition = Convert.ToInt32(platform.MyElementPosition.AdditionalPos);

                                if (headEndPosition < nextPlatformCenterPosition)
                                {
                                    var distanceToNextPlatform = (nextPlatformCenterPosition - headEndPosition) / 1000;

                                    var secondsToPlatform = distanceToNextPlatform / trip.MyAverageMetersPerSecond;
                                    var projectDateTime = DateTime.Now.AddSeconds(secondsToPlatform);
                                    tl.MyNextTimedLocation.ArrivalTimeAdjusted = projectDateTime;
                                    tl.MyNextTimedLocation.DepartureTimeAdjusted = projectDateTime.AddSeconds(30);

                                    MyLogger?.LogInfo("Projected Time to NextPlatform <" + tl.MyNextTimedLocation.Description + "> Trip <" + trip.TripCode + ">");
                                }
                                else
                                {
                                    var distanceToNextPlatform = (headEndPosition - nextPlatformCenterPosition) / 1000;
                                    var secondsToPlatform = distanceToNextPlatform / trip.MyAverageMetersPerSecond;
                                    var projectDateTime = DateTime.Now.AddSeconds(secondsToPlatform);
                                    tl.MyNextTimedLocation.ArrivalTimeAdjusted = projectDateTime;
                                    tl.MyNextTimedLocation.DepartureTimeAdjusted = projectDateTime.AddSeconds(30);

                                    MyLogger?.LogInfo("Projected Time to NextPlatform <" + tl.MyNextTimedLocation.Description + "> Trip <" + trip.TripCode + ">");
                                }
                                var deltaTime = tl.MyNextTimedLocation.ArrivalTimeAdjusted - tl.MyNextTimedLocation.ArrivalTimePlan;

                                DoUpdateForecastFromProjection(trip, tl.MyNextTimedLocation, deltaTime);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        MyLogger?.LogException(e);
                    }
                    Thread.Sleep(1000 * 60);
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
        }
        private void PerformTripDwellUpdateToForecast()
        {
            try
            {
                Thread.Sleep(30000);

                while (true)
                {
                    try
                    {
                        List<Trip> tempTripList;
                        lock (MyTripsAllocated)
                        {
                            //Create temp list of allocated trains
                            tempTripList = new List<Trip>(MyTripsAllocated);
                        }

                        foreach (var trip in tempTripList)
                        {
                            if (trip.MyTrainPosition == null) continue;
                            TimedLocation theLastArrivalLocation = null;
                            foreach (var tl in trip.TimedLocations)
                            {
                                if (tl.HasArrivedToPlatform && !tl.HasDepartedFromPlatform) theLastArrivalLocation = tl;
                            }
                            if (theLastArrivalLocation != null) BuildNewForecastToPublishFromDwellTimeAtPlatform(trip);
                        }
                    }
                    catch (Exception e)
                    {
                        MyLogger?.LogException(e);
                    }
                    Thread.Sleep(1000 * 60);
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
        }
        private void PerformTriggerPointStatusUpdates()
        {
            try
            {
                Thread.Sleep(20000);
                while (true)
                {
                    try
                    {
                        List<Trip> tempTripList;
                        lock (MyTripsAllocated)
                        {
                            //Create temp list of allocated trains
                            tempTripList = new List<Trip>(MyTripsAllocated);
                        }

                        foreach (var tp in tempTripList.SelectMany(trip => trip.MyTriggerPoints))
                        {
                            tp.UpdateOccupancyStatus();
                        }
                    }
                    catch (Exception e)
                    {
                        MyLogger?.LogException(e);
                    }
                    Thread.Sleep(5000);
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
        }
        private void DoAllocatedTrainPositionCheck()
        {
            try
            {
                Thread.Sleep(20000);
                while (true)
                {
                    try
                    {
                        lock (MyTripsAllocated)
                        {
                            foreach (var trip in MyTripsAllocated)
                            {
                                var theTrainPosition = trip.MyTrainPosition;
                                if (theTrainPosition == null) continue;
                                CheckPositionChange(trip, theTrainPosition!, true);
                            }

                        }
                    }
                    catch (Exception e)
                    {
                        MyLogger?.LogException(e);
                    }
                    Thread.Sleep(10000);
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
        }
        public void CheckForTrainAllocationFromPosition(string theTrainPosition)
        {
            try
            {
                var trainPosition = DeserializeMyObject<TrainPosition>(MyLogger, theTrainPosition);
                var thread = new Thread(() => DoCheckForTrainAllocation("nothing",0,trainPosition,string.Empty, 0)); thread.Start();
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
        }
        public void CheckForTrainAllocation(string theTrainTd, int tripUid, string trainType, int tripDayCode)
        {
            try
            {
                var thread = new Thread(() => DoCheckForTrainAllocation(theTrainTd, tripUid, null!,trainType, tripDayCode,true)); thread.Start();
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
        }
        private void DoCheckForTrainAllocation(string theTrainTd, int tripUid, TrainPosition? theTrainPosition, string trainType, int tripDayCode, bool isAllocated = false)
        {
            try
            {
                var trainTd = theTrainTd;
                Trip tripToUpdate = null!;
                var trainInformationChanged = false;
                if (theTrainPosition != null)
                {
                    trainTd = theTrainPosition.Train.Td;
                    if (string.IsNullOrEmpty(trainType)) theTrainPosition.Train.TrainType = trainType;
                }
                MyLogger.LogInfo("DoCheckForTrainAllocation:Train UID <" + theTrainTd + ">");
                lock (TripList)
                {
                    foreach (var trip in TripList.Where(trip => trip.TripCode == trainTd))
                    {
                        if (theTrainPosition != null)
                        {
                            //trainInformationChanged = DidTrainInformationChange(theTrainPosition, trip);
                            trip.MyTrainPosition = theTrainPosition;
                            AddPositionToTrip(trip, theTrainPosition);
                            tripToUpdate = trip;
                            if (theTrainPosition.Train != null) trip.MyTrainSystemUid = theTrainPosition.Train.Sysid.ToString();
                            CheckForArrivalDepartureEvent(trip, theTrainPosition);
                            MyLogger.LogInfo("DoCheckForTrainAllocation1:Train UID <" + theTrainTd + ">");
                           // trip.ProjectTimeToNextTimedLocation();
                        }
                        else if (theTrainPosition == null)
                        {
                            theTrainPosition = trip.MyTrainPosition;
                            //AddPositionToTrip(trip, theTrainPosition);
                            //DoCheckIfTrainPlacedOnTriggerPointElseWhereInPlan(trip);
                            CheckForArrivalDepartureEvent(trip, theTrainPosition);
                            MyLogger.LogInfo("DoCheckForTrainAllocation5Train UID <" + theTrainTd + ">");
                        }

                        if (!trip.IsAllocated && isAllocated)
                        {
                            //Do check of trip day code to be sure the trip found is correct
                            //if (trip.TripId != tripUid) 
                            if (trip.ScheduledPlanDayCode != tripDayCode)
                            {
                                MyLogger.LogInfo("DoCheckForTrainAllocation:The trip is not the correct Trip Uid for Allocation <" + theTrainTd + "><" + trip.ScheduledPlanDayCode + ">");
                                continue;
                            }
                            TripAdd(trip, theTrainPosition!);
                            trip.IsAllocated = true;
                            ApplyTrainIdentifier(theTrainPosition, trip);
                            MyTrainSchedulerManager?.ProduceMessage2004(trip);
                            CheckPositionChange(trip, theTrainPosition);
                            var message = "Trip Has Been ALLOCATED for Automatic Routing Trip <" + trip.TripCode + "> CTC UID <" + trip.CtcUid + "> Day Code <" + trip.ScheduledPlanDayCode + "> System UID <" + trip.MyTrainSystemUid + ">";
                            CreateEvent(message, "TRIP AS BEEN ALLOCATED", AlertLevel.WARN, true);
                            CheckForArrivalDepartureEvent(trip, theTrainPosition);
                            //BuildNewForecastToPublishFromTrain(trip);
                            CheckToExecuteRouteToNextPlatform(trip, trip.MyTrainPosition);
                            MyLogger.LogDebug("DoCheckForTrainAllocation:Trip Has Been Allocated <" + theTrainTd + ">");
                        }
                        else if (isAllocated)
                        {
                            CheckPositionChange(trip, theTrainPosition!);
                            MyLogger.LogDebug("DoCheckForTrainAllocation3:Train UID <" + theTrainTd + ">");
                        }
                    }
                }

                //if (trainInformationChanged)
                //{
                //    lock (TripList)
                //    {
                //        MyTrainSchedulerManager?.ProduceMessage2003(tripToUpdate!);
                //        MyLogger.LogInfo("DoCheckForTrainAllocation4:Train UID <" + theTrainTd + ">");
                //        DoTrainLengthCheck(tripToUpdate);
                //    }
                //}
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
        }
        public void PerformDeallocateTrain(string theTrainTd)
        {
            try
            {
                var thread = new Thread(() => DoDeallocateTrain(theTrainTd)); thread.Start();
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
        }
        public void UpdateTrainLength(int length, string theTripCode, string theDayCode)
        {
            try
            {
                var thread = new Thread(() => DoUpdateTrainLength(length, theTripCode, theDayCode));
                thread.Start();

            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void DoUpdateTrainLength(int length, string theTripCode, string theDayCode)
        {
            try
            {
                var trip = FindTripFromScheduleDayCode(theTripCode, theDayCode);
                if (trip != null)
                {
                    var trainLengthInMeters = length / 1000;
                    var trainInformationChanged = DidTrainInformationChange(trip, length);
                    if (trainInformationChanged)
                    {
                        //trip.MyTrainPosition.Train.DefaultLength = length;
                        foreach (var tl in trip.TimedLocations)
                        {
                            if (trainLengthInMeters > tl.MyPlatform.MyDistanceInMeters)
                            {
                                if (!trip.CheckIfLengthConflictExists(tl.MyPlatform, trainLengthInMeters))
                                {
                                    trip.CreateLengthConflict(tl.MyPlatform, tl);
                                    MyLogger.LogInfo("DoTrainLengthCheck-CreateLengthConflict: Trip Uid <" + trip.TripCode + "> Platform Name <" + tl.MyPlatform.MyName + "> Platform Length <" + tl.MyPlatform.MyDistanceInMeters + "> Train Length <" + length + ">");
                                }
                            }
                            else if (trainLengthInMeters < tl.MyPlatform.MyDistanceInMeters)
                            {
                                if (trip.CheckIfLengthConflictExists(tl.MyPlatform, trainLengthInMeters))
                                {
                                    trip.DeleteLengthConflict(tl.MyPlatform);
                                    MyLogger.LogInfo("DoTrainLengthCheck-DeleteLengthConflict: Trip Uid <" + trip.TripCode + "> Platform Name <" + tl.MyPlatform.MyName + "> Platform Length <" + tl.MyPlatform.MyDistanceInMeters + "> Train Length <" + length + ">");
                                }
                            }
                        }
                        MyLogger.LogInfo("DoUpdateTrainLength: Trip Found <" + theTripCode + "><" + theDayCode + ">");
                    }
                }
                else
                {
                    MyLogger.LogInfo("DoUpdateTrainLength: Trip Not Found <" + theTripCode + "><" + theDayCode + ">");
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void DoDeallocateTrain(string sysUid)
        {
            try
            {
                lock (MyTripsAllocated)
                {
                    foreach (var trip in MyTripsAllocated.Where(trip => trip.MyTrainSystemUid == sysUid))
                    {
                        MyTripsAllocated.Remove(trip);
                        //trip.IsAllocated = false;
                        //trip.MyTrainPosition = null;
                        trip.CleanUp();
                        RemoveInfrastructureConflicts(trip);
                        MyTrainSchedulerManager?.ProduceMessage2004(trip);
                        var message = "Trip Has Been DE-ALLOCATED for Automatic Routing<" + trip.TripCode + ">";
                        CreateEvent(message, "TRIP AS BEEN DE-ALLOCATED", AlertLevel.WARN, true);
                        BuildNewForecastToPublishFromUnAllocate(trip);
                        DoRouteMarkingClear(trip);
                        //ClearTripFromDeAllocation(trip);
                        TripList.Remove(trip);
                        return;
                    }
                }
                //MyLogger.LogInfo("DoDeallocateTrain:Train Was Not De-Allocated <" + sysUid +">");
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
        }
        private void ClearTripFromDeAllocation(Trip theTrip)
        {
            try
            {
                foreach (var tl in theTrip.TimedLocations)
                {
                    tl.RouteIsExecutedForTrip = false;
                    tl.IsPastTriggerPoint = false;
                    tl.HasArrivedToPlatform = false;
                    tl.HasDepartedFromPlatform = false;
                    tl.HasSentRouteMarker = false;
                    tl.RouteIsAlreadyAvailableForTrip = false;
                    tl.UseAlternatePlatform = false;
                    tl.RemoveActionPointsFromRoutePlan = false;
                }

                theTrip.LastTimedLocation = null;
                theTrip.LocationCurrent = null;
                theTrip.MyRouteMarkingCurrent = new List<RouteMarkInfo>();
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
        }
        private void RemoveInfrastructureConflicts(Trip theTrip)
        {
            try
            {
                var theConflict = new List<ConflictObject>(theTrip.MyConflictObjects);
                foreach (var conflict in theConflict)
                {
                    RemoveConflict(theTrip,conflict);
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
        }
        private void ApplyTrainIdentifier(TrainPosition thePosition, Trip theTrip)
        {
            try
            {
                if (thePosition?.Train != null)
                {
                    theTrip.MyTrainObid = thePosition.Train.Obid;
                    theTrip.MyTrainSystemUid = thePosition.Train.Sysid.ToString();
                    theTrip.MyTrainObid = thePosition.Obid;
                    theTrip.CtcUid = thePosition.Train.CtcId;
                    MyLogger?.LogInfo("ApplyTrainIdentifier:Identifiers Add For Trip <" + theTrip.TripCode +">");
                }
                else
                {
                    MyLogger?.LogInfo("ApplyTrainIdentifier:Identifiers NOT Add For Trip <" + theTrip.TripCode + ">");
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
        }
        private bool DoTrainAllocationCheckForProperDate(Trip theTrip, string theTrainUid)
        {
            try
            {
                var tripCandidateList = new List<Trip>();
                foreach (var trip in TripList.Where(trip => trip.TripCode == theTrainUid))
                {
                    tripCandidateList.Add(trip);
                }
                //Only one trip candidate regardless of date time, return true
                if (tripCandidateList.Count == 1) return true;

                var theTripCandidateStartTime = DateTime.Now;
                if (tripCandidateList.Any(trip => theTripCandidateStartTime.Date < trip.PlanStartTime.Date)) return true;
                if (tripCandidateList.Any(trip => theTripCandidateStartTime.Date == trip.PlanStartTime.Date)) return true;
                if (tripCandidateList.Any(trip => theTripCandidateStartTime.Date.AddDays(1) == trip.PlanStartTime.Date)) return true;
                if (tripCandidateList.Any(trip => theTripCandidateStartTime.Date.AddDays(1) >= trip.PlanStartTime.Date)) return true;

            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }

            return false;
        }
        private bool CheckIfTrainLengthChange(Trip theTrip)
        {
            try
            {
               //return true;
                if (theTrip.MyTrainPosition != null)
                {
                    if (theTrip.MyTrainPosition.Train.DefaultLength != theTrip.MyTrainPosition.Train.PreviousLength)
                    {
                        theTrip.MyTrainPosition.Train.PreviousLength = theTrip.MyTrainPosition.Train.DefaultLength;
                        MyLogger.LogInfo("CheckIfTrainLengthChange Length Changed for Trip <" + theTrip.TripCode + "> <" + theTrip.MyTrainPosition.Train.PreviousLength + ">");

                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }

            return false;
        }
        private bool DidTrainInformationChange(TrainPosition? theTrainPosition, Trip? theTrip)
        {
            try
            {
                if (theTrip?.MyTrainPosition == null) return false;
                if (theTrainPosition != null && theTrainPosition?.Train != null)
                {
                    if (theTrip.MyTrainPosition.Train.TrainType != theTrainPosition.Train.TrainType)
                    {
                        return true;
                    }

                    if (theTrip.MyTrainPosition.Train.DefaultLength != theTrainPosition.Train.DefaultLength)
                    {
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }

            return false;
        }
        private bool DidTrainInformationChange(Trip? theTrip, int newLength)
        {
            try
            {
                //if (theTrip?.MyTrainPosition == null) return false;
                //if (theTrip.MyTrainPosition.Train.DefaultLength != newLength)
                //{
                //    return true;
                //}
                if (theTrip.Length != newLength)
                {
                    theTrip.Length = newLength / 1000;
                    return true;
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
            return false;
        }
        public void TripAdd(Trip theTrip, TrainPosition thePosition)
        {
            try
            {
                lock (MyTripsAllocated)
                {
                    MyTripsAllocated.Add(theTrip);
                }
                CheckPositionChange(theTrip, thePosition);
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        public void TripDelete(Trip theTrip, TrainPosition? thePosition)
        {
            try
            {
                var tripName = theTrip.TripCode;
                lock (MyTripsAllocated)
                {
                    MyTripsAllocated.Remove(theTrip);
                }

                lock (TripList)
                {
                    TripList.Remove(theTrip);
                }
                lock (TripList)
                {
                    MyTrainSchedulerManager?.ProduceMessage2002(theTrip);
                    MyLogger?.LogInfo("TripDelete: Trip Deleted <" + tripName + ">");
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }
        public void OperatorDeletedTrip(string? tripUid, string? startTime)
        {
            try
            {
                var tripToDelete = FindTrip(tripUid, startTime);
                if (tripToDelete == null) return;
                CheckToUpdateConflicts(tripToDelete.MyReservations);
                TripDelete(tripToDelete,null);
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private Trip FindTrip(string? tripUid, string? startTime)
        {
            try
            {
                foreach (var trip in TripList)
                {
                    if (trip.StartTime == startTime && trip.TripId == Convert.ToInt32(tripUid)) return trip;
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }

            return null!;
        }
        private Trip FindTrip(string tripCode, string tripUid, bool doFind = true)
        {
            try
            {
                foreach (var trip in TripList)
                {
                    if (trip.TripCode == tripCode && trip.TripId == Convert.ToInt32(tripUid)) return trip;
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
            return null!;
        }
        private Trip FindTripFromScheduleDayCode(string tripCode, string scheduleDayCode)
        {
            try
            {
                foreach (var trip in TripList)
                {
                    if (trip.TripCode == tripCode && trip.ScheduledPlanDayCode.ToString() == scheduleDayCode) return trip;
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
            return null!;
        }
        private Trip FindTripFromTrainSystemId(string trainSysId)
        {
            try
            {
                foreach (var trip in TripList)
                {
                    if (trip.MyTrainSystemUid == trainSysId) return trip;
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
            return null!;
        }
        private Trip FindTripFromTrainObid(string trainObid)
        {
            try
            {
                foreach (var trip in TripList)
                {
                    if (trip.MyTrainObid == trainObid) return trip;
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
            return null!;
        }
        private bool CheckTrainOnTriggerPoint(Trip theTrip, TrainPosition? thePosition, string triggerPointName)
        {
            try
            {
                if (thePosition?.MyCurrentTrackUidList != null)
                    foreach (var uid in thePosition?.MyCurrentTrackUidList)
                    { 
                        var theTrack = MyRailGraphManager?.ILTopoGraph?.getGraphObj(uid);
                        if (theTrack != null)
                        {
                            var track = MyRailGraphManager?.ILTopoGraph?.getGraphObj(theTrack.getId());
                            if (track == null) continue;
                            var occ = ((RailgraphLib.Interlocking.Track)track).getOccupationState();
                            var trackName = track.getName();
                            if (occ == EOccupation.occupationOn && trackName == triggerPointName) return true;
                        }
                    }

                var ilElement = MyRailGraphManager?.ILGraph?.getGraphObj(triggerPointName);
                if (ilElement != null)
                {
                    switch (ilElement)
                    {
                        case PointLeg leg:
                        {
                            var occ = ((RailgraphLib.Interlocking.PointLeg)leg).getOccupationState();
                            if (occ == EOccupation.occupationOn) return true;
                            var tp = GetTriggerPoint(theTrip, triggerPointName);
                                return tp.IsOccupied();
                        }
                        case TrackSection section:
                        {
                            var occ = ((RailgraphLib.Interlocking.TrackSection)section).getOccupationState();
                            if (occ == EOccupation.occupationOn) return true;
                            var tp = GetTriggerPoint(theTrip, triggerPointName);
                            return tp.IsOccupied();
                        }
                        case RailgraphLib.Interlocking.Track track:
                        {
                            var occ = ((RailgraphLib.Interlocking.Track)track).getOccupationState();
                            if (occ == EOccupation.occupationOn) return true;
                            var tp = GetTriggerPoint(theTrip, triggerPointName);
                            return tp.IsOccupied();
                        }
                        case RailgraphLib.Interlocking.Point:
                        {
                            break;
                        }
                    }
                }

                //var thetrack = MyRailGraphManager?.GetTrack(triggerPointName);
                //if (thetrack != null)
                //{
                //    var occ = ((RailgraphLib.Interlocking.Track)thetrack).getOccupationState();
                //    if (occ == EOccupation.occupationOn) return true;
                //}

                //var pointLeg = MyRailGraphManager?.ILGraph?.getGraphObj(triggerPointName);
                ////if (pointLeg != null && pointLeg is RailgraphLib.Interlocking.PointLeg) 
                //if (pointLeg != null)
                //{
                //    //var occ = ((pointLeg as RailgraphLib.Interlocking.PointLeg)!).getOccupationState();
                //    var occ = ((RailgraphLib.Interlocking.PointLeg)pointLeg).getOccupationState();
                //    if (occ == EOccupation.occupationOn) return true;
                //}
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
            return false;
        }
        private bool CheckTrainOnTriggerPoint(Trip theTrip, string triggerPointName, TimedLocation theLocation, GraphObj ilElement)
        {
            try
            {
                //var ilElement = MyRailGraphManager?.ILGraph?.getGraphObj(triggerPointName);
                if (ilElement != null && theLocation.MyMovementPlan != null)
                    foreach (var ra in theLocation.MyMovementPlan.MyRouteActions)
                    {
                        if (ra.ActionLocation == ilElement.getName())
                        {
                            ra.IsOnTriggerPoint = true;
                            return true;
                        }
                    }

            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
            return false;
        }
        private TriggerPoint GetTriggerPoint(Trip theTrip, string triggerPoint)
        {
            try
            {
                foreach (var tp in theTrip.MyTriggerPoints)
                {
                    if (tp.TheTriggerName == triggerPoint) return tp;
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }

            return null;
        }
        private void DoCheckIfTrainPlacedOnTriggerPointElseWhereInPlan(Trip theTrip)
        {
            try
            {
                var el = MyRailGraphManager.GetCoreObjByName(theTrip.MyTrainPosition.ElementExtension.StartPos.ElementId);
                if (theTrip.MyTrainPosition == null) return;
                
                TimedLocation? lastTimedLocation = null;
                foreach (var tp in theTrip.MyTriggerPoints)
                {
                    if (CheckTrainOnTriggerPoint(theTrip, theTrip.MyTrainPosition, tp.TheTriggerName))
                    {
                        lastTimedLocation = FindTriggerPointLocation(tp.TheTriggerName, theTrip);
                    }
                }

                foreach (var tl in theTrip.TimedLocations)
                {
                    if (tl.MyPlatform.MyElementPosition.ElementId == theTrip.MyTrainPosition.ElementExtension.StartPos.ElementId)
                    {
                        lastTimedLocation = tl; 
                        break;
                    }
                }

                if (lastTimedLocation == null) return;
                TimedLocation firstLocation = theTrip.TimedLocations[0];
                //if (firstLocation.Description == lastTimedLocation.Description) return;
                foreach (var tl in theTrip.TimedLocations)
                {
                    if (tl.Description != lastTimedLocation.Description)
                    {
                        tl.HasArrivedToPlatform = true;
                        tl.HasDepartedFromPlatform = true;
                        tl.IsPastTriggerPoint = true;
                        tl.RouteIsExecutedForTrip =true;
                    }
                    else if (tl.Description == lastTimedLocation.Description)
                    {
                        tl.RemoveActionPointsFromRoutePlan = true;
                        tl.IsPastTriggerPoint = true;

                    }
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private TimedLocation? FindTriggerPointLocation(string triggerPoint, Trip theTrip)
        {
            try
            {
                foreach (var tl in theTrip.TimedLocations)
                {
                    if (tl.MyMovementPlan == null) continue;
                    foreach (var ra in tl.MyMovementPlan.MyRouteActions)
                    {
                        if (ra.ActionLocation == triggerPoint) return tl;
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }

            return null;
        }
        #endregion

        #region Automatic Infrastructure Conflict Checking
        public void RegenerateAllConflictsForTrips()
        {
            //do this function if enabled from TSUI

            try
            {
                lock (MyTripsAllocated)
                {
                    foreach (var trip in MyTripsAllocated)
                    {
                        trip.MyConflicts = new List<Conflict>();
                        trip.UpdateConflicts();
                        var message = "Conflicts Regenerated for Trip <" + trip.TripCode + "> Account Automatic Conflict Detection Enabled";
                        MyAutoRoutingManager?.CreateEvent(message, "CONFLICT REGENERATED", TrainAutoRoutingManager.AlertLevel.WARN, true);
                    }
                }

                lock (TripList)
                {
                    foreach (var trip in TripList.Where(trip => trip.MyConflicts.Count > 0 && !trip.IsAllocated))
                    {
                        trip.MyConflicts = new List<Conflict>();
                        trip.UpdateConflicts();
                        var message = "Conflicts Regenerated for Trip <" + trip.TripCode + "> Account Automatic Conflict Detection Enabled";
                        MyAutoRoutingManager?.CreateEvent(message, "CONFLICT REGENERATED", TrainAutoRoutingManager.AlertLevel.WARN, true);
                    }
                }

            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        public void RemoveAllConflictsFromTrips()
        {
            //do this function if disabled from TSUI
            try
            {
                lock (MyTripsAllocated)
                {
                    foreach (var trip in MyTripsAllocated.Where(trip => trip.MyConflicts.Count > 0))
                    {
                        trip.MyConflicts = new List<Conflict>();
                        var message = "Conflicts Removed for Trip <" + trip.TripCode + "> Account Automatic Conflict Detection Disabled";
                        MyAutoRoutingManager?.CreateEvent(message, "CONFLICT REMOVED", TrainAutoRoutingManager.AlertLevel.WARN, true);
                        MyTrainSchedulerManager?.ProduceMessage2003(trip);
                    }
                }

                lock (TripList)
                {
                    foreach (var trip in TripList.Where(trip => trip.MyConflicts.Count > 0 && !trip.IsAllocated))
                    {
                        trip.MyConflicts = new List<Conflict>();
                        var message = "Conflicts Removed for Trip <" + trip.TripCode + "> Account Automatic Conflict Detection Disabled";
                        MyAutoRoutingManager?.CreateEvent(message, "CONFLICT REMOVED", TrainAutoRoutingManager.AlertLevel.WARN, true);
                        MyTrainSchedulerManager?.ProduceMessage2003(trip);
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }

        #region Check for Infrastructure Conflicts
        private void DoCheckForInfrastructureConflictsForAllocatedTrains()
        {
            try
            {
                Thread.Sleep(20000);
                while (true)
                {
                    if (!MyEnableAutomaticConflictResolution) continue;
                    try
                    {
                        List<Trip> tempTripList;
                        lock (MyTripsAllocated)
                        {
                            //Create temp list of allocated trains
                            tempTripList = new List<Trip>(MyTripsAllocated);
                        }
                        foreach (var trip in tempTripList)
                        {
                            foreach (var tl in trip.TimedLocations)
                            {
                                //check to see if the route plan has not been executed and the route plan is greater then 10 minutes
                                if (!tl.RouteIsExecutedForTrip )
                                //if (!tl.RouteIsExecutedForTrip && tl.ArrivalTimeAdjusted > DateTime.Now.AddMinutes(10))
                                {
                                    PerformInfrastructureChecksForConflicts(tl, trip);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        MyLogger.LogException(e.ToString());
                    }
                    Thread.Sleep(10000);
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void PerformInfrastructureChecksForConflicts(TimedLocation? theLocation, Trip theTrip)
        {
            try
            {
                if (theLocation?.MyMovementPlan == null) return;
                foreach (var routeAction in theLocation.MyMovementPlan.MyRouteActions)
                {
                    var route = GetRouteFromRailGraph(routeAction.RouteName);
                    var elementList = GetElementListForRouteAction(routeAction.EndLinkUid, routeAction);
                    //var trackExtension = MyRailGraphManager?.GetTrackExtensionOfRoute(route);
                    //var list = trackExtension?.getExtensionElements();
                    //var list2 = trackExtension?.getExtensionElementsRaw();
                    foreach (var uid in elementList)
                    {
                        var ilElement = MyRailGraphManager?.ILTopoGraph?.getGraphObj(uid);
                        if (ilElement != null)
                        {
                            switch (ilElement)
                            {
                                case RailgraphLib.Interlocking.PointLeg:
                                    {
                                        break;
                                    }
                                case RailgraphLib.Interlocking.TrackSection:
                                    {
                                        ConflictCheckTrack(ilElement, theTrip, theLocation);
                                        break;
                                    }
                                case RailgraphLib.Interlocking.Track:
                                    {
                                        //ConflictCheckTrack(ilElement, theTrip);
                                        break;
                                    }
                                case RailgraphLib.Interlocking.Point:
                                    {
                                        ConflictCheckSwitch(ilElement, theTrip, theLocation, route);
                                        break;
                                    }
                            }
                        }
                    }

                    PrintElementList(elementList, theLocation, routeAction);
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private List<uint> GetElementListForRouteAction(string linkUid, RouteAction routeAction)
        {
            var elementList = new List<uint>();
            try
            {
                var edgeUid = Convert.ToInt32(linkUid);
                //var path = MyRailwayNetworkManager?.GetPath(route?.RouteName);
                var link = MyRailwayNetworkManager?.GetLink(edgeUid);
                if (link == null) return elementList;
                var route = GetRouteFromRailGraph(routeAction.RouteName);
                RailgraphLib.RailExtension.TrackExtension? te = MyRailGraphManager?.GetTrackExtensionOfRoute(route);
                if (te != null)
                {
                    foreach (var obj in te.getExtensionElementsRaw())
                    {
                        var element = MyRailGraphManager?.ILGraph?.getGraphObj(obj);
                        if (element is TrackSection)
                        {
                            AddElement(element.getId(), elementList);
                        }
                        else if (element is Point)
                        {
                            AddElement(element.getId(), elementList);
                        }
                    }
                }

                if (link != null)
                {
                    foreach (var t in link.MyTracks)
                    {
                        AddElement(t.MyUid, elementList);
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
            return elementList;
        }
        private void AddElement(uint theUid, List<uint> theList)
        {
            foreach (var uid in theList)
            {
                if (theUid == uid)
                {
                    MyLogger?.LogInfo("Element Founded In List <" + uid + ">");
                    return;
                }
            }
            //theLogger?.LogInfo("Link Added to List <" + theLink.MyReferenceNumber + "><" + theLink.MyDescription + ">");

            theList.Add(theUid);
        }
        private void ConflictCheckTrack(GraphObj theTrack, Trip theTrip, TimedLocation theLocation)
        {
            try
            {
                var track = MyRailGraphManager?.ILTopoGraph?.getGraphObj(theTrack.getId());
                var interlockingTrack = MyRailGraphManager?.ILGraph?.getILGraphObj(theTrack.getId());

                var name = theTrack.getName();
                MyLogger.LogInfo("TRACK " + name + " " + track.getId());
                var blocked = ((track as RailgraphLib.Interlocking.Track)!).isBlocked();
                var occupied = ((RailgraphLib.Interlocking.Track)track).isTrackFalseOccupied();
                
                var notControlled = ((RailgraphLib.Interlocking.Track)track).isTrackOutOfControl();
                if (blocked)
                {
                    CreateTrackBlockConflict(interlockingTrack, track, theTrip, theLocation);
                }

                if (occupied)
                {
                    CreateTrackOccupancyConflict(interlockingTrack, track, theTrip, theLocation);
                }

                if (notControlled)
                {
                    CreateTrackOutOfControlConflict(interlockingTrack, track, theTrip, theLocation);
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void CreateTrackBlockConflict(ILGraphObj? theInterlockingTrack, GraphObj theTrack, Trip theTrip, TimedLocation theLocation)
        {
            try
            {
                var name = theTrack.getName();
                var uid = theTrack.getId();
                var typeIndex = 7;
                var station = GetStation(theInterlockingTrack);
                var stationName = "UNKNOWN";
                var kilometerValue = MyRailGraphManager?.GetElementKilometerValueOfTrack(uid);

                if (station != null)
                {
                    stationName = station.SysName;
                }
                var conflictExists = theTrip.DoesConflictExist(name, uid.ToString(), typeIndex);
                if (!conflictExists)
                {
                    var theConflictDescription = "Track Blocked @ <" + name + "> <" + stationName + ">";
                    var newConflict = Conflict.CreateInstanceFromInfrastructure(theTrip, theLocation, typeIndex,
                        ConflictEntity.EntityType.Track, ConflictType.TypeOfConflict.Track, theConflictDescription,
                        name, uid.ToString(), stationName, kilometerValue.ToString());
                    theTrip.MyConflicts.Add(newConflict);
                    theTrip.MyConflictObjects.Add(ConflictObject.CreateInstance(theTrack, newConflict));
                    lock (TripList)
                    {
                        MyTrainSchedulerManager?.ProduceMessage2003(theTrip!);
                        MyLogger.LogInfo("CreateTrackBlockConflict:Trip <" + theTrip.TripCode + "> <" + name + "> <" + theLocation.Description + ">");
                    }
                    var message = "Conflict Detected for Trip <" + theTrip.TripCode + "> " + theConflictDescription;
                    CreateEvent(message, "INFRASTRUCTURE CONFLICT DETECTED", AlertLevel.WARN, true);
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void CreateTrackOccupancyConflict(ILGraphObj? theInterlockingTrack, GraphObj theTrack, Trip theTrip, TimedLocation theLocation)
        {
            try
            {
                var name = "UNKNOWN";
                name = theTrack?.getName();
                var uid = theTrack.getId();
                var typeIndex = 9;
                var station = GetStation(theInterlockingTrack);
                var stationName = "UNKNOWN";
                var kilometerValue = MyRailGraphManager?.GetElementKilometerValueOfTrack(uid);
                if (station != null)
                {
                    stationName = station.SysName;
                }
                var conflictExists = theTrip.DoesConflictExist(name, uid.ToString(), typeIndex);
                if (!conflictExists)
                {
                    var theConflictDescription = "False Track Occupancy @ <" + name + "> <" + stationName + ">";
                    var newConflict = Conflict.CreateInstanceFromInfrastructure(theTrip, theLocation, typeIndex,
                        ConflictEntity.EntityType.Track, ConflictType.TypeOfConflict.Track, theConflictDescription,
                        name, uid.ToString(), stationName, kilometerValue.ToString());
                    theTrip.MyConflicts.Add(newConflict);
                    theTrip.MyConflictObjects.Add(ConflictObject.CreateInstance(theTrack, newConflict));

                    lock (TripList)
                    {
                        MyTrainSchedulerManager?.ProduceMessage2003(theTrip!);
                        MyLogger.LogInfo("CreateTrackOccupancyConflict:Trip <" + theTrip.TripCode + "> <" + name + "> <" + theLocation.Description + ">");
                    }
                    var message = "Conflict Detected for Trip <" + theTrip.TripCode + "> " + theConflictDescription;
                    CreateEvent(message, "INFRASTRUCTURE CONFLICT DETECTED", AlertLevel.WARN, true);
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void CreateTrackOutOfControlConflict(ILGraphObj? theInterlockingTrack, GraphObj theTrack, Trip theTrip, TimedLocation theLocation)
        {
            try
            {
                var name = "UNKNOWN";
                name = theTrack?.getName();
                var uid = theTrack.getId();
                var typeIndex = 8;
                var station = GetStation(theInterlockingTrack);
                var stationName = "UNKNOWN";
                var kilometerValue = MyRailGraphManager?.GetElementKilometerValueOfTrack(uid);
                if (station != null)
                {
                    stationName = station.SysName;
                }
                var conflictExists = theTrip.DoesConflictExist(name, uid.ToString(), typeIndex);
                if (!conflictExists)
                {
                    var theConflictDescription = "Track Out Of Control @ <" + name + "> <" + stationName + ">";
                    var newConflict = Conflict.CreateInstanceFromInfrastructure(theTrip, theLocation, typeIndex,
                        ConflictEntity.EntityType.Track, ConflictType.TypeOfConflict.Track, theConflictDescription,
                        name, uid.ToString(), stationName, kilometerValue.ToString());
                    theTrip.MyConflicts.Add(newConflict);
                    theTrip.MyConflictObjects.Add(ConflictObject.CreateInstance(theTrack, newConflict));

                    lock (TripList)
                    {
                        MyTrainSchedulerManager?.ProduceMessage2003(theTrip!);
                        MyLogger.LogInfo("CreateTrackOutOfControlConflict:Trip <" + theTrip.TripCode + "> <" + name + "> <" + theLocation.Description + ">");
                    }
                    var message = "Conflict Detected for Trip <" + theTrip.TripCode + "> " + theConflictDescription;
                    CreateEvent(message, "INFRASTRUCTURE CONFLICT DETECTED", AlertLevel.WARN, true);
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void PrintElementList(List<uint> elementList, TimedLocation theLocation, RouteAction theRouteAction)
        {
            try
            {
                foreach (var uid in elementList)
                {
                    var theObj = MyRailGraphManager?.ILTopoGraph?.getGraphObj(uid);

                    MyLogger.LogDebug("Element to Check for Conflict...Location <" + theLocation.Description + "><" +
                                     theObj?.getClassType() + "><" + theObj?.getName() + "><" + "><" + theObj?.getId() + "><" + theRouteAction.RouteName +
                                     ">");
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void PrintElementPerRouteAction(GraphObj theObj, TimedLocation theLocation, RouteAction theRouteAction)
        {
            try
            {
                MyLogger.LogInfo("Element to Check for Conflict...Location <" + theLocation.Description + "><" + theObj.getClassType() + "><" + theObj.getId() + "><" + theRouteAction.RouteName + ">");

            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void ConflictCheckTrackSection(GraphObj theTrackSection, Trip theTrip, TimedLocation theLocation)
        {
            try
            {
                var trackSection = MyRailGraphManager?.ILTopoGraph?.getGraphObj(theTrackSection.getId());
                var blocked = ((trackSection as RailgraphLib.Interlocking.TrackSection)!).isBlocked();
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private Track? GetTrack(string trackName)
        {
            foreach (var track in MyRailGraphManager?.MyTracks!)
            {
                if (track.MyName == trackName) return track;
            }

            return null;
        }
        private Station? GetStation(ILGraphObj? theObject)
        {
            try
            {
                foreach (var stn in MyRailGraphManager?.HierarchyRelations?.Stations!)

                {
                    if (stn.SysID == theObject?.getLogicalStation())
                    {
                        return stn;
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
            return null;
        }
        private void ConflictCheckSwitch(GraphObj theSwitch, Trip theTrip, TimedLocation theLocation, Route theRoute)
        {
            try
            {
                var sw = MyRailGraphManager?.ILTopoGraph?.getGraphObj(theSwitch.getId());
                var pt = MyRailGraphManager?.ILGraph?.getILGraphObj(sw.getId());
                var pointName = sw?.getName();

                var blocked = ((Point)sw!).isOperationBlocked();
                var occupiedState = ((RailgraphLib.Interlocking.Point)sw).isPointFalseOccupied();
                var outOfControl = ((RailgraphLib.Interlocking.Point)sw).isPointOutOfControl();
                var myPointListInWrongPosition = MyRailGraphManager?.GetAllPointsInWrongPositionOnRoute(theRoute);


                if (blocked && DoesPointExistInList(myPointListInWrongPosition, pointName))
                {
                    CreatePointBlockConflict(pt, sw, theTrip, theLocation);
                }

                if (occupiedState)
                {
                    CreatePointOccupancyConflict(pt, sw, theTrip, theLocation);
                }

                if (outOfControl)
                {
                    CreatePointOutOfControlConflict(pt, sw, theTrip, theLocation);
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void CreatePointBlockConflict(ILGraphObj? theInterlockingSwitch, GraphObj theSwitch, Trip theTrip, TimedLocation theLocation)
        {
            try
            {
                var name = theSwitch.getName();
                var uid = theSwitch.getId();
                var typeIndex = 12;
                var station = GetStation(theInterlockingSwitch);
                var stationName = "UNKNOWN";
                var kilometerValue = MyRailGraphManager?.GetElementKilometerValueOfPoint(uid);

                if (station != null)
                {
                    stationName = station.SysName;
                }
                var conflictExists = theTrip.DoesConflictExist(name, uid.ToString(), typeIndex);
                if (!conflictExists)
                {
                    var theConflictDescription = "Point Blocked @ <" + name + "> <" + stationName + ">";
                    var newConflict = Conflict.CreateInstanceFromInfrastructure(theTrip, theLocation, typeIndex,
                        ConflictEntity.EntityType.Point, ConflictType.TypeOfConflict.Point, theConflictDescription,
                        name, uid.ToString(), stationName, kilometerValue.ToString());
                    theTrip.MyConflicts.Add(newConflict);
                    theTrip.MyConflictObjects.Add(ConflictObject.CreateInstance(theSwitch, newConflict));
                    lock (TripList)
                    {
                        MyTrainSchedulerManager?.ProduceMessage2003(theTrip!);
                        MyLogger.LogInfo("CreatePointBlockConflict:Trip <" + theTrip.TripCode + "> <" + name + "> <" + theLocation.Description + ">");
                    }
                    var message = "Conflict Detected for Trip <" + theTrip.TripCode + "> " + theConflictDescription;
                    CreateEvent(message, "INFRASTRUCTURE CONFLICT DETECTED", AlertLevel.WARN, true);
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void CreatePointOccupancyConflict(ILGraphObj? theInterlockingSwitch, GraphObj theSwitch, Trip theTrip, TimedLocation theLocation)
        {
            try
            {
                var name = theSwitch.getName();
                var uid = theSwitch.getId();
                var typeIndex = 11;
                var station = GetStation(theInterlockingSwitch);
                var stationName = "UNKNOWN";
                var kilometerValue = MyRailGraphManager?.GetElementKilometerValueOfPoint(uid);

                if (station != null)
                {
                    stationName = station.SysName;
                }
                var conflictExists = theTrip.DoesConflictExist(name, uid.ToString(), typeIndex);
                if (!conflictExists)
                {
                    var theConflictDescription = "Point False Occupancy @ <" + name + "> <" + stationName + ">";
                    var newConflict = Conflict.CreateInstanceFromInfrastructure(theTrip, theLocation, typeIndex,
                        ConflictEntity.EntityType.Point, ConflictType.TypeOfConflict.Point, theConflictDescription,
                        name, uid.ToString(), stationName, kilometerValue.ToString());
                    theTrip.MyConflicts.Add(newConflict);
                    theTrip.MyConflictObjects.Add(ConflictObject.CreateInstance(theSwitch, newConflict));
                    lock (TripList)
                    {
                        MyTrainSchedulerManager?.ProduceMessage2003(theTrip!);
                        MyLogger.LogInfo("CreatePointOccupancyConflict:Trip <" + theTrip.TripCode + "> <" + name + "> <" + theLocation.Description + ">");
                    }
                    var message = "Conflict Detected for Trip <" + theTrip.TripCode + "> " + theConflictDescription;
                    CreateEvent(message, "INFRASTRUCTURE CONFLICT DETECTED", AlertLevel.WARN, true);
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void CreatePointOutOfControlConflict(ILGraphObj? theInterlockingSwitch, GraphObj theSwitch, Trip theTrip, TimedLocation theLocation)
        {
            try
            {
                var name = theSwitch.getName();
                var uid = theSwitch.getId();
                var typeIndex = 10;
                var station = GetStation(theInterlockingSwitch);
                var stationName = "UNKNOWN";
                var kilometerValue = MyRailGraphManager?.GetElementKilometerValueOfPoint(uid);

                if (station != null)
                {
                    stationName = station.SysName;
                }
                var conflictExists = theTrip.DoesConflictExist(name, uid.ToString(), typeIndex);
                if (!conflictExists)
                {
                    var theConflictDescription = "Point Out Of Control @ <" + name + "> <" + stationName + ">";
                    var newConflict = Conflict.CreateInstanceFromInfrastructure(theTrip, theLocation, typeIndex,
                        ConflictEntity.EntityType.Point, ConflictType.TypeOfConflict.Point, theConflictDescription,
                        name, uid.ToString(), stationName, kilometerValue.ToString());
                    theTrip.MyConflicts.Add(newConflict);
                    theTrip.MyConflictObjects.Add(ConflictObject.CreateInstance(theSwitch, newConflict));
                    lock (TripList)
                    {
                        MyTrainSchedulerManager?.ProduceMessage2003(theTrip!);
                        MyLogger.LogInfo("CreatePointOutOfControlConflict:Trip <" + theTrip.TripCode + "> <" + name + "> <" + theLocation.Description + ">");
                    }
                    var message = "Conflict Detected for Trip <" + theTrip.TripCode + "> " + theConflictDescription;
                    CreateEvent(message, "INFRASTRUCTURE CONFLICT DETECTED", AlertLevel.WARN, true);
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private bool DoesPointExistInList(List<Point>? thePoints, string? thePointName)
        {
            try
            {
                if (thePoints != null)
                    foreach (var p in thePoints)
                    {
                        if (((RailgraphLib.Interlocking.Point)p).getName() == thePointName) return true;
                    }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
            return false;
        }
        private Route GetRouteFromRailGraph(string? routeName)
        {
            foreach (var route in MyRailGraphManager.HierarchyRelations.Routes)
            {
                if (routeName == route.SysName) return route;
            }

            return null;
        }
        #endregion

        #region Check to remove Conflict
        private void DoCheckForInfrastructureRemovalOfConflictsForAllocatedTrains()
        {
            try
            {
                Thread.Sleep(60000);
                while (true)
                {
                    if (!MyEnableAutomaticConflictResolution) continue;
                    try
                    {
                        List<Trip> tempTripList;
                        lock (MyTripsAllocated)
                        {
                            //Create temp list of allocated trains
                            tempTripList = new List<Trip>(MyTripsAllocated);
                        }

                        foreach (var trip in tempTripList)
                        {
                            foreach (var obj in trip.MyConflictObjects)
                            {
                                if (PerformInfrastructureCheckToRemoveConflict(obj))
                                //Remove the Conflict and break for this trip
                                {
                                    RemoveConflict(trip, obj);
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        MyLogger.LogException(e.ToString());
                    }
                    Thread.Sleep(10000);
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void RemoveConflict(Trip theTrip, ConflictObject theConflictObject)
        {
            try
            {
                var track = MyRailGraphManager?.ILTopoGraph?.getGraphObj(theConflictObject.MyObject.getId());

                var name = track.getName();
                var interlockingTrack = MyRailGraphManager?.ILGraph?.getILGraphObj(theConflictObject.MyObject.getId());

                var station = GetStation(interlockingTrack);
                var stationName = "UNKNOWN";
                if (station != null)
                {
                    stationName = station.SysName;
                }


                foreach (var conflict in theTrip.MyConflicts)
                {
                    if (theConflictObject.MyConflict.MyGuid != conflict.MyGuid) continue;
                    theTrip.MyConflicts.Remove(conflict);
                    break;
                }
                foreach (var obj in theTrip.MyConflictObjects)
                {
                    if (obj.MyGuid == theConflictObject.MyGuid)
                    {
                        theTrip.MyConflictObjects.Remove(theConflictObject);
                        lock (TripList)
                        {
                            MyTrainSchedulerManager?.ProduceMessage2003(theTrip!);
                            MyLogger.LogInfo("RemoveConflict:Trip <" + theTrip.TripCode + "> <" + name + "> <" + stationName + ">");
                        }
                        var message = "Conflict Removed for Trip <" + theTrip.TripCode + "> " + theConflictObject.MyConflict.MyDescription + "<" + "> <" + name + "> <" + stationName + ">";
                        CreateEvent(message, "INFRASTRUCTURE CONFLICT REMOVED", AlertLevel.INFORMATION, true);

                        break;
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private bool PerformInfrastructureCheckToRemoveConflict(ConflictObject theConflictObject)
        {
            try
            {
                switch (theConflictObject.MyObject)
                {
                    case RailgraphLib.Interlocking.TrackSection:
                        {
                            if (!CheckConflictObjectStatus(theConflictObject)) return true;
                            break;
                        }
                    case RailgraphLib.Interlocking.Point:
                        {
                            if (!CheckConflictObjectStatus(theConflictObject)) return true;
                            break;
                        }
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }

            return false;
        }
        private bool CheckConflictObjectStatus(ConflictObject theConflictObject)
        {
            try
            {
                switch (theConflictObject.MyConflict.MyTypeOfConflict)
                {
                    case ConflictType.TypeOfConflict.Track:
                        {
                            return CheckTrackStatus(theConflictObject);
                        }
                    case ConflictType.TypeOfConflict.Point:
                    {
                        return (CheckPointStatus(theConflictObject));
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }

            return false;
        }
        private bool CheckTrackStatus(ConflictObject theConflictObject)
        {
            try
            {
                var track = MyRailGraphManager?.ILTopoGraph?.getGraphObj(theConflictObject.MyObject.getId());

                switch (theConflictObject.MyConflict.MySubtypeOfConflict.MyIndex)
                {
                    case 7:
                        {
                            //Track Blocked
                            return ((track as RailgraphLib.Interlocking.Track)!).isBlocked();
                        }
                    case 8:
                        {
                            //Track Not Controlled
                            return ((track as RailgraphLib.Interlocking.Track)!).isTrackOutOfControl();

                            break;
                        }
                    case 9:
                        {
                            return ((RailgraphLib.Interlocking.Track)track!).isTrackFalseOccupied();
                        }

                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }

            return false;
        }
        private bool CheckPointStatus(ConflictObject theConflictObject)
        {
            try
            {
                var point = MyRailGraphManager?.ILTopoGraph?.getGraphObj(theConflictObject.MyObject.getId());

                switch (theConflictObject.MyConflict.MySubtypeOfConflict.MyIndex)
                {
                    case 12:
                    {
                        //Point Blocked
                        return ((point as RailgraphLib.Interlocking.Point)!).isOperationBlocked();
                    }
                    case 10:
                    {
                            //Point Not Controlled
                            return ((point as RailgraphLib.Interlocking.Point)!).isPointOutOfControl();

                            break;
                    }
                    case 11:
                    {
                        return (((Point)point!)!).isPointFalseOccupied();
                    }

                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }

            return false;
        }

        #endregion

        #region Station Neck Conflicts

        private void DoStationNeckConflictChecks()
        {
            try
            {
                Thread.Sleep(30000);
                while (true)
                {
                    try
                    {
                        foreach (var stn in MyRailwayNetworkManager.MyStations)
                        {
                            if (stn.HasStationNecks)
                            {
                                List<Reservation> tempReservations = new List<Reservation>();
                                lock (stn.MyReservations)
                                {
                                    tempReservations = new List<Reservation>(stn.MyReservations);
                                    foreach (var r in tempReservations)
                                    {
                                        
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        MyLogger?.LogException(e.ToString());
                    }
                    Thread.Sleep(1000 * 15);
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }

        #endregion
        #region Possessions
        public void CheckToAddPossession(string thePossession)
        {
            try
            {
                var thread = new Thread(() => DoAddPossession(thePossession)); thread.Start();
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }
        public void CheckToRemovePossession(string thePossession)
        {
            try
            {
                var thread = new Thread(() => DoRemovePossession(thePossession)); thread.Start();
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }
        private void DoAddPossession(string thePossession)
        {
            try
            {
                var possession = DeserializeMyObject<Possession>(MyLogger, thePossession);
                if (possession == null) return;
                lock (MyPossessions)
                {
                    foreach (var poss in MyPossessions)
                    {
                        if (poss.Id != possession.Id) continue;
                        MyLogger?.LogInfo("DoAddPossession:Possession Already Exists <" + poss.Id + "><" + poss.Description + ">");
                        return;
                    }
                    possession.ApplyTimeUpdate();
                    MyPossessions.Add(possession);
                }
                DoCheckForConflictsFromPossession(possession);
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }
        private void DoRemovePossession(string thePossession)
        {
            try
            {
                var possession = DeserializeMyObject<Possession>(MyLogger, thePossession);
                if (possession == null) return;
                lock (MyPossessions)
                {
                    foreach (var poss in MyPossessions)
                    {
                        if (poss.Id != possession.Id) continue;
                        RemovePossessionConflicts(possession);
                        MyLogger?.LogInfo("DoRemovePossession:Possession Removed <" + poss.Id + "><" + poss.Description + ">");
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }
        private void RemovePossessionConflicts(Possession thePossession)
        {
            try
            {
                List<Trip> tempTripList;
                lock (TripList)
                {
                    //Create temp list of allocated trains
                    tempTripList = new List<Trip>(TripList);
                }

                foreach (var trip in tempTripList)
                {
                    foreach (var conflict in trip.MyConflicts)
                    {
                        if (!conflict.IsPossessionConflict || conflict.MyPossessionUid != thePossession.Id) continue;
                        trip.MyConflicts.Remove(conflict);
                        MyTrainSchedulerManager?.ProduceMessage2003(trip!);
                        var message = "Possession Conflict Removed for Trip <" + trip.TripCode + "> <" + conflict.MyDescription +">";
                        CreateEvent(message, "INFRASTRUCTURE CONFLICT DETECTED", AlertLevel.INFORMATION, true);
                        MyLogger.LogInfo("RemovePossessionConflicts:Trip <" + trip.TripCode + "> <" + thePossession.Description + "> <" + thePossession.Id + ">");
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }
        private void DoCheckForConflictsFromPossession(Possession thePossession)
        {
            try
            {
                List<Trip> tempTripList;
                lock (TripList)
                {
                    //Create temp list of allocated trains
                    tempTripList = new List<Trip>(TripList);
                }
                var position = MyRailGraphManager?.CreateElementPosition(thePossession.StartPos.ElementId, thePossession.StartPos.Offset, true);

                bool conflictFound = false;
                foreach (var trip in tempTripList)
                {
                    Reservation conflictingReservation = null;
                    foreach (var reservation in trip.MyReservations)
                    {
                        if (reservation.MyEdgeName == thePossession.StartPos?.ElementId || reservation.MyEdgeName == thePossession.EndPos?.ElementId)
                        {
                            if (reservation.TimeBegin >= thePossession.MyStartTime && reservation.TimeBegin <= thePossession.MyEndTime) conflictFound = true;
                            if (reservation.TimeEnd >= thePossession.MyStartTime && reservation.TimeEnd <= thePossession.MyEndTime) conflictFound = true;

                            if (thePossession.MyStartTime >= reservation.TimeBegin && thePossession.MyStartTime <= reservation.TimeEnd) conflictFound = true;
                            if (thePossession.MyEndTime >= reservation.TimeBegin && thePossession.MyEndTime <= reservation.TimeEnd) conflictFound = true;
                        }
                        if (conflictFound) conflictingReservation = reservation;
                    }

                    if (conflictFound)
                    {
                        var theTrack = MyRailGraphManager?.GetILElementOnElementPositionModel(thePossession?.StartPos);
                        if (theTrack != null)
                        {
                            var track = MyRailGraphManager?.ILTopoGraph?.getGraphObj(theTrack.getId());
                            var interlockingTrack = MyRailGraphManager?.ILGraph?.getILGraphObj(theTrack.getId());
                            CreatePossessionConflict(interlockingTrack, track, trip, conflictingReservation.MyTimedLocation, thePossession);
                        }
                        conflictFound = false;
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }
        private void CreatePossessionConflict(ILGraphObj? theInterlockingTrack, GraphObj theTrack, Trip theTrip, TimedLocation theLocation, Possession thePossession)
        {
            try
            {
                 var name = theTrack.getName();
                var uid = theTrack.getId();
                var typeIndex = 6;
                var station = GetStation(theInterlockingTrack);
                var stationName = "UNKNOWN";
                var kilometerValue = MyRailGraphManager?.GetElementKilometerValueOfTrack(uid);

                if (station != null)
                {
                    stationName = station.SysName;
                }
                var conflictExists = theTrip.DoesConflictExist(name, uid.ToString(), typeIndex);
                if (!conflictExists)
                {
                    var theConflictDescription = "Possession Exists @ <" + name + "> <" + stationName + ">";
                    var newConflict = Conflict.CreateInstanceFromInfrastructure(theTrip, theLocation, typeIndex,
                        ConflictEntity.EntityType.Track, ConflictType.TypeOfConflict.Track, theConflictDescription,
                        name, uid.ToString(), stationName, kilometerValue.ToString());
                    theTrip.MyConflicts.Add(newConflict);
                    newConflict.IsPossessionConflict = true;
                    newConflict.MyPossessionUid = thePossession.Id;
                    theTrip.MyConflictObjects.Add(ConflictObject.CreateInstance(theTrack, newConflict));
                    lock (TripList)
                    {
                        MyTrainSchedulerManager?.ProduceMessage2003(theTrip!);
                        MyLogger.LogInfo("CreatePossessionConflict:Trip <" + theTrip.TripCode + "> <" + name + "> <" + theLocation.Description + ">");
                    }
                    var message = "Possession Conflict Detected for Trip <" + theTrip.TripCode + "> " + theConflictDescription;
                    CreateEvent(message, "INFRASTRUCTURE CONFLICT DETECTED", AlertLevel.WARN, true);
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }

        #endregion
        #endregion

        #region Automatic Train Routing

        #region Delegate Tasks
        private async Task<bool> RequestExecuteRoute(RoutePlanInfo theRoute)
        {
            return await Task.FromResult(DoExecuteRoute!.Invoke(theRoute));
        }
        private async Task<bool> RequestSendCompleteRoutePlan(RoutePlanInfo theRoute)
        {
            return await Task.FromResult(DoSendCompleteRoutePlan!.Invoke(theRoute));
        }
        private async Task<bool> RequestIsRouteExecuted(string routeName, int planUid)
        {
            return await Task.FromResult(IsRouteExecuted!.Invoke(routeName, planUid));
        }
        private async Task<int> RequestIsRouteAvailable(string originalRouteName, string replacementRouteName, int planUid, int tripUid, string trainObid, RoutePlanInfo theRoutePlan)
        {
            return await Task.FromResult(IsRouteAvailable!.Invoke(originalRouteName,replacementRouteName, planUid, tripUid, trainObid, theRoutePlan));
        }
        private async Task<bool> RequestRouteMarkingUpdate(string trainObid, List<Tuple<DateTime, string?>> markings)
        {
            return await Task.FromResult(DoSendRouteMarkerUpdate!.Invoke(trainObid, markings));
        }
        private async Task<bool> RequestForecastUpdate(Forecast theForecast)
        {
            return await Task.FromResult(DoSendForecastUpdate!.Invoke(theForecast));
        }

        #endregion

        #region Routing Methods
        private void PerformMonitorRouteClearanceForAllocatedTrains()
        {
            try
            {
                Thread.Sleep(1000 * 40);
                while (true)
                {
                    {
                        try
                        {
                            List<Trip> tempTripList;
                            lock (MyTripsAllocated)
                            {
                                //Create temp list of allocated trains
                                tempTripList = new List<Trip>(MyTripsAllocated);
                            }

                            foreach (var trip in tempTripList)
                            {
                                foreach (var tl in trip.TimedLocations)
                                {
                                    if (!tl.RouteIsExecutedForTrip) continue;
                                    if (tl.MyMovementPlan == null) continue;
                                    foreach (var ra in tl.MyMovementPlan.MyRouteActions)
                                    {
                                        try
                                        {
                                            var sp = GetSignalPoint(trip, ra.MySignalName);
                                            var noConfirmationRouteCleared = !ra.HasConfirmationRouteClearedFromRos || !sp.IsClear();
                                            var pastTimeCommandToRos = ra.IsTimeRouteSentToRos < DateTime.Now.AddSeconds(-60);
                                            if (ra.HasBeenSentToRos && !noConfirmationRouteCleared && pastTimeCommandToRos)
                                            {
                                                ra.IsTimeRouteSentToRos = DateTime.Now;
                                                //TODO Resend route command to ROS with no trigger point
                                                var message = "Route Plan Was Detected NOT EXECUTED From ROS...Resending Route Command for Trip <" + trip.TripCode + "><" + ra.RouteName + ">";
                                                CreateEvent(message, "ROS ROUTE EXECUTION COMPLETE", AlertLevel.INFORMATION, true);
                                            }

                                        }
                                        catch (Exception e)
                                        {
                                            MyLogger.LogException(e.ToString());
                                        }
                                        //Route Marking Update, but not needed because of another thread monitoring this situation
                                        //if (ra.HasConfirmationRouteClearedFromRos && !ra.HasSignalBeenCleared)
                                        //{
                                        //    var index = trip.TimedLocations.IndexOf(tl);
                                        //    var nextTimeLocation = trip.TimedLocations[index + 1];
                                        //    var sp = GetSignalPoint(trip, ra.MySignalName);
                                        //    if (sp.IsClear()) ExecuteRouteMarkerUpdate(trip, tl, nextTimeLocation);
                                        //}
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            MyLogger.LogException(e.ToString());
                        }
                    }
                    Thread.Sleep(40 * 1000);
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        public void UpdateRouteExecutionComplete(string trainSysId, string trainObid, string routeName)
        {
            try
            {
                var thread = new Thread(() => DoUpdateRouteExecutionComplete(trainSysId, trainObid,routeName)); thread.Start();
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void DoUpdateRouteExecutionComplete(string trainSysId, string trainObid, string routeName)
        {
            try
            {
                CheckToUpdateAutoRoutingExecution(trainSysId, trainObid, routeName);
                DoUpdateRouteMarkingForRouteExecuted(trainSysId, trainObid, routeName);
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void CheckToUpdateAutoRoutingExecution(string trainSysId, string trainObid, string routeName)
        {
            try
            {
                var trip = FindTripFromTrainSystemId(trainSysId);
                if (trip == null)
                {
                    trip = FindTripFromTrainObid(trainObid);
                }
                if (trip == null)
                {
                    MyLogger.LogInfo("CheckToUpdateAutoRoutingExecution:Trip NOT FOUND FOR TRAIN SYSTEM ID" + trainSysId + "><" + routeName + ">");
                }

                foreach (var tl in trip.TimedLocations)
                {
                    if (tl.MyMovementPlan == null) continue;
                    foreach (var ra in tl.MyMovementPlan.MyRouteActions)
                    {
                        if (ra.RouteName == routeName)
                        {
                            if (tl == trip.LastTimedLocation) trip.LastTimedLocation.RouteIsExecutedForTrip = true;
                            ra.HasConfirmationRouteClearedFromRos = true;
                            var message = "Route Plan Was Confirmed From ROS as Execution Complete for Trip <" + trip.TripCode + "><" + routeName + ">";
                            CreateEvent(message, "ROS ROUTE EXECUTION COMPLETE", AlertLevel.WARN, true);
                            MyLogger.LogInfo("CheckToUpdateAutoRoutingExecution:Route Found at Timed Location <" + trip.LastTimedLocation.Description + "><" + routeName + ">");
                        }
                    }
                }

                //if (trip?.LastTimedLocation != null)
                //{
                //    var routeAction = trip.LastTimedLocation.MyMovementPlan.MyRouteActions[0];
                //    if (routeAction != null)
                //    {
                //        if (routeAction.RouteName == routeName)
                //        {
                //            trip.LastTimedLocation.RouteIsExecutedForTrip = true;
                //            var message = "Route Plan Was Confirmed From ROS as Execution Complete for MockTrip <" + trip.TripCode + "><" + routeName + ">";
                //            CreateEvent(message, "ROS ROUTE EXECUTION COMPLETE", AlertLevel.INFORMATION, true);
                //            MyLogger.LogInfo("CheckToUpdateAutoRoutingExecution:Route Found at Timed Location <" + trip.LastTimedLocation.Description + "><" + routeName + ">");
                //        }
                //    }
                //}
                //else
                //{
                //    MyLogger.LogInfo("CheckToUpdateAutoRoutingExecution:MockTrip Location NOT FOUND FOR TRAIN " + trainSysId + "><" + trainObid + "><" + routeName + ">");
                //}
                //DoUpdateRouteActionFromRosConfirmation(trip, routeName);
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void CheckToUpdateAutoRoutingExecutionOld(string trainSysId, string trainObid, string routeName)
        {
            try
            {
                var trip = FindTripFromTrainSystemId(trainSysId);
                if (trip == null)
                {
                    trip = FindTripFromTrainObid(trainObid);
                }

                if (trip == null)
                {
                    MyLogger.LogInfo("CheckToUpdateAutoRoutingExecution:Trip NOT FOUND FOR TRAIN SYSTEM ID" + trainSysId + "><" + routeName + ">");
                }

                if (trip?.LastTimedLocation != null)
                {
                    var routeAction = trip.LastTimedLocation.MyMovementPlan.MyRouteActions[0];
                    if (routeAction != null)
                    {
                        if (routeAction.RouteName == routeName)
                        {
                            trip.LastTimedLocation.RouteIsExecutedForTrip = true;
                            var message = "Route Plan Was Confirmed From ROS as Execution Complete for Trip <" + trip.TripCode + "><" + routeName +">";
                            CreateEvent(message, "ROS ROUTE EXECUTION COMPLETE", AlertLevel.INFORMATION, true);
                            MyLogger.LogInfo("CheckToUpdateAutoRoutingExecution:Route Found at Timed Location <"+ trip.LastTimedLocation.Description +"><" +routeName +">");
                        }
                    }
                }
                else
                {
                    MyLogger.LogInfo("CheckToUpdateAutoRoutingExecution:Trip Location NOT FOUND FOR TRAIN " + trainSysId + "><" + trainObid + "><" + routeName + ">");
                }
                DoUpdateRouteActionFromRosConfirmation(trip, routeName);
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void DoUpdateRouteActionFromRosConfirmation(Trip theTrip, string theRouteName)
        {
            try
            {
                foreach (var tl in theTrip.TimedLocations)
                {
                    if (tl.MyMovementPlan == null) continue;
                    foreach (var ra in tl.MyMovementPlan.MyRouteActions)
                    {
                        if (ra.RouteName == theRouteName)
                        {
                            ra.HasConfirmationRouteClearedFromRos = true;
                            var message = "Route Action Was Confirmed From ROS as Execution Complete for Trip <" + theTrip.TripCode + "><" + theRouteName + ">";
                            CreateEvent(message, "ROS ROUTE EXECUTION CONFIRMED", AlertLevel.WARN, true);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void DoForceRouteExecutionOnAllocatedTrains()
        {
            try
            {
                Thread.Sleep(1000 * 60);
                while (true)
                {
                    try
                    {
                        List<Trip> tempTripList;
                        lock (MyTripsAllocated)
                        {
                            //Create temp list of allocated trains
                            tempTripList = new List<Trip>(MyTripsAllocated);
                        }

                        foreach (var trip in tempTripList)
                        {
                            //if (IsTrainPositionTimePast(trip))
                            //{
                            CheckToExecuteRouteToNextPlatform(trip, trip.MyTrainPosition);
                            //}
                        }
                    }
                    catch (Exception e)
                    {
                        MyLogger.LogException(e.ToString());
                    }
                    Thread.Sleep(5000);
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void CheckPositionChange(Trip theTrip, TrainPosition thePosition, bool ignorePositionCheck = false)
        {
            try
            {
                var positionHasChanged = IsTrainPositionChange(theTrip, thePosition);
                if (!ignorePositionCheck)
                {
                    if (!positionHasChanged) return;
                }
                if (positionHasChanged) AddPositionToTrip(theTrip, thePosition);
                 
                if (positionHasChanged) CheckForArrivalDepartureEvent(theTrip, thePosition);

                //be sure the routes have executed to next platform
                if (positionHasChanged) CheckToExecuteRouteToNextPlatform(theTrip, thePosition, positionHasChanged);

                //check to see if reservations can be released behind the train movement
                var reservationsReleased =  CheckToReleaseReservation(theTrip, thePosition);

                //if reservations are released then perform a conflict update on all trips
                //and send updates to TSUI
                List<Trip> tripsToUpdateFromReleasedConflicts = null!;
                if (reservationsReleased.Count > 0)
                {
                    tripsToUpdateFromReleasedConflicts = CheckToUpdateConflicts(reservationsReleased);
                }

                //during the "check to see if reservations can be released above
                //a check is made to see if trip is completed to last platform, if so do trip delete and cleanup.
                if (tripsToUpdateFromReleasedConflicts is { Count: > 0 })
                {
                    DoTripUpdates(tripsToUpdateFromReleasedConflicts);
                }

                //if trip marked as completed then delete and inform TSUI
                if (theTrip.IsCompleted) TripDelete(theTrip, null);
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void CheckToExecuteRouteToNextPlatform(Trip theTrip, TrainPosition thePosition, bool performRouteMarking = true)
        {
            try
            {
                var trainUid = theTrip.MyTrainObid;
                var schedulePlanUid = Convert.ToInt32(theTrip.ScheduledPlanId);//theTrip.SerUid;
                var tripUid = theTrip.TripId;
                if (string.IsNullOrEmpty(trainUid))
                {
                    trainUid = thePosition.Obid;
                }
                var originalRouteName = "routeName";
                {
                    foreach (var tl in theTrip.TimedLocations)
                    {
                        if (tl.MyMovementPlan == null)
                        {
                            return;
                        }
                        //todo check if signal cleared 
                        if (tl.RouteIsExecutedForTrip || tl.MyMovementPlan.MyRouteActions.Count ==0) continue;
                        var firstRouteAction = tl.MyMovementPlan.MyRouteActions[0].ActionLocation;
                        var actionLocationFound = CheckForActionLocationMatch(firstRouteAction, thePosition, tl, theTrip);
                        MyLogger.LogDebug("Trip <" + theTrip.TripCode + "> is found @ <" + thePosition.ElementExtension?.StartPos.ElementId + ">");
                        if (!tl.IsPastTriggerPoint)
                        {
                            if (!actionLocationFound)
                            {
                                MyLogger.LogDebug("Trip <" + theTrip.TripCode + "> is found @ <" + thePosition.ElementExtension?.StartPos.ElementId + ">");
                                MyLogger.LogDebug("Action Location <" + firstRouteAction + ">");
                                continue;
                            }
                        }
                        MyLogger.LogDebug("Trip <" + theTrip.TripCode + "> IS ON OR PAST TRIGGER POINT @ <" + thePosition.ElementExtension?.StartPos.ElementId + ">");
                        tl.IsPastTriggerPoint = true; 

                        //Set timed location to the Last Location Found
                        theTrip.LastTimedLocation = tl;


                        //if route to next platform has already been executed for the train on the current platform then ignore

                        //if departure time is expired then execute routes to next platform
                        if (!MyDisableDepartTimeCheckForAutoRouting) if (tl.DepartureTimeActual > DateTime.Now) continue;

                        if (tl.MyMovementPlan == null) return;
                        var theRoutePlanInfo = CreateRoutePlanInformation(theTrip, tl);
                        //if (tl.RemoveActionPointsFromRoutePlan)
                        //{
                        //    theRoutePlanInfo.RemoveActionPointsFromRoutePlan = true;
                        //}
                        var index = 0;
                        foreach (var ra in tl.MyMovementPlan.MyRouteActions)
                        {
                            index+=1;
                            if (!string.IsNullOrEmpty(ra.RouteName)) originalRouteName = ra.RouteName;

                            ////Is route already cleared for train??
                            //var routeIsExecuted = RequestIsRouteExecuted(originalRouteName, schedulePlanUid).Result;
                            //if (routeIsExecuted) continue;

                            //if route not cleared for train, check availability
                            if (tl.RouteIsAlreadyAvailableForTrip) continue;
                            if (!MyRailGraphManager.DoesRouteExist(ra.RouteName)) continue;
                            var resultIndex = RequestIsRouteAvailable(originalRouteName, originalRouteName,schedulePlanUid, tripUid, trainUid, theRoutePlanInfo).Result;
                            if (GetRouteAvailabilityResult(resultIndex, ra, theTrip))
                            {
                                if (index == 1)
                                {
                                    //if the first route action (route) passes the pre-test, go ahead and send to ROS for execution
                                    MyLogger.LogInfo(originalRouteName + " has past ROS Pre-Test for Trip <" + theTrip.TripCode + "> from platform  <" + tl.MyMovementPlan.FromName + "> to platform <" + tl.MyMovementPlan.ToName + ">");
                                    tl.RouteIsAlreadyAvailableForTrip = true;
                                    var message = originalRouteName + " has past ROS Pre-Test for Trip <" + theTrip.TripCode + "> from platform  <" + tl.MyMovementPlan.FromName + "> to platform <" + tl.MyMovementPlan.ToName + ">";
                                    CreateEvent(message, "ROS ROUTE EXECUTION COMPLETE", AlertLevel.WARN, true);
                                    break;
                                }
                            }
                            else
                            {
                                MyLogger.LogInfo(originalRouteName + " has failed ROS Pre-Test for Trip <" + theTrip.TripCode + "> from platform  <" + tl.MyMovementPlan.FromName + "> to platform <" + tl.MyMovementPlan.ToName + ">");
                                return;
                            }
                        }
                        //if route is available then execute the route
                        var routeHasBeenExecuted = ExecuteAutoRoutePlan(theTrip, tl, performRouteMarking);
                        if (routeHasBeenExecuted)
                        {
                            MyLogger.LogInfo(originalRouteName + " has been sent for execution for Trip <" + theTrip.TripCode + "> from platform  <" + tl.MyMovementPlan.FromName + "> to platform <" + tl.MyMovementPlan.ToName + ">");
                            tl.RouteIsExecutedForTrip = true;
                        }
                        else
                        {
                            MyLogger.LogInfo(originalRouteName + " has NOT been executed for Trip <" + theTrip.TripCode + "> from platform  <" + tl.MyMovementPlan.FromName + "> to platform <" + tl.MyMovementPlan.ToName + ">");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void ReplaceRouteActions(List<RouteAction> theRouteActions, TimedLocation theLocation)
        {
            try
            {
                theLocation.MyMovementPlan.MyRouteActions = new List<RouteAction>();
                foreach (var ra in theRouteActions)
                {
                    theLocation.MyMovementPlan.MyRouteActions.Add(ra);
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void UpdateMovementPlan(Network.Platform thePlatform, TimedLocation theLocation, bool routeToNewPlatform = true, bool rerouteBackToPlan = false)
        {
            try
            {
                //TODO need to update reservations
                if (routeToNewPlatform)
                {
                    theLocation.MyMovementPlan.ToNameAlternate = thePlatform.MyPlatformAlternate.MyName;
                    theLocation.MyMovementPlan.MyRouteActionsAlternate.Clear();
                    foreach (var ra in thePlatform.MyPlatformAlternate.MyRouteActions)
                    {
                        theLocation.MyMovementPlan.MyRouteActionsAlternate.Add(ra);
                    }

                    theLocation.MyMovementPlan.UseAlternatePathToNewPlatform = true;
                }

                if (rerouteBackToPlan)
                {
                    theLocation.MyMovementPlan.FromNameAlternate = thePlatform.MyPlatformAlternate.MyName;
                    theLocation.MyMovementPlan.MyRouteActionsAlternate.Clear();
                    foreach (var ra in thePlatform.MyPlatformAlternate.MyRouteActionsToOriginalRoute)
                    {
                        theLocation.MyMovementPlan.MyRouteActionsAlternate.Add(ra);
                    }
                    theLocation.MyMovementPlan.UseAlternatePathToReRouteToPlan = true;
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void CheckForAlternatePlatformPath(RoutePlanInfo theRoutePlanInfo, TimedLocation theLocation, TimedLocation nextLocation)
        {
            try
            {
                //Check to see if next platform is available, if not use the alternate platform if it exists
                //Auto-routing code added for alternate platform
                //TODO MBB
                var toPlatform = MyRailwayNetworkManager?.FindPlatform(theLocation.MyMovementPlan.ToName);
                if (toPlatform != null)
                {
                    if (!IsPlatformAvailable(toPlatform))
                    {
                        UpdateMovementPlan(toPlatform, theLocation);
                        UpdateMovementPlan(toPlatform, nextLocation, false, true);
                        var message = "Platform <" + toPlatform.MyName + "> is NOT available for Trip <" + theRoutePlanInfo.MyTrip.TripCode + "> Using Alternate Platform <" + toPlatform.MyPlatformAlternate?.MyName + ">";
                        CreateEvent(message, "ALTERNATE PLATFORM BEING USED", AlertLevel.WARN, true);
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        public bool CheckForActionLocationMatch(string theActionLocation, TrainPosition thePosition, TimedLocation theTimedLocation, Trip theTrip)
        {
            bool onTriggerPoint = false;
            var element = MyRailGraphManager?.GetILElementFromOffset(thePosition.ElementExtension?.EndPos.ElementId, thePosition.ElementExtension.EndPos.Offset);
            if (element != null)
            {
                onTriggerPoint = CheckTrainOnTriggerPoint(theTrip, theActionLocation, theTimedLocation, element);
            }
            if (!onTriggerPoint) onTriggerPoint = CheckTrainOnTriggerPoint(theTrip, theTrip.MyTrainPosition, theActionLocation);
            if (onTriggerPoint)
            {
                var message = "Trip On Trigger Point <" + theTrip.TripCode + "> <" + theActionLocation + ">";
                CreateEvent(message, "TRAIN ON TRIGGER POINT", AlertLevel.INFORMATION, true);
                MyLogger.LogInfo("CheckForActionLocationMatch1:Trip <" + theTrip.TripCode + "> IS ON TRIGGER POINT @ <" + theActionLocation + ">");
                return true;
            }

            if (theActionLocation == thePosition.ElementExtension?.StartPos.ElementId) return true;
            if (theTimedLocation.Position.ElementId == thePosition.ElementExtension?.StartPos.ElementId) return true;
            if (thePosition.MyCurrentTrackNameList != null)
            {
                foreach (var trackName in thePosition.MyCurrentTrackNameList)
                {
                    if (theActionLocation == trackName)
                    {
                        MyLogger.LogInfo("CheckForActionLocationMatch:Trip <" + theTrip.TripCode + "> IS ON TRIGGER POINT @ <" + theActionLocation + ">");
                        return true;
                    }
                    if (theTimedLocation.Position.ElementId == trackName) return true;
                }
            }

            if (theTrip.MyTrainPositions != null)
            {
                foreach (var pos in theTrip.MyTrainPositions)
                {
                    if (pos.ElementExtension?.StartPos.AdditionalName == theActionLocation ||
                        pos.ElementExtension?.StartPos.ElementId == theActionLocation)
                    {
                        MyLogger.LogInfo("CheckForActionLocationMatch:Trip <" + theTrip.TripCode + "> IS PAST TRIGGER POINT @ <" + thePosition.ElementExtension?.StartPos.ElementId + ">");
                        return true;
                    }
                }

            }
            return false;
        }
        private RoutePlanInfo CreateRoutePlanInformation(Messages.ConflictManagementMessages.SendRoutePlanRequest theRequest)
        {
            try
            {
                var trip = GetTrip(theRequest.TripUid, theRequest.StartTime);
                if (trip == null) return null;
                var timedLocation = GetTimedLocation(theRequest.TimedLocationGuid, trip);
                if (timedLocation == null) return null;
                var trainUid = trip.MyTrainPosition?.Train.Obid;//theTrip.CtcUid;
                var schedulePlanUid = Convert.ToInt32(trip.ScheduledPlanId);//theTrip.SerUid;
                var tripUid = trip.TripId;//trip.TripId;
                var departingPlatform = MyRailwayNetworkManager!.FindPlatform(timedLocation.MyMovementPlan.FromName);
                var theRoute = RoutePlanInfo.CreateInstance(schedulePlanUid, tripUid, trainUid, trip.StartPosition, trip.EndPosition, timedLocation, trip, departingPlatform);
                return theRoute;
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
            return null;
        }
        private RoutePlanInfo CreateRoutePlanInformation(Trip theTrip, TimedLocation theTimeLocation)
        {
            try
            {
                var trip = theTrip;
                if (trip == null) return null;
                var timedLocation = theTimeLocation;
                if (timedLocation == null) return null;
                var trainUid = trip.MyTrainPosition?.Train.Obid;//theTrip.CtcUid;
                var schedulePlanUid = Convert.ToInt32(trip.ScheduledPlanId);//theTrip.SerUid;
                var tripUid = trip.TripId;//trip.TripId;
                var departingPlatform = MyRailwayNetworkManager!.FindPlatform(timedLocation.MyMovementPlan.FromName);
                var theRoute = RoutePlanInfo.CreateInstance(schedulePlanUid, tripUid, trainUid, trip.StartPosition, trip.EndPosition, timedLocation, trip, departingPlatform);
                return theRoute;
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
            return null;
        }
        public bool ExecuteAutoRoutePlan(Trip theTrip, TimedLocation theLocation, bool performRouteMarking = true)
        {
            try
            {
                var timedLocation = GetTimedLocation(theLocation.SystemGuid, theTrip);
                if (timedLocation == null) return false;
                var trainUid = theTrip.MyTrainPosition?.Train.Obid;//theTrip.CtcUid;
                var schedulePlanUid = Convert.ToInt32(theTrip.ScheduledPlanId);//theTrip.SerUid;
                var tripUid = theTrip.TripId;//trip.TripId;
                var departingPlatform = MyRailwayNetworkManager!.FindPlatform(theLocation.MyMovementPlan.FromName);
                var fromPlatform = theLocation.MyMovementPlan.FromName;
                var toPlatform = theLocation.MyMovementPlan.ToName;
                var theRoute = RoutePlanInfo.CreateInstance(schedulePlanUid, tripUid, trainUid, theTrip.StartPosition, theTrip.EndPosition, theLocation, theTrip, departingPlatform);
                var index = GetTimeLocationIndex(theLocation, theTrip);
                var nextLocation = GetTimedLocation(index + 1, theTrip);

                //Check to see if next platform is available, if not use the alternate platform if it exists
                //Auto-routing code added for alternate platform
                if (MyEnableAutomaticConflictResolution) CheckForAlternatePlatformPath(theRoute, theLocation, nextLocation);

                theTrip.CompleteRoutePlanSent = false;
                var routeHasBeenExecuted = RequestExecuteRoute(theRoute).Result;
                if (routeHasBeenExecuted)
                {
                    if (theRoute.UseAlternatePath)
                    {
                        if (theRoute.MyPlatformAlternate != null)
                        {
                            if (theRoute.MyPlatformAlternate.UseAlternateToPaths)
                            {
                                toPlatform = theRoute.MyPlatformAlternate.MyName;
                            }
                            else if (theRoute.MyPlatformAlternate.UseAlternateFromPaths)
                            {
                                fromPlatform = theRoute.MyPlatformAlternate.MyName;
                            }
                            var message1 = "Alternate platform Used <" + fromPlatform + "> to platform <" + toPlatform + " for Trip <" + theTrip.TripCode + ">";
                            CreateEvent(message1, "ALTERNATE PLATFORM USED", AlertLevel.WARN, true);
                        }
                    }
                    var message = "Route has been sent to ROS for execution from platform <" + fromPlatform + "> to platform <" + toPlatform + " for Trip <" + theTrip.TripCode + ">";
                    CreateEvent(message, "ROUTE EXECUTION COMPLETE", AlertLevel.INFORMATION, true);
                    //theLocation.RouteIsExecutedForTrip = true;

                    UpdateRouteActions(theLocation);

                    //Route Marking
                    //var index = GetTimeLocationIndex(theLocation, theTrip);
                    //var nextLocation = GetTimedLocation(index + 1, theTrip);
                    if (nextLocation != null && performRouteMarking)
                    {
                        var thread = new Thread(() => ExecuteRouteMarkerUpdate(theTrip, theLocation, nextLocation)); thread.Start();
                    }
                    //Route Marking
                    return true;
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
            return false;
        }
        private void UpdateRouteActions(TimedLocation theLocation)
        {
            try
            {
                if (theLocation.MyMovementPlan == null) return;
                foreach (var ra in theLocation.MyMovementPlan.MyRouteActions)
                {
                    ra.HasBeenSentToRos = true;
                    ra.IsTimeRouteSentToRos = DateTime.Now;
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }
        public bool ExecuteRoutePlan(ConflictManagementLibrary.Messages.ConflictManagementMessages.SendRoutePlanRequest theRequest)
        {
            try
            {
                var fromPlatform = theRequest.PlatformFrom;
                var toPlatform = theRequest.PlatformTo;
                var theRoute = CreateRoutePlanInformation(theRequest);
                var routeHasBeenExecuted = RequestExecuteRoute(theRoute).Result;
                if (routeHasBeenExecuted)
                {
                    MyLogger.LogInfo("Route has been sent to ROS for execution from platform <" + fromPlatform + "> to platform <" + toPlatform + ". for Trip <" + theRoute.MyTrip.TripCode + ">");
                    theRoute.CurrentTimedLocation.RouteIsExecutedForTrip = true;
                    return true;
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
            return true;
        }
        private List<Trip> CheckToUpdateConflicts(List<Reservation> theReleasedReservations)
        {
            var tripsToUpdate = new List<Trip>();
            try
            {
                foreach (var r in theReleasedReservations)
                {
                    foreach (var trip in TripList)
                    {
                        foreach (var c in trip.MyConflicts)
                        {
                            if (c.ConflictingReservation != r) continue;
                            trip.MyConflicts.Remove(c);
                            MyLogger.LogInfo("CheckToUpdateConflicts:Trip <" + trip.TripCode + "> Conflict to Release <" + c.MyDescription + ">");

                            if (!tripsToUpdate.Contains(trip))
                            {
                                tripsToUpdate.Add(trip);
                                MyLogger.LogInfo("CheckToUpdateConflicts:Trip To Send Update From Conflict Removal <" + trip.TripCode + ">");
                            }
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }

            return tripsToUpdate;
        }
        private List<Reservation> CheckToReleaseReservation(Trip theTrip, TrainPosition thePosition)
        {
            var reservationReleaseList = new List<Reservation>();
            try
            {
                if (theTrip.LastTimedLocation == null) return reservationReleaseList;
                if (theTrip.LastTimedLocation == theTrip.TimedLocations[0])
                {
                    MyLogger.LogDebug("CheckToReleaseReservation:Trip UID <" + theTrip.TripCode + "> On Begin Trip Platform <" + theTrip.LastTimedLocation.Description + ">");
                    return reservationReleaseList;
                }
                ////var locationIndex = 0;
                //loop through the timed-locations and see if tbe position is past the first platform
                //and positioned on the second platform track and if so capture the list index of this platform
                //so as to look behind the train to release reservations.
                foreach (var tl in theTrip.TimedLocations)
                {
                    if (tl == theTrip.LastTimedLocation)
                    {
                        MyLogger.LogInfo("CheckToReleaseReservation:Trip UID <" + theTrip.TripCode + "> Current Trip Platform <" + theTrip.LastTimedLocation.Description + ">");
                        //return reservationReleaseList;
                    }
                    reservationReleaseList.AddRange(theTrip.MyReservations);
                }

                //if not the first platform in the timed-locations, loop again through the time-locations
                //and find all the reservations behind the current platform that the train is occupying.
                //add these reservations to the list to be released.
                //
                //if reservations found to be released,
                //then set variable to return true so a conflict check can be performed
                ////if (locationIndex <= 0) return reservationReleaseList;
                ////{
                ////    foreach (var tl in theTrip.TimedLocations)
                ////    {
                ////        var thisLocationIndex = theTrip.TimedLocations.IndexOf(tl);
                ////        if (thisLocationIndex < locationIndex)
                ////        {
                ////            foreach (var r in theTrip.MyReservations)
                ////            {
                ////                if (r.MyTimedLocation == tl)
                ////                {
                ////                    reservationReleaseList.Add(r);
                ////                }
                ////            }
                ////        }
                ////    }
                ////}

                //remove the reservations from the trip
                foreach (var rl in reservationReleaseList)
                {
                    foreach (var tl in theTrip.TimedLocations)
                    {
                        foreach (var r in theTrip.MyReservations)
                        {
                            if (rl != r) continue;
                            MyLogger.LogInfo("CheckToReleaseReservation:Trip UID <" + theTrip.TripCode + "> Release Reservation Edge Name <" + r.MyEdgeName + ">");
                            //theTrip.MyReservations.Remove(r);
                            //r.HasBeenReleased = true;
                            break;
                        }
                    }

                }

                var lastLocationIndex = theTrip.TimedLocations.Count - 1;
                if (theTrip.LastTimedLocation == theTrip.TimedLocations[lastLocationIndex])
                {
                    reservationReleaseList.AddRange(theTrip.MyReservations);
                    theTrip.MyReservations.Clear();
                    theTrip.IsCompleted = true;
                    MyLogger.LogInfo("CheckToReleaseReservation:Trip UID Is Completed <" + theTrip.TripCode + ">");

                }
                return reservationReleaseList;

                //need to check for the last platform and release reservations and mark the trip to be removed
                ////var lastLocationIndex = theTrip.TimedLocations.Count - 1;
                ////if (locationIndex == lastLocationIndex)
                ////{
                ////    foreach (var r in theTrip.MyReservations)
                ////    {
                ////        reservationReleaseList.Add(r);
                ////    }
                ////    theTrip.MyReservations.Clear();
                ////    theTrip.IsCompleted = true;
                ////}

                ////return reservationReleaseList;
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
            return reservationReleaseList;
        }
        private List<Reservation> CheckToReleaseReservationNew(Trip theTrip, TrainPosition thePosition)
        {
            var reservationReleaseList = new List<Reservation>();
            try
            {
                if (thePosition == null) return reservationReleaseList;
                var edgeFound = false;
                var edgeName = thePosition.ElementExtension.EndPos.ElementId;
                foreach (var r in theTrip.MyReservations)
                {
                    if (r.MyEdgeName == edgeName)
                    {
                        edgeFound = true;
                        break;
                    }
                    reservationReleaseList.Add(r);
                }
                if (!edgeFound) return reservationReleaseList = new List<Reservation>();

                foreach (var rl in reservationReleaseList)
                {
                    rl.HasBeenReleased = true;
                }
                return reservationReleaseList;

                //if (theTrip.LastTimedLocation == null) return reservationReleaseList;
                //if (theTrip.LastTimedLocation == theTrip.TimedLocations[0])
                //{
                //    MyLogger.LogDebug("CheckToReleaseReservation:MockTrip UID <" + theTrip.TripCode + "> On Begin MockTrip Platform <" + theTrip.LastTimedLocation.Description + ">");
                //    return reservationReleaseList;
                //}
                //////var locationIndex = 0;
                ////loop through the timed-locations and see if tbe position is past the first platform
                ////and positioned on the second platform track and if so capture the list index of this platform
                ////so as to look behind the train to release reservations.
                //foreach (var tl in theTrip.TimedLocations)
                //{
                //    if (tl == theTrip.LastTimedLocation)
                //    {
                //        MyLogger.LogInfo("CheckToReleaseReservation:MockTrip UID <" + theTrip.TripCode + "> Current MockTrip Platform <" + theTrip.LastTimedLocation.Description + ">");
                //        //return reservationReleaseList;
                //    }
                //    reservationReleaseList.AddRange(theTrip.MyReservations);
                //}

                ////if not the first platform in the timed-locations, loop again through the time-locations
                ////and find all the reservations behind the current platform that the train is occupying.
                ////add these reservations to the list to be released.
                ////
                ////if reservations found to be released,
                ////then set variable to return true so a conflict check can be performed
                //////if (locationIndex <= 0) return reservationReleaseList;
                //////{
                //////    foreach (var tl in theTrip.TimedLocations)
                //////    {
                //////        var thisLocationIndex = theTrip.TimedLocations.IndexOf(tl);
                //////        if (thisLocationIndex < locationIndex)
                //////        {
                //////            foreach (var r in theTrip.MyReservations)
                //////            {
                //////                if (r.MyTimedLocation == tl)
                //////                {
                //////                    reservationReleaseList.Add(r);
                //////                }
                //////            }
                //////        }
                //////    }
                //////}

                ////remove the reservations from the trip
                //foreach (var rl in reservationReleaseList)
                //{
                //    foreach (var tl in theTrip.TimedLocations)
                //    {
                //        foreach (var r in theTrip.MyReservations)
                //        {
                //            if (rl != r) continue;
                //            MyLogger.LogInfo("CheckToReleaseReservation:MockTrip UID <" + theTrip.TripCode + "> Release Reservation Edge Name <" + r.MyEdgeName + ">");
                //            //theTrip.MyReservations.Remove(r);
                //            //r.HasBeenReleased = true;
                //            break;
                //        }
                //    }

                //}

                //var lastLocationIndex = theTrip.TimedLocations.Count - 1;
                //if (theTrip.LastTimedLocation == theTrip.TimedLocations[lastLocationIndex])
                //{
                //    reservationReleaseList.AddRange(theTrip.MyReservations);
                //    theTrip.MyReservations.Clear();
                //    theTrip.IsCompleted = true;
                //    MyLogger.LogInfo("CheckToReleaseReservation:MockTrip UID Is Completed <" + theTrip.TripCode + ">");

                //}
                //return reservationReleaseList;

                //need to check for the last platform and release reservations and mark the trip to be removed
                ////var lastLocationIndex = theTrip.TimedLocations.Count - 1;
                ////if (locationIndex == lastLocationIndex)
                ////{
                ////    foreach (var r in theTrip.MyReservations)
                ////    {
                ////        reservationReleaseList.Add(r);
                ////    }
                ////    theTrip.MyReservations.Clear();
                ////    theTrip.IsCompleted = true;
                ////}

                ////return reservationReleaseList;
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
            return reservationReleaseList;
        }
        public void SendCompleteRoutePlanForAllocatedTrain()
        {
            try
            {
                TimedLocation nextLocation = null;

                lock (MyTripsAllocated)
                {
                    foreach (var theTrip in MyTripsAllocated)
                    {
                        try
                        {
                            foreach (var tl in theTrip.TimedLocations)
                            {
                                if (theTrip.LastTimedLocation == null)
                                {
                                    nextLocation = theTrip.TimedLocations[0];
                                    break;
                                }
                                if (tl.Description == theTrip.LastTimedLocation.Description)
                                {
                                    var index = theTrip.TimedLocations.IndexOf(tl);
                                    if (theTrip.TimedLocations.Count-1 >= index + 1) nextLocation = theTrip.TimedLocations[index + 1];
                                    break;
                                }
                            }
                            ExecuteAutoRoutePlan(theTrip, nextLocation!);
                        }
                        catch (Exception e)
                        {
                            MyLogger.LogException(e.ToString());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void DoTripUpdates(List<Trip> theTripsToUpdate)
        {
            try
            {
                foreach (var trip in theTripsToUpdate)
                {
                    MyLogger.LogInfo("DoTripUpdates:Trip UID <" + trip.TripCode + ">");
                    PerformForecastUpdate(trip);
                    //GlobalDeclarations.MyTrainSchedulerManager?.ProduceMessage2003(trip);
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void AddPositionToTrip(Trip theTrip, TrainPosition thePosition)
        {
            try
            {
                if (theTrip.AddPosition(thePosition))
                {
                    var element = MyRailGraphManager?.GetILElementFromOffset(thePosition.ElementExtension?.EndPos.ElementId, thePosition.ElementExtension.EndPos.Offset);
                    if (element != null)
                    {
                        var triggerPoint = GetTriggerPointFromElement(theTrip, element.getName());
                        if (triggerPoint != null)
                        {
                            var message1 = "Trip <" + theTrip.TripCode + "> On Trigger Point <" + element.getName() + "> for Route <" + triggerPoint.MyRouteName + ">";
                            CreateEvent(message1, "TRIP ON TRIGGER POINT", AlertLevel.WARN, true);
                            theTrip.LastPositionUpdate = DateTime.Now;
                            return;
                        }
                        var message2 = "Train Position Has Changed For <" + theTrip.TripCode + "> To Element <" + element.getName() + ">";
                        CreateEvent(message2, "TRAIN POSITION CHANGED", AlertLevel.INFORMATION, true);
                        theTrip.LastPositionUpdate = DateTime.Now;
                        return;
                    }
                    var startPosition = thePosition?.ElementExtension?.StartPos.ElementId;
                    var endPosition = thePosition?.ElementExtension?.EndPos.ElementId;

                    var message = "Train Position Has Changed For <" + theTrip.TripCode + "> To Element <" + startPosition + "><" + endPosition +">";
                    CreateEvent(message, "TRAIN POSITION CHANGED", AlertLevel.INFORMATION, true);
                    theTrip.LastPositionUpdate = DateTime.Now;


                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private TriggerPoint GetTriggerPointFromElement(Trip theTrip, string elementName)
        {
            foreach (var tp in theTrip.MyTriggerPoints)
            {
                if (tp.TheTriggerName == elementName) return tp;
            }

            return null;
        }
        public void ReleaseReservationsConflictsFromDeletedTrip(Trip theTrip)
        {
            try
            {
                var reservationReleaseList = new List<Reservation>();
                var tripsToUpdateFromReleasedConflicts = new List<Trip>();

                reservationReleaseList.AddRange(theTrip.MyReservations);
                if (reservationReleaseList.Count > 0)
                {
                    MyLogger.LogInfo("ReleaseReservationsConflictsFromDeletedTrip: Number of Reservations To Clean Up from Trip <" + theTrip.TripCode +"><" + reservationReleaseList.Count +">");
                    tripsToUpdateFromReleasedConflicts = CheckToUpdateConflicts(reservationReleaseList);
                }
                if (tripsToUpdateFromReleasedConflicts is { Count: > 0 })
                {
                    DoTripUpdates(tripsToUpdateFromReleasedConflicts);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        #endregion

        #region General Functions
        private Trip GetTrip(string tripUid, string startTime)
        {
            lock (TripList)
            {
                foreach (var trip in TripList)
                {
                    if (trip.TripId.ToString() == tripUid && trip.StartTime == startTime) return trip;
                }
            }

            return null!;
        }
        private TimedLocation GetTimedLocation(string timedLocationGuid, Trip theTrip)
        {
            foreach (var tl in theTrip.TimedLocations)
            {
                if (tl.SystemGuid == timedLocationGuid) return tl;
            }

            return null!;
        }
        private TimedLocation GetTimedLocation(int index, Trip theTrip)
        {
            try
            {
                if (theTrip.TimedLocations.Count < index + 1) return null!;
                var timeLocation = theTrip.TimedLocations[index];
                return timeLocation;
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
            return null!;
        }
        private int GetTimeLocationIndex(TimedLocation theLocation, Trip theTrip)
        {
            try
            {
                var index = theTrip.TimedLocations.IndexOf(theLocation);
                return index;
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
            return 0;
        }
        private bool IsTrainPositionChange(Trip theTrip, TrainPosition thePosition)
        {
            try
            {
                if (theTrip?.MyTrainPosition == null)
                {
                    theTrip!.MyTrainPosition = thePosition;
                    return true;
                }
                var newPositionElementId = thePosition.ElementExtension?.StartPos.ElementId;
                var oldPositionElementId = theTrip.MyTrainPosition.ElementExtension?.StartPos.ElementId;
                var newPositionElementOffset = thePosition.ElementExtension?.StartPos.Offset;
                var oldPositionElementOffset = theTrip.MyTrainPosition.ElementExtension?.StartPos.Offset;

                if (newPositionElementId == oldPositionElementId && newPositionElementOffset == oldPositionElementOffset) return false;
                theTrip.MyTrainPosition = thePosition!;
                return true;
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }

            return false;
        }
        private bool IsPlatformAvailable(Network.Platform thePlatform)
        {
            try
            {
                var myTrack = thePlatform.MyTrackNetwork;
                //var trackName = myTrack.MyName;
                uint trackUid = 0;
                if (myTrack != null)
                {
                    trackUid = myTrack.MyUid;
                }
                else
                {
                    trackUid = thePlatform.MyTrackUId;
                }
                //var theTrack = MyRailGraphManager?.ILTopoGraph?.getGraphObj(myTrack.MyUid);

                var track = MyRailGraphManager?.ILTopoGraph?.getGraphObj(trackUid);
                //var interlockingTrack = MyRailGraphManager?.ILGraph?.getILGraphObj(theTrack!.getId());
                var blocked = ((track as RailgraphLib.Interlocking.Track)!).isBlocked();
                if (blocked)
                {
                    MyLogger.LogInfo("Platform <" + thePlatform.MyName + "> is not available account it is blocked");
                    return false;
                }

                var occupiedFalse = ((RailgraphLib.Interlocking.Track)track).isTrackFalseOccupied();
                if (occupiedFalse)
                {
                    MyLogger.LogInfo("Platform <" + thePlatform.MyName + "> is not available account it is falsely occupied");
                    return false;
                }
                var occupied = ((RailgraphLib.Interlocking.Track)track).getOccupationState();
                if (occupied == EOccupation.occupationOn)
                {
                    MyLogger.LogInfo("Platform <" + thePlatform.MyName + "> is not available account it is occupied");
                    return false;
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }

            return true;
        }
        private Trip GetAllocatedTrip(string tripUid, string startTime)
        {
            lock (MyTripsAllocated)
            {
                foreach (var trip in MyTripsAllocated)
                {
                    if (trip.TripId.ToString() == tripUid && trip.StartTime == startTime) return trip;
                }
            }

            return null!;
        }
        private bool IsTrainPositionTimePast(Trip theTrip)
        {
            try
            {
                if (theTrip.LastPositionUpdate < DateTime.Now.AddSeconds(-30)) return true;
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }

            return false;
        }
        #endregion

        #region ROS Pretest
        public void AddPretestResult(int index, bool success, string info)
        {
            var result = PreTestResult.CreateInstance(index, success, info);
            MyPreTestResults.Add(result);
        }
        private bool GetRouteAvailabilityResult(int resultIndex, RouteAction theRoute, Trip theTrip)
        {
            try
            {
                //if (!MyEnableAutomaticConflictResolution) return false;
                var waitIndex = 0;
                while (true)
                {
                    foreach (var result in MyPreTestResults)
                    {
                        if (result.PretestId == resultIndex)
                        {
                            if (result.Success == true)
                            {
                                MyLogger.LogInfo(theRoute.RouteName + " is available for Trip <" + theTrip.TripCode + ">");
                                var theMessage = theRoute.RouteName + " ROUTE IS AVAILABLE for Trip <" + theTrip.TripCode + ">";
                                CreateEvent(theMessage, "ROUTE IS AVAILABLE", AlertLevel.INFORMATION, true);
                                return true;
                            }
                            else
                            {
                                var theMessage = theRoute.RouteName + " ROUTE IS NOT AVAILABLE for Trip <" + theTrip.TripCode + ">";
                                CreateEvent(theMessage, "ROUTE IS NOT AVAILABLE", AlertLevel.WARN, true);
                                return false;
                            }
                        }
                    }
                    waitIndex++;
                    Thread.Sleep(100);
                    if (waitIndex == 20) return false;
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }

            return false;
        }
        public class PreTestResult
        {
            public int PretestId;
            public bool Success;
            public string ResultInformation;
            private PreTestResult(int pretestId, bool success, string resultInformation)
            {
                PretestId = pretestId;
                Success = success;
                ResultInformation = resultInformation;
            }
            internal static PreTestResult CreateInstance(int pretestId, bool success, string resultInformation)
            {
                return new PreTestResult(pretestId, success, resultInformation);
            }
        }

        #endregion
      
        #region Route Marking
        private void DoUpdateRouteMarkingForRouteExecuted(string trainSysId, string trainObid, string routeName)
        {
            try
            {
                var trip = FindTripFromTrainSystemId(trainSysId);
                if (trip == null)
                {
                    trip = FindTripFromTrainObid(trainObid);
                }

                if (trip == null)
                {
                    MyLogger.LogInfo("DoUpdateRouteMarkingForRouteExecuted:Trip NOT FOUND FOR TRAIN SYSTEM ID" + trainSysId + "><" + routeName + ">");
                    return;
                }
                var index = GetTimeLocationIndex(trip.LastTimedLocation, trip);
                var nextLocation = GetTimedLocation(index + 1, trip);

                if (trip?.LastTimedLocation != null)
                {
                    var routeAction = trip.LastTimedLocation.MyMovementPlan.MyRouteActions[0];
                    if (routeAction != null)
                    {
                        if (routeAction.RouteName == routeName)
                        {
                            trip.LastTimedLocation.RouteIsExecutedForTrip = true;
                            var message = "Route Plan Was Confirmed From ROS as Execution Complete for Trip <" + trip.TripCode + "><" + routeName + ">";
                            CreateEvent(message, "ROS ROUTE EXECUTION COMPLETE", AlertLevel.INFORMATION, true);
                            MyLogger.LogInfo("DoUpdateRouteMarkingForRouteExecuted:Route Found at Timed Location <" + trip.LastTimedLocation.Description + "><" + routeName + ">");
                        }
                    }
                    ExecuteRouteMarkerUpdate(trip, trip.LastTimedLocation, nextLocation);
                }
                else
                {
                    MyLogger.LogInfo("DoUpdateRouteMarkingForRouteExecuted:Trip Location NOT FOUND FOR TRAIN " + trainSysId + "><" + trainObid + "><" + routeName + ">");
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void DoCheckForRouteMarkingUpdatesForAllocatedTrains()
        {
            try
            {
                Thread.Sleep(30000);
                while (true)
                {
                    try
                    {
                        List<Trip> tempTripList;
                        lock (MyTripsAllocated)
                        {
                            //Create temp list of allocated trains
                            tempTripList = new List<Trip>(MyTripsAllocated);
                        }
                        foreach (var trip in tempTripList)
                        {
                            foreach (var sp in trip.MySignalPoints)
                            {
                                sp.UpdateClearedStatus();
                            }
                            if (trip.MyRouteMarkingCurrent == null) continue;
                            foreach (var rm in trip.MyRouteMarkingCurrent)
                            {
                                var signalNames = rm.RouteName?.Split("-");
                                if (signalNames != null && signalNames.Length > 0)
                                {
                                    var theSignalName = signalNames[0];
                                    var sp = GetSignalPoint(trip, theSignalName);
                                    if (sp != null)
                                    {
                                        if (!sp.IsClear()) break;
                                        trip.MyRouteMarkingCurrent.Remove(rm);
                                        ReSendRouteMarkings(trip);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        MyLogger.LogException(e.ToString());
                    }
                    Thread.Sleep(5000);
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private SignalPoint GetSignalPoint(Trip theTrip, string theSignalName)
        {
            try
            {
                foreach (var sp in theTrip.MySignalPoints)
                {
                    if (sp.TheSignalName == theSignalName) return sp;
                }
                var signal = MyRailGraphManager?.ILGraph?.getGraphObj(theSignalName);
                if (signal == null || signal is not SignalOptical) return null;

                var theSignalPoint = SignalPoint.CreateInstance(theSignalName, signal);
                theTrip.MySignalPoints.Add(theSignalPoint);
                MyLogger?.LogInfo("CreateSignalPoints:Signal Point Created <" + theSignalName + "> for Trip <" + theTrip.TripCode + ">");

            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }

            return null;
        }
        public bool ExecuteRouteMarkerUpdate(Trip theTrip, TimedLocation firstTimedLocation, TimedLocation nextTimedLocation)
        {
            try
            {
                //Thread.Sleep(2000);
                //DoRouteMarkingClear(theTrip);

                //if (nextTimedLocation.MyMovementPlan == null) return false;
                var trainObid = theTrip.MyTrainObid;
                var actionTime = DateTime.UtcNow;
                List<Tuple<DateTime, string?>> markings = new();
                var routeMarkingList = GetRouteMarkingList(theTrip, firstTimedLocation, nextTimedLocation);
                if (routeMarkingList.Count == 0)
                {
                    var index = GetTimeLocationIndex(theTrip.LastTimedLocation, theTrip);
                    var nextLocation = GetTimedLocation(index + 2, theTrip);
                    if (nextLocation != null)
                    {
                        routeMarkingList = GetRouteMarkingList(theTrip, nextTimedLocation, nextLocation);
                    }
                }

                var routes = new StringBuilder();
                foreach (var markInfo in routeMarkingList)
                {
                    markings.Add(new(markInfo.RouteActionTime, markInfo.RouteName));
                    routes.Append(" <" + markInfo.RouteName + "><" + markInfo.RouteActionTime.ToLocalTime() + "> ");
                }

                var routeMarkingResult = RequestRouteMarkingUpdate(trainObid, markings).Result;

                if (routeMarkingResult)
                {
                    var message = "Route Marking Sent For Trip <" + theTrip.TripCode + "> For These Routes/Times <" + routes + ">";
                    CreateEvent(message, "ROUTE MARKING SENT", AlertLevel.INFORMATION, true);
                }
                else
                {
                    var message = "Route Marking NOT Sent For Trip <" + theTrip.TripCode + ">";
                    CreateEvent(message, "ROUTE MARKING NOT SENT", AlertLevel.WARN, true);
                }
                SerializeRouteMarking(routeMarkingList, theTrip);
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
            return false;
        }
        private void ReSendRouteMarkings(Trip theTrip)
        {
            try
            {
                var trainObid = theTrip.MyTrainObid;
                List<Tuple<DateTime, string?>> markings = new();
                var routes = new StringBuilder();

                foreach (var markInfo in theTrip.MyRouteMarkingCurrent)
                {
                    markings.Add(new(markInfo.RouteActionTime, markInfo.RouteName));
                    routes.Append(" <" + markInfo.RouteName + "><" + markInfo.RouteActionTime.ToLocalTime() + "> ");
                }
                var routeMarkingResult = RequestRouteMarkingUpdate(trainObid, markings).Result;

                if (routeMarkingResult)
                {
                    var message = "Route Marking Sent For Trip <" + theTrip.TripCode + "> For These Routes/Times <" + routes + ">";
                    CreateEvent(message, "ROUTE MARKING SENT", AlertLevel.INFORMATION, true);
                }
                else
                {
                    var message = "Route Marking NOT Sent For Trip <" + theTrip.TripCode + ">";
                    CreateEvent(message, "ROUTE MARKING NOT SENT", AlertLevel.WARN, true);
                }
                SerializeRouteMarking(theTrip.MyRouteMarkingCurrent, theTrip);

            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void DoRouteMarkingClear(Trip theTrip)
        {
            try
            {
                var trainObid = theTrip.MyTrainObid;
                List<Tuple<DateTime, string?>> markings = new();
                var routeMarkingResult = RequestRouteMarkingUpdate(trainObid, markings).Result;

            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private List<RouteMarkInfo> GetRouteMarkingList(Trip theTrip, TimedLocation firstLocation, TimedLocation nextLocation)
        {
            var myRouteMarkInfoList = new List<RouteMarkInfo>();
            var accumulativeDistance = 0;
            try
            {
                var routeActionList = new List<RouteAction>();
                RouteAction firstRouteCleared = null;
                if (firstLocation.MyMovementPlan != null && (firstLocation.MyMovementPlan.UseAlternatePathToNewPlatform || firstLocation.MyMovementPlan.UseAlternatePathToReRouteToPlan))
                {
                    foreach (var ra in firstLocation.MyMovementPlan.MyRouteActionsAlternate)
                    {
                        if (!IsSignalClear(ra.RouteName) && !ra.HasSignalBeenCleared && !ra.HasConfirmationRouteClearedFromRos) if (!DoesRouteExistInList(ra, routeActionList)) routeActionList.Add(ra);
                        if (IsSignalClear(ra.RouteName)) ra.HasSignalBeenCleared = true;
                    }
                }
                else
                {
                    foreach (var ra in firstLocation.MyMovementPlan.MyRouteActions)
                    {
                        if (!IsSignalClear(ra.RouteName) && !ra.HasSignalBeenCleared && !ra.HasConfirmationRouteClearedFromRos) if (!DoesRouteExistInList(ra,routeActionList)) routeActionList.Add(ra);
                        if (IsSignalClear(ra.RouteName)) ra.HasSignalBeenCleared = true;
                    }
                }

                if (nextLocation != null && nextLocation.MyMovementPlan != null && nextLocation.MyMovementPlan.UseAlternatePathToReRouteToPlan)
                {
                    foreach (var ra in nextLocation.MyMovementPlan.MyRouteActionsAlternate)
                    {
                        if (!IsSignalClear(ra.RouteName) && !ra.HasSignalBeenCleared && !ra.HasConfirmationRouteClearedFromRos) if (!DoesRouteExistInList(ra, routeActionList)) routeActionList.Add(ra);
                        if (IsSignalClear(ra.RouteName)) ra.HasSignalBeenCleared = true;
                    }
                }
                else
                {
                    if (nextLocation != null && nextLocation.MyMovementPlan != null)
                    {
                        foreach (var ra in nextLocation.MyMovementPlan.MyRouteActions)
                        {
                            if (!IsSignalClear(ra.RouteName) && !ra.HasSignalBeenCleared && !ra.HasConfirmationRouteClearedFromRos) if (!DoesRouteExistInList(ra, routeActionList)) routeActionList.Add(ra);
                            if (IsSignalClear(ra.RouteName)) ra.HasSignalBeenCleared = true;
                        }
                    }
                }
                var index = 0;
                var nextTimeAction = DateTime.UtcNow;
                RouteMarkingProjection lastMarkingActionTime = null;
                foreach (var ra in routeActionList)
                {
                    if (index == routeActionList.Count - 1)
                    {
                        if (lastMarkingActionTime == null) break;
                        myRouteMarkInfoList.Add(RouteMarkInfo.CreateInstance(routeActionList[index].RouteName, lastMarkingActionTime.ProjectTimeToTravel));
                        break;
                    }
                    var markingActionTime = CalculateRouteMarkingTime(routeActionList[index].RouteName, routeActionList[index + 1].RouteName, theTrip, accumulativeDistance);
                    lastMarkingActionTime = markingActionTime;
                    accumulativeDistance += markingActionTime.DistanceTraveledToNextSignal;
                    //GetPreviousRouteMarkingTime(routeActionList[index].RouteName, theTrip, ref markingActionTime);

                    myRouteMarkInfoList.Add(RouteMarkInfo.CreateInstance(routeActionList[index].RouteName, markingActionTime.ProjectTimeToTravel));
                    //myRouteMarkInfoList.Add(RouteMarkInfo.CreateInstance(routeActionList[index + 1].RouteName, markingActionTime.ProjectTimeToTravel));
                    index += 1;
                }
                theTrip.MyRouteMarkingCurrent = myRouteMarkInfoList;
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }

            return myRouteMarkInfoList;
        }
        private bool DoesRouteExistInList(RouteAction theAction, List<RouteAction> theRouteActions)
        {
            try
            {
                foreach (var ra in theRouteActions)
                {
                    if (ra.RouteName == theAction.RouteName) return true;
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }

            return false;
        }
        private List<RouteMarkInfo> GetRouteMarkingListOld(Trip theTrip, TimedLocation firstLocation, TimedLocation nextLocation)
        {
            var myRouteMarkInfoList = new List<RouteMarkInfo>();
            var accumulativeDistance = 0;
            try
            {
                var routeActionList = new List<RouteAction>();
                RouteAction firstRouteCleared = null;
                if (firstLocation.MyRoutePlan != null && firstLocation.MyRoutePlan.UseAlternatePath)
                {
                    foreach (var ra in firstLocation.MyRoutePlan.MyPlatformAlternate.MyRouteActions)
                    {
                        if (!IsSignalClear(ra.RouteName) && !ra.HasSignalBeenCleared && !ra.HasConfirmationRouteClearedFromRos) routeActionList.Add(ra);
                        if (IsSignalClear(ra.RouteName)) ra.HasSignalBeenCleared = true;
                    }
                }
                else
                {
                    foreach (var ra in firstLocation.MyMovementPlan.MyRouteActions)
                    {
                        if (!IsSignalClear(ra.RouteName) && !ra.HasSignalBeenCleared && !ra.HasConfirmationRouteClearedFromRos) routeActionList.Add(ra);
                        if (IsSignalClear(ra.RouteName)) ra.HasSignalBeenCleared = true;
                    }
                }

                if (firstLocation.MyRoutePlan != null && firstLocation.MyRoutePlan.UseAlternatePath)
                {
                    foreach (var ra in firstLocation.MyRoutePlan.MyPlatformAlternate.MyRouteActionsToOriginalRoute)
                    {
                        if (!IsSignalClear(ra.RouteName) && !ra.HasSignalBeenCleared && !ra.HasConfirmationRouteClearedFromRos) routeActionList.Add(ra);
                        if (IsSignalClear(ra.RouteName)) ra.HasSignalBeenCleared = true;
                    }
                }
                else
                {
                    foreach (var ra in nextLocation.MyMovementPlan.MyRouteActions)
                    {
                        if (!IsSignalClear(ra.RouteName) && !ra.HasSignalBeenCleared && !ra.HasConfirmationRouteClearedFromRos) routeActionList.Add(ra);
                        if (IsSignalClear(ra.RouteName)) ra.HasSignalBeenCleared = true;
                    }
                }
                var index = 0;
                var nextTimeAction = DateTime.UtcNow;
                foreach (var ra in routeActionList)
                {
                    if (index == routeActionList.Count - 1) break;
                    var markingActionTime = CalculateRouteMarkingTime(routeActionList[index].RouteName, routeActionList[index + 1].RouteName, theTrip, accumulativeDistance);
                    accumulativeDistance += markingActionTime.DistanceTraveledToNextSignal;
                    //GetPreviousRouteMarkingTime(routeActionList[index].RouteName, theTrip, ref markingActionTime);

                    myRouteMarkInfoList.Add(RouteMarkInfo.CreateInstance(routeActionList[index].RouteName, markingActionTime.ProjectTimeToTravel));
                    //myRouteMarkInfoList.Add(RouteMarkInfo.CreateInstance(routeActionList[index + 1].RouteName, markingActionTime.ProjectTimeToTravel));
                    index += 1;
                }
                theTrip.MyRouteMarkingCurrent = myRouteMarkInfoList;
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }

            return myRouteMarkInfoList;
        }
        private void GetPreviousRouteMarkingTime(string? theRouteName, Trip theTrip, ref RouteMarkingProjection theProjection)
        {
            try
            {
                if (theTrip.MyRouteMarkingCurrent == null) return;
                foreach (var route in theTrip.MyRouteMarkingCurrent.Where(route => route.RouteName == theRouteName))
                {
                    theProjection.ProjectTimeToTravel = route.RouteActionTime;
                    break;
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private RouteMarkingProjection CalculateRouteMarkingTime(string? signalRoutePrevious, string? signalRouteNext, Trip theTrip, int accumulativeDistanceToNextSignal)
        {
            RouteMarkingProjection theProjection;
            try
            {
                var totalSecondsCalculated = 0;
                var sigFrom = signalRoutePrevious.Split("-");
                var sigTo = signalRouteNext.Split("-");
                var signalNameFrom = sigFrom[0];
                var signalNameTo = sigTo[0];

                var signalFrom = MyRailGraphManager?.ILGraph?.getGraphObj(signalNameFrom) as SignalOptical;
                var signalTo = MyRailGraphManager?.ILGraph?.getGraphObj(signalNameTo) as SignalOptical;

                var signalFromInitPoint = signalFrom?.getDistanceFromInitPoint();
                var signalToInitPoint = signalTo?.getDistanceFromInitPoint();
                int? distance = 0;
                if (signalFromInitPoint < signalToInitPoint)
                {
                    distance = (signalToInitPoint - signalFromInitPoint);
                }
                else
                {
                    distance = (signalFromInitPoint - signalToInitPoint);
                }
                MyLogger.LogInfo("CalculateRouteMarkingTime: Current UTC Time: " + DateTime.UtcNow);
                MyLogger.LogInfo("CalculateRouteMarkingTime: between signal <" + signalNameFrom + "> to signal <" + signalNameTo +">");
                distance /= 1000;
                distance += accumulativeDistanceToNextSignal;
                MyLogger.LogInfo("calculated distance: " + distance);

                DateTime projectTimeToTravel;
                if (distance > 0)
                {
                    MyLogger.LogInfo("MyAverageMetersPerSecond: " + theTrip.MyAverageMetersPerSecond);
                    totalSecondsCalculated = Convert.ToInt32(distance) / theTrip.MyAverageMetersPerSecond;
                    MyLogger.LogInfo("totalSecondsCalculated: " + totalSecondsCalculated);

                    projectTimeToTravel = DateTime.UtcNow.AddSeconds(totalSecondsCalculated);
                    MyLogger.LogInfo("projectTimeToTravel: " + projectTimeToTravel);
                    projectTimeToTravel = projectTimeToTravel.AddMinutes(-6);
                    MyLogger.LogInfo("re-projectTimeToTravel minus 5: " + projectTimeToTravel);
                }
                else
                {
                    projectTimeToTravel = DateTime.UtcNow.AddMinutes(5);
                }

                theProjection= RouteMarkingProjection.CreateInstance(projectTimeToTravel, Convert.ToInt32(distance), totalSecondsCalculated);
                return theProjection;
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
            return null;
        }
        private bool IsSignalClear(string? theRouteName)
        {
            try
            {
                var signalNames = theRouteName?.Split("-");
                var theSignalName = "";
                if (signalNames != null && signalNames.Length > 0)
                {
                    theSignalName = signalNames[0];
                    if (MyRailGraphManager?.ILGraph?.getGraphObj(theSignalName) is SignalOptical signal)
                    {
                        var aspectState = signal.getAspectState();
                        if (aspectState == EAspect.aspectClear) return true;
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
            return false;
        }

        #region Route Marking Classes
        public class RouteMarkInfo
        {
            public string? RouteName { get; }
            public DateTime RouteActionTime { get; }

            private RouteMarkInfo(string? routeName, DateTime routeActionTime)
            {
                RouteName = routeName;
                RouteActionTime = routeActionTime;
            }

            public static RouteMarkInfo CreateInstance(string? routeName, DateTime routeActionTime)
            {
                return new RouteMarkInfo(routeName, routeActionTime);
            }
        }

        public class RouteMarkingProjection
        {
            public DateTime ProjectTimeToTravel { get; set; }
            public int DistanceTraveledToNextSignal { get; }
            public int TotalSecondsToNextSignal { get; }

            private RouteMarkingProjection(DateTime projectTimeToTravel, int distanceTraveledToNextSignal, int totalSecondsToNextSignal)
            {
                ProjectTimeToTravel = projectTimeToTravel;
                DistanceTraveledToNextSignal = distanceTraveledToNextSignal;
                TotalSecondsToNextSignal = totalSecondsToNextSignal;
            }

            public static RouteMarkingProjection CreateInstance(DateTime projectTimeToTravel, int distanceTraveledToNextSignal, int totalSecondsToNextSignal)
            {
                return new RouteMarkingProjection(projectTimeToTravel, distanceTraveledToNextSignal, totalSecondsToNextSignal);
            }
        }

        #endregion
        #endregion

        #endregion

        #region Automatic Forecast Updates

        #region Forecast Methods
        public void BuildNewForecastToPublishFromPlan(Trip theTrip)
        {
            //OK
            try
            {
                PerformForecastUpdate(theTrip, false);
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void CheckForArrivalDepartureEvent(Trip theTrip ,TrainPosition? thePosition)
        {
            try
            {
                if (thePosition == null) return;
                var elementName = thePosition.ElementExtension?.StartPos.ElementId;
                foreach (var tl in theTrip.TimedLocations)
                {
                    var onPlatform = DoCheckEdgeTrackPositionMatch(tl.MyPlatform, elementName);
                    if (onPlatform && theTrip.IsAllocated)
                    {
                        if (!tl.HasArrivedToPlatform)
                        {
                            tl.HasArrivedToPlatform = true;
                            tl.ArrivalTimeActual = DateTime.Now;
                            var deltaTime = tl.ArrivalTimeActual - tl.ArrivalTimePlan;
                            tl.ArrivalTimeAdjusted = tl.ArrivalTimeActual;
                            MyLogger.LogInfo("CheckForArrivalDepartureEvent:Trip <" + theTrip.TripCode +"> Has Arrived at Platform <" + tl.MyPlatform.MyName +"> On Element <" + elementName + "> Plan Arrival @ " + tl.ArrivalTimePlan + "> Arrival Actual @ <" + tl.ArrivalTimeActual +">");
                            var thread = new Thread(() => DoUpdateForecastInTrip(theTrip, tl, deltaTime)); thread.Start();
                            break;
                        }
                    }
                    else
                    {
                        if (tl.HasArrivedToPlatform && !tl.HasDepartedFromPlatform)
                        {
                            tl.HasDepartedFromPlatform = true;
                            tl.DepartureTimeActual = DateTime.Now;
                            var deltaTime = tl.DepartureTimeActual - tl.DepartureTimePlan;

                            tl.DepartureTimeAdjusted = tl.DepartureTimeActual;
                            MyLogger.LogInfo("CheckForArrivalDepartureEvent:Trip <" + theTrip.TripCode + "> Has Departed at Platform <" + tl.MyPlatform.MyName + "> On Element <" + elementName + "> Plan Departure @ " + tl.DepartureTimePlan + "> Departure Actual @ <" + tl.DepartureTimeActual + ">");
                            var thread = new Thread(() => DoUpdateForecastInTrip(theTrip, tl, deltaTime)); thread.Start();
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private bool DoCheckEdgeTrackPositionMatch(Network.Platform thePlatform, string? currentPositionName)
        {
            try
            {
                if (thePlatform.MyElementPosition?.ElementId == currentPositionName) return true;
                if (thePlatform.MyHierarchyTrack?.SysName == currentPositionName) return true;
                if (thePlatform.MyTrackNetwork?.MyTrackAssociation?.TrackName == currentPositionName) return true;
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }

            return false;
        }
        private void DoUpdateForecastInTrip(Trip theTrip, TimedLocation theLocation, TimeSpan deltaTime)
        {
            try
            {
                //var deltaTime = theLocation.DepartureTimeActual - theLocation.DepartureTimePlan;
                //if (deltaTime.TotalSeconds <= 30)
                //{
                //    MyLogger.LogInfo("DoUpdateForecastInTrip:MockTrip <" + theTrip.TripCode + "> Delta Time On Departure Less Than 30 Second");
                //    return;
                //}

                var timeLocationFound = false;
                foreach (var tl in theTrip.TimedLocations)
                {
                    if (tl.Description == theLocation.Description) timeLocationFound = true;

                    if (timeLocationFound)
                    {
                        if (tl.Description == theLocation.Description)
                        {
                            if (!tl.HasDepartedFromPlatform)
                            {
                                tl.DepartureTimeAdjusted = tl.DepartureTimePlan.AddSeconds(deltaTime.TotalSeconds);
                            }
                            //tl.ArrivalTimeAdjusted = tl.ArrivalTimeActual;
                            //tl.DepartureTimeAdjusted = tl.DepartureTimeActual;
                        }
                        else
                        {
                            tl.ArrivalTimeAdjusted = tl.ArrivalTimePlan.AddSeconds(deltaTime.TotalSeconds);
                            MyLogger.LogInfo("DoUpdateForecastInTrip:Trip <" + theTrip.TripCode + "> Adjusted Arrival Time <" + tl.MyPlatform.MyName + "> Plan Arrival @ " + tl.ArrivalTimePlan + "> Arrival Adjusted @ <" + tl.ArrivalTimeAdjusted + "> Delta <" + deltaTime.TotalSeconds + ">");

                            tl.DepartureTimeAdjusted = tl.DepartureTimePlan.AddSeconds(deltaTime.TotalSeconds);
                            MyLogger.LogInfo("DoUpdateForecastInTrip:Trip <" + theTrip.TripCode + "> Adjusted Depart Time <" + tl.MyPlatform.MyName + "> Plan Departure @ " + tl.DepartureTimePlan + "> Departure Adjusted @ <" + tl.DepartureTimeAdjusted + "> Delta <" + deltaTime.TotalSeconds + ">");
                        }
                    }
                }
                BuildNewForecastToPublishFromTrain(theTrip);
                UpdateReservationsConflicts(theTrip);
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void DoUpdateForecastFromProjection(Trip theTrip, TimedLocation theLocation, TimeSpan deltaTime)
        {
            try
            {
                var timeLocationFound = false;
                foreach (var tl in theTrip.TimedLocations)
                {
                    if (tl.Description == theLocation.Description) timeLocationFound = true;

                    if (timeLocationFound)
                    {
                        tl.ArrivalTimeAdjusted = tl.ArrivalTimePlan.AddSeconds(deltaTime.TotalSeconds);
                        MyLogger.LogInfo("DoUpdateForecastFromProjection:Trip <" + theTrip.TripCode + "> Adjusted Arrival Time <" + tl.MyPlatform.MyName + "> Plan Arrival @ " + tl.ArrivalTimePlan + "> Arrival Adjusted @ <" + tl.ArrivalTimeAdjusted + "> Delta <" + deltaTime.TotalSeconds + ">");

                        tl.DepartureTimeAdjusted = tl.DepartureTimePlan.AddSeconds(deltaTime.TotalSeconds);
                        MyLogger.LogInfo("DoUpdateForecastFromProjection:Trip <" + theTrip.TripCode + "> Adjusted Depart Time <" + tl.MyPlatform.MyName + "> Plan Departure @ " + tl.DepartureTimePlan + "> Departure Adjusted @ <" + tl.DepartureTimeAdjusted + "> Delta <" + deltaTime.TotalSeconds + ">");
                    }
                }
                BuildNewForecastToPublishFromTrain(theTrip);
                UpdateReservationsConflicts(theTrip);
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }

        private void BuildNewForecastToPublishFromTrain(Trip theTrip)
        {
            try
            {

                var result = PerformForecastUpdate(theTrip);
                if (result)
                {
                    var message = "Updated Forecast Sent For Allocated Trip <" + theTrip.TripCode + ">";
                    CreateEvent(message, "UPDATED FORECAST SENT", AlertLevel.INFORMATION, true);
                }
                else
                {
                    var message = "Updated Forecast NOT Sent For Allocated Trip <" + theTrip.TripCode + ">";
                    CreateEvent(message, "UPDATED FORECAST NOT SENT", AlertLevel.WARN, true);
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void BuildNewForecastToPublishFromUnAllocate(Trip theTrip)
        {
            try
            {
                foreach (var tl in theTrip.TimedLocations)
                {
                    tl.ArrivalTimeAdjusted = tl.ArrivalTimePlan;
                    tl.DepartureTimeAdjusted = tl.DepartureTimePlan;
                }
                UpdateReservationsConflicts(theTrip);
                var result = PerformForecastUpdate(theTrip);
                if (result)
                {
                    var message = "Updated Forecast Sent For UnAllocated Trip <" + theTrip.TripCode + ">";
                    CreateEvent(message, "UPDATED FORECAST SENT", AlertLevel.INFORMATION, true);
                }
                else
                {
                    var message = "Updated Forecast NOT Sent For UnAllocated Trip <" + theTrip.TripCode + ">";
                    CreateEvent(message, "UPDATED FORECAST NOT SENT", AlertLevel.WARN, true);
                }

            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void BuildNewForecastToPublishFromDwellTimeAtPlatform(Trip theTrip)
        {
            try
            {
                var platformDwell = "UNKNOWN";
                foreach (var tl in theTrip.TimedLocations)
                {
                    if (tl.HasArrivedToPlatform && !tl.HasDepartedFromPlatform)
                    {
                        tl.DepartureTimeAdjusted = DateTime.Now;
                        var deltaTime = tl.DepartureTimeAdjusted - tl.DepartureTimePlan;

                        //tl.DepartureTimeAdjusted = tl.DepartureTimeActual;
                        platformDwell = tl.Description;
                        //MyLogger.LogInfo("BuildNewForecastToPublishFromDwellTimeAtPlatform:MockTrip <" + theTrip.TripCode + "> Has Departed at Platform <" + tl.MyPlatform.MyName + "> On Element <" + elementName + "> Plan Departure @ " + tl.DepartureTimePlan + "> Departure Actual @ <" + tl.DepartureTimeActual + ">");
                        var thread = new Thread(() => DoUpdateForecastInTrip(theTrip, tl, deltaTime)); thread.Start();
                        //DoUpdateForecastInTrip(theTrip, tl, deltaTime);
                        break;
                    }
                }

                //BuildNewForecastToPublishFromTrain(theTrip);

                //var result = PerformForecastUpdate(theTrip);
                //if (result)
                //{
                //    var message = "Updated Forecast Sent For Allocated MockTrip <" + theTrip.TripCode + "> at Platform <" + platformDwell +">";
                //    CreateEvent(message, "UPDATED FORECAST SENT", AlertLevel.INFORMATION, true);
                //}
                //else
                //{
                //    var message = "Updated Forecast NOT Sent For Allocated MockTrip <" + theTrip.TripCode + ">";
                //    CreateEvent(message, "UPDATED FORECAST NOT SENT", AlertLevel.WARN, true);
                //}

                //UpdateReservationsConflicts(theTrip);

            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }

        #endregion

        #region Reservation and Conflict Updates
        private void UpdateReservationsConflicts(Trip theTrip)
        {
            try
            {
                RemoveConflicts(theTrip);
                UpdateReservations(theTrip);
                UpdateConflicts(theTrip);
                lock (TripList)
                {
                    MyTrainSchedulerManager?.ProduceMessage2003(theTrip);
                }
                MyLogger.LogInfo("UpdateReservationsConflicts:Trip <" + theTrip.TripCode + ">");

            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void UpdateConflicts(Trip theTrip)
        {
            try
            {
                //do check for conflicts
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void RemoveConflicts(Trip theTrip)
        {
            try
            {
                //Remove old conflicts
                //theTrip.MyConflicts = new List<Conflict>();
                var tempConflicts = new List<Conflict>(theTrip.MyConflicts);
                foreach (var conflict in tempConflicts)
                {
                   if (conflict.IsReservationConflict) theTrip.MyConflicts.Remove(conflict);
                }
                lock (TripList)
                {
                    foreach (var reservation in theTrip.MyReservations)
                    {
                        foreach (var trip in TripList)
                        {
                            var conflictsToRemove = new List<Conflict>();
                            var updateRequired = false;
                            foreach (var conflict in trip.MyConflicts)
                            {
                                if (conflict.ConflictingReservation == null) continue;
                                if (conflict.ConflictingReservation.MyGuid == reservation.MyGuid)
                                {
                                    conflictsToRemove.Add(conflict);
                                    updateRequired = true;
                                }
                            }
                            foreach (var conflict in conflictsToRemove)
                            {
                                trip.MyConflicts.Remove(conflict);
                            }
                            //if(updateRequired) MyTrainSchedulerManager?.ProduceMessage2003(trip);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void UpdateReservations(Trip theTrip)
        {
            try
            {
                //Remove old reservations
                //theTrip.MyReservations = new List<Reservation>();

                //Build New reservations
                theTrip.UpdateTripReservations(true);
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        #endregion

        #region Delay Update
        public void UpdateForecastOnDelay(int theDelayInSeconds, string theTripCode, string theDayCode)
        {
            try
            {
                var thread = new Thread(() => DoUpdateForecastOnDelay(theDelayInSeconds, theTripCode, theDayCode));
                thread.Start();

            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void DoUpdateForecastOnDelay(int theDelayInSeconds, string theTripCode, string theDayCode)
        {
            try
            {
                var trip = FindTripFromScheduleDayCode(theTripCode, theDayCode);
                if (trip != null)
                {
                    MyLogger.LogInfo("DoUpdateForecastOnDelay: Trip Found <" + theTripCode + "><" + theDayCode + ">");
                    BuildNewForecastToPublishFromDelay(trip, theDelayInSeconds);
                }
                else
                {
                    MyLogger.LogInfo("DoUpdateForecastOnDelay: Trip Not Found <" + theTripCode +"><" + theDayCode +">");
                }

            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void BuildNewForecastToPublishFromDelay(Trip theTrip, int theDelayInSeconds)
        {
            try
            {
                foreach (var tl in theTrip.TimedLocations)
                {
                    if (!theTrip.IsAllocated)
                    {
                        tl.DepartureTimeAdjusted = tl.DepartureTimePlan.AddSeconds(theDelayInSeconds);
                        tl.ArrivalTimeAdjusted = tl.ArrivalTimePlan.AddSeconds(theDelayInSeconds);
                        MyLogger.LogInfo("BuildNewForecastToPublishFromDelay:UnAllocated Trip <" + theTrip.TripCode + "> Adjusted Arrival Time <" + tl.MyPlatform.MyName + "> Plan Arrival @ " + tl.ArrivalTimePlan + "> Arrival Adjusted @ <" + tl.ArrivalTimeAdjusted + "> Delta <" + theDelayInSeconds + ">");
                        MyLogger.LogInfo("BuildNewForecastToPublishFromDelay:UnAllocated Trip <" + theTrip.TripCode + "> Adjusted Depart Time <" + tl.MyPlatform.MyName + "> Plan Departure @ " + tl.DepartureTimePlan + "> Departure Adjusted @ <" + tl.DepartureTimeAdjusted + "> Delta <" + theDelayInSeconds + ">");
                    }
                    else
                    {
                        if (tl.HasDepartedFromPlatform) continue;
                        var actualDepart = tl.DepartureTimeActual;
                        var actualArrive = tl.ArrivalTimeActual;
                        tl.DepartureTimeActual = tl.DepartureTimeActual.AddSeconds(theDelayInSeconds);
                        tl.ArrivalTimeActual = tl.ArrivalTimeActual.AddSeconds(theDelayInSeconds);
                        MyLogger.LogInfo("BuildNewForecastToPublishFromDelay:Allocated Trip <" + theTrip.TripCode + "> Adjusted Arrival Time <" + tl.MyPlatform.MyName + "> Current Arrival @ " + actualArrive + "> Arrival Adjusted @ <" + tl.ArrivalTimeAdjusted + "> Delta <" + theDelayInSeconds + ">");
                        MyLogger.LogInfo("BuildNewForecastToPublishFromDelay:Allocated Trip <" + theTrip.TripCode + "> Adjusted Depart Time <" + tl.MyPlatform.MyName + "> Current Departure @ " + actualDepart + "> Departure Adjusted @ <" + tl.DepartureTimeAdjusted + "> Delta <" + theDelayInSeconds + ">");
                    }
                }
                PerformForecastUpdate(theTrip);
                UpdateReservationsConflicts(theTrip);
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        #endregion

        #region General Forecast Update Functions
        private bool PerformForecastUpdate(Trip theTrip, bool publishUpdate = true)
        {
            try
            {
                var newForecast = Forecast.CreateInstance(theTrip);
                if (newForecast != null)
                {
                    var forecastUpdateResult = RequestForecastUpdate(newForecast).Result;

                    if (publishUpdate)
                    {
                        //lock (TripList)
                        //{
                            MyTrainSchedulerManager?.ProduceMessage2003(theTrip);
                        //}
                    }
                    DoSerializeForecast(newForecast, theTrip);
                    return true;
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
            return false;
        }
        private string GetTripString(ScheduledPlan thePlan)
        {
            var tripString = new StringBuilder();
            try
            {
                foreach (var trip in thePlan.Trips)
                {
                    tripString.Append(trip.Value.TripCode + "-");
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
            return tripString.ToString();
        }
        
        #endregion

        #endregion

        #region Event Logging
        public void AddLogEvent(string theEvent, AlertLevel theAlertLevel)
        {
            try
            {
                var thread = new Thread(() => DoAddLogEvent(theEvent, theAlertLevel));
                thread.Start();
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void DoAddLogEvent(string thEvent, AlertLevel theAlertLevel)
        {
            try
            {
                switch (theAlertLevel)
                {
                    case AlertLevel.INFORMATION:
                    {
                        MyLogger.LogInfo(thEvent);
                        break;
                    }
                    case AlertLevel.FATAL:
                        break;
                    case AlertLevel.WARN:
                        break;
                    case AlertLevel.ERROR:
                        MyLogger.LogException(thEvent);

                        break;
                    case AlertLevel.DEBUG:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(theAlertLevel), theAlertLevel, null);
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        public void CreateEvent(string theMessage, string theEventName, AlertLevel theLevel, bool inform, bool log = true )
        {
            try
            {
                if (log)
                {
                    switch (theLevel)
                    {
                        case AlertLevel.INFORMATION:
                        {
                            MyLogger.LogInfo(theMessage);
                            break;
                        }
                    }
                }

                if (!inform) return;
                var alertLevel = Enum.GetName(theLevel);
                var eventMessage = Messages.EventMessage.CreateInstance(theEventName, alertLevel!, theMessage);
                MyLogger.LogInfo("CreateEvent: " + theMessage);
                //var message = Messages.ConflictManagementMessages.CmsEventMessage.CreateInstance(eventMessage);
                lock (TripList)
                {
                    MyTrainSchedulerManager?.ProduceMessage1005(eventMessage);
                }

            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        public enum AlertLevel
        {
            FATAL,
            WARN,
            ERROR,
            INFORMATION,
            DEBUG
        }

        #endregion

        #region Serialize Datasets
        public void DoSerializeTrip(string? tripCode, string? tripUid)
        {
            try
            {
                var trip = FindTrip(tripCode, tripUid, true);
                if (trip != null)
                {
                    var str = JsonConvert.SerializeObject(trip);
                    var filename = $"Trip-" + tripCode + "-" + tripUid + $"-{DateTime.Now:MMddyyyyhhmmssfff}.json";
                    var curDir = Environment.CurrentDirectory;
                    const string folder = @"Data\SerializeData\Trips";
                    if (!Directory.Exists(System.IO.Path.Combine(curDir, folder)))
                    {
                        Directory.CreateDirectory(System.IO.Path.Combine(curDir, folder));
                    }

                    var fullpath = System.IO.Path.Combine(curDir, folder, filename);
                    File.WriteAllText(fullpath, str);

                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void DoSerializeForecast(Forecast theForecast, Trip theTrip)
        {
            try
            {
                var str = JsonConvert.SerializeObject(theForecast);
                var filename = $"Forecast-" + theTrip.TripCode + "-" + theTrip.TripId + $"-{DateTime.Now:MMddyyyyhhmmssfff}.json";
                var curDir = Environment.CurrentDirectory;
                const string folder = @"Data\SerializeData\Forecasts";
                if (!Directory.Exists(System.IO.Path.Combine(curDir, folder)))
                {
                    Directory.CreateDirectory(System.IO.Path.Combine(curDir, folder));
                }

                var fullpath = System.IO.Path.Combine(curDir, folder, filename);
                File.WriteAllText(fullpath, str);
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        public void DoSerializeSchedulePlanChange(ScheduledPlan thePlan)
        {
            try
            {
                    var str = JsonConvert.SerializeObject(thePlan);
                    var trips = GetTripString(thePlan);
                    var filename = $"SchedulePlanChange-" + thePlan.ScheduledDayCode + "-" + thePlan.Name +"-" + trips + $"{DateTime.Now:MMddyyyyhhmmssfff}.json";
                    var curDir = Environment.CurrentDirectory;
                    const string folder = @"Data\SerializeData\SchedulePlanChanges";
                    if (!Directory.Exists(System.IO.Path.Combine(curDir, folder)))
                    {
                        Directory.CreateDirectory(System.IO.Path.Combine(curDir, folder));
                    }

                    var fullpath = System.IO.Path.Combine(curDir, folder, filename);
                    File.WriteAllText(fullpath, str);

            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void SerializeRouteMarking(List<RouteMarkInfo> theList, Trip theTrip)
        {
            try
            {
                var str = JsonConvert.SerializeObject(theList);
                var filename = $"RouteMarking-" + theTrip.TripCode + "-" + theTrip.TripId + $"-{DateTime.Now:MMddyyyyhhmmssfff}.json";
                var curDir = Environment.CurrentDirectory;
                const string folder = @"Data\SerializeData\RouteMarking";
                if (!Directory.Exists(System.IO.Path.Combine(curDir, folder)))
                {
                    Directory.CreateDirectory(System.IO.Path.Combine(curDir, folder));
                }

                var fullpath = System.IO.Path.Combine(curDir, folder, filename);
                File.WriteAllText(fullpath, str);


            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }

        #endregion

        #endregion
    }
}

