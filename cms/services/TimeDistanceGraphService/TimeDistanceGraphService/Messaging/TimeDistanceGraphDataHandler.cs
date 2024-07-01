namespace E2KService.MessageHandler;

using System;
using E2KService.ActiveMQ;
using System.Xml.Linq;
using E2KService.Model;
using System.Xml;
using Serilog;
using System.Configuration;
using System.Security.Cryptography;

internal class TimeDistanceGraphDataHandler : ActiveStateMessageHandler
{
	readonly ActiveMQ.AMQP.Rcs2kXmlMessageProcessor messageProcessor = new("Time-Distance Graph Data Message Processor");

	// External information requests

	// Channels
	static readonly Channel trainInfoRequestChannel = new(ChannelType.Queue, "jms.queue.rcs.e2k.ext.train.request");
	static readonly Channel estimationInfoRequestChannel = new(ChannelType.Queue, "jms.queue.rcs.e2k.ext.estimation.request");
	static readonly Channel schedulingInfoRequestChannel = new(ChannelType.Queue, "jms.queue.rcs.e2k.ext.scheduling.request");
	static readonly Channel restrictionInfoRequestChannel = new(ChannelType.Queue, "jms.queue.rcs.e2k.ext.restriction.request");

	// Schemas
	const string trainPositionRequestSchema = "rcs.e2k.ext.train.request.position.V1";
	const string estimationPlansRequestSchema = "rcs.e2k.ext.estimation.request.plan.V1";
	const string scheduledPlansRequestSchema = "rcs.e2k.ext.scheduling.request.plan.V1";
	const string possessionsRequestSchema = "rcs.e2k.ext.possession.request.V1";

	// Messages
	const string trainPositionRequestMsgType = "trainPositionRequest";
	const string estimationPlansRequestMsgType = "trainEstimationRequest";
	const string scheduledPlansRequestMsgType = "scheduledPlanRequest";
	const string possessionsRequestMsgType = "possessionRequest";

	// External information responses
	
	// Channels
	static readonly Channel trainChannel = new(ChannelType.Topic, "jms.topic.rcs.e2k.ext.train");
	static readonly Channel estimationChannel = new(ChannelType.Topic, "jms.topic.rcs.e2k.ext.estimation");
	static readonly Channel schedulingChannel = new(ChannelType.Topic, "jms.topic.rcs.e2k.ext.scheduling");
	static readonly Channel restrictionChannel = new(ChannelType.Topic, "jms.topic.rcs.e2k.ext.restriction");

	// Subscriptions
	static readonly Subscription TrainPosSubscription = new(trainChannel, "trainPos");
	static readonly Subscription DeletedTrainSubscription = new(trainChannel, "deletedTrain");
	static readonly Subscription EstimationPlansSubscription = new(estimationChannel, "estimationPlans");
	static readonly Subscription DeletedEstimationPlansSubscription = new(estimationChannel, "deletedEstimationPlans");
	static readonly Subscription ScheduledPlansSubscription = new(schedulingChannel, "scheduledPlans");
	static readonly Subscription DeletedScheduledPlansSubscription = new(schedulingChannel, "deletedScheduledPlans");
	static readonly Subscription PossessionsSubscription = new(restrictionChannel, "possessions");
	static readonly Subscription DeletedPossessionsSubscription = new(restrictionChannel, "deletedPossessions");
	
	const string trainPositionSchema = "rcs.e2k.ext.train.position.V1";
	const string trainDeletedSchema = "rcs.e2k.ext.train.deleted.V1";
	const string estimationPlanSchema = "rcs.e2k.ext.estimation.plan.V1";
	const string estimationPlanDeletedSchema = "rcs.e2k.ext.estimation.plan.deleted.V1";
	const string schedulingPlanSchema = "rcs.e2k.ext.scheduling.plan.V1";
	const string schedulingPlanDeletedSchema = "rcs.e2k.ext.scheduling.plan.deleted.V1";
	const string possessionSchema = "rcs.e2k.ext.possession.V1";
	const string possessionDeletedSchema = "rcs.e2k.ext.possession.deleted.V1";

