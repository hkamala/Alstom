namespace E2KService.Model;

using Cassandra;
using System.Collections.Concurrent;
using System.Text.Json;
using Serilog;
using System.Xml.Linq;
using System.Security.Cryptography;

////////////////////////////////////////////////////////////////////////////////

internal class DataHandler
{
    // Data collections
    public TrainMovementHistory TrainMovementHistory { get => this.trainMovementHistory; }
    public TrainEstimationPlans TrainEstimationPlans { get => this.trainEstimationPlans; }
    public EstimationPlans EstimationPlans => estimationPlans;
    public ScheduledPlans ScheduledPlans { get => this.scheduledPlans; }
    public Possessions Possessions { get => this.possessions; }

    // Callback delegates for data changes
    public delegate void DelegateTrainPositionChanged(MovementHistoryItem historyItem);
    public delegate void DelegateTrainDeleted(MovementHistoryItem historyItem);
    public delegate void DelegateEstimationPlanChanged(EstimationPlan estimationPlan);
    public delegate void DelegateEstimationPlanDeleted(EstimationPlan estimationPlan);
    public delegate void DelegateScheduledPlanChanged(ScheduledPlan scheduledPlan);
    public delegate void DelegateScheduledPlanDeleted(ScheduledPlan scheduledPlan);
    public delegate void DelegatePossessionChanged(Possession possession);
    public delegate void DelegatePossessionDeleted(Possession possession);
    public delegate void DelegateNotifyTrainPositionsRefreshRequestEnded();
    public delegate void DelegateNotifyEstimationPlansRefreshRequestEnded();
    public delegate void DelegateNotifyScheduledPlansRefreshRequestEnded();
    public delegate void DelegateNotifyPossessionsRefreshRequestEnded();

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
    public DelegateNotifyPossessionsRefreshRequestEnded? NotifyPossessionsRefreshRequestEnded { get; set; }
    
    // These are the default initialization values for concurrent collections. These are not the limits of collections!
    const int defaultConcurrencyLevel = 2;  // Estimated amount of threads updating collections
    const int defaultTrainCount = 40;
    const int defaultEstimationPlans = defaultTrainCount;
    const int defaultScheduledPlans = defaultTrainCount;
    const int defaultPossessions = 10;

    // TrainMovementHistory will keep only the latest movement of one train on same second (because we use 1 second resolution of time)! But that is probably enough
    private readonly TrainMovementHistory trainMovementHistory = new(defaultConcurrencyLevel, defaultTrainCount);
    private readonly TrainEstimationPlans trainEstimationPlans = new(defaultConcurrencyLevel, defaultEstimationPlans);
    private readonly EstimationPlans estimationPlans = new(defaultConcurrencyLevel, defaultEstimationPlans);
    private readonly ScheduledPlans scheduledPlans = new(defaultConcurrencyLevel, defaultScheduledPlans);
    private readonly Possessions possessions = new(defaultConcurrencyLevel, defaultPossessions);

    private readonly ConcurrentDictionary<string /*obid*/, Train> trains = new();

    private readonly Thread maintenanceThread;
    private const int c_SleepTimeMS = 1000;
    private volatile bool shuttingDown = false;

    // TODO: make these configurable?
    const int extTrainPositionsRequestPendingTimeout = 10; // seconds
    const int extEstimationPlansRequestPendingTimeout = 10; // seconds
    const int extScheduledPlansRequestPendingTimeout = 10; // TODO 20; // seconds
    const int extPossessionRequestPendingTimeout = 10; // TODO 120; // seconds. This is considerably longer than others, because this may cause possession deletion!

    ActionTime trainPositionsRequestTimeout = new();
    ActionTime estimationPlansRequestTimeout = new();
    ActionTime scheduledPlansRequestTimeout = new();
    ActionTime possessionsRequestTimeout = new();

    private delegate void RefreshRequestTimeoutHandler();

    PurgeTime? purgeTime = null;
    readonly List<string> obidTrainsToDeleteAfterRefresh = new();

    private readonly uint trainMovementHistoryHours = 24;   // 24 hours is the default
    private readonly uint possessionHistoryHours = 7 * 24;  // 7 days is the default

    List<string> possessionActiveStates = new();

    // Cassandra
    readonly Cassandra.ISession? cassandraSession = null;
    readonly Cassandra.ConsistencyLevel? cassandraConsistencyLevel = null;

    ////////////////////////////////////////////////////////////////////////////////

