using System.Runtime.CompilerServices;
using static ConflictManagementLibrary.Logging.AppLoggingGlobalEnums;

namespace ConflictManagementLibrary.Logging
{
    public interface IMyLogger
    {
        void LogInfo<T>(T value, LogEventTypes typeOfEvent = LogEventTypes.General, [CallerMemberName] string memberName = "");
        void LogDebug<T>(T value, LogEventTypes typeOfEvent = LogEventTypes.General,[CallerMemberName] string memberName = "");
        void LogTrace<T>(T value, LogEventTypes typeOfEvent = LogEventTypes.Performance, [CallerMemberName] string memberName = "");
        void LogException<T>(T value, LogEventTypes typeOfEvent = LogEventTypes.General, [CallerMemberName] string memberName = "");
        void LogCriticalError<T>(T value, LogEventTypes typeOfEvent = LogEventTypes.General, [CallerMemberName] string memberName = "");
        void LogCriticalEvent<T>(T value, LogEventTypes typeOfEvent = LogEventTypes.General, [CallerMemberName] string memberName = "");

    }
}
