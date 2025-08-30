namespace AutoPortForward
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> log;

        public Worker(ILogger<Worker> log)
        {
            this.log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (log.IsEnabled(LogLevel.Information))
                {
#pragma warning disable CA1848
                    log.LogInformation("Worker running at: {Time}", DateTimeOffset.Now);
#pragma warning restore CA1848
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
