using Bitwarden.SecretOperator;
using Bitwarden.SecretOperator.CliWrapping;
using KubeOps.KubernetesClient;
using KubeOps.Operator;
using KubeOps.Operator.Builder;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

IServiceCollection services = builder.Services;

services.AddLogging(s =>
{
    s.ClearProviders();
    s.AddSerilog(new LoggerConfiguration()
        .Enrich.FromLogContext()
        .WriteTo.Console(theme: AnsiConsoleTheme.Code, outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}][{Level:u4}][Thread:{ThreadId}] {Message:lj} {NewLine}{Exception}")
        .CreateLogger()
    );
});

IOperatorBuilder operatorBuilder = services.AddKubernetesOperator()
#if DEBUG
    .AddWebhookLocaltunnel()
#endif
    ;

operatorBuilder.AddReadinessCheck<ReadyHealthcheck>();
operatorBuilder.AddHealthCheck<LiveHealthcheck>();
operatorBuilder.AddLivenessCheck<BasicHealthcheck>();

services.AddSingleton<BitwardenCredentials>(new BitwardenCredentials
{
    ClientId = builder.Configuration.GetValue<string>("BW_CLIENTID") ?? throw new InvalidOperationException("BW_CLIENTID is missing"),
    ClientSecret = builder.Configuration.GetValue<string>("BW_CLIENTSECRET") ?? throw new InvalidOperationException("BW_CLIENTSECRET is missing"),
    ClientPassword = builder.Configuration.GetValue<string>("BW_PASSWORD") ?? throw new InvalidOperationException("BW_PASSWORD is missing"),
});
services.Configure<BitwardenOperatorOptions>(builder.Configuration.GetSection(nameof(BitwardenOperatorOptions)));
services.AddSingleton<BitwardenCliWrapper>();
services.AddHostedService<BitwardenCliWrapper>(s => s.GetRequiredService<BitwardenCliWrapper>());
services.AddSingleton<KubernetesClient>();

WebApplication app = builder.Build();

app.UseKubernetesOperator();

var cliWrapper = app.Services.GetRequiredService<BitwardenCliWrapper>();
app.Logger.LogInformation("Logging in...");
await cliWrapper.LoginAsync();
app.Logger.LogInformation("Unlocking...");
await cliWrapper.UnlockAsync();

cliWrapper.IsReady = true;

app.Logger.LogInformation("Started...");
await app.RunOperatorAsync(args);