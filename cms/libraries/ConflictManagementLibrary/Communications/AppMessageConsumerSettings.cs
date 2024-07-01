using System;
using System.Collections.Generic;
using ConflictManagementLibrary.Logging;

namespace ConflictManagementLibrary.Communications
{
    public class AppMessageConsumerSettings
    {
        public IMyLogger? MyLogger { get; }
        public string MySettings { get; }
        public string MyExchangeName = string.Empty;
        public List<AppConsumerQueueKeyPair> MyQueueRoutePairs = new List<AppConsumerQueueKeyPair>();

        public static AppMessageConsumerSettings CreateInstance(IMyLogger? theLogger, string theSettings)
        {
            return new AppMessageConsumerSettings(theLogger, theSettings);
        }
        private AppMessageConsumerSettings(IMyLogger? theLogger, string theSettings)
        {
            MyLogger = theLogger;
            MySettings = theSettings;
            ParseSettings(MySettings);
        }
        private void ParseSettings(string theSettings)
        {
            try
            {
                //<add key="MessageConsumerDefinitions" value="ExchangeName:[TestExchange]|QueueRoutingKeyPairs:[Test1]=[],[Test2]=[RouteKey2]|"/>


                var theParams = theSettings.Split('|');
                var theParamsName = theParams[0].Split(':');
                var theParamsQueueKeyPairs = theParams[1].Split(':');


                theParamsName[1] = theParamsName[1].Replace("[", "").Replace("]", "").Trim();
                if (!string.IsNullOrEmpty(theParamsName[1])) MyExchangeName = theParamsName[1];

                var thePairs = theParamsQueueKeyPairs[1].Split(',');
                foreach (var p in thePairs)
                {
                    var pair = p.Split('=');
                    var queueName = pair[0].Replace("[", "").Replace("]", "").Trim();
                    var keyName = pair[1].Replace("[", "").Replace("]", "").Trim();
                    if (!string.IsNullOrEmpty(queueName) || !string.IsNullOrEmpty(keyName)) 
                        MyQueueRoutePairs.Add(AppConsumerQueueKeyPair.CreateInstance(queueName, keyName));
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }

    }
}
