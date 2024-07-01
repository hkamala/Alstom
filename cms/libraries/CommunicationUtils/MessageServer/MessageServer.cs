using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using System.Threading;
using Amqp;
using Apache.NMS;
using Apache.NMS.AMQP;
using commUtils;
using Serilog;
using System.Data.Common;

namespace commUtils.MessageServer
{
	public class MessagesServer : IMessageServer
	{
		private struct ClientSetup
		{
			public IMessageProcessor? Consumer;
			public string Address;
			public bool IsQueue;
		};

		private List<ClientSetup> m_clientDefs = new List<ClientSetup>();

		private readonly List<IMessageConsumer> m_consumers = new List<IMessageConsumer>(); 
		private readonly SortedDictionary<string, IMessageProducer> m_producers = new SortedDictionary<string, IMessageProducer>();

		private SortedDictionary<string, IMessageProcessor> m_recvSinks = new SortedDictionary<string, IMessageProcessor>();
		private readonly List<ITextMessage> m_recvPendingMsgs = new List<ITextMessage>();
		private readonly List<KeyValuePair<string, List<MessageBuffer>>> m_sendPendingMsgs = new List<KeyValuePair<string, List<MessageBuffer>>>();
		private const int c_SleepTimeMS = 50;
		private bool m_allInitialized = false;

		private NmsConnection ?m_connection;
		private Apache.NMS.ISession ?m_session;

		private Thread m_recvMsgThread;
		private Thread m_sendMsgThread;
		private Thread ?m_connThread;

		private Apache.NMS.IConnectionFactory m_connectionFactory;
		private string m_connectionString;
		private string m_username;
		private string m_password;
		
		private enum EConnectionState
		{
			EDisconnected,
			EConnecting,
			EConnected,
			EClosing
		};

		private class MessageBuffer
		{
			public string Address;
			public object Message;
			public IDictionary<string, object> Properties;
			bool Async;

			public override string ToString()
			{
				string retVal = "{\n";
				retVal += $"\tAddress: \"{Address}\",\n";
				retVal += $"\tMessage: \"{Message?.ToString() ?? "null"}\",\n";
				retVal += Properties != null ? $"\tProperties: \"{Properties}\"\n" : "";
				retVal += "\n}";
				return retVal;
			}
		}
		public MessagesServer(Apache.NMS.IConnectionFactory factory, string host, int port, string username, string password)
		{
			Log.Information($"Apache: Starting message client: {username}@{host}:{port}");
			m_connectionFactory = factory;
			m_connectionString = $"amqp://{host}:{port}";
			m_username = username;
			m_password = password;

			m_recvMsgThread = new Thread(new ThreadStart(MessageRecvProcessingMainFunc))
			{
				IsBackground = true,
				Name = "Receive message Thread"
			};
			m_recvMsgThread.Start();

			m_sendMsgThread = new Thread(new ThreadStart(MessageSendProcessingMainFunc))
			{
				IsBackground = true,
				Name = "Send message Thread"
			};
			m_sendMsgThread.Start();
		}

		public IMessageServer.ConnectionState GetConnectionState()
		{
			if (m_connection == null)
				return IMessageServer.ConnectionState.Disconnected;

			if (m_connection.IsConnected)
				return IMessageServer.ConnectionState.Connected;

			if (m_connection.IsStarted)
				return IMessageServer.ConnectionState.Started;

			if (m_connection.IsClosed)
				return IMessageServer.ConnectionState.Closed;

			return IMessageServer.ConnectionState.Disconnected;
		}

		public void AddClient(IMessageProcessor? processor, string address, IMessageServer.AddressType addressType)
		{
			Log.Information($"Apache: Adding client on {addressType} {address}");

			if (addressType == IMessageServer.AddressType.Queue && !address.StartsWith("jms.queue."))
				address = "jms.queue." + address;
			else if (addressType == IMessageServer.AddressType.Topic && !address.StartsWith("jms.topic."))
				address = "jms.topic." + address;

			m_clientDefs.Add(new ClientSetup() { Consumer = processor, Address = address, IsQueue = (addressType == IMessageServer.AddressType.Queue) });
		}
		public void Start() => StartConnectionThread();
		
		private void StartConnectionThread()
		{
			m_connThread = new Thread(new ThreadStart(ConnectionThreadMainFunc))
			{
				IsBackground = true,
				Name = "Connection Thread"
			};
			m_connThread.Start();
		}

		private void ConnectionThreadMainFunc()
		{
			Log.Information("Apache: Starting connection thread");
			CloseConnectionToServer();
			while (true)
			{
				try
				{
					if (m_connection == null)
					{
						m_connectionFactory.BrokerUri = new Uri(m_connectionString);
						m_connection = m_connectionFactory.CreateConnection(m_username, m_password) as NmsConnection;
						m_connection.ExceptionListener += OnExceptionListener;
						m_connection.ConnectionInterruptedListener += OnConnectionInterruptedListener;
					}

					Log.Information("Apache: Starting connections...");
					m_connection.Start();
					m_session = m_connection.CreateSession(AcknowledgementMode.AutoAcknowledge);
					RegisterClients();
					m_allInitialized = true;
					Log.Information("Apache: Connection established");
					
					return;
				}
				catch (Exception e)
				{
					Log.Error($"Apache: Connection timeout. Reason: {e.Message}. Reconnecting...");
				}

				Thread.Sleep(2000);
			}
		}

