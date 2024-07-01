using NLog;

namespace ConflictManagementLibrary.Logging
{
    public static class AppLoggingStart
    {
        public static IMyLogger? CreateLogger()
        {
           var theLogger = new EventLogger("ConflictManager", nameof(CreateLogger));
           AppLoggingGlobalDeclarations.TheLoggerConfiguration = AppLoggingConfiguration.CreateInstance(theLogger); 
           return theLogger;
        }
    }
}
