namespace E2KService.MessageHandler;

using System;
using System.Xml.Linq;
using E2KService.ActiveMQ;
using ConflictManagementService.Model;
using Serilog;
using Apache.NMS;
using System.Collections.Generic;
using System.Linq;

internal class TimeDistanceGraphDataHandler : ActiveStateMessageHandler
{
    readonly ActiveMQ.AMQP.Rcs2kXmlMessageProcessor messageProcessor = new("Time-Distance Graph Data Message Processor");

    // Time-distance graph service requests

    // Channels
    static readonly Channel trainInfoRequestChannel = new(ChannelType.Queue, "jms.queue.rcs.e2k.ext.train.request");
    static readonly Channel estimationInfoRequestChannel = new(ChannelType.Queue, "jms.queue.rcs.e2k.ext.estimation.request");
    static readonly Channel schedulingInfoRequestChannel = new(ChannelType.Queue, "jms.queue.rcs.e2k.ext.scheduling.request");

    // Schemas
    const string trainPositionsRequestSchema = "rcs.e2k.ext.train.request.position.V1";
    const string estimationPlansRequestSchema = "rcs.e2k.ext.estimation.request.plan.V1";
    const string scheduledPlansRequestSchema = "rcs.e2k.ext.scheduling.request.plan.V1";

    // Messages
    const string trainPositionsRequestMsgType = "trainPositionRequest";
    const string estimationPlansRequestMsgType = "trainEstimationRequest";
    const string scheduledPlansRequestMsgType = "scheduledPlanRequest";

    // Subscriptions
    static readonly Subscription TrainPositionRequestSubscription = new(trainInfoRequestChannel, trainPositionsRequestMsgType);
    static readonly Subscription EstimationPlansRequestSubscription = new(estimationInfoRequestChannel, estimationPlansRequestMsgType);
    static readonly Subscription ScheduledPlanRequestSubscription = new(schedulingInfoRequestChannel, scheduledPlansRequestMsgType);

    // Time-distance graph service indications

    // Channels
    static readonly Channel trainChannel = new(ChannelType.Topic, "jms.topic.rcs.e2k.ext.train");
    static readonly Channel estimationChannel = new(ChannelType.Topic, "jms.topic.rcs.e2k.ext.estimation");
    static readonly Channel schedulingChannel = new(ChannelType.Topic, "jms.topic.rcs.e2k.ext.scheduling");

    // Schemas  
    const string trainPositionSchema = "rcs.e2k.ext.train.position.V1";
    const string trainDeletedSchema = "rcs.e2k.ext.train.deleted.V1";
    const string estimationPlanSchema = "rcs.e2k.ext.estimation.plan.V1";
    const string estimationPlanDeletedSchema = "rcs.e2k.ext.estimation.plan.deleted.V1";
    const string schedulingPlanSchema = "rcs.e2k.ext.scheduling.plan.V1";
    const string schedulingPlanDeletedSchema = "rcs.e2k.ext.scheduling.plan.deleted.V1";

    // Messages
    const string trainPosMsgType = "trainPos";
    const string deletedTrainMsgType = "deletedTrain";
    const string estimationPlansMsgType = "estimationPlans";
    const string deletedEstimationPlansMsgType = "deletedEstimationPlans";
    const string scheduledPlansMsgType = "scheduledPlans";
    const string deletedScheduledPlansMsgType = "deletedScheduledPlans";

    private bool trainPositionsRequested = false;
    private bool estimationPlansRequested = false;
    private bool scheduledPlansRequested = false;

    // TODO : This is not a good idea, if there are more than one client requesting refresh simultaneously!
    // But now we only have TimeDistanceGraphService, so we will do this simple way...
    private string trainposRequestCorrelationId = "";
    private string estimationPlansRequestCorrelationId = "";
    private string scheduledPlansRequestCorrelationId = "";

    ////////////////////////////////////////////////////////////////////////////////

    public TimeDistanceGraphDataHandler(Connection connection, DataHandler dataHandler) : base(connection, dataHandler)
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
                string? selector = Connection.RcsNodeSelector;
                Connection.Subscribe(TrainPositionRequestSubscription, messageProcessor, OnTrainPositionsRequest, selector);
                Connection.Subscribe(EstimationPlansRequestSubscription, messageProcessor, OnEstimationPlansRequest, selector);
                Connection.Subscribe(ScheduledPlanRequestSubscription, messageProcessor, OnScheduledPlansRequest, selector);
                break;

