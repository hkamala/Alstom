using System.Threading;
//using System.Configuration;
using Microsoft.Extensions.Configuration;
using static E2KService.ServiceStateHelper;
using SkeletonService.Model;

namespace E2KService;

using ActiveMQ;
using MessageHandler;
using Serilog;

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

    private DataHandler? dataHandler;
    private RailgraphHandler? railgraphHandler;
    private WDSMessageHandler? wdsMessageHandler;
    private TrainInformationAndCommandHandler? trainInformationAndCommandHandler;

    // Default configuration. Overridden in configuration file
    // The name of the configuration file must be App.config
    private readonly Dictionary<string, string> appConfig = new()
    {
        { "Service:ServiceId", "SkeletonService" },
        { "Service:RcsNode", "ATS_1.CTC_1" },
        { "Connection:AMQHost", "127.0.0.1" },
        { "Connection:AMQPort", "5672" },
        { "Connection:AMQUsername", "guest" },
        { "Connection:AMQPassword", "guest" },
        { "Connection:AllowExtensiveMessageLogging", "false" }
    };

    private readonly int periodicTaskInterval = 1000;
    private volatile bool serviceRunning = true;

    public ServiceImp()
    {
        Log.Information("========================");
        Log.Information("Skeleton Service started");
        Log.Information("========================");
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
                dataHandler = new DataHandler();

                Log.Information("Creating railgraph handler");
                railgraphHandler = new RailgraphHandler(dataHandler);

                Log.Information("Creating message handlers");
                wdsMessageHandler = new WDSMessageHandler(Connection, SetServiceState, GetServiceState);
                trainInformationAndCommandHandler = new TrainInformationAndCommandHandler(Connection, dataHandler);

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
                //...

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
        trainInformationAndCommandHandler?.ServiceDeactivated();
        dataHandler?.ServiceStateChangedOffline(this.serviceState == ServiceState.Shutdown);
    }

    private void GoStandby()
    {
        trainInformationAndCommandHandler?.ServiceDeactivated();
        dataHandler?.ServiceStateChangedStandby();
    }

    private void GoOnline()
    {
        DateTime start = DateTime.Now;

        dataHandler?.ServiceStateChangedOnline();
        trainInformationAndCommandHandler?.ServiceActivated();

        Log.Debug($"Going to Online took {(DateTime.Now - start).TotalSeconds} seconds");
    }

    ////////////////////////////////////////////////////////////////////////////////
    //TODO: event sending is to be implemented better, this is now just for testing of the interface (not working yet in CtcMom!)

    internal void SendEvent(Train train, string eventKey, string text)
    {
        trainInformationAndCommandHandler?.SendEvent(eventKey, train.CtcId, train.Td, text);
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
	}

}
