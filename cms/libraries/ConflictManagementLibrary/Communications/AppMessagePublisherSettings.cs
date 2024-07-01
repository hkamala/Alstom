using System;
using System.Collections.Generic;
using ConflictManagementLibrary.Logging;

namespace ConflictManagementLibrary.Communications
{
    public class AppMessagePublisherSettings
    {
        public IMyLogger? MyLogger { get; }
        public string MySettings { get; }
        public string MyExchangeType = "direct";
        public string MyExchangeName = string.Empty;
        public List<string> MyRoutingKeys = new List<string>();
        public List<string> MyQueueDeclarations = new List<string>();

        public static AppMessagePublisherSettings CreateInstance(IMyLogger? theLogger, string theSettings)
        {
            return new AppMessagePublisherSettings(theLogger, theSettings);
        }
        private AppMessagePublisherSettings(IMyLogger? theLogger, string theSettings)
        {
            MyLogger = theLogger;
            MySettings = theSettings;
            ParseSettings(MySettings);
        }

        private void ParseSettings(string theSettings)
        {
            try
            {
                //	<add key="MessagePublisherDefinitions" value="ExchangeType:[Direct]|ExchangeName:[TestExchange]|RoutingKeys:[],[]|QueueDeclarations:[],[]|"/>

                var theParams = theSettings.Split('|');
                var theParamsType = theParams[0].Split(':');
                var theParamsName = theParams[1].Split(':');
                var theParamsKeys = theParams[2].Split(':');
                var theParamsQueues = theParams[3].Split(':');

                theParamsType[1] = theParamsType[1].Replace("[", "").Replace("]","").Trim();
                if (!string.IsNullOrEmpty(theParamsType[1])) MyExchangeType = theParamsType[1].ToLower();

                theParamsName[1] = theParamsName[1].Replace("[", "").Replace("]", "").Trim();
                if (!string.IsNullOrEmpty(theParamsName[1])) MyExchangeName = theParamsName[1];

                var theKeys = theParamsKeys[1].Split(',');
                foreach (var k in theKeys)
                {
                    var key = k.Replace("[", "").Replace("]", "").Trim();
                    if (!string.IsNullOrEmpty(key)) MyRoutingKeys.Add(key);
                }

                var theQueues = theParamsQueues[1].Split(',');
                foreach (var q in theQueues)
                {
                    var queue = q.Replace("[", "").Replace("]", "").Trim();
                    if (!string.IsNullOrEmpty(queue)) MyQueueDeclarations.Add(queue);
                }
            }
            catch (Exception e)
            {
                MyLogger.LogException(e.ToString());
            }
        }
    }
}
