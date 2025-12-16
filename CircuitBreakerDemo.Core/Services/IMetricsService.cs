using CircuitBreakerDemo.Core.Models;
using System.Collections.ObjectModel;

namespace CircuitBreakerDemo.Core.Services;

public interface IMetricsService
{
    int SuccessCount { get; }
    int FailureCount { get; }
    ObservableCollection<MetricEvent> Events { get; }

    void RecordSuccess(string source, TimeSpan duration);
    void RecordFailure(string source, TimeSpan duration);
    void RecordCircuitStateChange(string source, string newState);
    void Reset();
}