    internal DataHandler(uint trainMovementHistoryHours, uint possessionHistoryHours, List<string> cassandraContactPoints, int cassandraPort, uint cassandraConsistencyLevel, string possessionActiveStates)
    {
        // Set some reasonable limits for history
        this.trainMovementHistoryHours = Math.Max(8, Math.Min(trainMovementHistoryHours, 48));
        this.possessionHistoryHours = Math.Max(24, Math.Min(possessionHistoryHours, 14*24));
        this.possessionActiveStates = possessionActiveStates.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();

        try
        {
            Log.Information("Connecting to Cassandra cluster (keyspace 'tdgs')...");

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

            //          2022-02-04 13:20:24.4430 INFO | Reading train movements from DB
            //          2022-02-04 13:21:04.6812 INFO | Deserialization time (milliseconds): 5233,721400000002
            //          2022-02-04 13:21:04.6812 INFO | JSON handling time (milliseconds): 10947,122199999982
            //          2022-02-04 13:21:04.6812 INFO | 3455148 train movements read in 40238,3015 milliseconds
            //          2022-02-04 13:21:04.6960 INFO | Reading possessions from DB
            //          2022-02-04 13:21:04.7056 INFO | 4 possessions read in 0,0026333 seconds
            //
            //          => 19 hours 20 minutes of 50 trains moving every 1 second
            //
            //          => RJ TMS: Train movement history reading time: 10 trains, movement every 5 second, 24 hours :  2.0 seconds
            //          => BHP:    Train movement history reading time: 80 trains, movement every 5 second, 24 hours : 16.0 seconds

            var cluster = Cluster.Builder()
                .AddContactPoints(cassandraContactPoints)
                .WithPort(cassandraPort)
                .WithSocketOptions(new SocketOptions().SetReadTimeoutMillis(60000)) // Does nothing!
                .WithQueryOptions(new QueryOptions().SetPageSize(100))  // Solution to read timeout problem!
                .Build();

            this.cassandraConsistencyLevel = (ConsistencyLevel) Math.Max(1, Math.Min(cassandraConsistencyLevel, 3)); // ConsistencyLevel.One is minimum, ConsistencyLevel.Three is maximum

            // Connect and select 'tdgs' keyspace
            this.cassandraSession = cluster.Connect("tdgs");
            
            Log.Information("Connected to keyspace 'tdgs' of cluster: " + cluster.Metadata.ClusterName);
        }
        catch (Exception ex)
        {
            throw new Exception("Connection to Cassandra cluster failed: " + ex.Message);
        }

        this.maintenanceThread = new Thread(new ThreadStart(MaintenanceThread))
        {
            Name = "ModelPeriodicTask",
            IsBackground = true
        };
        this.maintenanceThread.Start();
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

        if (this.purgeTime != null && this.purgeTime.IsPurgeTime())
        {
            PurgeHistory();
        }

        lock (this)
        {
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

            maybeClearPendingRequest("Train positions", this.trainPositionsRequestTimeout, UpdateTrainsAfterRefresh);
            maybeClearPendingRequest("Estimation plans", this.estimationPlansRequestTimeout, UpdateEstimationPlansAfterRefresh);
            maybeClearPendingRequest("Scheduled plans", this.scheduledPlansRequestTimeout, UpdateScheduledPlansAfterRefresh);
            maybeClearPendingRequest("Possessions", this.possessionsRequestTimeout, UpdatePossessionsAfterRefresh);
        }
    }

    private void PurgeHistory()
    {
        var now = ActionTime.Now;

        Log.Information("Purging history");

        try
        {
            // Purge train movement history from memory (DB handles itself!)
            DateTime historyStart = DateTime.UtcNow - TimeSpan.FromHours(this.trainMovementHistoryHours); // UTC
            ulong firstHistoryMs = (ulong)(historyStart - DateTime.UnixEpoch).TotalMilliseconds;

            foreach (var trainMovements in this.trainMovementHistory.Values)
            {
                var movementsToDelete = from movement in trainMovements
                                        where movement.Key < firstHistoryMs
                                        select movement.Key;

                foreach (var occurredTime in movementsToDelete)
                {
                    trainMovements.TryRemove(occurredTime, out MovementHistoryItem? item);
                }
                // We can leave the train into internal movement history, even if all movements of it were removed
                // It is mapped by OBID, so there are not so many trains there, and they probably will appear soon again
            }

            // Purge possessions from memory and DB
            // First collect all possessions to be purged. If end time of history possession is outside of possession history time window, it can be purged
            var possessionsToDelete = from item in this.possessions
                                      where item.Value.IsHistoric() && (now - item.Value.EndTime).TotalSeconds > this.possessionHistoryHours * 60 * 60
                                      select item.Key;

            // Then delete from DB and memory
            foreach (var possession in possessionsToDelete)
            {
                DeletePossession(possession);
            }
        }
        catch (Exception ex)
        {
            Log.Error("History purging failed: {0}", ex.Message);
        }
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
        // Read movement history and possessions
        ClearAllInformation();

        // Read from persistent memory
        LoadTrainMovements();
        LoadPossessions();

        // Purge history immediately
        this.purgeTime = new PurgeTime();
        PurgeHistory();

        Log.Information("Model changed to Online");
    }

    private void ClearAllInformation()
    {
        // Remove all stored information
        Log.Information("Deleting trains");
        var existingtrains = trains.Values.ToList();
        foreach (var train in existingtrains)
            DeleteTrain(train);
        Log.Information("Deleted trains");

        this.trains.Clear();
        this.trainMovementHistory.Clear();
        this.trainEstimationPlans.Clear();
        this.scheduledPlans.Clear();

        Log.Information("All data cleared");
    }

    private void ClearPendingRequests()
    {
        // Clear request pending states
        this.trainPositionsRequestTimeout.SetTimeInvalid();
        this.estimationPlansRequestTimeout.SetTimeInvalid();
        this.scheduledPlansRequestTimeout.SetTimeInvalid();
        this.possessionsRequestTimeout.SetTimeInvalid();

        Log.Information("Pending requests information cleared");
    }

    ////////////////////////////////////////////////////////////////////////////////

