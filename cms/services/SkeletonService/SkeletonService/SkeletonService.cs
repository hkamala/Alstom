using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Serilog;

// See https://aka.ms/new-console-template for more information

using static E2KService.ServiceImp;
using E2KService;

Host.CreateDefaultBuilder(args)
.ConfigureAppConfiguration((hostingContext, config) =>
{})
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
