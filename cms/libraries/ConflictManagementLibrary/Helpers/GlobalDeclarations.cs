using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConflictManagementLibrary.Communications;
using ConflictManagementLibrary.Logging;
using ConflictManagementLibrary.Management;
using ConflictManagementLibrary.Model.Movement;
using ConflictManagementLibrary.Model.Possession;
using ConflictManagementLibrary.Model.Trip;
using Newtonsoft.Json;
using NLog;

namespace ConflictManagementLibrary.Helpers
{
    public static class GlobalDeclarations
    {
        public static AppExchangeManager? MyExchangeManager;
        public static IMyLogger? MyLogger { get; set; }
        public static TrainSchedulerManager? MyTrainSchedulerManager;
        public static RailwayNetworkManager? MyRailwayNetworkManager { get; set; }
        public static TrainForecastManager? MyTrainForecastManager { get; set; }
        public static RailGraphManager? MyRailGraphManager { get; set; }
        public static TrainAutoRoutingManager? MyAutoRoutingManager { get; set; }
        public static ObservableCollection<Trip> TripList { get; set; } = new ObservableCollection<Trip>();
        public static string CurrentUiCulture { get; set; } = "en-US";
        public static string MyArchivePath = GetExecutingDirectoryName() + @"\Archives\";
        public static AppArchiveManager MyFileArchiveManager { get; set; } = null!;
        public static bool MyEnableAutomaticConflictResolution = true;
        public static bool MyEnableSerializeSchedulePlan = false;
        public static bool MyEnableSerializeRoutePlan = false;
        public static bool MyEnableSerializeTrain = false;
        public static bool MyDisableDepartTimeCheckForAutoRouting = false;
        public static bool MyUseLocalTime = false;
        public static bool MyEnableRouteActionTriggerPoints = false;
        public static int MyArchiveEventInHours = 4;
        public static string MyAppVersion = "1.0.0.62";
        public static bool MyDisableExecutionTimeInRoutePlan = false;
        public static List<RunningTimes> MyRunningTimesList = new List<RunningTimes>();
        public static List<IncreaseDecreaseTimes> MyIncreaseDecreaseTimesList = new List<IncreaseDecreaseTimes>();
        public static List<RouteActionInfo> MyRouteActionInfoList = new List<RouteActionInfo>();
        public static int MyTrainServiceRetentionInHoursHistorical = 10;
        public static int MyTrainServiceRetentionInHoursFuture = 10;
        private static int _forceAutomaticRoutingInSeconds;
        public static List<Possession> MyPossessions = new List<Possession>();
        public static int MyForceAutomaticRoutingInSeconds
        {
            get { return _forceAutomaticRoutingInSeconds; }
            set { _forceAutomaticRoutingInSeconds = value < 10 ? 10 : value; }
        }

        public static bool MyEnableRouteMarkingsFlag = true;
        public static bool MyEnableAutomaticRoutingSettingFlag = true;

        #region Public Functions
        public static T DeserializeMyObjectFromFile<T>(IMyLogger? thisLogger, string fullPath)
        {
            try
            {
                using (var r = new StreamReader(fullPath))
                {
                    var json = r.ReadToEnd();
                    var ro = JsonConvert.DeserializeObject<T>(json);
                    return ro;
                }

            }
            catch (Exception e)
            {
                thisLogger?.LogException(e);
                Console.WriteLine(e);
            }

            return default;
        }
        public static T DeserializeMyObject<T>(IMyLogger? thisLogger, string theData)
        {
            try
            {
                    return JsonConvert.DeserializeObject<T>(theData);

            }
            catch (Exception e)
            {
                thisLogger?.LogException(e);
                Console.WriteLine(e);
            }

            return default;
        }
        public static Trip FindTrip(string tripCode, string startTime)
        {
            foreach (var trip in TripList)
            {
                if (trip.TripCode == tripCode && trip.StartTime == startTime) return trip;
            }
            return null;
        }
        public static string GetExecutingDirectoryName()
        {
            var strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            return System.IO.Path.GetDirectoryName(strExeFilePath);
        }
        #endregion
    }
}
