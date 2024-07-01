namespace E2KService.MessageHandler;

using System;
using System.Xml.Linq;
using System.Xml.Serialization;
using E2KService.ActiveMQ;
using ConflictManagementService.Model;
using ConflictManagementService.Model.TMS;
using Serilog;
using Apache.NMS;
using E2KService.ActiveMQ.AMQP;
using System.Collections.Generic;
using System.Linq;

internal class TimetableHandler : ActiveStateMessageHandler
{
    readonly ActiveMQ.AMQP.Rcs2kXmlMessageProcessor messageProcessor = new("Timetable Message Processor");

    // Channels 
    const string userAuthorityRequestDestination = "jms.topic.rcs.e2k.ctc.usermanagement";
    static readonly Channel userAuthorityRequestChannel = new(ChannelType.Topic, userAuthorityRequestDestination);
    const string userAuthoritiesDestination = "jms.topic.RCS.E2K.TMS.UserSessionClient";
    static readonly Channel userAuthoritiesChannel = new(ChannelType.Topic, userAuthoritiesDestination);

    const string timetableInfoDestination = "jms.topic.ConflictManagementService.TimetableInfo";                // Responses tscheduler -> CMS
    const string timetableRequestDestination = "jms.queue.TMS.TimetableHandlerServer";                          // Requests CMS -> tscheduler
    static readonly Channel timetableInfoChannel = new(ChannelType.Topic, timetableInfoDestination);
    static readonly Channel timetableInfoRequestChannel = new(ChannelType.Queue, timetableRequestDestination);

    const string commonTimetableInfoDestination = "jms.topic.TMS.TimetableHandlerClient";                       // tscheduler -> all listeners
    static readonly Channel commonTimetableInfoChannel = new(ChannelType.Topic, commonTimetableInfoDestination);

    const string regulationInfoDestination = "jms.topic.TMS.RegulationSessionHandlerClient";                    // tscheduler -> all listeners
    const string regulationInfoRequestDestination = "jms.queue.TMS.RegulationSessionHandlerServer";             // CMS -> tscheduler
    static readonly Channel regulationInfoChannel = new(ChannelType.Topic, regulationInfoDestination);
    static readonly Channel regulationInfoRequestChannel = new(ChannelType.Queue, regulationInfoRequestDestination);

    // Schemas
    const string userAuthorityRequestSchema = "rcs.e2k.ctc.usermanagement.V1";
    const string userAuthoritiesSchema = "RCS.E2K.TMS.UserSessionClient.V1";

    const string activeServiceRequestSchema = "RCS.E2K.TMS.ActiveServiceRequest.V1";
    const string activeServiceSchema = "RCS.E2K.TMS.TimetableSession.ActiveService.V1";             // These come as responses to request, the latter ones not...
    const string nonActiveServiceSchema = "RCS.E2K.TMS.TimetableSession.NonActiveService.V1";
    const string activeServiceSchema2 = "RCS.E2K.TMS.RegulationSession.ActiveService.V1";           // These come as dynamic updates, when tscheduler switches to master
    const string nonActiveServiceSchema2 = "RCS.E2K.TMS.RegulationSession.NonActiveService.V1";

    const string scheduledDaysRequestSchema = "RCS.E2K.TMS.SCHEDULED_DAYS_REQUEST.V1";
    const string scheduledDaysSchema = "RCS.E2K.TMS.SCHEDULED_DAYS.V1";
    const string stationRequestSchema = "RCS.E2K.TMS.TREE_STATION_REQUEST.V1";
    const string stationSchema = "RCS.E2K.TMS.TREE_STATION.V1";
    const string stationPlatformsRequestSchema = "RCS.E2K.TMS.STATION_INFO_REQUEST.V1";  // All platforms on station
    const string platformRequestSchema = "RCS.E2K.TMS.PLATFORM_INFO_REQUEST.V1"; // Only one platform
    const string platformInfoSchema = "RCS.E2K.TMS.PLATFORM_INFO.V1";
    const string linesRequestSchema = "RCS.E2K.TMS.TREE_LINE_REQUEST.V1";
    const string linesSchema = "RCS.E2K.TMS.TREE_LINE.V1";
    const string scheduledServicesRequestSchema = "RCS.E2K.TMS.TREE_SERVICE_REQUEST.V1";
    const string scheduledServicesSchema = "RCS.E2K.TMS.TREE_SERVICE.V1";
    const string serviceInfoRequestSchema = "RCS.E2K.TMS.SERVICE_INFO_REQUEST.V3"; // tsched 5.0 version?
    const string serviceInfoSchema = "RCS.E2K.TMS.SERVICE_INFO.V1";             // tsched 5.0 version?
    const string tripInfoRequestSchema = "RCS.E2K.TMS.MULTITRIP_INFO_REQUEST.V3";  // tsched 5.0 version?
    const string tripInfoSchema = "RCS.E2K.TMS.TRIP_INFO.V1";                   // tsched 5.0 version?
    const string timetableRequestSchema = "RCS.E2K.TMS.TIMETABLE_REQUEST.V1";
    const string timetableSchema = "RCS.E2K.TMS.SCHEDULED_GRAPH_TRAINS.V1";
    const string actionTypeListRequestSchema = "RCS.E2K.TMS.DEFAULT_ACTION_LIST_REQUEST.V1";
    const string actionTypeListSchema = "RCS.E2K.TMS.DEFAULT_ACTION_LIST.V1";
    const string serviceForecastUpdateSchema = "RCS.E2K.TMS.ServiceForecastUpdate.V1";
    const string movementTemplateRequestSchema = "RCS.E2K.TMS.MOVEMENT_TEMPLATES_REQUEST.V1";
    const string movementTemplateSchema = "RCS.E2K.TMS.TRIP_TEMPLATE_MOVEMENTS_INFO.V1";

    const string tsuiHelloRequestSchema = "RCS.E2K.TMS.TSUIHelloRequest.V1";
    const string tsuiHelloSchema = "RCS.E2K.TMS.TSUIHello.V1";
    const string simTimeSchema = "RCS.E2K.TMS.SUBTYPE_XML_SESSION_SIMULATION_TIME.V1";

    const string connectTimetableToTrainSchema = "RCS.E2K.TMS.CONNECT_TRAIN_REQUEST.V2";
    const string disconnectTimetableFromTrainSchema = "RCS.E2K.TMS.DEALLOCATE_TRAIN_REQUEST.V2";
    const string initialDataRequestSchema = "RCS.E2K.TMS.SUBTYPE_XML_SESSION_INITIAL_DATA_REQ.V1";
    const string plainTrainListSchema = "RCS.E2K.TMS.SUBTYPE_XML_SESSION_PLAIN_TRAIN_DATA.V1";
    const string authorityFailureSchema = "RCS.E2K.TMS.AUTHORITY.FAILURE.V1";

    // Messages
    const string userAuthorityRequestMsgType = "users";
    const string userAuthoritiesMsgType = "users";

    const string activeServiceRequestMsgType = "ActiveServiceRequest";
    const string activeServiceMsgType = "ActiveService";
    const string nonActiveServiceMsgType = "NonActiveService";

