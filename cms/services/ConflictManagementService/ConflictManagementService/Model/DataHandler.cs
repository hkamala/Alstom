using System.IO;
using System.Security.Cryptography.Pkcs;
using System.Text;
using ConflictManagementLibrary.Logging;
using ConflictManagementLibrary.Management;
using ConflictManagementLibrary.Model.Movement;
using ConflictManagementLibrary.Model.Trip;
using ConflictManagementLibrary.Network;
using Newtonsoft.Json;
using RoutePlanLib;
using Route = RailgraphLib.HierarchyObjects.Route;

namespace ConflictManagementService.Model;

using Cassandra;
using ConflictManagementService.Model.TMS;
using System.Collections.Concurrent;
using System.Text.Json;
using Serilog;
using static System.Diagnostics.Debug;
using E2KService.MessageHandler;
using static E2KService.ServiceImp;
using System.Security.Cryptography;
using XSD.RoutePlan;
using static System.Formats.Asn1.AsnWriter;
using System.Numerics;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using System.Drawing.Text;
using XSD.ServiceRoutePlanRequest;
using ConflictManagementLibrary.Helpers;
using Amqp.Types;
using Microsoft.AspNetCore.Routing;
using RailgraphLib.RailExtension;
using RailgraphLib.HierarchyObjects;
using ConflictManagementLibrary.Model.Schedule;
using Microsoft.AspNetCore.Components.Routing;
using TrainData;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

////////////////////////////////////////////////////////////////////////////////

internal class DataHandler
{
    #region Declarations Public
    
    // Public data collections
    public int PretestIndex = 0;
    public ScheduledDays ScheduledDays => scheduledDays;
    public TrainPositions TrainPositions => trainPositions;
    public TrainEstimationPlans TrainEstimationPlans => trainEstimationPlans;
    public EstimationPlans EstimationPlans => estimationPlans;
    public TrainRoutePlans TrainRoutePlans => trainRoutePlans;
    public ScheduledRoutePlans ScheduledRoutePlans => scheduledRoutePlans;
    public ScheduledPlans ScheduledPlans => scheduledPlans;
    public Possessions Possessions => possessions;
    public Dictionary<string /*stationId*/, Station> Stations => stations;
    public Dictionary<string /*obid*/, Tuple<ScheduledPlanKey, int /*MockTrip ID*/>> TimetableAllocations => timetableAllocations;
    public ActionTypeList? TypeLists => typeLists;
    public List<TrainTypeDetails>? TrainTypes => typeLists?.traintypes?.ToList();
    public TripProperties TripProperties => tripProperties;
    public Movements Movements => movements;

    public string TmsUserGuid { get => tmsUserGuid; set => tmsUserGuid = value; }
    #endregion

    #region Delegates

    // Callback delegates for data changes and requests
    public delegate void DelegateTrainPositionChanged(Train? train, TrainPosition trainPosition);
    public delegate void DelegateTrainDeleted(Train train, ActionTime occurredTime);
    public delegate void DelegateEstimationPlanChanged(EstimationPlan estimationPlan);
    public delegate void DelegateEstimationPlanDeleted(EstimationPlan estimationPlan);
    public delegate void DelegateScheduledPlanChanged(ScheduledPlan scheduledPlan);
    public delegate void DelegateScheduledPlanDeleted(ScheduledPlan scheduledPlan);
    public delegate void DelegatePossessionChanged(Possession possession);
    public delegate void DelegatePossessionDeleted(Possession possession);
    public delegate void DelegateNotifyTrainPositionsRefreshRequestEnded();
    public delegate void DelegateNotifyEstimationPlansRefreshRequestEnded();
    public delegate void DelegateNotifyScheduledPlansRefreshRequestEnded();
    public delegate void DelegateConnectTimetableToTrain(Train train, ScheduledPlan scheduledPlan, int tripId);   //TODO: add more parameters if needed!
    public delegate void DelegateDisconnectTimetableFromTrain(Train train);
    public delegate void DelegateStationPriorityChanged(Station station);
    public delegate void DelegateInitialDataRequest();
    public delegate void DelegateTripPropertyChanged(TripProperty tripProperty);
    public delegate void DelegateTimetableInfoChanged();
    public delegate void DelegateTrainPropertySet(Train train, TrainProperty trainProperty);
    public delegate void DelegateTrainPropertiesSet(Train train, List<TrainProperty> trainProperties);
    public delegate void DelegateScheduledServicesRequest(string lineId, string scheduledDaycode);

    // Delegate properties
    public DelegateTrainPositionChanged? NotifyTrainPositionChanged { get; set; }
    public DelegateTrainDeleted? NotifyTrainDeleted { get; set; }
    public DelegateEstimationPlanChanged? NotifyEstimationPlanChanged { get; set; }
    public DelegateEstimationPlanDeleted? NotifyEstimationPlanDeleted { get; set; }
    public DelegateScheduledPlanChanged? NotifyScheduledPlanChanged { get; set; }
    public DelegateScheduledPlanDeleted? NotifyScheduledPlanDeleted { get; set; }
    public DelegatePossessionChanged? NotifyPossessionChanged { get; set; }
    public DelegatePossessionDeleted? NotifyPossessionDeleted { get; set; }
    public DelegateNotifyTrainPositionsRefreshRequestEnded? NotifyTrainPositionsRefreshRequestEnded { get; set; }
    public DelegateNotifyEstimationPlansRefreshRequestEnded? NotifyEstimationPlansRefreshRequestEnded { get; set; }
    public DelegateNotifyScheduledPlansRefreshRequestEnded? NotifyScheduledPlansRefreshRequestEnded { get; set; }
    public DelegateConnectTimetableToTrain? SendConnectTimetableToTrain { get; set; }
    public DelegateDisconnectTimetableFromTrain? SendDisconnectTimetableFromTrain { get; set; }
    public DelegateStationPriorityChanged? NotifyStationPriorityChanged { get; set; }
    public DelegateInitialDataRequest? SendInitialDataRequest { get; set; }
    public DelegateTripPropertyChanged NotifyTripPropertyChanged { get; set; }
    public DelegateTimetableInfoChanged NotifyTimetableInfoChanged { get; set; }
    public DelegateTrainPropertySet NotifyTrainPropertySet { get; set; }
    public DelegateTrainPropertiesSet NotifyTrainPropertiesSet { get; set; }
    public DelegateScheduledServicesRequest? SendScheduledServicesRequest { get; set; }
    #endregion

    #region Declarations Private

    // These are the default initialization values for concurrent collections. These are not the limits of collections!
    const int defaultConcurrencyLevel = 2;  // Estimated amount of threads updating collections
    const int defaultTrainCount = 40;
    const int defaultEstimationPlans = defaultTrainCount;
    const int defaultRoutePlans = defaultTrainCount;
    const int defaultScheduledPlans = defaultTrainCount * 3;
    const int defaultPossessions = 10;

    // Timetables and possessions
    private readonly ScheduledDays scheduledDays = new(defaultConcurrencyLevel, 5);
    private readonly TrainPositions trainPositions = new(defaultConcurrencyLevel, defaultTrainCount);
    private readonly TrainEstimationPlans trainEstimationPlans = new(defaultConcurrencyLevel, defaultEstimationPlans);
    private readonly EstimationPlans estimationPlans = new(defaultConcurrencyLevel, defaultEstimationPlans);
    private readonly TrainRoutePlans trainRoutePlans = new(defaultConcurrencyLevel, defaultRoutePlans);
    private readonly ScheduledRoutePlans scheduledRoutePlans = new(defaultConcurrencyLevel, defaultScheduledPlans);
    private readonly ScheduledPlans scheduledPlans = new(defaultConcurrencyLevel, defaultScheduledPlans);
    private readonly Possessions possessions = new(defaultConcurrencyLevel, defaultPossessions);

    // Stations and platforms
    private readonly Dictionary<string /*station ID*/, Station> stations = new();

    // Services
    private readonly Dictionary<ScheduledPlanKey, ScheduledPlan> requestedScheduledPlans = new();

    // Trains
    private readonly ConcurrentDictionary<string /*obid*/, Train> trains = new();

    // Movements
    private readonly Movements movements = new(defaultConcurrencyLevel, 100);

    // Timetable allocations
    //TODO: Don't know how tscheduler manages timetable allocations, if daily timetable is loaded again during a day and IDs change
    //      We have to hope it re-allocates timetables and informs about them
    private Dictionary<string /*obid*/, Tuple<ActionTime, ScheduledPlanKey, int /*MockTrip ID*/>> timetableAllocationRequests = new();
    private Dictionary<string /*obid*/, Tuple<ScheduledPlanKey, int /*MockTrip ID*/>> timetableAllocations = new();

    // Timetable properties
    private readonly TripProperties tripProperties = new();

    // TMS types (train types etc.)
    private ActionTypeList? typeLists = null;

    // Route markings of train
    Dictionary<string /*obid*/, List<Tuple<ActionTime, string /*route name*/>>> trainRouteMarkings = new();

    private readonly Thread maintenanceThread;
    private const int c_SleepTimeMS = 1000;
    private volatile bool shuttingDown = false;
    private int historyHours = 24;

    // TODO: make these configurable?
    const int extTrainPositionsRequestPendingTimeout = 10; // seconds
    const int extScheduledPlansRequestPendingTimeout = 10;
    const int extPossessionRequestPendingTimeout = 10;

    ActionTime trainPositionsRequestTimeout = new();
    ActionTime scheduledPlansRequestTimeout = new();
    ActionTime possessionsRequestTimeout = new();

    ActionTime initialDataRequestedTimeStamp = ActionTime.Now;
    ActionTime lastRouteMarkingCheckTimeStamp = ActionTime.Now;

    private delegate void RefreshRequestTimeoutHandler();

    readonly List<string> obidTrainsToDeleteAfterRefresh = new();

    private PurgeTime? purgeTime = null;
    private string tmsUserGuid = "";

    // Cassandra
    readonly ISession? cassandraSession = null;
    readonly ConsistencyLevel? cassandraConsistencyLevel = null;

    ////////////////////////////////////////////////////////////////////////////////

    #endregion

    #region Constructor
    internal DataHandler(List<string> cassandraContactPoints, int cassandraPort, uint cassandraConsistencyLevel)
    {
        try
        {
            string keyspaceCMS = "cms";

            Log.Information($"Connecting to Cassandra cluster (keyspace '{keyspaceCMS}')...");

            //          PoolingOptions poolingOptions = PoolingOptions.Create()
            //                                          .SetCoreConnectionsPerHost(HostDistance.Local, 10) // default 2
            //                                          .SetMaxConnectionsPerHost(HostDistance.Local, 10) // default 8
            //                                          .SetCoreConnectionsPerHost(HostDistance.Remote, 10) // default 1
            //                                          .SetMaxConnectionsPerHost(HostDistance.Remote, 10); // default 2
            // 
            //          SocketOptions socketOptions = new SocketOptions()
            //                                        .SetReadTimeoutMillis(90000); // default 12000
            // 
            //          buildCluster = Cluster.Builder()
            //                         .AddContactPoint(Program.ContactPoint)
            //                         .WithPort(Program.CosmosCassandraPort)
            //                         .WithCredentials(Program.UserName, Program.Password)
            //                         .WithPoolingOptions(poolingOptions)
            //                         .WithSocketOptions(socketOptions)
            //                         .WithReconnectionPolicy(new ConstantReconnectionPolicy(1000)) // default ExponentialReconnectionPolicy
            //                         .WithSSL(sslOptions);
            //
            //
            //var cluster = Cluster.Builder()
            //           .AddContactPoints("192.168.1.198", "192.168.1.196", "192.168.1.197", "192.168.1.195")
            //           .WithPort(9042)
            //           .WithLoadBalancingPolicy(new DCAwareRoundRobinPolicy("<Data Centre (e.g AWS_VPC_US_EAST_1)>"))
            //           .WithSocketOptions(new SocketOptions().SetConnectTimeoutMillis(20000).SetReadTimeoutMillis(60000))
            //           .WithAuthProvider(new PlainTextAuthProvider("Username", "Password"))
            //           .Build();

            var cluster = Cluster.Builder()
                .AddContactPoints(cassandraContactPoints)
                .WithPort(cassandraPort)
                .WithSocketOptions(new SocketOptions().SetReadTimeoutMillis(60000)) // Does nothing!
                .WithQueryOptions(new QueryOptions().SetPageSize(100))  // Solution to read timeout problem!
                .Build();

            this.cassandraConsistencyLevel = (ConsistencyLevel)Math.Max(1, Math.Min(cassandraConsistencyLevel, 3)); // ConsistencyLevel.One is minimum, ConsistencyLevel.Three is maximum

            // Connect and select 'cms' keyspace
            cassandraSession = cluster.Connect(keyspaceCMS);

            Log.Information($"Connected to keyspace '{keyspaceCMS}' of cluster: " + cluster.Metadata.ClusterName);
        }
        catch (Exception ex)
        {
            throw new Exception("Connection to Cassandra cluster failed: " + ex.Message);
        }

        maintenanceThread = new Thread(new ThreadStart(MaintenanceThread))
        {
            Name = "ModelPeriodicTask",
            IsBackground = true
        };
        maintenanceThread.Start();
        //StartTests();
    }
    #endregion