    public Train CreateTrain(string obid, string guid, string ctcId, string td, string postfix = "", string traintype = "")
    {
        Train train = new(obid, guid, ctcId, td) { Postfix = postfix, TrainType = traintype };

        if (this.trains.ContainsKey(obid))
        {
            if (this.trains[obid] != train)
            {
                this.trains[obid] = train;
                Log.Information("Updated train: {0}", train);

                // Notify existing estimation plan of train to clients with new IDs of train, if not refreshing
                if (!IsEstimationPlansRequestPending() && this.trainEstimationPlans.ContainsKey(train.Obid))
                {
                    NotifyEstimationPlanChanged?.Invoke(this.trainEstimationPlans[train.Obid]);
                }
            }
        }
        else
        {
            this.trains.TryAdd(obid, train);
            Log.Information("Created train: {0}", train);
        }
        
        return train;
    }
    
    public Train? GetTrain(string obid)
    {
        if (this.trains.ContainsKey(obid))
            return this.trains[obid];

        return null;
    }

    ////////////////////////////////////////////////////////////////////////////////
    
    public static EdgePosition CreateEdgePosition(string edge, uint offset, string fromVertex, long additionalPos = 0, string additionalName = "")
    {
        return new EdgePosition(edge, offset, fromVertex, additionalPos, additionalName);
    }
    public static EdgeExtension CreateEdgeExtension(EdgePosition startPos, EdgePosition endPos, List<string> edges)
    {
        return new EdgeExtension(startPos, endPos, edges);
    }
    public static TimedLocation CreateTimedLocation(string description, EdgePosition pos, ActionTime arrival, ActionTime departure, bool arrivalOccurred = false, bool departureOccurred = false, int tripId=0, string tripName="")
    {
        return new TimedLocation(description, pos, arrival, departure) { ArrivalOccurred = arrivalOccurred, DepartureOccurred = departureOccurred, TripId = tripId, TripName = tripName };
    }
    public static Trip CreateTrip(string id, string name, int tripNumber, string description, EdgePosition startPos, EdgePosition endPos, ActionTime startTime, ActionTime endTime, List<TimedLocation> timedLocations, bool activeTrip, bool allocated)
    {
        return new Trip(id, name, tripNumber, description, startPos, endPos, startTime, endTime, timedLocations, activeTrip) { Allocated = allocated };
    }

    ////////////////////////////////////////////////////////////////////////////////

    public void SetTrainPositionsRequested(bool requested)
    {
        lock(this)
        {
            if (requested)
            {
                // Let first request (and probably the only one) decide timeout
                if (!IsTrainPositionsRequestPending())
                {
                    // Remember all existing trains
                    this.obidTrainsToDeleteAfterRefresh.Clear();
                    foreach (var obid in this.trains.Keys)
                        this.obidTrainsToDeleteAfterRefresh.Add(obid);

                    this.trainPositionsRequestTimeout = ActionTime.Now + TimeSpan.FromSeconds(extTrainPositionsRequestPendingTimeout);
                }
            }
            else
            {
                this.trainPositionsRequestTimeout.SetTimeInvalid();
                UpdateTrainsAfterRefresh();
            }
        }
    }

    public void SetEstimationPlansRequested(bool requested)
    {
        lock(this)
        {
            if (requested)
            {
                // Let first request (and probably the only one) decide timeout
                if (!IsEstimationPlansRequestPending())
                {
                    // Mark existing estimation plans as not updated by refresh
                    foreach (var estimationPlan in this.trainEstimationPlans.Values)
                    {
                        estimationPlan.ClearUpdatedByRefresh();
                    }

                    this.estimationPlansRequestTimeout = ActionTime.Now + TimeSpan.FromSeconds(extEstimationPlansRequestPendingTimeout);
                }
            }
            else
            {
                this.estimationPlansRequestTimeout.SetTimeInvalid();
                UpdateEstimationPlansAfterRefresh();
            }
        }
    }

    public void SetScheduledPlansRequested(bool requested)
    {
        lock(this)
        {
            if (requested)
            {
                // Let first request (and probably the only one) decide timeout
                if (!IsScheduledPlansRequestPending())
                {
                    // Mark existing scheduled plans as not updated by refresh
                    foreach (var scheduledPlan in this.scheduledPlans.Values)
                    {
                        scheduledPlan.ClearUpdatedByRefresh();
                    }

                    this.scheduledPlansRequestTimeout = ActionTime.Now + TimeSpan.FromSeconds(extScheduledPlansRequestPendingTimeout);
                }
            }
            else
            {
                this.scheduledPlansRequestTimeout.SetTimeInvalid();
                UpdateScheduledPlansAfterRefresh();
            }
        }
    }

    public void SetPossessionsRequested(bool requested)
    {
        lock(this)
        {
            if (requested)
            {
                // Let first request (and probably the only one) decide timeout
                if (!IsPossessionsRequestPending())
                {
                    // Mark existing possessions as not updated by refresh
                    foreach (var possession in this.possessions.Values)
                    {
                        if (!possession.IsHistoric())
                            possession.ClearUpdatedByRefresh();
                    }

                    this.possessionsRequestTimeout = ActionTime.Now + TimeSpan.FromSeconds(extPossessionRequestPendingTimeout);
                }
            }
            else
            {
                this.possessionsRequestTimeout.SetTimeInvalid();
                UpdatePossessionsAfterRefresh();
            }
        }
    }

