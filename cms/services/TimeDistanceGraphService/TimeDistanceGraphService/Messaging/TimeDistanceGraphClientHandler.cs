namespace E2KService.MessageHandler;

using System;
using E2KService.ActiveMQ;
using System.Xml.Linq;
using E2KService.Model;
using System.Xml;
using Serilog;

internal class TimeDistanceGraphClientHandler : ActiveStateMessageHandler
{
	readonly ActiveMQ.AMQP.Rcs2kXmlMessageProcessor messageProcessor = new("Time-Distance Graph Client Message Processor");

	// Client requests

	// Channels
	static readonly Channel clientRequestChannel = new(ChannelType.Queue, "jms.queue.TimeDistanceGraphService.Requests"); // TODO: should be Topic, because if only one Artemis (Linux), may happen that standby service gets the message!!!

	// Schemas
	const string c_trainPositionsRequestSchema = "RCS.E2K.TimeDistanceGraphService.RequestTrainPositionsMsg.V1";
	const string c_estimationPlansRequestSchema = "RCS.E2K.TimeDistanceGraphService.RequestEstimationPlansMsg.V1";
	const string c_scheduledPlansRequestSchema = "RCS.E2K.TimeDistanceGraphService.RequestScheduledPlansMsg.V1";
	const string c_possessionsRequestSchema = "RCS.E2K.TimeDistanceGraphService.RequestPossessionsMsg.V1";

	const string c_activeServiceRequestSchema = "RCS.E2K.TimeDistanceGraphService.ActiveServiceRequest.V1";

	// Messages
	const string c_trainPositionsRequestMsgType = "RequestTrainPositions";
	const string c_estimationPlansRequestMsgType = "RequestEstimationPlans";
	const string c_scheduledPlansRequestMsgType = "RequestScheduledPlans";
	const string c_possessionsRequestMsgType = "RequestPossessions";

	const string c_activeServiceRequestMsgType = "ActiveServiceRequest";

	// Subscriptions
	static readonly Subscription TrainPositionRequestSubscription = new(clientRequestChannel, c_trainPositionsRequestMsgType);
	static readonly Subscription EstimationPlansRequestSubscription = new(clientRequestChannel, c_estimationPlansRequestMsgType);
	static readonly Subscription ScheduledPlanRequestSubscription = new(clientRequestChannel, c_scheduledPlansRequestMsgType);
	static readonly Subscription PossessionRequestSubscription = new(clientRequestChannel, c_possessionsRequestMsgType);
	static readonly Subscription ActiveServiceRequestSubscription = new(clientRequestChannel, c_activeServiceRequestMsgType);

	// Client indications

	// Channels
	static readonly Channel clientIndicationChannel = new(ChannelType.Topic, "jms.topic.TimeDistanceGraphService.Indications");

	// Schemas
	const string c_trainPosSchema = "RCS.E2K.TimeDistanceGraphService.TrainPosMsg.V1";
	const string c_estimationPlanSchema = "RCS.E2K.TimeDistanceGraphService.EstimationPlanMsg.V1";
	const string c_scheduledPlanSchema = "RCS.E2K.TimeDistanceGraphService.ScheduledPlanMsg.V1";
	const string c_possessionSchema = "RCS.E2K.TimeDistanceGraphService.PossessionMsg.V1";
	const string c_deleteEstimationPlanSchema = "RCS.E2K.TimeDistanceGraphService.DeleteEstimationPlanMsg.V1";
	const string c_deleteScheduledPlanSchema = "RCS.E2K.TimeDistanceGraphService.DeleteScheduledPlanMsg.V1";
	const string c_deletePossessionSchema = "RCS.E2K.TimeDistanceGraphService.DeletePossessionMsg.V1";

	const string c_activeServiceSchema = "RCS.E2K.TimeDistanceGraphService.ActiveService.V1";

	// Messages
	const string c_trainPosMsgType = "TrainPos";
	const string c_estimationPlanMsgType = "EstimationPlans";
	const string c_scheduledPlanMsgType = "ScheduledPlans";
	const string c_possessionMsgType = "Possessions";
	const string c_deleteEstimationPlanMsgType = "DeleteEstimationPlans";
	const string c_deleteScheduledPlanMsgType = "DeleteScheduledPlans";
	const string c_deletePossessionMsgType = "DeletePossessions";

	const string c_activeServiceMsgType = "ActiveService";

