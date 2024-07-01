namespace E2KService.MessageHandler;

using System;
using System.Xml.Linq;
using System.Xml;
using System.Text;
using E2KService.ActiveMQ;
using ConflictManagementService.Model;
using Serilog;
using Apache.NMS;
using System.Collections.Generic;

internal class RestrictionHandler : ActiveStateMessageHandler
{
    readonly ActiveMQ.AMQP.Rcs2kXmlMessageProcessor messageProcessor = new("Restrictions Message Processor");

    // External information requests
    // Channels
    static readonly Channel restrictionInfoRequestChannel = new(ChannelType.Queue, "jms.queue.rcs.e2k.ext.restriction.request");

    // Schemas
    const string possessionsRequestSchema = "rcs.e2k.ext.possession.request.V1";

    // Messages
    const string possessionsRequestMsgType = "possessionRequest";

    // External information responses
    // Channels
    static readonly Channel restrictionChannel = new(ChannelType.Topic, "jms.topic.rcs.e2k.ext.restriction");

    // Subscriptions
    static readonly Subscription PossessionsSubscription = new(restrictionChannel, "possessions");
    static readonly Subscription DeletedPossessionsSubscription = new(restrictionChannel, "deletedPossessions");

    const string possessionSchema = "rcs.e2k.ext.possession.V1";
    const string possessionDeletedSchema = "rcs.e2k.ext.possession.deleted.V1";

    ////////////////////////////////////////////////////////////////////////////////

    public RestrictionHandler(Connection connection, DataHandler dataHandler) : base(connection, dataHandler)
    {
        HandleSubscriptions(MessagingStateSubscription.Always);
    }

    private void HandleSubscriptions(MessagingStateSubscription state)
    {
        switch (state)
        {
            case MessagingStateSubscription.Always:
                break;

            case MessagingStateSubscription.MessagingActive:
                string? selector = this.Connection.RcsNodeSelector;
                Connection.Subscribe(PossessionsSubscription, this.messageProcessor, OnPossessions, selector);
                Connection.Subscribe(DeletedPossessionsSubscription, this.messageProcessor, OnPossessionsDeleted, selector);
                break;

            case MessagingStateSubscription.MessagingInactive:
                Connection.Unsubscribe(PossessionsSubscription);
                Connection.Unsubscribe(DeletedPossessionsSubscription);
                break;
        }
    }

    ////////////////////////////////////////////////////////////////////////////////

    protected override void MessagingActivated()
    {
        HandleSubscriptions(MessagingStateSubscription.MessagingActive);
        Log.Information("RestrictionHandler: Message processing activated: accepting messages");

        SendPossessionRequest();
    }

    protected override void MessagingDeactivated()
    {
        HandleSubscriptions(MessagingStateSubscription.MessagingInactive);
        Log.Information("RestrictionHandler: Message processing deactivated: rejecting messages");
    }

    ////////////////////////////////////////////////////////////////////////////////

#pragma warning disable CS8602 // Dereference of a possibly null reference. try-catch in caller will handle missing mandatory fields in run-time

    private ElementPosition GetElementPosition(XElement parentNode, string nodeName)
    {
        ElementPosition position = new();

        var positionNode = parentNode.Element(nodeName);
        if (positionNode != null)
        {
            string edge = positionNode.Attribute("edge").Value;
            uint offset = XmlConvert.ToUInt32(positionNode.Attribute("offset").Value);
            //string vertex = positionNode.Attribute("vertex").Value;
            long additionalPos = XmlConvert.ToInt64(positionNode.Attribute("addpos").Value);
            string additionalName = GetOptionalAttributeValueOrEmpty(positionNode, "addname");

            position = CreateElementPosition(edge, offset, additionalPos, additionalName);
        }

        return position;
    }

    private ActionTime GetActionTime(XElement parentNode, string nodeName)
    {
        ActionTime time = new();
        var timeNode = parentNode.Element(nodeName);
        if (timeNode != null)
        {
            ActionTime nodeTime = new();
            if (nodeTime.InitFromATSDateTimeString(timeNode.Value))
                time = nodeTime;
        }

        return time;
    }

    ////////////////////////////////////////////////////////////////////////////////

    public static ElementPosition CreateElementPosition(string edge, uint offset, long additionalPos = 0, string additionalName = "")
    {
        return new ElementPosition(edge, offset, additionalPos, additionalName);
    }
    public static ElementExtension CreateElementExtension(ElementPosition startPos, ElementPosition endPos, List<string> edges)
    {
        return new ElementExtension(startPos, endPos, edges);
    }

    ////////////////////////////////////////////////////////////////////////////////

