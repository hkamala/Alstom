namespace E2KService.ActiveMQ;

using Apache.NMS;
using System.Xml.Linq;
using E2KService.ActiveMQ.AMQP;
using System.Collections.Generic;

public enum ChannelType { Topic, Queue };

abstract class Connection
{
    private static uint requestNum = 0;

    public string ServiceId { get => serviceId; set => serviceId = value; }
    public string RcsNode { get => rcsNode; set => rcsNode = value; }
    public string RcsNodePostfix => RcsNode != "" ? "." + RcsNode : "";
    public string? RcsNodeSelector => RcsNode != "" ? "rcsNode = '" + RcsNode + "'" : null;

    public string CreateNewMessageId()
    {
        return ServiceId + (RcsNode != "" ? "." + RcsNode : "") + "-" + GetNewRequestNumber();
    }
    public string GetNewRequestNumber()
    {
        try
        {
            if (++requestNum == 0)
                ++requestNum;
        }
        catch
        {
            requestNum = 1;
        }
        return (requestNum).ToString();
    }
    public bool AllowExtensiveMessageLogging => allowExtensiveMessageLogging;
    protected bool allowExtensiveMessageLogging = false;

    protected enum ConnectionState
    {
        Disconnecting,
        Disconnected,
        Connecting,
        Connected
    };

    protected ConnectionState connectionState = ConnectionState.Disconnected;
    private string serviceId = "serviceId";
    private string rcsNode = "";

    public delegate void MessageReceived(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg);

    // Methods for clients to use
    virtual public bool IsConnected() { return connectionState == ConnectionState.Connected; }
    abstract public bool Connect();
    abstract public void Disconnect();
    abstract public void Subscribe(Subscription subscription, MessageProcessor messageProcessor, MessageReceived handler, string? selector = null);
    abstract public void Unsubscribe(Subscription subscription);
    abstract public bool SendMessage(Channel channel, IMessage? message);

    // Methods for message processors to use
    abstract public void DispatchMessageToSubscribers(Subscription subscription, Dictionary<string, string> hdr, XElement? msg, Dictionary<string, string> msgProperties, IMessage rawMsg);
}
