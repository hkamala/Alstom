namespace E2KService.ActiveMQ.AMQP;

using System.Collections.Generic;
using System.Threading;
using System.Xml.Linq;
using Apache.NMS;

abstract class MessageProcessor
{
    protected Connection? Connection { get => connection; }

    private readonly List<IMessage> messages = new();
    private readonly Thread workerThread;
    private const int c_SleepTimeMS = 5;
    private Connection? connection = null;

    // NMS message static property names in services
    public const string PropertyCorrelationId = "E2KService-correlationid";
    public const string PropertyReplyTo = "E2KService-replyto";
    public const string PropertyReplyToType = "E2KService-replytotype";  // Values are "topic" or "queue"

    protected MessageProcessor(string name)
    {
        workerThread = new Thread(new ThreadStart(ProcessMessagesThread))
        {
            Name = name,
            IsBackground = true
        };
        workerThread.Start();
    }

    abstract protected void ProcessMessage(IMessage msg);
    public abstract IMessage? CreateMessage(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, List<string>? namespaces = null);
    public abstract IMessage? CreateMessage(string body, IDictionary<string, object> properties);

    public void SetConnection(Connection connection)
    {
        this.connection = connection;
    }

    public void AddMessage(IMessage msg)
    {
        if (Connection != null && Connection.IsConnected())
        {
            lock (messages)
            {
                messages.Add(msg);
            }
        }
    }

    private void ProcessMessagesThread()
    {
        while (true)
        {
            while (messages.Count > 0)
            {
                IMessage msg;
                lock (messages)
                {
                    msg = messages[0];
                    messages.RemoveAt(0);
                }

                ProcessMessage(msg);
            }

            Thread.Sleep(c_SleepTimeMS);
        }
    }
}
