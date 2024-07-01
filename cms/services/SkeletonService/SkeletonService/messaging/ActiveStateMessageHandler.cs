namespace E2KService.MessageHandler;

using System.Xml.Linq;
using E2KService.ActiveMQ;
using SkeletonService.Model;

abstract class ActiveStateMessageHandler
{
    readonly Connection connection;
    readonly DataHandler datahandler;

    private bool isActiveService = false;

    protected Connection Connection { get => connection; }
    protected DataHandler DataHandler { get => datahandler; }
    protected bool AllowMessageProcessing { get => isActiveService; }

    protected enum MessagingStateSubscription { Always, MessagingActive, MessagingInactive };

    protected ActiveStateMessageHandler(Connection connection, DataHandler dataHandler)
    {
        this.connection = connection;
        datahandler = dataHandler;
    }

    abstract protected void MessagingActivated();
    abstract protected void MessagingDeactivated();

    public void ServiceActivated()
    {
        isActiveService = true;
        MessagingActivated();
    }

    public void ServiceDeactivated()
    {
        MessagingDeactivated();
        isActiveService = false;
    }

    protected static string GetOptionalElementValueOrEmpty(XElement? parentNode, string nodeName)
    {
        if (parentNode != null)
        {
            var node = parentNode.Element(nodeName);
            if (node != null)
                return node.Value;
        }
        return "";
    }

    protected static string GetOptionalAttributeValueOrEmpty(XElement? node, string attrName)
    {
        if (node != null)
        {
            var attrNode = node.Attribute(attrName);
            if (attrNode != null)
                return attrNode.Value;
        }
        return "";
    }
}
