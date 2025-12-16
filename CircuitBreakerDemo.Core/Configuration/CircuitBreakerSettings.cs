namespace CircuitBreakerDemo.Core.Configuration;

public class CircuitBreakerSettings
{
    public double FailureThreshold { get; set; } = 0.5;
    public int DurationOfBreakSeconds { get; set; } = 10;
}