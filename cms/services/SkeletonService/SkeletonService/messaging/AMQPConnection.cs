namespace E2KService.ActiveMQ.AMQP;

using Apache.NMS;
using System.Collections.Concurrent;
using System.Xml.Linq;
using Serilog;

internal class AMQPConnection : Connection
{
    private readonly string host;
    private readonly string port;
    private readonly string username;
    private readonly string password;

    private readonly ConcurrentDictionary<Channel, IMessageConsumer> consumers = new();
    private readonly ConcurrentDictionary<Channel, IMessageProducer> producers = new();
    private readonly ConcurrentDictionary<Subscription, MessageReceived> subscriptions = new();
    private readonly ConcurrentDictionary<Channel, MessageProcessor> messageProcessors = new();     // Only one message processor allowed per channel, don't want to parse message in this class. Several message types are handled with the same processor
    private readonly ConcurrentDictionary<Channel, string?> selectors = new();

    private IConnection? connection;
    private ISession? session;
    System.Timers.Timer? reconnectTimer;

    public ISession? Session => session;

    public AMQPConnection(string serviceId, string rcsNode, string host, string port, string username, string password, bool allowExtensiveMessageLogging)
    {
        ServiceId = serviceId;
        RcsNode = rcsNode;
        this.host = host;
        this.port = port;
        this.username = username;
        this.password = password;
        this.allowExtensiveMessageLogging = allowExtensiveMessageLogging;
    }

    public override bool Connect()
    {
        CloseConnection();

        connectionState = ConnectionState.Connecting;

        string connectURI = string.Format($"amqp://{host}:{port}");
        Log.Information($"Connecting: {connectURI}...");

        var CF = new Apache.NMS.AMQP.ConnectionFactory();
        CF.BrokerUri = new Uri(connectURI);

        connectionState = CreateConnection(CF);

        return connectionState == ConnectionState.Connected;
    }

    public override void Disconnect()
    {
        connectionState = ConnectionState.Disconnecting;
        if (reconnectTimer != null)
            reconnectTimer.Enabled = false;
        CloseConnection();
    }

    private ConnectionState CreateConnection(IConnectionFactory factory)
    {
        try
        {
            connection = factory.CreateConnection(username, password) as Apache.NMS.AMQP.NmsConnection;
            if (connection == null)
                throw new Exception("CreateConnection returned null");

            session = connection.CreateSession(AcknowledgementMode.AutoAcknowledge);

            connection.ExceptionListener += OnConnectionException;
            connection.ConnectionInterruptedListener += OnConnectionInterrupted;
            connection.Start();
        }
        catch (Exception ex)
        {
            Log.Error($"Connection error: {ex.Message}...");
            return ConnectionState.Disconnected;
        }

        return ConnectionState.Connected;
    }

    private void CloseConnection()
    {
        if (connectionState != ConnectionState.Disconnected)
        {
            Log.Information("Closing connection...");

            foreach (var producer in producers)
            {
                producer.Value.Close();
                producer.Value.Dispose();
            }
            producers.Clear();

            foreach (var consumer in consumers)
            {
                consumer.Value.Close();
                consumer.Value.Dispose();
            }
            consumers.Clear();

            if (session != null)
            {
                try
                {
                    session.Close();
                    session.Dispose();
                }
                catch (Exception)
                {
                }
                session = null;
            }

            if (connection != null)
            {
                try
                {
                    connection.Stop();
                    connection.Close();
                    connection.Dispose();
                }
                catch
                {
                }
                connection = null;
            }

            // Clear subscription data only when explicitly disconnecting
            if (connectionState == ConnectionState.Disconnecting)
            {
                subscriptions.Clear();
                messageProcessors.Clear();
                selectors.Clear();
            }

            connectionState = ConnectionState.Disconnected;
        }
    }

    public override void Subscribe(Subscription subscription, MessageProcessor messageProcessor, MessageReceived handler, string? selector = null)
    {
        if (IsConnected())
        {
            if (session == null)
                return;

            lock (this)
            {
                if (!consumers.ContainsKey(subscription.Channel))
                {
                    IMessageConsumer consumer;
                    if (selector != null)
                        consumer = session.CreateConsumer(GetChannelDestination(subscription.Channel), selector);
                    else
                        consumer = session.CreateConsumer(GetChannelDestination(subscription.Channel));

                    consumer.Listener += messageProcessor.AddMessage;
                    consumers.TryAdd(subscription.Channel, consumer);

                    if (!messageProcessors.ContainsKey(subscription.Channel))
                    {
                        messageProcessor.SetConnection(this);
                        messageProcessors.TryAdd(subscription.Channel, messageProcessor);
                        selectors.TryAdd(subscription.Channel, selector);
                    }
                }

                if (subscriptions.ContainsKey(subscription))
                    subscriptions[subscription] += handler;
                else
                    subscriptions.TryAdd(subscription, handler);
            }
        }
        else
        {
            Log.Error($"Not connected when subscribing, subscription discarded: {subscription}");
        }
    }

