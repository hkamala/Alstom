using System;
using NLog;
using static ConflictManagementLibrary.Logging.AppLoggingGlobalDeclarations;

namespace ConflictManagementLibrary.Logging
{
    /// <SystemEvent>
    /// This is used to serialize the system event into a JSON format to commit to the database
    /// </SystemEvent>
    public class SystemEvent
    {
        public static SystemEvent CreateInstance(LogEventInfo logEvent)
        {
            return new SystemEvent(logEvent);
        }

        public string TheTime;
        public string TheLevel;
        public string TheType;
        public string TheSource;
        public string TheMethod;
        public string TheMessage;

        private SystemEvent(LogEventInfo logEvent)
        {
            TheTime = logEvent.TimeStamp.ToString(MyLoggingDateFormat);
            TheLevel = logEvent.Level.ToString();
            TheType = logEvent.Properties["typeOfEvent"].ToString();
            TheSource = Environment.MachineName;
            TheMethod = logEvent.Properties["callingMethod"].ToString();
            TheMessage = logEvent.Message;

        }
    }
}