    public bool IsTrainPositionsRequestPending()
    {
        lock(this)
        {
            return this.trainPositionsRequestTimeout.IsValid();
        }
    }
    public bool IsEstimationPlansRequestPending()
    {
        lock(this)
        {
            return this.estimationPlansRequestTimeout.IsValid();
        }
    }
    public bool IsScheduledPlansRequestPending()
    {
        lock(this)
        {
            return this.scheduledPlansRequestTimeout.IsValid();
        }
    }
    public bool IsPossessionsRequestPending()
    {
        lock(this)
        {
            return this.possessionsRequestTimeout.IsValid();
        }
    }

    ////////////////////////////////////////////////////////////////////////////////

    private void UpdateTrainsAfterRefresh()
    {
        foreach (string trainObid in this.obidTrainsToDeleteAfterRefresh)
        {
            // Delete train (and possible estimation plan)
            Train? train = GetTrain(trainObid);
            if (train != null)
            {
                TrainDeleted(train, ActionTime.Now);
                Log.Information("Deleted train obid='" + trainObid + "' because it was not in refresh data any more");
            }
        }
        this.obidTrainsToDeleteAfterRefresh.Clear();

        // Inform about ended refresh
        NotifyTrainPositionsRefreshRequestEnded?.Invoke();
    }

    private void UpdateEstimationPlansAfterRefresh()
    {
        // Get list of train estimations, that were not updated in refresh
        List<string> trainEstimationsToDelete = new();
        List<ScheduledPlanKey> estimationsToDelete = new();

        foreach (var estimation in TrainEstimationPlans)
        {
            if (!estimation.Value.IsUpdatedByRefresh())
                trainEstimationsToDelete.Add(estimation.Key);
        }

        // And delete them
        foreach (string obid in trainEstimationsToDelete)
        {
            EstimationPlan estimationPlan = EstimationPlanDeleted(obid);
            if (estimationPlan.IsValid())
            {
                Log.Information("Deleted estimation plan for train '" + obid + "' because it was not in refresh data any more");
            }
        }

        foreach (var estimation in EstimationPlans)
        {
            if (!estimation.Value.IsUpdatedByRefresh())
                estimationsToDelete.Add(estimation.Key);
        }

        foreach (var scheduledPlanKey in estimationsToDelete)
        {
            EstimationPlan estimationPlan = EstimationPlanDeleted(scheduledPlanKey);
            if (estimationPlan.IsValid())
            {
                Log.Information("Deleted scheduled plan estimation '" + scheduledPlanKey + "' because it was not in refresh data any more");
            }
        }

        // Inform about ended refresh
        NotifyEstimationPlansRefreshRequestEnded?.Invoke();
    }

    private void UpdateScheduledPlansAfterRefresh()
    {
        // Get list of scheduled plans, that were not updated in refresh
        List<ScheduledPlanKey> scheduledPlansToDelete = new();

        foreach (var scheduled in ScheduledPlans)
        {
            if (!scheduled.Value.IsUpdatedByRefresh())
                scheduledPlansToDelete.Add(scheduled.Key);
        }

        // And delete them
        foreach (var id in scheduledPlansToDelete)
        {
            ScheduledPlan scheduledPlan = ScheduledPlanDeleted(id);
            if (scheduledPlan.IsValid())
            {
                Log.Information("Deleted scheduled plan '" + id + "' because it was not in refresh data any more");
            }
        }

        // Inform about ended refresh
        NotifyScheduledPlansRefreshRequestEnded?.Invoke();
    }

    private void UpdatePossessionsAfterRefresh()
    {
        // Get list of possession IDs, that were not updated in refresh
        List<string> possessionsToDelete = new();

        foreach (var possession in Possessions.Values)
        {
            if (!possession.IsHistoric() && !possession.IsUpdatedByRefresh())
                possessionsToDelete.Add(possession.ExternalId);
        }

        // And delete them (maybe moving to history), no need to update to distance graph handler
        foreach (var id in possessionsToDelete)
        {
            PossessionDeleted(id);

            Log.Information("Deleted possession '" + id + "' because it was not in refresh data any more");
        }

        // Inform about ended refresh
        NotifyPossessionsRefreshRequestEnded?.Invoke();
    }

    ////////////////////////////////////////////////////////////////////////////////

    private static bool ConvertToMovementHistoryItem(Train? train, ActionTime occurredTime, EdgeExtension extension, out MovementHistoryItem historyItem)
    {
        historyItem = new();

        if (train != null && occurredTime.IsValid() && extension.IsValid())
        {
            try
            {
                // Now we are actually interested only in head (end) position
                EdgePosition headPos = extension.EndPos;

                var edgeId = headPos.EdgeId;
                var additionalPos = headPos.AdditionalPos;
                var additionalName = headPos.AdditionalName;

                // Extra data that may be needed later
                //var offset = headPos.Offset;
                //var fromNodeId = headPos.FromVertexId;

                historyItem = new MovementHistoryItem(train.Obid, train.Td, occurredTime, edgeId, additionalPos, additionalName) { Postfix = train.Postfix, TrainType = train.TrainType };
            }
            catch
            {
            }
        }

        return historyItem.IsValid();
    }

    private static bool ConvertToTerminatedMovementHistoryItem(Train? train, ActionTime occurredTime, out MovementHistoryItem historyItem)
    {
        if (train != null)
            historyItem = new MovementHistoryItem(train.Obid, train.Td, occurredTime) { Postfix = train.Postfix, TrainType = train.TrainType };
        else
            historyItem = new MovementHistoryItem();

        return historyItem.IsValid();
    }

