using Bitwarden.SecretOperator;
using Bitwarden.SecretOperator.CliWrapping;
using KubeOps.KubernetesClient;
using KubeOps.Operator;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

IServiceCollection services = builder.Services;

services.AddLogging(s =>
    s.AddSerilog(new LoggerConfiguration()
        .Enrich.FromLogContext()
        .WriteTo.Console(theme: AnsiConsoleTheme.Code, outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}][{Level:u4}][Thread:{ThreadId}] {Message:lj} {NewLine}{Exception}")
        .CreateLogger()
    )
);
services.AddKubernetesOperator()
#if DEBUG
    .AddWebhookLocaltunnel()
#endif
    ;

services.AddSingleton<BitwardenCredentials>(new BitwardenCredentials
{
    ClientId = builder.Configuration.GetValue<string>("BW_CLIENTID") ?? throw new InvalidOperationException("BW_CLIENTID is missing"),
    ClientSecret = builder.Configuration.GetValue<string>("BW_CLIENTSECRET") ?? throw new InvalidOperationException("BW_CLIENTSECRET is missing"),
    ClientPassword = builder.Configuration.GetValue<string>("BW_PASSWORD") ?? throw new InvalidOperationException("BW_PASSWORD is missing"),
});
services.Configure<BitwardenOperatorOptions>(builder.Configuration.GetSection(nameof(BitwardenOperatorOptions)));
services.AddSingleton<BitwardenCliWrapper>();
services.AddSingleton<KubernetesClient>();

WebApplication app = builder.Build();

app.UseKubernetesOperator();

var cliWrapper = app.Services.GetRequiredService<BitwardenCliWrapper>();
await cliWrapper.LoginAsync();

await app.RunOperatorAsync(args);