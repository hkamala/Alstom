using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;
using Apache.NMS;
using E2KService.ActiveMQ;
using ConflictManagementService.Model;
using E2KService.ActiveMQ.AMQP;
using RoutePlanLib;
using System.Xml;
using Newtonsoft.Json;
using XSD.CancelRoutePlan;
using Train = ConflictManagementService.Model.Train;
using E2KService.MessageHandler;
using System.Threading;

namespace E2KService.MessageHandler
{
	internal class RosMessagingHandler : ActiveStateMessageHandler, IRosMessaging
	{
		private readonly ActiveMQ.AMQP.Rcs2kXmlMessageProcessor messageProcessor = new ActiveMQ.AMQP.Rcs2kXmlMessageProcessor("Route Information Message Processor");
		private RoutePlanLib.RosMessageHandler roshandler;

		private Channel movementSessionClient;
		private Channel movementSessionServer;
		private Channel ctcResClient;
		private Channel ctcRouteInfo;

		private Subscription subsTMSReq;
		private Subscription subsCancelTMSReq;
		private Subscription subsTrainMovementStateChange;
		private Subscription subsPretestResponse;
		private Subscription subsRouteInfo;

        string routeplanschema = "";
        string cancelrouteplanSchema = "";
        string movementschema = "";
        string pretestRequestSchema = "";
        string pretestResponseSchema = "";
        string serviceRoutePlanSchema = "";
        string serviceRoutePlanRequestSchema = "";
        string routeInfoSchema = "";

        private System.Collections.Concurrent.ConcurrentDictionary<string /*messageId*/, Tuple<ActionTime, int /*pretestId*/, IRosMessaging.DelegatePretestResult /*resultCall*/>> pretestRequests = new(2, 100);
		private readonly Thread maintenanceThread;

		public RosMessagingHandler(Connection connection, DataHandler dataHandler, IDictionary<string, string> appConfig) : base(connection, dataHandler)
		{
			string tmsReq = appConfig["Ros:topics:tmsreq"];
            string ctcres = appConfig["Ros:queues:ctcrouterequest"];
            string routeInfo = appConfig["Ros:queues:ctcrouteinfo"];
            string tmsServerReq = appConfig["Ros:queues:tmsserverreq"];

			routeplanschema = appConfig["Ros:schemas:tmsreq"];
			cancelrouteplanSchema = appConfig["Ros:schemas:tmscancelreq"];			
			movementschema = appConfig["Ros:schemas:tmsrtsreq"];
			pretestRequestSchema = appConfig["Ros:schemas:pretestreq"];
			pretestResponseSchema = appConfig["Ros:schemas:pretestres"];
            serviceRoutePlanSchema = appConfig["Ros:schemas:servicerouteplan"];
            serviceRoutePlanRequestSchema = appConfig["Ros:schemas:servicerouteplanrequest"];
            routeInfoSchema = appConfig["Ros:schemas:routeinfo"];

            roshandler = new RoutePlanLib.RosMessageHandler(Connection.ServiceId, Connection.RcsNode)
			{
				RoutePlanSchema = routeplanschema,
				CancelRoutePlanSchema = cancelrouteplanSchema,
				MovementSchema = movementschema,
				PretestRequestSchema = pretestRequestSchema,
				PretestResponseSchema = pretestResponseSchema,
				ServiceRoutePlanSchema = serviceRoutePlanSchema,
                ServiceRoutePlanRequestSchema = serviceRoutePlanRequestSchema
            };

			movementSessionClient = new Channel(ChannelType.Topic, tmsReq);
            movementSessionServer = new Channel(ChannelType.Queue, tmsServerReq);
            ctcResClient = new Channel(ChannelType.Queue, ctcres);
			ctcRouteInfo = new Channel(ChannelType.Queue, routeInfo);

			subsTMSReq = new Subscription(movementSessionClient, "RoutePlan");	// This also handles service route plan!
			subsCancelTMSReq = new Subscription(movementSessionClient, "CancelRoutePlan");
			subsTrainMovementStateChange = new Subscription(movementSessionClient, "TrainMovementStateChange");
			subsPretestResponse = new Subscription(ctcRouteInfo, "PretestResponse");
			subsRouteInfo = new Subscription(ctcRouteInfo, "routeinfo");

            HandleSubscriptions(MessagingStateSubscription.Always);

			maintenanceThread = new Thread(new ThreadStart(MaintenanceThread))
			{
				Name = "RosMessagingPeriodicTask",
				IsBackground = true
			};
			maintenanceThread.Start();
		}