    private void AddMovementHistoryItem(MovementHistoryItem historyItem)
    {
        if (historyItem.IsValid())
        {
            // Store to memory
            if (!this.trainMovementHistory.ContainsKey(historyItem.Obid))
            {
                // Initial size is one train movement every second for entire history
                // TODO: Do we need to use run-time setting for very large collections?
                this.trainMovementHistory.TryAdd(historyItem.Obid, new TrainMovements(defaultConcurrencyLevel, (int)(this.trainMovementHistoryHours * 60 * 60)));
            }
            this.trainMovementHistory[historyItem.Obid].TryAdd(historyItem.OccurredTime.GetMilliSecondsFromEpoch(), historyItem);
        }
    }

    ////////////////////////////////////////////////////////////////////////////////

    public MovementHistoryItem TrainPositionChanged(Train train, ActionTime occurredTime, EdgeExtension footprint)
    {
        if (ConvertToMovementHistoryItem(train, occurredTime, footprint, out MovementHistoryItem historyItem))
        {
            // Store to memory
            AddMovementHistoryItem(historyItem);

            // Store to persistent memory
            StoreTrainMovement(historyItem);

            // Notify clients, if not refreshing
            if (!IsTrainPositionsRequestPending())
            {
                NotifyTrainPositionChanged?.Invoke(historyItem);
            }
            else
            {
                // Train exists, so remove it from list of trains to be removed after refresh
                this.obidTrainsToDeleteAfterRefresh.Remove(train.Obid);
            }
        }

        return historyItem;
    }

    public MovementHistoryItem TrainDeleted(Train train, ActionTime occurredTime)
    {
        // Convert to history item
        if (ConvertToTerminatedMovementHistoryItem(train, occurredTime, out MovementHistoryItem historyItem))
        {
            // Store to memory
            AddMovementHistoryItem(historyItem);

            // Store to persistent memory
            StoreTrainMovement(historyItem);

            // Notify clients, if not refreshing
            if (!IsTrainPositionsRequestPending())
            {
                NotifyTrainDeleted?.Invoke(historyItem);
            }
        }

        DeleteTrain(train);

        return historyItem;
    }

    private void DeleteTrain(Train train)
    {
        // Delete estimation plan
        this.trainEstimationPlans.TryRemove(train.Obid, out _);

        Log.Information("Deleted train: {0}", train);

        // Delete train
        this.trains.TryRemove(train.Obid, out _);
    }

    ////////////////////////////////////////////////////////////////////////////////

    public EstimationPlan EstimationPlanChanged(Train train, List<TimedLocation> timedLocations, EdgeExtension trainPath, int tripId = 0)
    {
        EstimationPlan estimationPlan = new(train.Obid, train.Td, timedLocations, trainPath) { TripId = tripId };
        if (estimationPlan.IsValid())
        {
            if (this.trainEstimationPlans.ContainsKey(train.Obid))
                this.trainEstimationPlans[train.Obid] = estimationPlan;
            else
                this.trainEstimationPlans.TryAdd(train.Obid, estimationPlan);

            // Notify clients, if not refreshing
            if (!IsEstimationPlansRequestPending())
            {
                NotifyEstimationPlanChanged?.Invoke(estimationPlan);
            }
        }
        
        return estimationPlan;
    }

    public EstimationPlan EstimationPlanChanged(ScheduledPlanKey scheduledPlanKey, List<TimedLocation> timedLocations)
    {
        var estimationPlan = new EstimationPlan(timedLocations) { ScheduledPlanKey = scheduledPlanKey };
        if (estimationPlan.IsValid())
        {
            if (this.estimationPlans.ContainsKey(scheduledPlanKey))
                this.estimationPlans[scheduledPlanKey] = estimationPlan;
            else
                this.estimationPlans.TryAdd(scheduledPlanKey, estimationPlan);

            // Notify clients, if not refreshing
            if (!IsEstimationPlansRequestPending())
            {
                NotifyEstimationPlanChanged?.Invoke(estimationPlan);
            }
        }

        return estimationPlan;
    }

    public EstimationPlan EstimationPlanDeleted(Train train)
    {
        return EstimationPlanDeleted(train.Obid);
    }

    public EstimationPlan EstimationPlanDeleted(string obid)
    {
        EstimationPlan estimationPlan = new();

        if (this.trainEstimationPlans.ContainsKey(obid))
        {
            this.trainEstimationPlans.TryRemove(obid, out EstimationPlan? plan);
            if (plan != null)
                estimationPlan = plan;
        }

        // Notify clients, if not refreshing
        if (estimationPlan.IsValid() && !IsEstimationPlansRequestPending())
        {
            NotifyEstimationPlanDeleted?.Invoke(estimationPlan);
        }

        return estimationPlan;
    }

    public EstimationPlan EstimationPlanDeleted(ScheduledPlanKey key)
    {
        EstimationPlan estimationPlan = new();

        if (this.estimationPlans.ContainsKey(key))
        {
            this.estimationPlans.TryRemove(key, out EstimationPlan? plan);
            if (plan != null)
                estimationPlan = plan;
        }

        // Notify clients, if not refreshing
        if (estimationPlan.IsValid() && !IsEstimationPlansRequestPending())
        {
            NotifyEstimationPlanDeleted?.Invoke(estimationPlan);
        }

        return estimationPlan;
    }

    ////////////////////////////////////////////////////////////////////////////////

