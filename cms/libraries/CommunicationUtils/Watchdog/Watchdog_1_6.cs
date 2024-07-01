using Apache.NMS;
using commUtils.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Resources;
using commUtils.DataSchemas;
using commUtils.MessageServer;
using Serilog;

namespace commUtils.Watchdog
{
	public class Watchdog1_6 : IMessageProcessor
	{
		public enum EProcessState
		{
			test,
			Offline,
			ReadyForStandby,
			Standby,
			ReadyForOnline,
			Online,
			OnlineDegraded
		};

		public enum ELaunchMode
		{
			Automatic,
			Manual
		};

		private string m_appName;
		private string m_replyTo;
		private string m_rcsNode;
		private ELaunchMode m_mode;
		private IMessageServer m_server;
		private EProcessState m_state;
		private int m_counter = 0;
		private IDictionary<string, object> m_props = new Dictionary<string, object>();

		public delegate void StateChanged(EProcessState state);
		public event StateChanged? OnStateChanged = null;

		public delegate void ReportRequest();
		public event ReportRequest? OnReportRequest = null;

		public delegate void StopRequest();
		public event StopRequest? OnStopRequest = null;

		public Watchdog1_6(string replyTo, string appName, string rcsNode, IMessageServer sendServer, ELaunchMode mode = ELaunchMode.Automatic)
		{
			Log.Information($"WD: Starting watchdog on {appName} with mode {mode}");
			m_appName = appName;
			m_replyTo = replyTo;
			m_rcsNode = rcsNode;
			m_server = sendServer;
			m_mode = mode;
			m_props.Add("rcsNode", rcsNode);
			if (mode == ELaunchMode.Manual)
				return;

			m_state = EProcessState.ReadyForOnline;
			SendApplicationStarted();
		}

		public Tuple<string, string, IDictionary<string, object>> OnMessage(IMessage msg)
		{
			if (msg is ITextMessage txtMsg)
			{
				if (txtMsg.Properties.GetString("rcsNode") != m_rcsNode)
					return new Tuple<string, string, IDictionary<string, object>>("", "", null);

				Log.Debug($"WD: Received message\n{txtMsg.Text}\n");
				XSD.Watchdog.Message? wdMessage = (XSD.Watchdog.Message)XmlSerialization.DeserializeObjectFromString<XSD.Watchdog.Message>(txtMsg.Text, out string errorText);
				if (wdMessage == null || !string.IsNullOrEmpty(errorText))
				{
					if (!string.IsNullOrEmpty(errorText))
						Log.Error($"WD: Can't parse message\n{txtMsg.Text}\nError message from parser: {errorText}\n");
					else if (wdMessage == null)
						Log.Error($"WD: Can't cast to WD Message!\n{txtMsg.Text}");
					return new Tuple<string, string, IDictionary<string, object>>("", "", null);
				}

				if (wdMessage.data.Item is XSD.Watchdog.processReportRequest procReportReq)
					return ProcessReportRequest(wdMessage.hdr.correlationId, procReportReq.scopeId, procReportReq.tsn);
				else if (wdMessage.data.Item is XSD.Watchdog.processStateChangeRequest procStateChangeReq)
					return ProcessStateChangeRequest(procStateChangeReq.state, wdMessage.hdr.correlationId, procStateChangeReq.scopeId, procStateChangeReq.tsn);
				else if (wdMessage.data.Item is XSD.Watchdog.processStopRequest procStopReq)
					return ProcessStopRequest(procStopReq);
			}

			return new Tuple<string, string, IDictionary<string, object>>("", "", null);
		}

		private Tuple<string, string, IDictionary<string, object>> ProcessReportRequest(string correlationId, string scopeId, string tsn)
		{
			Log.Debug("WD: Processing ProcessReportRequest");
			string reportMsg = Properties.Resources.WDProcessStateReport;
			if (reportMsg == null)
				Log.Error("WD: Can't get 'report responce' xml file template from binary!");

			if (scopeId == "all" || scopeId == m_appName)
			{
				string sendMsg = string.Format(reportMsg, m_appName, m_appName.ToLower(), (++m_counter).ToString(), correlationId, tsn, m_state.ToString());
				Log.Debug($"WD: Responce to ReportRequest sent from WD:\n{sendMsg}\n");
				OnReportRequest?.Invoke();
				if (m_mode == ELaunchMode.Automatic)
					return new Tuple<string, string, IDictionary<string, object>>(m_replyTo, sendMsg, m_props);
			}
			else
				Log.Debug($"WD: Omitting, reason: message sent not to ${m_appName}");

			return new Tuple<string, string, IDictionary<string, object>>("", "", null);
		}

		private Tuple<string, string, IDictionary<string, object>> ProcessStateChangeRequest(string state, string correlationId, string scopeId, string tsn)
		{
			Log.Information("WD: Processing ProcessStateChangeRequest");
			try
			{
				m_state = (EProcessState)Enum.Parse(typeof(EProcessState), state, true);
				OnStateChanged?.Invoke(m_state);
				if (m_mode == ELaunchMode.Automatic) 
					return ProcessReportRequest(correlationId, scopeId, tsn);

				Log.Information($"WD: State changed to {m_mode}");
			}
			catch (Exception)
			{
				Log.Error("WD: Not supported application state requested!");
			}

			return new Tuple<string, string, IDictionary<string, object>>("", "", null);
		}

		private Tuple<string, string, IDictionary<string, object>> ProcessStopRequest(XSD.Watchdog.processStopRequest procStopReq)
		{
			Log.Information("WD: Processing ProcessStopRequest");
			if (procStopReq.scopeId == "all" || procStopReq.scopeId == m_appName)
			{
				if (OnStopRequest != null)
				{
					Log.Information("WD: Invoking OnStopRequest callbacks");
					OnStopRequest.Invoke();
				}
				else
				{
					Log.Information("WD: Calling Exit(0)");
					Environment.Exit(0);
				}
			}

			return new Tuple<string, string, IDictionary<string, object>>("", "", null);
		}

		public async void SendApplicationStarted()
		{
			Log.Information("WD: Sending application started message");
			string startedMsg = Properties.Resources.WDMessageStarted;
			if (startedMsg == null)
			{
				Log.Error("WD: Can't get 'report started' xml file template from binary!");
				return;
			}

			string sendMsg = string.Format(startedMsg, m_appName, m_counter.ToString());
			m_server?.Send(m_replyTo, sendMsg, m_props);
			m_state = EProcessState.Online;
		}
	}
}
