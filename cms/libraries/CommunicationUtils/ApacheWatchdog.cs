using commUtils.MessageServer;
using System;
using System.Collections.Generic;
using System.Text;
using Apache.NMS;
using commUtils.Watchdog;

namespace commUtils
{
	public class ApacheWatchdog : IMessageServer
	{
		public struct Options
		{
			public string NMSHost;
			public int NMSPort;
			public string NMSUser;
			public string NMSPassword;
			public string NMSRcsMode;

			public string WDAppName;
			public Watchdog1_6.ELaunchMode WDMode;
			public string WDSendTo;
			public string WDReceiveFrom;
		}

		private MessagesServer? m_apacheConn;
		private Watchdog1_6 m_watchdog;

		public event Watchdog1_6.StateChanged OnStateChanged
		{
			add { m_watchdog.OnStateChanged += value; }
			remove { m_watchdog.OnStateChanged -= value; }
		}

		public event Watchdog1_6.StopRequest OnStopRequest
		{
			add { m_watchdog.OnStopRequest += value; }
			remove { m_watchdog.OnStopRequest -= value; }
		}

		public event Watchdog1_6.ReportRequest OnReportRequest
		{
			add { m_watchdog.OnReportRequest += value; }
			remove { m_watchdog.OnReportRequest -= value; }
		}

		public ApacheWatchdog(Options opts)
		{
			m_apacheConn = new MessagesServer(new Apache.NMS.AMQP.ConnectionFactory(), opts.NMSHost, opts.NMSPort, opts.NMSUser, opts.NMSPassword);
			m_watchdog = new Watchdog1_6(opts.WDSendTo, opts.WDAppName, opts.NMSRcsMode, m_apacheConn, opts.WDMode);
			m_apacheConn.AddClient(m_watchdog, opts.WDReceiveFrom, IMessageServer.AddressType.Topic);
			m_apacheConn.AddClient(null, opts.WDSendTo, IMessageServer.AddressType.Topic);
		}

		public ApacheWatchdog(string host, int port, string user, string password, string rcsNode, string appName, string receiveFrom, string sendTo, Watchdog1_6.ELaunchMode mode)
		{
			m_apacheConn = new MessagesServer(new Apache.NMS.AMQP.ConnectionFactory(), host, port, user, password);
			m_watchdog = new Watchdog1_6(sendTo, appName, rcsNode, m_apacheConn, mode);
			m_apacheConn.AddClient(m_watchdog, receiveFrom, IMessageServer.AddressType.Topic);
			m_apacheConn.AddClient(null, sendTo, IMessageServer.AddressType.Queue);
		}

		public void AddClient(IMessageProcessor? processor, string address, IMessageServer.AddressType addressType) => m_apacheConn?.AddClient(processor, address, addressType);

		public void Start() => m_apacheConn?.Start();

		public void Send(string address, IMessage message, bool sendAsync = true) => m_apacheConn?.Send(address, message, sendAsync);

		public IMessageServer.ConnectionState GetConnectionState() => m_apacheConn?.GetConnectionState() ?? IMessageServer.ConnectionState.Disconnected;

		public void Send(string address, string message, IDictionary<string, object> props, bool sendAsync = true) => m_apacheConn?.Send(address, message, props, sendAsync);

		public void Send(string address, byte[] message, IDictionary<string, object> props, bool sendAsync = true) => m_apacheConn?.Send(address, message, props, sendAsync);

		public void Send(string address, IDictionary<string, object> values, IDictionary<string, object> props, bool sendAsync = true) => m_apacheConn?.Send(address, values, props, sendAsync);
	}
}
