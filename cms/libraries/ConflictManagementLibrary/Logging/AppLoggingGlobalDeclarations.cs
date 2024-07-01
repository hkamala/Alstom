using System.Collections.Specialized;
using System.Windows.Forms;
using NLog.Config;
using NLog.Targets;
using NLog.Windows.Forms;

namespace ConflictManagementLibrary.Logging
{
    public static class AppLoggingGlobalDeclarations
    {
        /// <LoggingInstantiations>
        /// 
        /// </LoggingInstantiations>
        public static IMyLogger TheLogger { get; set; }
        public static AppLoggingConfiguration TheLoggerConfiguration;
        public static NameValueCollection MailTargetProperties;
        public static AppLoggingDatabaseInterface MyDatabaseInterface;
        public static string MyApplicationAbbreviatedName;

        /// <LoggingTargets>
        /// 
        /// </LoggingTargets>
        public static FileTarget LogfileApplication;
        public static FileTarget LogfileException;
        public static FileTarget LogfileCriticalError;
        public static NetworkTarget LogNetwork;
        public static ConsoleTarget LogConsole;
        public static RichTextBoxTarget LogTextBox;
        //public static DatabaseTarget LogDatabase;
        public static MailTarget LogMail;

        /// <LoggingRules>
        /// 
        /// </LoggingRules>
        public static LoggingRule LogfileApplicationRule;
        public static LoggingRule LogfileExceptionRule;
        public static LoggingRule LogfileCriticalErrorRule;
        public static LoggingRule LogConsoleRule;
        public static LoggingRule LogNetworkRule;
        public static LoggingRule LogDatabaseRule;
        public static LoggingRule LogMailRule;
        public static LoggingRule LogTextBoxRule;

        /// <LoggingOptions>
        /// 
        /// </LoggingOptions>
        public static bool MyLoggingApplication = true;
        public static bool MyLoggingException = true;
        public static bool MyLoggingCriticalError = true;
        public static bool MyLoggingConsole = false;
        public static bool MyLoggingNetwork = true;
        public static bool MyLoggingDatabase = true;
        public static string MyLoggingTimeStampFormat = "yyyy-MM-dd HHmmss";
        public static string MyLoggingDateFormat = "yyyy-MM-dd HH:mm:ss.fff";
        public static string MyLoggingLayout = "${longdate}|${level: uppercase = true}|${event-properties:typeOfEvent}|${event-properties:AppVersion}|${machinename}|${event-properties:callingMethod}|${message}";
        public static string MyLoggingArchiveFileName = "${basedir}/archives/${level}-{#}.txt";
        public static string MyLogPathApplicationFile = "${basedir}/logs/ApplicationLog.txt";
        public static string MyLogTargetNameApplication = "LogfileApplication";
        public static string MyLogPathExceptionFile = "${basedir}/logs/ExceptionLog.txt";
        public static string MyLogTargetNameException = "LogfileException";
        public static string MyLogPathCriticalErrorFile = "${basedir}/logs/CriticalErrorLog.txt";
        public static string MyLogTargetNameCriticalError = "LogfileCriticalError";
        public static string MyLogTargetNameTextBox = "LogfileTextBox";

        public static AppLoggingGlobalEnums.LogEventTypes TypeOfEvent;
        public static string MyLastCriticalErrorMessage;
        public static bool MyLoggingTraceEventsEnabled = true;
        public static bool MyLoggingDebugEventsEnabled = false;

    }
}
