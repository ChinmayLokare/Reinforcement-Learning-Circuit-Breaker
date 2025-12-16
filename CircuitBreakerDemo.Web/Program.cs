using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CircuitBreakerDemo.Web;
using CircuitBreakerDemo.Core.Services;
using Radzen;
using CircuitBreakerDemo.Core.Configuration;
using Microsoft.Extensions.Options;
using CircuitBreakerDemo.Core.ReinforcementLearning;

using Serilog; 
using Serilog.Core;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var levelSwitch = new LoggingLevelSwitch();
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.ControlledBy(levelSwitch)
    .Enrich.FromLogContext()
    .WriteTo.BrowserConsole()
    .CreateLogger();

builder.Logging.AddSerilog();

builder.Services.AddRadzenComponents();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddOptions<CircuitBreakerSettings>()
    .Bind(builder.Configuration.GetSection("CircuitBreakerSettings"));

builder.Services.AddSingleton<UnstableService>();
builder.Services.AddSingleton<BackupService>();

builder.Services.AddSingleton<IMetricsService, MetricsService>();
builder.Services.AddSingleton<HealthyService>();
builder.Services.AddSingleton<StaticCircuitBreakerService>();
builder.Services.AddSingleton<QLearningAgent>();
builder.Services.AddSingleton<RLCircuitBreakerService>();
builder.Services.AddSingleton<SimulationService>();
await builder.Build().RunAsync();
