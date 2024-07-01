using System.Threading;
//using System.Configuration;
using Microsoft.Extensions.Configuration;
using static E2KService.ServiceStateHelper;
using ConflictManagementService.Model;

namespace E2KService;

using ActiveMQ;
using MessageHandler;
using Serilog;
using ConflictManagementLibrary.Management;
using static ConflictManagementLibrary.Helpers.GlobalDeclarations;
using System;
using System.Collections.Generic;


////////////////////////////////////////////////////////////////////////////////
// 
// Log levels:
//    Trace - very detailed logs, which may include high-volume information such as protocol payloads. This log level is typically only enabled during development
//    Debug - debugging information, less detailed than trace, typically not enabled in production environment.
//    Info - information messages, which are normally enabled in production environment
//    Warn - warning messages, typically for non-critical issues, which can be recovered or which are temporary failures
//    Error - error messages - most of the time these are Exceptions
//    Fatal - very serious errors!
// 
////////////////////////////////////////////////////////////////////////////////

class ServiceImp
{
    public static ServiceImp? Service { get; set; }

    public ServiceState ServiceState { get => serviceState; }
    private ServiceState serviceState = ServiceState.Offline;

    public Connection? Connection { get; set; }
    internal DataHandler? DataHandler => dataHandler;
    internal RailgraphHandler? RailgraphHandler => railgraphHandler;
    internal IRosMessaging? RosMessaging => rosMessagingHandler;

    private DataHandler? dataHandler;
    private RailgraphHandler? railgraphHandler;
    private WDSMessageHandler? wdsMessageHandler;
    private TimeDistanceGraphDataHandler? timeDistanceGraphDataHandler;
    private TrainInformationAndCommandHandler? trainInformationAndCommandHandler;
    private TimetableHandler? timetableHandler;
    private ConflictManagementClientHandler? conflictManagementClientHandler;
    private RosMessagingHandler? rosMessagingHandler;
    private RestrictionHandler? restrictionHandler;
    public static InitializationManager? theInitializationManager;
    public RailwayNetworkManager? theRailwayNetworkManager;
    public TrainForecastManager? theTrainForecastManager;
    public TrainAutoRoutingManager? theAutoRoutingManager;


    // Default configuration. Overridden in configuration file
    // The name of the configuration file is appsettings.json
    private readonly Dictionary<string, string> appConfig = new()
    {
        { "Service:ServiceId", "ConflictManagementServer" },
        { "Service:RcsNode", "ATS_1.CTC_1" },
        { "Connection:AMQHost", "127.0.0.1" },
        { "Connection:AMQPort", "5672" },
        { "Connection:AMQUsername", "guest" },
        { "Connection:AMQPassword", "guest" },
        { "Connection:AllowExtensiveMessageLogging", "false" },
        { "Cassandra:CassandraPort", "9042"},
        { "Cassandra:CassandraConsistencyLevel", "1"},
        { "Ros:topics:tmsreq", "jms.topic.TMS.MovementSessionClient" },
        { "Ros:queues:ctcrouterequest", "jms.queue.rcs.e2k.ctc.routing.request.RoutePlanService" },
        { "Ros:queues:ctcrouteinfo", "jms.queue.rcs.e2k.ctc.routing.routeinfo" },
        { "Ros:queues:tmsserverreq", "jms.queue.TMS.MovementSessionServer" },
        { "Ros:schemas:tmsreq", "RCS.E2K.TMS.RoutePlan.V3" },
        { "Ros:schemas:tmscancelreq", "RCS.E2K.TMS.CancelRoutePlan.V2" },
        { "Ros:schemas:tmsrtsreq", "RCS.E2K.TMS.TrainMovementStateChange.V1" },
        { "Ros:schemas:pretestreq", "rcs.e2k.ctc.RosPretestRequest.V1" },
        { "Ros:schemas:pretestres", "rcs.e2k.ctc.RosPretestResponse.V1" },
        { "Ros:schemas:routeinfo", "rcs.e2k.ctc.RouteInfo.V1" },
        { "Ros:schemas:servicerouteplan", "RCS.E2K.TMS.ServiceRoutePlan.V1" },
        { "Ros:schemas:servicerouteplanrequest", "RCS.E2K.TMS.ServiceRoutePlanRequest.V1" }
    };
    private readonly List<string> cassandraContactPoints = new();    // Configuration file keys: Cassandra:CassandraNodeIPAddress1, Cassandra:CassandraNodeIPAddress2, ...

    private readonly int periodicTaskInterval = 1000;
    private volatile bool serviceRunning = true;

    public ServiceImp()
    {
        Log.Information("===================================");
        Log.Information("Conflict Management Service started");
        Log.Information("===================================");
    }

