namespace E2KService;

using System.Threading;
using E2KService.MessageHandler;
using static E2KService.ServiceStateHelper;
using Microsoft.Extensions.Configuration;
using Serilog;

////////////////////////////////////////////////////////////////////////////////
// 
// NLog levels:
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

	public ServiceStateHelper.ServiceState ServiceState { get => serviceState; }
	private ServiceStateHelper.ServiceState serviceState = E2KService.ServiceStateHelper.ServiceState.Offline;

	public ActiveMQ.Connection? Connection { get; set; }
	private Model.DataHandler? dataHandler;
	private WDSMessageHandler? wdsMessageHandler;
	private TimeDistanceGraphDataHandler? timeDistanceGraphDataHandler;
	private TimeDistanceGraphClientHandler? timeDistanceGraphClientHandler;
	public AutoResetEvent? tickEvent = null;

	// Default configuration. Overridden in configuration file
	// The name of the configuration file must be App.config
	private readonly Dictionary<string, string> appConfig = new()
	{
		{ "Service:ServiceId", "TimeDistanceGraphService" },
        { "Service:RcsNode", "ATS_1.CTC_1" },
		{ "Connection:AMQHost", "127.0.0.1" },
		{ "Connection:AMQPort", "5672" },
		{ "Connection:AMQUsername", "guest" },
		{ "Connection:AMQPassword", "guest" },
		{ "Connection:AllowExtensiveMessageLogging", "false" },
		{ "Connection:TrainMovementProvider", "" },
		{ "Connection:EstimationPlansProvider", "" },
		{ "Connection:ScheduledPlansProvider", "" },
		{ "Connection:PossessionsProvider", "" },
		{ "Connection:PossessionActiveStates", "Active,Locked"},
		{ "Cassandra:CassandraPort", "9042"},
		{ "Cassandra:CassandraConsistencyLevel", "1"},
		{ "Cassandra:TrainMovementHistoryHours", "24" },
		{ "Cassandra:PossessionHistoryHours", "168" }    // 7 days
	};
    private readonly List<string> cassandraContactPoints = new();    // Configuration file keys: CassandraNodeIPAddress1, CassandraNodeIPAddress2, ...

	private readonly int periodicTaskInterval = 1000;
	private volatile bool serviceRunning = true;

	public ServiceImp()
    {
		Log.Information("===================================");
		Log.Information("Time-Distance Graph Service started");
		Log.Information("===================================");
	}

	public bool Init(IConfiguration conf)
	{
		Log.Information("Initializing service");

		bool success = false;

		this.serviceState = ServiceState.Offline;

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
				this.dataHandler = new Model.DataHandler(uint.Parse(appConfig["Cassandra:TrainMovementHistoryHours"]),
														uint.Parse(appConfig["Cassandra:PossessionHistoryHours"]),
														cassandraContactPoints,
														int.Parse(appConfig["Cassandra:CassandraPort"]),
														uint.Parse(appConfig["Cassandra:CassandraConsistencyLevel"]),
														appConfig["Connection:PossessionActiveStates"]);

				Log.Information("Creating message handlers");
				this.wdsMessageHandler = new WDSMessageHandler(Connection, SetServiceState, GetServiceState);
				this.timeDistanceGraphClientHandler = new TimeDistanceGraphClientHandler(Connection, this.dataHandler);
				this.timeDistanceGraphDataHandler = new TimeDistanceGraphDataHandler(Connection, this.dataHandler,
																					appConfig["Connection:TrainMovementProvider"],
																					appConfig["Connection:EstimationPlansProvider"],
																					appConfig["Connection:ScheduledPlansProvider"],
																					appConfig["Connection:PossessionsProvider"]);
				
				// Give connection and handlers some time to settle
				Thread.Sleep(500);

				Log.Information("Informing service start to WDS");
				this.wdsMessageHandler.InformProcessStarted();
			}
			else
				Log.Error("Initialization failed, couldn't connect: {0}", Connection.ToString());
		}
		catch (Exception ex)
		{
			Log.Error("Initialization failed: {0}", ex.Message);
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

		try
		{
			while (this.serviceRunning)
			{
				Thread.Sleep(periodicTaskInterval);
				PerformPeriodicTask();
			}

			GoOffline();
		}
		catch {}
	}

	public void Exit()
	{
		Log.Information("Exiting service");

		if (Connection != null)
		{
			Connection.Disconnect();
			Log.Information("Connection closed");
		}

		Log.Information("Service shut down");
		Log.CloseAndFlushAsync();

		Environment.Exit(0);
	}

	////////////////////////////////////////////////////////////////////////////////
	static DateTime period = DateTime.UnixEpoch;
	ulong loops = 0;

	public void PerformPeriodicTask()
	{
		if (loops % 10 == 0)
		{
			if (period != DateTime.UnixEpoch)
			{
				// Send active service state every 10 seconds
				bool active = serviceState == ServiceState.Online || serviceState == ServiceState.OnlineDegraded;
				this.timeDistanceGraphClientHandler?.SendActiveServiceInfo(active, false);

				if (appConfig["Connection:AllowExtensiveMessageLogging"] == "true")
					Log.Debug("Ten 1 second main thread periodic task calls took {0:#.##} seconds (should take 10 seconds)", (DateTime.Now - period).TotalSeconds);
			}
			period = DateTime.Now;
		}
		loops++;
	}

	////////////////////////////////////////////////////////////////////////////////

	public void Shutdown()
	{
		Log.Information("Shutdown requested");

		this.serviceRunning = false;
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

		this.serviceState = newState;

		Log.Information("Service state is now {0}", GetState(this.serviceState));
	}

	public ServiceState GetServiceState()
    {
		return this.serviceState;
    }

	private void GoOffline()
	{
		this.timeDistanceGraphDataHandler?.ServiceDeactivated();
		this.timeDistanceGraphClientHandler?.ServiceDeactivated();
		this.dataHandler?.ServiceStateChangedOffline(this.serviceState == ServiceState.Shutdown);
	}

	private void GoStandby()
    {
		this.timeDistanceGraphDataHandler?.ServiceDeactivated();
		this.timeDistanceGraphClientHandler?.ServiceDeactivated();
		this.dataHandler?.ServiceStateChangedStandby();
	}

	private void GoOnline()
	{
		this.dataHandler?.ServiceStateChangedOnline();
		this.timeDistanceGraphDataHandler?.ServiceActivated();
		this.timeDistanceGraphClientHandler?.ServiceActivated();
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
