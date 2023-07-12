using System.Text;
using System.Text.Json;
using CliWrap;
using Microsoft.Extensions.Options;

namespace Bitwarden.SecretOperator.CliWrapping;

public class BitwardenCliWrapper : BackgroundService
{
    private readonly BitwardenOperatorOptions _operatorOptions;
    private readonly BitwardenCredentials _credentials;
    private readonly ILogger<BitwardenCliWrapper> _logger;

    private string _sessionId;
    private bool _isLoggedIn;
    private DateTime? _lastSync;

    public BitwardenCliWrapper(BitwardenCredentials credentials, ILogger<BitwardenCliWrapper> logger, IOptions<BitwardenOperatorOptions> operatorOptions)
    {
        _credentials = credentials;
        _logger = logger;
        _operatorOptions = operatorOptions.Value;
    }

    public bool IsReady { get; set; }

    public async Task LoginAsync()
    {
        var stdOutBuffer = new StringBuilder();
        var stdErrBuffer = new StringBuilder();

        try
        {
            _logger.LogInformation("using `bw login` command");
            CommandResult loginResult = await Cli.Wrap("bw")
                .WithArguments(args => args
                    .Add("login")
                    .Add("--apikey")
                    .Add("--nointeraction")
                )
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithEnvironmentVariables(new Dictionary<string, string?>
                {
                    { "BW_CLIENTID", _credentials.ClientId },
                    { "BW_CLIENTSECRET", _credentials.ClientSecret }
                })
                .ExecuteAsync();
        }
        catch (Exception e)
        {
            string stdErr = stdErrBuffer.ToString();
            _logger.LogError(e, "bw login: {error}", stdErr);
        }
    }

    public async Task UnlockAsync()
    {
        var stdOutBuffer = new StringBuilder();
        var stdErrBuffer = new StringBuilder();

        try
        {
            _logger.LogInformation("using `bw unlock` command");
            CommandResult result = await Cli.Wrap("bw")
                .WithArguments(args => args
                    .Add("unlock")
                    .Add("--passwordenv")
                    .Add("BW_PASSWORD")
                    .Add("--nointeraction")
                )
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithEnvironmentVariables(new Dictionary<string, string?>
                {
                    { "BW_CLIENTID", _credentials.ClientId },
                    { "BW_CLIENTSECRET", _credentials.ClientSecret },
                    { "BW_PASSWORD", _credentials.ClientPassword }
                })
                .ExecuteAsync();
            string stdOut = stdOutBuffer.ToString();

            const string beginning = "BW_SESSION=\"";
            int tmp = stdOut.IndexOf(beginning);
            string sessionIdBuffer = stdOut[(tmp + beginning.Length)..];
            int endOfSessionKey = sessionIdBuffer.IndexOf('"');
            _sessionId = sessionIdBuffer[..endOfSessionKey];
            _lastSync = DateTime.UtcNow;
        }
        catch (Exception e)
        {
            string stdErr = stdErrBuffer.ToString();
            _logger.LogError(e, "bw unlock: {error}", stdErr);
        }
    }

    public async Task<BitwardenItem?> GetAsync(string itemId)
    {
        var stdOutBuffer = new StringBuilder();
        var stdErrBuffer = new StringBuilder();

        try
        {
            _logger.LogInformation("using `bw get item` command");
            CommandResult result = await Cli.Wrap("bw")
                .WithArguments(args => args
                    .Add("--response")
                    .Add("get")
                    .Add("item")
                    .Add(itemId)
                    .Add("--nointeraction")
                )
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithEnvironmentVariables(new Dictionary<string, string?>
                {
                    { "BW_SESSION", _sessionId },
                })
                .ExecuteAsync();
            string stdOut = stdOutBuffer.ToString();
            
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("`bw get item`: {Debug}", stdOut);
            }

            var item = JsonSerializer.Deserialize<BitwardenCliResponse<BitwardenItem>>(stdOut);
            if (item is { Success: false })
            {
                _logger.LogWarning("`bw get item`: {Error}", stdOut);
            }

            return item!.Data;
        }
        catch (Exception e)
        {
            string stdErr = stdErrBuffer.ToString();
            _logger.LogError(e, "`bw get item`: {ItemId}: {Error}", itemId, stdErr);
            return null;
        }
    }

    public async Task SynchronizeAsync()
    {
        var stdErrBuffer = new StringBuilder();
        var stdOutBuffer = new StringBuilder();
        try
        {
            _logger.LogInformation("using `bw sync` command");
            CommandResult result = await Cli.Wrap("bw")
                .WithArguments(args => args
                    .Add("sync")
                )
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithEnvironmentVariables(new Dictionary<string, string?>
                {
                    { "BW_SESSION", _sessionId },
                })
                .ExecuteAsync();
        }
        catch (Exception e)
        {
            string stdErr = stdErrBuffer.ToString();
            _logger.LogError(e, "bw sync: {Error}", stdErr);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_lastSync == null)
                {
                    await Task.Delay(_operatorOptions.RefreshRate, stoppingToken);
                    continue;
                }

                DateTime now = DateTime.UtcNow;
                TimeSpan timeBeforeNewRefresh = _lastSync.Value + _operatorOptions.RefreshRate - now;
                if (timeBeforeNewRefresh.TotalMilliseconds > 0)
                {
                    await Task.Delay(timeBeforeNewRefresh, stoppingToken);
                    continue;
                }

                _logger.LogInformation("[{Scope}] Synchronizing...", nameof(BitwardenCliWrapper));
                
                await SynchronizeAsync();
                _logger.LogInformation("[{Scope}] Synchronized", nameof(BitwardenCliWrapper));
                // Synchronize may take some time
                _lastSync = DateTime.UtcNow;
                await Task.Delay(_operatorOptions.RefreshRate, stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[{Scope}]", nameof(ExecuteAsync));
            }
        }
    }
}