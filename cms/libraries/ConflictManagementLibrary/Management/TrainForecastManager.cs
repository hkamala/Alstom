using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ConflictManagementLibrary.Logging;
using ConflictManagementLibrary.Model.Movement;
using ConflictManagementLibrary.Model.Reservation;
using ConflictManagementLibrary.Model.Schedule;
using ConflictManagementLibrary.Model.Trip;
using ConflictManagementLibrary.Network;
using NLog.Fluent;
using static System.Net.WebRequestMethods;
using static ConflictManagementLibrary.Helpers.GlobalDeclarations;
using File = System.IO.File;
using Path = System.IO.Path;
using System.Threading;
using Cassandra;
using ConflictManagementLibrary.Helpers;
using ConflictManagementLibrary.Model.Conflict;
using NLog.LayoutRenderers;


namespace ConflictManagementLibrary.Management
{
    public class TrainForecastManager
    {
        #region Declarations
        private const string DataFolderName = "Data";
        private const string PlanFolderName = "Plan";
        private const string PlanArchiveFolderName = "Archive";
        private static string PlanFolderPath = "";
        private static string PlanArchiveFolderPath = "";
        private ConcurrentDictionary<string, ScheduledPlan>? ReadyToProcess { get; set; }
        private ConcurrentDictionary<string, ScheduledPlan>? ReadyToArchive { get; set; }
        private ConcurrentDictionary<string, ScheduledPlan>? PlansList { get; set; }
        public List<TrainService> MyTrainServices { get; set; } = new List<TrainService>();
        private ConcurrentDictionary<string, ScheduledPlan>? MyArchiveList { get; set; }
        private readonly RailwayNetworkManager? _railwayNetworkManager;
        private readonly TrainSchedulerManager? theTrainSchedulerManager;
        private IMyLogger? MyLogger { get; }

        private Queue<ScheduledPlan> SchedulePlans = new Queue<ScheduledPlan>();
        #endregion

        #region Delegates
        public delegate void ScheduleProcessorDelegate(Trip? theTrip);
        public ScheduleProcessorDelegate? DoProcessTrip;
        #endregion

        #region Constructor
        private TrainForecastManager(IMyLogger? theLogger, RailwayNetworkManager? railwayNetworkManager, TrainSchedulerManager? trainSchedulerManager)
        {
            MyLogger = theLogger;
            _railwayNetworkManager = railwayNetworkManager;// new RailwayNetworkManager(theLogger);
            theTrainSchedulerManager = trainSchedulerManager;
            StartSchedulePlanProcessing();
            var doCleanUpTrainServices = new Thread(DoCleanUpTrainServices);
            doCleanUpTrainServices.Start();

        }
        public static TrainForecastManager? CreateInstance(IMyLogger? theLogger, RailwayNetworkManager? railwayNetworkManager, TrainSchedulerManager? trainSchedulerManager)
        {
            return new TrainForecastManager(theLogger, railwayNetworkManager, trainSchedulerManager);
        }
        #endregion

