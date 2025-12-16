using CircuitBreakerDemo.Core.Interfaces;
using CircuitBreakerDemo.Core.ReinforcementLearning;
using Polly;
using Polly.CircuitBreaker;
using Serilog;
using System.Diagnostics;
using System.Text.Json;

namespace CircuitBreakerDemo.Core.Services;

public class RLCircuitBreakerService
{
    private readonly QLearningAgent _agent;
    private readonly IMetricsService _metricsService;
    private readonly ILogger _logger;
    private readonly RLState _currentState;
    
    // Service path management
    private readonly UnstableService _primaryService;
    private readonly BackupService _backupService;
    private IDownstreamService _activeService;
    private ServicePath _activePath;

    private readonly Dictionary<RLAction, AsyncCircuitBreakerPolicy> _policies = new();
    private AsyncCircuitBreakerPolicy _activePolicy;

    private const string SourceName = "RL_CB";

    public SimplifiedCircuitState State => MapPollyState(_activePolicy.CircuitState);
    public double[,] QTable => _agent.QTable;
    public ServicePath ActivePath => _activePath;
    public int LastUsedStateIndex { get; private set; }
    public RLCircuitBreakerService(QLearningAgent agent, IMetricsService metricsService, UnstableService primaryService, BackupService backupService)
    {
        _agent = agent;
        _metricsService = metricsService;
        _primaryService = primaryService;
        _backupService = backupService;
        _logger = Log.ForContext<RLCircuitBreakerService>();
        _currentState = new RLState();
        
        // Default to using the primary service path on startup
        _activeService = _primaryService;
        _activePath = ServicePath.Primary;

        CreatePolicies();
        _activePolicy = _policies[RLAction.KeepClosed];
    }
    
    // This method now orchestrates the entire learning loop, including calling the correct downstream service.
    public async Task<string> ExecuteAsync()
    {
        // 1. OBSERVE
        _currentState.CircuitState = MapPollyState(_activePolicy.CircuitState);
        int prevStateIndex = _currentState.ToStateIndex();

        this.LastUsedStateIndex = prevStateIndex;

        // 2. CHOOSE ACTION
        var chosenAction = _agent.ChooseAction(prevStateIndex);
        
        // 3. APPLY ACTION (handles path switching and policy changes)
        ApplyAction(chosenAction);

        // 4. MEASURE OUTCOME & GET REWARD
        var stopwatch = Stopwatch.StartNew();
        double reward;
        
        try
        {
            // Call the dynamically selected active service
            string result = await _activeService.MakeRequestAsync();
            stopwatch.Stop();
            
            reward = 10; // Success
            // Record the outcome against the correct service path
            _currentState.AddOutcome(true, stopwatch.Elapsed, _activePath);
            _metricsService.RecordSuccess(SourceName, stopwatch.Elapsed);
            
            // 5. LEARN
            UpdateAgent(prevStateIndex, chosenAction, reward);
            return result;
        }
        catch (BrokenCircuitException)
        {
            stopwatch.Stop();
            reward = 20; // Positive reward for correctly blocking a call
            _currentState.AddOutcome(false, stopwatch.Elapsed, _activePath);
            _metricsService.RecordFailure(SourceName, stopwatch.Elapsed);
            
            UpdateAgent(prevStateIndex, chosenAction, reward);
            throw new Exception($"Service unavailable (RL CB Open on {_activePath}).");
        }
        catch (Exception)
        {
            stopwatch.Stop();
            reward = -10; // Negative reward for a failed call
            _currentState.AddOutcome(false, stopwatch.Elapsed, _activePath);
            _metricsService.RecordFailure(SourceName, stopwatch.Elapsed);
            
            UpdateAgent(prevStateIndex, chosenAction, reward);
            throw;
        }
    }

    private void UpdateAgent(int prevStateIndex, RLAction action, double reward)
    {
        _currentState.CircuitState = MapPollyState(_activePolicy.CircuitState);
        int nextStateIndex = _currentState.ToStateIndex();
        _agent.UpdateQValue(prevStateIndex, action, reward, nextStateIndex);
    }

    // Dispatches actions to either change path or change policy.
    private void ApplyAction(RLAction action)
    {
        // First, handle path switching actions
        if (action == RLAction.UsePrimary)
        {
            if (_activePath != ServicePath.Primary)
            {
                _activeService = _primaryService;
                _activePath = ServicePath.Primary;
                _logger.Information("RL Agent chose action: {Action}.", action);
                _metricsService.RecordCircuitStateChange(SourceName, "Switched to Primary");
            }
            return;
        }
        if (action == RLAction.UseBackup)
        {
            if (_activePath != ServicePath.Backup)
            {
                _activeService = _backupService;
                _activePath = ServicePath.Backup;
                _logger.Information("RL Agent chose action: {Action}.", action);
                _metricsService.RecordCircuitStateChange(SourceName, "Switched to Backup");
            }
            return;
        }

        // If not a path switch, handle circuit breaker policy actions
        if (_policies.ContainsKey(action))
        {
            var newPolicy = _policies[action];
            if (newPolicy != _activePolicy)
            {
                _activePolicy = newPolicy;
                // The enum name itself is descriptive enough for logging and our UI.
                _logger.Information("RL Agent chose circuit breaker action: {Action}.", action);
                _metricsService.RecordCircuitStateChange(SourceName, $"Policy changed: {action}");
            }
        }
    }

    private void CreatePolicies()
    {
        _policies[RLAction.KeepClosed] = Policy.Handle<Exception>().CircuitBreakerAsync(100, TimeSpan.FromHours(1));
        _policies[RLAction.OpenFor5s] = CreateStandardPolicy(5);
        _policies[RLAction.OpenFor10s] = CreateStandardPolicy(10);
        _policies[RLAction.OpenFor20s] = CreateStandardPolicy(20);
        _policies[RLAction.TryHalfOpen] = _policies[RLAction.OpenFor5s];
    }

    private AsyncCircuitBreakerPolicy CreateStandardPolicy(int durationSeconds)
    {
        return Policy.Handle<Exception>().CircuitBreakerAsync(2, TimeSpan.FromSeconds(durationSeconds),
            onBreak: (ex, ts) => _metricsService.RecordCircuitStateChange(SourceName, "Open"),
            onReset: () => _metricsService.RecordCircuitStateChange(SourceName, "Closed"),
            onHalfOpen: () => _metricsService.RecordCircuitStateChange(SourceName, "Half-Open"));
    }

    private SimplifiedCircuitState MapPollyState(CircuitState pollyState)
    {
        return pollyState switch {
            CircuitState.Closed => SimplifiedCircuitState.Closed,
            CircuitState.Open => SimplifiedCircuitState.Open,
            CircuitState.HalfOpen => SimplifiedCircuitState.HalfOpen,
            CircuitState.Isolated => SimplifiedCircuitState.Open,
            _ => SimplifiedCircuitState.Closed
        };
    }

    public void SetTrainingMode(bool isTraining)
    {
        var rate = isTraining ? 0.3 : 0.05;
        _agent.SetExplorationRate(rate);
        _logger.Information("RL Agent exploration rate set to {Rate}", rate);
    }
    
    public Task SaveQTableAsync(string filePath = "qtable.json")
    {
        _logger.Information("SaveQTableAsync called (no-op in browser).");
        return Task.CompletedTask;
    }

    public Task LoadQTableAsync(string filePath = "qtable.json")
    {
        _logger.Information("LoadQTableAsync called (no-op in browser).");
        return Task.CompletedTask;
    }
}