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
        public ConcurrentBag<TrainService> MyTrainServices { get; set; } = new ConcurrentBag<TrainService>();
        private ConcurrentDictionary<string, ScheduledPlan>? MyArchiveList { get; set; }
        private readonly RailwayNetworkManager _railwayNetworkManager;
        private readonly TrainSchedulerManager theTrainSchedulerManager;
        private IMyLogger MyLogger { get; }

        #endregion

        #region Constructor
        private TrainForecastManager(IMyLogger theLogger, RailwayNetworkManager railwayNetworkManager, TrainSchedulerManager trainSchedulerManager)
        {
            MyLogger = theLogger;
            _railwayNetworkManager = railwayNetworkManager;// new RailwayNetworkManager(theLogger);
            theTrainSchedulerManager = trainSchedulerManager;
            StartSchedulePlanProcessing();
        }
        public static TrainForecastManager CreateInstance(IMyLogger theLogger, RailwayNetworkManager railwayNetworkManager, TrainSchedulerManager trainSchedulerManager)
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
                var plan = DeserializeMyObject<ScheduledPlan>(MyLogger, fullPath);
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
                var plan = DeserializeMyObject<ScheduledPlan>(MyLogger, fullPath);
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
                    var trip = new Trip()
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
                            ArrivalTime = loc.Arrival.DateTime,
                            DepartureTime = loc.Departure.DateTime,
                            HasStopping = loc.HasStopping,
                            Position = TimedLocationPosition.CreateInstance(loc.Pos.ElementId, loc.Pos.Offset.ToString(), loc.Pos.AdditionalPos.ToString(), loc.Pos.AdditionalName)
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
                while (true)
                {
                    try
                    {
                        CheckFolderExists();
                        ProcessPlanFolder();
                        MovePlansToArchiveFolder();
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

                        var plan = AddPlan(filename);
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
        public ScheduledPlan AddPlan(string theData)
        {
            try
            {
                var plan = DeserializeMyObject<ScheduledPlan>(MyLogger, theData);
                if (plan != null && ValidatePlan(plan))
                {
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
                    if (DateTime.Now.AddSeconds(-30) < plan.StartTime.DateTime)
                    {
                        return !DoesPlanExist(plan);
                    }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }

            return false;
        }
        private bool DoesPlanExist(ScheduledPlan plan)
        {
            foreach (var ts in MyTrainServices)
            {
                if (ts.ScheduledPlanName == plan.Name && ts.ScheduledDayCode == plan.ScheduledDayCode)
                {
                    MyLogger.LogCriticalError("Train Service Plan Already Exists <" + ts.ScheduledPlanName + ">");
                    return true;
                }
            }

            return false;
        }
        private bool DoesTripExist(string TripCode, string startTime)
        {
            foreach (var trip in GlobalDeclarations.TripList)
            {
                if (trip.TripCode == TripCode && trip.StartTime == startTime)
                {
                    MyLogger.LogCriticalError("Trip Plan Already Exists <" + TripCode + ">");
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
                };
                foreach (var kv in plan.Trips)
                {
                    if (DoesTripExist(kv.Value.TripCode, kv.Value.StartTime.DateTime.ToString("dd/MM/yy HH:mm"))) continue;
                    var trip = new Trip()
                    {
                        Name = kv.Value.Name,
                        ServiceName = ts.ScheduledPlanName,
                        StartTime = kv.Value.StartTime.DateTime.ToString("dd/MM/yy HH:mm"),
                        TripCode = kv.Value.TripCode,
                        Number = kv.Value.TripNumber,
                        Direction = (kv.Value.StartPos.Offset > kv.Value.EndPos.Offset) ? "L" : "R", 
                        TripId = kv.Value.Id, 
                        StartPosition = kv.Value.StartPos.AdditionalName,
                        EndPosition = kv.Value.EndPos.AdditionalName,

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
                            ArrivalTime = loc.Arrival.DateTime,
                            DepartureTime = loc.Departure.DateTime,
                            HasStopping = loc.HasStopping,
                            Position = TimedLocationPosition.CreateInstance(loc.Pos.ElementId, loc.Pos.Offset.ToString(), loc.Pos.AdditionalPos.ToString(), loc.Pos.AdditionalName)
                        };
                        //if (stn.CheckStationReserved(timeLoc) || !stn.AddReservations(timeLoc)) continue;
                        trip.TimedLocations.Add(timeLoc);
                    }
                    ts.Trips.Add(kv.Key, trip);
                    GlobalDeclarations.TripList.Add(trip);
                    //trip.CreateReservations(_railwayNetworkManager.MyMovementPlans, _railwayNetworkManager.MyStations);
                    trip.CreateTripReservations(_railwayNetworkManager.MyMovementPlans, _railwayNetworkManager.MyStations);
                    MyLogger?.LogInfo(ts.GetServiceInformation() + trip.GetTripInformation());
                    GlobalDeclarations.MyTrainSchedulerManager.ProduceMessage2001(trip);
                }
                MyTrainServices.Add(ts);

            }
            catch (Exception e)
            {
                MyLogger?.LogException(e);
            }
        }

        #endregion

    }
}
