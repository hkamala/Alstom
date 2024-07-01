using System.IO;
using ConflictManagementLibrary.Helpers;
using Newtonsoft.Json;

namespace E2KService.MessageHandler;

using System;
using System.Xml.Linq;
using System.Xml;
using System.Text;
using E2KService.ActiveMQ;
using ConflictManagementService.Model;
using Serilog;
using Apache.NMS;
using static ServiceImp;
using TrainData;
using Google.Protobuf;
using IMessage = Apache.NMS.IMessage;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;

internal class TrainInformationAndCommandHandler : ActiveStateMessageHandler
{
    readonly ActiveMQ.AMQP.Rcs5kXmlMessageProcessor messageProcessor = new("Train Information Message Processor");

    const string disNameSpace = "dis";
    const string distrainNameSpace = "distrain";

    // Train indications
    // Channels
    static readonly Channel trainInformationChannel = new(ChannelType.Topic, "jms.topic.dis.ebi.ctc.train.indication"); // Default train information indication topic

    // Schemas
    const string c_trainDataSchema = "trainData";
    const string c_trainDataSchemaWithNs = distrainNameSpace + ":" + c_trainDataSchema;
    const string c_trainCommandStartingSchema = "trainCommandStarting";
    const string c_trainCommandStartingSchemaWithNs = distrainNameSpace + ":" + c_trainCommandStartingSchema;
    const string c_trainCommandEndedSchema = "trainCommandEnded";
    const string c_trainCommandEndedSchemaWithNs = distrainNameSpace + ":" + c_trainCommandEndedSchema;

    // Messages
    const string c_trainDataMsgType = c_trainDataSchema;
    const string c_trainCommandStartingMsgType = c_trainCommandStartingSchema;
    const string c_trainCommandEndedMsgType = c_trainCommandEndedSchema;

    // Subscriptions
    static readonly Subscription TrainDataSubscription = new(trainInformationChannel, distrainNameSpace + ":" + c_trainDataMsgType);
    static readonly Subscription TrainCommandStartingSubscription = new(trainInformationChannel, distrainNameSpace + ":" + c_trainCommandStartingMsgType);
    static readonly Subscription TrainCommandEndedSubscription = new(trainInformationChannel, distrainNameSpace + ":" + c_trainCommandEndedMsgType);

    // Train commands and data requests
    // Channels
    static readonly Channel trainInformationCommandChannel = new(ChannelType.Queue, "jms.queue.dis.ebi.ctc.train.command"); // Train information command queue

    // Schemas
    const string c_trainRefreshRequestSchema = "trainRefreshRequest";
    const string c_trainRefreshRequestSchemaWithNs = distrainNameSpace + ":" + c_trainRefreshRequestSchema;
    const string c_trainPropertiesSetSchema = "trainPropertiesSet";
    const string c_trainPropertiesSetSchemaWithNs = distrainNameSpace + ":" + c_trainPropertiesSetSchema;
    const string c_trainSendEventSchema = "trainSendEvent";
    const string c_trainSendEventSchemaWithNs = distrainNameSpace + ":" + c_trainSendEventSchema;

    // Messages (namespace is added in the message sending method)
    const string c_trainRefreshRequestMsgType = c_trainRefreshRequestSchema;
    const string c_trainPropertiesSetMsgType = c_trainPropertiesSetSchema;
    const string c_trainSendEventMsgType = c_trainSendEventSchema;

    private readonly Dictionary<TrainData.EUpdateMode, string> UpdateMode = new()
    {
        { TrainData.EUpdateMode.StaticFirst, "StaticFirst" },
        { TrainData.EUpdateMode.Static, "Static" },
        { TrainData.EUpdateMode.StaticLast, "StaticLast" },
        { TrainData.EUpdateMode.Dynamic, "Dynamic" }
    };


    // Event report TODO: should be removed, because now we have event sending for train?
    const string disERNameSpace = "disER";

    // Channels
    static readonly Channel eventReportChannel = new(ChannelType.Queue, "jms.queue.dis.ebi.ctc.eventreport");
    
    // Schemas
    const string c_eventReportSchema = "eventReport";
    const string c_eventReportSchemaWithNs = disERNameSpace + ":" + c_eventReportSchema;

