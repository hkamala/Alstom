using System.Runtime.CompilerServices;
using NLog;
using static ConflictManagementLibrary.Logging.AppLoggingGlobalDeclarations;

namespace ConflictManagementLibrary.Logging
{
    public class EventLogger : IMyLogger
    {
        public static EventLogger CreateInstance(string callingMember)
        {
            return new EventLogger(callingMember);
        }

        public Logger LogThis { get; set; } = default;

        private string _callingMember;

        private EventLogger(string callingMember)
        {
            _callingMember = callingMember;
            LogThis = LogManager.GetCurrentClassLogger();
        }

        public EventLogger(string loggerName, string callingMember)
        {
            _callingMember = callingMember;
            LogThis = LogManager.GetLogger(loggerName);
        }

        public void LogInfo<T>(T value, AppLoggingGlobalEnums.LogEventTypes typeOfEvent = AppLoggingGlobalEnums.LogEventTypes.General, [CallerMemberName] string memberName = "")
        {
            if (value == null) return;
            DoLogEvent(LogLevel.Info, typeOfEvent, memberName, value.ToString());
        }

        public void LogException<T>(T value, AppLoggingGlobalEnums.LogEventTypes typeOfEvent = AppLoggingGlobalEnums.LogEventTypes.General, [CallerMemberName] string memberName = "")
        {
            if (value == null) return;
            DoLogEvent(LogLevel.Error, typeOfEvent, memberName, value.ToString());
        }

        public void LogDebug<T>(T value, AppLoggingGlobalEnums.LogEventTypes typeOfEvent = AppLoggingGlobalEnums.LogEventTypes.General, [CallerMemberName] string memberName = "")
        { 
            if (value == null) return;
            if (MyLoggingDebugEventsEnabled) {DoLogEvent(LogLevel.Debug, typeOfEvent, memberName, value.ToString());}
        }

        public void LogTrace<T>(T value, AppLoggingGlobalEnums.LogEventTypes typeOfEvent = AppLoggingGlobalEnums.LogEventTypes.Performance, [CallerMemberName] string memberName = "")
        {
            if (value == null) return;
            if (MyLoggingTraceEventsEnabled) {DoLogEvent(LogLevel.Trace, typeOfEvent, memberName, value.ToString());}
        }

        public void LogCriticalEvent<T>(T value, AppLoggingGlobalEnums.LogEventTypes typeOfEvent = AppLoggingGlobalEnums.LogEventTypes.General, [CallerMemberName] string memberName = "")
        {
            if (value == null) return;
            DoLogEvent(LogLevel.Warn, typeOfEvent, memberName, value.ToString());
        }
       
        public void LogCriticalError<T>(T value, AppLoggingGlobalEnums.LogEventTypes typeOfEvent = AppLoggingGlobalEnums.LogEventTypes.General, [CallerMemberName] string memberName = "")
        {
            if (value == null) return;
            DoLogEvent(LogLevel.Fatal, typeOfEvent, memberName, value.ToString());
        }

        private void DoLogEvent(LogLevel theLevel, AppLoggingGlobalEnums.LogEventTypes theType, string callingMethod, string theMessage)
        {
            var theEvent = new LogEventInfo(theLevel, LogThis.Name, theMessage);
            theEvent.Properties["AppVersion"] = MyAppVersion;
            theEvent.Properties["typeOfEvent"] = theType.ToString();
            theEvent.Properties["callingMethod"] = callingMethod;
            LogThis.Log(theEvent);
        }
    }
    
}
