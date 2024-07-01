using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
//using Amqp;
using Apache.NMS;
using Apache.NMS.Util;
//using Apache.NMS.AMQP;
using ConflictManagementLibrary.Logging;
//using ConnectionFactory = Apache.NMS.AMQP.ConnectionFactory;
using IConnection = Apache.NMS.IConnection;
using ISession = Apache.NMS.ISession;

namespace ConflictManagementLibrary.Communications
{
    public class AppExchangeConnectionManager
    {
        #region Public Delegates

        public delegate void MessageHandlerDelegate(string theMessage);

        #endregion

        public IMyLogger? MyLogger { get; }
        public AppExchangeSettings MySettings { get; }
        public AppExchangeManager MyExchangeManager { get; }
        public AppMessagePublisherSettings MyPublisherSettings;
        public AppMessageConsumerSettings MyConsumerSettings;
        private IConnectionFactory theFactory;
        private IConnection theConnection;
        private ISession? theSessionTransmit;
        private ISession? theSessionReceive;

        private IDestination? theQueueTransmit;
        private IDestination? theQueueReceive;
        private IMessageProducer? theProducer;
        private IMessageConsumer? theConsumer;
        public bool IsMonitoring;
        public bool IsFlippedRouting;
        public Queue<ITextMessage> MyReceiveQueue = new Queue<ITextMessage>();
        public MessageHandlerDelegate OnHandleMessage; //the message handler logic will be performed by the consumer of this class

        public static AppExchangeConnectionManager CreateInstance(IMyLogger? theLogger, AppExchangeSettings theSettings, AppMessagePublisherSettings thePublisherSettings, AppMessageConsumerSettings theConsumerSettings, AppExchangeManager theExchangeManager, bool FlipRouting = false)
        {
            return new AppExchangeConnectionManager(theLogger, theSettings, thePublisherSettings,theConsumerSettings, theExchangeManager, FlipRouting);
        }
        private AppExchangeConnectionManager(IMyLogger? theLogger, AppExchangeSettings theSettings, AppMessagePublisherSettings thePublisherSettings, AppMessageConsumerSettings theConsumerSettings, AppExchangeManager theExchangeManager, bool FlipRouting = false)
        {
            MyLogger = theLogger;
            MySettings = theSettings;
            MyExchangeManager = theExchangeManager;
            MyPublisherSettings = thePublisherSettings;
            MyConsumerSettings = theConsumerSettings;
            IsFlippedRouting = FlipRouting;
            InitializeManager();
        }
        public bool InitializeManager()
        {
            try
            {
                return InitializeConnection();
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
            return false;
        }
        private bool InitializeConnection()
        {
            try
            {
                var connectUri = string.Format($"tcp://{MySettings.MyHostName}:61616");


                theFactory = new NMSConnectionFactory(connectUri);
                MyLogger.LogInfo("Apache Factory Created...");
                theConnection = theFactory.CreateConnection(MySettings.MyUserName, MySettings.MyPassword);

                //theConnection?.Start();
                theConnection.ExceptionListener += TheConnection_ExceptionListener;


                MyLogger.LogInfo("Apache AMQP Connection Created...");
                theSessionTransmit = theConnection?.CreateSession(AcknowledgementMode.AutoAcknowledge);
                MyLogger.LogInfo("Apache Transmit Session Created...");
                theSessionReceive = theConnection?.CreateSession(AcknowledgementMode.AutoAcknowledge);
                MyLogger.LogInfo("Apache Receive Session Created...");
                theConnection.Start();
                CreatePublisher();
                CreateConsumer();
                theConnection.ExceptionListener += OnConnectionException;
                theConnection.ConnectionInterruptedListener += OnConnectionInterrupted;
                return true;
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }

            return false;
        }

        private void TheConnection_ExceptionListener(Exception exception)
        {
            MyLogger.LogInfo(exception.Message);
            MyLogger.LogInfo(exception.InnerException);
            MyLogger.LogInfo(exception.StackTrace);
        }

        private void CreatePublisher()
        {
            var queueName = MyPublisherSettings.MyQueueDeclarations[1];
            if (IsFlippedRouting) queueName = MyPublisherSettings.MyQueueDeclarations[0];
            theQueueTransmit = theSessionTransmit?.GetQueue(queueName);
            theProducer = theSessionTransmit?.CreateProducer(theQueueTransmit);
            theProducer.DeliveryMode = MsgDeliveryMode.NonPersistent;
            MyLogger.LogInfo("Apache Publisher Created...");

        }
        private void CreateConsumer()
        {
            var queueName = MyPublisherSettings.MyQueueDeclarations[0];
            if (IsFlippedRouting) queueName = MyPublisherSettings.MyQueueDeclarations[1];
			            
            theQueueReceive = theSessionReceive?.GetQueue(queueName);
            theConsumer = theSessionReceive?.CreateConsumer(theQueueReceive);
            theConsumer.Listener += Consumer_Listener;
            MyLogger.LogInfo("Apache Consumer Created...");

        }
        public void BindConsumer(AppExchangeConnectionManager.MessageHandlerDelegate theMessageHandler)
        {
            this.OnHandleMessage = theMessageHandler ?? throw new ArgumentNullException(nameof(theMessageHandler));
        }
        private void DoReconnect(Exception theException)
        {
            if (InitializeConnection())
            {
                MyLogger.LogInfo("Message Broker Reconnected @ " + MySettings.MyHostName);
            }
            else
            {
                MyLogger.LogInfo("Message Broker Not Reconnected @ " + MySettings.MyHostName);
                MyLogger.LogException(theException.ToString());
            }

        }
        private void OnConnectionException(Exception theException)
        {
            DoReconnect(theException);
        }
        private void OnConnectionInterrupted()
        {
            DoReconnect(new Exception("Connection was interrupted @ " + MySettings.MyHostName));
        }
        private void Consumer_Listener(IMessage theMessage)
        {
            var message = theMessage as ITextMessage;
            //MessageBox.Show($"received message {message}");
            OnHandleMessage?.Invoke(message?.Text);
        }
        public bool SendMessage(string theMessage, string theRoutingKey)
        {
            try
            {
                var message = theSessionTransmit?.CreateTextMessage(theMessage);
                if (message != null)
                    theProducer?.Send(message);
                return true;
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
            return false;
        }
    }
}