            case MessagingStateSubscription.MessagingInactive:
                Connection.Unsubscribe(TrainPositionRequestSubscription);
                Connection.Unsubscribe(EstimationPlansRequestSubscription);
                Connection.Unsubscribe(ScheduledPlanRequestSubscription);
                break;
        }
    }

    ////////////////////////////////////////////////////////////////////////////////

    protected override void MessagingActivated()
    {
        HandleSubscriptions(MessagingStateSubscription.MessagingActive);
        Log.Information("TimeDistanceGraphDataHandler: Message processing activated: accepting messages");

        // Add callbacks for DataHandler's model change notifications
        DataHandler.NotifyTrainPositionChanged += SendTrainPosition;
        DataHandler.NotifyTrainDeleted += SendDeleteTrain;
        DataHandler.NotifyEstimationPlanChanged += SendEstimationPlan;
        DataHandler.NotifyEstimationPlanDeleted += SendDeleteEstimationPlan;
        DataHandler.NotifyScheduledPlanChanged += SendScheduledPlan;
        DataHandler.NotifyScheduledPlanDeleted += SendDeleteScheduledPlan;
    }

    protected override void MessagingDeactivated()
    {
        HandleSubscriptions(MessagingStateSubscription.MessagingInactive);
        Log.Information("TimeDistanceGraphDataHandler: Message processing deactivated: rejecting messages");

        // Remove callbacks for DataHandler's model change notifications
        DataHandler.NotifyTrainPositionChanged -= SendTrainPosition;
        DataHandler.NotifyTrainDeleted -= SendDeleteTrain;
        DataHandler.NotifyEstimationPlanChanged -= SendEstimationPlan;
        DataHandler.NotifyEstimationPlanDeleted -= SendDeleteEstimationPlan;
        DataHandler.NotifyScheduledPlanChanged -= SendScheduledPlan;
        DataHandler.NotifyScheduledPlanDeleted -= SendDeleteScheduledPlan;
    }

    ////////////////////////////////////////////////////////////////////////////////

