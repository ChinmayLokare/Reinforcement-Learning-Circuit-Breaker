namespace CircuitBreakerDemo.Core.Interfaces;

public interface IDownstreamService
{
    Task<string> MakeRequestAsync();
}