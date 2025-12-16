using CircuitBreakerDemo.Core.Interfaces;

namespace CircuitBreakerDemo.Core.Services;

/// <summary>
/// Represents a downstream service that is slower but highly reliable.
/// </summary>
public class BackupService : IDownstreamService
{
    private readonly Random _random = new();

    public async Task<string> MakeRequestAsync()
    {
        // Simulate higher, but consistent, network latency
        await Task.Delay(_random.Next(300, 500));

        // 99% success rate - very stable
        if (_random.NextDouble() > 0.99)
        {
            throw new HttpRequestException("Simulated rare failure in BackupService.");
        }

        return "Success from BackupService";
    }
}