    public override void Unsubscribe(Subscription subscription)
    {
        if (IsConnected())
        {
            if (session == null || subscription == null)
                return;

            lock (this)
            {
                var channel = subscription.Channel;

                if (consumers.TryRemove(channel, out IMessageConsumer? consumer))
                {
                    consumer.Close();
                    consumer.Dispose();
                }

                messageProcessors.TryRemove(channel, out _);
                selectors.TryRemove(channel, out _);
                subscriptions.TryRemove(subscription, out _);
            }
        }
        else
        {
            Log.Error($"Not connected when unsubscribing, unsubscription discarded: {subscription}");
        }
    }

    private void Resubscribe()
    {
        if (session == null)
            return;

        lock (this)
        {
            foreach (var channel in messageProcessors.Keys)
            {
                string? selector = selectors.ContainsKey(channel) ? selectors[channel] : null;

                IMessageConsumer consumer;
                if (selector != null)
                    consumer = session.CreateConsumer(GetChannelDestination(channel), selector);
                else
                    consumer = session.CreateConsumer(GetChannelDestination(channel));
                consumer.Listener += messageProcessors[channel].AddMessage;
                consumers[channel] = consumer;
            }
        }
    }

    private void Reconnect()
    {
        reconnectTimer = new() { AutoReset = true, Interval = 2000 };
        reconnectTimer.Elapsed += (o, i) =>
        {
            if (Connect())
            {
                reconnectTimer.Enabled = false;
                Resubscribe();
            }
        };
        reconnectTimer.Enabled = true;
    }

    private void OnConnectionException(Exception exception)
    {
        if (connectionState == ConnectionState.Connecting)
            return;

        connectionState = ConnectionState.Connecting;
        Log.Warning("Listener exception, reconnecting...");
        Reconnect();
    }

    private void OnConnectionInterrupted()
    {
        if (connectionState == ConnectionState.Connecting)
            return;

        connectionState = ConnectionState.Connecting;
        Log.Warning("Connection interrupted, reconnecting...");
        Reconnect();
    }

    public IDestination GetChannelDestination(Channel channel)
    {
        var destinationType = channel.ChannelType == ChannelType.Topic ? DestinationType.Topic : DestinationType.Queue;
        return Apache.NMS.Util.SessionUtil.GetDestination(session, channel.ChannelName, destinationType);
    }

    public override void DispatchMessageToSubscribers(Subscription subscription, Dictionary<string, string> hdr, XElement? msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        if (msg != null)
        {
            Subscription subscribed = new(new Channel(subscription.Channel.ChannelType, subscription.Channel.ChannelName), subscription.MessageType);

            bool isSubscribed = subscriptions.ContainsKey(subscribed);
            if (!isSubscribed)
            {
                // jms.topic. and jms.queue. may have been added to or removed from message property destination values (depending on message conversion?)
                // Try again by removing them or adding them back
                if (subscription.Channel.ChannelType == ChannelType.Topic)
                {
                    if (subscription.Channel.ChannelName.StartsWith("jms.topic."))
                        subscribed = new Subscription(new Channel(ChannelType.Topic, subscription.Channel.ChannelName[10..]), subscription.MessageType);
                    else
                        subscribed = new Subscription(new Channel(ChannelType.Topic, "jms.topic." + subscription.Channel.ChannelName), subscription.MessageType);
                }
                else
                {
                    if (subscription.Channel.ChannelName.StartsWith("jms.queue."))
                        subscribed = new Subscription(new Channel(ChannelType.Queue, subscription.Channel.ChannelName[10..]), subscription.MessageType);
                    else
                        subscribed = new Subscription(new Channel(ChannelType.Queue, "jms.queue." + subscription.Channel.ChannelName), subscription.MessageType);
                }

                isSubscribed = subscriptions.ContainsKey(subscribed);
            }

            if (isSubscribed)
            {
                lock (this)
                {
                    try
                    {
                        subscriptions[subscribed](hdr, msg, msgProperties, rawMsg);
                    }
                    catch
                    {
                    }
                }
            }
            else //if (this.AllowExtensiveMessageLogging)
                Log.Error($"{subscription} is not subscribed. Nothing to worry about, SkeletonService does not need information in this message");
        }
    }

    public override bool SendMessage(Channel channel, IMessage? message)
    {
        bool success = false;

        if (IsConnected())
        {
            if (session == null)
                return false;

            if (message != null)
            {
                lock (this)
                {
                    IMessageProducer producer;
                    if (producers.ContainsKey(channel))
                        producer = producers[channel];
                    else
                    {
                        producer = session.CreateProducer(GetChannelDestination(channel));
                        producers[channel] = producer;
                    }

                    producer.Send(message);
                    success = true;

                    // Log message, if allowed
                    if (AllowExtensiveMessageLogging)
                        Log.Debug("Sent message: {0}", message);
                }
            }
            else
            {
                Log.Error($"Message sent to channel {channel} is null");
            }
        }
        else
        {
            Log.Error($"Not connected when message to channel {channel} is sent: {message}");
        }

        return success;
    }
}