		private void HandleSubscriptions(MessagingStateSubscription state)
		{
			switch (state)
			{
				case MessagingStateSubscription.Always:
					// We want to receive and handle route plan messages from TMS also in standby server, so in server switch situation we have plan ready
					// and do not have to wait until next station/platform. We are not sending it, though
					Connection.Subscribe(subsTMSReq, this.messageProcessor, OnMessage); // Do not use selector with TMS message!
                    Connection.Subscribe(subsCancelTMSReq, this.messageProcessor, OnMessage);   // Do not use selector with TMS message!
                    break;

				case MessagingStateSubscription.MessagingActive:
					string? selector = Connection.RcsNodeSelector;
					Connection.Subscribe(subsTrainMovementStateChange, this.messageProcessor, OnMessage);   // Do not use selector with TMS message!
					Connection.Subscribe(subsPretestResponse, this.messageProcessor, OnMessage, selector);    // Use selector in ROS messages!
					//Connection.Subscribe(subsRouteInfo, this.messageProcessor, OnMessage, selector);    // Use selector in ROS messages (this subscription is for RPL handling the messsage)!
                    Connection.Subscribe(subsRouteInfo, this.messageProcessor, OnRouteInfo, selector);    // Use selector in ROS messages!
                    break;

				case MessagingStateSubscription.MessagingInactive:
					Connection.Unsubscribe(subsTrainMovementStateChange);
					Connection.Unsubscribe(subsPretestResponse);
					Connection.Unsubscribe(subsRouteInfo);
					break;
			}
		}

		private void MaintenanceThread()
		{
			while (ServiceImp.Service.ServiceState != ServiceStateHelper.ServiceState.Shutdown)
			{
				ActionTime now = ActionTime.Now;

				List<string> failedRequests = new();

				// Send response to failed requests and remove requests
				foreach (var request in this.pretestRequests)
				{
					if (request.Value.Item1 + new TimeSpan(0, 0, 0, 5) < now)
						failedRequests.Add(request.Key);
				}

				foreach (var messageId in failedRequests)
				{
					if (this.pretestRequests.TryRemove(messageId, out Tuple<ActionTime, int, IRosMessaging.DelegatePretestResult>? info) && info != null && info.Item3 != null)
					{
						info.Item3(info.Item2, false, null);
					}
				}

				Thread.Sleep(1000);
			}
		}

		////////////////////////////////////////////////////////////////////////////////

		protected override void MessagingActivated()
		{
			HandleSubscriptions(MessagingStateSubscription.MessagingActive);
			Log.Information("RosMessagingHandler: Message processing activated: accepting messages");

			// Add callbacks for DataHandler's notifications/requests
		}

		protected override void MessagingDeactivated()
		{
			HandleSubscriptions(MessagingStateSubscription.MessagingInactive);
			Log.Information("RosMessagingHandler: Message processing deactivated: rejecting messages");

			// Remove callbacks for DataHandler's notifications
		}

		////////////////////////////////////////////////////////////////////////////////

		private void RememberPretestRequest(int pretestId, string messageId, IRosMessaging.DelegatePretestResult resultCall)
        {
			this.pretestRequests.TryAdd(messageId, new Tuple<ActionTime, int, IRosMessaging.DelegatePretestResult>(ActionTime.Now, pretestId, resultCall));
		}

		////////////////////////////////////////////////////////////////////////////////