		private void RegisterClients()
		{
			foreach (var setupItem in m_clientDefs)
			{
				IDestination dest = Apache.NMS.Util.SessionUtil.GetDestination(m_session, setupItem.Address, setupItem.IsQueue ? DestinationType.Queue : DestinationType.Topic);
				if (dest == null)
					continue;

				if (setupItem.Consumer != null)
				{
					IMessageConsumer? consumer = m_session?.CreateConsumer(dest);
					if (consumer == null)
						continue;

					consumer.Listener += OnConsumerListener;
					m_consumers.Add(consumer);
					m_recvSinks.Add(setupItem.Address, setupItem.Consumer);
				}
				else
				{
					IMessageProducer? producer = m_session?.CreateProducer(dest);
					if (producer != null)
					{
						m_producers.Add(setupItem.Address, producer);
						if (setupItem.Address.StartsWith("jms.queue."))
							m_producers.Add(setupItem.Address.Substring("jms.queue.".Length), producer);
						else if (setupItem.Address.StartsWith("jms.topic."))
							m_producers.Add(setupItem.Address.Substring("jms.topic.".Length), producer);
					}
				}
			}
		}

		private void OnExceptionListener(Exception exception)
		{
			Log.Error($"Apache: Connection listener exception occured: {exception.Message}");
			StartConnectionThread();
		}

		private void OnConnectionInterruptedListener()
{
			Log.Error("Apache: Connection interrupt listener exception occured");
			StartConnectionThread();
		}

		private void OnConsumerListener(IMessage message)
		{
			lock (this)
			{
				if (message is ITextMessage textMsg)
					m_recvPendingMsgs.Add(textMsg);
			}
		}

		private void CloseConnectionToServer()
		{
			try
			{
				foreach (var consumer in m_consumers)
					consumer.Close();
				foreach (var producer in m_producers)
					producer.Value.Close();

				m_session?.Close();
				m_connection?.Stop();
			}
			catch (Exception e)
			{
				Log.Error($"Apache: Exception on closing connection: {e.Message}\n{e.StackTrace}");
			}

			//needs to be separated
			try
			{
				foreach (var consumer in m_consumers)
					consumer.Dispose();
				foreach (var producer in m_producers)
					producer.Value.Dispose();

				m_consumers.Clear();
				m_producers.Clear();
				m_recvSinks.Clear();

				m_session?.Dispose();
				m_connection?.Dispose();
				m_session = null;
				m_connection = null;
			}
			catch (Exception e)
			{
				Log.Error($"Apache: Exception on disposing connection: {e.Message}\n{e.StackTrace}");
			}
		}

		private void MessageRecvProcessingMainFunc()
		{
			while (true)
			{
				if (m_connection?.IsConnected ?? false)
				{
					while (m_recvPendingMsgs.Count > 0)
					{
						ITextMessage processedMsg;
						lock (this)
						{
							processedMsg = m_recvPendingMsgs[0];
							m_recvPendingMsgs.RemoveAt(0);
						}

						ProcessRecvSingleMessage(processedMsg);
					}
				}

				Thread.Sleep(c_SleepTimeMS);
			}
		}

		private void MessageSendProcessingMainFunc()
		{
			while (true)
			{
				if ((m_connection?.IsConnected ?? false) && m_session != null && m_allInitialized)
				{
					if (m_sendPendingMsgs.Count != 0)
						Log.Debug($"Apache: sending {m_sendPendingMsgs.Count} messages");

					lock(this)
					{
						int addressCnt = 0;
						while (addressCnt < m_sendPendingMsgs.Count)
						{
							var addr2Msg = m_sendPendingMsgs[addressCnt];
							if (!m_producers.ContainsKey(addr2Msg.Key))
							{
								Log.Debug($"Apache: Can't find producer for given address {addr2Msg.Key}");
								addressCnt++;
							}
							else
							{
								foreach (var bufferedmessage in addr2Msg.Value)
								{
									IMessage message = ProduceMessage(bufferedmessage);
									SendToServer(addr2Msg.Key, message);
								}

								m_sendPendingMsgs.RemoveAt(addressCnt);
							}
						}
					}
				}

				Thread.Sleep(c_SleepTimeMS);
			}
		}

		private void SendToServer(string address, IMessage processedMsg)
		{
			IMessageProducer producer = m_producers[address];
			producer.Send(processedMsg);
			string logText = $"Apache: message sent to {address}\n";
			
			if (processedMsg is ITextMessage txtMsg)
				logText += txtMsg.Text;
			Log.Debug(logText);
		}