    // Messages
    const string c_eventReportMsgType = c_eventReportSchema;


    // Timetable info
    const string timetableNameSpace = "timetable";

    // Channels
    static readonly Channel timetableInfoChannel = new(ChannelType.Topic, "jms.topic.ctc.timetable.info");

    // Schemas
    const string c_timetableInfoSchema = "serviceinfo";
    const string c_timetableInfoSchemaWithNs = timetableNameSpace + ":" + c_timetableInfoSchema;

    // Messages
    const string c_timetableInfoMsgType = c_timetableInfoSchema;

    private string requestMessageId = "";

    ////////////////////////////////////////////////////////////////////////////////

    public TrainInformationAndCommandHandler(Connection connection, DataHandler dataHandler) : base(connection, dataHandler)
    {
        HandleSubscriptions(MessagingStateSubscription.Always);

        // Always send timetable info to listeners (online and standby)
        DataHandler.NotifyTimetableInfoChanged += SendTimetableInfo;
    }

    private void HandleSubscriptions(MessagingStateSubscription state)
    {
        switch (state)
        {
            case MessagingStateSubscription.Always:
                Connection.Subscribe(TrainDataSubscription, messageProcessor, OnTrainDataChanged);  // Need to have trains also in standby server
                break;

            case MessagingStateSubscription.MessagingActive:
                Connection.Subscribe(TrainCommandStartingSubscription, messageProcessor, OnTrainCommandStarting);
                Connection.Subscribe(TrainCommandEndedSubscription, messageProcessor, OnTrainCommandEnded);
                break;

            case MessagingStateSubscription.MessagingInactive:
                Connection.Unsubscribe(TrainCommandStartingSubscription);
                Connection.Unsubscribe(TrainCommandEndedSubscription);
                break;
        }
    }
    ////////////////////////////////////////////////////////////////////////////////

    protected override void MessagingActivated()
    {
        HandleSubscriptions(MessagingStateSubscription.MessagingActive);
        Log.Information("TrainInformationAndCommandHandler: Message processing activated: accepting messages");

        DataHandler.NotifyTrainPropertySet += SendTrainProperty;
        DataHandler.NotifyTrainPropertiesSet += SendTrainProperties;

        SendTrainDataRequest();
    }

    protected override void MessagingDeactivated()
    {
        HandleSubscriptions(MessagingStateSubscription.MessagingInactive);
        Log.Information("TrainInformationAndCommandHandler: Message processing deactivated: rejecting messages");

        DataHandler.NotifyTrainPropertySet -= SendTrainProperty;
        DataHandler.NotifyTrainPropertiesSet -= SendTrainProperties;
    }

    ////////////////////////////////////////////////////////////////////////////////

