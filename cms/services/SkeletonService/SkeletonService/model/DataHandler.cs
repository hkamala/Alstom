namespace SkeletonService.Model;

using Cassandra;
using System.Collections.Concurrent;
using System.Text.Json;
using Serilog;
using static System.Diagnostics.Debug;
using E2KService.MessageHandler;
using static E2KService.ServiceImp;
using static TrainData.DescriberWithConsist.Types;
using System;

////////////////////////////////////////////////////////////////////////////////

internal class DataHandler
{
    // Public data collections
    public Dictionary<string /*stationId*/, Station> Stations => stations;

    // Callback delegates for data changes and requests
    public delegate void DelegateTrainPositionChanged(Train? train, TrainPosition trainPosition);
    public delegate void DelegateTrainDeleted(Train train, ActionTime occurredTime);
    public delegate void DelegateNotifyTrainPositionsRefreshRequestEnded();

    // Delegate properties
    public DelegateTrainPositionChanged? NotifyTrainPositionChanged { get; set; }
    public DelegateTrainDeleted? NotifyTrainDeleted { get; set; }
    public DelegateNotifyTrainPositionsRefreshRequestEnded? NotifyTrainPositionsRefreshRequestEnded { get; set; }

    // These are the default initialization values for concurrent collections. These are not the limits of collections!
    const int defaultConcurrencyLevel = 2;  // Estimated amount of threads updating collections
    const int defaultTrainCount = 40;

    // Timetables and possessions
    private readonly TrainPositions trainPositions = new(defaultConcurrencyLevel, defaultTrainCount);

    // Stations and platforms
    private readonly Dictionary<string /*station ID*/, Station> stations = new();

    // Trains
    private readonly ConcurrentDictionary<string /*obid*/, Train> trains = new();


    private readonly Thread maintenanceThread;
    private const int c_SleepTimeMS = 1000;
    private volatile bool shuttingDown = false;

    // TODO: make these configurable?
    const int extTrainPositionsRequestPendingTimeout = 10; // seconds

    ActionTime trainPositionsRequestTimeout = new();

    private delegate void RefreshRequestTimeoutHandler();

    readonly List<string> obidTrainsToDeleteAfterRefresh = new();

    ////////////////////////////////////////////////////////////////////////////////

    internal DataHandler()
    {
        maintenanceThread = new Thread(new ThreadStart(MaintenanceThread))
        {
            Name = "ModelPeriodicTask",
            IsBackground = true
        };
        maintenanceThread.Start();
    }

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

            maybeClearPendingRequest("Train positions", trainPositionsRequestTimeout, DeleteTrainsNotInRefresh);
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

        Log.Information("Model changed to Online");
    }

    private void ClearAllInformation()
    {
        Log.Information("All data cleared");
    }

    private void ClearPendingRequests()
    {
        // Clear request pending states
        trainPositionsRequestTimeout.SetTimeInvalid();

        Log.Information("Pending requests information cleared");
    }

    ////////////////////////////////////////////////////////////////////////////////

    public Train CreateTrain(string obid, string guid, string ctcId, string td, uint sysid, Train.CtcTrainType type)
    {
        Train train = new(obid, guid, ctcId, td, sysid, type);

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
        }

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

    ////////////////////////////////////////////////////////////////////////////////

    public void SetTrainPositionsRequested(bool requested)
    {
        lock (this)
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
            }
        }
    }

    
    public bool IsTrainPositionsRequestPending()
    {
        lock (this)
        {
            return trainPositionsRequestTimeout.IsValid();
        }
    }
    
    ////////////////////////////////////////////////////////////////////////////////

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
            }
        }
        obidTrainsToDeleteAfterRefresh.Clear();

        // Handle possible pending request
        NotifyTrainPositionsRefreshRequestEnded?.Invoke();
    }

    ////////////////////////////////////////////////////////////////////////////////

    public Station? GetStation(string? stationId)
    {
        if (stationId != null && stations.ContainsKey(stationId))
            return stations[stationId];
        return null;
    }

    ////////////////////////////////////////////////////////////////////////////////

    public void ReportModel()
    {
        // Report train positions
        Log.Debug($"Report of train positions:");
        foreach (var pos in this.trainPositions.Values)
        {
            Log.Information($"  Train {pos.Train}: {pos.ElementExtension}");
        }
    }

    ////////////////////////////////////////////////////////////////////////////////

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
                        long tailAdditionalPos = 0; // TODO: get this from RailGraph for tail!
                        long headAdditionalPos = 0; // TODO: get this from RailGraph for head!

                        // TODO : simulated position
                        headAdditionalPos = describer.KmValue;
                        tailAdditionalPos = headAdditionalPos + (long)(footprint.EndDirection == TrainData.EDirection.Nominal ? -30 : 30) * 1000;

                        ElementPosition tailPos = new(footprint.ElementStr.First(), footprint.StartDistance, tailAdditionalPos);
                        ElementPosition headPos = new(footprint.ElementStr.Last(), footprint.EndDistance, headAdditionalPos);

                        ElementExtension elementExtension = new(tailPos, headPos, footprint.ElementStr.ToList());

                        trainPosition = new TrainPosition(train, elementExtension, occurredTime);

                        // TODO : remove!
                        var action = describerWithConsist.Action;
                        Log.Information($"Train {describer.Train.Describer} ({action}) - Core footprint : {elementExtension}");
                        var km = describer.KmValue / 1000000.0;
                        Log.Information($"Head position: {km:0.000} km");
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
            if (trainPositions.ContainsKey(trainPosition.Train.Obid))
            {
                changed = trainPositions[trainPosition.Train.Obid] != trainPosition;
                if (changed)
                    trainPositions[trainPosition.Train.Obid] = trainPosition;
            }
            else
            {
                trainPositions.TryAdd(trainPosition.Train.Obid, trainPosition);
                changed = true;
            }
        }

        return changed;
    }

    ////////////////////////////////////////////////////////////////////////////////

    public void TrainDataChanged(Train train, ActionTime occurredTime, TrainData.DescriberWithConsist describerWithConsist)
    {
        // Note: Not all members in describerWithConsist contain meaningful information. For example, timetable information refer to old ES2000 TTS data, not to TMS data

        var requestPending = IsTrainPositionsRequestPending();
        var informChange = false;

        // Train exists, so remove it from list of trains to be removed after refresh, if refreshing
        if (requestPending)
            obidTrainsToDeleteAfterRefresh.Remove(train.Obid);

        // Check train movement
        if (ConvertToTrainPosition(train, occurredTime, describerWithConsist, out TrainPosition trainPosition))
        {
            // Store to memory if position is changed
            var positionChanged = RememberTrainPosition(trainPosition);

            // ... if train really moved
            if (positionChanged)
            {
                // Notify clients, if not refreshing
                informChange = !requestPending;
            }
        }

        // Handle other data needed (example train property postfix, ...)
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

}
