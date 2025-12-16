namespace CircuitBreakerDemo.Core.Models;

public record MetricEvent(DateTime Timestamp, string Source, string Message, TimeSpan? Duration = null);