namespace E2KService.MessageHandler;

using System;
using E2KService.ActiveMQ;
using System.Xml.Linq;
using ConflictManagementService.Model;
using System.Xml;
using Apache.NMS;
using Serilog;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using System.Collections.Generic;
using System.Linq;

internal class ConflictManagementClientHandler : ActiveStateMessageHandler
{
	readonly ActiveMQ.AMQP.Rcs2kXmlMessageProcessor messageProcessor = new("Conflict Management Client Message Processor");

	// Conflicts related data ConflictManagementService -> TSUI

	// Channels
	static readonly Channel serverInfoChannel = new(ChannelType.Topic, "jms.topic.rcs.e2k.service.conflictmanagement.conflictinfo");

	// Schemas
	const string c_activeServiceSchema = "RCS.E2K.SERVICE.ConflictManagement.ActiveService.V1";
	const string c_stationPrioritiesSchema = "RCS.E2K.SERVICE.ConflictManagement.StationPriorities.V1";
    const string c_tripPropertiesSchema = "RCS.E2K.SERVICE.ConflictManagement.TripProperties.V1";

	// Messages
	const string c_activeServiceMsgType = "ActiveService";
	const string c_stationPrioritiesMsgType = "StationPriorities";
    const string c_tripPropertiesMsgType = "TripProperties";

	// Conflicts related data TSUI -> ConflictManagementService

	// Channels
	static readonly Channel clientInfoChannel = new(ChannelType.Topic, "jms.topic.rcs.e2k.client.conflictmanagement.conflictinfo");

	// Schemas
    const string c_activeServiceRequestSchema = "RCS.E2K.CLIENT.ConflictManagement.ActiveServiceRequest.V1";
	const string c_stationPrioritiesRequestSchema = "RCS.E2K.CLIENT.ConflictManagement.StationPrioritiesRequest.V1";
	const string c_stationPriorityChangeRequestSchema = "RCS.E2K.CLIENT.ConflictManagement.StationPriorityChangeRequest.V1";
    const string c_tripPropertiesRequestSchema = "RCS.E2K.CLIENT.ConflictManagement.TripPropertiesRequest.V1";
    const string c_tripPropertyChangeRequestSchema = "RCS.E2K.CLIENT.ConflictManagement.TripPropertyChangeRequest.V1";

	// Messages
    const string c_activeServiceRequestMsgType = "ActiveServiceRequest";
	const string c_stationPrioritiesRequestMsgType = "StationPrioritiesRequest";
	const string c_stationPriorityChangeRequestMsgType = "StationPriorityChangeRequest";
    const string c_tripPropertiesRequestMsgType = "TripPropertiesRequest";
    const string c_tripPropertyChangeRequestMsgType = "TripPropertyChangeRequest";

	// Subscriptions
    static readonly Subscription ActiveServiceRequestSubscription = new(clientInfoChannel, c_activeServiceRequestMsgType);
	static readonly Subscription StationPrioritiesRequestSubscription = new(clientInfoChannel, c_stationPrioritiesRequestMsgType);
	static readonly Subscription StationPriorityChangeRequestSubscription = new(clientInfoChannel, c_stationPriorityChangeRequestMsgType);
    static readonly Subscription TripPropertiesRequestSubscription = new(clientInfoChannel, c_tripPropertiesRequestMsgType);
    static readonly Subscription TripPropertyChangeRequestSubscription = new(clientInfoChannel, c_tripPropertyChangeRequestMsgType);

	////////////////////////////////////////////////////////////////////////////////

	public ConflictManagementClientHandler(Connection connection, DataHandler dataHandler) : base(connection, dataHandler)
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
                Connection.Subscribe(ActiveServiceRequestSubscription, this.messageProcessor, OnActiveServiceRequest);
                Connection.Subscribe(StationPrioritiesRequestSubscription, this.messageProcessor, OnStationPrioritiesRequest);
                Connection.Subscribe(StationPriorityChangeRequestSubscription, this.messageProcessor, OnStationPriorityChangeRequest);
                Connection.Subscribe(TripPropertiesRequestSubscription, this.messageProcessor, OnTripPropertiesRequest);
                Connection.Subscribe(TripPropertyChangeRequestSubscription, this.messageProcessor, OnTripPropertyChangeRequest);
                break;

