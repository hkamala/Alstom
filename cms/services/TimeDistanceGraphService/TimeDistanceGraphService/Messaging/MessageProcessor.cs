namespace E2KService.ActiveMQ;

using System.Xml.Linq;
using Apache.NMS;

abstract class MessageProcessor
{
	protected Connection? Connection { get => this.connection; }

	private readonly List<IMessage> messages = new();
	private readonly Thread workerThread;
	private const int c_SleepTimeMS = 5;
	private Connection? connection = null;

	// NMS message static property names in services
	protected const string PropertyCorrelationId = "E2KService-correlationid";
	protected const string PropertyReplyTo = "E2KService-replyto";
	protected const string PropertyReplyToType = "E2KService-replytotype";	// Values are "topic" or "queue"

	protected MessageProcessor(string name)
	{
		this.workerThread = new Thread(new ThreadStart(ProcessMessagesThread))
		{
			Name = name,
			IsBackground = true
		};
		this.workerThread.Start();
	}

	abstract protected void ProcessMessage(IMessage msg);
	abstract public IMessage? CreateMessage(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, List<string>? namespaces = null);

	public void SetConnection(Connection connection)
	{
		this.connection = connection;
	}

	public void AddMessage(IMessage msg)
    {
        if (Connection != null && Connection.IsConnected())
        {
            lock(this.messages)
            {
                this.messages.Add(msg);
            }
        }
	}

	private void ProcessMessagesThread()
	{
		while (true)
		{
			while (this.messages.Count > 0)
			{
				IMessage msg;
				lock(this.messages)
				{
					msg = this.messages[0];
					this.messages.RemoveAt(0);
				}

				ProcessMessage(msg);
			}

			Thread.Sleep(c_SleepTimeMS);
		}
	}
}
