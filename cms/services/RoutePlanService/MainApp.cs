using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

using commUtils;
using commUtils.Watchdog;
using commUtils.Utils;
using commUtils.MessageServer;
using RoutePlanLib;
using Serilog;
using Serilog.Sinks.SystemConsole;
using Serilog.Sinks.File;

namespace RoutePlanService
{
	internal class MainApp : IHostedService
	{
		private ConfigurationManager? m_configMgr;

		private IConfiguration CloudConfiguration;
		private ApacheWatchdog? m_apacheConn;

		public MainApp(IConfiguration cloudConf)
		{
			CloudConfiguration = cloudConf;
		}

		public bool Start()
		{
			Log.Information("Log tests:");
			Log.Debug("Log Debug");
			Log.Information("Log Information");
			Log.Warning("Log Warning");
			Log.Error("Log Error");
			Log.Fatal("Log Fatal");

			Log.Information("Starting application\n");

			if (CloudConfiguration != null)
				m_configMgr = new ConfigurationManager(CloudConfiguration);
			else
				Log.Fatal("Cannot start Configuration!");

			ApacheWatchdog.Options opts = new ApacheWatchdog.Options
			{
				NMSHost = m_configMgr.NMSHost(),
				NMSPort = m_configMgr.NMSPort(),
				NMSUser = m_configMgr.NMSUser(),
				NMSPassword = m_configMgr.NMSPassword(),
				NMSRcsMode = m_configMgr.NMSRcsMode(),

				WDAppName = m_configMgr.WDAppName(),
				WDMode = m_configMgr.WDMode(),
				WDSendTo = m_configMgr.WDSendTo(),
				WDReceiveFrom = m_configMgr.WDReceiveFrom()
			};
			m_apacheConn = new ApacheWatchdog(opts);
			m_apacheConn.OnStateChanged += Wd_OnStateChanged;
			m_apacheConn.OnStopRequest += Wd_OnStopRequest;
			m_apacheConn.OnReportRequest += Wd_OnReportRequest;

			RosMessageProcessor rosProcessor = new RosMessageProcessor("RCS.E2K.TMS." + m_configMgr.WDAppName(), 
				opts.NMSRcsMode,
				m_configMgr.TmsReqSchema(),
				m_configMgr.TmsCancelReqSchema(),
				m_configMgr.CtcResQueue());
			m_apacheConn.AddClient(rosProcessor, m_configMgr.TmsReqTopic(), IMessageServer.AddressType.Topic);
			m_apacheConn.AddClient(rosProcessor, m_configMgr.TSInfoTopic(), IMessageServer.AddressType.Topic);
			m_apacheConn.AddClient(null, m_configMgr.CtcResQueue(), IMessageServer.AddressType.Queue);

			m_apacheConn.Start();

			return true;
		}

		private void Wd_OnStopRequest()
		{
			Environment.Exit(0);
		}

		private void Wd_OnReportRequest()
		{
			return;
		}

		private void Wd_OnStateChanged(Watchdog1_6.EProcessState state)
		{
			
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
	}
}
