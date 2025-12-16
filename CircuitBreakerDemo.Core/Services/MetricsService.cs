using CircuitBreakerDemo.Core.Models;
using System.Collections.ObjectModel;

namespace CircuitBreakerDemo.Core.Services;

public class MetricsService : IMetricsService
{
    public int SuccessCount { get; private set; }
    public int FailureCount { get; private set; }
    public ObservableCollection<MetricEvent> Events { get; } = new();

    public void RecordSuccess(string source, TimeSpan duration)
    {
        SuccessCount++;
        AddEvent(new MetricEvent(DateTime.UtcNow, source, "Request Succeeded", duration));
    }

    public void RecordFailure(string source, TimeSpan duration)
    {
        FailureCount++;
        AddEvent(new MetricEvent(DateTime.UtcNow, source, "Request Failed", duration));
    }

    public void RecordCircuitStateChange(string source, string newState)
    {
        AddEvent(new MetricEvent(DateTime.UtcNow, source, $"Circuit state changed to: {newState}"));
    }

    public void Reset()
    {
        SuccessCount = 0;
        FailureCount = 0;
        Events.Clear();
    }

    private void AddEvent(MetricEvent newEvent)
    {
        
        if (Events.Count > 200)
        {
            Events.RemoveAt(0);
        }
        Events.Add(newEvent);
    }
}