	string trainMovementProvider = "";
	string estimationPlansProvider = "";
	string scheduledPlansProvider = "";
	string possessionsProvider = "";

	////////////////////////////////////////////////////////////////////////////////

	public TimeDistanceGraphDataHandler(Connection connection, DataHandler dataHandler, string trainMovementProvider,
		string estimationPlansProvider, string scheduledPlansProvider, string possessionsProvider) : base(connection, dataHandler)
	{
		this.trainMovementProvider = trainMovementProvider;
		this.estimationPlansProvider = estimationPlansProvider;
		this.scheduledPlansProvider = scheduledPlansProvider;
		this.possessionsProvider = possessionsProvider;

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
				Connection.Subscribe(TrainPosSubscription, this.messageProcessor, OnTrainPosition, selector);
				Connection.Subscribe(DeletedTrainSubscription, this.messageProcessor, OnTrainDeleted, selector);
				Connection.Subscribe(EstimationPlansSubscription, this.messageProcessor, OnEstimationPlans, selector);
				Connection.Subscribe(DeletedEstimationPlansSubscription, this.messageProcessor, OnEstimationPlansDeleted, selector);
				Connection.Subscribe(ScheduledPlansSubscription, this.messageProcessor, OnScheduledPlans, selector);
				Connection.Subscribe(DeletedScheduledPlansSubscription, this.messageProcessor, OnScheduledPlansDeleted, selector);
				Connection.Subscribe(PossessionsSubscription, this.messageProcessor, OnPossessions, selector);
				Connection.Subscribe(DeletedPossessionsSubscription, this.messageProcessor, OnPossessionsDeleted, selector);
				break;

			case MessagingStateSubscription.MessagingInactive:
				Connection.Unsubscribe(TrainPosSubscription);
				Connection.Unsubscribe(DeletedTrainSubscription);
				Connection.Unsubscribe(EstimationPlansSubscription);
				Connection.Unsubscribe(DeletedEstimationPlansSubscription);
				Connection.Unsubscribe(ScheduledPlansSubscription);
				Connection.Unsubscribe(DeletedScheduledPlansSubscription);
				Connection.Unsubscribe(PossessionsSubscription);
				Connection.Unsubscribe(DeletedPossessionsSubscription);
				break;
		}
	}

	////////////////////////////////////////////////////////////////////////////////

	protected override void MessagingActivated()
	{
		HandleSubscriptions(MessagingStateSubscription.MessagingActive);

		// Request current information
		SendScheduledPlanRequest();
		SendEstimationPlanRequest();
		SendTrainPositionRequest();
		SendPossessionRequest();

		Log.Information("TimeDistanceGraphDataHandler: Message processing activated: accepting messages");
	}

	protected override void MessagingDeactivated()
	{
		HandleSubscriptions(MessagingStateSubscription.MessagingInactive);

		Log.Information("TimeDistanceGraphDataHandler: Message processing deactivated: rejecting messages");
	}

	////////////////////////////////////////////////////////////////////////////////

