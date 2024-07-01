using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConflictManagementLibrary.Communications;
using ConflictManagementLibrary.Helpers;
using ConflictManagementLibrary.Logging;
using ConflictManagementLibrary.Model.Trip;
using static ConflictManagementLibrary.Helpers.GlobalDeclarations;

namespace ConflictManagementLibrary.Management
{
    public class InitializationManager
    {
        #region Constructors
        private InitializationManager(bool FlipRouting = false, bool initializeNetworkManager = true, bool initializeTrainScheduleManager = true, bool initializeTrainForecastManager = true)
        {
            InitializeLogging();
            InitializeConfigurationFile();
            LoadRuntimeConfiguration();
            LoadIncreaseDecreaseConfiguration();
            InitializeMessageBroker(MyLogger, FlipRouting);
            if (initializeTrainScheduleManager) InitializeTrainScheduleManager();
            if (initializeNetworkManager) InitializeRailwayNetworkManager();
            if (!initializeTrainForecastManager) return;
            InitializeTrainForecastManager();
            InitializeAutoRoutingManager();
            MyTrainSchedulerManager!.ProduceMessage1100(MyEnableAutomaticConflictResolution);
        }
        private InitializationManager(bool initializeNetworkManager)
        {
            InitializeLogging();
            InitializeRailwayNetworkManager();
        }

        #endregion

