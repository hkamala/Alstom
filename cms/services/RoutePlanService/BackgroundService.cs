using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace RoutePlanService
{
	public abstract class BackgroundService : IHostedService, IDisposable
	{
		private Task m_executingTask;
		private readonly CancellationTokenSource m_stoppingCts = new CancellationTokenSource();
		
		protected abstract Task ExecuteAsync(CancellationToken stoppingToken);

		public virtual Task StartAsync(CancellationToken cancellationToken)
		{
			m_executingTask = ExecuteAsync(m_stoppingCts.Token);

			if (m_executingTask.IsCompleted)
				return m_executingTask;

			return Task.CompletedTask;
		}

		public virtual async Task StopAsync(CancellationToken cancellationToken)
		{
			if (m_executingTask == null)
				return;

			try
			{
				m_stoppingCts.Cancel();
			}
			finally
			{
				await Task.WhenAny(m_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
			}
		}

		public void Dispose()
		{
			m_stoppingCts.Cancel();
		}
	}
}