    private void OnTrainDataChanged(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        if (!AllowMessageProcessing)
            return;

        try
        {
            var schema = msgProperties["rcsschema"];
            var correlationId = "";
            if (hdr.ContainsKey("correlationId"))
                correlationId = hdr["correlationId"];

            string? rcsNode = null;

            if (msgProperties.ContainsKey("rcsNode"))
                rcsNode = msgProperties["rcsNode"];

            if (schema == c_trainDataSchemaWithNs)
            {
                string? scope = msg.Attribute("scope")?.Value;

                bool toUs = (scope == null || scope == "all" || scope == Connection.ServiceId) && (correlationId == "" || correlationId == requestMessageId) && (rcsNode == null || rcsNode == Connection.RcsNode);

                if (toUs)
                {
                    string? protoMsg = msg.Attribute("protoMessageType")?.Value;

                    if (protoMsg == "DescriberWithConsistList")
                    {
                        var protodataNode = msg.Element("protoData");
                        if (protodataNode != null)
                        {
                            var parser = new Google.Protobuf.MessageParser<TrainData.DescriberWithConsistList>(() => { return new TrainData.DescriberWithConsistList(); });
                            TrainData.DescriberWithConsistList? listOfDescribersWithConsist = parser.ParseFrom(Convert.FromBase64String(protodataNode.Value));

                            // Unserializing can be done this way, too
                            //Google.Protobuf.CodedInputStream inputStream = new Google.Protobuf.CodedInputStream(Convert.FromBase64String(protodataNode.Value));
                            //TrainData.DescriberWithConsistList listOfDescribersWithConsist = new();
                            //listOfDescribersWithConsist.MergeFrom(inputStream);

                            if (listOfDescribersWithConsist != null)
                            {
                                Log.Debug("UpdateMode: {0}", UpdateMode[listOfDescribersWithConsist.UpdateMode]);

                                switch (listOfDescribersWithConsist.UpdateMode)
                                {
                                    case TrainData.EUpdateMode.StaticFirst:
                                        // TODO: clear all train info, and also everything attached to train ?????
                                        break;

                                    case TrainData.EUpdateMode.Static:
                                        break;

                                    case TrainData.EUpdateMode.StaticLast:
                                        requestMessageId = ""; // Our request has ended
                                        break;

                                    case TrainData.EUpdateMode.Dynamic:
                                        break;
                                }

                                Log.Debug("# describers in train data message: {0}", listOfDescribersWithConsist.Describer.Count);

                                if (listOfDescribersWithConsist.Describer.Count > 0)
                                {
                                    foreach (var describerWithConsist in listOfDescribersWithConsist.Describer)
                                    {
                                        var action = describerWithConsist.Action;
                                        var describer = describerWithConsist.Describer;

                                        //var sysid = describer.Train.SysId;
                                        var td = describer.Train.Description;
                                        var ctcid = describer.Train.Sysname;
                                        var obid = describer.ExternalTrain.VehicleId;
                                        var guid = describer.ExternalTrain.Guid;
                                        var sysid = describer.Train.Id;
                                        var type = describer.DescriberType == TrainData.DescriberWithConsist.Types.Describer.Types.EDescriberType.Train ? Train.CtcTrainType.Train : Train.CtcTrainType.Sequence;

                                        if (obid == "")
                                            obid = ctcid;
                                        //if (guid == "")
                                        //    guid = obid;

                                        Log.Debug($"Train data from message: obid={obid}, ctcid={ctcid}, td={td}, guid={guid}, action {action}");

                                        Train? train = DataHandler.GetTrain(obid);
                                        if (train != null)
                                            train.UpdateBaseInfo(guid, ctcid, td, type);

                                        // If train is to be deleted or created (and it exists already), delete existing train, IDs may be changed
                                        if (train != null && action != TrainData.DescriberWithConsist.Types.EDescriberAction.Change)
                                        {
                                            Log.Information($"Train deleted: {train}");

                                            // Only obid is needed in train deletion
                                            DataHandler.DeleteTrain(obid);
                                            train = null;
                                        }
                                        if (train == null && action == TrainData.DescriberWithConsist.Types.EDescriberAction.Create)
                                        {
                                            train = DataHandler.CreateTrain(obid, guid, ctcid, td, sysid, type);
                                            if (train != null)
                                                Log.Information($"Train created: {train}");
                                        }

                                        if (train != null)
                                        {
                                            DataHandler.TrainDataChanged(train, describerWithConsist);
                                            if (GlobalDeclarations.MyEnableSerializeTrain) SerializeTrain(train);
                                        }
                                    }
                                }

                                // TODO: what to do?
                                if (listOfDescribersWithConsist.UpdateMode == TrainData.EUpdateMode.StaticLast)
                                {
                                    DataHandler.SetTrainPositionsRequested(false);
                                    SendTimetableInfo();
                                }

                            }
                        }
                    }
                    else
                    {
                        Log.Warning("Unknown protobuf message discarded: {0}", protoMsg);
                    }
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
    public void SerializeTrain(Train theTrain)
    {

        var str = JsonConvert.SerializeObject(theTrain);
        var filename = $"Train_{DateTime.Now:MMddyyyyhhmmssfff}.json";
        var curDir = Environment.CurrentDirectory;
        const string folder = @"Data\SerializeData\Train";
        if (!Directory.Exists(System.IO.Path.Combine(curDir, folder)))
        {
            Directory.CreateDirectory(System.IO.Path.Combine(curDir, folder));
        }

        var fullpath = System.IO.Path.Combine(curDir, folder, filename);
        File.WriteAllText(fullpath, str);


    }

    private void OnTrainCommandStarting(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        if (!AllowMessageProcessing)
            return;

        try
        {
            var schema = msgProperties["rcsschema"];
            string? rcsNode = null;

            if (msgProperties.ContainsKey("rcsNode"))
                rcsNode = msgProperties["rcsNode"];

            if (schema == c_trainCommandStartingSchemaWithNs)
            {
                bool toUs = rcsNode == null || rcsNode == Connection.RcsNode;

                if (toUs)
                {
                    string? protoMsg = msg.Attribute("protoMessageType")?.Value;

                    if (protoMsg == "TrainCommandStarting")
                    {
                        var protodataNode = msg.Element("protoData");
                        if (protodataNode != null)
                        {
                            var parser = new Google.Protobuf.MessageParser<TrainData.TrainCommandStarting>(() => { return new TrainData.TrainCommandStarting(); });
                            TrainData.TrainCommandStarting? trainCommandStarting = parser.ParseFrom(Convert.FromBase64String(protodataNode.Value));

                            if (trainCommandStarting != null)
                            {
                                switch (trainCommandStarting.TrainCommand)
                                {
                                    case TrainData.ETrainCommand.Set:
                                        {
                                            // We don't have these in Set -command, when starting!
                                            //var sysid = trainCommandStarting.TrainIdentity.First().Sysid;
                                            //var guid = trainCommandStarting.TrainIdentity.First().Guid;
                                            //var describer = trainCommandStarting.TrainIdentity.First().Describer;
                                            break;
                                        }
                                    case TrainData.ETrainCommand.Replace:
                                        {
                                            // Won't do anything with these right now!
                                            var sysid = trainCommandStarting.TrainIdentity.First().Sysid;
                                            var guid = trainCommandStarting.TrainIdentity.First().Guid;
                                            var describer = trainCommandStarting.TrainIdentity.First().Describer;
                                            break;
                                        }
                                    case TrainData.ETrainCommand.Remove:
                                        break;
                                    case TrainData.ETrainCommand.Move:
                                        break;
                                    case TrainData.ETrainCommand.Exchange:
                                        break;
                                    case TrainData.ETrainCommand.Split:
                                        break;
                                    case TrainData.ETrainCommand.Join:
                                        break;
                                }
                            }
                        }
                    }
                    else
                    {
                        Log.Warning("Unknown protobuf message discarded: {0}", protoMsg);
                    }
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

    private void OnTrainCommandEnded(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        if (!AllowMessageProcessing)
            return;

        try
        {
            var schema = msgProperties["rcsschema"];
            string? rcsNode = null;

            if (msgProperties.ContainsKey("rcsNode"))
                rcsNode = msgProperties["rcsNode"];

            if (schema == c_trainCommandEndedSchemaWithNs)
            {
                bool toUs = rcsNode == null || rcsNode == Connection.RcsNode;

                if (toUs)
                {
                    string? protoMsg = msg.Attribute("protoMessageType")?.Value;

                    if (protoMsg == "TrainCommandEnded")
                    {
                        var protodataNode = msg.Element("protoData");
                        if (protodataNode != null)
                        {
                            var parser = new Google.Protobuf.MessageParser<TrainData.TrainCommandEnded>(() => { return new TrainData.TrainCommandEnded(); });
                            TrainData.TrainCommandEnded? trainCommandEnded = parser.ParseFrom(Convert.FromBase64String(protodataNode.Value));

                            if (trainCommandEnded != null && trainCommandEnded.Status == TrainData.TrainCommandEnded.Types.ETrainCommandStatus.Success)
                            {
                                uint sysid = 0;
                                string guid = "", describer = "";
                                bool tryConnectToTimetable = false;
                                bool connectToTimetable = false;

                                switch (trainCommandEnded.TrainCommand)
                                {
                                    case TrainData.ETrainCommand.Set:
                                        Log.Information($"Received train command 'Set' ended message");
                                        tryConnectToTimetable = true;
                                        break;
                                    case TrainData.ETrainCommand.Replace:
                                        Log.Information($"Received train command 'Replace' ended message");
                                        tryConnectToTimetable = true;
                                        break;
                                    case TrainData.ETrainCommand.Remove:
                                        break;
                                    case TrainData.ETrainCommand.Move:
                                        break;
                                    case TrainData.ETrainCommand.Exchange:
                                        break;
                                    case TrainData.ETrainCommand.Split:
                                        break;
                                    case TrainData.ETrainCommand.Join:
                                        break;
                                }

                                if (tryConnectToTimetable)
                                {
                                    if (trainCommandEnded.TrainIdentity.Count == 1)
                                    {
                                        sysid = trainCommandEnded.TrainIdentity.First().Sysid;
                                        guid = trainCommandEnded.TrainIdentity.First().Guid;
                                        describer = trainCommandEnded.TrainIdentity.First().Describer;

                                        connectToTimetable = sysid != 0 && describer != "";

                                        Log.Information($"--> sysid={sysid}, guid={guid}, describer={describer}, connectToTimetable={connectToTimetable}");
                                    }
                                    else
                                        Log.Error($"Message had too many train identities");
                                }

                                // Did we get the data for timetable allocation?
                                if (connectToTimetable)
                                {
                                    Log.Information($"Connecting guid={guid}, describer={describer} to timetable");

                                    // Train may not have changed GUID or describer inside CMS because it has not moved or changed any properties
                                    // We have to change those first before trying to connect to timetable
                                    Train? train = DataHandler.GetTrainBySysid(sysid);
                                    train?.UpdateBaseInfo(guid, null, describer, null);

                                    DataHandler.ConnectTimetableToTrain(guid, describer, sysid);
                                }
                            }
                        }
                    }
                    else
                    {
                        Log.Warning("Unknown protobuf message discarded: {0}", protoMsg);
                    }
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

    private bool SendTrainDataRequest()
    {
        Dictionary<string, string> hdr = new();
        Dictionary<string, string> msgProperties = new();

        var messageId = Connection.CreateNewMessageId();
        requestMessageId = messageId;

        hdr["source"] = Connection.ServiceId;
        hdr["messageId"] = messageId;
        hdr["content"] = c_trainRefreshRequestSchemaWithNs;
        hdr["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.000Z");
        //hdr["replyTo"] = "";

        msgProperties["rcsContent"] = c_trainRefreshRequestSchemaWithNs;
        msgProperties["rcsMessageId"] = messageId;
        msgProperties["rcsNode"] = Connection.RcsNode;
        //msgProperties["rcsReplyTo"] = "";

        try
        {
            // Namespaces used
            XNamespace distrain = XMLNamespaces.GetXNamespace(distrainNameSpace);
            List<string> namespaces = new() { disNameSpace, distrainNameSpace, "vc" };

            XElement msgNode = new(distrain + c_trainRefreshRequestMsgType,
                                   new XAttribute("sourceId", Connection.ServiceId),
                                   new XElement("routingToken", "")
                                  );

            DataHandler.SetTrainPositionsRequested(true);

            return Connection.SendMessage(trainInformationCommandChannel, messageProcessor.CreateMessage(hdr, msgNode, msgProperties, namespaces));
        }
        catch (Exception e)
        {
            Log.Error("Internal error in XML message creation: {0}", e.ToString());
        }

        return false;
    }

    ////////////////////////////////////////////////////////////////////////////////

    public void SendTrainProperty(Train train, TrainProperty trainProperty)
    {
        SendTrainProperties(train, new() { trainProperty });
    }

    private void SendTrainProperties(Train train, List<TrainProperty> trainProperties)
    {
        if (!AllowMessageProcessing)
            return;

        Dictionary<string, string> hdr = new();
        Dictionary<string, string> msgProperties = new();

        var messageId = Connection.CreateNewMessageId();

        hdr["source"] = Connection.ServiceId;
        hdr["messageId"] = messageId;
        hdr["content"] = c_trainPropertiesSetSchemaWithNs;
        hdr["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.000Z");
        //hdr["replyTo"] = "";

        msgProperties["rcsContent"] = c_trainPropertiesSetSchemaWithNs;
        msgProperties["rcsMessageId"] = messageId;
        msgProperties["rcsNode"] = Connection.RcsNode;
        //msgProperties["rcsReplyTo"] = "";

        try
        {
            // Create protobuf message
            SetAlarmingTrainProperties setAlarmingTrainProperties = new();
            setAlarmingTrainProperties.Sysid = train.Sysid;
            setAlarmingTrainProperties.Sysname = train.CtcId;

            foreach (var trainProperty in trainProperties)
            {
                SetAlarmingTrainProperties.Types.AlarmingTrainProperty alarmingTrainProperty = new();
                alarmingTrainProperty.TrainProperty = new();

                alarmingTrainProperty.TrainProperty.Name = trainProperty.Name;
                alarmingTrainProperty.TrainProperty.Valid = trainProperty.Valid;
                alarmingTrainProperty.TrainProperty.Value = trainProperty.Value;
                alarmingTrainProperty.TrainProperty.Type = (Property.Types.EPropertyType)trainProperty.Type;

                if (trainProperty.AlarmLevel != null)
                    alarmingTrainProperty.Alarm = (SetAlarmingTrainProperties.Types.AlarmingTrainProperty.Types.ETrainPropertyAlarm)trainProperty.AlarmLevel;
                else
                    alarmingTrainProperty.Alarm = SetAlarmingTrainProperties.Types.AlarmingTrainProperty.Types.ETrainPropertyAlarm.Alarmnotset;

                setAlarmingTrainProperties.TrainProperties.Add(alarmingTrainProperty);
            }

            // Serialize protobuf message to string and encode as base64
            string serialized = setAlarmingTrainProperties.ToByteString().ToBase64();

            // Namespaces used in message
            XNamespace distrain = XMLNamespaces.GetXNamespace(distrainNameSpace);
            List<string> namespaces = new() { disNameSpace, distrainNameSpace, "vc" };

            XElement msgNode = new(distrain + c_trainPropertiesSetMsgType,
                                new XAttribute("protoMessageType", "SetAlarmingTrainProperties"),
                                new XElement("protoData", serialized));

            var message = messageProcessor.CreateMessage(hdr, msgNode, msgProperties, namespaces);

            Connection.SendMessage(trainInformationCommandChannel, message);
        }
        catch (Exception e)
        {
            Log.Error("Internal error in XML message creation: {0}", e.ToString());
        }
    }

    ////////////////////////////////////////////////////////////////////////////////

    internal void SendEvent(Train? train, CtcEvent ctcEvent)
    {
        // Allow event sending also in standby service!

        Dictionary<string, string> hdr = new();
        Dictionary<string, string> msgProperties = new();

        var messageId = Connection.CreateNewMessageId();

        hdr["source"] = Connection.ServiceId;
        hdr["messageId"] = messageId;
        hdr["content"] = c_trainSendEventSchemaWithNs;
        hdr["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.000Z");
        //hdr["replyTo"] = "";

        msgProperties["rcsContent"] = c_trainSendEventSchemaWithNs;
        msgProperties["rcsMessageId"] = messageId;
        msgProperties["rcsNode"] = Connection.RcsNode;
        //msgProperties["rcsReplyTo"] = "";

        try
        {
            // Create protobuf message
            SendEventOrAlarm sendEventOrAlarm = new();
            sendEventOrAlarm.Sysid = train != null ? train.Sysid : 0;
            sendEventOrAlarm.Sysname = train != null ? train.CtcId : "";

            sendEventOrAlarm.Key = ctcEvent.Key;
            sendEventOrAlarm.Str1 = ctcEvent.Str1;
            sendEventOrAlarm.Str2 = ctcEvent.Str2;
            sendEventOrAlarm.Uint1 = ctcEvent.Uint1;
            sendEventOrAlarm.Uint2 = ctcEvent.Uint2;
            sendEventOrAlarm.Sysid1 = ctcEvent.Sysid1;
            sendEventOrAlarm.Sysid2 = ctcEvent.Sysid2;
            sendEventOrAlarm.Success = ctcEvent.Success;
            sendEventOrAlarm.LogId = ctcEvent.LogId;
            sendEventOrAlarm.ActorId = ctcEvent.ActorId;

            // Serialize protobuf message to string and encode as base64
            string serialized = sendEventOrAlarm.ToByteString().ToBase64();

            // Namespaces used in message
            XNamespace distrain = XMLNamespaces.GetXNamespace(distrainNameSpace);
            List<string> namespaces = new() { disNameSpace, distrainNameSpace, "vc" };

            XElement msgNode = new(distrain + c_trainSendEventMsgType,
                                new XAttribute("protoMessageType", "SendEventOrAlarm"),
                                new XElement("protoData", serialized));

            var message = messageProcessor.CreateMessage(hdr, msgNode, msgProperties, namespaces);

            Connection.SendMessage(trainInformationCommandChannel, message);
        }
        catch (Exception e)
        {
            Log.Error("Internal error in XML message creation: {0}", e.ToString());
        }
    }

    internal bool SendEventCtcMom(string eventKey, string masterObject, string text1, string? text2 = null)
    {
        // Allow event sending also in standby service!

        Dictionary<string, string> hdr = new();
        Dictionary<string, string> msgProperties = new();

        var messageId = Connection.CreateNewMessageId();
        var timeStamp = ActionTime.Now.AsDateTime().ToString("yyyy-MM-ddTHH:mm:ss.000Z");

        hdr["source"] = Connection.ServiceId;
        hdr["messageId"] = messageId;
        hdr["content"] = c_eventReportSchemaWithNs;
        hdr["schema"] = c_eventReportSchemaWithNs;      // CtcMom expects this?
        hdr["timestamp"] = timeStamp;

        msgProperties["rcsContent"] = c_eventReportSchemaWithNs;
        msgProperties["rcsschema"] = c_eventReportSchemaWithNs;      // CtcMom expects this?
        msgProperties["rcsSchema"] = c_eventReportSchemaWithNs;      // CtcMom expects this?
        msgProperties["rcsMessageId"] = messageId;
        msgProperties["rcsNode"] = Connection.RcsNode;

        try
        {
            // Namespaces used in message
            XNamespace disER = XMLNamespaces.GetXNamespace(disERNameSpace);

            XElement msgNode = new(disER + c_eventReportMsgType, new XAttribute(XNamespace.Xmlns + disERNameSpace, disER.NamespaceName),
                                   new XElement(disER + "masterObject", masterObject),
                                   new XElement(disER + "operator", "None" /*Connection.ServiceId // Use now sysobj, that exists in Solid DB, until ROS is changed*/),
                                   new XElement(disER + "eventType", "TextKey"),
                                   new XElement(disER + "eventKey", eventKey),
                                   new XElement(disER + "timestamp", timeStamp)
                                    );

            XElement paramsNode = new(disER + "params");
            if (text1 != null)
                paramsNode.Add(new XElement(disER + "freeTextValue", text1));
            if (text2 != null)
                paramsNode.Add(new XElement(disER + "freeTextValue", text2));

            msgNode.Add(paramsNode);

            var message = messageProcessor.CreateMessage(hdr, msgNode, msgProperties);
            Log.Information($"Sent event message: {message}");

            return Connection.SendMessage(eventReportChannel, message);
        }
        catch (Exception e)
        {
            Log.Error("Internal error in XML message creation: {0}", e.ToString());
        }

        return false;
    }

    ////////////////////////////////////////////////////////////////////////////////

    internal void SendTimetableInfo()
    {
        // Allow timetable info sending also in standby service!

        Dictionary<string, string> hdr = new();
        Dictionary<string, string> msgProperties = new();

        var messageId = Connection.CreateNewMessageId();
        var timeStamp = ActionTime.Now.AsDateTime().ToString("yyyy-MM-ddTHH:mm:ss.000Z");

        hdr["source"] = Connection.ServiceId;
        hdr["messageId"] = messageId;
        hdr["content"] = c_timetableInfoSchemaWithNs;
        hdr["timestamp"] = timeStamp;

        msgProperties["rcsContent"] = c_timetableInfoSchemaWithNs;
        msgProperties["rcsMessageId"] = messageId;
        msgProperties["rcsNode"] = Connection.RcsNode;

        try
        {
            var GetIlElementName = (ElementPosition pos) =>
            {
                string elementName = "";

                // First try with platform, then with element position
                var platform = Service?.RailgraphHandler?.GetPlatform(pos.AdditionalName);
                if (platform != null)
                    elementName = platform.Tracks.FirstOrDefault(new RailgraphLib.HierarchyObjects.Track(0, "", "")).SysName;
                if (elementName == "")
                {
                    RailgraphLib.GraphObj graphObj = Service?.RailgraphHandler?.GetILElementOnElementPosition(pos);
                    if (graphObj != null)
                        elementName = graphObj.getName();
                }
                return elementName;
            };

            var GetIlElementId = uint (ElementPosition pos) =>
            {
                RailgraphLib.GraphObj graphObj = Service?.RailgraphHandler?.GetILElementOnElementPosition(pos);
                if (graphObj != null)
                    return graphObj.getId();
                return 0;
            };

            // Namespaces used in message
            XNamespace timetableNs = XMLNamespaces.GetXNamespace(timetableNameSpace);

            XElement msgNode = new(timetableNs + c_timetableInfoMsgType, new XAttribute(XNamespace.Xmlns + timetableNameSpace, timetableNs.NamespaceName));

            foreach (var scheduledPlan in DataHandler.ScheduledPlans.Values)
            {
                var scheduledDay = DataHandler.GetScheduledDay(scheduledPlan.ScheduledDayCode);
                if (scheduledDay != null)
                {
                    XElement serviceNode = new XElement(timetableNs + "service",
                        new XAttribute("day", scheduledDay.AsISODateString()),
                        new XAttribute("name", scheduledPlan.Name),
                        new XAttribute("starttime", scheduledPlan.StartTime.ToString()),
                        new XAttribute("endtime", scheduledPlan.EndTime.ToString()),
                        new XAttribute("startloc", scheduledPlan.StartSite.AdditionalName),
                        new XAttribute("startelem", GetIlElementName(scheduledPlan.StartSite)),
                        new XAttribute("startelemid", GetIlElementId(scheduledPlan.StartSite)),
                        new XAttribute("endloc", scheduledPlan.EndSite.AdditionalName),
                        new XAttribute("endelem", GetIlElementName(scheduledPlan.EndSite)),
                        new XAttribute("endelemid", GetIlElementId(scheduledPlan.EndSite)),
                        new XAttribute("traintype", DataHandler.GetTrainType(scheduledPlan.TrainTypeId)),
                        new XAttribute("spare", scheduledPlan.IsSparePlan),
                        new XAttribute("allocated", DataHandler.IsAllocated(scheduledPlan.Key)));

                    // Now we shift all trips' times with delay of previous one, or by delay given to that particular trip
                    int delay = 0;

                    foreach (var trip in scheduledPlan.Trips.Values)
                    {
                        if (trip.DelaySeconds != 0)
                            delay = trip.DelaySeconds;

                        var startTime = trip.StartTime + new TimeSpan(0, 0, 0, delay);
                        var endTime = trip.EndTime + new TimeSpan(0, 0, 0, delay);

                        serviceNode.Add(timetableNs + "trip", new XElement(timetableNs + "trip",
                            new XAttribute("tripnumber", trip.TripNumber),
                            new XAttribute("name", trip.Name),
                            new XAttribute("tripcode", trip.TripCode),
                            new XAttribute("starttime", startTime.ToString()),
                            new XAttribute("endtime", endTime.ToString()),
                            new XAttribute("startloc", trip.StartPos.AdditionalName),
                            new XAttribute("startelem", GetIlElementName(trip.StartPos)),
                            new XAttribute("startelemid", GetIlElementId(trip.StartPos)),
                            new XAttribute("endloc", trip.EndPos.AdditionalName),
                            new XAttribute("endelem", GetIlElementName(trip.EndPos)),
                            new XAttribute("endelemid", GetIlElementId(trip.EndPos)),
                            new XAttribute("spare", trip.IsSpareTrip),
                            new XAttribute("allocated", DataHandler.IsAllocated(scheduledPlan.Key, trip.Id))));
                    }

                    msgNode.Add(serviceNode);
                }
            }

            var message = messageProcessor.CreateMessage(hdr, msgNode, msgProperties);
            //Log.Information($"Sent timetable info message: {message}");

            Connection.SendMessage(timetableInfoChannel, message);
        }
        catch (Exception e)
        {
            Log.Error("Internal error in XML message creation: {0}", e.ToString());
        }
    }

}
