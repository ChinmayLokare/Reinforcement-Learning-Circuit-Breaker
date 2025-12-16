namespace CircuitBreakerDemo.Core.ReinforcementLearning;

/// <summary>
/// Represents the complete observable state of the environment for the Q-Learning agent.
/// It now tracks the health of both primary and backup services independently.
/// </summary>
public class RLState
{
    private const int WindowSize = 10; // Tracks the last 10 outcomes for each service
    private const double SlowResponseThresholdMs = 250;

    // A sliding window of the last 10 outcomes (true=success, false=failure) for the Primary service.
    private readonly Queue<bool> _primaryOutcomes = new(WindowSize);

    // A separate sliding window for the Backup service.
    private readonly Queue<bool> _backupOutcomes = new(WindowSize);

    // --- State Features ---
    public SimplifiedCircuitState CircuitState { get; set; }
    public double PrimaryFailureRate { get; private set; }
    public double BackupFailureRate { get; private set; }
    public ResponseTimeCategory ResponseTimeCat { get; private set; }

    /// <summary>
    /// Records the outcome of a service call and updates the relevant state features.
    /// </summary>
    /// <param name="success">Whether the call succeeded.</param>
    /// <param name="duration">The duration of the call.</param>
    /// <param name="path">Which service path (Primary or Backup) this outcome belongs to.</param>
    public void AddOutcome(bool success, TimeSpan duration, ServicePath path)
    {
        // Select the correct queue based on which service was called.
        var outcomeQueue = path == ServicePath.Primary ? _primaryOutcomes : _backupOutcomes;

        // Maintain the sliding window size.
        if (outcomeQueue.Count == WindowSize)
        {
            outcomeQueue.Dequeue();
        }
        outcomeQueue.Enqueue(success);

        // Recalculate failure rates for both services.
        // If a queue has no entries, its failure rate is 0.
        PrimaryFailureRate = _primaryOutcomes.Any()
            ? _primaryOutcomes.Count(o => !o) / (double)_primaryOutcomes.Count
            : 0;

        BackupFailureRate = _backupOutcomes.Any()
            ? _backupOutcomes.Count(o => !o) / (double)_backupOutcomes.Count
            : 0;

        // Update the response time category based on the most recent call.
        ResponseTimeCat = duration.TotalMilliseconds > SlowResponseThresholdMs
            ? ResponseTimeCategory.Slow
            : ResponseTimeCategory.Fast;
    }

    /// <summary>
    /// Converts the current state features into a single, unique integer index
    /// that can be used to look up values in the Q-Table.
    /// </summary>
    public int ToStateIndex()
    {
        // This now calls the updated GetStateIndex method that accepts the health of both services.
        return QLearningAgent.GetStateIndex(
            CircuitState,
            PrimaryFailureRate,
            BackupFailureRate,
            ResponseTimeCat);
    }
}


