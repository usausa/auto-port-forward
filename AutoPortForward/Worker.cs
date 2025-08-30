namespace AutoPortForward;

using Microsoft.Extensions.Options;

using Renci.SshNet;
using Renci.SshNet.Common;

internal class Worker : BackgroundService
{
    private readonly ILogger<Worker> log;

    private readonly SshSetting setting;

    private SshClient? client;

    public Worker(
        ILogger<Worker> log,
        IOptions<SshSetting> setting)
    {
        this.log = log;
        this.setting = setting.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reconnectDelay = TimeSpan.FromSeconds(setting.ReconnectDelay);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if ((client is null) || !client.IsConnected)
                {
                    await SetupConnection(stoppingToken);
                }

                if (client is not null)
                {
                    while (client.IsConnected)
                    {
                        await Task.Delay(1000, stoppingToken);
                    }
                }
            }
            catch (Exception e)
            {
                log.ErrorUnhandledException(e);
            }

            await Task.Delay(reconnectDelay, stoppingToken);
        }
    }

    private async ValueTask SetupConnection(CancellationToken stoppingToken)
    {
        try
        {
            client = new SshClient(setting.Host, setting.Port, setting.Username, new PrivateKeyFile(setting.PrivateKey, setting.PassPhase));
            client.ErrorOccurred += ClientOnErrorOccurred;

            client.KeepAliveInterval = TimeSpan.FromSeconds(setting.KeepAliveInterval);

            await client.ConnectAsync(stoppingToken);

            log.InfoClientConnected();

            foreach (var forward in setting.PortForwards)
            {
                var forwardedPort = forward.Remote
                    ? (ForwardedPort)new ForwardedPortRemote(forward.BoundHost, forward.BoundPort, forward.Host, forward.Port)
                    : new ForwardedPortLocal(forward.BoundHost, forward.BoundPort, forward.Host, forward.Port);
                client.AddForwardedPort(forwardedPort);
                forwardedPort.Start();
            }
        }
        catch (Exception e)
        {
            log.ErrorConnectFailed(e);

            client?.Dispose();
            client = null;
            throw;
        }
    }

    private void ClientOnErrorOccurred(object? sender, ExceptionEventArgs e)
    {
        log.ErrorErrorOccurred(e.Exception);
    }
}