    const string scheduledDaysRequestMsgType = "DayCodesReq";
    const string scheduledDaysMsgType = "ScheduledDayList";
    const string stationsRequestMsgType = "TreeStationReq";
    const string stationsMsgType = "StationList";
    const string stationPlatformsRequestMsgType = "StationReq";
    const string platformRequestMsgType = "PlatformReq";
    const string platformInfoMsgType = "PlatformList";
    const string linesRequestMsgType = "TreeLineReq";
    const string linesMsgType = "LineList";
    const string scheduledServicesRequestMsgType = "TreeServiceReq";
    const string serviceInfoRequestMsgType = "ServiceReq";
    const string serviceInfoMsgType = "ServiceList";        // Common to scheduledServicesRequestMsgType and serviceInfoRequestMsgType!
    const string tripInfoRequestMsgType = "MultiTripReq";
    const string tripInfoMsgType = "TripList";
    const string timetableRequestMsgType = "TimeTableReq";
    const string timetableMsgType = "ATRTimeTable";
    const string actionTypeListRequestMsgType = "SUBTYPE_SESSION_DEFAULT_ACTION_LIST_REQUEST";
    const string actionTypeListMsgType = "ActionTypeList";
    const string serviceForecastUpdateMsgType = "ServiceForecast";
    const string movementTemplateRequestMsgType = "TripTemplatesReq";
    const string movementTemplateMsgType = "TripMovementTemplateList";

    const string tsuiHelloRequestMsgType = "TSUIHelloRequest";
    const string tsuiHelloMsgType = "TSUIHello";
    const string simTimeMsgType = "SimTime";

    const string connectTimetableToTrainMsgType = "ConnectTrainReq";
    const string disconnectTimetableFromTrainMsgType = "DeallocateTrainReq";
    const string initialDataRequestMsgType = "InitialDataReq";
    const string plainTrainListMsgType = "PlainTrainList";
    const string authorityFailureMsgType = "AuthorityFailure";

    // Subscriptions
    static readonly Subscription UserAuthoritiesSubscription = new(userAuthoritiesChannel, userAuthoritiesMsgType);

    static readonly Subscription ActiveServiceSubscription = new(timetableInfoChannel, activeServiceMsgType);
    static readonly Subscription NonActiveServiceSubscription = new(timetableInfoChannel, nonActiveServiceMsgType);
    static readonly Subscription ActiveServiceSubscription2 = new(regulationInfoChannel, activeServiceMsgType);
    static readonly Subscription NonActiveServiceSubscription2 = new(regulationInfoChannel, nonActiveServiceMsgType);

    static readonly Subscription ScheduledDaysSubscription = new(timetableInfoChannel, scheduledDaysMsgType);
    static readonly Subscription StationSubscription = new(timetableInfoChannel, stationsMsgType);
    static readonly Subscription PlatformInfoSubscription = new(timetableInfoChannel, platformInfoMsgType);
    static readonly Subscription LinesSubscription = new(timetableInfoChannel, linesMsgType);
    static readonly Subscription ServiceInfoSubscription = new(timetableInfoChannel, serviceInfoMsgType);    // Common to scheduledServicesRequestMsgType and serviceInfoRequestMsgType
    static readonly Subscription TripInfoSubscription = new(timetableInfoChannel, tripInfoMsgType);
    static readonly Subscription ATRTimetableSubscription = new(timetableInfoChannel, timetableMsgType);
    static readonly Subscription ActionListSubscription = new(timetableInfoChannel, actionTypeListMsgType);
    static readonly Subscription AuthorityFailureSubscription = new(timetableInfoChannel, authorityFailureMsgType);
    static readonly Subscription MovementTemplateSubscription = new(timetableInfoChannel, movementTemplateMsgType);

    static readonly Subscription CommonScheduledDaysSubscription = new(commonTimetableInfoChannel, scheduledDaysMsgType);
    static readonly Subscription CommonServiceInfoSubscription = new(commonTimetableInfoChannel, serviceInfoMsgType);    // Common to scheduledServicesRequestMsgType and serviceInfoRequestMsgType
    static readonly Subscription CommonTripInfoSubscription = new(commonTimetableInfoChannel, tripInfoMsgType);

    static readonly Subscription TSUIHelloRequestSubscription = new(regulationInfoChannel, tsuiHelloRequestMsgType);
    static readonly Subscription SimTimeSubscription = new(regulationInfoChannel, simTimeMsgType);
    static readonly Subscription PlainTrainListSubscription = new(regulationInfoChannel, plainTrainListMsgType);

    private Dictionary<uint, ScheduledPlanKey> tripRequests = new();
    private int lastRequestedTripId = 0;
    private enum DeallocationType { NORMAL_DEALLOCATION = 1, CANCEL_REFORM };

    ////////////////////////////////////////////////////////////////////////////////

    public TimetableHandler(Connection connection, DataHandler dataHandler) : base(connection, dataHandler)
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
                Connection.Subscribe(UserAuthoritiesSubscription, messageProcessor, OnUserAuthorities);
                Connection.Subscribe(ActiveServiceSubscription, messageProcessor, OnActiveService);
                Connection.Subscribe(NonActiveServiceSubscription, messageProcessor, OnNonActiveService);
                Connection.Subscribe(ActiveServiceSubscription2, messageProcessor, OnActiveService);
                Connection.Subscribe(NonActiveServiceSubscription2, messageProcessor, OnNonActiveService);
                Connection.Subscribe(ScheduledDaysSubscription, messageProcessor, OnScheduledDays);
                Connection.Subscribe(StationSubscription, messageProcessor, OnStations);
                Connection.Subscribe(PlatformInfoSubscription, messageProcessor, OnPlatforms);
                Connection.Subscribe(LinesSubscription, messageProcessor, OnLines);
                Connection.Subscribe(ServiceInfoSubscription, messageProcessor, OnServices);
                Connection.Subscribe(TripInfoSubscription, messageProcessor, OnTrips);
                Connection.Subscribe(ActionListSubscription, messageProcessor, OnActionList);
                Connection.Subscribe(AuthorityFailureSubscription, messageProcessor, OnAuthorityFailure);
                Connection.Subscribe(MovementTemplateSubscription, messageProcessor, OnMovementTemplates);

                // Subscriptions to global channels
                Connection.Subscribe(CommonScheduledDaysSubscription, messageProcessor, OnScheduledDays);
                Connection.Subscribe(CommonServiceInfoSubscription, messageProcessor, OnServices);
                Connection.Subscribe(CommonTripInfoSubscription, messageProcessor, OnTrips);
                Connection.Subscribe(ATRTimetableSubscription, messageProcessor, OnTimetable);
                Connection.Subscribe(PlainTrainListSubscription, messageProcessor, OnPlainTrainList);
                Connection.Subscribe(TSUIHelloRequestSubscription, messageProcessor, OnTSUIHello);
                Connection.Subscribe(SimTimeSubscription, messageProcessor, OnSimTime);
                break;