	private ActionTime trainPositionsRequestedFromTime = new(); // This means send all train positions!
	private ActionTime possessionsRequestedFromTime = new();    // This means send all possessions!

	////////////////////////////////////////////////////////////////////////////////

	public TimeDistanceGraphClientHandler(Connection connection, DataHandler dataHandler) : base(connection, dataHandler)
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
                Connection.Subscribe(TrainPositionRequestSubscription, this.messageProcessor, OnTrainPositionsRequest);
                Connection.Subscribe(EstimationPlansRequestSubscription, this.messageProcessor, OnEstimationPlansRequest);
                Connection.Subscribe(ScheduledPlanRequestSubscription, this.messageProcessor, OnScheduledPlansRequest);
                Connection.Subscribe(PossessionRequestSubscription, this.messageProcessor, OnPossessionsRequest);
                Connection.Subscribe(ActiveServiceRequestSubscription, this.messageProcessor, OnActiveServiceRequest);
                break;

            case MessagingStateSubscription.MessagingInactive:
                Connection.Unsubscribe(TrainPositionRequestSubscription);
                Connection.Unsubscribe(EstimationPlansRequestSubscription);
                Connection.Unsubscribe(ScheduledPlanRequestSubscription);
                Connection.Unsubscribe(PossessionRequestSubscription);
                Connection.Unsubscribe(ActiveServiceRequestSubscription);
                break;
        }
	}

	////////////////////////////////////////////////////////////////////////////////

	protected override void MessagingActivated()
    {
        HandleSubscriptions(MessagingStateSubscription.MessagingActive);
        
		// Add callbacks for DataHandler's model change notifications
		DataHandler.NotifyTrainPositionChanged += SendTrainPosition;
		DataHandler.NotifyTrainDeleted += SendDeleteTrain;
		DataHandler.NotifyEstimationPlanChanged += SendEstimationPlan;
		DataHandler.NotifyEstimationPlanDeleted += SendDeleteEstimationPlan;
		DataHandler.NotifyScheduledPlanChanged += SendScheduledPlan;
		DataHandler.NotifyScheduledPlanDeleted += SendDeleteScheduledPlan;
		DataHandler.NotifyPossessionChanged += SendPossession;
		DataHandler.NotifyPossessionDeleted += SendDeletePossession;

		DataHandler.NotifyTrainPositionsRefreshRequestEnded += HandlePendingTrainPositionsRequest;
		DataHandler.NotifyEstimationPlansRefreshRequestEnded += HandlePendingEstimationPlansRequest;
		DataHandler.NotifyScheduledPlansRefreshRequestEnded += HandlePendingScheduledPlansRequest;
		DataHandler.NotifyPossessionsRefreshRequestEnded += HandlePendingPossessionsRequest;

		SendActiveServiceInfo(true, true);

		Log.Information("TimeDistanceGraphClientHandler: Message processing activated: accepting messages");
	}

	protected override void MessagingDeactivated()
	{
        HandleSubscriptions(MessagingStateSubscription.MessagingInactive);

		SendActiveServiceInfo(false, true);

		// Remove callbacks for DataHandler's model change notifications
		DataHandler.NotifyTrainPositionChanged -= SendTrainPosition;
		DataHandler.NotifyTrainDeleted -= SendDeleteTrain;
		DataHandler.NotifyEstimationPlanChanged -= SendEstimationPlan;
		DataHandler.NotifyEstimationPlanDeleted -= SendDeleteEstimationPlan;
		DataHandler.NotifyScheduledPlanChanged -= SendScheduledPlan;
		DataHandler.NotifyScheduledPlanDeleted -= SendDeleteScheduledPlan;
		DataHandler.NotifyPossessionChanged -= SendPossession;
		DataHandler.NotifyPossessionDeleted -= SendDeletePossession;

		DataHandler.NotifyTrainPositionsRefreshRequestEnded -= HandlePendingTrainPositionsRequest;
		DataHandler.NotifyEstimationPlansRefreshRequestEnded -= HandlePendingEstimationPlansRequest;
		DataHandler.NotifyScheduledPlansRefreshRequestEnded -= HandlePendingScheduledPlansRequest;
		DataHandler.NotifyPossessionsRefreshRequestEnded -= HandlePendingPossessionsRequest;

		Log.Information("TimeDistanceGraphClientHandler: Message processing deactivated: rejecting messages");
	}

	////////////////////////////////////////////////////////////////////////////////

	private XElement AddEdgePosition(string posNodeName, EdgePosition pos)
	{
		var edgePos = new XElement(posNodeName,
								   new XAttribute("edge", pos.EdgeId),
								   new XAttribute("addpos", pos.AdditionalPos));
		if (pos.AdditionalName != "")
			edgePos.Add(new XAttribute("addname", pos.AdditionalName));

		return edgePos;
	}

	private XElement AddTimedLocations(List<TimedLocation> timedLocations, bool addOccurredAttributes = false, bool addTrips = false)
	{
        XElement masterElement = addTrips ? new XElement("Trips") : new XElement("TimedLocations");

        try
        {
            XElement? timedLocationsElement = masterElement;
			XElement? tripElement = null;

			int tripId = -1;

			foreach (var timedLocation in timedLocations)
			{
				if (addTrips)
				{
					if (timedLocation.TripId != tripId)
					{
						if (tripId != -1)
						{
							tripElement!.Add(timedLocationsElement);
							masterElement.Add(tripElement);
						}
						tripElement = new("Trip", new XAttribute("id", timedLocation.TripId), new XAttribute("name", timedLocation.TripName));
						timedLocationsElement = new XElement("TimedLocations");
						tripId = timedLocation.TripId;
					}
				}

                var loc = new XElement("Loc");
				if (timedLocation.Description != "")
					loc.Add(new XAttribute("description", timedLocation.Description));

				loc.Add(AddEdgePosition("Pos", timedLocation.Pos));

				if (timedLocation.Arrival.IsValid())
				{
					var arrival = new XElement("Arrival", timedLocation.Arrival.ToString());
					if (addOccurredAttributes)
						arrival.Add(new XAttribute("occurred", timedLocation.ArrivalOccurred));
					loc.Add(arrival);
				}
				if (timedLocation.Departure.IsValid())
				{
					var departure = new XElement("Departure", timedLocation.Departure.ToString());
					if (addOccurredAttributes)
						departure.Add(new XAttribute("occurred", timedLocation.DepartureOccurred));
					loc.Add(departure);
				}
				timedLocationsElement!.Add(loc);
			}

            if (addTrips && tripId != -1)
            {
                // Last trip
                tripElement!.Add(timedLocationsElement);
                masterElement.Add(tripElement);
            }
        }
        catch (Exception) { }

        return masterElement;
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

        hdr["destination"] = clientIndicationChannel.ChannelName;
		hdr["sender"] = Connection.ServiceId;
		hdr["schema"] = c_activeServiceSchema;

		msgProperties["rcsschema"] = c_activeServiceSchema;
		msgProperties["rcsMessageId"] = messageId;

		try
		{
			XElement msgNode = new(c_activeServiceMsgType,
									new XAttribute("active", active),
									new XAttribute("changed", changed));

			Connection.SendMessage(clientIndicationChannel, this.messageProcessor.CreateMessage(hdr, msgNode, msgProperties));
		}
		catch (Exception e)
		{
			Log.Error("Internal error in XML message creation: {0}", e.ToString());
		}
	}

	////////////////////////////////////////////////////////////////////////////////

	private XElement CreateMovementHistoryTrainNode(MovementHistoryItem movementHistoryItem)
	{
        XElement trainNode = new("Train",
                       new XAttribute("obid", movementHistoryItem.Obid),
                       new XAttribute("name", movementHistoryItem.Td),
                       new XAttribute("basetime", movementHistoryItem.OccurredTime.ToString()));

        if (movementHistoryItem.Postfix != "")
            trainNode.Add(new XAttribute("postfix", movementHistoryItem.Postfix));
        
		if (movementHistoryItem.TrainType != "")
            trainNode.Add(new XAttribute("traintype", movementHistoryItem.TrainType));

		return trainNode;
    }

	private void SendTrainPosition(MovementHistoryItem movementHistoryItem)
	{
		if (!AllowMessageProcessing)
			return;

		if (movementHistoryItem.IsValid())
		{
			Dictionary<string, string> hdr = new();
			Dictionary<string, string> msgProperties = new();

			var messageId = Connection.CreateNewMessageId();

            hdr["destination"] = clientIndicationChannel.ChannelName;
            hdr["sender"] = Connection.ServiceId;
			hdr["schema"] = c_trainPosSchema;

			msgProperties["rcsschema"] = c_trainPosSchema;
			msgProperties["rcsMessageId"] = messageId;

			try
			{
				XElement trainNode = CreateMovementHistoryTrainNode(movementHistoryItem);
				trainNode.Add(new XElement("p",
								new XAttribute("e", movementHistoryItem.EdgeId),
                                new XAttribute("p", movementHistoryItem.AdditionalPosition),
                                new XAttribute("t", "0")));

                XElement msgNode = new(c_trainPosMsgType, trainNode);

				Connection.SendMessage(clientIndicationChannel, this.messageProcessor.CreateMessage(hdr, msgNode, msgProperties));
			}
			catch (Exception e)
			{
				Log.Error("Internal error in XML message creation: {0}", e.ToString());
			}
		}
	}

	private void SendTrainPositions()
	{
		if (!AllowMessageProcessing)
			return;

		string? errorEncountered = null;
		
		// Start from this point of time
		ulong fromTimeMs = 0;
		if (this.trainPositionsRequestedFromTime.IsValid())
			fromTimeMs = this.trainPositionsRequestedFromTime.GetMilliSecondsFromEpoch();

		Dictionary<string, string> hdr = new();
		Dictionary<string, string> msgProperties = new();

        hdr["destination"] = clientIndicationChannel.ChannelName;
        hdr["sender"] = Connection.ServiceId;
		hdr["schema"] = c_trainPosSchema;

		msgProperties["rcsschema"] = c_trainPosSchema;

		foreach (var item in DataHandler.TrainMovementHistory)
		{
			// Sort concurrent collection by action time
			var singleTrainHistory = item.Value.OrderBy(x => x.Key).ToDictionary(k => k.Key, v => v.Value);

			var messageId = Connection.CreateNewMessageId();
			msgProperties["rcsMessageId"] = messageId;

			ulong? firstHistoryTimeMs = null;
			string currentTd = "";
			string currentPostfix = "";
			string currentTrainType = "";

			XElement msgNode = new(c_trainPosMsgType);
			XElement? trainNode = null;

			foreach (var historyItem in singleTrainHistory)
			{
				var currentHistoryTimeMs = historyItem.Key;

				if (currentHistoryTimeMs >= fromTimeMs)
				{
					MovementHistoryItem movementHistoryItem = historyItem.Value;
					
					try
					{
						// If first item or train describer or postfix or train type changes, new train node must be created
						if (firstHistoryTimeMs == null || (!movementHistoryItem.Terminated && movementHistoryItem.Td != currentTd) || movementHistoryItem.Postfix != currentPostfix || movementHistoryItem.TrainType != currentTrainType)
						{
							firstHistoryTimeMs = currentHistoryTimeMs;
							currentTd = movementHistoryItem.Td;
							currentPostfix = movementHistoryItem.Postfix;
							currentTrainType = movementHistoryItem.TrainType;

                            if (trainNode != null)
								msgNode.Add(trainNode);

							trainNode = CreateMovementHistoryTrainNode(movementHistoryItem);
						}

						if (trainNode != null)
						{
							int occurredTime = (int) ((currentHistoryTimeMs - firstHistoryTimeMs) / 1000 + 0.5); // In seconds!

							if (!movementHistoryItem.Terminated)
							{
								trainNode.Add(new XElement("p",
												new XAttribute("e", movementHistoryItem.EdgeId),
												new XAttribute("p", movementHistoryItem.AdditionalPosition),
												new XAttribute("t", occurredTime)));
							}
							else
								trainNode.Add(new XElement("d", new XAttribute("t", occurredTime)));
						}
					}
					catch (Exception ex)
					{
						// Catch only first error
						if (errorEncountered == null)
							errorEncountered = ex.Message;
					}
				}
			}

			if (firstHistoryTimeMs != null)
			{
				// Add last train node to message
				if (trainNode != null)
					msgNode.Add(trainNode);

				Connection.SendMessage(clientIndicationChannel, this.messageProcessor.CreateMessage(hdr, msgNode, msgProperties));
			}
		}
		
		if (errorEncountered != null)
			Log.Error("Internal error happened in XML message creation, some movement history items were discarded: {0}", errorEncountered);

		this.trainPositionsRequestedFromTime = new ActionTime();	// This means send all train positions!
	}
	
	private void SendDeleteTrain(MovementHistoryItem movementHistoryItem)
	{
		if (!AllowMessageProcessing)
			return;

		if (movementHistoryItem.IsValid() && movementHistoryItem.Terminated)
		{
			Dictionary<string, string> hdr = new();
			Dictionary<string, string> msgProperties = new();

			var messageId = Connection.CreateNewMessageId();

            hdr["destination"] = clientIndicationChannel.ChannelName;
            hdr["sender"] = Connection.ServiceId;
			hdr["schema"] = c_trainPosSchema;

			msgProperties["rcsschema"] = c_trainPosSchema;
			msgProperties["rcsMessageId"] = messageId;

			try
			{
				XElement trainNode = CreateMovementHistoryTrainNode(movementHistoryItem);
				trainNode.Add(new XElement("d", new XAttribute("t", "0")));

                XElement msgNode = new(c_trainPosMsgType, trainNode);

				Connection.SendMessage(clientIndicationChannel, this.messageProcessor.CreateMessage(hdr, msgNode, msgProperties));
			}
			catch (Exception e)
			{
				Log.Error("Internal error in XML message creation: {0}", e.ToString());
			}
		}
	}
	
	////////////////////////////////////////////////////////////////////////////////

	private void SendEstimationPlan(EstimationPlan estimationPlan)
	{
		if (!AllowMessageProcessing)
			return;

		if (estimationPlan.IsValid())
		{
			Dictionary<string, string> hdr = new();
			Dictionary<string, string> msgProperties = new();

			var messageId = Connection.CreateNewMessageId();

            hdr["destination"] = clientIndicationChannel.ChannelName;
            hdr["sender"] = Connection.ServiceId;
			hdr["schema"] = c_estimationPlanSchema;

			msgProperties["rcsschema"] = c_estimationPlanSchema;
			msgProperties["rcsMessageId"] = messageId;

			try
			{
                XElement msgNode = new(c_estimationPlanMsgType);
                
				if (estimationPlan.IsTrainEstimationPlan())
				{
                    // Send estimation plan for allocated train (occurred events added to message)
                    var AddEdges = () =>
					{
						var edges = new XElement("Edges");
						foreach (var edgeName in estimationPlan.TrainPath.Edges)
						{
							var edge = new XElement("Edge",
								new XAttribute("name", edgeName));
							edges.Add(edge);
						}
						return edges;
					};

					string td = estimationPlan.Td;

					Train? train = DataHandler.GetTrain(estimationPlan.Obid);
					if (train != null)
						td = train.Td;

					XElement trainNode = new XElement("Train",
										   new XAttribute("obid", estimationPlan.Obid),
										   new XAttribute("name", td),
										   new XAttribute("tripId", estimationPlan.TripId),
										   AddTimedLocations(estimationPlan.TimedLocations, true),
										   new XElement("TrainPath",
										   AddEdgePosition("StartPos", estimationPlan.TrainPath.StartPos),
										   AddEdgePosition("EndPos", estimationPlan.TrainPath.EndPos),
										   AddEdges()));
					msgNode.Add(trainNode);
				}
				else
				{
                    // Send estimation plan prepared for scheduled plan trip(s) (occurred events are not added to message, but trips are)
                    var scheduledPlanKey = estimationPlan.ScheduledPlanKey;
                    if (scheduledPlanKey != null)
                    {
                        XElement scheduledPlanNode = new("ScheduledPlan", new XAttribute("dayCode", scheduledPlanKey.ScheduledDayCode), new XAttribute("id", scheduledPlanKey.ScheduledPlanId), new XAttribute("name", scheduledPlanKey.ScheduledPlanName));

                        XElement tripsNode = AddTimedLocations(estimationPlan.TimedLocations, addTrips: true);
                        scheduledPlanNode.Add(tripsNode);
                        msgNode.Add(scheduledPlanNode);
                    }
                    else
                        throw new Exception("Scheduled plan not found from estimation plan");
                }

                Connection.SendMessage(clientIndicationChannel, this.messageProcessor.CreateMessage(hdr, msgNode, msgProperties));
			}
			catch (Exception e)
			{
				Log.Error("Internal error in XML message creation: {0}", e.ToString());
			}
		}
	}
	
	private void SendEstimationPlans()
	{
		if (!AllowMessageProcessing)
			return;

		foreach (var estimationPlan in DataHandler.TrainEstimationPlans.Values)
			SendEstimationPlan(estimationPlan);

        foreach (var estimationPlan in DataHandler.EstimationPlans.Values)
            SendEstimationPlan(estimationPlan);
    }

    private void SendDeleteEstimationPlan(EstimationPlan estimationPlan)
	{
		if (!AllowMessageProcessing)
			return;

		if (estimationPlan.IsValid())
		{
			Dictionary<string, string> hdr = new();
			Dictionary<string, string> msgProperties = new();

			var messageId = Connection.CreateNewMessageId();

            hdr["destination"] = clientIndicationChannel.ChannelName;
            hdr["sender"] = Connection.ServiceId;
			hdr["schema"] = c_deleteEstimationPlanSchema;

			msgProperties["rcsschema"] = c_deleteEstimationPlanSchema;
			msgProperties["rcsMessageId"] = messageId;

			try
			{
                XElement msgNode = new(c_deleteEstimationPlanMsgType);

				if (estimationPlan.IsTrainEstimationPlan())
				{
					msgNode.Add(new XElement("Train",
								new XAttribute("obid", estimationPlan.Obid),
								new XAttribute("name", estimationPlan.Td)));
				}
				else
				{
                    // Delete estimation plan of scheduled plan (all trips)
                    if (estimationPlan.ScheduledPlanKey != null && DataHandler.ScheduledPlans.Keys.Contains(estimationPlan.ScheduledPlanKey))
                    {
						ScheduledPlan scheduledPlan = DataHandler.ScheduledPlans[estimationPlan.ScheduledPlanKey];

                        foreach (var trip in scheduledPlan.Trips)
                        {
                            XElement scheduledPlanNode = new("ScheduledPlan",
                                new XAttribute("dayCode", scheduledPlan.DayCode),
                                new XAttribute("id", scheduledPlan.Id),
                                new XAttribute("name", scheduledPlan.Name),
                                new XAttribute("tripId", trip.Id),
                                new XAttribute("tripName", trip.Description)
                                );
                            msgNode.Add(scheduledPlanNode);
                        }
                    }
                    else
                        throw new Exception("Scheduled plan not found from estimation plan");
                }

                Connection.SendMessage(clientIndicationChannel, this.messageProcessor.CreateMessage(hdr, msgNode, msgProperties));
			}
			catch (Exception e)
			{
				Log.Error("Internal error in XML message creation: {0}", e.ToString());
			}
		}
	}
	
	////////////////////////////////////////////////////////////////////////////////

	private void SendScheduledPlan(ScheduledPlan scheduledPlan)
	{
		if (!AllowMessageProcessing)
			return;

		if (scheduledPlan.IsValid())
		{
			Dictionary<string, string> hdr = new();
			Dictionary<string, string> msgProperties = new();

			var messageId = Connection.CreateNewMessageId();

            hdr["destination"] = clientIndicationChannel.ChannelName;
            hdr["sender"] = Connection.ServiceId;
			hdr["schema"] = c_scheduledPlanSchema;

			msgProperties["rcsschema"] = c_scheduledPlanSchema;
			msgProperties["rcsMessageId"] = messageId;

			try
			{
				var AddTrips = () =>
				{
					var trips = new XElement("Trips");
					foreach (var trip in scheduledPlan.Trips)
					{
						var tripElement = new XElement("Trip",
                                                       new XAttribute("id", trip.Id),
                                                       new XAttribute("number", trip.TripNumber),
													   new XAttribute("active", trip.ActiveTrip),
													   new XAttribute("allocated", trip.Allocated),
                                                       new XAttribute("description", trip.Description),
													   AddEdgePosition("StartPos", trip.StartPos),
													   AddEdgePosition("EndPos", trip.EndPos));
						if (trip.StartTime.IsValid())
							tripElement.Add(new XElement("StartTime", trip.StartTime.ToString()));
						if (trip.EndTime.IsValid())
							tripElement.Add(new XElement("EndTime", trip.EndTime.ToString()));
						tripElement.Add(AddTimedLocations(trip.TimedLocations));
						trips.Add(tripElement);
					}
					return trips;
				};

				XElement msgNode = new(c_scheduledPlanMsgType,
									   new XElement("Plan",
									   new XAttribute("id", scheduledPlan.Id),
									   new XAttribute("name", scheduledPlan.Name),
									   new XAttribute("traintype", scheduledPlan.TrainType),
									   new XAttribute("description", scheduledPlan.Description),
									   new XAttribute("active", scheduledPlan.ActivePlan),
                                       new XAttribute("allocated", scheduledPlan.Allocated),
                                       AddTrips()));

				Connection.SendMessage(clientIndicationChannel, this.messageProcessor.CreateMessage(hdr, msgNode, msgProperties));
			}
			catch (Exception e)
			{
				Log.Error("Internal error in XML message creation: {0}", e.ToString());
			}
		}
	}
	
	private void SendScheduledPlans()
	{
		if (!AllowMessageProcessing)
			return;

		foreach (var scheduledPlan in DataHandler.ScheduledPlans.Values)
			SendScheduledPlan(scheduledPlan);
	}

	private void SendDeleteScheduledPlan(ScheduledPlan scheduledPlan)
	{
		if (!AllowMessageProcessing)
			return;

		if (scheduledPlan.IsValid())
		{
			Dictionary<string, string> hdr = new();
			Dictionary<string, string> msgProperties = new();

			var messageId = Connection.CreateNewMessageId();

            hdr["destination"] = clientIndicationChannel.ChannelName;
            hdr["sender"] = Connection.ServiceId;
			hdr["schema"] = c_deleteScheduledPlanSchema;

			msgProperties["rcsschema"] = c_deleteScheduledPlanSchema;
			msgProperties["rcsMessageId"] = messageId;

			try
			{
				XElement msgNode = new(c_deleteScheduledPlanMsgType,
									   new XElement("Plan",
									   new XAttribute("id", scheduledPlan.Id),
									   new XAttribute("name", scheduledPlan.Name)));

				Connection.SendMessage(clientIndicationChannel, this.messageProcessor.CreateMessage(hdr, msgNode, msgProperties));
			}
			catch (Exception e)
			{
				Log.Error("Internal error in XML message creation: {0}", e.ToString());
			}
		}
	}
	
	////////////////////////////////////////////////////////////////////////////////

	private void SendPossession(Possession possession)
	{
		if (!AllowMessageProcessing)
			return;

		if (possession.IsValid())
		{
			Dictionary<string, string> hdr = new();
			Dictionary<string, string> msgProperties = new();

			var messageId = Connection.CreateNewMessageId();

            hdr["destination"] = clientIndicationChannel.ChannelName;
            hdr["sender"] = Connection.ServiceId;
			hdr["schema"] = c_possessionSchema;

			msgProperties["rcsschema"] = c_possessionSchema;
			msgProperties["rcsMessageId"] = messageId;

			try
			{
				var CreateActivationActions = () =>
				{
					var activationActionsNode = new XElement("ActivationActions");
					foreach (var action in possession.GetActivationActions())
					{
						if (action.Item1.IsValid())
							activationActionsNode.Add(new XElement(action.Item2 ? "Activated" : "Deactivated", new XAttribute("state", action.Item3), action.Item1.ToString()));
					}
					return activationActionsNode;
				};

				XElement possessionElement = new("Possession",
										new XAttribute("id", possession.GetId()),
										new XAttribute("name", possession.ExternalId),
										new XAttribute("description", possession.Description),
										new XAttribute("historical", possession.IsHistoric()),
										AddEdgePosition("StartPos", possession.StartPos),
										AddEdgePosition("EndPos", possession.EndPos));

				if (possession.StartTime.IsValid())
					possessionElement.Add(new XElement("StartTime", possession.StartTime.ToString()));
				if (possession.EndTime.IsValid())
					possessionElement.Add(new XElement("EndTime", possession.EndTime.ToString()));

				possessionElement.Add(new XElement("State", possession.State));
				possessionElement.Add(new XElement("Active", possession.IsActive()));
				possessionElement.Add(CreateActivationActions());

				XElement msgNode = new(c_possessionMsgType,  possessionElement);

				Connection.SendMessage(clientIndicationChannel, this.messageProcessor.CreateMessage(hdr, msgNode, msgProperties));
			}
			catch (Exception e)
			{
				Log.Error("Internal error in XML message creation: {0}", e.ToString());
			}
		}
	}
	
	private void SendPossessions()
	{
		if (!AllowMessageProcessing)
			return;

		foreach (var possession in DataHandler.Possessions.Values)
		{
			// If historic possession's end time is later than requested time, send it. Active possessions are always sent.
			if (!possession.IsHistoric() || !this.possessionsRequestedFromTime.IsValid() || (possession.EndTime - this.possessionsRequestedFromTime).TotalSeconds > 0)
				SendPossession(possession);
		}

		this.possessionsRequestedFromTime = new ActionTime();	// This means send all possessions
	}
	
	private void SendDeletePossession(Possession possession)
	{
		if (!AllowMessageProcessing)
			return;

		if (possession.IsValid())
		{
			Dictionary<string, string> hdr = new();
			Dictionary<string, string> msgProperties = new();

			var messageId = Connection.CreateNewMessageId();

            hdr["destination"] = clientIndicationChannel.ChannelName;
            hdr["sender"] = Connection.ServiceId;
			hdr["schema"] = c_deletePossessionSchema;

			msgProperties["rcsschema"] = c_deletePossessionSchema;
			msgProperties["rcsMessageId"] = messageId;

			try
			{
				// Inform deletion with previous ID, not the current one!
				// Or with current one, if possession has never been activated and is deleted before that!
				// Nevertheless, the ID is correct in both the cases
				string deletedPossessionId = possession.GetId(true);

				XElement msgNode = new(c_deletePossessionMsgType,
									   new XElement("Possession",
									   new XAttribute("id", deletedPossessionId)));

				Connection.SendMessage(clientIndicationChannel, this.messageProcessor.CreateMessage(hdr, msgNode, msgProperties));
			}
			catch (Exception e)
			{
				Log.Error("Internal error in XML message creation: {0}", e.ToString());
			}
		}
	}

	////////////////////////////////////////////////////////////////////////////////

	private void OnTrainPositionsRequest(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties)
	{
		if (!AllowMessageProcessing)
			return;

		try
		{
			string schema = hdr["schema"];

			if (schema == c_trainPositionsRequestSchema)
			{
				// Set earliest requested from time
				var fromNode = msg.Element("From");
				if (fromNode != null)
				{
					ActionTime fromTime = new();
					if (fromTime.InitFromISOString(fromNode.Value) && (!this.trainPositionsRequestedFromTime.IsValid() || this.trainPositionsRequestedFromTime.DateTime > fromTime.DateTime))
					{
						this.trainPositionsRequestedFromTime = fromTime;
					}
				}

				if (!DataHandler.IsTrainPositionsRequestPending())
				{
					SendTrainPositions();
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

	private void OnEstimationPlansRequest(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties)
	{
		if (!AllowMessageProcessing || DataHandler.IsEstimationPlansRequestPending())
			return;

		try
		{
			string schema = hdr["schema"];

			if (schema == c_estimationPlansRequestSchema)
			{
				SendEstimationPlans();
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

	private void OnScheduledPlansRequest(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties)
	{
		if (!AllowMessageProcessing || DataHandler.IsScheduledPlansRequestPending())
			return;

		try
		{
			string schema = hdr["schema"];

			if (schema == c_scheduledPlansRequestSchema)
			{
				SendScheduledPlans();
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

	private void OnPossessionsRequest(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties)
	{
		if (!AllowMessageProcessing)
			return;

		try
		{
			string schema = hdr["schema"];

			if (schema == c_possessionsRequestSchema)
			{
				// Set earliest requested from time
				var fromNode = msg.Element("From");
				if (fromNode != null)
				{
					ActionTime fromTime = new();
					if (fromTime.InitFromISOString(fromNode.Value) && (!this.possessionsRequestedFromTime.IsValid() || this.possessionsRequestedFromTime.DateTime > fromTime.DateTime))
					{
						this.possessionsRequestedFromTime = fromTime;
					}
				}

				if (!DataHandler.IsPossessionsRequestPending())
				{
					SendPossessions();
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

	private void OnActiveServiceRequest(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties)
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
	////////////////////////////////////////////////////////////////////////////////

	private void HandlePendingTrainPositionsRequest()
	{
		Log.Information("Train positions data refresh request ended, sending all train positions to clients");

		SendTrainPositions();
	}

	private void HandlePendingEstimationPlansRequest()
	{
		Log.Information("Estimation plans data refresh request ended, sending all estimation plans to clients");

		SendEstimationPlans();
	}
	
	private void HandlePendingScheduledPlansRequest()
	{
		Log.Information("Scheduled plans data refresh request ended, sending all scheduled plans to clients");
		
		SendScheduledPlans();
	}
	
	private void HandlePendingPossessionsRequest()
	{
		Log.Information("Possessions data refresh request ended, sending all possessions to clients");

		SendPossessions();
	}
}
