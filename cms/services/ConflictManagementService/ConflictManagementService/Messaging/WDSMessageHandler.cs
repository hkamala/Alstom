﻿using E2KService;

namespace E2KService.MessageHandler;

using System;
using System.Xml.Linq;
using E2KService.ActiveMQ;
using E2KService.ActiveMQ.AMQP;
using static E2KService.ServiceStateHelper;
using Serilog;
using Apache.NMS;
using System.Collections.Generic;

internal partial class WDSMessageHandler
{
    public delegate void SetServiceState(ServiceState newState);
    public delegate ServiceState GetServiceState();

    readonly Connection connection;

    private SetServiceState serviceStateChange;
    private GetServiceState getServiceState;

    // Channels
    static readonly Channel WdsReportChannel = new(ChannelType.Topic, "jms.topic.rcs.e2k.ctc.wds.report");
    static readonly Channel WdsRequestChannel = new(ChannelType.Topic, "jms.topic.rcs.e2k.ctc.wds.request");

    // Subscriptions and messages
    readonly ActiveMQ.AMQP.Rcs5kXmlMessageProcessor messageProcessor = new("WDS Message Processor");

    const string rcswdsNameSpace = "rcswds";

    static readonly Subscription ProcessStateChangeRequestSubscription = new(WdsRequestChannel, "rcswds:processStateChangeRequest");
    static readonly Subscription ProcessReportRequestSubscription = new(WdsRequestChannel, "rcswds:processReportRequest");
    static readonly Subscription ProcessStopRequestSubscription = new(WdsRequestChannel, "rcswds:processStopRequest");

    const string processStarted = "processStarted";
    const string processStartedWithNs = rcswdsNameSpace + ":" + processStarted;
    const string processStateReport = "processStateReport";
    const string processStateReportWithNs = rcswdsNameSpace + ":" + processStateReport;

    ////////////////////////////////////////////////////////////////////////////////

    public WDSMessageHandler(Connection connection, SetServiceState serviceStateChange, GetServiceState getServiceState)
    {
        this.connection = connection;
        this.serviceStateChange = serviceStateChange;
        this.getServiceState = getServiceState;

        AddSubscriptions();
    }

    private void AddSubscriptions()
    {
        string? selector = connection.RcsNodeSelector;

        connection.Subscribe(ProcessStateChangeRequestSubscription, messageProcessor, OnProcessStateChangeRequest, selector);
        connection.Subscribe(ProcessReportRequestSubscription, messageProcessor, OnProcessReportRequest, selector);
        connection.Subscribe(ProcessStopRequestSubscription, messageProcessor, OnProcessStopRequest, selector);
    }

    ////////////////////////////////////////////////////////////////////////////////

    public bool InformProcessStarted()
    {
        /*
            <rcs:Message>
                <hdr>
                    <source>LCSS_RATOIF_ID</source>                     <!-- sender, processId, RATOIF in this case -->
                    <messageId>rcs.e2k.ctc.ratoif-1234</messageId>      <!-- not mandatory --> 
                    <content>rcswds:processStarted</content>            <!-- not mandatory --> 
                </hdr>
                <data>
                    <rcswds:processStarted sourceId="LCSS_RATOIF_ID">
                    </rcswds:processStarted>
                </data>
            </rcs:Message>
        */

        Dictionary<string, string> hdr = new();
        Dictionary<string, string> msgProperties = new();

        var messageId = connection.CreateNewMessageId();

        hdr["source"] = connection.ServiceId;
        hdr["messageId"] = messageId;
        hdr["content"] = processStartedWithNs;

        msgProperties["rcsContent"] = processStartedWithNs;
        msgProperties["rcsMessageId"] = messageId;
        msgProperties["rcsNode"] = connection.RcsNode;

        try
        {
            // Namespaces used
            XNamespace rcswds = XMLNamespaces.GetXNamespace(rcswdsNameSpace);
            List<string> namespaces = new() { rcswdsNameSpace };

            XElement msgNode = new(rcswds + processStarted,
                new XAttribute("sourceId", connection.ServiceId)
            );

            var message = messageProcessor.CreateMessage(hdr, msgNode, msgProperties, namespaces);

            return connection.SendMessage(WdsReportChannel, message);
        }
        catch (Exception e)
        {
            Log.Error("Internal error in XML message creation: {0}", e.ToString());
        }

        return false;
    }