    public ScheduledPlan ScheduledPlanChanged(int dayCode, string id, string name, string traintype, string description, List<Trip> trips, bool activePlan, bool allocated)
    {
        ScheduledPlan scheduledPlan = new(dayCode, id, name, traintype, description, trips, activePlan) { Allocated = allocated };
        if (scheduledPlan.IsValid())
        {
            var key = scheduledPlan.Key;

            if (this.scheduledPlans.ContainsKey(key))
                this.scheduledPlans[key] = scheduledPlan;
            else
                this.scheduledPlans.TryAdd(key, scheduledPlan);

            // Notify clients, if not refreshing
            if (!IsScheduledPlansRequestPending())
            {
                NotifyScheduledPlanChanged?.Invoke(scheduledPlan);
            }
        }

        return scheduledPlan;
    }

    public ScheduledPlan ScheduledPlanDeleted(ScheduledPlanKey key)
    {
        ScheduledPlan scheduledPlan = new();

        if (this.scheduledPlans.ContainsKey(key))
        {
            this.scheduledPlans.TryRemove(key, out ScheduledPlan? plan);
            if (plan != null)
                scheduledPlan = plan;
        }

        // Notify clients, if not refreshing
        if (scheduledPlan.IsValid() && !IsScheduledPlansRequestPending())
        {
            NotifyScheduledPlanDeleted?.Invoke(scheduledPlan);
        }

        return scheduledPlan;
    }

    ////////////////////////////////////////////////////////////////////////////////

    public Possession PossessionChanged(string id, string description, EdgePosition startPos, EdgePosition endPos, ActionTime startTime, ActionTime endTime, string state)
    {
        Possession newPossession = new(id, description, startPos, endPos, startTime, endTime, IsPossessionActiveState(state), state);

        if (newPossession.IsValid())
        {
            // NOTE: historic possession is not found, because its operational ID differs from existing possession even when all basic data is the same!
            if (this.possessions.ContainsKey(newPossession.GetId()))
            {
                // Merge existing (not historic) possession
                newPossession.Merge(this.possessions[newPossession.GetId()]);
            }

            RememberPossession(newPossession);
        }
        
        return newPossession;
    }

    public bool IsPossessionActiveState(string state)
    {
        return this.possessionActiveStates.Contains(state);
    }

    private void RememberPossession(Possession possession)
    {
        // Store to memory
        if (this.possessions.ContainsKey(possession.GetId()))
            this.possessions[possession.GetId()] = possession;
        else
            this.possessions.TryAdd(possession.GetId(), possession);

        // Store to persistent memory (possibly overwriting existing one)
        StorePossession(possession);

        // Notify clients, if not refreshing
        if (possession.IsValid() && !IsPossessionsRequestPending())
        {
            NotifyPossessionChanged?.Invoke(possession);
        }
    }

    public Possession PossessionDeleted(string id)
    {
        // Have to search by external identifier. Loop and check external ID and historic state
        foreach (var possession in this.possessions.Values)
        {
            if (possession.ExternalId == id && !possession.IsHistoric())
            {
                var deletedPossession = DeletePossession(possession.GetId());

                if (deletedPossession != null)
                {
                    // Has this ever been activated?
                    bool hasBeenActive = deletedPossession.IsStartTimeLocked();

                    // If yes, it must be remembered as historic possession
                    if (hasBeenActive)
                    {
                        deletedPossession.SetHistoric();
                        RememberPossession(deletedPossession);
                    }

                    return deletedPossession;
                }
            }
        }

        return new Possession();
    }

    public Possession? DeletePossession(string id)
    {
        // Delete from memory
        this.possessions.TryRemove(id, out Possession? possession);

        // And from persistent storage
        if (possession != null && this.cassandraSession != null)
        {
            try
            {
                string statement = "delete from restrictions where id=? and type=?";

                var preparedStatement = this.cassandraSession.Prepare(statement);
                var boundStatement = preparedStatement.Bind(possession.GetId(), (int)RestrictionType.POSSESSION);
                this.cassandraSession.Execute(boundStatement);

                Log.Information("Deleted possession from DB: {0}", possession);
            }
            catch (Exception ex)
            {
                Log.Error("Possession deletion from DB failed: {0}: {1}", possession, ex.Message);
            }
        }

        // Notify clients, if not refreshing
        if (possession != null && possession.IsValid() && !IsPossessionsRequestPending())
        {
            NotifyPossessionDeleted?.Invoke(possession);
        }

        return possession;
    }

    ////////////////////////////////////////////////////////////////////////////////
    