    private void OnPossessions(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        if (!AllowMessageProcessing)
            return;

        try
        {
            var schema = hdr["schema"];

            if (schema == possessionSchema)
            {
                string refresh = "";
                var attr = msg.Attribute("refresh");
                if (attr != null)   // Normally missing
                    refresh = attr.Value;

                bool refreshing = refresh != "";
                bool refreshEnds = refresh == "end";

                bool refreshRequestPending = DataHandler.IsPossessionsRequestPending();
                bool acceptMessage = !refreshRequestPending || refreshing;

                if (acceptMessage)
                {
                    foreach (var child in msg.Elements())
                    {
                        if (child.Name == "possession")
                        {
                            Possession possession = new();

                            string id = GetOptionalAttributeValueOrEmpty(child, "id");
                            string description = GetOptionalAttributeValueOrEmpty(child, "description");

                            bool idOK = id != "";

                            if (idOK)
                            {
                                ElementPosition startPos = GetElementPosition(child, "startpos");
                                ElementPosition endPos = GetElementPosition(child, "endpos");

                                ActionTime startTime = GetActionTime(child, "starttime");
                                ActionTime endTime = GetActionTime(child, "endtime");

                                string state = GetOptionalElementValueOrEmpty(child, "state"); // This really is not optional!
                                if (state == "")
                                {
                                    string activeState = GetOptionalElementValueOrEmpty(child, "active"); // This really is not optional!
                                    if (activeState != "")
                                    {
                                        var active = XmlConvert.ToBoolean(activeState.ToLower());
                                        if (active)
                                            state = "active";
                                        else
                                            state = "defined";
                                    }
                                }
                                if (state != "")
                                {
                                    possession = DataHandler.PossessionChanged(id, description, startPos, endPos, startTime, endTime, state);
                                }
                                else
                                {
                                    Log.Warning("Possession state node missing: id=" + id + " description=" + description);
                                }
                            }

                            if (!possession.IsValid())
                            {
                                Log.Warning("Possession not handled: id=" + id + " description=" + description);
                            }
                        }
                    }

                    if (refreshRequestPending && refreshEnds)
                    {
                        DataHandler.SetPossessionsRequested(false);
                        Log.Information("Possessions refresh data was received and handled");
                    }
                }
                else
                {
                    Log.Warning("Discarded possession message - waiting for refresh");
                }
            }
            else
            {
                Log.Warning("Unknown message schema: {0}", schema);
            }
        }
        catch (Exception ex)
        {
            Log.Error("Parsing of XML message failed: {0}", ex.ToString());
        }
    }

    private void OnPossessionsDeleted(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        if (!AllowMessageProcessing)
            return;

        try
        {
            var schema = hdr["schema"];

            if (schema == possessionDeletedSchema)
            {
                //string senderDC = msg.Attribute("senderDC").Value;

                bool acceptMessage = !DataHandler.IsPossessionsRequestPending();

                if (acceptMessage)
                {
                    foreach (var child in msg.Elements())
                    {
                        if (child.Name == "possession")
                        {
                            Possession possession = new();

                            string id = GetOptionalAttributeValueOrEmpty(child, "id");

                            if (id != "")
                            {
                                possession = DataHandler.PossessionDeleted(id);
                            }

                            if (!possession.IsValid())
                            {
                                Log.Warning("Possession deleted not handled, possession: {0}", id);
                            }
                        }
                    }
                }
                else
                {
                    Log.Warning("Discarded possession deletion message - waiting for refresh");
                }
            }
            else
            {
                Log.Warning("Unknown message schema: {0}", schema);
            }
        }
        catch (Exception ex)
        {
            Log.Error("Parsing of XML message failed: {0}", ex.ToString());
        }
    }


#pragma warning restore CS8602 // Dereference of a possibly null reference.

    ////////////////////////////////////////////////////////////////////////////////

    private void SendPossessionRequest()
    {
        Dictionary<string, string> hdr = new();
        Dictionary<string, string> msgProperties = new();

        var messageId = Connection.CreateNewMessageId();

        hdr["destination"] = restrictionInfoRequestChannel.ChannelName;
        hdr["sender"] = Connection.ServiceId;
        hdr["schema"] = possessionsRequestSchema;

        msgProperties["rcsschema"] = possessionsRequestSchema;
        msgProperties["rcsMessageId"] = messageId;
        msgProperties["rcsNode"] = this.Connection.RcsNode;

        try
        {
            XElement msgNode = new(possessionsRequestMsgType);

            DataHandler.SetPossessionsRequested(true);

            Connection.SendMessage(restrictionInfoRequestChannel, this.messageProcessor.CreateMessage(hdr, msgNode, msgProperties));
        }
        catch (Exception e)
        {
            Log.Error("Internal error in XML message creation: {0}", e.ToString());
        }
    }

}    