    private bool SendProcessStateMessage(string correlationId, string tsn, string state)
    {
        /*
            <rcs:Message>   
                <hdr>
                    <source>LCSS_RATOIF_ID</source>                             <!-- sender, processId, RATOIF in this case -->
                    <messageId>rcs.e2k.ctc.ratoif-5678</messageId>              <!-- id generated by sender --> 
                    <correlationId>rcs.e2k.ctc.wds-4321</correleationId>        <!-- returned id generated by wds --> 
                    <content>rcswds:processStateReport</content>                <!--  --> 
                </hdr>
            <data>
                <rcswds:processStateReport sourceId="LCSS_RATOIF_ID">   
                        <tsn>33</tsn>                                   <!-- transmission sequence number from rcswds::processStateRequest  -->
                        <state>Online</state>                           <!-- current process state {test, Offline, ReadyForStandby, Standby, ReadyForOnline, Online, OnlineDegraded } -->
                </rcswds:processStateReport>
            </data>
            </rcs:Message>
        */

        Dictionary<string, string> hdr = new();
        Dictionary<string, string> msgProperties = new();

        var messageId = connection.CreateNewMessageId();

        hdr["source"] = connection.ServiceId;
        hdr["messageId"] = messageId;
        hdr["correlationId"] = correlationId;
        hdr["content"] = processStateReportWithNs;

        msgProperties["rcsContent"] = processStateReportWithNs;
        msgProperties["rcsMessageId"] = messageId;
        msgProperties["rcsNode"] = connection.RcsNode;

        try
        {
            // Namespaces used
            XNamespace rcswds = XMLNamespaces.GetXNamespace(rcswdsNameSpace);
            List<string> namespaces = new() { rcswdsNameSpace };

            XElement msgNode = new(rcswds + processStateReport,
                new XAttribute("sourceId", connection.ServiceId),
                new XElement("tsn", tsn),
                new XElement("state", state)
            );

            var message = messageProcessor.CreateMessage(hdr, msgNode, msgProperties, namespaces);

            return connection.SendMessage(WdsReportChannel, message);
        }
        catch (Exception e)
        {
            Log.Error("Internal error in XML message creation: {0}", e.ToString());
        }

        return false;
    }

    ////////////////////////////////////////////////////////////////////////////////
#pragma warning disable CS8602 // Dereference of a possibly null reference. try-catch blocks will take care of these and ignore messages

    private bool IsMessageToUs(string scope, Dictionary<string, string> msgProperties)
    {
        return (scope == "all" || scope == connection.ServiceId) && (!msgProperties.ContainsKey("rcsNode") || msgProperties["rcsNode"] == connection.RcsNode);
    }

    private void OnProcessStateChangeRequest(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        /*
            <rcs:Message>   
                <hdr>
                    <source>LCSS_WDS_ID</source>                            <!-- sender, processId -->
                    <messageId>rcs.e2k.ctc.wds-8765</messageId>             <!-- id generated by sender --> 
                    <content>rcswds:processStateChangeRequest</content>     <!--  --> 
                </hdr>
            <data>
                <rcswds:processStateChangeRequest scopeId="{all, LCSS_RATOIF_ID}">
                        <tsn>33</tsn>                                       <!-- transmission sequence number to be returned in processStateReport -message -->
                        <state>Online</state>                               <!-- current server state to be followed {test, Offline, ReadyForStandby, Standby, ReadyForOnline, Online, OnlineDegraded } -->
                </rcswds:processStateChangeRequest>
            </data>
            </rcs:Message>
        */

        try
        {
            string scope = msg.Attribute("scopeId").Value;

            if (IsMessageToUs(scope, msgProperties))
            {
                Log.Information("Process state change request message received from WDS");

                string correlationId = hdr["messageId"];
                string tsn = msg.Element("tsn").Value;
                string state = msg.Element("state").Value;

                // Set new service state
                serviceStateChange(GetState(state));

                // Inform new state to WDS
                SendProcessStateMessage(correlationId, tsn, GetState(getServiceState()));
            }
        }
        catch (Exception e)
        {
            Log.Error("Parsing of XML message failed: {0}", e.ToString());
        }
    }

    private void OnProcessReportRequest(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        /*
            <rcs:Message>   
                <hdr>
                    <source>LCSS_WDS_ID</source>                        <!-- sender, processId -->
                    <messageId>rcs.e2k.ctc.wds-4321</messageId>         <!-- id generated by sender --> 
                    <content>rcswds:processReportRequest</content>      <!--  --> 
                </hdr>
            <data>
                <rcswds:processReportRequest scopeId="{all, LCSS_RATOIF_ID}">   
                        <tsn>33</tsn>                                   <!-- transmission sequence number to be returned in processStateReport -message -->
                </rcswds:processReportRequest>
            </data>
            </rcs:Message>
        */

        try
        {
            string scope = msg.Attribute("scopeId").Value;

            if (IsMessageToUs(scope, msgProperties))
            {
                Log.Debug("Process report request message received from WDS");

                string correlationId = hdr["messageId"];
                string tsn = msg.Element("tsn").Value;

                SendProcessStateMessage(correlationId, tsn, GetState(getServiceState()));
            }
        }
        catch (Exception e)
        {
            Log.Error("Parsing of XML message failed: {0}", e.ToString());
        }
    }

    private void OnProcessStopRequest(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        /*
            <rcs:Message>   
                <hdr>
                    <source>LCSS_WDS_ID</source>                    <!-- sender, processId -->
                    <messageId>rcs.e2k.ctc.wds-2109</messageId>     <!-- id generated by sender --> 
                    <content>rcswds:processStopRequest</content>    <!--  --> 
                </hdr>
            <data>
                <rcswds:processStopRequest scopeId="{all, LCSS_RATOIF_ID}"> 
                </rcswds:processStopRequest>
            </data>
            </rcs:Message>
        */

        try
        {
            string scope = msg.Attribute("scopeId").Value;

            if (IsMessageToUs(scope, msgProperties))
            {
                Log.Information("Process stop request message received from WDS");

                serviceStateChange(ServiceState.Shutdown);
            }
        }
        catch (Exception e)
        {
            Log.Error("Parsing of XML message failed: {0}", e.ToString());
        }
    }
}

#pragma warning restore CS8602 // Dereference of a possibly null reference.