    #region CMS Maintenance Functions 
    public void CreateStationNetwork(Dictionary<uint, Station> networkedStations)
    {
        foreach (var station in networkedStations.Values)
        {
            // If external ID (name) has not been configured, use sysname as station ID
            string stationId = station.CTCStation.ExternalID != "" ? station.CTCStation.ExternalID : station.CTCStation.SysName;
            station.StationId = stationId;

            this.stations.Add(stationId, station);
        }
    }
    private void MaintenanceThread()
    {
        while (!shuttingDown)
        {
            PerformPeriodicTasks();

            Thread.Sleep(c_SleepTimeMS);
        }
    }
    private void PerformPeriodicTasks()
    {
        ActionTime utcTimeNow = ActionTime.Now;

        // Are there pending timetable allocations that have not succeeded?
        List<string> trains;
        lock (this.timetableAllocationRequests)
        {
            trains = this.timetableAllocationRequests.Keys.ToList();
        }
        foreach (var obid in trains)
        {
            var allocation = new Tuple<ActionTime, ScheduledPlanKey, int>(new ActionTime(), new ScheduledPlanKey(0, ""), 0);
            lock (this.timetableAllocationRequests)
            {
                allocation = this.timetableAllocationRequests[obid];
            }
            // Allocation requests should finally succeed in 30 seconds
            if ((allocation.Item1 + new TimeSpan(0, 0, 0, 30)) < utcTimeNow)
            {
                TimetableAllocationRequestFailed(obid);
            }
            else
            {
                // Try re-connection to timetable every 3 seconds, if train has GUID
                Train? train = GetTrain(obid);
                if (train != null && train.Guid != "")
                {
                    int passedTime = (int)(utcTimeNow - allocation.Item1).TotalSeconds;
                    if (passedTime > 0 && passedTime % 3 == 0)
                    {
                        var scheduledPlan = GetScheduledPlan(allocation.Item2);
                        var tripId = allocation.Item3;

                        if (SendConnectTimetableToTrain != null && scheduledPlan != null)
                        {
                            Log.Information($"==> ConnectTimetableToTrain: Sending connection request: train={train}, scheduledPlan={scheduledPlan}, tripId={tripId}");
                            SendConnectTimetableToTrain(train, scheduledPlan, tripId);
                        }
                    }
                }
            }
        }

        // Handle refresh request timeouts 
        var maybeClearPendingRequest = (string name, ActionTime pendingTime, RefreshRequestTimeoutHandler requestTimeoutHandler) =>
        {
            if (pendingTime.IsValid() && (utcTimeNow - pendingTime).TotalSeconds > 0)
            {
                pendingTime.SetTimeInvalid();
                Log.Information("{0} refresh request timeout occurred - calling timeout handler...", name);

                requestTimeoutHandler();
            }
        };

        maybeClearPendingRequest("Train positions", trainPositionsRequestTimeout, DeleteTrainsNotInRefresh);
        maybeClearPendingRequest("Scheduled plans", scheduledPlansRequestTimeout, DeleteScheduledPlansNotInRefresh);
        maybeClearPendingRequest("Possessions", possessionsRequestTimeout, DeletePossessionsNotInRefresh);

        // Handle route markings in queue (every 10 seconds)
        if (this.lastRouteMarkingCheckTimeStamp + new TimeSpan(0, 0, 0, 10) < utcTimeNow)
        {
            lock (trainRouteMarkings)
            {
                List<string> obids = trainRouteMarkings.Keys.ToList();
                foreach (var obid in obids)
                {
                    try
                    {
                        SetNextForecastedRoutes(GetTrain(obid), this.trainRouteMarkings[obid]);
                    }
                    catch { }
                }
            }

            this.lastRouteMarkingCheckTimeStamp = utcTimeNow;
        }

        // Purge old scheduled days, scheduled plans, estimation plans, possessions, timetable properties, etc.
        if (this.purgeTime != null && this.purgeTime.IsPurgeTime())
        {
            PurgeOldData();
        }

        // Request initial data periodically
        if (IsServiceOnline() && this.initialDataRequestedTimeStamp + new TimeSpan(0, 0, 0, 10) < utcTimeNow)
        {
            SendInitialDataRequest?.Invoke();
            this.initialDataRequestedTimeStamp = utcTimeNow;
        }
    }
    private void PurgeOldData()
    {
        Log.Information("Purging old data");

        ActionTime historyStart = ActionTime.Now - TimeSpan.FromHours(this.historyHours);

        try
        {
            // Train positions
            var trainPositionsToDelete = from item in this.trainPositions where item.Value.OccurredTime < historyStart select item.Key;
            foreach (var key in trainPositionsToDelete)
                trainPositions.TryRemove(key, out _);

            // Possessions
            var possessionsToDelete = from item in this.possessions where item.Value.EndTime < historyStart select item.Key;
            foreach (var key in possessionsToDelete)
                this.possessions.TryRemove(key, out _);

            // Estimation plans
            var trainEstimationPlansToDelete = from item in this.trainEstimationPlans where item.Value.TimedLocations.Count > 0 && item.Value.TimedLocations.Last().Departure < historyStart select item.Key;
            foreach (var key in trainEstimationPlansToDelete)
                EstimationPlanDeleted(key);
            var estimationPlansToDelete = from item in this.estimationPlans where item.Value.TimedLocations.Count > 0 && item.Value.TimedLocations.Last().Departure < historyStart select item.Key;
            foreach (var key in estimationPlansToDelete)
                EstimationPlanDeleted(key);

            // Scheduled days (remove older than yesterday), and scheduled plans and trip properties of that day
            ActionTime keepScheduledPlanThreshold = ActionTime.Now - TimeSpan.FromDays(2);
            var scheduledDaysToDelete = from item in this.scheduledDays where item.Value.StartTime < keepScheduledPlanThreshold select item.Key;
            foreach (var scheduledDayCode in scheduledDaysToDelete)
            {
                bool reallyDeleteScheduledDay = true;

                // Scheduled plans
                var scheduledPlansToDelete = from item in this.scheduledPlans where item.Key.ScheduledDayCode == scheduledDayCode select item.Key;
                foreach (var planKey in scheduledPlansToDelete)
                {
                    // Delete scheduled plan (and scheduled day) only if scheduled plan's end time falls before threshold used in here
                    if (this.scheduledPlans[planKey].EndTime < keepScheduledPlanThreshold)
                        ScheduledPlanDeleted(planKey);
                    else
                        reallyDeleteScheduledDay = false;
                }

                // MockTrip properties
                if (reallyDeleteScheduledDay)
                {
                    var tripPropertiesToDelete = from item in this.tripProperties where item.Key.ScheduledDayCode == scheduledDayCode select item.Key;
                    foreach (var propKey in tripPropertiesToDelete)
                        this.tripProperties.Remove(propKey);

                    this.scheduledDays.TryRemove(scheduledDayCode, out _);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("Old data purging failed: {0}", ex.Message);
        }
    }
    private bool IsServiceOnline()
    {
        return Service?.GetServiceState() == E2KService.ServiceStateHelper.ServiceState.Online || Service?.GetServiceState() == E2KService.ServiceStateHelper.ServiceState.OnlineDegraded;
    }
    public void ServiceStateChangedOffline(bool shutdownInProgress)
    {
        shuttingDown = shutdownInProgress;

        ClearPendingRequests();
        ClearAllInformation();

        Log.Information("Model changed to Offline");
    }
    public void ServiceStateChangedStandby()
    {
        ClearPendingRequests();
        ClearAllInformation();

        Log.Information("Model changed to Standby");
    }
    public void ServiceStateChangedOnline()
    {
        ClearAllInformation();

        // Read from persistent memory
        LoadStationPriorities();
        LoadTimetableProperties();
        // TODO: Load conflict resolutions, ...

        this.initialDataRequestedTimeStamp = ActionTime.Now;

        // Purge old data immediately
        this.purgeTime = new PurgeTime();
        PurgeOldData();

        Log.Information("Model changed to Online");
    }
    private void ClearAllInformation()
    {
        // Trains, train positions and route plans are kept and updated all the time
        timetableAllocationRequests.Clear();
        timetableAllocations.Clear();
        trainEstimationPlans.Clear();
        estimationPlans.Clear();
        scheduledDays.Clear();
        scheduledPlans.Clear();
        possessions.Clear();
        requestedScheduledPlans.Clear();
        tripProperties.Clear();

        tmsUserGuid = "";

        Log.Information("All data cleared");
    }
    private void ClearPendingRequests()
    {
        // Clear request pending states
        trainPositionsRequestTimeout.SetTimeInvalid();
        scheduledPlansRequestTimeout.SetTimeInvalid();
        possessionsRequestTimeout.SetTimeInvalid();

        Log.Information("Pending requests information cleared");
    }
    public void SetScheduledPlansRequested(bool requested)
    {
        lock (scheduledPlansRequestTimeout)
        {
            if (requested)
            {
                // Let first request (and probably the only one) decide timeout
                if (!IsScheduledPlansRequestPending())
                {
                    // Mark existing scheduled plans as not updated by refresh
                    foreach (var scheduledPlan in scheduledPlans.Values)
                    {
                        scheduledPlan.ClearUpdatedByRefresh();
                    }

                    scheduledPlansRequestTimeout = ActionTime.Now + TimeSpan.FromSeconds(extScheduledPlansRequestPendingTimeout);
                }
            }
            else
            {
                scheduledPlansRequestTimeout.SetTimeInvalid();

                bool clientRequestPending = NotifyScheduledPlansRefreshRequestEnded != null;

                // If client request is pending, this call will update scheduled plans to it
                DeleteScheduledPlansNotInRefresh();

                // Otherwise, we have to send scheduled plans to it as normal dynamic update
                if (!clientRequestPending)
                    SendScheduledPlans();
            }
        }
    }
    public void SetPossessionsRequested(bool requested)
    {
        lock (possessionsRequestTimeout)
        {
            if (requested)
            {
                // Let first request (and probably the only one) decide timeout
                if (!IsPossessionsRequestPending())
                {
                    // Mark existing possessions as not updated by refresh
                    foreach (var possession in possessions.Values)
                    {
                        possession.ClearUpdatedByRefresh();
                    }

                    possessionsRequestTimeout = ActionTime.Now + TimeSpan.FromSeconds(extPossessionRequestPendingTimeout);
                }
            }
            else
            {
                possessionsRequestTimeout.SetTimeInvalid();

                DeletePossessionsNotInRefresh();
            }
        }
    }
    public bool IsTrainPositionsRequestPending()
    {
        lock (trainPositionsRequestTimeout)
        {
            return trainPositionsRequestTimeout.IsValid();
        }
    }
    public bool IsScheduledPlansRequestPending()
    {
        lock (scheduledPlansRequestTimeout)
        {
            return scheduledPlansRequestTimeout.IsValid();
        }
    }
    public bool IsPossessionsRequestPending()
    {
        lock (possessionsRequestTimeout)
        {
            return possessionsRequestTimeout.IsValid();
        }
    }
    private void DeleteTrainsNotInRefresh()
    {
        foreach (string trainObid in obidTrainsToDeleteAfterRefresh)
        {
            // Delete train (and possible estimation plan)
            Train? train = GetTrain(trainObid);
            if (train != null)
            {
                TrainDeleted(train, ActionTime.Now);
                Log.Information("Deleted train obid='" + trainObid + "' because it was not in refresh data any more");

                // Delete possible route plan
                if (TrainRoutePlans.ContainsKey(train.Guid))
                    TrainRoutePlans.TryRemove(train.Guid, out _);
            }
        }
        obidTrainsToDeleteAfterRefresh.Clear();

        // Handle possible pending request
        NotifyTrainPositionsRefreshRequestEnded?.Invoke();
    }
    private void DeleteScheduledPlansNotInRefresh()
    {
        // Get list of scheduled plans, that were not updated in refresh
        List<ScheduledPlanKey> scheduledPlansToDelete = new();

        foreach (var scheduled in ScheduledPlans)
        {
            if (!scheduled.Value.IsUpdatedByRefresh())
                scheduledPlansToDelete.Add(scheduled.Key);
        }

        // And delete them
        foreach (var key in scheduledPlansToDelete)
        {
            ScheduledPlan scheduledPlan = ScheduledPlanDeleted(key);
            if (scheduledPlan.IsValid())
            {
                Log.Information("Deleted scheduled plan '" + key + "' because it was not in refresh data any more");
            }
        }

        // Handle possible pending request
        NotifyScheduledPlansRefreshRequestEnded?.Invoke();
    }
    private void DeletePossessionsNotInRefresh()
    {
        // Get list of possession IDs, that were not updated in refresh
        List<string> possessionsToDelete = new();

        foreach (var possession in Possessions.Values)
        {
            if (!possession.IsUpdatedByRefresh())
                possessionsToDelete.Add(possession.Id);
        }

        // And delete them
        foreach (var id in possessionsToDelete)
        {
            PossessionDeleted(id);

            Log.Information("Deleted possession '" + id + "' because it was not in refresh data any more");
        }
    }
    public void ScheduledServicesUpdated(ServiceNodeDataList serviceList)
    {
        // This may be spontaneous update from TMS, not just response to our request!
        if (serviceList != null && serviceList.services != null)
        {
            // We may need to restrict service info requests by scheduled day code, so now we remember what we have requested to be able to do that
            int scheduledDayCode = serviceList.dayCode;

            // When scheduled service is removed (from openSchedule or otherwise), this list does not contain service any more
            // We have to hope that service info for deleted service has been sent separately by TMS

            foreach (var service in serviceList.services)
            {
                if (service != null && service.serviceID != null)
                {
                    ScheduledPlan scheduledPlan = new(scheduledDayCode, serviceList.lineId, service);

                    if (this.requestedScheduledPlans.ContainsKey(scheduledPlan.Key))
                        this.requestedScheduledPlans[scheduledPlan.Key] = scheduledPlan;
                    else
                        this.requestedScheduledPlans.Add(scheduledPlan.Key, scheduledPlan);    // Wait for service info...

                    Log.Information($"Scheduled service received (day code {scheduledDayCode}): name {service.name}, service ID {service.serviceID}, start site {service.startSite}, end site {service.endSite}");
                }
            }
        }
    }
    public List<ServiceItem> ServicesUpdated(ServiceList serviceList)
    {
        // This may be spontaneous update from TMS, not just response to our request!
        // When scheduled service is removed (from openSchedule or otherwise), the service 'reason' field contains info about it
        // When trip inside allocated service is changed, the service 'reason' field contains info about it

        var requestMissingScheduledServicesForScheduledDay = new List<int>();

        Dictionary<int, List<ServiceItem>> moreInfoNeededList = new();

        if (serviceList != null && serviceList.services != null)
        {
            foreach (var service in serviceList.services)
            {
                ScheduledDay? scheduledDay = GetScheduledDay(service.scheduledDayCode);

                if (scheduledDay != null && service.serviceID != null)
                {
                    if (!moreInfoNeededList.Keys.Contains(service.scheduledDayCode))
                        moreInfoNeededList.Add(service.scheduledDayCode, new List<ServiceItem>());

                    // There are quite a few reason codes for service changes, now only handle service deletion separately...
                    var serviceInfoReason = service.reason;
                    var scheduledPlanKey = ScheduledPlan.CreateKey(scheduledDay.ScheduledDayCode, service.name);

                    Log.Information($"===> Service info reason received from TMS: {serviceInfoReason}");    //TODO: remove this!

                    if (serviceInfoReason != eServiceInfoReason.deleted)
                    {
                        if (serviceInfoReason != eServiceInfoReason.tripStateChanged)
                        {
                            if (this.requestedScheduledPlans.ContainsKey(scheduledPlanKey))
                            {
                                // This was info requested by us, need more info for it
                                ScheduledPlan scheduledPlan = this.requestedScheduledPlans[scheduledPlanKey];
                                scheduledPlan.Merge(scheduledDay, service);     // Wait for trip info...
                                moreInfoNeededList[service.scheduledDayCode].Add(service);

                                //TODO: handle allocation????? -> Never has any info about allocation...
                                //if (service.tmsPTI.tripDBID)
                                //    Log.Error($"########################### >>>> Found allocation?: {service.tmsPTI.trackedTrainGUID} -> {service.name}: {service.tmsPTI.serviceDBID}/{service.tmsPTI.tripDBID}");

                                Log.Information($"Service info merged to scheduled plan (day code {service.scheduledDayCode}): {this.requestedScheduledPlans[scheduledPlanKey]}");
                            }
                            else
                            {
                                // This is spontaneous service info from TMS, service was changed some way, need more info for it
                                if (this.ScheduledPlans.ContainsKey(scheduledPlanKey))
                                {
                                    //TODO: what to do actually, trips are requested for the scheduled plan automatically, and they must be handled in TripsUpdated()
                                    // We will merge new service and delete existing trips here, they will be requested again after exiting this function
                                    this.scheduledPlans[scheduledPlanKey].Merge(scheduledDay, service);
                                    this.scheduledPlans[scheduledPlanKey].DeleteTrips();
                                    moreInfoNeededList[service.scheduledDayCode].Add(service);
                                }
                                else
                                {
                                    if (!requestMissingScheduledServicesForScheduledDay.Contains(service.scheduledDayCode))
                                        requestMissingScheduledServicesForScheduledDay.Add(service.scheduledDayCode);
                                    Log.Information($"Service info was received, but scheduled plan for it does not exist: {service.tmsPTI?.serviceGUID}. Requesting scheduled plan...");
                                }
                            }
                        }
                        else
                        {
                            // MockTrip was changed inside service
                            // No need to request data of service again
                        }
                    }
                    else
                    {
                        // Service has been deleted. Delete scheduled plan from our collection
                        // No need to request data of service again
                        if (this.ScheduledPlans.ContainsKey(scheduledPlanKey))
                        {
                            // NOTE! Now work like TMS does: if scheduled service is removed and it is an allocated one, keep it and the allocation to train (otherwise we will all the time receive invalid data from TMS) 
                            if (!IsAllocated(scheduledPlanKey))
                            {
                                ScheduledPlanDeleted(scheduledPlanKey);
                                Log.Information($"Scheduled plan {scheduledPlanKey} deleted!");
                            }
                            else
                            {
                                Log.Warning($"Scheduled plan {scheduledPlanKey} deleted, but it is allocated to train. Keep it and the allocation! (This will change later, if functionality of TMS is changed)");
                            }
                        }
                    }
                }
                else
                {
                    Log.Error($"Scheduled day did not exist: {service.scheduledDayCode}");
                }
            }
        }

        // If we missed even one scheduled service from some schedule day, we have to start asking everything again for that day
        if (requestMissingScheduledServicesForScheduledDay.Count > 0)
        {
            foreach (var dayCode in requestMissingScheduledServicesForScheduledDay)
            {
                string scheduledDayCode = $"{dayCode / 1000}{dayCode % 1000:000}";
                SendScheduledServicesRequest?.Invoke("0", scheduledDayCode);

                if (moreInfoNeededList.Keys.Contains(dayCode))
                    moreInfoNeededList.Remove(dayCode);
            }
        }

        List<ServiceItem> moreInfoList = new();
        foreach (var service in moreInfoNeededList.Values)
            moreInfoList.AddRange(service);

        return moreInfoList;
    }
    public void ReportModel()
    {
        Log.Debug("=> Datahandler report of current trains");
        foreach (var tr in this.trains)
        {
            var train = GetTrain(tr.Key);
            if (train != null)
                Log.Debug($"    Train: {train.ToString()}");
            else
                Log.Debug($"    Train: null, obid = {tr.Key}");
        }

        //ReportScheduledPlans(); //TODO: remove or simplify

        Log.Debug("=> Datahandler report of current timetable allocations");
        foreach (var allocation in this.TimetableAllocations)
        {
            var td = GetTrain(allocation.Key)?.Td;
            Log.Debug($"    Train {td}: {GetScheduledPlan(allocation.Value.Item1)?.Name} - {allocation.Value.Item2}");
        }

        Log.Debug("=> Datahandler report of current route plans");
        foreach (var routePlan in this.TrainRoutePlans)
        {
            var td = GetTrainByGUID(routePlan.Key)?.Td;
            Log.Debug($"    Train {td}: First item: {routePlan.Value.TMSRoutePlan?.data.RoutePlan.Trains.First().Items.First().From.ename} -> {routePlan.Value.TMSRoutePlan?.data.RoutePlan.Trains.First().Items.First().To.ename}");
        }

        Log.Debug("=> Datahandler report of current possessions");
        foreach (var possession in this.Possessions)
        {
            Log.Debug($"    {possession}");
        }
    }
    private void ReportScheduledPlans()
    {
        var sp = scheduledPlans.Values.OrderBy(x => x.ScheduledDayCode).OrderBy(y => y.StartTime.DateTime).ToList();

        Log.Debug("=> Datahandler report of current scheduled services");
        foreach (var scheduledPlan in sp)
        {
            Log.Debug($"     {scheduledPlan.ScheduledDayCode}: {scheduledPlan.Name} ({scheduledPlan.Id}): #trips = {scheduledPlan.Trips.Count}");
        }
    }
    
    #endregion

    #region Train Functions
    public void TrainDataChanged(Train train, TrainData.DescriberWithConsist describerWithConsist)
    {
        // Note: Not all members in describerWithConsist contain meaningful information. For example, timetable information refer to old ES2000 TTS data, not to TMS data

        var requestPending = IsTrainPositionsRequestPending();
        var informChange = false;

        // Train exists, so remove it from list of trains to be removed after refresh, if refreshing
        if (requestPending)
            obidTrainsToDeleteAfterRefresh.Remove(train.Obid);

        ActionTime occurredTime = ActionTime.Now;   // If train position is not changed, this should not be set!

        // Check train movement
        if (ConvertToTrainPosition(train, occurredTime, describerWithConsist, out TrainPosition trainPosition))
        {
            // Store to memory if position is changed

            var positionChanged = RememberTrainPosition(trainPosition);
            //CML
            trainPosition.MyCurrentTrackNameList = GetTrackNameList(trainPosition);
            trainPosition.MyCurrentTrackUidList = GetTrackUidList(trainPosition);

            GlobalDeclarations.MyAutoRoutingManager?.CheckForTrainAllocationFromPosition(JsonConvert.SerializeObject(trainPosition));
            if (GlobalDeclarations.MyEnableSerializeTrain) SerializeTrainPosition(trainPosition);
            //CML

            // Calculate new forecast, check conflicts etc. if train really moved
            if (positionChanged)
            {
                //TODO: conflicts...

                //var estimationPlan = CreateForecast(train);      //TODO: CML should create forecast, this call must be removed when that is done! This is just for testing purposes!

                // Notify clients, if not refreshing
                informChange = !requestPending;
            }
        }

        // Handle other data needed (postfix, ...)
        foreach (var trainProperty in describerWithConsist.Describer.Train.TrainProperty)
        {
            // Postfix
            if (trainProperty.Name == "ctc.ts.trainDescriberPostfix" && train.Postfix != trainProperty.Value)
            {
                train.Postfix = trainProperty.Value;
                informChange = true;
            }

            // Other properties...
        }

        if (informChange)
            NotifyTrainPositionChanged?.Invoke(train, trainPosition);
    }
    public void TrainDeleted(Train train, ActionTime occurredTime)
    {
        if (train != null && train.IsValid())
        {
            // Notify clients, if not refreshing
            if (!IsTrainPositionsRequestPending())
            {
                NotifyTrainDeleted?.Invoke(train, occurredTime);
            }

            DeleteTrain(train);
        }
    }
    public Train? DeleteTrain(string obid)
    {
        Train? train = GetTrain(obid);

        // Delete possible timetable allocation info and route plan
        RemoveAllocationRequest(train);
        UnallocateTimetable(train);
        RemoveRoutePlan(train);

        // Delete estimation plan
        trainEstimationPlans.TryRemove(obid, out _);

        // Delete train
        trainPositions.TryRemove(obid, out _);
        trains.TryRemove(obid, out train);

        if (train != null)
            Log.Information("Deleted train: {0}", train);
        else
            Log.Information($"Train (obid={obid}) did not exist when deleting it");

        return train;
    }
    private void DeleteTrain(Train train)
    {
        if (train != null && train.IsValid())
            DeleteTrain(train.Obid);
    }
    private static bool ConvertToTrainPosition(Train? train, ActionTime occurredTime, TrainData.DescriberWithConsist? describerWithConsist, out TrainPosition trainPosition)
    {
        trainPosition = new();

        if (train != null && occurredTime.IsValid() && describerWithConsist != null)
        {
            try
            {
                var describer = describerWithConsist.Describer;

                if (describer.TrainPositionCore != null && describer.TrainPositionCore.Footprint.Modified)
                {
                    var footprint = describer.TrainPositionCore.Footprint;

                    if (footprint.ElementStr.Count > 0)
                    {
#if true
                        RailgraphLib.Enums.EDirection direction = footprint.EndDirection != TrainData.EDirection.Opposite ? RailgraphLib.Enums.EDirection.dNominal : RailgraphLib.Enums.EDirection.dOpposite;
                        bool offsetIsFromNominalEnd = direction == RailgraphLib.Enums.EDirection.dNominal;

                        ElementPosition tailPos = Service.RailgraphHandler.CreateElementPosition(footprint.ElementStr.First(), footprint.StartDistance, offsetIsFromNominalEnd);
                        ElementPosition headPos = Service.RailgraphHandler.CreateElementPosition(footprint.ElementStr.Last(), footprint.EndDistance, !offsetIsFromNominalEnd);

                        ElementExtension elementExtension = new(tailPos, headPos, footprint.ElementStr.ToList());
#else
                        long tailAdditionalPos = 0; // TODO: get this from RailGraph for tail!
                        long headAdditionalPos = 0; // TODO: get this from RailGraph for head!

                        // TODO : simulated position
                        headAdditionalPos = describer.KmValue;
                        tailAdditionalPos = headAdditionalPos + (long)(footprint.EndDirection == TrainData.EDirection.Nominal ? -30 : 30) * 1000;

                        ElementPosition tailPos = new(footprint.ElementStr.First(), footprint.StartDistance, tailAdditionalPos);
                        ElementPosition headPos = new(footprint.ElementStr.Last(), footprint.EndDistance, headAdditionalPos);

                        ElementExtension elementExtension = new(tailPos, headPos, footprint.ElementStr.ToList());
#endif
                        trainPosition = new TrainPosition(train, elementExtension, occurredTime);

                        // TODO : remove!
                        var action = describerWithConsist.Action;
                        Log.Debug($"Train {describer.Train.Describer} ({action}) - Core footprint : {elementExtension}");
                        var km = describer.KmValue / 1000000.0;
                        Log.Debug($"Head position: msg={km:0.000} km, RG-converted={headPos.AdditionalPos / 1000000.0:0.000} km");
                    }
                }
            }
            catch
            {
            }
        }

        return trainPosition.IsValid();
    }
    private bool RememberTrainPosition(TrainPosition trainPosition)
    {
        // Returns true, if train position is actually changed
        bool changed = false;

        if (trainPosition != null && trainPosition.IsValid() && trainPosition.Train != null)
        {
            // Store to memory
            //TODO: Should we remember route plan action points?
            if (trainPositions.ContainsKey(trainPosition.Obid))
            {
                var oldTrainPosition = trainPositions[trainPosition.Obid];

                changed = trainPosition != oldTrainPosition;
                if (changed)
                {
                    Log.Debug($"OLD TRAIN POS: {oldTrainPosition}");
                    Log.Debug($"NEW TRAIN POS: {trainPosition}");

                    trainPositions[trainPosition.Obid] = trainPosition;
                }
            }
            else
            {
                trainPositions.TryAdd(trainPosition.Train.Obid, trainPosition);
                changed = true;
            }
        }

        return changed;
    }
    private List<string> GetTrackNameList(TrainPosition trainPosition)
    {
        var TrackNames = new List<string>();
        var trackExtension = Service.RailgraphHandler!.CreateTrackExtension(trainPosition.ElementExtension);
        for (int i = 0; i < trackExtension.getExtensionElements().Count; i++)
        {
            var ilElement = Service.RailgraphHandler.ILTopoGraph.getGraphObj(trackExtension.getExtensionElements()[i]);
            if (ilElement != null && ilElement is RailgraphLib.Interlocking.Track)
            {
                TrackNames.Add(ilElement.getName());
                GlobalDeclarations.MyLogger.LogInfo("GetTrackNameList:<" + trainPosition.ElementExtension.StartPos.ElementId + "><" + ilElement.getName() +">");
                //var occ = ilElement.
            }
            // Do whatever you want to do with ilElement data (SYSID, name, …) or the member functions it provides, like isLocked2Route() etc.
        }
        return TrackNames;
    }
    private List<uint> GetTrackUidList(TrainPosition trainPosition)
    {
        var TrackUids = new List<uint>();
        var trackExtension = Service.RailgraphHandler!.CreateTrackExtension(trainPosition.ElementExtension);
        for (int i = 0; i < trackExtension.getExtensionElements().Count; i++)
        {
            var ilElement = Service.RailgraphHandler.ILTopoGraph.getGraphObj(trackExtension.getExtensionElements()[i]);
            if (ilElement != null && ilElement is RailgraphLib.Interlocking.Track)
            {
                TrackUids.Add(ilElement.getId());

            }
            // Do whatever you want to do with ilElement data (SYSID, name, …) or the member functions it provides, like isLocked2Route() etc.
        }
        return TrackUids;
    }
    public Train CreateTrain(string obid, string guid, string ctcId, string td, uint sysid, Train.CtcTrainType type)
    {
        Train train = new(obid, guid, ctcId, td, sysid, type);

        //+++ TEST TEST REMOVE!
        //RailgraphLib.HierarchyObjects.Route? route = GetRailGraphRoute("SIN1_SKU-SIP_SKU");
        //RailgraphLib.HierarchyObjects.Route? route2 = GetRailGraphRoute("SIN_SAU-SIN1_SAU");
        //SetTrainRouteMarking(train, new() { route, route2 });
        //+++

        if (trains.ContainsKey(obid))
        {
            if (trains[obid] != train)
            {
                trains[obid] = train;
                Log.Information("Updated train: {0}", train);
            }
        }
        else
        {
            trains.TryAdd(obid, train);
            Log.Information("Created train: {0}", train);
        }

        return train;
    }
    public Train? GetTrain(string obid)
    {
        if (trains.ContainsKey(obid))
            return trains[obid];

        return null;
    }
    public Train? GetTrainByGUID(string? GUID)
    {
        // Return train by either of the guid set to it
        if (GUID != null && GUID != "")
        {
            foreach (var train in trains.Values)
            {
                if (train.Guid == GUID)
                    return train;
            }

            foreach (var train in trains.Values)
            {
                if (train.AllocatedTrainGuid != null && train.AllocatedTrainGuid == GUID)
                    return train;
            }
        }

        return null;
    }
    public Train? GetTrain(EstimationPlan? estimationPlan)
    {
        if (estimationPlan != null)
            return GetTrain(estimationPlan.Obid);

        return null;
    }
    public Train? GetTrainBySysid(uint sysid)
    {
        foreach (var train in trains.Values)
        {
            if (train.Sysid == sysid)
                return train;
        }

        return null;
    }
    public Train? GetTrainByCtcId(string ctcId)
    {
        foreach (var train in trains.Values)
        {
            if (train.CtcId == ctcId)
                return train;
        }

        return null;
    }
    private List<Train> GetListOfTrains(List<string> obids)
    {
        List<Train> trains = new();
        foreach (var obid in obids)
        {
            var train = GetTrain(obid);
            if (train != null)
                trains.Add(train);
        }

        return trains;
    }
    public void SetTrainPositionsRequested(bool requested)
    {
        lock (trainPositionsRequestTimeout)
        {
            if (requested)
            {
                // Let first request (and probably the only one) decide timeout
                if (!IsTrainPositionsRequestPending())
                {
                    // Remember all existing trains
                    obidTrainsToDeleteAfterRefresh.Clear();
                    foreach (var obid in trains.Keys)
                        obidTrainsToDeleteAfterRefresh.Add(obid);

                    trainPositionsRequestTimeout = ActionTime.Now + TimeSpan.FromSeconds(extTrainPositionsRequestPendingTimeout);
                }
            }
            else
            {
                trainPositionsRequestTimeout.SetTimeInvalid();

                bool clientRequestPending = NotifyTrainPositionsRefreshRequestEnded != null;

                // If client request is pending, this call will update train positions to it
                DeleteTrainsNotInRefresh();

                // Otherwise, we have to send scheduled plans to it as normal dynamic update
                if (!clientRequestPending)
                    SendTrainPositions();
            }
        }
    }
    private void SendTrainPositions()
    {
        foreach (var trainPosition in TrainPositions)
        {
            NotifyTrainPositionChanged?.Invoke(GetTrain(trainPosition.Key), trainPosition.Value);
        }
    }
    public string GetTrainType(int trainTypeId)
    {
        var result = this.TrainTypes?.Where(trainTypeDetail => trainTypeDetail.ID == trainTypeId).ToList();
        if (result?.Count == 1 && result.First().Description != null)
            return result.First().Description;
        return "";
    }
    public int GetTrainDefaultLength(string trainType)
    {
        var result = this.TrainTypes?.Where(trainTypeDetail => trainTypeDetail.Description == trainType).ToList();
        if (result?.Count == 1)
            return result.First().DefaultLength;
        return 0;
    }
    #endregion

    #region Schedule Functions
    private void SendScheduledPlans()
    {
        foreach (var scheduledPlan in ScheduledPlans.Values)
        {
            NotifyScheduledPlanChanged?.Invoke(scheduledPlan);
        }
    }
    private ScheduledPlan ScheduledPlanChanged(ScheduledPlan scheduledPlan)
    {
        try
        {
            if (scheduledPlan.IsValid())
            {
                if (scheduledPlans.ContainsKey(scheduledPlan.Key))
                    scheduledPlans[scheduledPlan.Key] = scheduledPlan;
                else
                    scheduledPlans.TryAdd(scheduledPlan.Key, scheduledPlan);

                // Notify clients, if not refreshing
                if (!IsScheduledPlansRequestPending())
                {
                    NotifyScheduledPlanChanged?.Invoke(scheduledPlan);
                    if (scheduledPlan.IsUpdatedByRefresh())
                    {
                        //Console.Beep();
                    }
                    #region CML
                    //var message = "DataHandler:ScheduledPlanChanged:Schedule Found for Update <" + scheduledPlan?.ScheduledDayCode + "><" + scheduledPlan?.Name + ">";
                    //GlobalDeclarations.MyAutoRoutingManager?.AddLogEvent(message, TrainAutoRoutingManager.AlertLevel.INFORMATION);
                    GlobalDeclarations.MyTrainForecastManager?.UpdateSchedulePlan(scheduledPlan.ScheduledDayCode, scheduledPlan.Name, JsonConvert.SerializeObject(scheduledPlan));
                    #endregion
                }
            }
            return scheduledPlan;

        }
        catch (Exception e)
        {
            var message = "DataHandler:ScheduledPlanChanged:" + e;
            GlobalDeclarations.MyAutoRoutingManager?.AddLogEvent(message, TrainAutoRoutingManager.AlertLevel.ERROR);
        }
        return scheduledPlan;
    }
    public ScheduledPlan ScheduledPlanDeleted(ScheduledPlanKey key)
    {
        ScheduledPlan scheduledPlan = new();

        if (scheduledPlans.ContainsKey(key))
        {
            scheduledPlans.TryGetValue(key, out ScheduledPlan? plan);
            if (plan != null)
                scheduledPlan = plan;

            #region CML
            var message = "DataHandler:ScheduledPlanDeleted:Schedule Found for Deletion <" + plan?.ScheduledDayCode + "><" + plan?.Name + ">";
            GlobalDeclarations.MyAutoRoutingManager?.AddLogEvent(message,TrainAutoRoutingManager.AlertLevel.INFORMATION);
            #endregion

            // Remove scheduled route plan
            if (scheduledRoutePlans.ContainsKey(key))
                scheduledRoutePlans.TryRemove(key, out _);
        }

        // Notify clients, if not refreshing
        if (scheduledPlan.IsValid() && !IsScheduledPlansRequestPending())
        {
            NotifyScheduledPlanDeleted?.Invoke(scheduledPlan);

            #region CML
            GlobalDeclarations.MyTrainForecastManager?.DeleteSchedulePlan(key.ScheduledDayCode,key.ScheduledPlanName);
            #endregion
            RemoveForecast(key);
            scheduledPlans.TryRemove(key, out ScheduledPlan? plan);

        }

        return scheduledPlan;
    }
    private bool FindScheduledPlanAndTripIdForTripCode(string tripCode, out ScheduledPlan scheduledPlan, out int tripId)
    {
        scheduledPlan = new();
        tripId = 0;

        //TODO: Now accept only today's scheduled plans, this surely will be changed when we are working around midnight. Should we take yesterday into consideration, too?
        var today = 0;
        foreach (var sd in scheduledDays.Values)
        {
            if (sd.IsToday())
            {
                today = sd.ScheduledDayCode;
                break;
            }
        }

        if (today != 0)
        {
            foreach (var sp in scheduledPlans.Values)
            {
                if (sp.ScheduledDayCode == today)
                {
                    foreach (var tripItem in sp.Trips)
                    {
                        if (tripItem.Value.TripCode == tripCode)
                        {
                            scheduledPlan = sp;
                            tripId = tripItem.Value.Id;

                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }
    public ScheduledDay? GetScheduledDay(int scheduledDayCode)
    {
        return scheduledDays.ContainsKey(scheduledDayCode) ? scheduledDays[scheduledDayCode] : null;
    }
    public ScheduledPlan? GetScheduledPlan(ScheduledPlanKey? scheduledPlanKey)
    {
        return scheduledPlanKey != null && scheduledPlans.ContainsKey(scheduledPlanKey) ? scheduledPlans[scheduledPlanKey] : null;
    }
    public ScheduledPlan? GetScheduledPlan(int scheduledPlanId)
    {
        foreach (var scheduledPlan in scheduledPlans.Values)
        {
            if (scheduledPlan.Id == scheduledPlanId)
                return scheduledPlan;
        }

        return null;
    }
    public ScheduledDay UpdateScheduledDay(ActionTime startTime, ScheduledDayItem scheduledDayTMS)
    {
        ScheduledDay scheduledDay = new(startTime, scheduledDayTMS);

        if (scheduledDay.IsValid())
        {
            int dc = scheduledDay.ScheduledDayCode;

            if (scheduledDays.ContainsKey(dc))
                scheduledDays[dc] = scheduledDay;
            else
                scheduledDays.TryAdd(dc, scheduledDay);
        }

        return scheduledDay;
    }
    private void DeleteScheduledDay(int scheduledDayCode)
    {
        //TODO: from internal maintenance or from TMS?
    }

    #endregion

    #region Route Marking
    ////////////////////////////////////////////////////////////////////////////////
    //
    // This route marking is implemented by using the train's path property and therefore kept only in here,
    // it may later need to be implemented differently
    //
    ////////////////////////////////////////////////////////////////////////////////

    public class Extent
    {
        public int startDistance { get; set; }
        public int endDistance { get; set; }
        public int usageDir { get; set; }
        public int startDir { get; set; }
        public int endDir { get; set; }
        public List<uint> elements { get; set; }

        public Extent(int startDistance, int endDistance, int usageDir, int startDir, int endDir, List<uint> elements)
        {
            this.startDistance = startDistance;
            this.endDistance = endDistance;
            this.usageDir = usageDir;
            this.startDir = startDir;
            this.endDir = endDir;
            this.elements = elements;
        }
    }
    public class PathSegment
    {
        public string route { get; set; }
        public Extent extent { get; set; }

        public PathSegment(string routeName, RailgraphLib.RailExtension.CoreExtension ext)
        {
            this.route = routeName;
            extent = new(ext.getStartDistance(), ext.getEndDistance(), (int)ext.getUsageDirection(), (int)ext.getStartDirection(), (int)ext.getEndDirection(), ext.getExtensionElements().ToList());
        }
    }
    public class RouteMarking
    {
        public List<PathSegment> pathSegments => _pathSegments;

        private List<PathSegment> _pathSegments = new();

        public RouteMarking(List<Tuple<string, List<RailgraphLib.RailExtension.CoreExtension>>> markings)
        {
            foreach (var marking in markings)
            {
                foreach (var coreExtension in marking.Item2)
                    _pathSegments.Add(new PathSegment(marking.Item1, coreExtension));
            }
        }
    }
    public void SetTrainRouteMarking(Train train, List<RailgraphLib.HierarchyObjects.Route> routes)
    {
        try
        {
            List<Tuple<string, List<RailgraphLib.RailExtension.CoreExtension>>> markings = new();

            foreach (var route in routes)
            {
                List<RailgraphLib.RailExtension.CoreExtension> coreExtensions = Service!.RailgraphHandler!.GetCoreExtensionsOfRouteTracks(route);
                markings.Add(new(route.SysName, coreExtensions));
            }

            string json = "";

            if (markings.Count > 0)
            {
                var routeMarking = new RouteMarking(markings);
                json = JsonConvert.SerializeObject(routeMarking);
            }

            NotifyTrainPropertySet?.Invoke(train, new TrainProperty("ctc.cmm.train.pathStatus", json, TrainProperty.EPropertyType.ptString) { Valid = json != "" });
        }
        catch { }
    }
    public void SetNextForecastedRoutes(string trainOBID, List<Tuple<DateTime, string?>> nextRoutes)
    {
        SetNextForecastedRoutes(GetTrain(trainOBID), nextRoutes);
    }
    public void SetNextForecastedRoutes(Train? train, List<Tuple<DateTime /*UTC*/, string /*route name*/>> nextRoutes)
    {
        List<Tuple<ActionTime, string>> nextTimeConvertedRoutes = new();

        foreach (var nextRoute in nextRoutes)
            nextTimeConvertedRoutes.Add(new Tuple<ActionTime, string>(new(nextRoute.Item1), nextRoute.Item2));

        SetNextForecastedRoutes(train, nextTimeConvertedRoutes);
    }
    public void SetNextForecastedRoutes(Train? train, List<Tuple<ActionTime, string>> nextRoutes)
    {
        // This is now just checking the 5 minute rule for route marking, we have no trigger points for route marking anywhere
        if (train != null)
        {
            List<RailgraphLib.HierarchyObjects.Route> routeMarkingsToSet = new();
            bool routeMarkingsToQueue = false;

            var now = ActionTime.Now;
            foreach (var nextRoute in nextRoutes)
            {
                RailgraphLib.HierarchyObjects.Route route = GetRailGraphRoute(nextRoute.Item2);
                if (route != null)
                {
                    if (nextRoute.Item1 <= now + new TimeSpan(0, 0, 5, 0))
                        routeMarkingsToSet.Add(route);
                    else
                        routeMarkingsToQueue = true;
                }
            }

            SetTrainRouteMarking(train, routeMarkingsToSet);    // Call this even if no items in list!

            lock (this.trainRouteMarkings)
            {
                if (routeMarkingsToQueue)
                {
                    if (this.trainRouteMarkings.Keys.Contains(train.Obid))
                        this.trainRouteMarkings[train.Obid] = nextRoutes;
                    else
                        this.trainRouteMarkings.Add(train.Obid, nextRoutes);
                }
                else
                    this.trainRouteMarkings.Remove(train.Obid);
            }
        }
    }

    // This function is called when ROS informs that route is set/executed
    public void RouteSetInfoReceived(Train train, string routeName)
    {
        #region CML
        GlobalDeclarations.MyAutoRoutingManager?.UpdateRouteExecutionComplete(train.Sysid.ToString(), train.Obid, routeName);
        var message = "RouteSetInfoReceived:<" + train.Sysid + "><" + routeName + ">";
        GlobalDeclarations.MyAutoRoutingManager?.AddLogEvent(message, TrainAutoRoutingManager.AlertLevel.INFORMATION);
        #endregion

        // Now we will remove it from our collection for test purposes
        List<Tuple<ActionTime, string>> markings = new();
        lock (this.trainRouteMarkings)
        {
            if (this.trainRouteMarkings.Keys.Contains(train.Obid))
                markings = this.trainRouteMarkings[train.Obid];
        }
        for (var marking = 0; marking < markings.Count; marking++)
        {
            if (markings[marking].Item2 == routeName)
            {
                markings.RemoveRange(marking, 1);
                SetNextForecastedRoutes(train, markings);
                break;
            }
        }
    }
    #endregion
    
    #region Possessions
    public Possession PossessionChanged(string id, string description, ElementPosition startPos, ElementPosition endPos, ActionTime startTime, ActionTime endTime, string active)
    {
        Possession possession = new(id, description, startPos, endPos, startTime, endTime, active);

        if (possession.IsValid())
        {
            // Store to memory
            if (possessions.ContainsKey(possession.Id))
                possessions[possession.Id] = possession;
            else
                possessions.TryAdd(possession.Id, possession);

            // Notify clients, if not refreshing
            if (possession.IsValid() && !IsPossessionsRequestPending())
            {
                NotifyPossessionChanged?.Invoke(possession);
            }
        }

        #region CML
        GlobalDeclarations.MyAutoRoutingManager?.CheckToAddPossession(JsonConvert.SerializeObject(possession));
        var message = "PossessionChanged:Possession Added: " + possession.Description; 
        GlobalDeclarations.MyAutoRoutingManager?.AddLogEvent(message,TrainAutoRoutingManager.AlertLevel.INFORMATION);
        SerializePossession(possession);
        #endregion

        return possession;
    }
    public Possession PossessionDeleted(string id)
    {
        Possession possession = new();

        if (possessions.ContainsKey(id))
        {
            possessions.TryRemove(id, out Possession? poss);
            if (poss != null)
                possession = poss;
        }

        // Notify clients, if not refreshing
        if (possession.IsValid() && !IsPossessionsRequestPending())
        {
            NotifyPossessionDeleted?.Invoke(possession);
            #region CML
            GlobalDeclarations.MyAutoRoutingManager?.CheckToRemovePossession(JsonConvert.SerializeObject(possession));
            var message = "PossessionChanged:Possession Added: " + possession.Description;
            GlobalDeclarations.MyAutoRoutingManager?.AddLogEvent(message, TrainAutoRoutingManager.AlertLevel.INFORMATION);
            #endregion

        }

        return new Possession();
    }
    
    #endregion

    #region Timetable Functions
    private void StoreTimetableProperties(ScheduledPlan scheduledPlan)
    {
        if (scheduledPlan != null && this.cassandraSession != null)
        {
            try
            {
                var scheduledDayCode = scheduledPlan.ScheduledDayCode;
                var serviceName = scheduledPlan.Name;

                var Q = (string text) =>
                {
                    return "\"" + text + "\"";
                };

                // Cassandra does not understand JSON array properly, can not use it
                // And even map is impossible to generate with Utf8JsonWriter so that Cassandra would understand it as a map
                // Do it by hand...
                // update cms.timetableproperties set services = services + fromJson('{"70":{"trips":{"T7001":{"trainlength":100000,"delays":[{"platform":"","delayseconds":200,"departure":true}],"properties":[]},"T7002":{"trainlength":4000,"delays":[{"platform":"","delayseconds":800,"departure":true}],"properties":[]}},"properties":[]}}') where scheduleddaycode=1;

                string serviceJson = "{" + Q(serviceName) + ":{" + Q("trips") + ":{";
                bool tripAdded = false;

                foreach (var trip in scheduledPlan.Trips.Values)
                {
                    var tripCode = trip.TripCode;

                    if (tripCode != null && tripCode != "")
                    {
                        var trainLength = trip.TrainLength;
                        var delaySeconds = trip.DelaySeconds;   // Only one delay with trip is supported now, although DB supports more!

                        // Has to add trip properties to DB, even if they are not set currently. They may have been set previously...
                        if (tripAdded)
                            serviceJson += ",";

                        serviceJson += Q(tripCode) + ":{" + Q("trainlength") + ":" + trainLength.ToString() + ",";
                        serviceJson += Q("delays") + ":[{" + Q("platform") + ":" + Q("") + "," + Q("delayseconds") + ":" + delaySeconds.ToString() + "," + Q("departure") + ":true}],";
                        serviceJson += Q("properties") + ":[]}";   // No properties yet, maybe later
                        tripAdded = true;
                    }
                }

                serviceJson += "}," + Q("properties") + ":[]}";   // No properties yet, maybe later
                serviceJson += "}";

                string statement = "update cms.timetableproperties set services = services + fromJson(?) where scheduleddaycode=?";

                var preparedStatement = this.cassandraSession.Prepare(statement);
                var boundStatement = preparedStatement.Bind(serviceJson, scheduledDayCode);

                // First try with configured consistency level, if that fails, drop consistency level to One
                try
                {
                    boundStatement.SetConsistencyLevel(this.cassandraConsistencyLevel);
                    this.cassandraSession.Execute(boundStatement);
                }
                catch (Cassandra.UnavailableException)
                {
                    boundStatement.SetConsistencyLevel(ConsistencyLevel.One);
                    this.cassandraSession.Execute(boundStatement);
                }

                Log.Information($"Inserted timetable properties into DB: {scheduledPlan.Name}");
            }
            catch (Exception ex)
            {
                Log.Error($"Timetable properties insertion into DB failed: {scheduledPlan.Name}: {ex.Message}");
            }
        }
    }
    private void LoadTimetableProperties()
    {
        if (this.cassandraSession != null)
        {
            ulong count = 0;
            DateTime start = DateTime.Now;

            Log.Information("Reading timetable properties from DB");

            try
            {
                // Read whole rows as JSON strings
                string statement = "select json scheduleddaycode,services from timetableproperties";

                var rowset = this.cassandraSession.Execute(statement);

                if (rowset != null)
                {
                    start = DateTime.Now;

                    foreach (var row in rowset.ToArray())
                    {
                        var json = row.GetValue<string>("[json]");

                        var doc = JsonSerializer.Deserialize<JsonDocument>(json);

                        if (doc != null)
                        {
                            try
                            {
                                var root = doc.RootElement;

                                var scheduledDayCode = root.GetProperty("scheduleddaycode").GetInt32();
                                var services = root.GetProperty("services");
                                if (services.ValueKind == JsonValueKind.Object)
                                {
                                    foreach (var service in services.EnumerateObject())
                                    {
                                        var serviceName = service.Name;
                                        var trips = service.Value.GetProperty("trips");
                                        var serviceProperties = service.Value.GetProperty("properties"); // Nothing here yet!

                                        if (trips.ValueKind == JsonValueKind.Object)
                                        {
                                            foreach (var trip in trips.EnumerateObject())
                                            {
                                                var tripCode = trip.Name;
                                                var trainLength = trip.Value.GetProperty("trainlength").GetInt32();
                                                var delays = trip.Value.GetProperty("delays");
                                                var tripProperties = service.Value.GetProperty("properties"); // Nothing here yet!

                                                if (delays.ValueKind == JsonValueKind.Array)
                                                {
                                                    foreach (var tripDelay in delays.EnumerateArray())
                                                    {
                                                        // We'll only have one item in delays now, because only trip delay is supported
                                                        // 'platform' is "" and 'isDeparture' is 'true' (see StoreTimetableProperties())
                                                        var platform = tripDelay.GetProperty("platform").GetString();
                                                        var delaySeconds = tripDelay.GetProperty("delayseconds").GetInt32();
                                                        var isDeparture = tripDelay.GetProperty("departure").GetBoolean();

                                                        // Create trip property only, when one or more of the properties are set
                                                        if (trainLength != 0 || delaySeconds != 0)
                                                        {
                                                            var key = new TripPropertyKey(scheduledDayCode, serviceName, tripCode);
                                                            this.tripProperties.Add(key, new TripProperty(key) { DelaySeconds = delaySeconds, TrainLength = trainLength });
                                                            count++;
                                                        }

                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                // Parsing of one scheduled day in DB failed, have to discard it
                                Log.Error(ex.Message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Reading timetable properties from DB failed: {0}", ex.Message);
            }

            Log.Information("{0} timetable properties read in {1} seconds", count, (DateTime.Now - start).TotalSeconds);
        }
    }
    public void TimetableUpdateStarted()
    {
        Log.Information($"========================");
        Log.Information($"Timetable update started");
        Log.Information($"========================");
    }
    public void TimetableUpdateComplete()
    {
        ScheduledPlan scheduledPlanForTrain = null;

        try
        {
            Log.Information($"=================================");
            Log.Information($"Timetable/service update complete");
            Log.Information($"=================================");

            // We have to check, if timetable allocations have now changed from normal to spare trips, or vice-versa
            // This affects the route plan sending and indication of train in CTC track picture
            // MockTrip may have also been deleted...
            List<string> obidsUnallocated = new();
            foreach (var allocation in this.timetableAllocations)
            {
                Train? train = GetTrain(allocation.Key);
                if (train != null)
                {
                    scheduledPlanForTrain = GetScheduledPlan(allocation.Value.Item1);
                    var trip = scheduledPlanForTrain?.GetTripByTripId(allocation.Value.Item2);

                    if (trip != null)
                        PerformTripStateChangeOperations(train, trip);
                    else
                        obidsUnallocated.Add(train.Obid);
                }
            }

            foreach (var obid in obidsUnallocated)
            {
                Train? train = GetTrain(obid);
                UnallocateTimetable(train);
                //RemoveRoutePlan(train); // Remove route plan, because we don't have trip any more
            }

            //Request route plan for all existing scheduled plan, to be on the safe side after timetable updates
            foreach (var scheduledPlan in this.scheduledPlans.Values)
            {
                Service?.RosMessaging?.SendScheduledRoutePlanRequest(scheduledPlan);
                Log.Information($"Sent scheduled route plan request for scheduled plan {scheduledPlan.Name}/{scheduledPlan.Id}, daycode {scheduledPlan.ScheduledDayCode}");
            }
            //if (scheduledPlan != null)
            //{
            //    Service?.RosMessaging?.SendScheduledRoutePlanRequest(scheduledPlan);
            //    Log.Information($"Sent scheduled route plan request for scheduled plan {scheduledPlan.Name}/{scheduledPlan.Id}, daycode {scheduledPlan.ScheduledDayCode}");
            //}
            DeliverTimetableInfo();

            //akk TEST TEST REMOVE+++
            //ScheduledPlanKey key = new(2024082, "04");
            //ScheduledPlan plan = GetScheduledPlan(key);
            //if (plan != null)
            //{
            //    var trip = plan.Trips.Last().Value;
            //    if (trip != null)
            //    {
            //        CreateForecast(plan, new List<int>() { trip.Id });
            //    }
            //    RemoveForecast(plan);
            //}

        }
        catch (Exception e)
        {
            GlobalDeclarations.MyLogger?.LogException(e.ToString());
        }

    }
    private void DeliverTimetableInfo()
    {
        // Log values to be sent to TS for train adaptation describer proposition purposes (do not log allocated scheduled plans, even though they ARE sent to TS!)
        // This code is to be removed later
        foreach (var scheduledPlan in scheduledPlans.Values)
        {
            var scheduledDay = GetScheduledDay(scheduledPlan.ScheduledDayCode);
            if (scheduledDay != null && scheduledDay.IsToday()) // <- yesterday and/or tomorrow?
            {
                Log.Debug($"{scheduledPlan.Name}: {scheduledDay.AsDateString()} {scheduledPlan.StartTime.ToLocalTimeString()}-{scheduledPlan.EndTime.ToLocalTimeString()}");
                foreach (var trip in scheduledPlan.Trips.Values)
                {
                    // If timetable trip is allocated to train, do not use it (actually should probably not use any trip from the same service, tsched does not support it?)
                    if (GetListOfTrains(this.timetableAllocations.Keys.ToList()).Select(train => train.Td == trip.TripCode).ToList().Count == 0)
                        Log.Debug($"    {trip.TripNumber}: '{trip.TripCode}' ({trip.Name}): {trip.StartTime.ToLocalTimeString()} - {trip.StartPos.AdditionalName} ({trip.StartPos.ElementId}:{trip.StartPos.Offset}) -> {trip.EndPos.AdditionalName} ({trip.EndPos.ElementId}:{trip.EndPos.Offset})");
                }
            }
        }

        // Send timetables to CTC etc.
        NotifyTimetableInfoChanged?.Invoke();
    }
    public void ConnectTimetableToTrain(string guid, string describer, uint sysid)
    {
        // GUID may be missing from train, we may have to wait for train to get it. We remember allocation request and will 
        Train? train = GetTrainByGUID(guid);
        if (train == null)
            train = GetTrainBySysid(sysid);

        if (train != null)
        {
            if (FindScheduledPlanAndTripIdForTripCode(describer, out ScheduledPlan scheduledPlan, out int tripId))
            {
                // Check if trip is in allowed allocation window (now hard-coded -6 .. +8 hours)
                bool allowedToAllocate = false;
                ActionTime utcTimeNow = ActionTime.Now;
                Trip? trip = scheduledPlan.GetTripByTripId(tripId);
                bool isSpareTrip = false;
                if (trip != null)
                {
                    var tripStartTime = trip.StartTime + new TimeSpan(0, 0, 0, trip.DelaySeconds);
                    allowedToAllocate = tripStartTime > (utcTimeNow - new TimeSpan(0, 6, 0, 1)) && tripStartTime < utcTimeNow + new TimeSpan(0, 8, 0, 1);
                    isSpareTrip = trip.IsSpareTrip;
                }

                if (allowedToAllocate && SendConnectTimetableToTrain != null)
                {
                    // If train is currently allocated to timetable, we have to unallocate it for new allocation to succeed in TMS
                    Train? allocatedTrain = GetTimetableAllocatedTrainByObid(train.Obid);
                    if (allocatedTrain != null)
                    {
                        SendDisconnectTimetableFromTrain?.Invoke(allocatedTrain);
                        UnallocateTimetable(allocatedTrain);
                    }

                    lock (this.timetableAllocationRequests)
                    {
                        this.timetableAllocationRequests.Add(train.Obid, new Tuple<ActionTime, ScheduledPlanKey, int>(ActionTime.Now, scheduledPlan.Key, tripId));
                    }

                    if (train.Guid != null)
                    {
                        Log.Information($"==> ConnectTimetableToTrain: Sending connection request: train={train}, scheduledPlan={scheduledPlan}, tripNumber={tripId}");
                        SendConnectTimetableToTrain!(train, scheduledPlan, tripId);

                        // Train allocated to inactive (spare) trip?
                        if (isSpareTrip)
                            Service?.SendEvent(train, new CtcEvent("ConflictManagementService.AllocatedTimetableIsInactive") { Str2 = scheduledPlan.Name });
                    }
                }
                else
                {
                    // Send event about failed timetable allocation
                    Service?.SendEvent(train, new CtcEvent("ConflictManagementService.TimetableAllocationFailed") { Str2 = scheduledPlan.Name });
                    Log.Error($"Trip does not start in allowed time window or messaging callback does not exist when trying to connect scheduledPlan {scheduledPlan} to train {train}. Request to connect timetable discarded!");
                }
            }
            else
            {
                // Send event about failed timetable allocation
                Service?.SendEvent(train, new CtcEvent("ConflictManagementService.TimetableAllocationFailed") { Str2 = describer });
                Log.Error($"Trip (trip code={describer}) does not exist when trying to connect timetable to train {train}");
            }
        }
        else
        {
            /*
                [14:44:23 INF] Received train command 'Set' or 'Replace' ended message
                [14:44:23 INF] --> sysid=260103, guid=Lenovo-P350b51c2d1f-b91e-488c-bb57-c7119e96da75, describer=T7001, connectToTimetable=True
                [14:44:23 INF] Connecting guid=Lenovo-P350b51c2d1f-b91e-488c-bb57-c7119e96da75, describer=T7001 to timetable
                [14:44:23 ERR] Train (guid=Lenovo-P350b51c2d1f-b91e-488c-bb57-c7119e96da75) does not exist when trying to connect timetable T7001 to it
                [14:44:23 ERR] Existing trains:
                [14:44:23 ERR]     Train [Train3, [Obid=Train3, Guid=Lenovo-P350f42e1ddd-dd0b-4fe0-882f-3cd7c6f2df3e, CtcId=Train3], Td=T7001, CtcType=Train, Postfix=, AllocatedTrainGuid=A:72d59dea-d2ce-4e13-abd4-170a4142e0d4]
            */
            Log.Error($"Train (guid={guid}, sysid={sysid}) does not exist when trying to connect timetable {describer} to it");
            Log.Error($"Existing trains:");
            foreach (var existingTrain in this.trains)
                Log.Error($"    Train {existingTrain}");
        }
    }
    public void TrainConnectionToTimetableFailed()
    {
        // 2023-02-03: We don't use this any more, because now we try allocation several times and the request may fail at first (Kigas Set -command, in particular). Let timeout handle all failures
        // Impossible to know, which connection request failed, but we probably have only one allocation requested. If we have more, let timeout handle failure
        //lock (this.timetableAllocationRequests)
        //{
        //    if (this.timetableAllocationRequests.Count == 1)
        //    {
        //        TimetableAllocationRequestFailed(this.timetableAllocationRequests.First().Key);
        //    }
        //}
    }
    private void TimetableAllocationRequestFailed(string obid)
    {
        Train? train = GetTrain(obid);
        if (train != null)
        {
            var scheduledPlan = GetScheduledPlan(this.timetableAllocationRequests[obid].Item2);

            if (scheduledPlan != null)
            {
                Log.Error($"Timetable allocation failed for train {train.Td} ({train.Guid}): {scheduledPlan.Name}, trip ID {this.timetableAllocationRequests[obid].Item3}");

                // Send event about failed timetable allocation
                Service?.SendEvent(train, new CtcEvent("ConflictManagementService.TimetableAllocationFailed") { Str2 = scheduledPlan.Name });
            }
        }

        RemoveAllocationRequest(train);
    }
    public int GetTripIdOfAllocatedTimetable(Train train, ScheduledPlan scheduledPlan)
    {
        // Given scheduled plan must also match currently allocated one to get trip ID! (Maybe this method should be in calling class...)
        int tripId = 0;

        try
        {
            if (this.timetableAllocations.ContainsKey(train.Obid) && this.timetableAllocations[train.Obid].Item1 == scheduledPlan.Key)
                tripId = this.timetableAllocations[train.Obid].Item2;
        }
        catch (Exception) { }

        return tripId;
    }
    public int GetTripNumberOfAllocatedTimetable(Train train, ScheduledPlan scheduledPlan)
    {
        // Given scheduled plan must also match currently allocated one to get trip number! (Maybe this method should be in calling class...)
        int tripNumber = 0;

        if (this.timetableAllocations.ContainsKey(train.Obid) && this.timetableAllocations[train.Obid].Item1 == scheduledPlan.Key)
            tripNumber = scheduledPlan.GetTripNumberByTripId(this.timetableAllocations[train.Obid].Item2);

        return tripNumber;
    }
    public bool IsAllocated(ScheduledPlanKey key, int tripId = 0)
    {
        foreach (var allocation in this.timetableAllocations.Values)
        {
            if (allocation.Item1 == key && (tripId == 0 || allocation.Item2 == tripId))
                return true;
        }

        return false;
    }
    public Train? GetTimetableAllocatedTrainByObid(string obid)
    {
        if (this.timetableAllocations.ContainsKey(obid))
            return GetTrain(obid);

        return null;
    }
    private void AllocateTimetable(Train train, ScheduledPlan scheduledPlan, int tripId)
    {
        UnallocateTimetable(train); // To be sure!

        Trip? trip = scheduledPlan.GetTripByTripId(tripId);
        if (trip == null)
            return;

        RemoveAllocationRequest(train);

        string tripCode = "";
        int tripDayCode = 0;
        int tripUid = trip.Id;
        if (scheduledPlan.HasTripWithTripId(tripId))
        {
            tripCode = trip.TripCode;
            tripDayCode = trip.ScheduledDayCode;
        }

        // Add timetable allocation
        this.timetableAllocations.Add(train.Obid, new Tuple<ScheduledPlanKey, int>(scheduledPlan.Key, tripId));
        Log.Information($"==== TIMETABLE ALLOCATED ====> Scheduled plan '{scheduledPlan.Id}/{scheduledPlan.Name}', tripId={tripId}/{tripCode} connected to train {train}");

        // Set train type, so it is faster to get it when informing movements to TDGS
        var trainType = train.TrainType = GetTrainType(scheduledPlan.TrainTypeId);

        //CML
        //var sysId = train.Sysid;
        //GlobalDeclarations.MyAutoRoutingManager?.CheckForTrainAllocation(train.Td, tripUid, trainType);
        GlobalDeclarations.MyAutoRoutingManager?.CheckForTrainAllocation(tripCode, tripId, trainType, tripDayCode);
        GlobalDeclarations.MyLogger?.LogInfo("DataHandler:AllocateTimetable: Trip <" + tripCode + "> TripDayCode <" + tripDayCode + ">  Trip UID <" + tripId +">");
        //SerializeTrainPosition(trainPosition);
        //CML

        // Send event to CTC
        Service?.SendEvent(train, new CtcEvent("ConflictManagementService.TimetableAllocated") { Str2 = train.Td });

        PerformTripStateChangeOperations(train, trip);

        // Deliver timetable info to CTC
        DeliverTimetableInfo();
    }
    private void UnallocateTimetable(Train? train)
    {
        if (train != null)
        {
            // Use only OBID here, because in train removal case, other identifiers may have been removed or changed already
            var allocatedTrain = GetTimetableAllocatedTrainByObid(train.Obid);
            if (allocatedTrain != null)
            {
                if (this.timetableAllocations.ContainsKey(allocatedTrain.Obid))
                {
                    // Send event to CTC
                    Service?.SendEvent(allocatedTrain, new CtcEvent("ConflictManagementService.TimetableUnallocated") { Str2 = allocatedTrain.Td });

                    this.timetableAllocations.Remove(allocatedTrain.Obid);
                    Log.Information($"==== TIMETABLE UNALLOCATED ====> Disconnected timetable from train {train}");
                    //CML
                    //Do Deallocate train
                    GlobalDeclarations.MyAutoRoutingManager?.PerformDeallocateTrain(train.Sysid.ToString());
                    //CML
                }

                train.TrainType = "";

                //TODO: Remove forecast etc...
                RemoveForecast(train);

                //RemoveRoutePlan(train); // NOTE: Route plan should not be removed when unallocating timetable. Unallocation inside CMS happens with various reasons and re-allocation probably is done soon, but route plan is only sent once from TMS. There should be request for route plan in TMS!

                NotifyTrainPropertySet?.Invoke(train, new TrainProperty("ConflictManagementService.AllocatedTimetableIsInactive", "false", TrainProperty.EPropertyType.ptBoolean) { Valid = false });

                // Deliver timetable info to CTC
                DeliverTimetableInfo();
            }
        }
    }
    private void RemoveAllocationRequest(Train? train)
    {
        if (train != null)
        {
            lock (this.timetableAllocationRequests)
            {
                if (this.timetableAllocationRequests.ContainsKey(train.Obid))
                    this.timetableAllocationRequests.Remove(train.Obid, out _);
            }
        }
    }
    public void UpdateTrainOrTimetableData(PlainTrainList plainTrainList)
    {
        foreach (var trainListTrain in plainTrainList.trains)
        {
            if (trainListTrain.tmsPTI != null)
            {
                // Timetable connection changes TMS train to tracked train
                // When timetable is disconnected, TMS train changes back to train?
                string? trackedTrainGuid = trainListTrain.tmsPTI.trackedTrainGUID;
                string? trainGuid = trainListTrain.tmsPTI.trainGUID;

                // Do we have this train?
                Train? train = GetTrainByGUID(trackedTrainGuid);
                if (train != null)
                {
                    // Remember allocated train GUID
                    train.AllocatedTrainGuid = trainGuid;

                    int scheduledDayCode = trainListTrain.tmsPTI.serviceDayCode;
                    string? serviceGUID = trainListTrain.tmsPTI.serviceGUID;
                    string? tripGUID = trainListTrain.tmsPTI.tripGUID;

                    int scheduledPlanId = serviceGUID == null || serviceGUID == "" ? 0 : int.Parse(serviceGUID);
                    int tripId = tripGUID == null || tripGUID == "" ? 0 : int.Parse(tripGUID);
                    string scheduledPlanName = "";

                    if (scheduledPlanId != 0)
                    {
                        foreach (var plan in ScheduledPlans)
                        {
                            if (plan.Key.ScheduledDayCode == scheduledDayCode && plan.Value.Id == scheduledPlanId)
                            {
                                scheduledPlanName = plan.Value.Name;
                                break;
                            }
                        }
                    }

                    //TODO: Should we always start by removing current timetable connection, because it is now practically repeated in every branch below?
                    // Maybe not, if the connection remains the same as before, because we have calculated forecast based on timetable connected

                    if (scheduledPlanName != "")
                    {
                        ScheduledPlanKey key = new(scheduledDayCode, scheduledPlanName);

                        if (ScheduledPlans.ContainsKey(key) && ScheduledPlans[key].ScheduledDayCode == scheduledDayCode)
                        {
                            ScheduledPlan scheduledPlan = ScheduledPlans[key];
                            if (scheduledPlan.HasTripWithTripId(tripId))
                            {
                                // Now we consider timetable to be connected to train. If there is already different connection to this train, it will be replaced by new one
                                bool changed = false;

                                // Check if connection has indeed changed
                                if (this.timetableAllocations.ContainsKey(train.Obid))
                                {
                                    var timetableConnection = this.timetableAllocations[train.Obid];
                                    if (timetableConnection != null)
                                    {
                                        changed = key != timetableConnection.Item1 || tripId != timetableConnection.Item2;
                                    }
                                }
                                else
                                    changed = true;

                                if (changed)
                                {
                                    AllocateTimetable(train, scheduledPlan, tripId); // Will handle unallocation and forecast!
                                }
                            }
                            else
                            {
                                // Service is connected, but no trip as of now. We consider timetable as disconnected, but stop sending allocation requests. TMS will allocate to trip on its own time
                                Log.Information($"Scheduled plan '{scheduledPlanId}', tripId={tripId} received, but trip not found from plan: train {train}");
                                UnallocateTimetable(train);
                                RemoveAllocationRequest(train);
                            }
                        }
                        else
                        {
                            // No scheduled plan found, bad thing...
                            Log.Error($"Scheduled plan '{scheduledPlanId}', tripId={tripId} does not exist when parsing plain train list of train {train}");
                            UnallocateTimetable(train);
                        }
                    }
                    else
                    {
                        // No scheduled plan given, what does this mean...
                        Log.Error($"ServiceGUID '{scheduledPlanId}', tripId={tripId} empty when parsing plain train list of train {train}");
                        UnallocateTimetable(train);
                    }
                }
                else
                {
                    // Do we have this train?
                    train = GetTrainByGUID(trainGuid);
                    if (train != null)
                    {
                        // This is normal timetable disconnection from TMS
                        UnallocateTimetable(train);
                    }
                }
            }
        }
    }

    #endregion

    #region Trip Functions
    private void PerformTripStateChangeOperations(Train train, Trip trip)
    {
        /* Removed, because automatic change from inactive trip to active trip implemented in tscheduler
        // Train allocated to inactive (spare) trip?
        if (trip.IsSpareTrip)
            NotifyTrainPropertySet?.Invoke(train, new TrainProperty("ConflictManagementService.AllocatedTimetableIsInactive", "true", TrainProperty.EPropertyType.ptBoolean, TrainProperty.EAlarmLevel.atAlarm));
        else
            NotifyTrainPropertySet?.Invoke(train, new TrainProperty("ConflictManagementService.AllocatedTimetableIsInactive", "false", TrainProperty.EPropertyType.ptBoolean, TrainProperty.EAlarmLevel.atNoAlarm) { Valid = false });
        */

        // Try sending route plan to ROS (it may not exist yet, or has already been sent)
        //TrySendingRoutePlanToROS(train);
    }
    public void TripPropertyChangeRequested(string scheduledDayCode, string scheduledPlanName, string tripCode, int trainLength, int delaySeconds)
    {
        try
        {
            TripProperty tripProperty;
            bool removeProperty = trainLength == 0 && delaySeconds == 0;

            int dayCode = int.Parse(scheduledDayCode);
            var key = TripProperty.CreateKey(dayCode, scheduledPlanName, tripCode);

            if (this.tripProperties.ContainsKey(key))
            {
                tripProperty = this.tripProperties[key];
            }
            else
            {
                tripProperty = new TripProperty(key);
                this.tripProperties.Add(key, tripProperty);
            }

            tripProperty.TrainLength = trainLength;
            tripProperty.DelaySeconds = delaySeconds;

            NotifyTripPropertyChanged?.Invoke(tripProperty);

            #region CML
            GlobalDeclarations.MyAutoRoutingManager?.UpdateForecastOnDelay(delaySeconds,tripCode,scheduledDayCode);
            GlobalDeclarations.MyAutoRoutingManager?.UpdateTrainLength(trainLength, tripCode, scheduledDayCode);
            #endregion

            // Find scheduled plan and store properties of it to persistent memory
            // This has to be done, even if trip property is cleared
            var scheduledPlanKey = ScheduledPlan.CreateKey(dayCode, scheduledPlanName);
            if (this.ScheduledPlans.ContainsKey(scheduledPlanKey))
            {
                StoreTimetableProperties(this.ScheduledPlans[scheduledPlanKey]);

                //var estimationPlan = CreateForecast(this.ScheduledPlans[scheduledPlanKey]);     //TODO: This call must be removed when CML creates the forecast. This is just for testing purposes!
            }

            // Remove from internal memory, if there's no trip property any more
            if (removeProperty)
                this.tripProperties.Remove(key);
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
    }
    public TripProperty? GetTripProperty(TripPropertyKey key)
    {
        if (this.tripProperties.ContainsKey(key))
            return this.tripProperties[key];

        return null;
    }
    public ScheduledPlan TripsUpdated(ScheduledPlanKey? scheduledPlanKey, TripList tripList)
    {
        ScheduledPlan? scheduledPlan = null;

        if (tripList != null && tripList.trips != null)
        {
            if (scheduledPlanKey == null)
            {
                try
                {
                    var plan = GetScheduledPlan(int.Parse(tripList.serviceID));
                    if (plan != null)
                        scheduledPlanKey = new(plan.ScheduledDayCode, plan.Name);
                }
                catch (Exception) { }
            }

            if (scheduledPlanKey != null)
            {
                string? serviceId = tripList.serviceID;

                if (serviceId != null)
                {
                    bool requestedByUs = this.requestedScheduledPlans.ContainsKey(scheduledPlanKey);
                    if (requestedByUs)
                    {
                        scheduledPlan = this.requestedScheduledPlans[scheduledPlanKey];
                    }
                    else if (scheduledPlans.ContainsKey(scheduledPlanKey))
                    {
                        scheduledPlan = scheduledPlans[scheduledPlanKey];
                    }

                    if (scheduledPlan != null)
                    {
                        foreach (var tripItem in tripList.trips)
                        {
                            var scheduledDay = GetScheduledDay(scheduledPlan.ScheduledDayCode);
                            if (scheduledDay != null)
                            {
                                var trip = scheduledPlan.Add(scheduledDay, tripItem);
                                if (trip.IsValid())
                                    Log.Information($"Trip added to scheduled plan (scheduled day {scheduledDay.AsDateString()}) (scheduled time {scheduledDay.StartTime.AsDateTime()}) {scheduledPlan.Name} ({scheduledPlan.Id}): trip code = '{trip.TripCode}', active = {!trip.IsSpareTrip}");
                                else
                                    Log.Error($"Trip addition to scheduled plan (scheduled day {scheduledDay.AsDateString()}) {scheduledPlan.Name} ({scheduledPlan.Id}) failed with invalid trip: trip ID = {tripItem.tripID}");
                            }
                            else
                                Log.Error($"Scheduled day referred to in scheduled plan {scheduledPlan.Name} missing: {scheduledPlan.ScheduledDayCode}");
                        }

                        if (scheduledPlan.IsValid())
                        {
                            ScheduledPlanChanged(scheduledPlan);

                            if (requestedByUs)
                                requestedScheduledPlans.Remove(scheduledPlanKey);
                        }
                        else
                        {
                            Log.Error($"Trip info addition to scheduled plan failed: {scheduledPlan}");
                        }
                    }
                    else
                    {
                        Log.Error($"The following scheduled plan did not exist: {serviceId}");
                    }
                }
                else if (tripList.trips.Count == 0)
                {
                    Log.Error($"No service ID and no trips in trip list");
                }
                else
                {
                    Log.Error($"Service ID in message is null");
                }
            }
            else
            {
                Log.Error($"Matching trip request or scheduled plan not found");
            }
        }

        return scheduledPlan == null ? new() : scheduledPlan;
    }
    
    #endregion

    #region Route Plan/PreTest/ROS Functions
    public void PretestResultReceived(int pretestId, bool success, IRosMessaging.PretestResult? result)
    {

        if (result != null)
        {
            if (result.RejectInfo != null && result.RejectInfo.Count() > 0)
            {
                string obj = result.RejectInfo.First().Item1;
                RosRfnaCode rfna = result.RejectInfo.First().Item2;
                string severity = result.RejectInfo.First().Item3;
                Log.Error($"############ Pretest result: Id={pretestId}, success={success}, has result: Id={result.PretestId}, success={result.Success}, rejectInfo#={result.RejectInfo.Count()}, first: obj={obj}, rfna={rfna}, severity={severity}");
            }
            else
                Log.Error($"############ Pretest result: Id={pretestId}, success={success}, has result: Id={result.PretestId}, success={result.Success}, no reject info");
        }
        else
            Log.Error($"############ Pretest result: Id={pretestId}, success={success}, no result");

        if (result != null)
            GlobalDeclarations.MyAutoRoutingManager?.AddPretestResult(result.PretestId, result.Success, result?.RejectInfo.ToString());
    }
    public void RoutePlanReceivedFromTMS(XSD.RoutePlan.rcsMsg msg)
    {
        // Create or update route plans according to information in message
        // Note: There can be several train route plans in one message, but in practice there is only one train every time
        //       We should be prepared for this, but now the simple code below assumes, that there is only one train there
        //       Otherwise we should split the TMS message to pieces and re-assemble it later (which we must do anyway later with conflict management)
        // In service route plan, that was requested by us, the train information does not exist or is empty

        if (msg.data.RoutePlan.Trains != null && msg.data.RoutePlan.Trains.Count() == 1)
        {
            var routePlanTrain = msg.data.RoutePlan.Trains.First();
            var guid = routePlanTrain.GUID; // This is GUID for allocated train or scheduled train

            // Service route plan does not have these
            var ctcId = routePlanTrain.CTCID;
            var trackedTrainGuid = routePlanTrain.TrackedGUID;

            bool isServiceRoutePlan = (ctcId == null || ctcId == "") && (trackedTrainGuid == null || trackedTrainGuid == "");

            if (isServiceRoutePlan)
            {
                // Create service route plan and remember it
                var scheduledPlanId = routePlanTrain.serid;
                var scheduledPlan = GetScheduledPlan(scheduledPlanId);

                if (scheduledPlan != null)
                {
                    var scheduledPlanKey = scheduledPlan.Key;
                    var schedulePlanInfo = @"Name " + scheduledPlan.Name + " ID " + scheduledPlan.Id + " daycode " + scheduledPlan.ScheduledDayCode;

                    if (this.scheduledRoutePlans.ContainsKey(scheduledPlanKey))
                    {
                        this.scheduledRoutePlans[scheduledPlanKey].UpdateTMSRoutePlan(msg);
                    }
                    else
                    {
                        ScheduledRoutePlan scheduledRoutePlan = new ScheduledRoutePlan(scheduledPlanKey, msg);
                        var planAdded = this.scheduledRoutePlans.TryAdd(scheduledPlanKey, scheduledRoutePlan);
                        if (!planAdded) Log.Information($"Received scheduled route plan for scheduled plan " + schedulePlanInfo + " NOT ADDED");

                    }

                    //CML
                    GlobalDeclarations.MyTrainForecastManager?.AddPlanFromDataHandler(JsonConvert.SerializeObject(scheduledPlan));
                    if (GlobalDeclarations.MyEnableSerializeRoutePlan) SerializeSchedulePlan(scheduledPlan);
                    //CML

                    Log.Information($"Received scheduled route plan for scheduled plan " + schedulePlanInfo);
                    //TestRoute(routePlanTrain, guid, ctcId, trackedTrainGuid);
                    //CML
                    //CML 
                    if (GlobalDeclarations.MyEnableSerializeRoutePlan) SerializeRoutePlan(routePlanTrain, "", schedulePlanInfo);
                    //CML

                    //CML
                }
                else
                {
                    // This service route plan is for plan we don't have in our collection. Why, because we have requested it by ourselves...
                    Log.Error($"Received scheduled route plan for unexisting scheduled plan: SerN={routePlanTrain.SerN}, serid={routePlanTrain.serid}, TripId={routePlanTrain.TripID}, TrackedGUID={routePlanTrain.TrackedGUID}");
                    return;
                }
            }
            else
            {
                // Create train route plan and remember it
                Train? train = GetTrainByGUID(trackedTrainGuid);
                if (train == null)
                    train = GetTrainByGUID(guid);

                if (train != null && ctcId == train.CtcId)
                {
                    // TEST-TEST-TEST pretest reachability and availability with (possible) first route from route plan (log as Error to find it better from log)

                    try
                    {
                        foreach (var action in routePlanTrain.Items.First().MasterRoute.First().Actions)
                        {
                            if (action is XSD.RoutePlan.RCA RCA)
                            {
                                var command = RCA.Command.First().cmd;
                                var routeName = RCA.Command.First().value;
                                foreach (var route in Service.RailgraphHandler.HierarchyRelations.Routes)
                                {
                                    if (route.SysName == routeName)
                                    {
                                        //Service.RosMessaging.PretestRouteReachable(10, train, route, command, PretestResultReceived);
                                        //Service.RosMessaging.PretestRouteAvailable(11, train, route, command, PretestResultReceived);
                                        ////Log.Error($"############ Pretest reachability (10) and availability (11) requests sent: route={route.SysName}, command={command}");
                                    }
                                }
                            }
                        }
                    }
                    catch { }

                    // TEST-TEST-TEST pretest reachability and availability with (possible) first route from route plan

                    // Create route plan and remember it
                    if (this.trainRoutePlans.ContainsKey(train.Guid))
                    {
                        this.trainRoutePlans[train.Guid].UpdateTMSRoutePlan(msg);
                    }
                    else
                    {
                        RoutePlan routePlan = new RoutePlan(train.Guid, msg);
                        this.trainRoutePlans.TryAdd(train.Guid, routePlan);
                    }

                    // Try sending it to ROS. It may not be sent, if timetable is not allocated to train, or allocated timetable is spare one
                    //TrySendingRoutePlanToROS(train);
                }
                else
                {
                    Log.Error($"DataHandler: No train for route plan found: ctcId={ctcId}, guid={guid}, trackedTrainGuid={trackedTrainGuid}");
                }
            }
        }
        else
        {
            Log.Error($"DataHandler: No train(s) in route plan");
        }
    }
    public void TestRoute(XSD.RoutePlan.Train routePlanTrain, string guid, string ctcId, string trackedTrainGuid)
    {
        try
        {
            Train? train = GetTrainByGUID(trackedTrainGuid);
            if (train == null)
                train = GetTrainByGUID(guid);

            //if (train != null && ctcId == train.CtcId)
            //{
            // TEST-TEST-TEST pretest reachability and availability with (possible) first route from route plan (log as Error to find it better from log)

            foreach (var action in routePlanTrain.Items.First().MasterRoute.First().Actions)
            {
                if (action is XSD.RoutePlan.RCA RCA)
                {
                    var command = RCA.Command.First().cmd;
                    var routeName = RCA.Command.First().value;
                    foreach (var route in Service.RailgraphHandler.HierarchyRelations.Routes)
                    {
                        if (route.SysName == routeName)
                        {
                            //Service.RosMessaging.PretestRouteReachable(10, train, route, command,
                            //    PretestResultReceived);
                            //Service.RosMessaging.PretestRouteAvailable(11, train, route, command,
                            //    PretestResultReceived);
                            //Log.Error(
                            //    $"############ Pretest reachability (10) and availability (11) requests sent: route={route.SysName}, command={command}");
                        }
                    }
                }
            }

            // TEST-TEST-TEST pretest reachability and availability with (possible) first route from route plan

            // Create route plan and remember it
            if (this.trainRoutePlans.ContainsKey(train.Guid))
            {
                //this.trainRoutePlans[train.Guid].UpdateTMSRoutePlan(msg);
            }
            else
            {
                //RoutePlan routePlan = new RoutePlan(train.Guid, msg);
                //this.trainRoutePlans.TryAdd(train.Guid, routePlan);
            }

            // Try sending it to ROS. It may not be sent, if timetable is not allocated to train, or allocated timetable is spare one
            //TrySendingRoutePlanToROS(train);
            //}
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            //throw;
        }
    }
    public ScheduledRoutePlan? GetScheduledRoutePlan(ScheduledPlanKey scheduledPlanKey)
    {
        if (this.scheduledRoutePlans.ContainsKey(scheduledPlanKey))
            return this.scheduledRoutePlans[scheduledPlanKey];

        return null;
    }
    private void TrySendingRoutePlanToROS(Train train)
    {
        // We have route plan for train?
        RoutePlan? routePlan = null;
        if (this.trainRoutePlans.ContainsKey(train.Guid))
            routePlan = this.trainRoutePlans[train.Guid];

        // If active timetable has been allocated to train, send whole route plan to ROS (like RoutePlanService) does, if not done before
        if (routePlan != null && timetableAllocations.ContainsKey(train.Obid))
        {
            var scheduledPlan = GetScheduledPlan(timetableAllocations[train.Obid].Item1);
            var trip = scheduledPlan?.GetTripByTripId(timetableAllocations[train.Obid].Item2);
            if (!trip!.IsSpareTrip)
            {
                if (!GlobalDeclarations.MyEnableAutomaticConflictResolution)
                {
                    Service?.RosMessaging?.SendRoutePlan(routePlan);
                    routePlan.SentToROS = true;
                    Log.Information("DataHandler:TrySendingRoutePlanToROS: Sent COMPLETE route plan to ROS <" + trip.TripCode + ">");
                }
            }
        }
    }
    public void CancelRoutePlanReceivedFromTMS(XSD.CancelRoutePlan.rcsMsg msg)
    {
        //TODO: What does this do to internal model?
        //...probably remove route plan, but what else?
        foreach (var routedTrain in msg.data.CancelRoutePlan.Trains)
        {
            var train = GetTrainByGUID(routedTrain.TrackedGUID);
            RemoveRoutePlan(train);
        }

        //TODO: Work as RoutePlanService, send cancel route plan immediately to ROS!
        Service.RosMessaging.SendCancelRoutePlan(msg);
        Log.Information($"DataHandler: Sent cancel route plan to ROS");
    }
    private void RemoveRoutePlan(Train? train)
    {
        //TODO: Route plan should be removed from ROS or does ROS do it by itself, probably does, when train is deleted...
        //TODO: We don't have any means to remove route plan right now, we should generate message for it like it was received from TMS
        //TODO: TMS probably does the removal, if allocation has been removed
        if (train != null)
        {
            if (this.TrainRoutePlans.ContainsKey(train.Guid))
                this.TrainRoutePlans.TryRemove(train.Guid, out _);

            //TODO: what else (inform conflict management, ...)
        }
    }
    
    #endregion

    #region Forecast

    // Create forecast based on trip allocated to train. Train position, delays(s) and conflict resolutions must be taken into account
    // Forecast may affect following trips in scheduled plan, in which case forecast for those particular trips should be created by another CreateForecast method
    private EstimationPlan? CreateForecast(Train train)
    {
        //TODO: Create real forecast, not just this simple and not very accurate one based on allocated timetable and train position
        if (train != null && this.timetableAllocations.ContainsKey(train.Obid))
        {
            if (this.TrainPositions.ContainsKey(train.Obid))
            {
                var trainPosition = this.TrainPositions[train.Obid];
                var headKmValue = trainPosition?.ElementExtension?.EndPos.AdditionalPos;
                var tailKmValue = trainPosition?.ElementExtension?.StartPos.AdditionalPos;
                RailgraphLib.Enums.EDirection dir = trainPosition.Direction;

                ScheduledPlan scheduledPlan = GetScheduledPlan(this.timetableAllocations[train.Obid].Item1);
                if (scheduledPlan != null)
                {
                    var tripId = this.timetableAllocations[train.Obid].Item2;

                    ActionTime now = ActionTime.Now;
                    List<TimedLocation> timedLocations = new();
                    bool tripFound = false;
                    bool platformFound = false;
                    TimeSpan? totalDelay = null;
                    ElementPosition? prevPos = trainPosition?.ElementExtension?.EndPos;
                    ActionTime prevDeparture = ActionTime.Now;

                    foreach (var trip in scheduledPlan.Trips.Values)
                    {
                        if (!tripFound && trip.Id == tripId)
                            tripFound = true;

                        if (tripFound)
                        {
                            var tripDelay = trip.DelaySeconds;  // Only single trip delay now!
                            bool firstPlatformInTrip = true;
                            int amountTimedLocations = trip.TimedLocations.Count;
                            int currentTimedLocation = 0;

                            foreach (var tripLocation in trip.TimedLocations)
                            {
                                currentTimedLocation++;

                                var platform = tripLocation.Pos;
                                var platformKmValue = platform.AdditionalPos;
                                var arrival = tripLocation.Arrival;
                                var departure = tripLocation.Departure;
                                bool lastPlatformInTrip = currentTimedLocation == amountTimedLocations;

                                if (firstPlatformInTrip)
                                    prevPos = new ElementPosition();

                                if (!platformFound)
                                {
                                    RailgraphLib.RailExtension.TrackExtension? trackExtension = Service.RailgraphHandler.CreateTrackExtension(trainPosition.ElementExtension);
                                    RailgraphLib.GraphObj? platformTrack = Service.RailgraphHandler.GetILElementOnElementPosition(platform);

                                    if (trackExtension != null && platformTrack != null)
                                    {
                                        if (firstPlatformInTrip && trackExtension.getExtensionElementsRaw().Contains(platformTrack.getId()))
                                            platformFound = true;
                                        else if (lastPlatformInTrip && trackExtension.getExtensionElementsRaw().Contains(platformTrack.getId()))
                                            platformFound = true;
                                    }
                                    if (!platformFound && ((dir == RailgraphLib.Enums.EDirection.dNominal && platformKmValue >= headKmValue) || (dir == RailgraphLib.Enums.EDirection.dOpposite && platformKmValue <= headKmValue)))
                                        platformFound = true;
                                }

                                if (totalDelay == null && platformFound)
                                {
                                    // Calculate delay from currently allocated trip
                                    var scheduledTripDelaySeconds = (int)(now - (arrival.IsValid() ? arrival : departure)).TotalSeconds;
                                    totalDelay = new TimeSpan(0, 0, 0, (int)scheduledTripDelaySeconds + tripDelay);
                                    if (prevPos.IsValid() && prevDeparture.IsValid())
                                        totalDelay += new TimeSpan(0, 0, 0, (int)(Math.Abs((double)platformKmValue - (double)headKmValue) / Math.Abs((double)platformKmValue - (double)prevPos.AdditionalPos) * (departure - prevDeparture).TotalSeconds));
                                }

                                // TODO: CML conflict resolution "delays" must be added to forecast

                                if (totalDelay != null)
                                {
                                    var description = tripLocation.Description;
                                    var id = tripLocation.Id;
                                    var stopping = tripLocation.HasStopping;
                                    arrival += (TimeSpan)totalDelay;
                                    departure += (TimeSpan)totalDelay;

                                    // TODO: Create TimedLocation with occurred arrivals and departures in CML!
                                    TimedLocation timedLocation = new(description, platform, arrival, departure, stopping)
                                    {
                                        Id = id,
                                        TripId = trip.Id,
                                        TripName = trip.TripCode,
                                         ArrivalOccurred = false,
                                        DepartureOccurred = false
                                    };
                                    timedLocations.Add(timedLocation);
                                }
                                else
                                {
                                    prevPos = platform;
                                    prevDeparture = departure;
                                }

                                firstPlatformInTrip = false;
                            }
                        }
                    }

                    EstimationPlan estimationPlan = new EstimationPlan(train.Obid, train.Td, timedLocations, new ElementExtension()) { ScheduledPlanKey = scheduledPlan.Key }; //TODO: Train's path should be updated from route plan of train, when it is received from TMS!

                    if (estimationPlan.IsValid())
                    {
                        EstimationPlanChanged(train, estimationPlan);    // TODO: This call in DataHandler must be done from CML with populated estimation plan to update forecast to TDGS!
                        return estimationPlan;
                    }
                }
            }
        }

        return null;
    }
    // Create forecast based on scheduled plan times and trip delays. Returned estimation plan does not have any link to train!
    // If tripIDsToCreate is null, forecast is created to all trips in scheduled plan, otherwise only to trips, whose ID is in the list
    // For example, if first trip in scheduled plan is allocated to train, forecast for that is created by another CreateForecast() method,
    // and that trip is not to be included in list. Also, if first trip is already "passed" (in history or next trip is allocated to train)
    // that should not be included in list. Little bit complicated, but we shouldn't forecast "passed" trips.
    private EstimationPlan? CreateForecast(ScheduledPlan scheduledPlan, List<int>? tripIDsToForecast = null)
    {
        if (scheduledPlan != null)
        {
            List<TimedLocation> timedLocations = new();
            TimeSpan totalDelay = new(0, 0, 0, 0);

            foreach (var trip in scheduledPlan.Trips.Values)
            {
                if (tripIDsToForecast == null || tripIDsToForecast.Contains(trip.Id))
                {
                    var tripDelaySeconds = trip.DelaySeconds;  // Only single trip delay now!
                    totalDelay += new TimeSpan(0, 0, 0, tripDelaySeconds);

                    // TODO: CML conflict resolution "delays" must be added to forecast

                    foreach (var tripLocation in trip.TimedLocations)
                    {
                        // TODO: Create TimedLocation with occurred arrivals and departures in CML!
                        TimedLocation timedLocation = new(tripLocation.Description, tripLocation.Pos, tripLocation.Arrival + totalDelay, tripLocation.Departure + totalDelay, tripLocation.HasStopping)
                        {
                            Id = tripLocation.Id,
                            TripId = trip.Id,
                            TripName = trip.TripCode,
                            ArrivalOccurred = false,
                            DepartureOccurred = false
                        };
                        timedLocations.Add(timedLocation);
                    }
                }
            }

            EstimationPlan estimationPlan = new EstimationPlan(timedLocations) { ScheduledPlanKey = scheduledPlan.Key };

            EstimationPlanChanged(estimationPlan);    // TODO: This call in DataHandler must be done from CML with populated estimation plan to update forecast to TDGS!
            return estimationPlan;
        }

        return null;
    }
    private void RemoveForecast(Train train)
    {
            // TODO: This call must be done from CML to remove forecast from CMS/TDGS!
            //Looks like this is being called from unallocate TRIP SO THIS SHOULD BE WORKING FINE
            EstimationPlan estimationPlan = EstimationPlanDeleted(train.Obid);

            //if (estimationPlan.IsValid())
            //{
            //    //TODO: Delete forecast from tscheduler (what does this actually mean? Going back to original timetable times as forecast?)
            //    // Don't do anything now!
            //}

    }
    private void RemoveForecast(ScheduledPlan scheduledPlan)
    {
        // TODO: This call must be done from CML to remove forecast from CMS/TDGS!
        // Removal of forecasts of all scheduled plan trips are performed!
        EstimationPlan estimationPlan = EstimationPlanDeleted(scheduledPlan.Key);

        //if (estimationPlan.IsValid())
        //{
        //    //TODO: Delete forecast from tscheduler (what does this actually mean? Going back to original timetable times as forecast?)
        //    // Don't do anything now!
        //}
    }
    private void RemoveForecast(ScheduledPlanKey scheduledPlanKey)
    {
        // TODO: This call must be done from CML to remove forecast from CMS/TDGS!
        // Removal of forecasts of all scheduled plan trips are performed!
        EstimationPlan estimationPlan = EstimationPlanDeleted(scheduledPlanKey);

        //if (estimationPlan.IsValid())
        //{
        //    //TODO: Delete forecast from tscheduler (what does this actually mean? Going back to original timetable times as forecast?)
        //    // Don't do anything now!
        //}
    }
    public void EstimationPlanChanged(Train train, EstimationPlan estimationPlan)
    {
        if (estimationPlan.IsValid())
        {
            if (trainEstimationPlans.ContainsKey(train.Obid))
                trainEstimationPlans[train.Obid] = estimationPlan;
            else
                trainEstimationPlans.TryAdd(train.Obid, estimationPlan);

            // Notify clients
            NotifyEstimationPlanChanged?.Invoke(estimationPlan);
        }
    }
    public void EstimationPlanChanged(EstimationPlan estimationPlan)
    {
        if (estimationPlan.IsValid())
        {
            if (estimationPlans.ContainsKey(estimationPlan.ScheduledPlanKey))
                estimationPlans[estimationPlan.ScheduledPlanKey] = estimationPlan;
            else
                estimationPlans.TryAdd(estimationPlan.ScheduledPlanKey, estimationPlan);

            // Notify clients
            NotifyEstimationPlanChanged?.Invoke(estimationPlan);
        }
    }
    // This is mainly for removal of old estimations from time-distance graph, CMS calculates estimations (forecasts) for all existing schedule plans, allocated or not (by request)
    public EstimationPlan EstimationPlanDeleted(string obid)
    {
        EstimationPlan estimationPlan = new();

        if (trainEstimationPlans.ContainsKey(obid))
        {
            trainEstimationPlans.TryRemove(obid, out EstimationPlan? plan);
            if (plan != null)
            {
                estimationPlan = plan;
                #region CML
                var message = "DataHandler:EstimationPlanDeleted:Forecast Found for Deletion <" + plan?.Obid + ">";
                GlobalDeclarations.MyAutoRoutingManager?.AddLogEvent(message, TrainAutoRoutingManager.AlertLevel.INFORMATION);
                #endregion
            }
            else
            {
                #region CML
                var message = "DataHandler:EstimationPlanDeleted:Forecast NOT Found for Deletion <" + obid + ">";
                GlobalDeclarations.MyAutoRoutingManager?.AddLogEvent(message, TrainAutoRoutingManager.AlertLevel.INFORMATION);
                #endregion
            }
        }

        // Notify clients
        if (estimationPlan.IsValid())
        {
            NotifyEstimationPlanDeleted?.Invoke(estimationPlan);
            #region CML
            var message = "DataHandler:EstimationPlanDeleted:Forecast Valid for Deletion <" + estimationPlan?.Obid + ">";
            GlobalDeclarations.MyAutoRoutingManager?.AddLogEvent(message, TrainAutoRoutingManager.AlertLevel.INFORMATION);
            #endregion
        }
        else
        {
            #region CML
            var message = "DataHandler:EstimationPlanDeleted:Forecast NOT Valid for Deletion <" + estimationPlan?.Obid + ">";
            GlobalDeclarations.MyAutoRoutingManager?.AddLogEvent(message, TrainAutoRoutingManager.AlertLevel.INFORMATION);
            #endregion
        }

        return estimationPlan;
    }
    public EstimationPlan EstimationPlanDeleted(ScheduledPlanKey scheduledPlanKey)
    {
        EstimationPlan estimationPlan = new();

        if (estimationPlans.ContainsKey(scheduledPlanKey))
        {
            estimationPlans.TryRemove(scheduledPlanKey, out EstimationPlan? plan);
            if (plan != null)
                estimationPlan = plan;
        }

        // Notify clients
        if (estimationPlan.IsValid())
        {
            NotifyEstimationPlanDeleted?.Invoke(estimationPlan);
            #region CML
            var message = "DataHandler:EstimationPlanDeleted:Forecast Valid for Deletion <" + estimationPlan?.Obid + ">";
            GlobalDeclarations.MyAutoRoutingManager?.AddLogEvent(message, TrainAutoRoutingManager.AlertLevel.INFORMATION);
            #endregion
        }
        else
        {
            #region CML
            var message = "DataHandler:EstimationPlanDeleted:Forecast NOT Valid for Deletion <" + estimationPlan?.Obid + ">";
            GlobalDeclarations.MyAutoRoutingManager?.AddLogEvent(message, TrainAutoRoutingManager.AlertLevel.INFORMATION);
            #endregion
        }
        return estimationPlan;
    }
    #endregion

    #region Station/Platform Functions
    private void StoreStationPriority(Station station)
    {
        if (station != null && this.cassandraSession != null)
        {
            try
            {
                // NOTE: we don't need separate UPDATE statement, because we are not using "IF NOT EXISTS" in CQL.
                // Insert either inserts new or replaces existing line in table
                string statement = "insert into stationpriorities (stationid,date,priority) values (?,?,?)";
                var preparedStatement = this.cassandraSession.Prepare(statement);
                var boundStatement = preparedStatement.Bind(station.StationId, DateTime.UtcNow, (int)station.StationPriority);

                // First try with configured consistency level, if that fails, drop consistency level to One
                try
                {
                    boundStatement.SetConsistencyLevel(this.cassandraConsistencyLevel);
                    this.cassandraSession.Execute(boundStatement);
                }
                catch (Cassandra.UnavailableException)
                {
                    boundStatement.SetConsistencyLevel(ConsistencyLevel.One);
                    this.cassandraSession.Execute(boundStatement);
                }

                Log.Information($"Inserted station priority into DB: {station}: {station.StationPriority}");
            }
            catch (Exception ex)
            {
                Log.Error($"Station priority insertion into DB failed: {station}: {station.StationPriority}: {ex.Message}");
            }
        }
    }
    private void LoadStationPriorities()
    {
        if (this.cassandraSession != null)
        {
            ulong count = 0;
            DateTime start = DateTime.Now;

            Log.Information("Reading station priorities from DB");

            try
            {
                // Read whole rows as JSON strings
                string statement = "select json stationid,date,priority from stationpriorities";

                var rowset = this.cassandraSession.Execute(statement);

                if (rowset != null)
                {
                    start = DateTime.Now;

                    foreach (var row in rowset.ToArray())
                    {
                        var json = row.GetValue<string>("[json]");

                        var doc = JsonSerializer.Deserialize<JsonDocument>(json);

                        if (doc != null)
                        {
                            try
                            {
                                var root = doc.RootElement;

                                var stationId = root.GetProperty("stationid").GetString();
                                var station = GetStation(stationId);

                                if (station != null)
                                {
                                    var priority = (Station.Priority)root.GetProperty("priority").GetInt32();

                                    // Date is not needed now, just read it for later use
                                    ActionTime changeTime = new();
                                    var s = root.GetProperty("date").GetString();

                                    // Take only needed part of time and remove Z which would cause localtime conversion!
                                    s = s?[..19];

                                    if (s != null)
                                        changeTime.InitFromFormat(s, "yyyy-MM-dd HH:mm:ss");

                                    // Set station priority
                                    station.StationPriority = priority;

                                    count++;
                                }
                            }
                            catch (Exception ex)
                            {
                                // Parsing of one station priority in DB failed, has to discard it
                                Log.Error(ex.Message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Loading station priorities from DB failed: {0}", ex.Message);
            }

            Log.Information("{0} station priorities read in {1} seconds", count, (DateTime.Now - start).TotalSeconds);
        }
    }
    public ElementPosition GetElementPositionOfPlatform(string platformName)
    {
        foreach (var station in this.Stations.Values)
        {
            foreach (var platform in station.CTCStation.Platforms)
            {
                if (platform.SysName == platformName)
                {
                    return Service.RailgraphHandler.GetElementPositionOfPlatform(platform);
                }
            }
        }

        return new ElementPosition();
    }
    public void CreateStationsAndPlatforms(StationList? stationList)
    {
        if (stationList != null && stationList.stations != null)
        {
            foreach (var station in stationList.stations)
            {
                // Get station from list and add needed information from TMS there (if any)
                //stations.Add(station.id, station);

                //TODO: remove below logging later
#if false
                Log.Information($"  Station {station.name}, id {station.id}, schedule name {station.scheduleName}");

                foreach (var platform in station.platforms)
                {
                    Log.Information($"    Platform {platform.name}, id {platform.id}, schedule name {platform.scheduleName}, node ID {platform.nodeId}");

                    foreach (var line in platform.lines)
                    {
                        Log.Information($"      Line {line.lineID}, location seqno {line.locationSeqno}");
                    }
                }
#endif
            }
        }
    }
    public Station? GetStation(string? stationId)
    {
        if (stationId != null && stations.ContainsKey(stationId))
            return stations[stationId];
        return null;
    }
    public void StationPriorityChangeRequested(string stationId, Station.Priority priority)
    {
        var station = GetStation(stationId);
        if (station != null)
        {
            station.StationPriority = priority;
            StoreStationPriority(station);

            NotifyStationPriorityChanged?.Invoke(station);
        }
    }
    public void UpdateMovementTemplates(MovementTemplateList movementTemplateList)
    {
        try
        {
            foreach (var movementTemplate in movementTemplateList.movementTemplates)
            {
                Movement movement = new(movementTemplate);

                if (this.movements.ContainsKey(movementTemplate.movementTemplateID))
                    this.movements[movementTemplate.movementTemplateID] = movement;
                else
                    this.movements.TryAdd(movementTemplate.movementTemplateID, movement);

                #region CML
                GlobalDeclarations.MyRailwayNetworkManager?.AddMovementTemplate(JsonConvert.SerializeObject(movementTemplate));
                #endregion
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
    }
    public List<string> GetAllowedMovementPlatforms(string fromStationName, string fromPlatformName, string toStationName)
    {
        List<string> allowedMovementPlatforms = new();

        foreach (var movement in this.movements.Values)
        {
            if (movement.IsFromTo(fromStationName, fromPlatformName, toStationName))
                allowedMovementPlatforms.Add(movement.ToName!);
        }

        return allowedMovementPlatforms;
    }
    #endregion

    #region CML Cassandra Functions
    public class ConflictInfo
    {
        public string MyGuid { get; set; }
        public string? MyEntity { get; set; }
        public string? MyTypeOfConflict { get; set; }
        public string? MyResolution { get; set; }
        public string? MyLocation { get; set; }
        public string? MyLocationDetail { get; set; }
        public string? MySubtypeOfConflict { get; set; }
        public string? MyTripDayCode { get; set; }
        public string? MyTripName { get; set; }
        public string? IsRejected { get; set; }
        public string? IsAccepted { get; set; }
        public string? MyOtherConflictGuid { get; set; }

        public ConflictInfo(string MyGuid)
        {
            this.MyGuid = MyGuid;
        }

        public override string ToString()
        {
            return $"{MyGuid}, {MyEntity}, {MyTypeOfConflict}, {MyResolution}, {MyLocation}, {MyLocationDetail}, {MySubtypeOfConflict}, {MyTripDayCode}, {MyTripName}, {IsRejected}, {IsAccepted}, {MyOtherConflictGuid}";
        }
    }
    private void StoreConflictInfo(ConflictInfo ci)
    {
        if (ci.MyGuid != null && this.cassandraSession != null)
        {
            try
            {
                // NOTE: we don't need separate UPDATE statement, because we are not using "IF NOT EXISTS" in CQL.
                // Insert either inserts new or replaces existing line in table
                string statement = "insert into conflictinfo (myguid,myentity,mytypeofconflict,myresolution,mylocation,mylocationdetail,mysubtypeofconflict,mytripdaycode,mytripname,isrejected,isaccepted,myotherconflictguid) values (?,?,?,?,?,?,?,?,?,?,?,?)";
                var preparedStatement = this.cassandraSession.Prepare(statement);
                var boundStatement = preparedStatement.Bind(ci.MyGuid, ci.MyEntity, ci.MyTypeOfConflict, ci.MyResolution, ci.MyLocation, ci.MyLocationDetail, ci.MySubtypeOfConflict, ci.MyTripDayCode, ci.MyTripName, ci.IsRejected, ci.IsAccepted, ci.MyOtherConflictGuid);

                // First try with configured consistency level, if that fails, drop consistency level to One
                try
                {
                    boundStatement.SetConsistencyLevel(this.cassandraConsistencyLevel);
                    this.cassandraSession.Execute(boundStatement);
                }
                catch (Cassandra.UnavailableException)
                {
                    boundStatement.SetConsistencyLevel(ConsistencyLevel.One);
                    this.cassandraSession.Execute(boundStatement);
                }

                Log.Information($"Inserted conflict info into DB: {ci}");
            }
            catch (Exception ex)
            {
                Log.Error($"Conflict info insertion into DB failed: {ci}: {ex.Message}");
            }
        }
    }
    private void RemoveConflictInfo(string MyGuid)
    {
        if (this.cassandraSession != null)
        {
            try
            {
                string statement = "delete from conflictinfo where myguid=?";

                var preparedStatement = this.cassandraSession.Prepare(statement);
                var boundStatement = preparedStatement.Bind(MyGuid);
                this.cassandraSession.Execute(boundStatement);

                Log.Information($"Deleted conflict info from DB: {MyGuid}");
            }
            catch (Exception ex)
            {
                Log.Error($"Conflict info deletion from DB failed: {MyGuid}: {ex.Message}");
            }
        }
    }
    private void LoadConflictInfo(out List<ConflictInfo> conflicts)
    {
        conflicts = new();

        if (this.cassandraSession != null)
        {
            ulong count = 0;
            DateTime start = DateTime.Now;

            Log.Information("Reading conflict info from DB");

            try
            {
                // Read whole rows as JSON strings
                string statement = "select json myguid,myentity,mytypeofconflict,myresolution,mylocation,mylocationdetail,mysubtypeofconflict,mytripdaycode,mytripname,isrejected,isaccepted,myotherconflictguid from conflictinfo";

                var rowset = this.cassandraSession.Execute(statement);

                if (rowset != null)
                {
                    start = DateTime.Now;

                    foreach (var row in rowset.ToArray())
                    {
                        var json = row.GetValue<string>("[json]");

                        var doc = JsonSerializer.Deserialize<JsonDocument>(json);

                        if (doc != null)
                        {
                            try
                            {
                                var root = doc.RootElement;

                                string? MyGuid = root.GetProperty("myguid").GetString();

                                if (MyGuid != null)
                                {
                                    var conflict = new ConflictInfo(MyGuid)
                                    {
                                        MyEntity = root.GetProperty("myentity").GetString(),
                                        MyTypeOfConflict = root.GetProperty("mytypeofconflict").GetString(),
                                        MyResolution = root.GetProperty("myresolution").GetString(),
                                        MyLocation = root.GetProperty("mylocation").GetString(),
                                        MyLocationDetail = root.GetProperty("mylocationdetail").GetString(),
                                        MySubtypeOfConflict = root.GetProperty("mysubtypeofconflict").GetString(),
                                        MyTripDayCode = root.GetProperty("mytripdaycode").GetString(),
                                        MyTripName = root.GetProperty("mytripname").GetString(),
                                        IsRejected = root.GetProperty("isrejected").GetString(),
                                        IsAccepted = root.GetProperty("isaccepted").GetString(),
                                        MyOtherConflictGuid = root.GetProperty("myotherconflictguid").GetString()
                                    };

                                    conflicts.Add(conflict);

                                    count++;
                                }
                            }
                            catch (Exception ex)
                            {
                                // Parsing of one conflict info in DB failed, has to discard it
                                Log.Error(ex.Message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Loading conflict info from DB failed: {ex.Message}");
            }

            Log.Information("{0} conflict infos read in {1} seconds", count, (DateTime.Now - start).TotalSeconds);
        }
    }
    #endregion

    #region General Functions
    public void CreateTypes(ActionTypeList? actionTypeList)
    {
        // Various types received from TMS (like train types), just store it as received
        this.typeLists = actionTypeList;
    }

    #endregion

    #region CML

    #region Delegate Functions
    public void LinkDelegatesToAutoRoutingManager()
    {
        GlobalDeclarations.MyAutoRoutingManager?.LinkDelegate(IsRouteAvailable, DoExecuteRoute, IsRouteExecuted, SendCompleteRoutePlan, DoExecuteRouteMarker, DoForecastUpdate);
        SerializeTrainTypes();
    }

    private void SerializeTrainTypes()
    {
        var trainType = JsonConvert.SerializeObject(TrainTypes);
        GlobalDeclarations.MyAutoRoutingManager?.AddTrainTypes(trainType);
    }
    #endregion

    #region Routing
    private RoutePlan GetRoutePlan(int routePlanUid, string origin, string destination)
    {
        RoutePlan routePlan = null!;
        try
        {
            foreach (var r in scheduledRoutePlans)
            {
                if (r.Value.TMSRoutePlan?.data.RoutePlan.Trains[0].TripID == routePlanUid || r.Value.TMSRoutePlan.data.RoutePlan.Trains[0].serid == routePlanUid)
                {
                    var trainGuid = r.Value.TMSRoutePlan.data.RoutePlan.Trains[0].GUID;
                    var msg = r.Value.TMSRoutePlan;
                    return new RoutePlan(trainGuid, msg);
                }
            }
            GlobalDeclarations.MyLogger?.LogInfo("GetRoutePlan:Route Plan Not Found for UID <" + routePlanUid + ">");
        }
        catch (Exception e)
        {
            GlobalDeclarations.MyLogger?.LogException(e.ToString());
        }

        return routePlan;
    }
    private RailgraphLib.HierarchyObjects.Route GetRailGraphRoute(string routeName)
    {
        try
        {
            foreach (var route in Service!.RailgraphHandler!.HierarchyRelations!.Routes)
            {
                if (route.SysName == routeName)
                {
                    return route;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return null!;
    }
    private string GetRouteCommand(RoutePlan routePlan, string routeName)
    {
        try
        {
            foreach (var path in routePlan.TMSRoutePlan.data.RoutePlan.Trains.First().Items)
            {
                foreach (var ra in path.MasterRoute.First().Actions)
                {
                    if (ra.Command[0].value == routeName) return ra.Command.First().cmd;
                    //                                var command = RCA.Command.First().cmd;

                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return "Cmd_UPM";
    }
    private int IsRouteAvailable(string originalRouteName, string replaceRouteName, int planUid, int tripUid, string trainObid, RoutePlanInfo theRoutePlan)
    {
        try
        {
            var schedulePlan = GetScheduledPlan(planUid);
            if (schedulePlan == null) return 0;
            var routePlan = GetRoutePlan(theRoutePlan.PlanUid, theRoutePlan.TripOrigin, theRoutePlan.TripDestination);
            var train = GetTrain(trainObid);
            var command = GetRouteCommand(routePlan, originalRouteName);
            var route = GetRailGraphRoute(replaceRouteName);
            if (train == null) return 0;
            PretestIndex++;
            Service?.RosMessaging?.PretestRouteAvailable(PretestIndex, train, route, command, PretestResultReceived);

            return PretestIndex;
        }
        catch (Exception e)
        {
            GlobalDeclarations.MyLogger?.LogException(e.ToString());
        }
        return 0;
    }
    private static bool IsRouteExecuted(string routeName, int planUid)
    {
        try
        {
            return false;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return false;
    }
    private bool DoExecuteRoute(RoutePlanInfo theRoute)
    {
        {
            try
            {
                RoutePlan routePlan = null!;
                routePlan = GetRoutePlan(theRoute.PlanUid, theRoute.TripOrigin, theRoute.TripDestination);
                if (routePlan is null)
                {
                    GlobalDeclarations.MyLogger?.LogInfo("DoExecuteRoute:Route Plan Not Found for Trip <" + theRoute.MyTrip.TripCode + ">");
                    return false;
                }
                RoutePlan newPlan = GetPartialPlan(routePlan, theRoute);
                if (newPlan.TMSRoutePlan != null)
                {
                    //akk CTC train ID seems to be missing sometimes. Try to set it from couple of sources
                    if (newPlan.TMSRoutePlan.data.RoutePlan.Trains[0].CTCID == "")
                    {
                        string? ctcTrainId = theRoute.MyTrip.CtcUid;
                        if (ctcTrainId == null || ctcTrainId == "")
                        {
                            var obid = theRoute.TrainObid;
                            Train? train = GetTrain(obid);
                            if (train != null)
                                ctcTrainId = train.CtcId;
                        }
                        if (ctcTrainId != null)
                            newPlan.TMSRoutePlan.data.RoutePlan.Trains[0].CTCID = ctcTrainId;
                        else
                        {
                            GlobalDeclarations.MyLogger?.LogInfo("DoExecuteRoute:New Partial Route Plan Does Not Have Train, Discarded <" + theRoute.MyTrip.TripCode + ">");
                            return false;
                        }
                    }

                    //akk MockTrip ID must match in route plan and in path, it seems to differ sometimes. See commented line below
                    int trID = newPlan.TMSRoutePlan.data.RoutePlan.Trains[0].TripID;
                    int pathTrID = newPlan.TMSRoutePlan.data.RoutePlan.Trains[0].Items[0].TrID;
                    if (trID != pathTrID)
                        newPlan.TMSRoutePlan.data.RoutePlan.Trains[0].TripID = pathTrID;
                    GlobalDeclarations.MyLogger?.LogInfo($"DoExecuteRoute:DEBUG:New Partial Route Plan: Trip <{theRoute.MyTrip.TripCode}>: CtcTrainId: {newPlan.TMSRoutePlan.data.RoutePlan.Trains[0].CTCID}, tripID: {trID}, pathTrId: {pathTrID}");
                    GlobalDeclarations.MyLogger?.LogInfo("DoExecuteRoute:New Partial Route Plan Created for Trip <" + theRoute.MyTrip.TripCode + ">");
                    GlobalDeclarations.MyLogger?.LogInfo("DoExecuteRoute:New Partial Route Plan Created From <" + theRoute.TripOrigin + "> to <" + theRoute.TripDestination + ">");
                    GlobalDeclarations.MyLogger?.LogInfo("DoExecuteRoute:Trip UID <" + pathTrID.ToString() + ">");  //akk
                    Service?.RosMessaging?.SendRoutePlan(newPlan, pathTrID.ToString()); //akk: not sure if trip sent as an argument here will be used correctly, so it is set to route plan above
                    return true;
                }
                GlobalDeclarations.MyLogger?.LogInfo("DoExecuteRoute:New Partial Route Plan Not Created for Trip <" + theRoute.MyTrip.TripCode + ">");
                return false;

            }
            catch (Exception e)
            {
                GlobalDeclarations.MyLogger?.LogException(e.ToString());
            }
            return false;
        }
    }
    private RoutePlan GetPartialPlan(RoutePlan thePlan, RoutePlanInfo theRoutePlanInfo)
    {
        RoutePlan theNewPlan = null!;
        try

        {
            //SerializeTrainRoutePlan(thePlan, "Old");
            theNewPlan = new RoutePlan(thePlan.Guid, thePlan.TMSRoutePlan!);
            foreach (var path in thePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items)
            {
                if (path.From.ename == theRoutePlanInfo.FromPlatform && path.To.ename == theRoutePlanInfo.ToPlatform)
                {
                    theNewPlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items = Array.Empty<Path>();
                    theNewPlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items = new Path[1];// Array(1);
                    theNewPlan.TMSRoutePlan.data.RoutePlan.Trains[0].Items[0] = path;

                    ModifySequence(theNewPlan, theRoutePlanInfo);

                    if (GlobalDeclarations.MyDisableExecutionTimeInRoutePlan)
                    {
                        ModifyExecutionTime(theNewPlan);
                    }

                    if (theRoutePlanInfo.CurrentTimedLocation.MyMovementPlan.UseAlternatePathToReRouteToPlan || theRoutePlanInfo.CurrentTimedLocation.MyMovementPlan.UseAlternatePathToNewPlatform)
                    {
                        ModifyPath(theNewPlan, theRoutePlanInfo);
                    }

                    if (theRoutePlanInfo.RemoveActionPointsFromRoutePlan)
                    {
                        ModifyTriggerPoints(theNewPlan);
                    }
                    SerializeTrainRoutePlan(theNewPlan, theRoutePlanInfo, "New");

                    break;
                }
            }
        }
        catch (Exception e)
        {
            GlobalDeclarations.MyLogger?.LogException(e.ToString());
        }
        return theNewPlan;
    }
    private static void ModifySequence(RoutePlan theRoutePlan, RoutePlanInfo theRoutePlanInfo)
    {
        try
        {
            //set the path to execute to path item ID always to number 1 else ROS will not execute
            var data = theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].ID.Split("-");
            string routeSequence = theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].ID;
            if (data.Length > 2)
            {
                routeSequence = "1-" + data[1] + "-" + data[2];
            }
            //set route sequence to 1 for ROS to execute
            theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].ID = routeSequence;
            //theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].MasterRoute[0].Actions[0].Obj.ename = "";
            var tripUid = theRoutePlanInfo.TripUid;
            theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].TrID = tripUid;
            GlobalDeclarations.MyLogger?.LogInfo("ModifyActions:New Partial Route Actions Update for Trip UID <" +
                                                 theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].TrID +
                                                 ">");


        }
        catch (Exception e)
        {
            GlobalDeclarations.MyLogger?.LogException(e.ToString());
        }
    }
    private static void ModifyActions(RoutePlan theRoutePlan, RoutePlanInfo theRoutePlanInfo)
    {
        try
        {
            //get the first base action from the first path to set the other paths with the same parameters
            BaseAction firstAction = theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].MasterRoute[0].Actions[0];

            //set the master route type to Default
            theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].MasterRoute[0].Type = "Default";
            //set the path to execute to path item ID always to number 1 else ROS will not execute
            var data = theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].ID.Split("-");
            string routeSequence = theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].ID;
            if (data.Length > 2)
            {
                routeSequence = "1-" + data[1] + "-" + data[2];
            }
            //var routeSequence = theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].ID.Remove(0, 1).Insert(0, "1");
            // get the track name and ID of where the platform is located
            var trackName = "nothing";
            var trackUid = 0;
            if (theRoutePlanInfo.CurrentPlatform.MyTrackNetwork == null)
            {
                trackName = theRoutePlanInfo.CurrentPlatform.MyHierarchyPlatform!.Tracks[0].SysName;
                trackUid = Convert.ToInt32(theRoutePlanInfo.CurrentPlatform.MyHierarchyPlatform.Tracks[0].SysID);
            }
            else
            {
                trackUid = Convert.ToInt32(theRoutePlanInfo.CurrentPlatform.MyTrackNetwork!.MyTrackAssociation!.TrackUid);
                trackName = theRoutePlanInfo.CurrentPlatform.MyTrackNetwork.MyTrackAssociation.TrackName;
            }

            //set route sequence to 1 for ROS to execute
            theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].ID = routeSequence;

            var tripUid = theRoutePlanInfo.TripUid;
            theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].TrID = tripUid;
            GlobalDeclarations.MyLogger?.LogInfo("ModifyActions:New Partial Route Actions Update for Trip UID <" +
                                                 theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].TrID +
                                                 ">");

            if (!GlobalDeclarations.MyEnableRouteActionTriggerPoints)
            {
                foreach (var ra in theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].MasterRoute[0].Actions)
                {
                    ra.Obj.tmsID = trackUid;
                    ra.Obj.ename = trackName;
                    ra.Obj.secs = firstAction.Obj.secs;
                    ra.Obj.par1 = firstAction.Obj.par1;
                    ra.Obj.par2 = firstAction.Obj.par2;
                }
                GlobalDeclarations.MyLogger?.LogInfo("ModifyActions:New Partial Route Actions Update for Trip <" + theRoutePlanInfo.MyTrip.TripCode + ">");
            }
            else
            {
                GlobalDeclarations.MyLogger?.LogInfo("Route Actions Included In Update for Trip <" + theRoutePlanInfo.MyTrip.TripCode + ">");
            }
        }
        catch (Exception e)
        {
            GlobalDeclarations.MyLogger?.LogException(e.ToString());
        }
    }
    private bool SendCompleteRoutePlan(RoutePlanInfo theRoute)
    {
        try
        {
            RoutePlan routePlan = null!;
            routePlan = GetRoutePlan(theRoute.PlanUid, theRoute.TripOrigin, theRoute.TripDestination);
            if (routePlan is null)
            {
                GlobalDeclarations.MyLogger?.LogInfo("SendCompleteRoutePlan:Route Plan Not Found for Trip <" + theRoute.MyTrip.TripCode + ">");
                return false;
            }
            RoutePlan newPlan = GetParsedPlan(routePlan, theRoute);
            if (newPlan.TMSRoutePlan != null)
            {
                GlobalDeclarations.MyLogger?.LogInfo("DoExecuteRoute:New Parsed Route Plan Created for Trip <" + theRoute.MyTrip.TripCode + ">");
                GlobalDeclarations.MyLogger?.LogInfo("DoExecuteRoute:New Parsed Route Plan Created From <" + theRoute.TripOrigin + "> to <" + theRoute.TripDestination + ">");
                newPlan.TMSRoutePlan.data.RoutePlan.Trains[0].CTCID = theRoute.MyTrip.CtcUid;
                var tripId = theRoute.TripUid.ToString();
                GlobalDeclarations.MyLogger?.LogInfo("DoExecuteRoute:Trip UID <" + tripId + ">");
                Service?.RosMessaging?.SendRoutePlan(newPlan, tripId);
                return true;
            }
            GlobalDeclarations.MyLogger?.LogInfo("DoExecuteRoute:New Parsed Route Plan Not Created for Trip <" + theRoute.MyTrip.TripCode + ">");
            return false;




            ////var trackedTrainGuid = theRoute.MyTrip.MyTrainPosition.Train.Guid;
            ////var train = GetTrainByGUID(trackedTrainGuid);

            ////if (train != null) TrySendingRoutePlanToROS(train);

            //RoutePlan routePlan = null;
            //routePlan = GetRoutePlan(theRoute.PlanUid, theRoute.TripOrigin, theRoute.TripDestination);
            //routePlan.TMSRoutePlan.data.RoutePlan.Trains[0].CTCID = theRoute.MyTrip.CtcUid;
            ////var route = this.trainRoutePlans[theRoute.MyTrip.];
            //if (routePlan == null) return false;
            //Service?.RosMessaging?.SendRoutePlan(routePlan);
            //routePlan.SentToROS = true;
            Log.Information($"SendCompleteRoutePlan: Complete route plan to ROS for Trip <" + theRoute.MyTrip.TripCode + ">");
            return true;
        }
        catch (Exception e)
        {
            GlobalDeclarations.MyLogger?.LogException(e.ToString());
        }
        return false;
    }
    private RoutePlan GetParsedPlan(RoutePlan thePlan, RoutePlanInfo theRoutePlanInfo)
    {
        RoutePlan theNewPlan = null!;
        try

        {
            //SerializeTrainRoutePlan(thePlan, "Old");
            theNewPlan = new RoutePlan(thePlan.Guid, thePlan.TMSRoutePlan!);
            if (theRoutePlanInfo.CurrentTimedLocation == null) return thePlan;

            var platformFrom = theRoutePlanInfo.MyTrip.LastTimedLocation.MyMovementPlan.FromName;
            var platformTo = theRoutePlanInfo.MyTrip.LastTimedLocation.MyMovementPlan.ToName;

            var pathList = new List<Path>();
            var lastLocationFound = false;
            foreach (var path in thePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items)
            {
                if (lastLocationFound) pathList.Add(path);

                if (path.From.ename == platformFrom && path.To.ename == platformTo)
                {
                    lastLocationFound = true;
                }
            }

            theNewPlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items = Array.Empty<Path>();
            theNewPlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items = new Path[pathList.Count];// Array(1);

            var index = 0;
            foreach (var path in pathList)
            {
                theNewPlan.TMSRoutePlan.data.RoutePlan.Trains[0].Items[index] = path;
                ModifySequence(theNewPlan, theRoutePlanInfo, index);
                index++;
            }
            //foreach (var path in thePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items)
            //{
            //    if (path.From.ename == theRoutePlanInfo.FromPlatform && path.To.ename == theRoutePlanInfo.ToPlatform)
            //    {
            //        //theNewPlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items = Array.Empty<Path>();
            //        //theNewPlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items = new Path[1];// Array(1);
            //        theNewPlan.TMSRoutePlan.data.RoutePlan.Trains[0].Items[0] = path;

            //        ModifySequence(theNewPlan, theRoutePlanInfo);

            //        //ModifyActions(theNewPlan,theRoutePlanInfo);
            //        SerializeTrainRoutePlan(theNewPlan, "New");

            //        break;
            //    }
            //}
        }
        catch (Exception e)
        {
            GlobalDeclarations.MyLogger?.LogException(e.ToString());
        }
        return theNewPlan;
    }
    private static void ModifySequence(RoutePlan theRoutePlan, RoutePlanInfo theRoutePlanInfo, int index)
    {
        try
        {
            //set the path to execute to path item ID always to number 1 else ROS will not execute
            //index = 0;
            var data = theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[index].ID.Split("-");
            string routeSequence = theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[index].ID;
            var newSequence = index + 1;
            if (data.Length > 2)
            {
                routeSequence = newSequence + "-" + data[1] + "-" + data[2];
            }
            //set route sequence to 1 for ROS to execute
            theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[index].ID = routeSequence;

            var tripUid = theRoutePlanInfo.TripUid;
            theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[index].TrID = tripUid;
            GlobalDeclarations.MyLogger?.LogInfo("ModifyActions:New Partial Route Actions Update for Trip UID <" + theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].TrID +
                                                 ">");
        }
        catch (Exception e)
        {
            GlobalDeclarations.MyLogger?.LogException(e.ToString());
        }
    }
    private static void ModifyExecutionTime(RoutePlan theRoutePlan)
    {
        try
        {
            theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].MasterRoute[0].start = 0;
            theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].MasterRoute[0].end = 0;

            foreach (var routeAction in theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].MasterRoute[0].Actions)
            {
                routeAction.TimingMode = 9;
                routeAction.TimingModeSpecified = false;
                routeAction.Obj.par1 = 0;
                routeAction.Obj.par2 = 0;
                routeAction.Obj.par1Specified = false;
                routeAction.Obj.par2Specified = false;
                routeAction.Obj.secs = 0;
                routeAction.Obj.secsSpecified = false;
                GlobalDeclarations.MyLogger?.LogInfo("Modifying Execution Time for Route Action <" + routeAction.Obj.ename + ">");
            }
        }
        catch (Exception e)
        {
            GlobalDeclarations.MyLogger?.LogException(e.ToString());
        }
    }
    private static void ModifyTriggerPoints(RoutePlan theRoutePlan)
    {
        try
        {
            foreach (var ra in theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].MasterRoute[0].Actions)
            {
                ra.Obj.ename = "";
            }
        }
        catch (Exception e)
        {
            GlobalDeclarations.MyLogger?.LogException(e.ToString());
        }
    }
    private static void ModifyPath(PlatformAlternate? thePlatformAlternate, RoutePlan theRoutePlan)
    {
        try
        {
            //TODO ....MBB what to do :-)....LOTS OF STUFF
            //theRoutePlan.TMSRoutePlan.data.RoutePlan.Trains[0].Items[0].To = new From().ename= ""; // = thePlatformAlternate.PlatformReplacementName;
            //theRoutePlan.TMSRoutePlan.data.RoutePlan.Trains[0].Items[0].MasterRoute[0].Actions[1].Command[0].value = "SIN_SAU-SIN2_SAU";

            if (thePlatformAlternate != null)
            {
                theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].MasterRoute[0].Type = "Default";
                if (thePlatformAlternate.UseAlternateToPaths)
                {
                    var index = 0;
                    foreach (var ra in thePlatformAlternate.MyRouteActions)
                    {
                        theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].MasterRoute[0].Actions[index].Command[0].value = ra.RouteName;
                        theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].MasterRoute[0].Actions[index].Obj.ename = ra.ActionLocation;
                        index += 1;
                    }
                }
                
                if (thePlatformAlternate.UseAlternateFromPaths)
                {
                    var index = 0;
                    foreach (var ra in thePlatformAlternate.MyRouteActionsToOriginalRoute)
                    {
                        theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].MasterRoute[0].Actions[index].Command[0].value = ra.RouteName;
                        theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].MasterRoute[0].Actions[index].Obj.ename = ra.ActionLocation;
                        index += 1;
                    }

                }
            }

        }
        catch (Exception e)
        {
            GlobalDeclarations.MyLogger?.LogException(e.ToString());
        }
    }
    private static void ModifyPath(RoutePlan theRoutePlan, RoutePlanInfo theRoutePlanInfo)
    {
        try
        {
            //TODO ....MBB what to do :-)....LOTS OF STUFF
            //theRoutePlan.TMSRoutePlan.data.RoutePlan.Trains[0].Items[0].To = new From().ename= ""; // = thePlatformAlternate.PlatformReplacementName;
            //theRoutePlan.TMSRoutePlan.data.RoutePlan.Trains[0].Items[0].MasterRoute[0].Actions[1].Command[0].value = "SIN_SAU-SIN2_SAU";

            //if (thePlatformAlternate != null)
            //{
            var index = 0;
            theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].MasterRoute[0].Type = "Default";
            foreach (var ra in theRoutePlanInfo.CurrentTimedLocation.MyMovementPlan.MyRouteActionsAlternate)
            {
                theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].MasterRoute[0].Actions[index].Command[0].value = ra.RouteName;
                theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].MasterRoute[0].Actions[index].Obj.ename = ra.ActionLocation;
                index += 1;
            }

            //if (thePlatformAlternate.UseAlternateToPaths)
            //    {
            //        var index = 0;
            //        foreach (var ra in thePlatformAlternate.MyRouteActions)
            //        {
            //            theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].MasterRoute[0].Actions[index].Command[0].value = ra.RouteName;
            //            theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].MasterRoute[0].Actions[index].Obj.ename = ra.ActionLocation;
            //            index += 1;
            //        }
            //    }

            //    if (thePlatformAlternate.UseAlternateFromPaths)
            //    {
            //        var index = 0;
            //        foreach (var ra in thePlatformAlternate.MyRouteActionsToOriginalRoute)
            //        {
            //            theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].MasterRoute[0].Actions[index].Command[0].value = ra.RouteName;
            //            theRoutePlan.TMSRoutePlan!.data.RoutePlan.Trains[0].Items[0].MasterRoute[0].Actions[index].Obj.ename = ra.ActionLocation;
            //            index += 1;
            //        }

            //    }
            //}

        }
        catch (Exception e)
        {
            GlobalDeclarations.MyLogger?.LogException(e.ToString());
        }
    }

    #endregion

    #region Route Marking
    private bool DoExecuteRouteMarker(string trainObid, List<Tuple<DateTime, string?>> markers)
    {
        try
        {
            SetNextForecastedRoutes(trainObid, markers);
            return true;
        }
        catch (Exception e)
        {
            GlobalDeclarations.MyLogger?.LogException(e.ToString());
        }
        return false;
    }

    #endregion

    #region Forecast
    private bool DoForecastUpdate(Forecast theForecast)
    {
        try
        {
            if (theForecast.TheTrip != null)
            {
                if (theForecast.TheTrip.IsAllocated)
                {
                    //Create forecast from allocated trains, train position movement, and trip property chancges
                    CreateForecastOnAllocatedTrain(theForecast);
                }
                else
                {
                    //Create forecast from unallocated trips
                    CreateForecastOnTrip(theForecast);
                }
            }
            return true;
        }
        catch (Exception e)
        {
            GlobalDeclarations.MyLogger?.LogException(e.ToString());
        }
        return false;
    }
    private void CreateForecastOnAllocatedTrain(Forecast theForecast)
    {
        try
        {
            var train = GetTrain(theForecast.TheTrip.MyTrainObid);
            var scheduledPlan = GetScheduledPlan(this.timetableAllocations[train?.Obid!].Item1);

            var timedLocations = new List<TimedLocation>();
            if (scheduledPlan == null)
            {
                ScheduledPlanKey key = new(theForecast.TheTrip.ScheduledPlanDayCode, theForecast.TheTrip.ScheduledPlanName);
                scheduledPlan = GetScheduledPlan(key);
            }

            if (train != null)
            {
                var trip = theForecast.TheTrip;
                foreach (var tl in theForecast.TheTrip.TimedLocations)
                {
                    var description = tl.Description;
                    var stopping = tl.HasStopping;
                    var arrivalTime = new ActionTime(tl.ArrivalTimeAdjusted.ToUniversalTime());
                    var departTime = new ActionTime(tl.DepartureTimeAdjusted.ToUniversalTime());
                    var platform = Service?.RailgraphHandler?.GetPlatform(tl.Description);
                    var platformPosition = Service?.RailgraphHandler?.GetElementPositionOfPlatform(platform);
                    TimedLocation timedLocation = new(description, platformPosition, arrivalTime, departTime, stopping)
                    {
                        Id = tl.Id,
                        TripId = trip.TripId,
                        TripName = trip.TripCode!,
                        ArrivalOccurred = tl.HasArrivedToPlatform,
                        DepartureOccurred = tl.HasDepartedFromPlatform
                    };
                    timedLocations.Add(timedLocation);
                }
                var estimationPlan = new EstimationPlan(train.Obid, train.Td, timedLocations, new ElementExtension()) { ScheduledPlanKey = scheduledPlan.Key }; //TODO: Train's path should be updated from route plan of train, when it is received from TMS!
                SerializeEstimationPlan(estimationPlan, trip);

                if (estimationPlan.IsValid())
                {
                    EstimationPlanChanged(train, estimationPlan);
                    //return estimationPlan;
                }
            }
        }
        catch (Exception e)
        {
            GlobalDeclarations.MyLogger?.LogException(e.ToString());
        }
    }
    private void CreateForecastOnTrip(Forecast theForecast)
    {
        try
        {
            var trip = theForecast.TheTrip;
            var timedLocations = new List<TimedLocation>();
            ScheduledPlanKey key = new(theForecast.TheTrip.ScheduledPlanDayCode, theForecast.TheTrip.ScheduledPlanName);
            ScheduledPlan plan = GetScheduledPlan(key);

            foreach (var tl in theForecast.TheTrip.TimedLocations)
            {
                var description = tl.Description;
                var stopping = tl.HasStopping;
                var arrivalTime = new ActionTime(tl.ArrivalTimeAdjusted.ToUniversalTime());
                var departTime = new ActionTime(tl.DepartureTimeAdjusted.ToUniversalTime());
                var platform = Service?.RailgraphHandler?.GetPlatform(tl.Description);
                var platformPosition = Service?.RailgraphHandler?.GetElementPositionOfPlatform(platform);
                TimedLocation timedLocation = new(description, platformPosition, arrivalTime, departTime, stopping)
                {
                    Id = tl.Id,
                    TripId = tl.TripId,
                    TripName = trip.TripCode!,
                    ArrivalOccurred = tl.HasArrivedToPlatform,
                    DepartureOccurred = tl.HasDepartedFromPlatform
                };
                timedLocations.Add(timedLocation);

            }

            var estimationPlan = new EstimationPlan(timedLocations) { ScheduledPlanKey = plan?.Key };
            SerializeEstimationPlan(estimationPlan, trip);
            if (estimationPlan.IsValid())
            {
                EstimationPlanChanged(estimationPlan);
                //return estimationPlan;
            }
        }
        catch (Exception e)
        {
            GlobalDeclarations.MyLogger?.LogException(e.ToString());
        }
    }

    #endregion

    #region Serialize Functions
    public void SerializeEstimationPlan(EstimationPlan thePlan, ConflictManagementLibrary.Model.Trip.Trip theTrip)
    {
        var str = JsonConvert.SerializeObject(thePlan);
        var filename = $"Trip-{theTrip.TripCode}-{DateTime.Now:MMddyyyyhhmmssfff}.json";
        var curDir = Environment.CurrentDirectory;
        const string folder = @"Data\SerializeData\EstimationPlans";
        if (!Directory.Exists(System.IO.Path.Combine(curDir, folder)))
        {
            Directory.CreateDirectory(System.IO.Path.Combine(curDir, folder));
        }

        var fullpath = System.IO.Path.Combine(curDir, folder, filename);
        File.WriteAllText(fullpath, str);
    }
    public void SerializePossession(Possession thePossession)
    {
        var str = JsonConvert.SerializeObject(thePossession);
        var filename = $"Possession-{thePossession.Id}-{thePossession.Description}-{DateTime.Now:MMddyyyyhhmmssfff}.json";
        var curDir = Environment.CurrentDirectory;
        const string folder = @"Data\SerializeData\Possessions";
        if (!Directory.Exists(System.IO.Path.Combine(curDir, folder)))
        {
            Directory.CreateDirectory(System.IO.Path.Combine(curDir, folder));
        }

        var fullpath = System.IO.Path.Combine(curDir, folder, filename);
        File.WriteAllText(fullpath, str);
    }
    public void SerializeTrainPosition(TrainPosition thePosition)
    {
        var str = JsonConvert.SerializeObject(thePosition);
        var trainName1 = thePosition?.Train?.CtcId;
        var trainName2 = thePosition?.Train?.Td;
        var filename = $"TrainPosition-{trainName1}-{trainName2}-{DateTime.Now:MMddyyyyhhmmssfff}.json";
        var curDir = Environment.CurrentDirectory;
        const string folder = @"Data\SerializeData\Train";
        if (!Directory.Exists(System.IO.Path.Combine(curDir, folder)))
        {
            Directory.CreateDirectory(System.IO.Path.Combine(curDir, folder));
        }

        var fullpath = System.IO.Path.Combine(curDir, folder, filename);
        File.WriteAllText(fullpath, str);
    }
    public void SerializeTrain(Train theTrain)
    {

        var str = JsonConvert.SerializeObject(theTrain);
        var filename = $"Train_{DateTime.Now:MMddyyyyhhmmssfff}.json";
        var curDir = Environment.CurrentDirectory;
        const string folder = @"Data\SerializeData\Train";
        if (!Directory.Exists(System.IO.Path.Combine(curDir, folder)))
        {
            Directory.CreateDirectory(System.IO.Path.Combine(curDir, folder));
        }

        var fullpath = System.IO.Path.Combine(curDir, folder, filename);
        File.WriteAllText(fullpath, str);
    }
    public void SerializeSchedulePlan(ScheduledPlan thePlan)
    {

        var str = JsonConvert.SerializeObject(thePlan);
        //$"Sent scheduled route plan request for scheduled plan {scheduledPlan.Name}/{scheduledPlan.Id}, daycode {scheduledPlan.ScheduledDayCode}");
        var filename = $"SchedulePlan_{thePlan.Name} {thePlan.Id}, daycode {thePlan.ScheduledDayCode}.json";
        var curDir = Environment.CurrentDirectory;
        const string folder = @"Data\SerializeData\SchedulePlan";
        if (!Directory.Exists(System.IO.Path.Combine(curDir, folder)))
        {
            Directory.CreateDirectory(System.IO.Path.Combine(curDir, folder));
        }

        var fullpath = System.IO.Path.Combine(curDir, folder, filename);
        File.WriteAllText(fullpath, str);
    }
    public void SerializeRoutePlan(XSD.RoutePlan.Train thePlan, string suffix = "", string scheduleData = "", bool doXML = true)
    {
        string filename;
        string str;
        if (doXML)
        {
            str = XmlSerialization.SerializeObject(thePlan, out string error);
            string _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            if (str.StartsWith(_byteOrderMarkUtf8))
            {
                str = str.Remove(0, _byteOrderMarkUtf8.Length);
            }
            filename = $"RoutePlan_{thePlan.TripID + "-" + thePlan.serid + "-" + suffix + "-" + scheduleData}.xml";
        }
        else
        {
            str = JsonConvert.SerializeObject(thePlan);
            filename = $"RoutePlan_{thePlan.TripID + "-" + thePlan.serid + "-" + suffix + "-" + scheduleData}.json";
        }
        var curDir = Environment.CurrentDirectory;
        const string folder = @"Data\SerializeData\RoutePlan";
        if (!Directory.Exists(System.IO.Path.Combine(curDir, folder)))
        {
            Directory.CreateDirectory(System.IO.Path.Combine(curDir, folder));
        }
        var fullpath = System.IO.Path.Combine(curDir, folder, filename);
        File.WriteAllText(fullpath, str);
    }
    public void SerializeTrainRoutePlan(RoutePlan thePlan, RoutePlanInfo theRoutePlanInfo, string suffix = "", bool doXML = true)
    {
        string filename;
        string str;

        if (doXML)
        {
            str = XmlSerialization.SerializeObject(thePlan, out string error);
            string _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            if (str.StartsWith(_byteOrderMarkUtf8))
            {
                str = str.Remove(0, _byteOrderMarkUtf8.Length);
            }
            filename = $"RoutePlan-{theRoutePlanInfo.MyTrip.TripCode + "-" + suffix}-{DateTime.Now:MMddyyyyhhmmssfff}.xml";
        }
        else
        {
            str = JsonConvert.SerializeObject(thePlan);
            filename = $"RoutePlan-{theRoutePlanInfo.MyTrip.TripCode + "-" + suffix}-{DateTime.Now:MMddyyyyhhmmssfff}.json";
        }

        var curDir = Environment.CurrentDirectory;
        const string folder = @"Data\SerializeData\TrainRoutePlanToROS";
        if (!Directory.Exists(System.IO.Path.Combine(curDir, folder)))
        {
            Directory.CreateDirectory(System.IO.Path.Combine(curDir, folder));
        }

        var fullpath = System.IO.Path.Combine(curDir, folder, filename);
        File.WriteAllText(fullpath, str);
    }

    #endregion

    #region Test Functions

    private void StartTests()
    {
        try
        {
            var thread = new Thread(DoTests); thread.Start();
        }
        catch (Exception )
        {
            Console.WriteLine();
            throw;
        }
    }

    public void DoTests()
    {
        try
        {
            while (true)
            {
                Service?.RailgraphHandler?.TestBlockings();

                Thread.Sleep(10000);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    #endregion
    #endregion
}