    private void StoreTrainMovement(MovementHistoryItem historyItem)
    {
        if (historyItem.IsValid() && this.cassandraSession != null)
        {
            try
            { 
                DateTime occurredtime = historyItem.OccurredTime.DateTime;

                int hour = occurredtime.Hour;
                string date = System.Xml.XmlConvert.ToString(occurredtime, "yyyyMMdd");

                // Insert/update statement (inserts also with given WHERE data, if row does not exist!)

                // update tdgs.movementhistory set positions = positions + {3055:{td:'M12346',t:False,e:'WA-TA-ET',p:140000}} where obid='XBOX05' and date='20201117' and hour=15;
#if false
                string json = historyItem.ToJsonString();
                string statement = "update movementhistory set positions = positions + fromJson(?) where obid='" + historyItem.Obid + "' and date='" + date + "' and hour=" + hour;
                var preparedStatement = this.cassandraSession.Prepare(statement);
                var boundStatement = preparedStatement.Bind(json);
#else
                // Insert data as fast as possible without JSON
                string secondOffset = (historyItem.OccurredTime.DateTime.Minute * 60 + historyItem.OccurredTime.DateTime.Second).ToString();
                string t = historyItem.Terminated ? "True" : "False";
                string e = historyItem.Terminated ? "" : historyItem.EdgeId;
                string p = historyItem.Terminated ? "0" : historyItem.AdditionalPosition.ToString();

                string statement = "update tdgs.movementhistory set positions = positions + {" + secondOffset + ":{td:'" + historyItem.Td + "',pf:'" + historyItem.Postfix + "',tt:'" + historyItem.TrainType + "',t:" + t + ",e:'" + e + "',p:" + p + "}} where obid='" + historyItem.Obid + "' and date='" + date + "' and hour=" + hour;
                var preparedStatement = this.cassandraSession.Prepare(statement);
                var boundStatement = preparedStatement.Bind();
#endif
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
            }
            catch (Exception ex)
            {
                Log.Error("Train position addition to Cassandra failed: {0}, {1}", historyItem, ex.Message);
            }
        }
    }