    public bool Init(IConfiguration conf)
    {
        Log.Information("Initializing service");

        bool success = false;

        serviceState = ServiceState.Offline;

        try
        {
            CreateAppConfig(conf);

            // Initialize AMQP messaging for RCS XML messages
            Connection = new ActiveMQ.AMQP.AMQPConnection(appConfig["Service:ServiceId"],
                                                           appConfig["Service:RcsNode"],
                                                           appConfig["Connection:AMQHost"],
                                                           appConfig["Connection:AMQPort"],
                                                           appConfig["Connection:AMQUsername"],
                                                           appConfig["Connection:AMQPassword"],
                                                           appConfig["Connection:AllowExtensiveMessageLogging"] == "true");
            success = Connection.Connect();

            if (success)
            {
                Log.Information("Creating data handler");
                dataHandler = new DataHandler(cassandraContactPoints,
                                                        int.Parse(appConfig["Cassandra:CassandraPort"]),
                                                        uint.Parse(appConfig["Cassandra:CassandraConsistencyLevel"]));

                Log.Information("Creating railgraph handler");
                railgraphHandler = new RailgraphHandler(dataHandler);
               
                //CML
                Log.Information("Initializing Conflict Management Library");
                theInitializationManager = InitializationManager.CreateInstance();
                Log.Information("Initializing Railway Network Manager");
                theRailwayNetworkManager = MyRailwayNetworkManager;
                Log.Information("Initializing Train Forecast Manager");
                theTrainForecastManager = MyTrainForecastManager;
                Log.Information("Initializing Train Auto-Routing Manager");
                theAutoRoutingManager = MyAutoRoutingManager;
                dataHandler.LinkDelegatesToAutoRoutingManager();
                //CML

                Log.Information("Creating message handlers");
                wdsMessageHandler = new WDSMessageHandler(Connection, SetServiceState, GetServiceState);
                trainInformationAndCommandHandler = new TrainInformationAndCommandHandler(Connection, dataHandler);
                timeDistanceGraphDataHandler = new TimeDistanceGraphDataHandler(Connection, dataHandler);
                timetableHandler = new TimetableHandler(Connection, dataHandler);
                conflictManagementClientHandler = new ConflictManagementClientHandler(Connection, dataHandler);
                rosMessagingHandler = new RosMessagingHandler(Connection, dataHandler, appConfig);
                restrictionHandler = new RestrictionHandler(Connection, dataHandler);

                // Give connection and handlers some time to settle
                Thread.Sleep(500);

                Log.Information("Informing service start to WDS");
                wdsMessageHandler.InformProcessStarted();
            }
            else
                Log.Error($"Initialization failed, couldn't connect: {Connection}");
        }
        catch (Exception ex)
        {
            Log.Error($"Initialization failed: {ex.Message}");
            success = false;
        }

        return success;
    }

    public void Run()
    {
        Log.Information("Running service");

#if DEBUG
        //SetServiceState(ServiceState.Online); // TODO:  Production service should always be Release compilation! This is for running without WDS
#endif
        while (serviceRunning)
        {
            Thread.Sleep(periodicTaskInterval);
            PerformPeriodicTask();
        }
    }

    public void Exit()
    {
        Log.Information("Exiting service");

        CloseConnection();

        Log.Information("Service shut down");
        Log.CloseAndFlushAsync();

        Environment.Exit(0);
    }

    private void CloseConnection()
    {
        if (Connection != null)
        {
            Connection.Disconnect();
            Connection = null;
            Log.Information("Connection closed");
        }
    }

    ////////////////////////////////////////////////////////////////////////////////
    static DateTime period = DateTime.UnixEpoch;
    ulong loops = 0;

    public void PerformPeriodicTask()
    {
        if (loops % 10 == 0)
        {
            // 10 second tasks, just for testing that main thread is running
            if (period != DateTime.UnixEpoch)
            {
                // Send state service state every 10 seconds
                bool active = serviceState == ServiceState.Online || serviceState == ServiceState.OnlineDegraded;
                this.conflictManagementClientHandler?.SendActiveServiceInfo(active, false);

                if (appConfig["Connection:AllowExtensiveMessageLogging"] == "true")
                    Log.Debug("Ten 1 second main thread periodic task calls took {0:#.##} seconds (should take 10 seconds)", (DateTime.Now - period).TotalSeconds);
                DataHandler?.ReportModel();
            }
            period = DateTime.Now;
        }
        if (loops == 3)
        {
            //REMOVE Test RailGraph
            RailgraphHandler?.TestRailgraph();
            //REMOVE Test RailGraph
        }
        loops++;
    }

    ////////////////////////////////////////////////////////////////////////////////

    public void Shutdown()
    {
        Log.Information("Shutdown requested");

        GoOffline();
        CloseConnection();
        serviceRunning = false;
    }

    ////////////////////////////////////////////////////////////////////////////////

