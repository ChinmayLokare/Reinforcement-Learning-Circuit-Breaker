using CircuitBreakerDemo.Core.Interfaces;

namespace CircuitBreakerDemo.Core.Services;

public class HealthyService : IDownstreamService
{
    private readonly Random _random = new();

    public async Task<string> MakeRequestAsync()
    {
        // Simulate network latency
        await Task.Delay(_random.Next(50, 150));

        // 95% success rate
        if (_random.NextDouble() > 0.95)
        {
            throw new HttpRequestException("Simulated network failure in HealthyService.");
        }

        return "Success from HealthyService";
    }
}