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

    public Apache.NMS.ISession? Session => this.session;

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

        this.connectionState = ConnectionState.Connecting;

        string connectURI = string.Format($"amqp://{this.host}:{this.port}");
        Log.Information($"Connecting: {connectURI}...");

        var CF = new Apache.NMS.AMQP.ConnectionFactory();
        CF.BrokerUri = new Uri(connectURI);

        this.connectionState = CreateConnection(CF);
        
        return this.connectionState == ConnectionState.Connected;
    }

    public override void Disconnect()
    {
        this.connectionState = ConnectionState.Disconnecting;
        if (reconnectTimer != null)
            reconnectTimer.Enabled = false;
        CloseConnection();
    }

    private ConnectionState CreateConnection(IConnectionFactory factory)
    {
        try
        {
            this.connection = factory.CreateConnection(this.username, this.password) as Apache.NMS.AMQP.NmsConnection;
            if (this.connection == null)
                throw new Exception("CreateConnection returned null");

            this.session = this.connection.CreateSession(AcknowledgementMode.AutoAcknowledge);

            this.connection.ExceptionListener += OnConnectionException;
            this.connection.ConnectionInterruptedListener += OnConnectionInterrupted;
            this.connection.Start();
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
        if (this.connectionState != ConnectionState.Disconnected)
        {
            Log.Information("Closing connection...");

            foreach (var producer in this.producers)
            {
                producer.Value.Close();
                producer.Value.Dispose();
            }
            this.producers.Clear();

            foreach (var consumer in this.consumers)
            {
                consumer.Value.Close();
                consumer.Value.Dispose();
            }
            this.consumers.Clear();

            if (this.session != null)
            {
                try
                {
                    this.session.Close();
                    this.session.Dispose();
                }
                catch (Exception)
                {
                }
                this.session = null;
            }

            if (this.connection != null)
            {
                try
                {
                    this.connection.Stop();
                    this.connection.Close();
                    this.connection.Dispose();
                }
                catch
                {
                }
                this.connection = null;
            }

            // Clear subscription data only when explicitly disconnecting
            if (this.connectionState == ConnectionState.Disconnecting)
            {
                this.subscriptions.Clear();
                this.messageProcessors.Clear();
                this.selectors.Clear();
            }

            this.connectionState = ConnectionState.Disconnected;
        }
    }

    public override void Subscribe(Subscription subscription, MessageProcessor messageProcessor, MessageReceived handler, string? selector = null)
    {
        if (IsConnected())
        {
            if (this.session == null)
                return;

            lock (this)
            {
                if (!this.consumers.ContainsKey(subscription.Channel))
                {
                    IMessageConsumer consumer;
                    if (selector != null)
                        consumer = this.session.CreateConsumer(GetChannelDestination(subscription.Channel), selector);
                    else
                        consumer = this.session.CreateConsumer(GetChannelDestination(subscription.Channel));

                    consumer.Listener += messageProcessor.AddMessage;
                    this.consumers.TryAdd(subscription.Channel, consumer);

                    if (!this.messageProcessors.ContainsKey(subscription.Channel))
                    {
                        messageProcessor.SetConnection(this);
                        this.messageProcessors.TryAdd(subscription.Channel, messageProcessor);
                        this.selectors.TryAdd(subscription.Channel, selector);
                    }
                }

                if (this.subscriptions.ContainsKey(subscription))
                    this.subscriptions[subscription] += handler;
                else
                    this.subscriptions.TryAdd(subscription, handler);
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
            if (this.session == null || subscription == null)
                return;

            lock (this)
            {
                var channel = subscription.Channel;
                
                if (this.consumers.TryRemove(channel, out IMessageConsumer? consumer))
                {
                    consumer.Close();
                    consumer.Dispose();
                }
                
                this.messageProcessors.TryRemove(channel, out _);
                this.selectors.TryRemove(channel, out _);
                this.subscriptions.TryRemove(subscription, out _);
            }
        }
        else
        {
            Log.Error($"Not connected when unsubscribing, unsubscription discarded: {subscription}");
        }
    }

    private void Resubscribe()
    {
        if (this.session == null)
            return;

        lock (this)
        {
            foreach (var channel in this.messageProcessors.Keys)
            {
                string? selector = this.selectors.ContainsKey(channel) ? this.selectors[channel] : null;

                IMessageConsumer consumer;
                if (selector != null)
                    consumer = this.session.CreateConsumer(GetChannelDestination(channel), selector);
                else
                    consumer = this.session.CreateConsumer(GetChannelDestination(channel));
                consumer.Listener += this.messageProcessors[channel].AddMessage;
                this.consumers[channel] = consumer;
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
        if (this.connectionState == ConnectionState.Connecting)
            return;

        this.connectionState = ConnectionState.Connecting;
        Log.Warning("Listener exception, reconnecting...");
        Reconnect();
    }

    private void OnConnectionInterrupted()
    {
        if (this.connectionState == ConnectionState.Connecting)
            return;

        this.connectionState = ConnectionState.Connecting; 
        Log.Warning("Connection interrupted, reconnecting...");
        Reconnect();
    }

    public IDestination GetChannelDestination(Channel channel)
    {
        var destinationType = channel.ChannelType == ChannelType.Topic ? Apache.NMS.DestinationType.Topic : Apache.NMS.DestinationType.Queue;
        return Apache.NMS.Util.SessionUtil.GetDestination(this.session, channel.ChannelName, destinationType);
    }

    public override void DispatchMessageToSubscribers(Subscription subscription, Dictionary<string, string> hdr, XElement? msg, Dictionary<string, string> msgProperties)
    {
        if (msg != null)
        {
            Subscription subscribed = new(new Channel(subscription.Channel.ChannelType, subscription.Channel.ChannelName), subscription.MessageType);

            bool isSubscribed = this.subscriptions.ContainsKey(subscribed);
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

                isSubscribed = this.subscriptions.ContainsKey(subscribed);
            }

            if (isSubscribed)
            {
                lock (this)
                {
                    this.subscriptions[subscribed](hdr, msg, msgProperties);
                }
            }
            else
                Log.Error("{0} is not subscribed", subscription);
        }
    }

    public override bool SendMessage(Channel channel, IMessage? message)
    {
        bool success = false;

        if (IsConnected())
        {
            if (this.session == null)
                return false;

            if (message != null)
            {
                IMessageProducer producer;
                if (this.producers.ContainsKey(channel))
                    producer = this.producers[channel];
                else
                {
                    producer = this.session.CreateProducer(GetChannelDestination(channel));
                    this.producers[channel] = producer;
                }

                producer.Send(message);
                success = true;

                // Log message, if allowed
                if (this.AllowExtensiveMessageLogging)
                    Log.Debug("Sent message: {0}", message);
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
