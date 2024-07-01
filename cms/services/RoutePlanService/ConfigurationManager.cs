using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using commUtils.Utils;
using commUtils.Watchdog;
using Microsoft.Extensions.Configuration;
using RoutePlanLib;
using XSD;

namespace RoutePlanService
{
	internal class ConfigurationManager
	{
		private IConfiguration m_cloudConfiguration;
		private XSD.configuration m_fileConfiguration;
		
		public ConfigurationManager(IConfiguration conf)
		{
			m_cloudConfiguration = conf;
		}

		public string NMSHost() => m_fileConfiguration?.connection.host ?? m_cloudConfiguration.GetSection("Connection:host").Value;
		public int NMSPort() => m_fileConfiguration?.connection.port ?? Convert.ToInt32(m_cloudConfiguration?.GetSection("Connection:port").Value);
		public string NMSUser() => m_fileConfiguration?.connection.user ?? m_cloudConfiguration.GetSection("Connection:user").Value;
		public string NMSPassword() => m_fileConfiguration?.connection.password ?? m_cloudConfiguration.GetSection("Connection:pass").Value;
		public string NMSRcsMode() => m_fileConfiguration?.connection.rcsNode ?? m_cloudConfiguration.GetSection("Connection:rcsNode").Value;

		public string WDAppName() => m_fileConfiguration?.watchdog.appName ?? m_cloudConfiguration.GetSection("Watchdog:appName").Value;
		public Watchdog1_6.ELaunchMode WDMode()
		{
			if (Enum.TryParse(typeof(Watchdog1_6.ELaunchMode), m_fileConfiguration?.watchdog.mode ?? m_cloudConfiguration.GetSection("Watchdog:mode").Value, out Object? mode))
				return (Watchdog1_6.ELaunchMode)mode;

			return Watchdog1_6.ELaunchMode.Automatic;
		}
		public string WDSendTo() => m_fileConfiguration?.watchdog.sendTo ?? m_cloudConfiguration.GetSection("Watchdog:sendTo").Value;
		public string WDReceiveFrom() => m_fileConfiguration?.watchdog.receiveFrom ?? m_cloudConfiguration.GetSection("Watchdog:receiveFrom").Value;
		public string TmsReqSchema() => m_fileConfiguration?.connection.schemas.tmsreq ?? m_cloudConfiguration.GetSection("Connection:schemas:tmsreq").Value;
		public string TmsCancelReqSchema() => m_fileConfiguration?.connection.schemas.tmscancelreq ?? m_cloudConfiguration.GetSection("Connection:schemas:tmscancelreq").Value;
		public string CtcResQueue() => m_fileConfiguration?.connection.queues.ctcres?? m_cloudConfiguration.GetSection("Connection:queues:ctcres").Value;
		public string TmsReqTopic() => m_fileConfiguration?.connection.topics.tmsreq ?? m_cloudConfiguration.GetSection("Connection:topics:tmsreq").Value;
		public string TSInfoTopic() => m_fileConfiguration?.connection.topics.tsinfo?? m_cloudConfiguration.GetSection("Connection:topics:tsinfo").Value;
	}
}