		private void OnMessage(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
		{
			var routingMessage = roshandler.DeserializeMessage(rawMsg);

            // TMS route plan messages must be received also in standby server
			if (routingMessage is XSD.RoutePlan.rcsMsg)
			{
                DataHandler.RoutePlanReceivedFromTMS((XSD.RoutePlan.rcsMsg)routingMessage);

                XSD.RoutePlanResponce.rcsMsg outMsg = new XSD.RoutePlanResponce.rcsMsg("testApp", "testSchema");
                outMsg.data.ActionPlan = new XSD.RoutePlanResponce.ActionPlan { Trains = new XSD.RoutePlanResponce.ActionPlanTrains((routingMessage as XSD.RoutePlan.rcsMsg).data.RoutePlan.Trains) };
                string stringMsg = RoutePlanLib.XmlSerialization.SerializeObject<XSD.RoutePlanResponce.rcsMsg>(outMsg, out string errorText);
                return;
			}
            else if (routingMessage is XSD.CancelRoutePlan.rcsMsg)
            {
                DataHandler.CancelRoutePlanReceivedFromTMS((XSD.CancelRoutePlan.rcsMsg)routingMessage);
            }
            else if (routingMessage is XSD.RoutePlan.rcsMsg)
            {
                DataHandler.RoutePlanReceivedFromTMS((XSD.RoutePlan.rcsMsg)routingMessage);
                return;
            }

            // Others depend on active state of CMS
            if (!AllowMessageProcessing)
                return;

            if (routingMessage is XSD.PretestResponse.rcsMsg response)
            {
                var correlationId = String.Empty;

                /* 2023-08-15 A.Kautonen : the 7 lines after this comment region should be replaced by these 2 lines!
				if (msgProperties.ContainsKey(E2KService.ActiveMQ.AMQP.MessageProcessor.PropertyCorrelationId))
					correlationId = msgProperties[E2KService.ActiveMQ.AMQP.MessageProcessor.PropertyCorrelationId];
                */
                if (msgProperties.ContainsKey(
                        "rcsCorrelationId")) // || msgProperties.ContainsKey("E2KService-correlationid"))
                    correlationId = msgProperties["rcsCorrelationId"];
                else if
                    (msgProperties.ContainsKey(
                        "E2KService-correlationid")) // || msgProperties.ContainsKey("E2KService-correlationid"))
                    correlationId = msgProperties["E2KService-correlationid"];

                if (!string.IsNullOrEmpty(correlationId))

                    if (this.pretestRequests.ContainsKey(correlationId))
                    {
                        this.pretestRequests.TryRemove(correlationId,
                            out Tuple<ActionTime, int, IRosMessaging.DelegatePretestResult>? info);
                        if (info != null && info.Item3 != null)
                        {
                            // Call the given result method
                            info.Item3(info.Item2, response.data.PretestResponse.Success,
                                new IRosMessaging.PretestResult(info.Item2, response));
                        }
                    }
            }
        
        /*else if (routingMessage is XSD.RouteInfo.rcsMsg routeInfo)    // RoutePlanLib handles the message!
        {
                Here's the message data (schema: rcs.e2k.ctc.RouteInfo.V1, <hdr> most probably the same as in pretest response message)
                No need to implement anything here, I don't actually know yet, what to do with this data...

                <data>
                    <routeinfo>
                        <trainid>..</trainid>			// These IDs are strings
                        <commandid>..</commandid>		// May be "None"
                        <routeid>..</routeid>			// May be "None"
                        <routeobjects>					// Optional, may be missing
                            <obj>..</obj>				// string
                            ...
                        </routeobjects>
                        <destinations>					// Optional, may be missing
                            <dest>..</dest>				// string
                            ...
                        </destinations>
                    </routeinfo>
                </data>

                Example:
                    <?xml version="1.0" encoding="utf-8"?>
                    <routeinfo>
                        <trainid>4873</trainid>
                        <commandid>None</commandid>
                        <routeid>None</routeid>
                        <routeobjects>
                            <obj>TC2C_CAR</obj>
                            <obj>SIP2_CAR</obj>
                            <obj>TJ_TC2C_CAR_PT1_CAR_PL1</obj>
                            <obj>PT1_CAR_PL1</obj>
                            <obj>PT1_CAR</obj>
                            <obj>PT1_CAR_PL0</obj>
                            <obj>TJ_PT1_CAR_PL0_TCNAP_CAR</obj>
                            <obj>TCNAP_CAR</obj>
                            <obj>TJ_TCNAP_CAR_PT5_5S_CAR_PL0</obj>
                            <obj>PT5_5S_CAR_PL0</obj>
                            <obj>PT5/5S_CAR</obj>
                            <obj>PT5_5S_CAR_PL2</obj>
                            <obj>TJ_PT5_5S_CAR_PL2_TCCHP_CAR</obj>
                            <obj>TCCHP_CAR</obj>
                            <obj>TJ_KUU1P_CAR_KUU2P_CAR</obj>
                            <obj>SI4_CAR</obj>
                            <obj>TC4_CAR_LIL</obj>
                            <obj>TJ_KUU2P_CAR_KPU1P_LIL</obj>
                            <obj>TC2_CAR_LIL</obj>
                            <obj>SIP_LIL</obj>
                        </routeobjects>
                        <destinations>
                            <dest>P2C_CAR</dest>
                            <dest>P2_GAU</dest>
                        </destinations>
                    </routeinfo>

        }*/
		}

        private void OnRouteInfo(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
        {
            /*
                Here's the message data (schema: rcs.e2k.ctc.RouteInfo.V1, <hdr> most probably the same as in pretest response message)
                No need to implement anything here, I don't actually know yet, what to do with this data...

                <data>
                    <routeinfo>
                        <trainid>..</trainid>			// These IDs are strings
                        <commandid>..</commandid>		// May be "None"
                        <routeid>..</routeid>			// May be "None"
                        <routeobjects>					// Optional, may be missing
                            <obj>..</obj>				// string
                            ...
                        </routeobjects>
                        <destinations>					// Optional, may be missing
                            <dest>..</dest>				// string
                            ...
                        </destinations>
                    </routeinfo>
                </data>

                Example:
                    <?xml version="1.0" encoding="utf-8"?>
                    <routeinfo>
                        <trainid>4873</trainid>
                        <commandid>None</commandid>
                        <routeid>None</routeid>
                        <routeobjects>
                            <obj>TC2C_CAR</obj>
                            <obj>SIP2_CAR</obj>
                            <obj>TJ_TC2C_CAR_PT1_CAR_PL1</obj>
                            <obj>PT1_CAR_PL1</obj>
                            <obj>PT1_CAR</obj>
                            <obj>PT1_CAR_PL0</obj>
                            <obj>TJ_PT1_CAR_PL0_TCNAP_CAR</obj>
                            <obj>TCNAP_CAR</obj>
                            <obj>TJ_TCNAP_CAR_PT5_5S_CAR_PL0</obj>
                            <obj>PT5_5S_CAR_PL0</obj>
                            <obj>PT5/5S_CAR</obj>
                            <obj>PT5_5S_CAR_PL2</obj>
                            <obj>TJ_PT5_5S_CAR_PL2_TCCHP_CAR</obj>
                            <obj>TCCHP_CAR</obj>
                            <obj>TJ_KUU1P_CAR_KUU2P_CAR</obj>
                            <obj>SI4_CAR</obj>
                            <obj>TC4_CAR_LIL</obj>
                            <obj>TJ_KUU2P_CAR_KPU1P_LIL</obj>
                            <obj>TC2_CAR_LIL</obj>
                            <obj>SIP_LIL</obj>
                        </routeobjects>
                        <destinations>
                            <dest>P2C_CAR</dest>
                            <dest>P2_GAU</dest>
                        </destinations>
                    </routeinfo>
            */

            if (!AllowMessageProcessing)
                return;

            try
            {
                var schema = msgProperties["rcsschema"];

                if (schema == routeInfoSchema)
                {
                    var ctcId = msg.Element("trainid")!.Value;
                    var routeId = msg.Element("routeid")!.Value;

                    Train? train = DataHandler.GetTrainByCtcId(ctcId);
                    if (train != null && routeId != "" && routeId != "None")
                    {
                        DataHandler.RouteSetInfoReceived(train, routeId);
                        Log.Information($"Route set info received from ROS: {train} - {routeId}");
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

        private string? SerializeAndSend(object? dataToSerialize, Channel channel, string schema = "", bool recordData = false, string tripId = "")
		{
			string? messageId = null;

			if (AllowMessageProcessing && dataToSerialize != null)
            {
                if (recordData) SerializedDataToSerialize(dataToSerialize);
                var msgInfo = roshandler.SerializeData(dataToSerialize, schema, tripId);

				messageId = Connection.CreateNewMessageId();

                // Set all necessary e2k properties. Do not directly use msgInfo.Item3, because it may in that case contain all information from previously sent message!
                // Just copy all information from it set by RoutePlanLib
                var messageProperties = new Dictionary<string, object>(msgInfo.Item3)
                {
                    { "rcsschema", msgInfo.Item1 },
                    { "rcsMessageId", messageId }
                };

                IMessage? message = messageProcessor.CreateMessage(msgInfo.Item2, messageProperties);
                //SerializeIMessage(message);
				if (message != null)
				{
					Connection.SendMessage(channel, message);
				}
			}

			return messageId;
		}

		////////////////////////////////////////////////////////////////////////////////
        public void SerializeIMessage(IMessage theMessage)
        {

            var str = JsonConvert.SerializeObject(theMessage);
            var filename = $"IMessage-{DateTime.Now.ToString("ddMMyyHHmmss")}.json";
            var curDir = Environment.CurrentDirectory;
            const string folder = "Data";
            if (!Directory.Exists(System.IO.Path.Combine(curDir, folder)))
            {
                Directory.CreateDirectory(System.IO.Path.Combine(curDir, folder));
            }

            var fullpath = System.IO.Path.Combine(curDir, folder, filename);
            File.WriteAllText(fullpath, str);
        }
        public void SerializedDataToSerialize(object theData)
        {

            var str = JsonConvert.SerializeObject(theData);
            var filename = $"DataSentToROS-{DateTime.Now.ToString("ddMMyyHHmmss")}.json";
            var curDir = Environment.CurrentDirectory;
            const string folder = @"Data\SerializeData\ROS";
            if (!Directory.Exists(System.IO.Path.Combine(curDir, folder)))
            {
                Directory.CreateDirectory(System.IO.Path.Combine(curDir, folder));
            }

            var fullpath = System.IO.Path.Combine(curDir, folder, filename);
            File.WriteAllText(fullpath, str);
        }


        void IRosMessaging.SendRoutePlan(RoutePlan routePlan, string tripId = "")
		{
			SerializeAndSend(routePlan.TMSRoutePlan, ctcResClient, routeplanschema, true, tripId);
		}

        void IRosMessaging.SendScheduledRoutePlan(ScheduledRoutePlan scheduledRoutePlan)
        {
            SerializeAndSend(scheduledRoutePlan.TMSRoutePlan, ctcResClient, serviceRoutePlanSchema);
        }

        void IRosMessaging.SendScheduledRoutePlanRequest(ScheduledPlan scheduledPlan)
		{
			XSD.ServiceRoutePlanRequest.rcsMsg msg = new(Connection?.ServiceId, serviceRoutePlanRequestSchema, scheduledPlan.Id);
            SerializeAndSend(msg, movementSessionServer);
        }

        void IRosMessaging.SendCancelRoutePlan(XSD.CancelRoutePlan.rcsMsg cancelRoutePlan)
		{
			SerializeAndSend(cancelRoutePlan, ctcResClient);
		}

		void IRosMessaging.PretestRouteReachable(int pretestId, Train train, RailgraphLib.HierarchyObjects.Route route, string command, IRosMessaging.DelegatePretestResult resultCall)
		{
			XSD.PretestRequest.rcsMsg msg = new(Connection?.ServiceId, pretestRequestSchema, train.CtcId, command, route.SysName, "RouteReachable");
			var messageId = SerializeAndSend(msg, ctcResClient);

			if (messageId != null)
				RememberPretestRequest(pretestId, messageId, resultCall);
		}

		void IRosMessaging.PretestRouteAvailable(int pretestId, Train train, RailgraphLib.HierarchyObjects.Route route, string command, IRosMessaging.DelegatePretestResult resultCall)
		{
			XSD.PretestRequest.rcsMsg msg = new(Connection?.ServiceId, pretestRequestSchema, train.CtcId, command, route.SysName, "RouteAvailable");
			var messageId = SerializeAndSend(msg, ctcResClient);

			if (messageId != null)
				RememberPretestRequest(pretestId, messageId, resultCall);
            ;
        }

		void IRosMessaging.PretestSingleObject(int pretestId, RailgraphLib.Interlocking.ILGraphObj element, string command, IRosMessaging.DelegatePretestResult resultCall)
		{
			XSD.PretestRequest.rcsMsg msg = new(Connection?.ServiceId, pretestRequestSchema, "", command, element.getName(), "SingleObject");
			var messageId = SerializeAndSend(msg, ctcResClient);

			if (messageId != null)
				RememberPretestRequest(pretestId, messageId, resultCall);
		}

    }
}
