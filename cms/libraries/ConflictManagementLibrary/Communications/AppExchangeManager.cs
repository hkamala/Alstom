using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Threading;
using ConflictManagementLibrary.Logging;
using System.Runtime.CompilerServices;

namespace ConflictManagementLibrary.Communications
{
    public class AppExchangeManager
    {

        public IMyLogger? MyLogger { get; }
        public AppExchangeSettings MyConnectionSettingPrimary;
        public AppExchangeSettings MyConnectionSettingSecondary;
        public List<AppExchangeConnectionManager> MyConnectionManagers = new List<AppExchangeConnectionManager>();
        public Queue<object> MyQueueSend = new Queue<object>();
        public Queue<object> MyQueueReceive = new Queue<object>();
        public List<string> MyQueueNamesToPublish = new List<string>();
        public List<string> MyQueueNamesToConsume = new List<string>();
        public AppMessagePublisherSettings MyPublisherSettings;
        public AppMessageConsumerSettings MyConsumerSettings;

        private string theHostNamePrimaryBroker;
        private string theUserNamePrimaryBroker;
        private string thePortPrimaryBroker;
        private string thePasswordPrimaryBroker;
        private string theMessagePublisherDefinitions;
        private string theMessageConsumerDefinitions;
        private bool FlipRouting;
        public static AppExchangeManager? CreateInstance(IMyLogger? theLogger, bool FlipRouting = false)
        {
            return new AppExchangeManager(theLogger, FlipRouting);
        }
        private AppExchangeManager(IMyLogger? theLogger, bool FlipRouting = false)
        {
            MyLogger = theLogger;
            this.FlipRouting = FlipRouting;
            InitializeConfigurationFile();
            InitializeConnections();
        }

        private void InitializeConfigurationFile()
        {
            try
            {
                var thecfg = System.Configuration.ConfigurationManager.GetSection("LibraryConfigurationFileNames") as NameValueCollection;
                var fileMap = new ExeConfigurationFileMap
                {
                    ExeConfigFilename = Environment.CurrentDirectory + @"\" + thecfg?["ConflictManagementLibrary"]
                };
                var cfg = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);

                theHostNamePrimaryBroker = cfg.AppSettings.Settings["HostNamePrimaryBroker"].Value;
                theUserNamePrimaryBroker = cfg.AppSettings.Settings["UserNamePrimaryBroker"].Value;
                thePasswordPrimaryBroker = cfg.AppSettings.Settings["PasswordPrimaryBroker"].Value;
                thePortPrimaryBroker = cfg.AppSettings.Settings["PortPrimaryBroker"].Value;
                theMessagePublisherDefinitions = cfg.AppSettings.Settings["MessagePublisherDefinitions"].Value;
                theMessageConsumerDefinitions = cfg.AppSettings.Settings["MessageConsumerDefinitions"].Value;
            }
            catch (Exception e)
            {
                MyLogger.LogException(value: e.ToString());
            }
        }
        private void InitializeConnections()
        {
            try
            {
                MyConsumerSettings  = AppMessageConsumerSettings.CreateInstance(MyLogger, theMessageConsumerDefinitions );
                MyPublisherSettings = AppMessagePublisherSettings.CreateInstance(MyLogger, theMessagePublisherDefinitions);
                MyConnectionSettingPrimary = AppExchangeSettings.CreateInstance(
                    theHostNamePrimaryBroker,
                    thePortPrimaryBroker,
                    theUserNamePrimaryBroker,
                    thePasswordPrimaryBroker);
                MyConnectionManagers.Add(AppExchangeConnectionManager.CreateInstance(MyLogger, MyConnectionSettingPrimary, MyPublisherSettings, MyConsumerSettings, this, FlipRouting));
            }
            catch (Exception e)
            {
                MyLogger.LogException(value: e.ToString());
            };
        }
        public bool BindMessageHandlerToConsumers(AppExchangeConnectionManager.MessageHandlerDelegate theMessageHandler)
        {
            try
            {
                foreach (var conn in MyConnectionManagers)
                {
                    conn.BindConsumer(theMessageHandler);
                }

                return true;

            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }

            return false;
        }
        public void TestConnections()
        {
            foreach (var pair in MyConsumerSettings.MyQueueRoutePairs)
            {
                foreach (var cm in MyConnectionManagers)
                {
                    var messageSent = cm.SendMessage("Hello from <" + pair.MyQueueName + "><" + pair.MyKeyName + ">", pair.MyKeyName);
                    if (messageSent) break;
                }
            }
        }
        public bool SendMessage(string theMessage, string theRoutingKey = "")
        {
            try
            {
                foreach (var connectionManager in MyConnectionManagers)
                {
                    if (connectionManager.SendMessage(theMessage, theRoutingKey))
                    {
                        //Console.WriteLine(theMessage);
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(value: e.ToString());
            }

            return false;
        }
    }
}
