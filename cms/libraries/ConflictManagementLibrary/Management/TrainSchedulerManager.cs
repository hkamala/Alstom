using System;
using System.Collections.Generic;
using ConflictManagementLibrary.Communications;
using ConflictManagementLibrary.Helpers;
using ConflictManagementLibrary.Logging;
using ConflictManagementLibrary.Messages;
using ConflictManagementLibrary.Model.Conflict;
using ConflictManagementLibrary.Model.Movement;
using ConflictManagementLibrary.Model.Trip;
using Newtonsoft.Json;

namespace ConflictManagementLibrary.Management
{
    public class TrainSchedulerManager
    {
        #region Delegates
        public delegate void TripProcessorDelegate(Trip? theTrip, string theCommand);
        public TripProcessorDelegate? DoProcessTrip;
        #endregion
        #region Delegates
        public delegate void StatusProcessorDelegate(ConflictManagementMessages.ConflictResolutionStatus theStatus);
        public StatusProcessorDelegate? DoProcessStatus;
        #endregion
        #region Delegates
        public delegate void EventProcessorDelegate(ConflictManagementMessages.CmsEventMessage? theEvent);
        public EventProcessorDelegate? DoProcessEvent;
        #endregion
        #region Delegates
        public delegate void ForecastProcessorDelegate(Forecast? theForecast);
        public ForecastProcessorDelegate? DoProcessForecast;
        #endregion

        #region Declarations
        public List<IMessageJson> MyMessageTypes = new List<IMessageJson>();
        private readonly IMyLogger? _theLogger;
        private readonly AppExchangeManager? _exchangeManager;
        #endregion

        #region Constructor
        public static TrainSchedulerManager? CreateInstance(IMyLogger? theLogger, AppExchangeManager? theExchangeManager)
        {
            return new TrainSchedulerManager(theLogger, theExchangeManager);
        }
        private TrainSchedulerManager(IMyLogger? theLogger, AppExchangeManager? theExchangeManager)
        {
            _theLogger = theLogger;
            _exchangeManager = theExchangeManager;

            LoadMessageTypes();
            DoBindConsumers();
        }
        #endregion

        #region Initialization
        private void LoadMessageTypes()
        {
            MyMessageTypes.Add(new Messages.ConflictManagementMessages.SendAllTrips());
            MyMessageTypes.Add(new Messages.ConflictManagementMessages.DeleteTrip());
            MyMessageTypes.Add(new Messages.ConflictManagementMessages.AddNewTrip());
            MyMessageTypes.Add(new Messages.ConflictManagementMessages.UpdateTrip());
            MyMessageTypes.Add(new Messages.ConflictManagementMessages.DeleteTrip());
            MyMessageTypes.Add(new Messages.ConflictManagementMessages.TripAllocated());
            MyMessageTypes.Add(new Messages.ConflictManagementMessages.PublishForecast());

            MyMessageTypes.Add(ConflictManagementMessages.InitializeClient.CreateInstance());
            MyMessageTypes.Add(ConflictManagementMessages.ConflictResolutionStatus.CreateInstance());
            MyMessageTypes.Add(new ConflictManagementMessages.SendRoutePlanRequest());


            MyMessageTypes.Add(new Messages.ConflictManagementMessages.ResolutionAccept());
            MyMessageTypes.Add(new Messages.ConflictManagementMessages.ResolutionReject());
            MyMessageTypes.Add(new Messages.ConflictManagementMessages.OperatorDeleteTrip());
            MyMessageTypes.Add(new Messages.ConflictManagementMessages.OperatorConflictManagementControl());
            MyMessageTypes.Add(new Messages.ConflictManagementMessages.CmsEventMessage());
            MyMessageTypes.Add(new Messages.ConflictManagementMessages.SerializeTrip());

        }
        public void LinkDelegate(TripProcessorDelegate? processTrip, StatusProcessorDelegate? processStatus, EventProcessorDelegate? processEvent, ForecastProcessorDelegate processForecast)
        {
            this.DoProcessTrip = processTrip ?? throw new ArgumentNullException(nameof(processTrip));
            this.DoProcessStatus = processStatus ?? throw new ArgumentNullException(nameof(processStatus));
            this.DoProcessEvent = processEvent ?? throw new ArgumentNullException(nameof(processEvent));
            this.DoProcessForecast = processForecast ?? throw new ArgumentNullException(nameof(processForecast));
        }
        private void DoBindConsumers()
        {
            _exchangeManager?.BindMessageHandlerToConsumers(MessageHandler);
        }
        #endregion