    public void SetServiceState(ServiceState newState)
    {
        // Shutdown is special service state (not sent by watchdog)
        if (newState == ServiceState.Shutdown)
        {
            this.serviceState = newState;

            Shutdown();
            return;
        }

        // If existing state is requested, do nothing
        if (newState == this.serviceState)
            return;

        Log.Information("Changing service state from {0} to {1}", GetState(this.serviceState), GetState(newState));

        this.serviceState = newState;

        Log.Information("Service state is now {0}", GetState(this.serviceState));

        switch (newState)
        {
            case ServiceState.Offline:
                {
                    GoOffline();
                    break;
                }
            case ServiceState.ReadyForStandby:
                {
                    // Nothing to do here
                    break;
                }
            case ServiceState.Standby:
                {
                    GoStandby();
                    break;
                }
            case ServiceState.ReadyForOnline:
                {
                    // Nothing to do here
                    break;
                }
            case ServiceState.Online:
                {
                    // We are Online in ServiceState.Online and ServiceState.OnlineDegraded
                    if (this.serviceState != ServiceState.OnlineDegraded)
                        GoOnline();
                    break;
                }
            case ServiceState.OnlineDegraded:
                {
                    // We are Online in ServiceState.Online and ServiceState.OnlineDegraded
                    if (this.serviceState != ServiceState.Online)
                        GoOnline();
                    break;
                }
            default:
                break;
        }
    }

    public ServiceState GetServiceState()
    {
        return serviceState;
    }

    private void GoOffline()
    {
        restrictionHandler?.ServiceDeactivated();
        rosMessagingHandler?.ServiceDeactivated();
        conflictManagementClientHandler?.ServiceDeactivated();
        timetableHandler?.ServiceDeactivated();
        timeDistanceGraphDataHandler?.ServiceDeactivated();
        trainInformationAndCommandHandler?.ServiceDeactivated();
        dataHandler?.ServiceStateChangedOffline(this.serviceState == ServiceState.Shutdown);
    }

    private void GoStandby()
    {
        restrictionHandler?.ServiceDeactivated();
        rosMessagingHandler?.ServiceDeactivated();
        conflictManagementClientHandler?.ServiceDeactivated();
        timetableHandler?.ServiceDeactivated();
        timeDistanceGraphDataHandler?.ServiceDeactivated();
        trainInformationAndCommandHandler?.ServiceDeactivated();
        dataHandler?.ServiceStateChangedStandby();
    }

    private void GoOnline()
    {
        DateTime start = DateTime.Now;

        dataHandler?.ServiceStateChangedOnline();
        timeDistanceGraphDataHandler?.ServiceActivated();
        trainInformationAndCommandHandler?.ServiceActivated();
        timetableHandler?.ServiceActivated();
        conflictManagementClientHandler?.ServiceActivated();
        rosMessagingHandler?.ServiceActivated();
        restrictionHandler?.ServiceActivated();

        Log.Debug($"Going to Online took {(DateTime.Now - start).TotalSeconds} seconds");
    }

    ////////////////////////////////////////////////////////////////////////////////
    //TODO: event sending is to be implemented better, this is now just for testing of the interface
    // Event keys now in use:
    //      "ConflictManagementService.TimetableAllocated"
    //      "ConflictManagementService.TimetableUnallocated"
    //      "ConflictManagementService.TimetableAllocationFailed"

    internal void SendEvent(Train train, CtcEvent ctcEvent)
    {
        trainInformationAndCommandHandler?.SendEvent(train, ctcEvent);
    }

    ////////////////////////////////////////////////////////////////////////////////

    private void CreateAppConfig(IConfiguration conf)
    {
        foreach (var item in conf.AsEnumerable())
        {
            if (this.appConfig.ContainsKey(item.Key))
            {
                this.appConfig[item.Key] = item.Value;
                Log.Information($"Config: Key: {item.Key}, Value: {item.Value}");
            }
        }

        // Cassandra contact points
        int nodeNumber = 1;
		bool done = false;
		do
		{
			string name = "Cassandra:CassandraNodeIPAddress" + nodeNumber.ToString();
            var sect = conf.GetSection(name);
            if (sect.Exists() && sect.Value != null)
            {
                this.cassandraContactPoints.Add(sect.Value);
                nodeNumber++;
                Log.Information($"Config: Key: {name}, Value: {sect.Value}");
            }
            else
                done = true;
		}
		while (!done);

		// If no Cassandra contact points have been defined, use localhost as default
		if (this.cassandraContactPoints.Count == 0)
		{
			this.cassandraContactPoints.Add("127.0.0.1");
			Log.Information("Config: Key: Cassandra:CassandraNodeIPAddress1, Value: 127.0.0.1 (set internally, because no contact points is configured)");
		}
	}

}
