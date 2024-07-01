using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace RoutePlanService
{
	internal class HostAppService : BackgroundService
	{
		private readonly ILogger<HostAppService> m_logger;
		private readonly IConfiguration m_cloudConfiguration;

		public HostAppService(ILogger<HostAppService> logger, IConfiguration conf)
		{
			m_logger = logger;
			m_cloudConfiguration = conf;

			string host = m_cloudConfiguration.GetSection("connection:host").Value;
		}
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			m_logger.LogWarning("Hello from logger!");
			MainApp mainApp = new MainApp(m_cloudConfiguration);
			if (!mainApp.Start())
				return;

			while (!stoppingToken.IsCancellationRequested)
			{
				await Task.Delay(1000, stoppingToken);
			}	
		}
	}
}