        #region Message Processing
        private void MessageHandler(string theMessage)
        {
            try
            {
                ProcessMessage(IdentifyMessageType(theMessage), theMessage);
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        private IMessageJson IdentifyMessageType(string theMessage)
        {
            try
            {
                foreach (var m in MyMessageTypes)
                {
                    if (theMessage.Contains(m.ClassName)) return m;
                }
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }

            return null!;
        }
        private void ProcessMessage(IMessageJson theMessageType, string theMessage)
        {
            try
            {
                _theLogger?.LogInfo($@"Data Received...Message Number <{theMessageType.MessageNumber}> <Message Type {theMessageType.MessageName}> ");
                switch (theMessageType?.MessageNumber)
                {
                    //This message is from the TSUI to initialize the client with trips/conflicts from CMS
                    case "1000": //InitializeClient
                        {
                            ProcessMessage1000();
                            break;
                        }
                    case "1001": //ResolutionAccept
                        {
                            ProcessMessage1001(theMessage);
                            break;
                        }
                    case "1002": //ResolutionReject
                    {
                        ProcessMessage1002(theMessage);
                        break;
                    }
                    case "1003": //OperatorDeleteTrip
                    {
                        ProcessMessage1003(theMessage);
                        break;
                    }
                    case "1004": //OperatorConflictManagementControl
                    {
                        ProcessMessage1004(theMessage);
                        break;
                    }
                    case "1005": //CMS Event
                    {
                        ProcessMessage1005(theMessage);
                        break;
                    }
                    case "1006": //Serialize MockTrip
                    {
                        ProcessMessage1006(theMessage);
                        break;
                    }

                    case "1100": //ConflictResolutionStatus
                        {
                        ProcessMessage1100(theMessage);
                        break;
                    }
                    case "1200": //SendRoutePlanRequest
                        {
                        ProcessMessage1200(theMessage);
                        break;
                    }

                    case "2000": //SendAllTrips
                        {
                        ProcessMessage2000(theMessage);
                        break;
                        }
                    case "2001": //AddNewTrip
                        {
                        ProcessMessage2001(theMessage);
                        break;
                        }
                    case "2002": //DeleteTrip
                        {
                        ProcessMessage2002(theMessage);
                        break;
                        }
                    case "2003": //UpdateTrip
                        {
                        ProcessMessage2003(theMessage);
                        break;
                        }
                    case "2004": //TripAllocated
                        {
                            ProcessMessage2004(theMessage);
                            break;
                        }
                    case "2005": //PublishForecast
                    {
                        ProcessMessage2005(theMessage);
                        break;
                    }

                }
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        private void LogMessageSendEvent(IMessageJson theMessage, string theContents, bool isError = false)
        {
            if (isError)
            {
                var theEvent = $"{theMessage.MessageNumber} Message NOT Sent. {theMessage.MessageDescription}";
                _theLogger?.LogCriticalError(theEvent);
                _theLogger?.LogCriticalError(theContents);
                return;
            }

            if (AppLoggingGlobalDeclarations.MyLoggingDebugEventsEnabled) return;
            {
                var theEvent = $"{theMessage.MessageNumber} Message Sent. {theMessage.MessageDescription}";
                _theLogger?.LogCriticalError(theEvent);
                _theLogger?.LogCriticalError(theContents);
            }
        }

        #endregion

        #region Process Messages
        private void ProcessMessage1000()
        {
            try
            {
                //send status
                MyTrainSchedulerManager!.ProduceMessage1100(MyEnableAutomaticConflictResolution);

                //send trips
                lock(GlobalDeclarations.TripList)
                {
                    foreach (var trip in TripList)
                    {
                        ProduceMessage2001(trip);
                    }
                }

            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        private void ProcessMessage1001(string theMessage)
        {
            try
            {
                var objectMessage = JsonConvert.DeserializeObject<ConflictManagementMessages.ResolutionAccept>(theMessage);

                var tripUid = objectMessage!.MyConflict!.MyTripUid;
                var tripStartTime = objectMessage.MyConflict.MyTripStartTime;
                var theTrip = FindTrip(tripUid,tripStartTime);
                var theConflict = theTrip?.FindConflict(objectMessage.MyConflict.MyGuid);
                if (theConflict == null) return;
                theConflict.IsAccepted = true; theConflict!.IsRejected = false;
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        private void ProcessMessage1002(string theMessage)
        {
            try
            {
                var objectMessage = JsonConvert.DeserializeObject<ConflictManagementMessages.ResolutionReject>(theMessage);
                var tripUid = objectMessage!.MyConflict!.MyTripUid;
                var tripStartTime = objectMessage.MyConflict.MyTripStartTime;
                var theTrip = FindTrip(tripUid, tripStartTime);
                var theConflict = theTrip?.FindConflict(objectMessage.MyConflict.MyGuid);
                if (theConflict == null) return;
                theConflict.IsAccepted = false; theConflict!.IsRejected = true;

            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        private void ProcessMessage1003(string theMessage)
        {
            try
            {
                var objectMessage = JsonConvert.DeserializeObject<ConflictManagementMessages.OperatorDeleteTrip>(theMessage);
                var tripUid = objectMessage!.MyTripUid;
                var startTime = objectMessage.MyStartTime;
                MyAutoRoutingManager!.OperatorDeletedTrip(tripUid, startTime);
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        private void ProcessMessage1004(string theMessage)
        {
            try

            {
                var objectMessage = JsonConvert.DeserializeObject<ConflictManagementMessages.OperatorConflictManagementControl>(theMessage);
                if (objectMessage!.TurnOff)
                {
                    MyEnableAutomaticConflictResolution = false;
                    //MyAutoRoutingManager?.RemoveAllConflictsFromTrips();
                }
                else
                {
                    //MyAutoRoutingManager?.RegenerateAllConflictsForTrips();
                    MyEnableAutomaticConflictResolution = true;
                    
                }
                MyTrainSchedulerManager!.ProduceMessage1100(MyEnableAutomaticConflictResolution);
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        private void ProcessMessage1005(string theMessage)
        {
            try
            {
                var objectMessage = JsonConvert.DeserializeObject<ConflictManagementMessages.CmsEventMessage>(theMessage);
                DoProcessEvent?.Invoke(objectMessage);
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        private void ProcessMessage1006(string theMessage)
        {
            try
            {
                var objectMessage = JsonConvert.DeserializeObject<ConflictManagementMessages.SerializeTrip>(theMessage);
                MyAutoRoutingManager?.DoSerializeTrip(objectMessage?.theTripCode, objectMessage?.theTripUid);
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        private void ProcessMessage1100(string theMessage)
        {
            try
            {
                var objectMessage = JsonConvert.DeserializeObject<ConflictManagementMessages.ConflictResolutionStatus>(theMessage);
                DoProcessStatus?.Invoke(objectMessage!);
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        private void ProcessMessage1200(string theMessage)
        {
            try
            {
                var objectMessage = JsonConvert.DeserializeObject<ConflictManagementMessages.SendRoutePlanRequest>(theMessage);
                //DoProcessStatus?.Invoke(objectMessage!);
                MyAutoRoutingManager!.ExecuteRoutePlan(objectMessage!);
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        private void ProcessMessage2000(string theMessage)
        {
            try
            {
                var objectNewTrip = JsonConvert.DeserializeObject<ConflictManagementMessages.SendAllTrips>(theMessage);
               
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        private void ProcessMessage2001(string theMessage)
        {
            try
            {
                var objectNewTrip = JsonConvert.DeserializeObject<ConflictManagementMessages.AddNewTrip>(theMessage);
                DoProcessTrip?.Invoke(objectNewTrip?.TheTrip,"ADD");
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        private void ProcessMessage2002(string theMessage)
        {
            try
            {
                var objectNewTrip = JsonConvert.DeserializeObject<ConflictManagementMessages.DeleteTrip>(theMessage);
                DoProcessTrip?.Invoke(objectNewTrip?.TheTrip,"DELETE");
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        private void ProcessMessage2003(string theMessage)
        {
            try
            {
                var objectNewTrip = JsonConvert.DeserializeObject<ConflictManagementMessages.UpdateTrip>(theMessage);
                DoProcessTrip?.Invoke(objectNewTrip?.TheTrip, "UPDATE");
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        private void ProcessMessage2004(string theMessage)
        {
            try
            {
                var objectNewTrip = JsonConvert.DeserializeObject<ConflictManagementMessages.TripAllocated>(theMessage);
                DoProcessTrip?.Invoke(objectNewTrip?.TheTrip, "ALLOCATE");
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        private void ProcessMessage2005(string theMessage)
        {
            try
            {
                var objectNewTrip = JsonConvert.DeserializeObject<ConflictManagementMessages.PublishForecast>(theMessage);
                DoProcessForecast?.Invoke(objectNewTrip?.TheForecast);
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }

        #endregion

        #region Produce Messages
        public void ProduceMessage1000()
        {
            try
            {
                var message = ConflictManagementMessages.InitializeClient.CreateInstance();
                var initTrips = JsonConvert.SerializeObject(message);
                SendMessage(message, initTrips);
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        public void ProduceMessage1001(Conflict theConflict)
        {
            try
            {
                var message = ConflictManagementMessages.ResolutionAccept.CreateInstance(theConflict);
                var resolutionAccept = JsonConvert.SerializeObject(message);
                SendMessage(message, resolutionAccept);
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        public void ProduceMessage1002(Conflict theConflict)
        {
            try
            {
                var message = ConflictManagementMessages.ResolutionReject.CreateInstance(theConflict);
                var resolutionReject = JsonConvert.SerializeObject(message);
                SendMessage(message, resolutionReject);
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        public void ProduceMessage1003(Trip theTrip)
        {
            try
            {
                var message = ConflictManagementMessages.OperatorDeleteTrip.CreateInstance();
                message.MyTripUid = theTrip.TripId.ToString();
                message.MyStartTime = theTrip.StartTime!;
                var deleteTrip = JsonConvert.SerializeObject(message);
                SendMessage(message, deleteTrip);
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        public void ProduceMessage1004(bool turnOff)
        {
            try
            {
                var message = ConflictManagementMessages.OperatorConflictManagementControl.CreateInstance(turnOff);
                var control = JsonConvert.SerializeObject(message);
                SendMessage(message, control);
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        public void ProduceMessage1005(Messages.EventMessage theEventMessage)
        {
            try
            {
                var message = ConflictManagementMessages.CmsEventMessage.CreateInstance(theEventMessage);
                var theEvent = JsonConvert.SerializeObject(message);
                SendMessage(message, theEvent);
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        public void ProduceMessage1006(string? tripCode, string? tripUid)
        {
            try
            {
                var message = ConflictManagementMessages.SerializeTrip.CreateInstance(tripCode, tripUid);
                var theEvent = JsonConvert.SerializeObject(message);
                SendMessage(message, theEvent);
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        public void ProduceMessage1100(bool conflictResolutionEnabled)
        {
            try
            {
                var message = ConflictManagementMessages.ConflictResolutionStatus.CreateInstance(conflictResolutionEnabled);
                message.ConflictResolutionEnabled = conflictResolutionEnabled;
                var conflictResolutionStatus = JsonConvert.SerializeObject(message);
                SendMessage(message, conflictResolutionStatus);
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        public void ProduceMessage1200(ConflictManagementMessages.SendRoutePlanRequest theMessage)
        {
            try
            {
                var routePlanRequest = JsonConvert.SerializeObject(theMessage);
                SendMessage(theMessage, routePlanRequest);
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        public void ProduceMessage2001(Trip theTrip)
        {
            try
            {
                var message = ConflictManagementMessages.AddNewTrip.CreateInstance(theTrip);
                var objectNewTrip = JsonConvert.SerializeObject(message);
                SendMessage(message, objectNewTrip);
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        public void ProduceMessage2002(Trip theTrip)
        {
            try
            {
                var message = ConflictManagementMessages.DeleteTrip.CreateInstance(theTrip);
                var objectUpdateTrip = JsonConvert.SerializeObject(message);
                SendMessage(message, objectUpdateTrip);
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        public void ProduceMessage2003(Trip theTrip)
        {
            try
            {
                var message = ConflictManagementMessages.UpdateTrip.CreateInstance(theTrip);
                var objectUpdateTrip = JsonConvert.SerializeObject(message);
                SendMessage(message, objectUpdateTrip);
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        public void ProduceMessage2004(Trip theTrip)
        {
            try
            {
                var message = ConflictManagementMessages.TripAllocated.CreateInstance(theTrip);
                var objectUpdateTrip = JsonConvert.SerializeObject(message);
                SendMessage(message, objectUpdateTrip);
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        public void ProduceMessage2005(Forecast theForecast)
        {
            try
            {
                var message = ConflictManagementMessages.PublishForecast.CreateInstance(theForecast);
                var objectUpdateTrip = JsonConvert.SerializeObject(message);
                SendMessage(message, objectUpdateTrip);
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        private void SendMessage(IMessageJson theMessage, string messageToSend)
        {
            try
            {
                if (_exchangeManager!.SendMessage(messageToSend)) return;
                LogMessageSendEvent(theMessage, messageToSend, true);
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        #endregion

    }
}
