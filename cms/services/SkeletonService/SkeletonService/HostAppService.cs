using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace E2KService
{
	internal class HostAppService : BackgroundService
	{
		IConfiguration m_conf;

		public HostAppService(ILogger<HostAppService> logger, IConfiguration conf)
		{
			m_conf = conf;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			Log.Information("Logging started");
			var service = new E2KService.ServiceImp();
			E2KService.ServiceImp.Service = service;

			if (service.Init(m_conf))
			{
				service.Run();
			}

			service.Exit();
		}
	}
}
