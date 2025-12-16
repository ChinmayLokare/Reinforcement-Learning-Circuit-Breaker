using CircuitBreakerDemo.Core.Interfaces;

namespace CircuitBreakerDemo.Core.Services;



/// <summary>
/// Represents a downstream service that is faster but unreliable.
/// </summary>
public class UnstableService : IDownstreamService
{
    private readonly Random _random = new();

    
    public double FailureRate { get; set; } = 0.7; // Default to 70% failure

    public async Task<string> MakeRequestAsync()
    {
        // Simulate longer, more variable network latency
        await Task.Delay(_random.Next(100, 400));

        if (_random.NextDouble() < FailureRate)
        {
            throw new HttpRequestException($"Simulated failure from UnstableService (Rate: {FailureRate:P}).");
        }

        return "Success from UnstableService";
    }
}