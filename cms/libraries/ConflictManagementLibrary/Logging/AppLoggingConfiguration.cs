using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Mail;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NLog;
using NLog.Config;
using NLog.LayoutRenderers;
using NLog.Targets;
using NLog.Windows.Forms;
using static ConflictManagementLibrary.Logging.AppLoggingGlobalDeclarations;
using Formatting = System.Xml.Formatting;


namespace ConflictManagementLibrary.Logging
{
    public class AppLoggingConfiguration
    {
        public static AppLoggingConfiguration CreateInstance(IMyLogger? theLogger)
        {
            return new AppLoggingConfiguration(theLogger);
        }
        public IMyLogger? TheLogger { get; private set; }
        private AppLoggingConfiguration(IMyLogger? theLogger)
        {
            TheLogger = theLogger;
            ConfigureApplicationLogFile();
            ConfigureExceptionLogFile();
            ConfigureCriticalErrorLogFile();
            ConfigureConsoleLog();
            ConfigureNetworkLog();
            if (MailTargetProperties != null)
            {
                ConfigureMailLog();
            }
            LogManager.ReconfigExistingLoggers();
        }
        public void LoadDatabaseLogger()
        {
            ConfigureDatabaseLog();
        }
        public void ReloadConfig()
        {
            LogManager.ReconfigExistingLoggers();
        }
        private static void ConfigureApplicationLogFile()
        {
            LogfileApplication = new FileTarget(MyLogTargetNameApplication)
            
            {
                Layout = MyLoggingLayout,
                FileName = MyLogPathApplicationFile,
                KeepFileOpen = false,
                ArchiveFileName = MyLoggingArchiveFileName,
                ArchiveDateFormat = MyLoggingDateFormat,
                ArchiveNumbering = ArchiveNumberingMode.Date,
                ArchiveEvery = FileArchivePeriod.Day,
            };


            LogfileApplicationRule = new LoggingRule("*", LogLevel.Trace, LogfileApplication);
            LogManager.Configuration = new LoggingConfiguration();
            LogManager.Configuration.AddTarget(LogfileApplication);
            if (MyLoggingApplication) {LogManager.Configuration.LoggingRules.Add(LogfileApplicationRule);}
        }
        private static void ConfigureExceptionLogFile()
        {
            LogfileException = new FileTarget(MyLogTargetNameException)
            {
                Layout = MyLoggingLayout,
                FileName = MyLogPathExceptionFile,
                KeepFileOpen = false,
                ArchiveFileName = MyLoggingArchiveFileName,
                ArchiveDateFormat = MyLoggingDateFormat,
                ArchiveNumbering = ArchiveNumberingMode.Date,
                ArchiveEvery = FileArchivePeriod.Day,
            };

            LogfileExceptionRule = new LoggingRule("*", LogLevel.Error, LogfileException);
            LogManager.Configuration.AddTarget(LogfileException);
            LogManager.Configuration.LoggingRules.Add(LogfileExceptionRule);
        }
        private void ConfigureCriticalErrorLogFile()
        {
            LogfileCriticalError = new FileTarget(MyLogTargetNameCriticalError)
            {
                Layout = MyLoggingLayout,
                FileName = MyLogPathCriticalErrorFile,
                KeepFileOpen = false,
                ArchiveFileName = MyLoggingArchiveFileName,
                ArchiveDateFormat = MyLoggingDateFormat,
                ArchiveNumbering = ArchiveNumberingMode.Date,
                ArchiveEvery = FileArchivePeriod.Day,

            };
            LogfileCriticalErrorRule = new LoggingRule("*", LogLevel.Fatal, LogfileCriticalError);
            LogManager.Configuration.AddTarget(LogfileCriticalError);
            LogManager.Configuration.LoggingRules.Add(LogfileCriticalErrorRule);
           
        }
        private void ConfigureConsoleLog()
        {
            LogConsole = new ConsoleTarget("logconsole")
            {
                Layout = "${longdate}|${level: uppercase = true}|${machinename}|${message}"
            };

            LogConsoleRule = new LoggingRule("*", LogLevel.Debug, LogConsole);
            LogManager.Configuration.AddTarget(LogConsole);
            if (MyLoggingConsole) {LogManager.Configuration.LoggingRules.Add(LogConsoleRule);}
        }
        private void ConfigureNetworkLog()
        {
            LogNetwork = new NetworkTarget("lognetwork")
            {
                Layout = "${longdate}|${level: uppercase = true}|${machinename}|${message}",
                Address = "udp://127.0.0.1:7070"
            };

            LogNetworkRule = new LoggingRule("*", LogLevel.Trace, LogNetwork);
            LogManager.Configuration.AddTarget(LogNetwork);
            LogManager.Configuration.LoggingRules.Add(LogNetworkRule); 
        }
        protected virtual void ConfigureDatabaseLog()
        {
            var command = new StringBuilder();
            command.Append("INSERT into dbo.tblSystemEvents ");
            command.Append("(EventTimeStamp, EventLevel, EventType, EventSource, EventCallingMethod, EventMessage, EventData) ");
            command.Append("VALUES (@EventTimeStamp, @EventLevel, @EventType, @EventSource, @EventCallingMethod, @EventMessage, @EventData)");

            //LogDatabase = new DatabaseTarget("logdatabase")
            //{
            //    DBProvider = "mssql",
            //    DBHost = MyDatabaseInterface.MyDatabaseServerName,
            //    DBUserName = MyDatabaseInterface.MyDatabaseUserId,
            //    DBPassword = MyDatabaseInterface.MyDatabasePassword,
            //    DBDatabase = MyDatabaseInterface.MyDatabaseDatabaseName,
            //    CommandText = command.ToString()
            //};

             
            //LayoutRenderer.Register<JsonEventPropertiesLayoutRenderer>("json-event-properties");

            //var param = new DatabaseParameterInfo { Name = "@EventTimeStamp", Layout = "${longdate}" };
            //        LogDatabase.Parameters.Add(param);

            //    param = new DatabaseParameterInfo { Name = "@EventLevel", Layout = "${level}" };
            //    LogDatabase.Parameters.Add(param);

            //    param = new DatabaseParameterInfo { Name = "@EventType", Layout = "${event-properties:typeOfEvent}" };
            //    LogDatabase.Parameters.Add(param);

            //    param = new DatabaseParameterInfo { Name = "@EventSource", Layout = "${machinename}" };
            //    LogDatabase.Parameters.Add(param);

            //    param = new DatabaseParameterInfo { Name = "@EventCallingMethod", Layout = "${event-properties:callingMethod}" };
            //    LogDatabase.Parameters.Add(param);

            //    param = new DatabaseParameterInfo {Name = "@EventMessage", Layout = "${message}"};
            //    LogDatabase.Parameters.Add(param);

            //    param = new DatabaseParameterInfo { Name = "@EventData", Layout = "${json-event-properties}" };
            //    LogDatabase.Parameters.Add(param);


            //LogDatabaseRule = new LoggingRule("*", LogLevel.Trace, LogDatabase);
            //LogManager.Configuration.AddTarget(LogDatabase);
            //LogManager.Configuration.LoggingRules.Add(LogDatabaseRule);
            ReloadConfig();
        }
        private void ConfigureMailLog()
        {
            LogMail = new MailTarget("logmail")
            {
                SmtpServer = MailTargetProperties["SmtpServer"], //"smtp.gmail.com",
                From = Environment.MachineName + "@crsi.com",
                To = MailTargetProperties["Recipients"], //"8178884621@vtext.com, 8175846822@vtext.com",
                //To = "2135030665@vtext.com, 8175846822@vtext.com, 2133599001@txt.att.net, 6265604377@tmomail.net, 2134949266@vtext.com",
                Subject = MyApplicationAbbreviatedName + " - Critical Error Notification",
                SmtpPassword = MailTargetProperties["SmtpPassword"], //"Rtms@0987654321",
                SmtpUserName = MailTargetProperties["SmtpUserName"], //"development@railwaytms.com",
                SmtpPort = Convert.ToInt32(MailTargetProperties["SmtpPort"]), //587,
                SmtpAuthentication = SmtpAuthenticationMode.Basic,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                EnableSsl = true
                //Timeout = 60
            };
            LogMailRule = new LoggingRule("*", LogLevel.Fatal, LogMail);
            LogManager.Configuration.AddTarget(LogMail);
            LogManager.Configuration.LoggingRules.Add(LogMailRule);
            //ReloadConfig();
        }
        public virtual void EnableApplicationLog()
        {
            MyLoggingApplication = !MyLoggingApplication;
            if (MyLoggingApplication)
            {
                TheLogger.LogInfo("Application Logging Enabled...");
                LogManager.Configuration.LoggingRules.Add(LogfileApplicationRule);
            }
            else
            {
                TheLogger.LogInfo("Application Logging Disabled...");
                LogManager.Configuration.LoggingRules.Remove(LogfileApplicationRule);
            }

            ReloadConfig();
        }
        public virtual void EnableExceptionLog()
        {
            MyLoggingException = !MyLoggingException;
            if (MyLoggingException)
            {
                TheLogger.LogInfo("Exception Logging Enabled...");
                LogManager.Configuration.LoggingRules.Add(LogfileExceptionRule);
            }
            else
            {
                TheLogger.LogInfo("Exception Logging Disabled...");
                LogManager.Configuration.LoggingRules.Remove(LogfileExceptionRule);
            }

            ReloadConfig();
        }
        public void EnableCriticalErrorLog()
        {
            MyLoggingCriticalError = !MyLoggingCriticalError;
            if (MyLoggingCriticalError)
            {
                TheLogger.LogInfo("Critical Error Logging Enabled...");
                LogManager.Configuration.LoggingRules.Add(LogfileCriticalErrorRule);
            }
            else
            {
                TheLogger.LogInfo("Critical Error Logging Disabled...");
                LogManager.Configuration.LoggingRules.Remove(LogfileCriticalErrorRule);
            }

            ReloadConfig();
        }
        public void EnableConsoleLog()
        {
            MyLoggingConsole = !MyLoggingConsole;
            if (MyLoggingConsole)
            {
                TheLogger.LogInfo("Console Logging Enabled...");
                LogManager.Configuration.LoggingRules.Add(LogConsoleRule);
            }
            else
            {
                TheLogger.LogInfo("Console Logging Disabled...");
                LogManager.Configuration.LoggingRules.Remove(LogConsoleRule);
            }

            ReloadConfig();
        }
        public void EnableNetworkLog()
        {
            MyLoggingNetwork = !MyLoggingNetwork;
            if (MyLoggingNetwork)
            {
                TheLogger.LogInfo("Network Logging Enabled...");
                LogManager.Configuration.LoggingRules.Add(LogNetworkRule);
            }
            else
            {
                TheLogger.LogInfo("Network Logging Disabled...");
                LogManager.Configuration.LoggingRules.Remove(LogNetworkRule);
            }

            ReloadConfig();
        }
        public void EnableDatabaseLog()
        {
            MyLoggingDatabase = !MyLoggingDatabase;
            if (MyLoggingDatabase)
            {
                TheLogger.LogInfo("Database Logging Enabled...");
                LogManager.Configuration.LoggingRules.Add(LogDatabaseRule);
            }
            else
            {
                TheLogger.LogInfo("Database Logging Disabled...");
                LogManager.Configuration.LoggingRules.Remove(LogDatabaseRule);
            }

            ReloadConfig();
        }
        //public static string ToJson(object obj, bool format = false, string dateFormat = null)
        //{
        //    var settings = new JsonSerializerSettings
        //    {
        //        NullValueHandling = NullValueHandling.Ignore
        //    };

        //    if (!String.IsNullOrWhiteSpace(dateFormat))
        //    {
        //        settings.Converters = new List<JsonConverter>
        //        {
        //            new IsoDateTimeConverter {DateTimeFormat = dateFormat}
        //        };

        //        return JsonConvert.SerializeObject(obj, format ? Formatting.Indented : Formatting.None, settings);
        //    }

        //    return JsonConvert.SerializeObject(obj, format ? Formatting.Indented : Formatting.None, settings);
        //}

        [LayoutRenderer("json-event-properties")]
        public class JsonEventPropertiesLayoutRenderer : LayoutRenderer
        {
            /// <summary>
            /// Renders the specified environmental information and appends it to the specified <see cref="T:System.Text.StringBuilder" />.
            /// </summary>
            /// <param name="builder">The <see cref="T:System.Text.StringBuilder" /> to append the rendered data to.</param>
            /// <param name="logEvent">Logging event.</param>
            protected override void Append(StringBuilder builder, LogEventInfo logEvent)
            {
                if (logEvent.Properties == null || logEvent.Properties.Count == 0)
                    return;
                var se = SystemEvent.CreateInstance(logEvent);
               // var serialized = ToJson(se,true);
               // builder.Append(serialized);
            }
        }

    }
}