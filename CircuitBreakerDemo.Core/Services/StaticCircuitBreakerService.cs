using CircuitBreakerDemo.Core.Configuration;
using CircuitBreakerDemo.Core.Interfaces;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Serilog;
using System.Diagnostics;

namespace CircuitBreakerDemo.Core.Services;

public class StaticCircuitBreakerService
{
    private readonly AsyncCircuitBreakerPolicy _breakerPolicy;
    private readonly IMetricsService _metricsService;
    private readonly ILogger _logger;
    private const string SourceName = "StaticCB";

    public CircuitState State => _breakerPolicy.CircuitState;

    /// <summary>
    /// Creates a new static circuit breaker using the provided settings and metrics service.
    /// </summary>
    public StaticCircuitBreakerService(IOptions<CircuitBreakerSettings> options, IMetricsService metricsService)
    {
        _metricsService = metricsService;
        _logger = Log.ForContext<StaticCircuitBreakerService>();
        var settings = options.Value;

        _breakerPolicy = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                // Number of consecutive exceptions before breaking the circuit
                exceptionsAllowedBeforeBreaking: 3, // Threshold : Break the CB after 3 failures for 10s
                // Timespan to stay open before switching to half-open
                durationOfBreak: TimeSpan.FromSeconds(settings.DurationOfBreakSeconds), 
                // Action to take when the circuit breaks
                onBreak: (ex, timespan) =>
                {
                    _logger.Warning("Circuit broken due to: {ExceptionMessage}. Breaking for {BreakTime} ms.", ex.Message, timespan.TotalMilliseconds);
                    _metricsService.RecordCircuitStateChange(SourceName, "Open");
                },
                // Action to take when the circuit resets to closed
                onReset: () =>
                {
                    _logger.Information("Circuit reset to Closed state.");
                    _metricsService.RecordCircuitStateChange(SourceName, "Closed");
                },
                // Action to take when the circuit enters half-open
                onHalfOpen: () =>
                {
                    _logger.Warning("Circuit is now Half-Open. Next request will test the service.");
                    _metricsService.RecordCircuitStateChange(SourceName, "Half-Open");
                }
            );
    }

    /// <summary>
    /// Safely executes a network call to the downstream service using a protective circuit breaker.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await _breakerPolicy.ExecuteAsync(action);
            stopwatch.Stop();
            _metricsService.RecordSuccess(SourceName, stopwatch.Elapsed);
            return result;
        }
        catch (BrokenCircuitException)
        {
            stopwatch.Stop();
            _logger.Error("Request blocked by circuit breaker. Circuit is Open.");
            _metricsService.RecordFailure(SourceName, stopwatch.Elapsed);
            // Re-throw a more user-friendly exception or handle as needed
            throw new Exception("Service is currently unavailable. Please try again later.");
        }
        catch (Exception)
        {
            stopwatch.Stop();
            // The onBreak delegate handles logging the failure that trips the breaker
            _metricsService.RecordFailure(SourceName, stopwatch.Elapsed);
            throw; // Re-throw the original exception
        }
    }
}