		private void ProcessRecvSingleMessage(ITextMessage processedMsg)
		{
			string? destination = processedMsg.NMSDestination.ToString().Substring("TopicName: ".Length);
			if (string.IsNullOrEmpty(destination))
			{
				Log.Error($"Apache: Unrecognized destination {processedMsg.NMSDestination}");
				return;
			}

			Tuple<string, string, IDictionary<string, object>> msgToSend = null;

			if (m_recvSinks.ContainsKey(destination))
			{
				IMessageProcessor processor = m_recvSinks[destination];
				msgToSend = processor?.OnMessage(processedMsg);
			}
			else if (destination.StartsWith("jms.topic.") && m_recvSinks.ContainsKey(destination.Substring("jms.topic.".Length)))
			{
				IMessageProcessor processor = m_recvSinks[destination.Substring("jms.topic.".Length)];
				msgToSend = processor?.OnMessage(processedMsg);
			}
			else if (!destination.StartsWith("jms.topic.") && m_recvSinks.ContainsKey("jms.topic." + destination))
			{
				IMessageProcessor processor = m_recvSinks["jms.topic." + destination];
				msgToSend = processor?.OnMessage(processedMsg);
			}
			else
			{
				Log.Debug($"Apache: destination not supported {processedMsg.NMSDestination}");
			}

			if (msgToSend?.Item1 != string.Empty && msgToSend?.Item2 != string.Empty)
				Send(msgToSend.Item1, msgToSend.Item2, msgToSend.Item3);
		}

		private void AddMessageToSendQueue(string address, object message, IDictionary<string, object>? props, bool sendAsync)
		{
			lock (this)
			{
				List<MessageBuffer> messages;
				var foundItem = m_sendPendingMsgs.Where(item => item.Key == address);
				if (!foundItem.Any())
				{
					messages = new List<MessageBuffer>();
					m_sendPendingMsgs.Add(new KeyValuePair<string, List<MessageBuffer>>(address, messages));
				}
				else
					messages = foundItem.First().Value;

				messages.Add(new MessageBuffer() { Address = address, Message = message, Properties = props });
			}
		}
		
		public void Send(string address, IMessage message, bool sendAsync) => AddMessageToSendQueue(address, message, null, sendAsync);
		public void Send(string address, string message, IDictionary<string, object> props, bool sendAsync = true) => AddMessageToSendQueue(address, message, props, sendAsync);
		public void Send(string address, byte[] message, IDictionary<string, object> props, bool sendAsync = true) => AddMessageToSendQueue(address, message, props, sendAsync);
		public void Send(string address, IDictionary<string, object> values, IDictionary<string, object> props, bool sendAsync = true) => AddMessageToSendQueue(address, values, props, sendAsync);

		private void AddPropertiesToMessage(IMessage message, IDictionary<string, object> props)
		{
			if (props == null)
				return;

			foreach (var prop in props)
			{
				if (prop.Value is string)
					message.Properties.SetString(prop.Key, prop.Value.ToString());
				else if (prop.Value is bool)
					message.Properties.SetBool(prop.Key, (bool)prop.Value);
				else if (prop.Value is byte)
					message.Properties.SetByte(prop.Key, (byte)prop.Value);
				else if (prop.Value is char)
					message.Properties.SetChar(prop.Key, (char)prop.Value);
				else if (prop.Value is short)
					message.Properties.SetShort(prop.Key, (short)prop.Value);
				else if (prop.Value is int)
					message.Properties.SetInt(prop.Key, (int)prop.Value);
				else if (prop.Value is long)
					message.Properties.SetLong(prop.Key, (long)prop.Value);
				else if (prop.Value is float)
					message.Properties.SetFloat(prop.Key, (float)prop.Value);
				else if (prop.Value is double)
					message.Properties.SetDouble(prop.Key, (double)prop.Value);
				else if (prop.Value is System.Collections.IList)
					message.Properties.SetList(prop.Key, (System.Collections.IList)prop.Value);
				else if (prop.Value is byte[])
					message.Properties.SetBytes(prop.Key, (byte[])prop.Value);
				else if (prop.Value is System.Collections.IDictionary)
					message.Properties.SetDictionary(prop.Key, (System.Collections.IDictionary)prop.Value);
			}
		}

		private IMessage ProduceMessage(MessageBuffer buffer)
		{
			IMessage retVal = null;
			if (buffer.Message is string strMsg)
				retVal = m_session.CreateTextMessage(strMsg);
			else if (buffer.Message is byte[] byteMsg)
				retVal = m_session.CreateBytesMessage(byteMsg);
			else if (buffer.Message is IDictionary<string, object> dictMsg)
			{
				IMapMessage mapMsg = m_session.CreateMapMessage();
				foreach (var pair in dictMsg)
				{
					if (pair.Key is string)
						mapMsg.Body[pair.Key] = pair.Value;
				}

				retVal = mapMsg;
			}
			else if (buffer.Message is IMessage imsg)
				retVal = imsg;

			if (retVal == null)
			{
				Log.Error($"Apache: Can't create message {buffer.ToString()}");
				return null;
			}

			AddPropertiesToMessage(retVal, buffer.Properties);
			return retVal;
		}
	}
}