#pragma warning disable CS8602 // Dereference of a possibly null reference. try-catch in caller will handle missing mandatory fields in run-time

	private EdgeExtension GetEdgeExtension(XElement parentNode, string nodeName)
	{
		EdgeExtension extension = new();

		try
		{
			var extensionNode = parentNode.Element(nodeName);
			if (extensionNode != null)
			{
				EdgePosition startPos = GetEdgePosition(extensionNode, "start");
				EdgePosition endPos = GetEdgePosition(extensionNode, "end");

				var edgesNode = extensionNode.Element("edges");
				if (edgesNode != null)
				{
					List<string> edges = new();

					foreach (var child in edgesNode.Elements())
					{
						if (child.Name == "e")
						{
							edges.Add(child.Attribute("name").Value);
						}
					}

					extension = DataHandler.CreateEdgeExtension(startPos, endPos, edges);
				}
			}
		}
		catch (Exception)
		{
		}

		return extension;
	}

	private EdgePosition GetEdgePosition(XElement parentNode, string nodeName)
	{
		EdgePosition position = new();

		var positionNode = parentNode.Element(nodeName);
		if (positionNode != null)
		{
			string edge = positionNode.Attribute("edge").Value;
			uint offset = XmlConvert.ToUInt32(positionNode.Attribute("offset").Value);
			string vertex = positionNode.Attribute("vertex").Value;
			long additionalPos = XmlConvert.ToInt64(positionNode.Attribute("addpos").Value);
			string additionalName = GetOptionalAttributeValueOrEmpty(positionNode, "addname");

			position = DataHandler.CreateEdgePosition(edge, offset, vertex, additionalPos, additionalName);
		}

		return position;
	}

	private ActionTime GetActionTime(XElement parentNode, string nodeName, out bool occurred)
	{
		ActionTime time = new();
		occurred = false;
		var timeNode = parentNode.Element(nodeName);
		if (timeNode != null)
		{
			ActionTime nodeTime = new();
			if (nodeTime.InitFromISOString(timeNode.Value))
				time = nodeTime;
            string occurredStr = GetOptionalAttributeValueOrEmpty(timeNode, "occurred");
			occurred = occurredStr == "true";
        }

        return time;
	}

	private bool GetTimedLocations(XElement parentNode, out List<TimedLocation> timedLocations, int tripId=0, string tripName="")
	{
		timedLocations = new();

		var timeLocationsNode = parentNode.Element("timedLocations");
		if (timeLocationsNode != null)
		{
			foreach (var child in timeLocationsNode.Elements())
			{
				if (child.Name == "loc")
				{
					string description = GetOptionalAttributeValueOrEmpty(child, "description");

                    EdgePosition pos = GetEdgePosition(child, "pos");

					if (pos.IsValid())
					{
						ActionTime arrival = GetActionTime(child, "arrival", out bool occurredArrival);
						ActionTime departure = GetActionTime(child, "departure", out bool occurredDeparture);

						timedLocations.Add(DataHandler.CreateTimedLocation(description, pos, arrival, departure, occurredArrival, occurredDeparture, tripId, tripName));
					}
				}
			}
		}

		return timedLocations.Count > 0;
	}

	private static bool GetTrainIds(XElement trainNode, out string ctcid, out string obid, out string td, out string postfix, out string traintype)
	{
		ctcid = GetOptionalAttributeValueOrEmpty(trainNode, "ctcid");
		obid = GetOptionalAttributeValueOrEmpty(trainNode, "extid");	// Used as OBID in TDGS (it is OBID in CTC)!
        td = GetOptionalAttributeValueOrEmpty(trainNode, "td");
        postfix = GetOptionalAttributeValueOrEmpty(trainNode, "postfix");
		traintype = GetOptionalAttributeValueOrEmpty(trainNode, "traintype");

        // As of now, we have to have CTC ID (sysname) of train!
        // If still no ctcid in message, use OBID as ctcid, may not be correct, but there's nothing else
        if (ctcid == "")
			ctcid = obid;

		// If no OBID, use ctc ID as it. OBID is used as key in many collections and it must be unique
		if (obid == "")
            obid = ctcid;
        // If no train describer, use OBID as it
        if (td == "")
            td = obid;

        return ctcid != "" && obid != "";
	}

	////////////////////////////////////////////////////////////////////////////////

	private void OnTrainPosition(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties)
	{
		if (!AllowMessageProcessing)
			return;

		try
		{
			// Check if message is from configured provider
			if (this.trainMovementProvider != "")
			{
				var sender = hdr["sender"];
				if (sender != this.trainMovementProvider)
					return;
			}

			var schema = hdr["schema"];

			if (schema == trainPositionSchema)
			{
				//string senderDC = msg.Attribute("senderDC").Value;
				string refresh = "";
				var attr = msg.Attribute("refresh");
				if (attr != null)   // Normally missing
					refresh = attr.Value;

				bool refreshing = refresh != "";
				bool refreshEnds = refresh == "end";

				bool refreshRequestPending = DataHandler.IsTrainPositionsRequestPending();
				bool acceptMessage = !refreshRequestPending || refreshing;

				if (acceptMessage)
				{
					foreach (var child in msg.Elements())
					{
						if (child.Name == "train")
						{
							MovementHistoryItem movementHistoryItem = new();

							bool idOK = GetTrainIds(child, out string ctcid, out string obid, out string td, out string postfix, out string traintype);

							string timeStamp;

							var timeNode = child.Element("time");
							if (idOK && timeNode != null)
							{
								timeStamp = timeNode.Value;

                                EdgeExtension footprint = GetEdgeExtension(child, "footprint");

								if (footprint.IsValid())
								{
									ActionTime occurredTime = new();
									if (occurredTime.InitFromISOString(timeStamp))
									{
                                        Train? train = DataHandler.CreateTrain(obid, obid, ctcid, td, postfix, traintype);

										if (train != null)
										{
											movementHistoryItem = DataHandler.TrainPositionChanged(train, occurredTime, footprint);
										}
									}
								}
							}

							if (!movementHistoryItem.IsValid())
							{
								Log.Error("Train position not handled, train: td=" + td + " extid=" + obid + " ctcid=" + ctcid);
							}
						}
					}

					if (refreshRequestPending && refreshEnds)
					{
						DataHandler.SetTrainPositionsRequested(false);
						Log.Information("Train positions refresh data was received and handled");
					}
				}
				else
				{
					Log.Warning("Discarded train position message - waiting for refresh");
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

	private void OnTrainDeleted(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties)
	{
		if (!AllowMessageProcessing)
			return;

		// Check if message is from configured provider
		if (this.trainMovementProvider != "")
		{
			var sender = hdr["sender"];
			if (sender != this.trainMovementProvider)
				return;
		}
		try
		{
			var schema = hdr["schema"];

			if (schema == trainDeletedSchema)
			{
				//string senderDC = msg.Attribute("senderDC").Value;

				bool acceptMessage = !DataHandler.IsTrainPositionsRequestPending();

				if (acceptMessage)
				{
					var trainNode = msg.Element("train");
					if (trainNode != null)
					{
						MovementHistoryItem movementHistoryItem = new();

						bool idOK = GetTrainIds(trainNode, out string ctcid, out string obid, out string td, out string postfix, out string traintype);

						if (idOK)
                        {
							ActionTime occurredTime = ActionTime.Now;
							ActionTime time = new();

							var timeNode = trainNode.Element("time");
							if (timeNode != null && time.InitFromISOString(timeNode.Value))
								occurredTime = time;

							// Delete train (and possible estimation plan)
							Train? train = DataHandler.GetTrain(obid);
							if (train != null)
							{
								movementHistoryItem = DataHandler.TrainDeleted(train, occurredTime);
							}
						}

						if (!movementHistoryItem.IsValid())
						{
							Log.Warning("Train deletion not handled, train: td=" + td + " extid=" + obid + " ctcid=" + ctcid);
						}
					}
					else
					{
						Log.Error("Discarded train deletion message - no train node in message");
					}
				}
				else
				{
					Log.Warning("Discarded train deletion message - waiting for refresh");
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

	////////////////////////////////////////////////////////////////////////////////

	private void OnEstimationPlans(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties)
	{
		if (!AllowMessageProcessing)
			return;

		// Check if message is from configured provider
		if (this.estimationPlansProvider != "")
		{
			var sender = hdr["sender"];
			if (sender != this.estimationPlansProvider)
				return;
		}

		try
		{
			var schema = hdr["schema"];

			if (schema == estimationPlanSchema)
			{
				string refresh = "";
				var attr = msg.Attribute("refresh");
				if (attr != null)   // Normally missing
					refresh = attr.Value;

				bool refreshing = refresh != "";
				bool refreshEnds = refresh == "end";

				bool refreshRequestPending = DataHandler.IsEstimationPlansRequestPending();
				bool acceptMessage = !refreshRequestPending || refreshing;

				if (acceptMessage)
				{
					foreach (var child in msg.Elements())
					{
						if (child.Name == "train")
						{
							EstimationPlan estimationPlan = new();

							bool idOK = GetTrainIds(child, out string ctcid, out string obid, out string td, out string postfix, out string traintype);

							if (idOK)
							{
                                string tripIdStr = GetOptionalAttributeValueOrEmpty(child, "tripId");
								int tripId = 0;
								if (tripIdStr != "")
									tripId = int.Parse(tripIdStr);

                                List<TimedLocation> timedLocations;
								GetTimedLocations(child, out timedLocations);

								EdgeExtension trainPath = GetEdgeExtension(child, "trainPath");
                                
                                Train? train = DataHandler.CreateTrain(obid, obid, ctcid, td, postfix, traintype);

								if (train != null)
								{
									estimationPlan = DataHandler.EstimationPlanChanged(train, timedLocations, trainPath, tripId);
								}
							}

							if (!estimationPlan.IsValid())
							{
								Log.Warning("Estimation plan not handled, train: td = " + td + " extid = " + obid + " ctcid = " + ctcid);
							}
						}
                        else if (child.Name == "scheduledPlan")
						{
                            EstimationPlan estimationPlan = new();

                            string dayCodeStr = GetOptionalAttributeValueOrEmpty(child, "dayCode");
                            string id = GetOptionalAttributeValueOrEmpty(child, "id");
                            string name = GetOptionalAttributeValueOrEmpty(child, "name");
                            int dayCode = dayCodeStr == "" ? 0 : int.Parse(dayCodeStr);

                            bool idOK = id != "" && name != "";

							if (idOK)
							{
								List<TimedLocation> allTimedLocations = new();

								var tripsNode = child.Element("trips");
								if (tripsNode != null)
								{
									foreach (var tripNode in tripsNode.Elements())
									{
										if (tripNode.Name == "trip")
										{
											List<TimedLocation> timedLocations;

											string tripIdStr = GetOptionalAttributeValueOrEmpty(tripNode, "id");
											string tripName = GetOptionalAttributeValueOrEmpty(tripNode, "name");
                                            int tripId = tripIdStr == "" ? 0 : int.Parse(tripIdStr);

                                            GetTimedLocations(tripNode, out timedLocations, tripId, tripName);

											allTimedLocations.AddRange(timedLocations);
										}
									}
								}

								estimationPlan = DataHandler.EstimationPlanChanged(new ScheduledPlanKey(dayCode, id, name), allTimedLocations);

								if (!estimationPlan.IsValid())
								{
									Log.Warning("Estimation plan not handled, scheduled plan: dayCode = " + dayCode + " id = " + id + " name = " + name);
								}
							}
                        }

                    }

                    if (refreshRequestPending && refreshEnds)
					{
						DataHandler.SetEstimationPlansRequested(false);
						Log.Information("Estimation plans refresh data was received and handled");
					}
				}
				else
				{
					Log.Warning("Discarded estimation plan message - waiting for refresh");
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

	private void OnEstimationPlansDeleted(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties)
	{
		if (!AllowMessageProcessing)
			return;

		// Check if message is from configured provider
		if (this.estimationPlansProvider != "")
		{
			var sender = hdr["sender"];
			if (sender != this.estimationPlansProvider)
				return;
		}

		try
		{
			var schema = hdr["schema"];

			if (schema == estimationPlanDeletedSchema)
			{
				//string senderDC = msg.Attribute("senderDC").Value;

				bool acceptMessage = !DataHandler.IsEstimationPlansRequestPending();

				if (acceptMessage)
				{
					foreach (var child in msg.Elements())
					{
						if (child.Name == "train")
						{
							EstimationPlan estimationPlan = new();

							bool idOK = GetTrainIds(child, out string ctcid, out string obid, out string td, out _, out _);

							if (idOK)
							{
								Train? train = DataHandler.GetTrain(obid);
								if (train != null)
								{
									estimationPlan = DataHandler.EstimationPlanDeleted(train);
								}
							}

							if (!estimationPlan.IsValid())
							{
								Log.Warning("Estimation plan deleted not handled, train: td = " + td + " extid = " + obid + " ctcid = " + ctcid);
							}
						}
                        else if (child.Name == "scheduledPlan")
                        {
                            string id = GetOptionalAttributeValueOrEmpty(child, "id");
                            string name = GetOptionalAttributeValueOrEmpty(child, "name");
                            string dayCodeStr = GetOptionalAttributeValueOrEmpty(child, "dayCode");
                            string tripIdStr = GetOptionalAttributeValueOrEmpty(child, "tripId");
                            string tripName = GetOptionalAttributeValueOrEmpty(child, "tripName");
                            int dayCode = dayCodeStr == "" ? 0 : int.Parse(dayCodeStr);
                            int tripId = tripIdStr == "" ? 0 : int.Parse(tripIdStr);

                            // Now, first estimated plan trip deletion of scheduled plan will delete all trips in estimation plan. Others will return invalid estimation plan
							// This matches the logic in ConflictManagementService
                            EstimationPlan estimationPlan = DataHandler.EstimationPlanDeleted(new ScheduledPlanKey(dayCode, id, name));

                            if (!estimationPlan.IsValid())
                            {
                                //Log.Warning("Estimation plan deleted not handled, scheduled plan: dayCode = " + dayCode + " id = " + id + " name = " + name + " trip id = " + tripId + " trip name = " + tripName);
                            }
                        }
                    }
				}
				else
				{
					Log.Warning("Discarded estimation plan deletion message - waiting for refresh");
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

	////////////////////////////////////////////////////////////////////////////////

	private void OnScheduledPlans(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties)
	{
		if (!AllowMessageProcessing)
			return;

		// Check if message is from configured provider
		if (this.scheduledPlansProvider != "")
		{
			var sender = hdr["sender"];
			if (sender != this.scheduledPlansProvider)
				return;
		}

		try
		{
			var schema = hdr["schema"];

			if (schema == schedulingPlanSchema)
			{
				string refresh = "";
				var attr = msg.Attribute("refresh");
				if (attr != null)   // Normally missing
					refresh = attr.Value;

				bool refreshing = refresh != "";
				bool refreshEnds = refresh == "end";

				bool refreshRequestPending = DataHandler.IsScheduledPlansRequestPending();
				bool acceptMessage = !refreshRequestPending || refreshing;

				if (acceptMessage)
				{
					foreach (var child in msg.Elements())
					{
						if (child.Name == "plan")
						{
							ScheduledPlan scheduledPlan = new();

							string id = GetOptionalAttributeValueOrEmpty(child, "id");
							string name = GetOptionalAttributeValueOrEmpty(child, "name");
							string dayCodeStr = GetOptionalAttributeValueOrEmpty(child, "dayCode");
							string traintype = GetOptionalAttributeValueOrEmpty(child, "traintype");
							string description = GetOptionalAttributeValueOrEmpty(child, "description");
							string active = GetOptionalAttributeValueOrEmpty(child, "active");
                            string allocated = GetOptionalAttributeValueOrEmpty(child, "allocated");
                            int dayCode = dayCodeStr == "" ? 0 : int.Parse(dayCodeStr);
							bool activePlan = active != "false";    // Plan is considered inactive only if "active" -attribute exists and is set "false"
							bool allocatedPlan = allocated == "true";

							bool idOK = id != "" && name != "";

							if (idOK)
							{
								List<Trip> trips = new();
								int tripNumber = 1; // If trips do not have numbers in message, we'll generate them by ourselves

								var tripsNode = child.Element("trips");
								if (tripsNode != null)
								{
									foreach (var tripNode in tripsNode.Elements())
									{
										if (tripNode.Name == "trip")
										{
                                            string tripId = GetOptionalAttributeValueOrEmpty(tripNode, "id");
                                            string tripName = GetOptionalAttributeValueOrEmpty(tripNode, "name");
                                            string number = GetOptionalAttributeValueOrEmpty(tripNode, "number");
											if (number != "")
												tripNumber = int.Parse(number);
											bool activeTrip = GetOptionalAttributeValueOrEmpty(tripNode, "active") != "false";  // Trip is considered inactive only if "active" -attribute exists and is set "false"
                                            bool allocatedTrip = GetOptionalAttributeValueOrEmpty(tripNode, "allocated") == "true";

                                            string tripDescr = GetOptionalAttributeValueOrEmpty(tripNode, "description");

											EdgePosition startPos = GetEdgePosition(tripNode, "startpos");
											EdgePosition endPos = GetEdgePosition(tripNode, "endpos");

											ActionTime startTime = GetActionTime(tripNode, "starttime", out _);
											ActionTime endTime = GetActionTime(tripNode, "endtime", out _);

											List<TimedLocation> timedLocations;
											GetTimedLocations(tripNode, out timedLocations);

											trips.Add(DataHandler.CreateTrip(tripId, tripName, tripNumber, tripDescr, startPos, endPos, startTime, endTime, timedLocations, activeTrip, allocatedTrip));
                                        
											tripNumber++;
                                        }
                                    }
								}

								scheduledPlan = DataHandler.ScheduledPlanChanged(dayCode, id, name, traintype, description, trips, activePlan, allocatedPlan);
							}

							if (!scheduledPlan.IsValid())
							{
								Log.Warning("Scheduled plan not handled: id=" + id + " name=" + name + " description=" + description);
							}
						}
					}

					if (refreshRequestPending && refreshEnds)
					{
						DataHandler.SetScheduledPlansRequested(false);
						Log.Information("Scheduled plans refresh data was received and handled");
					}
				}
				else
				{
					Log.Warning("Discarded scheduling plan message - waiting for refresh");
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

	private void OnScheduledPlansDeleted(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties)
	{
		if (!AllowMessageProcessing)
			return;

		// Check if message is from configured provider
		if (this.scheduledPlansProvider != "")
		{
			var sender = hdr["sender"];
			if (sender != this.scheduledPlansProvider)
				return;
		}

		try
		{
			var schema = hdr["schema"];

			if (schema == schedulingPlanDeletedSchema)
			{
				//string senderDC = msg.Attribute("senderDC").Value;

				bool acceptMessage = !DataHandler.IsScheduledPlansRequestPending();

				if (acceptMessage)
				{
					foreach (var child in msg.Elements())
					{
						if (child.Name == "plan")
						{
							ScheduledPlan scheduledPlan = new();

							string id = GetOptionalAttributeValueOrEmpty(child, "id");
							string name = GetOptionalAttributeValueOrEmpty(child, "name");
                            string dayCodeStr = GetOptionalAttributeValueOrEmpty(child, "dayCode");
                            int dayCode = dayCodeStr == "" ? 0 : int.Parse(dayCodeStr);

                            if (id != "")
							{
								scheduledPlan = DataHandler.ScheduledPlanDeleted(new ScheduledPlanKey(dayCode, id, name));
							}

							if (!scheduledPlan.IsValid())
							{
								Log.Warning("Scheduled plan deletion not handled, plan: {0}, {1}, {2}", dayCode, id, name);
							}
						}
					}
				}
				else
				{
					Log.Warning("Discarded scheduled plan deletion message - waiting for refresh");
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

	////////////////////////////////////////////////////////////////////////////////

	private void OnPossessions(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties)
	{
		if (!AllowMessageProcessing)
			return;

		// Check if message is from configured provider
		if (this.possessionsProvider != "")
		{
			var sender = hdr["sender"];
			if (sender != this.possessionsProvider)
				return;
		}

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
								EdgePosition startPos = GetEdgePosition(child, "startpos");
								EdgePosition endPos = GetEdgePosition(child, "endpos");

								ActionTime startTime = GetActionTime(child, "starttime", out _);
								ActionTime endTime = GetActionTime(child, "endtime", out _);

								string state = GetOptionalElementValueOrEmpty(child, "state"); // This really is not optional!
								if (state == "")
								{
									//TODO: Old implementation for BHP, to be removed
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

	private void OnPossessionsDeleted(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties)
	{
		if (!AllowMessageProcessing)
			return;

		// Check if message is from configured provider
		if (this.possessionsProvider != "")
		{
			var sender = hdr["sender"];
			if (sender != this.possessionsProvider)
				return;
		}

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

	private void SendTrainPositionRequest()
	{
		Dictionary<string, string> hdr = new();
		Dictionary<string, string> msgProperties = new();

		var messageId = Connection.CreateNewMessageId();

        hdr["destination"] = trainInfoRequestChannel.ChannelName;
        hdr["sender"] = Connection.ServiceId;
		hdr["schema"] = trainPositionRequestSchema;

		msgProperties["rcsschema"] = trainPositionRequestSchema;
		msgProperties["rcsMessageId"] = messageId;
		msgProperties["rcsNode"] = this.Connection.RcsNode;

		try
		{
			XElement msgNode = new(trainPositionRequestMsgType);

			DataHandler.SetTrainPositionsRequested(true);

			Connection.SendMessage(trainInfoRequestChannel, this.messageProcessor.CreateMessage(hdr, msgNode, msgProperties));
		}
		catch (Exception e)
		{
			Log.Error("Internal error in XML message creation: {0}", e.ToString());
		}
	}

	private void SendEstimationPlanRequest()
	{
		Dictionary<string, string> hdr = new();
		Dictionary<string, string> msgProperties = new();

		var messageId = Connection.CreateNewMessageId();

        hdr["destination"] = estimationInfoRequestChannel.ChannelName;
        hdr["sender"] = Connection.ServiceId;
		hdr["schema"] = estimationPlansRequestSchema;

		msgProperties["rcsschema"] = estimationPlansRequestSchema;
		msgProperties["rcsMessageId"] = messageId;
		msgProperties["rcsNode"] = this.Connection.RcsNode;

		try
		{
			XElement msgNode = new(estimationPlansRequestMsgType);

			DataHandler.SetEstimationPlansRequested(true);

			Connection.SendMessage(estimationInfoRequestChannel, this.messageProcessor.CreateMessage(hdr, msgNode, msgProperties));
		}
		catch (Exception e)
		{
			Log.Error("Internal error in XML message creation: {0}", e.ToString());
		}
	}

	private void SendScheduledPlanRequest()
	{
		Dictionary<string, string> hdr = new();
		Dictionary<string, string> msgProperties = new();

		var messageId = Connection.CreateNewMessageId();

        hdr["destination"] = schedulingInfoRequestChannel.ChannelName;
        hdr["sender"] = Connection.ServiceId;
		hdr["schema"] = scheduledPlansRequestSchema;

		msgProperties["rcsschema"] = scheduledPlansRequestSchema;
		msgProperties["rcsMessageId"] = messageId;
		msgProperties["rcsNode"] = this.Connection.RcsNode;

		try
		{
			XElement msgNode = new(scheduledPlansRequestMsgType);

			DataHandler.SetScheduledPlansRequested(true);

			Connection.SendMessage(schedulingInfoRequestChannel, this.messageProcessor.CreateMessage(hdr, msgNode, msgProperties));
		}
		catch (Exception e)
		{
			Log.Error("Internal error in XML message creation: {0}", e.ToString());
		}
	}

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