            case MessagingStateSubscription.MessagingInactive:
                Connection.Unsubscribe(ActiveServiceRequestSubscription);
                Connection.Unsubscribe(StationPrioritiesRequestSubscription);
                Connection.Unsubscribe(StationPriorityChangeRequestSubscription);
                Connection.Unsubscribe(TripPropertiesRequestSubscription);
                Connection.Unsubscribe(TripPropertyChangeRequestSubscription);
                break;
        }
	}

	////////////////////////////////////////////////////////////////////////////////

	protected override void MessagingActivated()
    {
        HandleSubscriptions(MessagingStateSubscription.MessagingActive);
        
		// Add callbacks for DataHandler's model change notifications
		DataHandler.NotifyStationPriorityChanged += SendStationPriority;
        DataHandler.NotifyTripPropertyChanged += SendTripProperty;

		SendActiveServiceInfo(true, true);

		Log.Information("ConflictManagementClientHandler: Message processing activated: accepting messages");
	}

	protected override void MessagingDeactivated()
	{
        HandleSubscriptions(MessagingStateSubscription.MessagingInactive);

		SendActiveServiceInfo(false, true);

		// Remove callbacks for DataHandler's model change notifications
		DataHandler.NotifyStationPriorityChanged -= SendStationPriority;
        DataHandler.NotifyTripPropertyChanged -= SendTripProperty;

		Log.Information("ConflictManagementClientHandler: Message processing deactivated: rejecting messages");
	}

	////////////////////////////////////////////////////////////////////////////////

    public void SendActiveServiceInfo(bool active, bool changed)
	{
		// Should we send this also in inactive server?
		if (!AllowMessageProcessing)
			return;

		Dictionary<string, string> hdr = new();
		Dictionary<string, string> msgProperties = new();

		var messageId = Connection.CreateNewMessageId();

        hdr["destination"] = serverInfoChannel.ChannelName;
		hdr["sender"] = Connection.ServiceId;
		hdr["schema"] = c_activeServiceSchema;

		msgProperties["rcsschema"] = c_activeServiceSchema;
		msgProperties["rcsMessageId"] = messageId;

		try
		{
			XElement msgNode = new(c_activeServiceMsgType,
									new XAttribute("state", active),
									new XAttribute("changed", changed));

			Connection.SendMessage(serverInfoChannel, this.messageProcessor.CreateMessage(hdr, msgNode, msgProperties));
		}
		catch (Exception e)
		{
			Log.Error("Internal error in XML message creation: {0}", e.ToString());
		}
	}
	
	////////////////////////////////////////////////////////////////////////////////

	private void SendStationPriority(Station station)
	{
		if (station != null)
		{
			SendStationPriorities(new List<Station>() { station });
		}
	}

	private void SendStationPriorities()
	{
		SendStationPriorities(this.DataHandler.Stations.Values.ToList());
	}

	private void SendStationPriorities(List<Station> stations)
	{
		if (!AllowMessageProcessing)
			return;

		try
		{
			Dictionary<string, string> hdr = new();
			Dictionary<string, string> msgProperties = new();

			var messageId = Connection.CreateNewMessageId();

			hdr["destination"] = serverInfoChannel.ChannelName;
			hdr["sender"] = Connection.ServiceId;
			hdr["schema"] = c_stationPrioritiesSchema;

			msgProperties["rcsschema"] = c_stationPrioritiesSchema;
			msgProperties["rcsMessageId"] = messageId;

			XElement msgNode = new(c_stationPrioritiesMsgType);

			foreach (var station in stations)
			{
				if (station != null)
				{
					try
					{
						// Priority direction now here: Nominal is to increasing kilometers
						XElement stationPriorityNode = new("StationPriority",
											   new XAttribute("station", station.StationId),
											   new XAttribute("priority", station.StationPriority == Station.Priority.NominalPriority ? "increasing" : (station.StationPriority == Station.Priority.OppositePriority ? "decreasing" : "nopriority")));
						msgNode.Add(stationPriorityNode);
					}
					catch (Exception e)
					{
					}
				}
			}

			Connection.SendMessage(serverInfoChannel, this.messageProcessor.CreateMessage(hdr, msgNode, msgProperties));
		}
		catch (Exception e)
		{
			Log.Error("Internal error in XML message creation: {0}", e.ToString());
		}
	}

    ////////////////////////////////////////////////////////////////////////////////

    private void SendTripProperty(TripProperty tripProperty)
    {
        if (tripProperty != null)
        {
            SendTripProperties(new List<TripProperty>() { tripProperty });
        }
    }

    private void SendTripProperties()
    {
        SendTripProperties(this.DataHandler.TripProperties.Values.ToList());
    }

    private void SendTripProperties(List<TripProperty> tripProperties)
    {
        if (!AllowMessageProcessing)
            return;

        try
        {
            Dictionary<string, string> hdr = new();
            Dictionary<string, string> msgProperties = new();

            var messageId = Connection.CreateNewMessageId();

            hdr["destination"] = serverInfoChannel.ChannelName;
            hdr["sender"] = Connection.ServiceId;
            hdr["schema"] = c_tripPropertiesSchema;

            msgProperties["rcsschema"] = c_tripPropertiesSchema;
            msgProperties["rcsMessageId"] = messageId;

            XElement msgNode = new(c_tripPropertiesMsgType);

            foreach (var tripProperty in tripProperties)
            {
                if (tripProperty != null)
                {
                    int tripId = 0;

                    var scheduledPlan = DataHandler.GetScheduledPlan(new ScheduledPlanKey(tripProperty.ScheduledDayCode, tripProperty.ServiceName));
                    if (scheduledPlan != null)
                        tripId = scheduledPlan.GetTripIdByTripCode(tripProperty.TripCode);

                    try
                    {
                        var tripPropertyNode = new XElement("TripProperty",
                                                            new XAttribute("scheduleddaycode", tripProperty.ScheduledDayCode),
                                                            new XAttribute("servicename", tripProperty.ServiceName),
                                                            new XAttribute("tripcode", tripProperty.TripCode),
                                                            new XAttribute("tripid", tripId));
                        tripPropertyNode.Add(new XElement("TrainLength", tripProperty.TrainLength));

                        //TODO: Now use just one delay for trip
                        tripPropertyNode.Add(new XElement("Delays", new XElement("Delay",
                            new XAttribute("platform", ""),
                            new XAttribute("arrival", ""),
                            new XAttribute("departure", tripProperty.DelaySeconds))));
                        
                        msgNode.Add(tripPropertyNode);
                    }
                    catch (Exception e)
                    {
                    }
                }
            }

            Connection.SendMessage(serverInfoChannel, this.messageProcessor.CreateMessage(hdr, msgNode, msgProperties));
        }
        catch (Exception e)
        {
            Log.Error("Internal error in XML message creation: {0}", e.ToString());
        }
    }

	////////////////////////////////////////////////////////////////////////////////

	private void OnActiveServiceRequest(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        // Standby server does not answer to request
        if (!AllowMessageProcessing)
            return;

        try
        {
            string schema = hdr["schema"];

            if (schema == c_activeServiceRequestSchema)
            {
                SendActiveServiceInfo(true, false);
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

    private void OnStationPrioritiesRequest(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
	{
		if (!AllowMessageProcessing)
			return;

		try
		{
			string schema = hdr["schema"];

			if (schema == c_stationPrioritiesRequestSchema)
			{
				SendStationPriorities();
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

    private void OnStationPriorityChangeRequest(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        if (!AllowMessageProcessing)
            return;

        try
        {
            string schema = hdr["schema"];

            if (schema == c_stationPriorityChangeRequestSchema)
            {
                string? stationId = msg.Attribute("station")?.Value;
                if (stationId != null)
                {
                    string? priorityInMsg = msg.Attribute("priority")?.Value;
                    Station.Priority priority;

                    // Priority direction now here: Nominal is to increasing kilometers
                    if (priorityInMsg == "increasing")
                        priority = Station.Priority.NominalPriority;
                    else if (priorityInMsg == "decreasing")
                        priority = Station.Priority.OppositePriority;
                    else if (priorityInMsg == "nopriority")
                        priority = Station.Priority.NoPriority;
                    else
                        throw new Exception($"Unknown station priority type {priorityInMsg}");

                    this.DataHandler.StationPriorityChangeRequested(stationId, priority);
                }
                else
                    Log.Error($"Station ID missing from message: {schema}");
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

    private void OnTripPropertiesRequest(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        if (!AllowMessageProcessing)
            return;

        try
        {
            string schema = hdr["schema"];

            if (schema == c_tripPropertiesRequestSchema)
            {
                SendTripProperties();
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

    private void OnTripPropertyChangeRequest(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        if (!AllowMessageProcessing)
            return;

        try
        {
            string schema = hdr["schema"];

            if (schema == c_tripPropertyChangeRequestSchema)
            {
                string? scheduledDayCode = msg.Attribute("scheduleddaycode")?.Value;
                string? serviceName = msg.Attribute("servicename")?.Value;
                string? tripCode = msg.Attribute("tripcode")?.Value;
                string? tripId = msg.Attribute("tripId")?.Value;

                if (scheduledDayCode != null && serviceName != null && tripCode != null)
                {
                    int trainLength = 0;
                    int delay = 0;

                    var trainLengthNode = msg.Element("TrainLength");
                    if (trainLengthNode != null)
                        trainLength = int.Parse(trainLengthNode.Value);

                    // Now only one delay for trip
                    var delaysNode = msg.Element("Delays");
                    if (delaysNode != null)
                    {
                        var delayNode = delaysNode.Element("Delay");
                        if (delayNode != null)
                        {
                            // delayNode.Attribute("platform")
                            // delayNode.Attribute("arrival")
                            var departureAttr = delayNode.Attribute("departure");
                            if (departureAttr != null)
                                delay = int.Parse(departureAttr.Value);
                        }
                    }

                    this.DataHandler.TripPropertyChangeRequested(scheduledDayCode, serviceName, tripCode, trainLength, delay);
                }
                else
                    Log.Error($"Service or trip ID missing from message: {schema}");
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

}