#pragma warning disable CS8602 // Dereference of a possibly null reference. try-catch in caller will handle missing mandatory fields in run-time

    private void OnTrainPositionsRequest(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        if (!AllowMessageProcessing || trainPositionsRequested)
            return;

        try
        {
            var schema = msgProperties["rcsschema"];

            if (schema == trainPositionsRequestSchema)
            {
                trainposRequestCorrelationId = msgProperties["rcsMessageId"];

                if (DataHandler.IsTrainPositionsRequestPending())
                {
                    // Add callback for refresh request ended
                    DataHandler.NotifyTrainPositionsRefreshRequestEnded += HandlePendingTrainPositionsRequest;
                    trainPositionsRequested = true;
                }
                else
                    SendTrainPositions();
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

    private void OnEstimationPlansRequest(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        if (!AllowMessageProcessing || estimationPlansRequested)
            return;

        try
        {
            var schema = msgProperties["rcsschema"];

            if (schema == estimationPlansRequestSchema)
            {
                estimationPlansRequestCorrelationId = msgProperties["rcsMessageId"];
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

    private void OnScheduledPlansRequest(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        if (!AllowMessageProcessing || scheduledPlansRequested)
            return;

        try
        {
            var schema = msgProperties["rcsschema"];

            if (schema == scheduledPlansRequestSchema)
            {
                scheduledPlansRequestCorrelationId = msgProperties["rcsMessageId"];

                if (DataHandler.IsScheduledPlansRequestPending())
                {
                    // Add callback for refresh request ended
                    DataHandler.NotifyScheduledPlansRefreshRequestEnded += HandlePendingScheduledPlansRequest;
                    scheduledPlansRequested = true;
                }
                else
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

    ////////////////////////////////////////////////////////////////////////////////

    public static XElement CreateElementPosition(string posNodeName, ElementPosition pos)
    {
        var edgePos = new XElement(posNodeName,
                                   new XAttribute("edge", pos.ElementId),
                                   new XAttribute("offset", pos.Offset),
                                   new XAttribute("vertex", ""),        // TODO
                                   new XAttribute("addpos", pos.AdditionalPos));
        if (pos.AdditionalName != "")
            edgePos.Add(new XAttribute("addname", pos.AdditionalName));

        return edgePos;
    }

    public static XElement CreateElementExtension(string extensionNodeName, ElementExtension? ext)
    {
        if (ext != null)
        {
            var extNode = new XElement(extensionNodeName,
                                       CreateElementPosition("start", ext.StartPos),
                                       CreateElementPosition("end", ext.EndPos));
            var edgesNode = new XElement("edges");
            foreach (var element in ext.Elements)
            {
                edgesNode.Add(new XElement("e", new XAttribute("name", element)));
            }
            extNode.Add(edgesNode);
            return extNode;
        }
        else
            return new XElement(extensionNodeName);
    }

    private static XElement CreateTimedLocations(List<TimedLocation> timedLocations, bool addOccurredAttributes = false, bool addTrips = false)
    {
        XElement masterElement = addTrips ? new XElement("trips") : new XElement("timedLocations");

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
                            tripElement.Add(timedLocationsElement);
                            masterElement.Add(tripElement);
                        }
                        tripElement = new("trip", new XAttribute("id", timedLocation.TripId), new XAttribute("name", timedLocation.TripName));
                        timedLocationsElement = new XElement("timedLocations");
                        tripId = timedLocation.TripId;
                    }
                }

                var loc = new XElement("loc");
                if (timedLocation.Description != "")
                    loc.Add(new XAttribute("description", timedLocation.Description));

                loc.Add(CreateElementPosition("pos", timedLocation.Pos));

                if (timedLocation.Arrival.IsValid())
                {
                    var arrival = new XElement("arrival", timedLocation.Arrival.ToString());
                    if (addOccurredAttributes)
                        arrival.Add(new XAttribute("occurred", timedLocation.ArrivalOccurred));
                    loc.Add(arrival);
                }
                if (timedLocation.Departure.IsValid())
                {
                    var departure = new XElement("departure", timedLocation.Departure.ToString());
                    if (addOccurredAttributes)
                        departure.Add(new XAttribute("occurred", timedLocation.DepartureOccurred));
                    loc.Add(departure);
                }
                timedLocationsElement.Add(loc);
            }

            if (addTrips && tripId != -1)
            {
                // Last trip
                tripElement.Add(timedLocationsElement);
                masterElement.Add(tripElement);
            }
        }
        catch (Exception) { }

        return masterElement;
    }

    private static XElement CreateTrainElement(Train? train)
    {
        var trainNode = new XElement("train");

        if (train != null)
        {
            if (train.CtcId != "")
                trainNode.Add(new XAttribute("ctcid", train.CtcId));
            if (train.Obid != "")
                trainNode.Add(new XAttribute("extid", train.Obid));
            if (train.Td != "")
                trainNode.Add(new XAttribute("td", train.Td));
            if (train.Postfix != "")
                trainNode.Add(new XAttribute("postfix", train.Postfix));
            if (train.TrainType != "")
                trainNode.Add(new XAttribute("traintype", train.TrainType));
        }

        return trainNode;
    }

    private string GenerateBasicHeaders(string destination, string schema, string requestCorrelationId, string requestCorrelationNode, out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties)
    {
        hdr = new();
        msgProperties = new();

        var messageId = Connection.CreateNewMessageId();

        hdr["destination"] = destination;
        hdr["sender"] = Connection.ServiceId;
        hdr["schema"] = schema;
        hdr["utc"] = ActionTime.Now.ToString();

        msgProperties["rcsschema"] = schema;
        msgProperties["rcsMessageId"] = messageId;
        msgProperties["rcsSender"] = Connection.ServiceId;
        msgProperties["rcsDestination"] = destination;
        msgProperties["rcsReplyToDest"] = "";
        msgProperties["_AMQ_LVQ_NAME"] = schema;
        msgProperties["rcsNode"] = Connection.RcsNode;
        if (requestCorrelationId != "")
        {
            msgProperties[E2KService.ActiveMQ.AMQP.MessageProcessor.PropertyCorrelationId] = requestCorrelationId;
            msgProperties["rcsCorrelationId"] = requestCorrelationId;       // This should not be needed, but...
        }
        if (requestCorrelationNode != "")
            msgProperties["rcsCorrelationNode"] = requestCorrelationNode;

        return messageId;
    }

    ////////////////////////////////////////////////////////////////////////////////

    private void SendTrainPosition(Train? train, TrainPosition trainPosition)
    {
        // Do not send send sequence train positions (TODO: should this be configurable?)
        if (AllowMessageProcessing && train != null && train.IsValid() && trainPosition.IsValid() && train.CtcType == Train.CtcTrainType.Train)
        {
            GenerateBasicHeaders(trainChannel.ChannelName, trainPositionSchema, "", "", out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties);

            try
            {
                XElement trainNode = CreateTrainElement(trainPosition.Train);
                trainNode.Add(new XElement("time", trainPosition.OccurredTime.ToString()),
                    CreateElementExtension("footprint", trainPosition.ElementExtension));

                XElement msgNode = new(trainPosMsgType, trainNode);

                Connection.SendMessage(trainChannel, messageProcessor.CreateMessage(hdr, msgNode, msgProperties));
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

        GenerateBasicHeaders(trainChannel.ChannelName, trainPositionSchema, trainposRequestCorrelationId, "", out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties);

        try
        {
            XElement msgNode = new(trainPosMsgType, new XAttribute("refresh", "end"));  // All train positions sent in one message

            foreach (var trainPosition in DataHandler.TrainPositions.Values)
            {
                if (trainPosition.IsValid() && trainPosition.Train.CtcType == Train.CtcTrainType.Train)
                {
                    try
                    {
                        XElement trainNode = CreateTrainElement(trainPosition.Train);
                        trainNode.Add(new XElement("time", trainPosition.OccurredTime.ToString()));
                        trainNode.Add(CreateElementExtension("footprint", trainPosition.ElementExtension));

                        msgNode.Add(trainNode);
                    }
                    catch (Exception ex)
                    {
                        // Catch only first error
                        if (errorEncountered == null)
                            errorEncountered = ex.Message;
                    }
                }
            }

            Connection.SendMessage(trainChannel, messageProcessor.CreateMessage(hdr, msgNode, msgProperties));
        }
        catch (Exception ex)
        {
            Log.Error("Internal error in XML message creation: {0}", ex.ToString());
        }

        if (errorEncountered != null)
            Log.Error("Internal error happened in XML message creation, some train positions were discarded: {0}", errorEncountered);

        trainposRequestCorrelationId = "";
    }

    private void SendDeleteTrain(Train train, ActionTime occurredTime)
    {
        if (!AllowMessageProcessing)
            return;

        if (train.IsValid())
        {
            GenerateBasicHeaders(trainChannel.ChannelName, trainDeletedSchema, "", "", out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties);

            if (!occurredTime.IsValid())
                occurredTime = ActionTime.Now;

            try
            {
                XElement trainNode = CreateTrainElement(train);
                trainNode.Add(new XElement("time", occurredTime.ToString()));

                XElement msgNode = new(deletedTrainMsgType, trainNode);

                Connection.SendMessage(trainChannel, messageProcessor.CreateMessage(hdr, msgNode, msgProperties));
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
        SendEstimationPlan(estimationPlan, "update");
    }
    private void SendEstimationPlan(EstimationPlan estimationPlan, string refresh)
    {
        if (!AllowMessageProcessing)
            return;

        if (estimationPlan.IsValid())
        {
            GenerateBasicHeaders(estimationChannel.ChannelName, estimationPlanSchema, estimationPlansRequestCorrelationId, "", out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties);

            try
            {
                XElement msgNode = new(estimationPlansMsgType, new XAttribute("refresh", refresh));

                if (estimationPlan.IsTrainEstimationPlan())
                {
                    // Send estimation plan for allocated train (occurred events added to message)
                    var AddEdges = () =>
                    {
                        var edges = new XElement("edges");
                        foreach (var edgeName in estimationPlan.TrainPath.Elements)
                        {
                            var edge = new XElement("e", new XAttribute("name", edgeName));
                            edges.Add(edge);
                        }
                        return edges;
                    };

                    string ctcId = "";
                    string guid = "";
                    uint sysid = 0;
                    string postfix = "";
                    string traintype = "";

                    Train? train = DataHandler.GetTrain(estimationPlan);
                    if (train != null)
                    {
                        ctcId = train.CtcId;
                        guid = train.Guid;
                        sysid = train.Sysid;
                        postfix = train.Postfix;
                        traintype = train.TrainType;
                    }

                    int tripId = 0;
                    var scheduledPlan = DataHandler.GetScheduledPlan(estimationPlan.ScheduledPlanKey);
                    if (train != null && scheduledPlan != null)
                        tripId = DataHandler.GetTripIdOfAllocatedTimetable(train, scheduledPlan);

                    XElement trainNode = CreateTrainElement(new Train(estimationPlan.Obid, guid, ctcId, estimationPlan.Td, sysid, Train.CtcTrainType.Train) { Postfix = postfix, TrainType = traintype });
                    if (tripId != 0)
                        trainNode.Add(new XAttribute("tripId", tripId));
                    XElement timedLocationsNode = CreateTimedLocations(estimationPlan.TimedLocations, true);
                    XElement trainPathNode = new XElement("trainPath",
                                                    CreateElementPosition("start", estimationPlan.TrainPath.StartPos),
                                                    CreateElementPosition("end", estimationPlan.TrainPath.EndPos),
                                                    AddEdges());
                    trainNode.Add(timedLocationsNode);
                    trainNode.Add(trainPathNode);
                    msgNode.Add(trainNode);
                }
                else
                {
                    // Send estimation plan prepared for scheduled plan trip(s) (occurred events are not added to message, but trips are)
                    var scheduledPlan = DataHandler.GetScheduledPlan(estimationPlan.ScheduledPlanKey);
                    if (scheduledPlan != null)
                    {
                        XElement scheduledPlanNode = new("scheduledPlan", new XAttribute("dayCode", scheduledPlan.ScheduledDayCode), new XAttribute("id", scheduledPlan.Id), new XAttribute("name", scheduledPlan.Name));

                        XElement tripsNode = CreateTimedLocations(estimationPlan.TimedLocations, addTrips: true);
                        scheduledPlanNode.Add(tripsNode);
                        msgNode.Add(scheduledPlanNode);
                    }
                    else
                        throw new Exception("Scheduled plan not found from estimation plan");
                }

                Connection.SendMessage(estimationChannel, messageProcessor.CreateMessage(hdr, msgNode, msgProperties));
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

        var allEstimationPlans = DataHandler.TrainEstimationPlans.Values.ToList();
        allEstimationPlans.AddRange(DataHandler.EstimationPlans.Values.ToList());

        var amountToSend = allEstimationPlans.Count;
        foreach (var estimationPlan in allEstimationPlans)
        {
            string refresh;
            if (amountToSend == allEstimationPlans.Count && amountToSend > 1)
                refresh = "start";
            else if (amountToSend == 1)
                refresh = "end";
            else
                refresh = "update";

            amountToSend--;

            SendEstimationPlan(estimationPlan, refresh);
        }

        estimationPlansRequestCorrelationId = "";
    }

    private void SendDeleteEstimationPlan(EstimationPlan estimationPlan)
    {
        if (AllowMessageProcessing && estimationPlan.IsValid())
        {
            GenerateBasicHeaders(estimationChannel.ChannelName, estimationPlanDeletedSchema, "", "", out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties);

            try
            {
                XElement msgNode = new(deletedEstimationPlansMsgType);

                if (estimationPlan.IsTrainEstimationPlan())
                {
                    // Delete estimation plan of timetable allocated train
                    Train? train = DataHandler.GetTrain(estimationPlan);
                    if (train == null)
                        train = new Train(estimationPlan.Obid, "", estimationPlan.Obid, estimationPlan.Td, 0, Train.CtcTrainType.Train);

                    msgNode.Add(CreateTrainElement(train));
                }
                else
                {
                    // Delete estimation plan of scheduled plan (all trips)
                    var scheduledPlan = DataHandler.GetScheduledPlan(estimationPlan.ScheduledPlanKey);
                    if (scheduledPlan != null)
                    {
                        foreach (var trip in scheduledPlan.Trips.Values)
                        {
                            XElement scheduledPlanNode = new("scheduledPlan",
                                new XAttribute("dayCode", scheduledPlan.ScheduledDayCode),
                                new XAttribute("id", scheduledPlan.Id),
                                new XAttribute("name", scheduledPlan.Name),
                                new XAttribute("tripId", trip.Id),
                                new XAttribute("tripName", trip.TripCode)
                                );
                            msgNode.Add(scheduledPlanNode);
                        }
                    }
                    else
                        throw new Exception("Scheduled plan not found from estimation plan");
                }

                Connection.SendMessage(estimationChannel, messageProcessor.CreateMessage(hdr, msgNode, msgProperties));
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
        SendScheduledPlan(scheduledPlan, "update");
    }
    private void SendScheduledPlan(ScheduledPlan scheduledPlan, string refresh)
    {
        if (!AllowMessageProcessing)
            return;

        if (scheduledPlan.IsValid())
        {
            GenerateBasicHeaders(schedulingChannel.ChannelName, schedulingPlanSchema, scheduledPlansRequestCorrelationId, "", out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties);

            try
            {
                var AddTrips = () =>
                {
                    var trips = new XElement("trips");
                    foreach (var trip in scheduledPlan.Trips.Values)
                    {
                        var tripElement = new XElement("trip",
                                                       new XAttribute("id", trip.Id),
                                                       new XAttribute("number", trip.TripNumber),
                                                       new XAttribute("active", !trip.IsSpareTrip),
                                                       new XAttribute("allocated", DataHandler.IsAllocated(scheduledPlan.Key, trip.Id)),
                                                       new XAttribute("description", trip.TripCode == null ? "" : trip.TripCode),
                                                       CreateElementPosition("startpos", trip.StartPos),
                                                       CreateElementPosition("endpos", trip.EndPos));
                        if (trip.StartTime.IsValid())
                            tripElement.Add(new XElement("starttime", trip.StartTime.ToString()));
                        if (trip.EndTime.IsValid())
                            tripElement.Add(new XElement("endtime", trip.EndTime.ToString()));
                        tripElement.Add(CreateTimedLocations(trip.TimedLocations));
                        trips.Add(tripElement);
                    }
                    return trips;
                };

                XElement msgNode = new(scheduledPlansMsgType, new XAttribute("refresh", refresh),
                                       new XElement("plan",
                                       new XAttribute("id", scheduledPlan.Id),
                                       new XAttribute("name", scheduledPlan.Name),
                                       new XAttribute("dayCode", scheduledPlan.ScheduledDayCode),
                                       new XAttribute("traintype", DataHandler.GetTrainType(scheduledPlan.TrainTypeId)),
                                       new XAttribute("description", string.Format($"{scheduledPlan.StartSite.AdditionalName}-{scheduledPlan.EndSite.AdditionalName}")),
                                       new XAttribute("active", !scheduledPlan.IsSparePlan),
                                       new XAttribute("allocated", DataHandler.IsAllocated(scheduledPlan.Key)),
                                       AddTrips()));

                Connection.SendMessage(schedulingChannel, messageProcessor.CreateMessage(hdr, msgNode, msgProperties));
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

        var amountToSend = DataHandler.ScheduledPlans.Count;
        foreach (var scheduledPlan in DataHandler.ScheduledPlans.Values)
        {
            string refresh;
            if (amountToSend == DataHandler.ScheduledPlans.Count && amountToSend > 1)
                refresh = "start";
            else if (amountToSend == 1)
                refresh = "end";
            else
                refresh = "update";

            amountToSend--;

            SendScheduledPlan(scheduledPlan, refresh);
        }

        scheduledPlansRequestCorrelationId = "";
    }

    private void SendDeleteScheduledPlan(ScheduledPlan scheduledPlan)
    {
        if (AllowMessageProcessing && scheduledPlan.IsValid())
        {
            GenerateBasicHeaders(schedulingChannel.ChannelName, schedulingPlanDeletedSchema, "", "", out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties);

            try
            {
                XElement msgNode = new(deletedScheduledPlansMsgType,
                                       new XElement("plan",
                                       new XAttribute("id", scheduledPlan.Id),
                                       new XAttribute("name", scheduledPlan.Name),
                                       new XAttribute("dayCode", scheduledPlan.ScheduledDayCode),
                                       new XAttribute("state", !scheduledPlan.IsSparePlan)));

                Connection.SendMessage(schedulingChannel, messageProcessor.CreateMessage(hdr, msgNode, msgProperties));
            }
            catch (Exception e)
            {
                Log.Error("Internal error in XML message creation: {0}", e.ToString());
            }
        }
    }

#pragma warning restore CS8602 // Dereference of a possibly null reference.

    ////////////////////////////////////////////////////////////////////////////////

    private void HandlePendingTrainPositionsRequest()
    {
        Log.Information("Train positions data refresh request ended, sending all train positions to clients");

        // Remove callback for refresh request ended
        DataHandler.NotifyTrainPositionsRefreshRequestEnded -= HandlePendingTrainPositionsRequest;
        trainPositionsRequested = false;

        SendTrainPositions();
    }

    private void HandlePendingEstimationPlansRequest()
    {
        Log.Information("Estimation plans data refresh request ended, sending all estimation plans to clients");

        // Remove callback for refresh request ended
        DataHandler.NotifyEstimationPlansRefreshRequestEnded -= HandlePendingEstimationPlansRequest;
        estimationPlansRequested = false;

        SendEstimationPlans();
    }

    private void HandlePendingScheduledPlansRequest()
    {
        Log.Information("Scheduled plans data refresh request ended, sending all scheduled plans to clients");

        // Remove callback for refresh request ended
        DataHandler.NotifyScheduledPlansRefreshRequestEnded -= HandlePendingScheduledPlansRequest;
        scheduledPlansRequested = false;

        SendScheduledPlans();
    }

}