        #region Test Methods
        private Task ReadPlanAsync(string fileName)
        {
            try
            {
                var fullPath = Path.Combine(PlanFolderPath, fileName);
                var plan = DeserializeMyObjectFromFile<ScheduledPlan>(MyLogger, fullPath);
                if (plan != null)
                {
                    PlansList.TryAdd(fileName, plan);
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
                return Task.FromException(e);
            }
            return Task.CompletedTask;
        }
        private void ReadPlan(string fileName)
        {
            try
            {
                var fullPath = Path.Combine(PlanFolderPath, fileName);
                var plan = DeserializeMyObjectFromFile<ScheduledPlan>(MyLogger, fullPath);
                if (plan != null)
                {
                    PlansList.TryAdd(fileName, plan);
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
        }
        private Task MovePlanToArchiveFolderAsync()
        {
            try
            {
                foreach (var kv in ReadyToArchive)
                {
                    var archFullPath = Path.Combine(PlanArchiveFolderPath, kv.Key);
                    if (File.Exists(archFullPath))
                    {
                        File.Delete(archFullPath);
                    }
                    var movePath = Path.Combine(PlanFolderPath, kv.Key);
                    File.Move(movePath, archFullPath);
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
            return Task.CompletedTask;
        }
        private void CheckFolderExists()
        {
            try
            {
                var curDir = Environment.CurrentDirectory;
                PlanFolderPath = Path.Combine(curDir, DataFolderName, PlanFolderName);
                PlanArchiveFolderPath = Path.Combine(curDir, DataFolderName, PlanFolderName, PlanArchiveFolderName);
                if (!Directory.Exists(PlanFolderPath))
                {
                    Directory.CreateDirectory(Path.Combine(curDir, PlanFolderPath));
                }
                if (!Directory.Exists(PlanArchiveFolderPath))
                {
                    Directory.CreateDirectory(Path.Combine(curDir, PlanArchiveFolderPath));
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
        }
        public void ProcessTheTestPlans()
        {
            DoProcessing().GetAwaiter().GetResult();

        }
        public async Task ProcessPlans()
        {
            lock (DoProcessing())
            {
                DoProcessing().GetAwaiter().GetResult();
            }

        }
        private async Task DoProcessing()
        {
            try
            {
                ReadyToProcess = new ConcurrentDictionary<string, ScheduledPlan>();
                ReadyToArchive = new ConcurrentDictionary<string, ScheduledPlan>();
                PlansList = new ConcurrentDictionary<string, ScheduledPlan>();
                var taskList = new List<Task>();
                var files = Directory.GetFiles(PlanFolderPath, "*.json", SearchOption.TopDirectoryOnly);
                if (files.Any())
                {
                    taskList.AddRange(from file in files where !string.IsNullOrEmpty(file) select Path.GetFileName(file) into filename select Task.Run(() => ReadPlanAsync(filename)));
                }
                Task.WaitAll(taskList.ToArray());
                ValidatePlanAsync();
                await BuildTrainServices();
                if (!ReadyToProcess.IsEmpty)
                {
                    await BuildTrainServices();
                }
                if (!ReadyToArchive.IsEmpty)
                { 
                    await Task.Factory.StartNew(MovePlansToArchiveFolder);
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
        }
        private void ValidatePlanAsync()
        {
            try
            {
                foreach (var kv in PlansList)
                {
                    if (DateTime.Now.AddSeconds(-30) < kv.Value.StartTime.DateTime)
                    {
                        ReadyToProcess.TryAdd(kv.Key, kv.Value);
                    }
                    else
                    {
                        ReadyToArchive.TryAdd(kv.Key, kv.Value);
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
        }
        private Task BuildTrainServices()
        {
            var taskList = new List<Task>();
            if (!ReadyToProcess.IsEmpty)
            {
                taskList.AddRange(ReadyToProcess.Select(kv => Task.Run(() => CreateTrainServiceAsync(kv.Value))));
            }
            Task.WaitAll(taskList.ToArray());
            return Task.CompletedTask;
        }
        private Task CreateTrainServiceAsync(ScheduledPlan plan)
        {
            try
            {
                var ts = new TrainService
                {
                    ScheduledDayCode = plan.Key.ScheduledDayCode,
                    ScheduledPlanName = plan.Key.ScheduledPlanName,
                    LineId = plan.LineId,
                    TrainTypeId = plan.TrainTypeId,
                };
                foreach (var kv in plan.Trips)
                {
                    var trip = new Trip(MyLogger)
                    {
                        Name = kv.Value.Name,
                        TripCode = kv.Value.TripCode,
                        Number = kv.Value.TripNumber,
                        Direction = (kv.Value.StartPos.Offset > kv.Value.EndPos.Offset) ? "L" : "R"
                    };
                    var locations = kv.Value.TimedLocations;
                    var startIndex = kv.Key;
                    if (!(locations?.Count > 0)) continue;
                    foreach (var loc in locations)
                    {
                        if (loc == null || !loc.HasStopping) continue;

                        var platform = _railwayNetworkManager.FindPlatform(loc.Description);
                        if (platform == null)
                        {
                            MyLogger?.LogCriticalError($"Platform Not Fount {loc.Description}");
                            continue;
                        }
                        var stn = _railwayNetworkManager.FindStationById(platform.StationId);
                        var timeLoc = new TimedLocation
                        {
                            Description = loc.Description,
                            ArrivalTimePlan = loc.Arrival.DateTime,
                            DepartureTimePlan = loc.Departure.DateTime,
                            ArrivalTimeActual = loc.Arrival.DateTime,
                            DepartureTimeActual = loc.Departure.DateTime,
                            HasStopping = loc.HasStopping,
                            Position = TimedLocationPosition.CreateInstance(loc.Pos.ElementId, loc.Pos.Offset.ToString(), loc.Pos.AdditionalPos.ToString(), loc.Pos.AdditionalName),
                            MyPlatform = platform,
                        };
                        //if (stn.CheckStationReserved(timeLoc) || !stn.AddReservations(timeLoc)) continue;
                        trip.TimedLocations.Add(timeLoc);
                    }
                    ts.Trips.Add(kv.Key, trip);
                    trip.CreateReservations(_railwayNetworkManager.MyMovementPlans, _railwayNetworkManager.MyStations);
                    MyLogger?.LogInfo(ts.GetServiceInformation() + trip.GetTripInformation());

                }
                MyTrainServices.Add(ts);

            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
            return Task.CompletedTask;
        }
        #endregion

        #region Train Service Methods
        private void StartSchedulePlanProcessing()
        {
            var StartScan = new Thread(DoScanSchedulePlanFolder); StartScan.Start();
        }
        private void DoScanSchedulePlanFolder()
        {
            try
            {
                while (!_railwayNetworkManager.IsInitialized)
                {
                    Thread.Sleep(1000);
                }
                while (true)
                {
                    try
                    {
                        //CheckFolderExists();
                        //ProcessPlanFolder();
                        //MovePlansToArchiveFolder();
                        ProcessSchedulePlanQueue();
                    }
                    catch (Exception e)
                    {
                        MyLogger.LogException(e.ToString());
                    }
                    Thread.Sleep(2000);
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void ProcessSchedulePlanQueue()
        {
            try
            {
                ScheduledPlan[] plans = new ScheduledPlan[] { }; 
                lock (SchedulePlans)
                {
                    if (SchedulePlans.Count > 0)
                    {
                        plans = SchedulePlans.ToArray();
                    }
                    SchedulePlans.Clear();
                }

                foreach (var plan in plans)
                {
                    CreateTrainService(plan);
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
        private void ProcessPlanFolder()
        {
            try
            {
                ReadyToArchive = new ConcurrentDictionary<string, ScheduledPlan>();

                var files = Directory.GetFiles(PlanFolderPath, "*.json", SearchOption.TopDirectoryOnly);
                if (files.Any())
                {
                    foreach (var f in files)
                    {
                        var filename = Path.Combine(PlanFolderPath, f);

                        var plan = AddPlanFromFile(filename);
                        if (plan != null)
                        {
                            ReadyToArchive.TryAdd(filename, plan);
                        }
                        else
                        {
                            ForceArchivePlan(filename);
                        }
                    }
                }

            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
        }
        public ScheduledPlan? AddPlanFromFile(string theData)
        {
            try
            {
                var plan = DeserializeMyObjectFromFile<ScheduledPlan>(MyLogger, theData);
                if (plan != null && ValidatePlan(plan))
                {
                    var filename = Path.GetFileName(theData);
                    CreateTrainService(plan);
                    return plan;
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
            return null;
        }
        public void AddPlanFromDataHandler(string thePlan)
        {
            try
            {
                var plan = DeserializeMyObject<ScheduledPlan>(MyLogger, thePlan);
                if (!ValidatePlan(plan)) return;

                if (DoesPlanExistWithDifferentStartTime(plan))
                {
                    //Delete the old trips...update the CMS/TSUI
                    var oldTrainService = FindTrainService(plan);
                    RemoveOldPlan(oldTrainService);
                    lock (SchedulePlans)
                    {
                        SchedulePlans.Enqueue(plan);
                    }
                }
                else
                {
                    lock (SchedulePlans)
                    {
                        SchedulePlans.Enqueue(plan);
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
        }
        private TrainService? FindTrainService(ScheduledPlan theNewPlan)
        {
            var planName = theNewPlan.Name;
            var planDayCode = theNewPlan.ScheduledDayCode;
            try
            {
                foreach (var ts in MyTrainServices)
                {
                    if (ts.ScheduledPlanName == planName && ts.ScheduledDayCode == planDayCode) return ts;
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }

            return null;
        }
        private void RemoveOldPlan(TrainService? theTrainService)
        {
            try
            {
                if (theTrainService == null) return;
                var tripList = new List<Trip>(GlobalDeclarations.TripList);
                var tempList = new List<Trip>();
                foreach (var t in tripList)
                {
                    if (t.ScheduledPlanDayCode == theTrainService.ScheduledDayCode &&
                        t.ScheduledPlanId == theTrainService.ScheduledPlanName) tempList.Add(t);
                }

                foreach (var t in tempList)
                {
                    lock (TripList)
                    {
                        MyTrainSchedulerManager?.ProduceMessage2002(t);
                        var tripRemoved = TripList.Remove(t);
                        MyLogger?.LogInfo("Train Service Was Updated and Removing Old Trip <" + t.TripCode + "> <" + tripRemoved + ">");
                    }
                }

                var trainServiceRemoved = MyTrainServices.Remove(theTrainService);
                MyLogger?.LogInfo("Train Service Was Updated and Removing Old Train Service <" + theTrainService.ScheduledPlanName + "> <" + theTrainService.ScheduledDayCode + "> <" + trainServiceRemoved + ">");

            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
        }
        private void MovePlansToArchiveFolder()
        {
            try
            {
                foreach (var p in ReadyToArchive)
                {
                    var archFullPath = Path.Combine(PlanArchiveFolderPath, p.Key);
                    if (File.Exists(archFullPath))
                    {
                        File.Delete(archFullPath);
                    }
                    var movePath = Path.Combine(PlanFolderPath, p.Key);
                    File.Move(movePath, archFullPath);
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
        }
        private void ForceArchivePlan(string fileToArchive)
        {
            try
            {
                var archFullPath = Path.Combine(PlanArchiveFolderPath, fileToArchive);

                if (File.Exists(archFullPath))
                {
                    File.Delete(archFullPath);
                }
                //var movePath = Path.Combine(PlanFolderPath, fileToArchive);
                //File.Move(movePath, archFullPath);

            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
        }
        private bool ValidatePlan(ScheduledPlan plan)
        {
            try
            {
                var planStartTime = plan.StartTime.DateTime;
                if (MyUseLocalTime)
                {
                    planStartTime = plan.StartTime.DateTime.ToLocalTime();
                }

                if (DateTime.Now.AddHours(-6) < planStartTime)
                //if (planStartTime > DateTime.Now.AddHours(-6) && planStartTime < DateTime.Now.AddHours(10))
                {
                    return !DoesPlanExistWithSameStartTime(plan);
                }
                //return true;
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
            MyLogger?.LogInfo("Scheduled Plan is old <" + plan.Name + "><" + plan.StartTime.DateTime.ToString("dd-MM-yy HH:mm" + ">"));
            return false;
        }
        private bool DoesPlanExistWithDifferentStartTime(ScheduledPlan plan)
        {
            foreach (var ts in MyTrainServices)
            {
                if (ts.ScheduledPlanName == plan.Name && ts.ScheduledDayCode == plan.ScheduledDayCode && plan.StartTime.DateTime != ts.StartTime )
                {
                    MyLogger?.LogInfo("Train Service Plan Exists with Difference Start Time... Name <" + ts.ScheduledPlanName + "> DayCode <" + ts.ScheduledDayCode + ">");
                    return true;
                }
            }

            return false;
        }
        private bool DoesPlanExistWithSameStartTime(ScheduledPlan plan)
        {
            foreach (var ts in MyTrainServices)
            {
                if (ts.ScheduledPlanName == plan.Name && ts.ScheduledDayCode == plan.ScheduledDayCode && plan.StartTime.DateTime == ts.StartTime)
                {
                    MyLogger?.LogInfo("Train Service Plan Exists with Same Start Time... Name <" + ts.ScheduledPlanName + "> DayCode <" + ts.ScheduledDayCode + "> StartTime <" +ts.StartTime +">");
                    return true;
                }
            }

            return false;
        }
        private Trip? GetTrip(string tripCode, string startTime)
        {
            foreach (var trip in GlobalDeclarations.TripList)
            {
                if (trip.TripCode == tripCode && trip.StartTime == startTime)
                {
                    MyLogger?.LogInfo("GetTrip:Trip Plan Exists <" + tripCode + ">");
                    return trip;
                }
            }
            return null;
        }
        private bool DoesTripExist(string TripCode, string startTime)
        {
            foreach (var trip in GlobalDeclarations.TripList)
            {
                if (trip.TripCode == TripCode && trip.StartTime == startTime)
                {
                    MyLogger?.LogInfo("Trip Plan Already Exists <" + TripCode + ">");
                    return true;
                }
            }
            return false;
        }
        private void CreateTrainService(ScheduledPlan plan)
        {
            try
            {
                var ts = new TrainService
                {
                    ScheduledDayCode = plan.Key.ScheduledDayCode,
                    ScheduledPlanName = plan.Key.ScheduledPlanName,
                    LineId = plan.LineId,
                    TrainTypeId = plan.TrainTypeId,
                    StartTime = plan.StartTime.DateTime
                };
                foreach (var kv in plan.Trips)
                {
                    if (DoesTripExist(kv.Value.TripCode, kv.Value.StartTime.DateTime.ToString("MM/dd/yy HH:mm")))
                    {
                        MyLogger?.LogInfo("<" + kv.Value.TripCode + "> Trip Already Exists At Time <" + kv.Value.StartTime.DateTime.ToString("MM/dd/yy HH:mm") +">");
                        continue;
                    }
                    var startPosition = kv.Value.StartPos.Offset + kv.Value.StartPos.AdditionalPos;
                    var endPosition = kv.Value.EndPos.Offset + kv.Value.EndPos.AdditionalPos;

                    var tripStartTime = kv.Value.StartTime.DateTime.ToString("MM/dd/yy HH:mm");
                    var planStartTime = plan.StartTime.DateTime.ToLocalTime();
                    if (MyUseLocalTime)
                    {
                        MyLogger?.LogInfo("<" + kv.Value.TripCode +"> Current Trip Start Time <" + tripStartTime + ">");
                        tripStartTime = kv.Value.StartTime.DateTime.ToLocalTime().ToString("MM/dd/yy HH:mm");
                        MyLogger?.LogInfo("<" + kv.Value.TripCode + "> Adjusted Trip Start Time <" + tripStartTime + ">");
                    }

                    var trip = new Trip(MyLogger!)
                    {
                        Name = kv.Value.Name,
                        ScheduledPlanName = ts.ScheduledPlanName,
                        StartTime = tripStartTime,
                        TripCode = kv.Value.TripCode,
                        Number = kv.Value.TripNumber,
                        Direction = (startPosition > endPosition) ? "L" : "R", 
                        TripId = kv.Value.Id, 
                        StartPosition = kv.Value.StartPos.AdditionalName,
                        EndPosition = kv.Value.EndPos.AdditionalName,
                        PlanStartTime = planStartTime,
                        SerUid = kv.Value.Id,
                        ScheduledPlanId = kv.Value.ScheduledPlanId,
                        ScheduledPlanDayCode = kv.Value.ScheduledDayCode

                    };
                    trip.SetTrainType(ts.TrainTypeId);
                    var locations = kv.Value.TimedLocations;
                    var startIndex = kv.Key;
                    if (!(locations?.Count > 0)) continue;
                    foreach (var loc in locations)
                    {
                        if (loc == null || !loc.HasStopping) continue;

                        var platform = _railwayNetworkManager?.FindPlatform(loc.Description);
                        if (platform == null)
                        {
                            MyLogger?.LogCriticalError($"Platform Not Found {loc.Description}");
                            //continue;
                        }

                        Station stn = null;
                        var stationName = "UNKNOWN";
                        if (platform != null) stn = _railwayNetworkManager?.FindStationById(platform.StationId);
                        if (stn != null) stationName = stn.StationName;
                        //var stn = _railwayNetworkManager?.FindStationById(platform.StationId);
                        var locArrivalTime = loc.Arrival.DateTime;
                        var locDepartureTime = loc.Departure.DateTime;
                        if (MyUseLocalTime)
                        {
                            MyLogger?.LogInfo("<" + trip.TripCode + "> Current Trip Arrival Time <" + locArrivalTime + ">");
                            MyLogger?.LogInfo("<" + trip.TripCode + "> Current Trip Depart Time <" + locDepartureTime + ">");

                            locArrivalTime = loc.Arrival.DateTime.ToLocalTime();
                            locDepartureTime = loc.Departure.DateTime.ToLocalTime();
                            MyLogger?.LogInfo("<" + trip.TripCode + "> Adjusted Trip Arrival Time <" + locArrivalTime + ">");
                            MyLogger?.LogInfo("<" + trip.TripCode + "> Adjusted Trip Depart Time <" + locDepartureTime + ">");
                        }
                        var timeLoc = new TimedLocation
                        {
                            Description = loc.Description,
                            Id = loc.Id,
                            TripId = loc.TripId,
                            ArrivalTimePlan = locArrivalTime,
                            DepartureTimePlan = locDepartureTime,
                            HasStopping = loc.HasStopping,
                            Position = TimedLocationPosition.CreateInstance(loc.Pos.ElementId, loc.Pos.Offset.ToString(), loc.Pos.AdditionalPos.ToString(), loc.Pos.AdditionalName),
                            MyPlatform = platform,
                            MyStationName = stationName
                        };

                        timeLoc.InitializeTime();
                        trip.TimedLocations.Add(timeLoc);

                    }

                    if (string.IsNullOrEmpty(trip.TripCode))
                    {
                        MyLogger?.LogCriticalError($"Trip Code Not Found in Plan {ts.ScheduledPlanName}");
                        continue;
                    }

                    ts.Trips.Add(kv.Key, trip);
                    ts.TripList.Add(trip);
                    GlobalDeclarations.TripList.Add(trip);
                    trip.CreateTripReservationsFromPlan(plan, _railwayNetworkManager?.MyStations!);
                    trip.CreateTriggerPoints();
                    trip.CreateSignalPoints();
                    trip.CreatePlatformPoints();
                    trip.DoIdentifyStationNecks();
                    trip.DoStationNeckConflictCheck();
                    //trip.DoConflictCheckForPlatform();
                    MyLogger?.LogInfo(ts.GetServiceInformation() + trip.GetTripInformation());
                    GlobalDeclarations.MyTrainSchedulerManager?.ProduceMessage2001(trip);
                    GlobalDeclarations.MyAutoRoutingManager?.BuildNewForecastToPublishFromPlan(trip);
                }
                MyTrainServices.Add(ts);
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
        }
        public void DeleteSchedulePlan(int scheduledDayCode, string scheduledPlanName)
        {
            try
            {
                var thread = new Thread(() => DoDeleteSchedulePlan(scheduledDayCode, scheduledPlanName)); thread.Start();
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }
        private void DoDeleteSchedulePlan(int scheduledDayCode, string scheduledPlanName)
        {
            try
            {
                lock (MyTrainServices)
                {
                    foreach (var ts in MyTrainServices)
                    {
                        if (ts.ScheduledDayCode == scheduledDayCode && ts.ScheduledPlanName == scheduledPlanName)
                        {
                            MyLogger?.LogInfo("DoDeleteSchedulePlan:Schedule Found for Deletion <" + scheduledDayCode + "><" + scheduledPlanName + ">");
                            foreach (var trip in ts.TripList)
                            {
                                MyAutoRoutingManager?.ReleaseReservationsConflictsFromDeletedTrip(trip);
                                MyAutoRoutingManager?.TripDelete(trip,null);
                            }

                            MyTrainServices.Remove(ts);
                            return;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }
        private void DoCleanUpTrainServices()
        {
            try
            {
                Thread.Sleep(20000);
                while (true)
                {
                    try
                    {

                        var tempServices = new List<TrainService>();
                        lock (MyTrainServices)
                        {
                            tempServices = new List<TrainService>(MyTrainServices);
                        }

                        foreach (var ts in tempServices)
                        {
                            var planStartTime = ts.StartTime;
                            if (MyUseLocalTime)
                            {
                                planStartTime = ts.StartTime.ToLocalTime();
                            }

                            if (planStartTime < DateTime.Now.AddHours(-MyTrainServiceRetentionInHoursHistorical))
                            {
                                foreach (var trip in ts.TripList)
                                {
                                    MyAutoRoutingManager?.ReleaseReservationsConflictsFromDeletedTrip(trip);
                                    MyAutoRoutingManager?.TripDelete(trip, null);
                                }
                                MyTrainServices.Remove(ts);
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        MyLogger?.LogException(e.ToString());
                    }
                    Thread.Sleep(60000);
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }
        public void UpdateSchedulePlan(int scheduledDayCode, string scheduledPlanName, string thePlan)
        {
            try
            {
                var thread = new Thread(() => DoUpdateSchedulePlan(scheduledDayCode, scheduledPlanName, thePlan)); thread.Start();
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }
        private void DoUpdateSchedulePlan(int scheduledDayCode, string scheduledPlanName, string thePlan)
        {
            try
            {
                var plan = DeserializeMyObject<ScheduledPlan>(MyLogger, thePlan);
                if (plan == null) return;
                foreach (var ts in MyTrainServices.Where(ts => ts.ScheduledDayCode == scheduledDayCode && ts.ScheduledPlanName == scheduledPlanName))
                {
                    if (!DoSchedulePlanCompare(ts, plan))
                    {
                        MyLogger?.LogInfo("DoSchedulePlanCompare:Schedule DID NOT Compare for Update <" + scheduledDayCode + "><" + scheduledPlanName + ">");
                        PerformUpdateSchedulePlan(scheduledDayCode, scheduledPlanName, thePlan);
                    }
                    else
                    {
                        MyLogger?.LogInfo("DoSchedulePlanCompare:Schedule DID Compare for Update <" + scheduledDayCode + "><" + scheduledPlanName + ">");

                    }
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }
        private void PerformUpdateSchedulePlan(int scheduledDayCode, string scheduledPlanName, string thePlan)
        {
            try
            {
                lock (MyTrainServices)
                {
                    foreach (var ts in MyTrainServices)
                    {
                        if (ts.ScheduledDayCode == scheduledDayCode && ts.ScheduledPlanName == scheduledPlanName)
                        {
                            MyLogger?.LogInfo("DoUpdateSchedulePlan:Schedule Found for Update <" + scheduledDayCode + "><" + scheduledPlanName + ">");
                            foreach (var trip in ts.TripList)
                            {
                                if (trip.IsAllocated)
                                {
                                    MyLogger?.LogInfo("DoUpdateSchedulePlan:Trip Is Allocate. Schedule Plan was NOT Update <" + scheduledDayCode + "><" + scheduledPlanName + "><" + trip.TripCode + ">");
                                    return;
                                }
                            }

                            foreach (var trip in ts.TripList)
                            {
                                MyLogger?.LogInfo("DoUpdateSchedulePlan:Cleaning Up Old Trip in Schedule Plan <" + scheduledDayCode + "><" + scheduledPlanName + "><" + trip.TripCode + ">");
                                MyAutoRoutingManager?.ReleaseReservationsConflictsFromDeletedTrip(trip);
                                MyAutoRoutingManager?.TripDelete(trip, null);
                            }
                            MyTrainServices.Remove(ts);

                            var plan = DeserializeMyObject<ScheduledPlan>(MyLogger, thePlan);
                            lock (SchedulePlans)
                            {
                                SchedulePlans.Enqueue(plan);
                            }
                            return;
                        }
                    }
                    MyLogger?.LogInfo("DoUpdateSchedulePlan:Schedule NOT Found for Update <" + scheduledDayCode + "><" + scheduledPlanName + ">");
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }
        private bool DoSchedulePlanCompare(TrainService theService, ScheduledPlan theNewPlan)
        {
            try
            {
                MyAutoRoutingManager?.DoSerializeSchedulePlanChange(theNewPlan);
                var newService = CreateTemporaryTrainService(theNewPlan);
                if (newService.TripList.Count == 0) return true;
                foreach (var ts in MyTrainServices.Where(ts => ts.ScheduledDayCode == theService.ScheduledDayCode && ts.ScheduledPlanName == theService.ScheduledPlanName))
                {
                    foreach (var trip in ts.TripList)
                    {
                        var tripIndex = ts.TripList.IndexOf(trip);
                        var otherTrip = newService.TripList[tripIndex];
                        if (trip.TripCode != otherTrip.TripCode) return false;

                        foreach (var tl in trip.TimedLocations)
                        {
                            var index = trip.TimedLocations.IndexOf(tl);
                            var otherTimedLocation = otherTrip.TimedLocations[index];
                            if (!DoCompareTrips(tl, otherTimedLocation)) return false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
            return true;
        }
        private bool DoCompareTrips(TimedLocation theLocation, TimedLocation theNewLocation)
        {
            try
            {
                if (theLocation.MyMovementPlan.FromName != theNewLocation.MyMovementPlan.FromName) return false;
                if (theLocation.MyMovementPlan.ToName != theNewLocation.MyMovementPlan.ToName) return false;
                if (theLocation.ArrivalTimePlan != theNewLocation.ArrivalTimePlan) return false;
                if (theLocation.DepartureTimePlan != theNewLocation.DepartureTimePlan) return false;

            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
            return true;
        }
        private TrainService CreateTemporaryTrainService(ScheduledPlan plan)
        {
            var ts = new TrainService();

            try
            {
                ts.ScheduledDayCode = plan.Key.ScheduledDayCode;
                ts.ScheduledPlanName = plan.Key.ScheduledPlanName;
                ts.LineId = plan.LineId;
                ts.TrainTypeId = plan.TrainTypeId;
                ts.StartTime = plan.StartTime.DateTime;
                foreach (var kv in plan.Trips)
                {
                    if (DoesTripExist(kv.Value.TripCode, kv.Value.StartTime.DateTime.ToString("MM/dd/yy HH:mm"))) continue;
                    var startPosition = kv.Value.StartPos.Offset + kv.Value.StartPos.AdditionalPos;
                    var endPosition = kv.Value.EndPos.Offset + kv.Value.EndPos.AdditionalPos;

                    var tripStartTime = kv.Value.StartTime.DateTime.ToString("MM/dd/yy HH:mm");
                    var planStartTime = plan.StartTime.DateTime.ToLocalTime();
                    if (MyUseLocalTime)
                    {
                        tripStartTime = kv.Value.StartTime.DateTime.ToLocalTime().ToString("MM/dd/yy HH:mm");
                    }

                    var trip = new Trip(MyLogger!)
                    {
                        Name = kv.Value.Name,
                        ScheduledPlanName = ts.ScheduledPlanName,
                        StartTime = tripStartTime,
                        TripCode = kv.Value.TripCode,
                        Number = kv.Value.TripNumber,
                        Direction = (startPosition > endPosition) ? "L" : "R",
                        TripId = kv.Value.Id,
                        StartPosition = kv.Value.StartPos.AdditionalName,
                        EndPosition = kv.Value.EndPos.AdditionalName,
                        PlanStartTime = planStartTime,
                        SerUid = kv.Value.Id,
                        ScheduledPlanId = kv.Value.ScheduledPlanId,
                        ScheduledPlanDayCode = kv.Value.ScheduledDayCode

                    };
                    var locations = kv.Value.TimedLocations;
                    var startIndex = kv.Key;
                    if (!(locations?.Count > 0)) continue;
                    foreach (var loc in locations)
                    {
                        if (loc == null || !loc.HasStopping) continue;

                        var platform = _railwayNetworkManager?.FindPlatform(loc.Description);
                        if (platform == null) continue;
                        var stn = _railwayNetworkManager?.FindStationById(platform.StationId);
                        var locArrivalTime = loc.Arrival.DateTime;
                        var locDepartureTime = loc.Departure.DateTime;
                        if (MyUseLocalTime)
                        {
                            locArrivalTime = loc.Arrival.DateTime.ToLocalTime();
                            locDepartureTime = loc.Departure.DateTime.ToLocalTime();
                        }

                        var timeLoc = new TimedLocation
                        {
                            Description = loc.Description,
                            Id = loc.Id,
                            TripId = loc.TripId,
                            ArrivalTimePlan = locArrivalTime,
                            DepartureTimePlan = locDepartureTime,
                            HasStopping = loc.HasStopping,
                            Position = TimedLocationPosition.CreateInstance(loc.Pos.ElementId,
                                loc.Pos.Offset.ToString(), loc.Pos.AdditionalPos.ToString(), loc.Pos.AdditionalName),
                            MyPlatform = platform
                        };
                        timeLoc.InitializeTime();
                        trip.TimedLocations.Add(timeLoc);
                    }
                    trip.CreateTripReservationsFromPlan(plan, _railwayNetworkManager?.MyStations!);
                    ts.TripList.Add(trip);
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }

            return ts;
        }
        #endregion

    }
}
