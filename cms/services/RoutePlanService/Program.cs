using System;
using System.Linq;
using System.Threading;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace RoutePlanService
{
	public class Program
	{
		public static void Main(string[] args)
		{
			Host.CreateDefaultBuilder(args)
			.ConfigureAppConfiguration((hostingContext, config) =>
			{
			})
			.UseSerilog(
				(hostingContext, loggerConfiguration) =>
				{
					loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration);
				}
			)
			.ConfigureServices(services =>
			{
				services.AddHostedService<HostAppService>();
			}).Build().Run();
		}
	}
}