        #region Initialization Methods
        private void InitializeConfigurationFile()
        {
            try
            {
                var thecfg = System.Configuration.ConfigurationManager.GetSection("LibraryConfigurationFileNames") as NameValueCollection;
                var fileMap = new ExeConfigurationFileMap
                {
                    ExeConfigFilename = Environment.CurrentDirectory + @"\" + thecfg?["ConflictManagementLibrary"]
                };
                var cfg = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);

                MyEnableAutomaticConflictResolution = Convert.ToBoolean(cfg.AppSettings.Settings["EnableAutomaticConflictResolution"].Value);
                MyEnableSerializeSchedulePlan = Convert.ToBoolean(cfg.AppSettings.Settings["EnableSerializeSchedulePlan"].Value);
                MyEnableSerializeRoutePlan = Convert.ToBoolean(cfg.AppSettings.Settings["EnableSerializeRoutePlan"].Value);
                MyEnableSerializeTrain = Convert.ToBoolean(cfg.AppSettings.Settings["EnableSerializeTrain"].Value);
                MyDisableDepartTimeCheckForAutoRouting = Convert.ToBoolean(cfg.AppSettings.Settings["DisableDepartTimeCheckForAutoRouting"].Value);
                MyUseLocalTime = Convert.ToBoolean(cfg.AppSettings.Settings["UseLocalTime"].Value);
                MyEnableRouteActionTriggerPoints = Convert.ToBoolean(cfg.AppSettings.Settings["EnableRouteActionTriggerPoints"].Value);
                AppLoggingGlobalDeclarations.MyLoggingDebugEventsEnabled = Convert.ToBoolean(cfg.AppSettings.Settings["EnableDebugMode"].Value);
                MyArchiveEventInHours = Convert.ToInt32(cfg.AppSettings.Settings["ArchiveLogFilesInHours"].Value);
                MyDisableExecutionTimeInRoutePlan = Convert.ToBoolean(cfg.AppSettings.Settings["DisableExecutionTimeInRoutePlan"].Value);
                MyTrainServiceRetentionInHoursHistorical = Convert.ToInt32(cfg.AppSettings.Settings["TrainServiceRetentionInHoursHistorical"].Value);
                MyTrainServiceRetentionInHoursFuture = Convert.ToInt32(cfg.AppSettings.Settings["TrainServiceRetentionInHoursFuture"].Value);
                MyForceAutomaticRoutingInSeconds = Convert.ToInt32(cfg.AppSettings.Settings["ForceAutomaticRoutingInSeconds"].Value);
                MyEnableRouteMarkingsFlag = Convert.ToBoolean(cfg.AppSettings.Settings["EnableRouteMarkings"].Value);
                MyEnableAutomaticRoutingSettingFlag = Convert.ToBoolean(cfg.AppSettings.Settings["EnableAutomaticRoutingSetting"].Value);
            }
            catch (Exception e)
            {
                MyLogger?.LogException(value: e.ToString());
            }
        }
        private void LoadIncreaseDecreaseConfiguration()
        {
            try
            {
                var workingDirectory = GetExecutingDirectoryName();
                var increaseDecreaseTimeList = workingDirectory + @"\Data\IncreaseDecreaseTimes.cfg";
                if (File.Exists(increaseDecreaseTimeList))
                {
                    foreach (string line in File.ReadLines(increaseDecreaseTimeList))
                    {
                        if (line.Contains("/")) continue;
                        var v = line.Split(',');
                        if (v.Length < 9) continue;
                        var time = IncreaseDecreaseTimes.CreateInstance(v[0], v[1], v[2], v[3], v[4], v[5], v[6], v[7], v[8]);
                        MyIncreaseDecreaseTimesList.Add(time);
                    }

                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(value: e.ToString());
            }
        }

        private void LoadRuntimeConfiguration()
        {
            try
            {
                var workingDirectory = GetExecutingDirectoryName();
                var runTimeList = workingDirectory + @"\Data\RunTimes.cfg";
                if (File.Exists(runTimeList))
                {
                    foreach (string line in File.ReadLines(runTimeList))
                    {
                        if (line.Contains("/")) continue;
                        var v = line.Split(',');
                        if (v.Length < 5) continue;
                        var runTime = RunningTimes.CreateInstance(v[0], v[1], v[2], v[3], v[4]);
                        MyRunningTimesList.Add(runTime);
                    }

                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(value: e.ToString());
            }
        }
        private InitializationManager()
        {
            InitializeMessageBroker(GlobalDeclarations.MyLogger, true);
        }
        public static InitializationManager CreateInstanceMessageBroker()
        {
            return new InitializationManager();
        }
        public static InitializationManager CreateRailwayNetworkManager(bool InitializeNetworkManager)
        {
            return new InitializationManager(InitializeNetworkManager);
        }
        public static InitializationManager CreateInstance(bool FlipRouting = false, bool initializeNetworkManager = true, bool initializeTrainScheduleManager = true, bool initializeTrainForecastManager = true)
        {
            return new InitializationManager(FlipRouting, initializeNetworkManager, initializeTrainScheduleManager, initializeTrainForecastManager);
        }
        private void InitializeLogging()
        {
            try
            {
                MyFileArchiveManager = AppArchiveManager.CreateInstance(MyArchivePath, MyLogger);
                //MyLogger.LogInfo("Archive Logging Initialized...");

                MyLogger = ConflictManagementLibrary.Logging.AppLoggingStart.CreateLogger();
                MyLogger?.LogInfo("Event Logging Initialized...");
                var theApplicationVersion = MyAppVersion; //System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
                MyLogger?.LogInfo("CMS Application Version <" + theApplicationVersion +">");

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        private static void InitializeMessageBroker(IMyLogger? theLogger, bool FlipRouting = false)
        {
            MyLogger?.LogInfo("Message Broker Initializing...");
            MyExchangeManager = AppExchangeManager.CreateInstance(GlobalDeclarations.MyLogger, FlipRouting);
        }
        private void InitializeTrainScheduleManager()
        {
            MyLogger.LogInfo("Train Schedule Manager Initializing...");
            MyTrainSchedulerManager = TrainSchedulerManager.CreateInstance(MyLogger,MyExchangeManager);
        }
        private void InitializeRailwayNetworkManager()
        {
            MyLogger.LogInfo("Railway Network Manager Initializing...");
            MyRailwayNetworkManager = new RailwayNetworkManager(MyLogger);
        }
        private void InitializeTrainForecastManager()
        {
            MyLogger.LogInfo("Train Forecast Manager Initializing...");
            MyTrainForecastManager = TrainForecastManager.CreateInstance(MyLogger, MyRailwayNetworkManager, MyTrainSchedulerManager);
        }
        private void InitializeAutoRoutingManager()
        {
            MyLogger.LogInfo("Train Auto-Routing Manager Initializing...");
            MyAutoRoutingManager = TrainAutoRoutingManager.CreateInstance(MyLogger);
        }

        #endregion

    }
}