            case MessagingStateSubscription.MessagingInactive:
                Connection.Unsubscribe(UserAuthoritiesSubscription);
                Connection.Unsubscribe(ActiveServiceSubscription);
                Connection.Unsubscribe(NonActiveServiceSubscription);
                Connection.Unsubscribe(ActiveServiceSubscription2);
                Connection.Unsubscribe(NonActiveServiceSubscription2);
                Connection.Unsubscribe(ScheduledDaysSubscription);
                Connection.Unsubscribe(StationSubscription);
                Connection.Unsubscribe(PlatformInfoSubscription);
                Connection.Unsubscribe(LinesSubscription);
                Connection.Unsubscribe(ServiceInfoSubscription);
                Connection.Unsubscribe(TripInfoSubscription);
                Connection.Unsubscribe(ActionListSubscription);
                Connection.Unsubscribe(AuthorityFailureSubscription);
                Connection.Unsubscribe(MovementTemplateSubscription);

                // Subscriptions to global channels
                Connection.Unsubscribe(CommonScheduledDaysSubscription);
                Connection.Unsubscribe(CommonServiceInfoSubscription);
                Connection.Unsubscribe(CommonTripInfoSubscription);
                Connection.Unsubscribe(ATRTimetableSubscription);
                Connection.Unsubscribe(PlainTrainListSubscription);
                Connection.Unsubscribe(TSUIHelloRequestSubscription);
                Connection.Unsubscribe(SimTimeSubscription);
                break;
        }
    }

    ////////////////////////////////////////////////////////////////////////////////

    protected override void MessagingActivated()
    {
        HandleSubscriptions(MessagingStateSubscription.MessagingActive);
        Log.Information("TimetableHandler: Message processing activated: accepting messages");

        SendUserAuthorityRequestMessage();
        SendActiveServiceRequest();

        // Add callbacks for DataHandler's notifications/requests
        DataHandler.SendInitialDataRequest += SendInitialDataRequest;
        DataHandler.SendConnectTimetableToTrain += SendConnectTimetableToTrain;
        DataHandler.SendDisconnectTimetableFromTrain += SendDisconnectTimetableFromTrain;
        //DataHandler.NotifyEstimationPlanChanged += SendForecast;  // We do not send forecast to TMS any more!
        DataHandler.SendScheduledServicesRequest += SendScheduledServicesRequest;
    }

    protected override void MessagingDeactivated()
    {
        HandleSubscriptions(MessagingStateSubscription.MessagingInactive);
        Log.Information("TimetableHandler: Message processing deactivated: rejecting messages");

        // Remove callbacks for DataHandler's notifications
        DataHandler.SendInitialDataRequest -= SendInitialDataRequest;
        DataHandler.SendConnectTimetableToTrain -= SendConnectTimetableToTrain;
        DataHandler.SendDisconnectTimetableFromTrain -= SendDisconnectTimetableFromTrain;
        //DataHandler.NotifyEstimationPlanChanged -= SendForecast;  // We do not send forecast to TMS any more!
        DataHandler.SendScheduledServicesRequest -= SendScheduledServicesRequest;
    }

    ////////////////////////////////////////////////////////////////////////////////