    private void LoadTrainMovements()
    {
        if (this.cassandraSession != null)
        {
            ulong count = 0;
            DateTime start = DateTime.Now;

            Log.Information("Reading train movements from DB");

            try
            {
                // Read whole rows as JSON strings
                string statement = "select json obid,date,hour,positions from movementhistory";

                var preparedStatement = this.cassandraSession.Prepare(statement);
                var boundStatement = preparedStatement.Bind();
                //boundStatement.SetReadTimeoutMillis(60000); // Doesn't work!
                //var rowset = this.cassandraSession.Execute(boundStatement);

                var rowset = this.cassandraSession.Execute(statement);

                // TODO: remove these at some point
                double deserT = 0;
                double handleT = 0;

                if (rowset != null)
                {
                    foreach (var row in rowset.ToArray())
                    {
                        var json = row.GetValue<string>("[json]");

                        DateTime t1 = DateTime.Now;
                        var doc = JsonSerializer.Deserialize<JsonDocument>(json);
                        deserT += (DateTime.Now - t1).TotalMilliseconds;

                        t1 = DateTime.Now;
                        if (doc != null)
                        {
                            try
                            {
                                var root = doc.RootElement;

                                var obid = root.GetProperty("obid").GetString();
                                var date = root.GetProperty("date").GetString();

                                bool validHour = true;
                                int hour = 0;
                                try { hour = root.GetProperty("hour").GetInt32(); } catch { validHour = false; }

                                // If some value in DB is invalid, discard the train movements of that part

                                if (obid != null && date != null && validHour)
                                {
                                    ActionTime dateHour = new();
                                    if (dateHour.InitFromDateStringAndTime(date, hour))
                                    {
                                        JsonElement positions = root.GetProperty("positions");
                                        if (positions.ValueKind == JsonValueKind.Object)
                                        {
                                            foreach (var movement in positions.EnumerateObject())
                                            {
                                                var secondOffset = int.Parse(movement.Name);
                                                var position = movement.Value;
                                                if (position.ValueKind == JsonValueKind.Object)
                                                {
                                                    var td = position.GetProperty("td").GetString();
                                                    var pf = position.GetProperty("pf").GetString();
                                                    var tt = position.GetProperty("tt").GetString();
                                                    var terminated = position.GetProperty("t").GetBoolean();
                                                    var edge = position.GetProperty("e").GetString();
                                                    var addpos = position.GetProperty("p").GetInt64();

                                                    ActionTime occurredTime = dateHour + new TimeSpan(0, 0, secondOffset);

                                                    if (td != null && td != "" && edge != null && edge != "" && occurredTime.IsValid())
                                                    {
                                                        MovementHistoryItem historyItem;
                                                        if (terminated)
                                                            historyItem = new MovementHistoryItem(obid, td, occurredTime) { Postfix = pf == null ? "" : pf, TrainType = tt == null ? "" : tt };
                                                        else
                                                            historyItem = new MovementHistoryItem(obid, td, occurredTime, edge, addpos) { Postfix = pf == null ? "" : pf, TrainType = tt == null ? "" : tt };

                                                        if (historyItem.IsValid())
                                                        {
                                                            // Store to memory
                                                            AddMovementHistoryItem(historyItem);
                                                            count++;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                // Parsing of one train movement hour failed, just discard it
                            }
                        }
                        handleT += (DateTime.Now - t1).TotalMilliseconds;
                    }
                }

                Log.Information("Deserialization time (milliseconds): {0}", deserT);
                Log.Information("JSON handling time (milliseconds): {0}", handleT);
            }
            catch (Exception ex)
            {
                Log.Error("Loading train movements from DB failed: {0}", ex.Message);
            }

            Log.Information("{0} train movements read in {1} milliseconds", count, (DateTime.Now - start).TotalMilliseconds);
        }
    }
    
    ////////////////////////////////////////////////////////////////////////////////

    private void StorePossession(Possession possession)
    {
        if (possession.IsValid() && this.cassandraSession != null)
        {
            try
            {
                string restriction = possession.ToJsonStrings(out string activationActions);

                //Log.Information("Restriction: {0}", restriction);
                //Log.Information("ActivationActions: {0}", activationActions);
                
                // NOTE: we don't need separate UPDATE statement, because we are not using "IF NOT EXISTS" in CQL.
                // Insert either inserts new or replaces existing line in table
                string statement = "insert into restrictions (id,type,active,historic,restriction,activationactions) values (?,?,?,?,fromJson(?),fromJson(?))";

                var preparedStatement = this.cassandraSession.Prepare(statement);
                var boundStatement = preparedStatement.Bind(possession.GetId(), (int)RestrictionType.POSSESSION, possession.IsActive(), possession.IsHistoric(), restriction, activationActions);

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

                Log.Information("Inserted possession into DB: {0}", possession);
            }
            catch (Exception ex)
            {
                Log.Error("Possession insertion into DB failed: {0}: {1}", possession, ex.Message);
            }
        }
    }

    private void LoadPossessions()
    {
        if (this.cassandraSession != null)
        {
            ulong count = 0;
            DateTime start = DateTime.Now;

            Log.Information("Reading possessions from DB");

            try
            {
                // When restrictions other than possessions are supported, the query should use filter:
                // select ... from restrictions where type=nnn ALLOW FILTERING;

                // Read whole rows as JSON strings
                string statement = "select json id,type,active,historic,restriction,activationactions from restrictions";

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

                                var id = root.GetProperty("id").GetString();
                                var type = root.GetProperty("type").GetInt32();

                                if (id != null && (RestrictionType)type == RestrictionType.POSSESSION)
                                {
                                    var active = root.GetProperty("active").GetBoolean();
                                    var historic = root.GetProperty("historic").GetBoolean();

                                    // Restriction
                                    var restriction = root.GetProperty("restriction");
                                    if (restriction.ValueKind == JsonValueKind.Object)
                                    {
                                        string? description = restriction.GetProperty("description").GetString();
                                        if (description == null)
                                            description = "";

                                        // Times
                                        ActionTime startTime = new();
                                        ActionTime endTime = new();

                                        var s = restriction.GetProperty("starttime").GetString();
                                        var e = restriction.GetProperty("endtime").GetString();

                                        // Take only needed part of time and remove Z which would cause localtime conversion!
                                        s = s?[..19];
                                        e = e?[..19];

                                        if (s == null || !startTime.InitFromFormat(s, "yyyy-MM-dd HH:mm:ss"))
                                            throw new Exception("Invalid start time in possession " + id);
                                        if (e == null || !endTime.InitFromFormat(e, "yyyy-MM-dd HH:mm:ss"))
                                            throw new Exception("Invalid end time in possession " + id);

                                        var startTimeLocked = restriction.GetProperty("starttimelocked").GetBoolean();
                                        var endTimeLocked = restriction.GetProperty("endtimelocked").GetBoolean();

                                        // Positions
                                        var createEdgePosition = (string node, out EdgePosition pos) =>
                                        {
                                            bool valid = false;
                                            string edgeid = "";
                                            string vertexid = "";
                                            string addname = "";
                                            uint offset = 0;
                                            long addpos = 0;

                                            var position = restriction.GetProperty(node);
                                            if (position.ValueKind == JsonValueKind.Object)
                                            {
                                                var s = position.GetProperty("edgeid").GetString();
                                                if (s != null)
                                                    edgeid = s;
                                                offset = position.GetProperty("offset").GetUInt32();
                                                s = position.GetProperty("fromvertexid").GetString();
                                                if (s != null)
                                                    vertexid = s;
                                                addpos = position.GetProperty("addpos").GetInt64();
                                                s = position.GetProperty("addname").GetString();
                                                if (s != null)
                                                    addname = s;

                                                valid = edgeid != "";
                                            }

                                            if (valid)
                                                pos = new EdgePosition(edgeid, offset, vertexid, addpos, addname);
                                            else
                                                pos = new EdgePosition();
                                        };

                                        createEdgePosition("startposition", out EdgePosition startPosition);
                                        createEdgePosition("endposition", out EdgePosition endPosition);

                                        // Activation actions
                                        ActivationActionVector actions = new();

                                        var activationActions = root.GetProperty("activationactions");
                                        if (activationActions.ValueKind == JsonValueKind.Object)
                                        {
                                            foreach (var activation in activationActions.EnumerateObject())
                                            {
                                                var time = activation.Name;
                                                var state = activation.Value.GetString();

                                                // Take only needed part of time and remove UTF indicator which would cause localtime conversion!
                                                time = time?[..19];

                                                ActionTime actionTime = new();
                                                if (time == null || !actionTime.InitFromFormat(time, "yyyy-MM-dd HH:mm:ss"))
                                                    throw (new Exception("Invalid activation action time"));

                                                if (actionTime.IsValid() && state != null)
                                                {
                                                    actions.Add(new ActivationAction(actionTime, IsPossessionActiveState(state), state));
                                                }
                                            }
                                        }

                                        // Create possession
                                        Possession possession = new(id, description, startPosition, endPosition, startTime, endTime, active, startTimeLocked, endTimeLocked, historic, actions);

                                        if (possession.IsValid())
                                        {
                                            // Store only to memory (possibly overwriting existing one)
                                            if (this.possessions.ContainsKey(possession.GetId()))
                                                this.possessions[possession.GetId()] = possession;
                                            else
                                                this.possessions.TryAdd(possession.GetId(), possession);

                                            count++;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                // Parsing of one possession in DB, have to discard it
                                Log.Error(ex.Message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Loading possessions from DB failed: {0}", ex.Message);
            }

            Log.Information("{0} possessions read in {1} seconds", count, (DateTime.Now - start).TotalSeconds);
        }
    }
}
