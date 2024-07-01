namespace E2KService.ActiveMQ.AMQP;

using System.Text;
using System.Xml.Linq;
using Apache.NMS;
using Serilog;

abstract class RcsXmlMessageProcessor : MessageProcessor
{
	public RcsXmlMessageProcessor(string name) : base(name)
	{
	}

	abstract protected XElement? ParseRootNode(string xml);
	abstract protected void ParseHeaders(XElement rootNode, Dictionary<string, string> hdr);
	abstract protected XElement? ParseDataMsgElement(XElement rootNode);
	abstract protected string ParseMsgType(XElement dataMsgElement, Dictionary<string, string> msgProperties);
	abstract protected XElement CreateRootNode();
    abstract protected void CreateNamespaces(XElement rootNode, List<string>? namespaces = null);

	override protected void ProcessMessage(IMessage msg)
	{
		if (msg != null && Connection != null)
		{
			XElement? dataMsgElement;	// Do not inline this, for some odd reason, it does not work!

			if (DeserializeMsg(msg, out Dictionary<string, string> hdr, out ChannelType type, out string channel, out string msgType, out dataMsgElement, out Dictionary<string, string> msgProperties))
            {
                Connection.DispatchMessageToSubscribers(new Subscription(new Channel(type, channel), msgType), hdr, dataMsgElement, msgProperties);
            }
        }
	}

	private bool DeserializeMsg(IMessage msg, out Dictionary<string, string> hdr, out ChannelType type, out string channel, out string msgType, out XElement? dataMsgElement, out Dictionary<string, string> msgProperties)
	{
		bool success = true;

		hdr = new Dictionary<string, string>();
		msgProperties = new Dictionary<string, string>();
		type = ChannelType.Topic;
		channel = "";
		msgType = "";
		dataMsgElement = null;

		try
		{
			string xml = "";

			if (msg is Apache.NMS.ITextMessage)
				xml = ((Apache.NMS.ITextMessage)msg).Text;
			else if (msg is Apache.NMS.IBytesMessage)
				xml = Encoding.ASCII.GetString(((Apache.NMS.IBytesMessage)msg).Content);
			else
				throw new Exception("Unknown NMS message type");
				
			// Log message, if allowed
			if (Connection != null && Connection.AllowExtensiveMessageLogging)
				Log.Debug("Received message: {0}", xml);

			// Message properties
			foreach (var prop in msg.Properties.Keys)
			{
				var name = prop.ToString();
				if (name != null)
					msgProperties[name] = msg.Properties.GetString(name);
			}
			msgProperties[PropertyCorrelationId] = msg.NMSCorrelationID;
			if (msg.NMSReplyTo != null)
			{
				var replyto = msg.NMSReplyTo;
				if (replyto != null)
				{
					if (msg.NMSReplyTo.IsTopic)
					{
						msgProperties[PropertyReplyTo] = ((Apache.NMS.AMQP.NmsTopic)replyto).TopicName;
						msgProperties[PropertyReplyToType] = "topic";
					}
					else
					{
						msgProperties[PropertyReplyTo] = ((Apache.NMS.AMQP.NmsQueue)replyto).QueueName;
						msgProperties[PropertyReplyToType] = "queue";
					}
				}
			}

			// Parse XML message according to inherited class
			var rootNode = ParseRootNode(xml);
			if (rootNode == null)
				throw new Exception("Unknown message class");

			ParseHeaders(rootNode, hdr);

			dataMsgElement = ParseDataMsgElement(rootNode);
			if (dataMsgElement == null)
				throw new Exception("Unrecognized application message body");

			msgType = ParseMsgType(dataMsgElement, msgProperties);

			// Find out channel of message (may be any combination depending on the sender...)
			if (msg.NMSDestination.IsTopic)
			{
				type = ChannelType.Topic;
				channel = ((Apache.NMS.AMQP.NmsTopic)msg.NMSDestination).TopicName;
				if (channel == null || channel.Length == 0)
					channel = ((Apache.NMS.AMQP.NmsQueue)msg.NMSDestination).QueueName;
				if (channel != null && channel.StartsWith("jms.queue"))
					type = ChannelType.Queue;
			}
			else if (msg.NMSDestination.IsQueue)
			{
				type = ChannelType.Queue;
				channel = ((Apache.NMS.AMQP.NmsQueue)msg.NMSDestination).QueueName;
				if (channel == null || channel.Length == 0)
					channel = ((Apache.NMS.AMQP.NmsTopic)msg.NMSDestination).TopicName;
				if (channel != null && channel.StartsWith("jms.topic"))
					type = ChannelType.Topic;
			}
			else
				throw new Exception("NMS Destination is not topic or queue, could not be resolved");
		}
		catch (Exception ex)
		{
			// Not valid AMQP NMS message!
			Log.Warning("Received unknown or invalid message: {0}: {1}", ex.Message, msg.ToString());
			success = false;
		}

		return success;
	}

	override public IMessage? CreateMessage(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, List<string>? namespaces = null)
	{
		try
		{
			AMQPConnection? connection = null;
			ISession? session = null;

			if (Connection != null)
			{
				connection = (AMQPConnection)Connection;
				if (connection.Session != null)
					session = connection.Session;
			}
			if (connection == null || session == null)
				return null;

			// Create message root node
			XElement rootNode = CreateRootNode();

            // Add needed namespaces from caller
            CreateNamespaces(rootNode, namespaces);

			// Add <hdr> node with values from caller
			XElement hdrElement = new("hdr");
			foreach (var key in hdr.Keys)
				hdrElement.Add(new XElement(key, hdr[key]));

            // Add current UTC times always to <hdr> for needed or debugging reasons
            var now = DateTime.UtcNow.ToString("yyyyMMddTHHmmss");
            hdrElement.Add(new XElement("utc", now));
            hdrElement.Add(new XElement("utctime", now));

			rootNode.Add(hdrElement);

			// Add <data> node, the real message
			rootNode.Add(new XElement("data", msg));

			string xml = rootNode.ToString();

			// Prepare NMS message
			IMessage message = session.CreateTextMessage(xml);

			// Add message properties
			foreach (var key in msgProperties.Keys)
			{
				var value = msgProperties[key];

				if (key == PropertyCorrelationId)
				{
					message.NMSCorrelationID = value;
				}
				else if (key == PropertyReplyTo)
				{
					// There shall also be destination's type as property
					ChannelType type = ChannelType.Queue;
					var rtType = msgProperties[PropertyReplyToType];
					if (rtType != null)
						type = rtType == "topic" ? ChannelType.Topic : ChannelType.Queue;
					else
						type = ChannelType.Queue;
					message.NMSReplyTo = connection.GetChannelDestination(new Channel(type, value));
				}
				else if (key != PropertyReplyToType)
					message.Properties.SetString(key, value);
			}

			return message;
		}
		catch (Exception ex)
		{
			Log.Error("Error in message creation: {0}", ex.ToString());
			return null;
		}
	}
}