#pragma warning disable CS8602 // Dereference of a possibly null reference. try-catch in caller will handle missing mandatory fields in run-time

    private void OnUserAuthorities(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        /*
         <rcsMsg>
	        <hdr>
		        <schema>RCS.E2K.TMS.UserSessionClient.V1</schema>
		        <sender>RCS.E2K.TMS.TSCHED</sender>
		        <utc>20220810T123156</utc>
		        <scnt>101</scnt>
	        </hdr>
	        <data>
		        <users>
			        <user name="ConflictManagementService" workstation="ConflictManagementService" guid="ConflictManagementService-ConflictManagementService:0c3d0220-fd8b-4744-9bea-f77bfce5e425" username="ConflictManagementService">
				        <authtickets>
					        <authticket username="tms.authoritytickets.ConflictManagementService"/>
					        <capabilities>
						        <capability type="2" username="tms.capability.service.deallocate"/>
						        <objects>
							        <object>1</object>
						        </objects>
						        <capability type="1" username="tms.capability.service.allocate"/>
						        <objects>
							        <object>1</object>
						        </objects>
					        </capabilities>
				        </authtickets>
			        </user>
                    ...
                </users>
            </data>
        */

        if (!AllowMessageProcessing)
            return;

        try
        {
            var schema = msgProperties["rcsschema"];

            if (schema == userAuthoritiesSchema)
            {
                foreach (var userNode in msg.Elements())
                {
                    if (userNode.Name == "user")
                    {
                        string name = userNode.Attribute("name").Value;
                        string userName = userNode.Attribute("username").Value;

                        if (name == Connection.ServiceId || userName == Connection.ServiceId)
                        {
                            string userGuid = userNode.Attribute("guid").Value;

                            if (DataHandler.TmsUserGuid != userGuid)
                            {
                                Log.Information(string.Format($"User authorities received: User GUID of '{Connection.ServiceId}' is changed from '{DataHandler.TmsUserGuid}' to {userGuid}"));
                                DataHandler.TmsUserGuid = userGuid;
                            }
                        }
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

    private void OnActiveService(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        if (!AllowMessageProcessing)
            return;

        try
        {
            var schema = msgProperties["rcsschema"];

            bool responseToOurRequest = schema == activeServiceSchema;
            bool spontaneousStateResponse = schema == activeServiceSchema2;

            if (responseToOurRequest || spontaneousStateResponse)
            {
                // May need this later
                string changed = msg.Element("ServiceStateChanged").Value;
                Log.Information(string.Format($"TimetableHandler: Active service channel ({schema}). Service state was changed: ") + changed);

                //TODO: should we check, if service state was really changed and start timetable request only based on that information?
                //      we switched to Online and tscheduler was running: state was not changed
                //      we were Online and tscheduler started: state was changed
                //      we were Online and tscheduler was running (spontaneous update message): state was not changed - Request????

                DataHandler.TimetableUpdateStarted();

                DataHandler.SetScheduledPlansRequested(true);
                lastRequestedTripId = 0;

                SendUserAuthorityRequestMessage();
                SendActionListRequest();
                SendScheduledDaysRequest();
                SendStationsRequest();
                SendMovementTemplateRequest();            }
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

    private void OnNonActiveService(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        if (!AllowMessageProcessing)
            return;

        try
        {
            var schema = msgProperties["rcsschema"];

            if (schema == nonActiveServiceSchema || schema == nonActiveServiceSchema2)
            {
                // May need this later
                string changed = msg.Element("ServiceStateChanged").Value;
                Log.Information(string.Format($"TimetableHandler: Inactive service channel ({schema}). Service state was changed: ") + changed);

                //TODO: should we remove all timetable data and stop all operation?
                // Probably not, this may be TMS cluster switchover or some other oddity
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

    private void OnScheduledDays(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        if (!AllowMessageProcessing)
            return;

        try
        {
            var schema = msgProperties["rcsschema"];

            if (schema == scheduledDaysSchema)
            {
                var serializer = new XmlSerializer(typeof(ScheduledDayList));
                var scheduledDayList = (ScheduledDayList?)serializer.Deserialize(msg.CreateReader());

                Log.Debug($"Scheduled days received");

                if (scheduledDayList != null)
                {
                    Log.Debug($"Scheduled days count: {scheduledDayList.scheduledDays.Count}");

                    if (scheduledDayList.scheduledDays.Count > 0)
                    {
                        foreach (var scheduledDayItem in scheduledDayList.scheduledDays)
                        {
                            ActionTime startTime = new();
                            if (startTime.InitFromDateStringAndTime($"{scheduledDayItem.startYear:D4}{scheduledDayItem.startMonth:D2}{scheduledDayItem.startDay:D2}"))
                            {
                                Log.Debug($"Scheduled day {scheduledDayItem.scheduledDayCode}: start time {startTime}");

                                // Only unarchived scheduled days beginning from yesterday are taken into account
                                //if (!scheduledDayItem.isArchived) // Let's now relax this a little bit to be able to use old scheduled days from TMS
                                if (!scheduledDayItem.isArchived && startTime.AsDateTime() >= (DateTime.UtcNow - new TimeSpan(2, 0, 0, 0)))
                                {
                                    ScheduledDay scheduledDay = DataHandler.UpdateScheduledDay(startTime, scheduledDayItem);

                                    if (scheduledDay.IsValid())
                                        SendLinesRequest(scheduledDay.ScheduledDayCode);
                                    else
                                        Log.Error($"Invalid scheduled day received: {scheduledDayItem.scheduledDayCode}");

                                    //SendTimetableRequest(dayCode);
                                }
                            }
                        }
                    }
                    else
                    {
                        //TODO: what to do?
                        Log.Warning("No scheduled days exist");
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

    private void OnStations(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        if (!AllowMessageProcessing)
            return;

        try
        {
            var schema = msgProperties["rcsschema"];

            if (schema == stationSchema)
            {
                var serializer = new XmlSerializer(typeof(StationList));
                var stationList = (StationList?)serializer.Deserialize(msg.CreateReader());

                if (stationList != null)
                {
                    Log.Debug($"Stations and platforms received, # of stations: {stationList.stations.Count}");

                    DataHandler.CreateStationsAndPlatforms(stationList);

                    foreach (var station in stationList.stations)
                    {
                        SendPlatformsRequest(DataHandler.ScheduledDays.ElementAt(0).Value.ScheduledDayCode, station.id);     //TODO!!!
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

    private void OnPlatforms(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        if (!AllowMessageProcessing)
            return;

        try
        {
            var schema = msgProperties["rcsschema"];

            if (schema == platformInfoSchema)
            {
                var serializer = new XmlSerializer(typeof(PlatformList));
                var platformList = (PlatformList?)serializer.Deserialize(msg.CreateReader());

                if (platformList != null)
                {
                    Log.Debug($"Platform info received");

                    foreach (var platform in platformList.platforms)
                    {
                        var actionId = platform.actionID;
                        var platformName = platform.platformName;
                        var platformActionName = platform.actionName;
                        var arrivalTimeSecs = platform.arrivalTimeSecs;
                        var departureTimeSecs = platform.departureTimeSecs;
                        var diffToTimetable = platform.diffToTimeTable;
                        var trainId = platform.trainID;
                        var serviceId = platform.serviceID;
                        var tripId = platform.tripID;

                        Log.Debug($"  Platform {platformName}: actionId {actionId}, platform action name {platformActionName}, train ID {trainId}, service ID {serviceId}, trip ID {tripId}, arrival secs {arrivalTimeSecs}, departure secs {departureTimeSecs}, diff {diffToTimetable}");

                        //DataHandler.UpdatePlatformDynamicTimetable(platform);
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

    private void OnLines(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        if (!AllowMessageProcessing)
            return;

        try
        {
            var schema = msgProperties["rcsschema"];

            if (schema == linesSchema)
            {
                var serializer = new XmlSerializer(typeof(LineList));
                var lineList = (LineList?)serializer.Deserialize(msg.CreateReader());

                if (lineList != null)
                {
                    //DataHandler.CreateOrUpdateLines(lineList);
                    //...
                    Log.Debug($"Lines received");

                    if (lineList.lines != null)
                    {
                        foreach (var line in lineList.lines)
                        {
                            var name = line.name;
                            var id = line.id.ToString();
                            var dayCode = line.dayCode.ToString();
                            Log.Debug($"Line name {name}, id {id} - day code {dayCode}");

                            SendScheduledServicesRequest(id, dayCode);
                        }
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

    private void OnServices(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        if (!AllowMessageProcessing)
            return;

        try
        {
            // These messages may be responses to our own requests, or spontaneous messages informing timetable (service) changes
            var schema = msgProperties["rcsschema"];

            if (schema == scheduledServicesSchema)
            {
                var serializer = new XmlSerializer(typeof(ServiceNodeDataList));
                var serviceList = (ServiceNodeDataList?)serializer.Deserialize(msg.CreateReader());

                if (serviceList != null)
                {
                    if (serviceList.services != null && serviceList.services.Count > 0)
                    {
                        DataHandler.ScheduledServicesUpdated(serviceList);

                        foreach (var service in serviceList.services)
                        {
                            if (service != null && service.serviceID != null)
                                SendServicesRequest(service.serviceID, "");
                        }
                    }
                    else
                    {
                        Log.Warning("No scheduled services in received message!");
                    }
                }
            }
            else if (schema == serviceInfoSchema)
            {
                var serializer = new XmlSerializer(typeof(ServiceList));
                var serviceList = (ServiceList?)serializer.Deserialize(msg.CreateReader());

                if (serviceList != null)
                {
                    var moreInfoNeededForService = DataHandler.ServicesUpdated(serviceList);

                    foreach (var service in moreInfoNeededForService)
                    {
                        SendTripInfoRequest(service);
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

    private void OnTrips(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        if (!AllowMessageProcessing)
            return;

        try
        {
            var schema = msgProperties["rcsschema"];

            if (schema == tripInfoSchema)
            {
                var serializer = new XmlSerializer(typeof(TripList));
                TripList? tripList = new();

                try
                {
                    tripList = (TripList?)serializer.Deserialize(msg.CreateReader());
                }
                catch (Exception)
                {
                }

                if (tripList != null)
                {
                    var reqNo = tripList.ReqID;
                    ScheduledPlanKey? scheduledPlanKey = this.tripRequests.ContainsKey(reqNo) ? this.tripRequests[reqNo] : null;

                    DataHandler.TripsUpdated(scheduledPlanKey, tripList);

                    // Is this the last trip requested? If so, the timetable update is complete, whether it was requested by us, or was update from TMS
                    var lastTripItem = tripList.trips.Count == 0 ? 0 : tripList.trips?.Last().tripID;
                    if (lastRequestedTripId != 0 && lastTripItem == lastRequestedTripId)
                    {
                        lastRequestedTripId = 0;

                        DataHandler.TimetableUpdateComplete();

                        if (DataHandler.IsScheduledPlansRequestPending())
                            DataHandler.SetScheduledPlansRequested(false);
                    }
                }
            }
            else
            {
                Log.Warning($"Unknown message schema: {schema}");
            }
        }
        catch (Exception ex)
        {
            Log.Error("Parsing of XML message failed: {0}", ex.ToString());
        }
    }

    private void OnTimetable(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        if (!AllowMessageProcessing)
            return;

        // This is the timetable tsched is sending periodically and when needed (don't know when) when it calculates the regulated timetable itself
        // This contains historic timetable and predictions
        // May need this at some point

        try
        {
            var schema = msgProperties["rcsschema"];

            if (schema == timetableSchema)
            {
                var serializer = new XmlSerializer(typeof(ATRTimeTable));
                var atrTimetable = (ATRTimeTable?)serializer.Deserialize(msg.CreateReader());

                if (atrTimetable != null)
                {
                    Log.Debug($"ATR timetable received");

                    var x = atrTimetable.isoDateTime;
                    var xx = atrTimetable.startIndex;
                    var xxx = atrTimetable.totalCount;
                    var t = atrTimetable.trains.Count;
                    Log.Information($"ATRTimeTable: {x}, {xx}, {xxx}, {t}");

                    if (t > 0)
                    {
                        var n = atrTimetable.trains[0].trainName;
                        var nn = atrTimetable.trains[0].tmsPTI.serviceDBID;
                        var nnn = atrTimetable.trains[0].tmsPTI.tripDBID;

                        if (nn != 0 && nnn != 0)
                            Log.Debug("Found allocation?");

                        Log.Debug($"ATRTimeTable: train {n}: {nn}, {nnn}");
                    }

                    //DataHandler.CreateOrUpdateAtrTimetable(atrTimetable);
                    //...
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

    private void OnPlainTrainList(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        if (!AllowMessageProcessing)
            return;

        try
        {
            var schema = msgProperties["rcsschema"];

            if (schema == plainTrainListSchema)
            {
                var serializer = new XmlSerializer(typeof(PlainTrainList));
                var plainTrainList = (PlainTrainList?)serializer.Deserialize(msg.CreateReader());

                if (plainTrainList != null)
                {
                    DataHandler.UpdateTrainOrTimetableData(plainTrainList);
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

    private void OnActionList(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        if (!AllowMessageProcessing)
            return;

        try
        {
            var schema = msgProperties["rcsschema"];

            if (schema == actionTypeListSchema)
            {
                var serializer = new XmlSerializer(typeof(ActionTypeList));
                var actionTypeList = (ActionTypeList?)serializer.Deserialize(msg.CreateReader());

                if (actionTypeList != null)
                {
                    this.DataHandler.CreateTypes(actionTypeList);
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

    private void OnTSUIHello(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        if (!AllowMessageProcessing)
            return;

        try
        {
            var schema = msgProperties["rcsschema"];

            if (schema == tsuiHelloRequestSchema)
            {
                var serializer = new XmlSerializer(typeof(TSUIHelloRequest));
                var tsuiHelloRequest = (TSUIHelloRequest?)serializer.Deserialize(msg.CreateReader());

                if (tsuiHelloRequest != null)
                {
                    SendTSUIHello(tsuiHelloRequest);

                    //TODO: What to do with...
                    //tsuiHelloRequest.serviceStateChanged;
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

    private void OnAuthorityFailure(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        /* FAILURE TO ALLOCATE TIMETABLE!
        2022-08-11 08:43:58.7460 INFO | Sent connect timetable to train request RCS.E2K.TMS.CONNECT_TRAIN_REQUEST.V2/ConnectTrainReq for service ID 2407, trip ID 2422 and train GUID Lenovo-P3501cb74227-19b6-4eb8-973b-a8bcdc0660fa as user 
        2022-08-11 08:43:58.7637 DEBUG | Received message: <?xml version="1.0"?>
        <rcsMsg><hdr><schema>RCS.E2K.TMS.AUTHORITY.FAILURE.V1</schema><sender>RCS.E2K.TMS.TSCHED</sender><utc>20220811T054358</utc><scnt>172</scnt></hdr><data><AuthorityFailure><RequestSchema>RCS.E2K.TMS.CONNECT_TRAIN_REQUEST.V2</RequestSchema><RequestHandler>TimetableSession</RequestHandler></AuthorityFailure></data></rcsMsg>
        */

        if (!AllowMessageProcessing)
            return;

        try
        {
            var schema = msgProperties["rcsschema"];

            if (schema == authorityFailureSchema)
            {
                var serializer = new XmlSerializer(typeof(AuthorityFailure));
                var authorityFailure = (AuthorityFailure?)serializer.Deserialize(msg.CreateReader());

                if (authorityFailure != null)
                {
                    if (authorityFailure.requestSchema == connectTimetableToTrainSchema)
                        DataHandler.TrainConnectionToTimetableFailed();
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

    private void OnMovementTemplates(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        if (!AllowMessageProcessing)
            return;

        try
        {
            var schema = msgProperties["rcsschema"];

            if (schema == movementTemplateSchema)
            {
                var serializer = new XmlSerializer(typeof(MovementTemplateList));
                var movementTemplateList = (MovementTemplateList?)serializer.Deserialize(msg.CreateReader());

                if (movementTemplateList != null)
                {
                    DataHandler.UpdateMovementTemplates(movementTemplateList);
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

    private void OnSimTime(Dictionary<string, string> hdr, XElement msg, Dictionary<string, string> msgProperties, IMessage rawMsg)
    {
        // We don't do anything with this as of now, this is here just for reducing the logging
    }

    ////////////////////////////////////////////////////////////////////////////////

    private string CreateMessageHeaders(string replytoDestination, string schema, out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties)
    {
        hdr = new();
        msgProperties = new();

        string messageId = Connection.CreateNewMessageId();

        hdr["sender"] = Connection.ServiceId;
        hdr["schema"] = schema;
        hdr["replyTo"] = replytoDestination;
        hdr["userguid"] = ""; // tsched crashes with real user guid...

        msgProperties["rcsschema"] = schema;
        msgProperties["message-id"] = messageId;
        msgProperties["reply-to"] = replytoDestination;
        msgProperties["rcsreplyto"] = replytoDestination;

        return messageId;
    }

    private string CreateTimetableInfoMessageHeaders(string schema, out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties)
    {
        return CreateMessageHeaders(timetableInfoDestination, schema, out hdr, out msgProperties);
    }

    private string CreateRegulationInfoMessageHeaders(string schema, out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties)
    {
        return CreateMessageHeaders(regulationInfoDestination, schema, out hdr, out msgProperties);
    }

    ////////////////////////////////////////////////////////////////////////////////

    private void SendUserAuthorityRequestMessage()
    {
        if (!AllowMessageProcessing)
            return;

        /*
	        <users>
		        <user name="XXXX" username="ZZZ" workstation="YYY"> <!-- can have N users -->
			        <authorities>
				        <authority>A1</authority>
				        <authority>A2</authority>
			        </authorities>
		        </user>
	        </users>
        */

        CreateMessageHeaders("", userAuthorityRequestSchema, out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties);

        try
        {
            XElement msgNode = new(userAuthorityRequestMsgType,
                                   new XElement("user",
                                        new XAttribute("name", Connection.ServiceId),
                                        new XAttribute("username", Connection.ServiceId),
                                        new XAttribute("workstation", Connection.ServiceId),
                                        new XElement("authorities",
                                            new XElement("authority", Connection.ServiceId)
                                   )));

            var message = messageProcessor.CreateMessage(hdr, msgNode, msgProperties);

            Connection.SendMessage(userAuthorityRequestChannel, message);

            Log.Information($"Sent user authority message: {userAuthorityRequestSchema}/{userAuthorityRequestMsgType}");
        }
        catch (Exception e)
        {
            Log.Error("Internal error in XML message creation: {0}", e.ToString());
        }
    }

    private void SendActiveServiceRequest()
    {
        if (!AllowMessageProcessing)
            return;

        // Request the answer to our own channel
        CreateTimetableInfoMessageHeaders(activeServiceRequestSchema, out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties);

        try
        {
            XElement msgNode = new(activeServiceRequestMsgType,
                                   new XElement("Counter", Connection.GetNewRequestNumber())
                                  );

            var message = messageProcessor.CreateMessage(hdr, msgNode, msgProperties);

            Connection.SendMessage(timetableInfoRequestChannel, message);

            Log.Information($"Sent state service request: {activeServiceRequestSchema}/{activeServiceRequestMsgType}");
        }
        catch (Exception e)
        {
            Log.Error("Internal error in XML message creation: {0}", e.ToString());
        }
    }

    private void SendStationsRequest()
    {
        if (!AllowMessageProcessing)
            return;

        CreateTimetableInfoMessageHeaders(stationRequestSchema, out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties);

        try
        {
            XElement msgNode = new(stationsRequestMsgType,
                                   new XElement("ReqID", Connection.GetNewRequestNumber()),
                                   new XElement("LineID", "0")  // All stations and platforms are always returned, whatever LineID you send here!
                                  );

            var message = messageProcessor.CreateMessage(hdr, msgNode, msgProperties);

            Connection.SendMessage(timetableInfoRequestChannel, message);

            Log.Information($"Sent stations request: {stationRequestSchema}/{stationsRequestMsgType}");
        }
        catch (Exception e)
        {
            Log.Error("Internal error in XML message creation: {0}", e.ToString());
        }
    }

    private void SendActionListRequest()
    {
        if (!AllowMessageProcessing)
            return;

        CreateTimetableInfoMessageHeaders(actionTypeListRequestSchema, out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties);

        try
        {
            XElement msgNode = new(actionTypeListRequestMsgType,
                                   new XElement("Counter", Connection.GetNewRequestNumber())
                                  );

            var message = messageProcessor.CreateMessage(hdr, msgNode, msgProperties);

            Connection.SendMessage(timetableInfoRequestChannel, message);

            Log.Information($"Sent action list request: {actionTypeListRequestSchema}/{actionTypeListRequestMsgType}");
        }
        catch (Exception e)
        {
            Log.Error("Internal error in XML message creation: {0}", e.ToString());
        }
    }

    private void SendPlatformsRequest(int scheduleDaycode, int stationId)
    {
        if (!AllowMessageProcessing)
            return;

        CreateTimetableInfoMessageHeaders(stationPlatformsRequestSchema, out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties);

        try
        {
            XElement msgNode = new(stationPlatformsRequestMsgType,
                                   new XElement("ReqID", Connection.GetNewRequestNumber()),
                                   new XElement("dc", scheduleDaycode),
                                   new XElement("StationID", stationId)
                                  );

            var message = messageProcessor.CreateMessage(hdr, msgNode, msgProperties);

            Connection.SendMessage(timetableInfoRequestChannel, message);

            Log.Information($"Sent station platforms request: {stationPlatformsRequestSchema}/{stationPlatformsRequestMsgType} for station ID {stationId} on {scheduleDaycode}");
        }
        catch (Exception e)
        {
            Log.Error("Internal error in XML message creation: {0}", e.ToString());
        }
    }

    private void SendScheduledDaysRequest()
    {
        if (!AllowMessageProcessing)
            return;

        CreateTimetableInfoMessageHeaders(scheduledDaysRequestSchema, out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties);

        try
        {
            XElement msgNode = new(scheduledDaysRequestMsgType,
                                   new XElement("ReqID", Connection.GetNewRequestNumber())
                                  );

            var message = messageProcessor.CreateMessage(hdr, msgNode, msgProperties);

            Connection.SendMessage(timetableInfoRequestChannel, message);

            Log.Information($"Sent scheduled days request: {scheduledDaysRequestSchema}/{scheduledDaysRequestMsgType}");
        }
        catch (Exception e)
        {
            Log.Error("Internal error in XML message creation: {0}", e.ToString());
        }
    }

    private void SendLinesRequest(int scheduledDayCode)
    {
        if (!AllowMessageProcessing)
            return;

        CreateTimetableInfoMessageHeaders(linesRequestSchema, out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties);

        try
        {
            XElement msgNode = new(linesRequestMsgType,
                                   new XElement("ReqID", Connection.GetNewRequestNumber()),
                                   new XElement("dc", scheduledDayCode)
                                  );

            var message = messageProcessor.CreateMessage(hdr, msgNode, msgProperties);

            Connection.SendMessage(timetableInfoRequestChannel, message);

            Log.Information($"Sent lines request: {linesRequestSchema}/{linesRequestMsgType} for day code {scheduledDayCode}");
        }
        catch (Exception e)
        {
            Log.Error("Internal error in XML message creation: {0}", e.ToString());
        }
    }

    public void SendScheduledServicesRequest(string lineId, string scheduledDaycode)
    {
        if (!AllowMessageProcessing)
            return;

        CreateTimetableInfoMessageHeaders(scheduledServicesRequestSchema, out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties);

        try
        {
            XElement msgNode = new(scheduledServicesRequestMsgType,
                                   new XElement("ReqID", Connection.GetNewRequestNumber()),
                                   new XElement("LineID", lineId),
                                   new XElement("dc", scheduledDaycode)
                                  );

            var message = messageProcessor.CreateMessage(hdr, msgNode, msgProperties);
            Connection.SendMessage(timetableInfoRequestChannel, message);

            Log.Information($"Sent scheduled services request: {scheduledServicesRequestSchema}/{scheduledServicesRequestMsgType} for line {lineId} and day code {scheduledDaycode}");
        }
        catch (Exception e)
        {
            Log.Error("Internal error in XML message creation: {0}", e.ToString());
        }
    }

    private void SendServicesRequest(int serviceId, string tguid)
    {
        if (!AllowMessageProcessing)
            return;

        CreateTimetableInfoMessageHeaders(serviceInfoRequestSchema, out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties);

        try
        {
            XElement msgNode = new(serviceInfoRequestMsgType,
                                   new XElement("ReqID", Connection.GetNewRequestNumber()),
                                   new XElement("serid", serviceId),
                                   new XElement("tguid", tguid),
                                   new XElement("ActiveTrainInstance", "False")
                                  );

            var message = messageProcessor.CreateMessage(hdr, msgNode, msgProperties);
            Connection.SendMessage(timetableInfoRequestChannel, message);

            Log.Information($"Sent services request {serviceInfoRequestSchema}/{serviceInfoRequestMsgType} for service {serviceId} and tguid {tguid}");
        }
        catch (Exception e)
        {
            Log.Error("Internal error in XML message creation: {0}", e.ToString());
        }
    }

    private void SendTripInfoRequest(ServiceItem service)
    {
        // Do not request trips for deleted service
        if (!AllowMessageProcessing || service == null || service.reason == eServiceInfoReason.deleted)
            return;

        CreateTimetableInfoMessageHeaders(tripInfoRequestSchema, out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties);

        try
        {
            XElement msgNode = new(tripInfoRequestMsgType,
                                   new XElement("serid", service.serviceID),
                                   new XElement("RequestLevel", TripList.TripUpdateData.allTripdata),
                                   new XElement("RequestedListType", TripList.TripListType.tripsInTrainOrService),
                                   new XElement("tguid", service.tmsPTI == null ? "" : service.tmsPTI.trainGUID),
                                   new XElement("ActiveTrainInstance", "False")
                                  );

            XElement tripsNode = new("Trips");
            foreach (var tripAction in service.serviceActions)
            {
                if (tripAction is RunTripAction)
                {
                    var reqNo = Connection.GetNewRequestNumber();

                    var rta = (RunTripAction)tripAction;
                    tripsNode.Add(new XElement("Req", new XElement("ReqID", reqNo), new XElement("trid", rta.tripID)));

                    // Remember number of trip request to be able to map it later to correct scheduled plan
                    this.tripRequests.Add(uint.Parse(reqNo), ScheduledPlan.CreateKey(service.scheduledDayCode, service.name));

                    // Remember last requested trip ID
                    if (rta.tripID != null)
                        lastRequestedTripId = rta.tripID;

                    Log.Debug($"  Run trip action ({rta.tripNo}): trip ID {rta.tripID} plannedSecs {rta.plannedSecs}, trip code {rta.tripCode}, trip name {rta.tripName}: {rta.plannedStartSecs} - {rta.plannedEndSecs}");
                }

                if (tripAction is GlueAction)
                {
                    var ga = tripAction as GlueAction;
                    Log.Debug($"  Glue action: plannedSecs {ga.plannedSecs}");
                }
            }
            msgNode.Add(tripsNode);

            var message = messageProcessor.CreateMessage(hdr, msgNode, msgProperties);
            Connection.SendMessage(timetableInfoRequestChannel, message);

            Log.Information($"Sent trips request ({service.serviceActions.Count()}): {tripInfoRequestSchema}/{tripInfoRequestMsgType} for service {service.serviceID}");
        }
        catch (Exception e)
        {
            Log.Error("Internal error in XML message creation: {0}", e.ToString());
        }
    }

    private void SendTimetableRequest(string dayCode)
    {
        if (!AllowMessageProcessing)
            return;

        CreateTimetableInfoMessageHeaders(timetableRequestSchema, out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties);

        try
        {
            XElement msgNode = new(timetableRequestMsgType,
                                   new XElement("ReqID", Connection.GetNewRequestNumber()),
                                   new XElement("dc", dayCode),
                                   new XElement("includeDutyInfo", "False")
                                  );

            var message = messageProcessor.CreateMessage(hdr, msgNode, msgProperties);
            Connection.SendMessage(timetableInfoRequestChannel, message);

            Log.Information($"Sent timetable request: {timetableRequestSchema}/{timetableRequestMsgType} for day code {dayCode}");
        }
        catch (Exception e)
        {
            Log.Error("Internal error in XML message creation: {0}", e.ToString());
        }
    }

    private void SendTSUIHello(TSUIHelloRequest request)
    {
        if (!AllowMessageProcessing)
            return;

        CreateTimetableInfoMessageHeaders(tsuiHelloSchema, out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties);

        try
        {
            XElement msgNode = new(tsuiHelloMsgType,
                                   new XElement("RecvSubID", request.subID)
                                  );

            var message = messageProcessor.CreateMessage(hdr, msgNode, msgProperties);
            Connection.SendMessage(regulationInfoRequestChannel, message);

            //Log.Debug($"Sent TSUIHello response: {tsuiHelloSchema}/{tsuiHelloMsgType}");
        }
        catch (Exception e)
        {
            Log.Error("Internal error in XML message creation: {0}", e.ToString());
        }
    }

    private void SendMovementTemplateRequest()
    {
        if (!AllowMessageProcessing)
            return;

        CreateTimetableInfoMessageHeaders(movementTemplateRequestSchema, out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties);

        try
        {
            XElement msgNode = new(movementTemplateRequestMsgType,
                                    new XAttribute("incTrMv", false),
                                    new XElement("ReqID", Connection.GetNewRequestNumber())
                                  );

            var message = messageProcessor.CreateMessage(hdr, msgNode, msgProperties);
            Connection.SendMessage(timetableInfoRequestChannel, message);

            Log.Information($"Sent movement template request: {movementTemplateRequestSchema}/{movementTemplateRequestMsgType}");
        }
        catch (Exception e)
        {
            Log.Error("Internal error in XML message creation: {0}", e.ToString());
        }
    }

    ////////////////////////////////////////////////////////////////////////////////

    private void SendConnectTimetableToTrain(Train train, ScheduledPlan scheduledPlan, int tripId)
    {
        if (!AllowMessageProcessing)
            return;

        /* 
        <rcsMsg>
            <hdr>
                <schema>RCS.E2K.TMS.CONNECT_TRAIN_REQUEST.V2</schema>
                <replyTo>topic://jms.topic.Lenovo-P350.TS-UI</replyTo>
                <sender>RCS.E2K.TMS.TSUI</sender>
                <userguid>ProfEditOperator:_256257-Workstation_1:aec46092-7126-4f70-9b2b-0ec0fed325b8</userguid>
            </hdr>
            <data>
                <ConnectTrainReq>
                    <ReqID>216</ReqID>
                    <ActiveTrainInstance>False</ActiveTrainInstance>
                    <tguid>Lenovo-P350d981e092-acbd-46bc-ae5c-fcca2b8d5493</tguid>
                    <serid>1863</serid>
                    <trid>1865</trid>
                    <NodeID>0</NodeID>
                    <Allocation>3</Allocation>
                    <TripReformType>0</TripReformType>
                    <ReformTrain>False</ReformTrain>
                    <TripReformType>0</TripReformType>
                    <FormStart>False</FormStart>
                    <ChangeTrip>False</ChangeTrip>
                    <AllowFutureLocationBasedOnTrips>False</AllowFutureLocationBasedOnTrips>
                    <AllowFutureLocation>False</AllowFutureLocation>
                </ConnectTrainReq>
            </data>
        </rcsMsg>
        */

        if (!train.IsValid() || !scheduledPlan.IsValid() || !scheduledPlan.HasTripWithTripId(tripId))
            return;

        var currentlyAllocatedTripId = DataHandler.GetTripIdOfAllocatedTimetable(train, scheduledPlan);

        var serviceId = scheduledPlan.Id;
        bool tripChanges = currentlyAllocatedTripId != 0 && tripId != currentlyAllocatedTripId;

        //CreateTimetableInfoMessageHeaders(connectTimetableToTrainSchema, out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties);
        CreateMessageHeaders("", connectTimetableToTrainSchema, out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties);

        hdr["userguid"] = DataHandler.TmsUserGuid;

        try
        {
            // Don't know the meaning of all these nodes, now just set the values as they are from normal allocation from TSUI
            XElement msgNode = new(connectTimetableToTrainMsgType,
                                   new XElement("ReqID", Connection.GetNewRequestNumber()),
                                   new XElement("ActiveTrainInstance", false),
                                   new XElement("tguid", train.Guid),
                                   new XElement("serid", serviceId),
                                   new XElement("trid", tripId),
                                   new XElement("NodeID", 0),
                                   new XElement("Allocation", 3),
                                   new XElement("TripReformType", 0),
                                   new XElement("ReformTrain", "False"),
                                   new XElement("TripReformType", 0),       // Is this intentional or accidental copy?
                                   new XElement("FormStart", "False"),
                                   new XElement("ChangeTrip", tripChanges ? "True" : "False"),
                                   new XElement("AllowFutureLocationBasedOnTrips", "False"),
                                   new XElement("AllowFutureLocation", "False")
                                  );

            var message = messageProcessor.CreateMessage(hdr, msgNode, msgProperties);
            Connection.SendMessage(timetableInfoRequestChannel, message);

            Log.Information($"Sent connect timetable to train request {connectTimetableToTrainSchema}/{connectTimetableToTrainMsgType} for service ID {serviceId}, trip ID {tripId} and train GUID {train.Guid} as user '{DataHandler.TmsUserGuid}'");
        }
        catch (Exception e)
        {
            Log.Error("Internal error in XML message creation: {0}", e.ToString());
        }
    }

    private void SendDisconnectTimetableFromTrain(Train train)
    {
        if (!AllowMessageProcessing)
            return;

        if (train == null || !train.IsValid() || train.AllocatedTrainGuid == null || train.AllocatedTrainGuid == "")
            return;

        CreateMessageHeaders("", disconnectTimetableFromTrainSchema, out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties);

        hdr["userguid"] = DataHandler.TmsUserGuid;

        try
        {
            XElement msgNode = new(disconnectTimetableFromTrainMsgType,
                                   new XElement("tguid", train.AllocatedTrainGuid),
                                   new XElement("DeallocationType", (int) DeallocationType.NORMAL_DEALLOCATION)
                                  );

            var message = messageProcessor.CreateMessage(hdr, msgNode, msgProperties);
            Connection.SendMessage(timetableInfoRequestChannel, message);

            Log.Information($"Sent disconnect timetable from train request {disconnectTimetableFromTrainSchema}/{disconnectTimetableFromTrainMsgType} to train GUID {train.Guid} as user '{DataHandler.TmsUserGuid}'");
        }
        catch (Exception e)
        {
            Log.Error("Internal error in XML message creation: {0}", e.ToString());
        }
    }

    ////////////////////////////////////////////////////////////////////////////////

    private void SendInitialDataRequest()
    {
        if (!AllowMessageProcessing)
            return;

        // Request service/user authorities again, if not received previously. The user GUID changes every time authorities are requested.
        // Don't know what happens to authorities/GUIDs, when tscheduler is switched in Linux cluster to another computer
        if (DataHandler.TmsUserGuid == "")
            SendUserAuthorityRequestMessage();

        // Request other initial data
        CreateRegulationInfoMessageHeaders(initialDataRequestSchema, out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties);

        try
        {
            XElement msgNode = new(initialDataRequestMsgType,
                                   new XElement("RequestTrainInQueue", true)   // Include "virtual" trains, if set to "true" (default is true)
                                  );

            var message = messageProcessor.CreateMessage(hdr, msgNode, msgProperties);
            Connection.SendMessage(regulationInfoRequestChannel, message);

            //Log.Information($"Sent initial data request: {initialDataRequestSchema}/{initialDataRequestMsgType}");
        }
        catch (Exception e)
        {
            Log.Error("Internal error in XML message creation: {0}", e.ToString());
        }
    }

    private void SendForecast(EstimationPlan estimationPlan)
    {
        if (!AllowMessageProcessing)
            return;

        /*
            queue = 'jms.queue.TMS.TimetableHandlerServer'

            <?xml version="1.0" encoding="utf-8"?>
            <rcsMsg>
                <hdr>
                    <schema>RCS.E2K.TMS.ServiceForecastUpdate.V1</schema>
                    <sender>ConflictManagementService</sender>
                    <utc>20230101T010000</utc>
                    <userguid>....</userguid>   <!-- needs capability 4 for CMS in TMS DB join.... -table -->
                </hdr>
                <data>
                    <ServiceForecast>
                        <services>
                            <service trainGUID="2303031-2341-12312-123">
                                <trips>
                                    <trip id="2303041">
                                        <platforms>
                                            <platform id="123" name="mkpr">
                                                <ra>20221019T195919</ra> <!--regulatedArrival-->
                                                <rd>20221019T195949</rd> <!--regulatedDeparture-->
                                            </platform>
                                        </platforms>
                                    </trip>
                                </trips>
                            </service>
                        </services>
                    </ServiceForecast>
                </data>
            </rcsMsg>
        */

        Train? train = DataHandler.GetTrain(estimationPlan);

        if (train == null || estimationPlan == null || !estimationPlan.IsValid())
            return;

        ScheduledPlan? scheduledPlan = DataHandler.GetScheduledPlan(estimationPlan.ScheduledPlanKey);

        string serviceName = scheduledPlan != null ? scheduledPlan.Name : "";
        int previousTripId = 0;

        CreateTimetableInfoMessageHeaders(serviceForecastUpdateSchema, out Dictionary<string, string> hdr, out Dictionary<string, string> msgProperties);

        hdr["userguid"] = DataHandler.TmsUserGuid;

        try
        {
            XElement platformsNode = new("platforms");
            XElement tripsNode = new("trips");
            XElement tripNode;

            foreach (var timedLocation in estimationPlan.TimedLocations)
            {
                var tripId = timedLocation.TripId;
                var trip = scheduledPlan != null && scheduledPlan.HasTripWithTripId(tripId) ? scheduledPlan.GetTripByTripId(tripId) : null;
                var tripCode = trip != null ? trip.TripCode : "";
                var tripNumber = trip != null ? trip.TripNumber : 0;

                if (tripId != previousTripId)
                {
                    tripNode = new XElement("trip", new XAttribute("id", tripId), new XAttribute("tripCode", tripCode), new XAttribute("tripNumber", tripNumber));
                    tripsNode.Add(tripNode);
                    platformsNode = new XElement("platforms");
                    tripNode.Add(platformsNode);

                    previousTripId = tripId;
                }

                var platformNode = new XElement("platform", new XAttribute("id", timedLocation.Id), new XAttribute("name", timedLocation.Description));

                platformNode.Add(new XElement("ra", timedLocation.Arrival));
                platformNode.Add(new XElement("rd", timedLocation.Departure));

                platformsNode.Add(platformNode);
            }

            XElement msgNode = new(serviceForecastUpdateMsgType, new XElement("services", new XElement("service", new XAttribute("name", serviceName), new XAttribute("trainGUID", train.AllocatedTrainGuid), tripsNode)));

            Log.Error("######### REMOVE THIS LOGGING");
            Log.Error($"{msgNode.ToString()}");
            Log.Error("######### REMOVE THIS LOGGING");

            var message = messageProcessor.CreateMessage(hdr, msgNode, msgProperties);
            Connection.SendMessage(timetableInfoRequestChannel, message);
        }
        catch (Exception e)
        {
            Log.Error("Internal error in XML message creation: {0}", e.ToString());
        }
    }

#pragma warning restore CS8602 // Dereference of a possibly null reference.

}

