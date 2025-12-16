namespace CircuitBreakerDemo.Core.Services;

/// <summary>
/// Orchestrates running a single time-step of the simulation,
/// allowing a side-by-side comparison of the static vs. RL circuit breaker strategies.
/// </summary>
public class SimulationService
{
    private readonly StaticCircuitBreakerService _staticCb;
    private readonly RLCircuitBreakerService _rlCb;
    private readonly UnstableService _primaryService;

    public SimulationService(StaticCircuitBreakerService staticCb, RLCircuitBreakerService rlCb, UnstableService primaryService)
    {
        _staticCb = staticCb;
        _rlCb = rlCb;
        _primaryService = primaryService;
    }

    public async Task RunRequestAsync()
    {
        // --- Simulate the Static Circuit Breaker's Strategy ---
        // Its strategy is simple: always call the primary service and apply the fixed circuit breaker policy.
        try
        {
            await _staticCb.ExecuteAsync(() => _primaryService.MakeRequestAsync());
        }
        catch 
        { 
            // Exceptions are expected and handled internally by the service for metric collection.
        }

        // --- Simulate the RL Circuit Breaker's Strategy ---
        // Its strategy is complex: decide whether to change path or policy, then execute the request.
        try
        {
            await _rlCb.ExecuteAsync();
        }
        catch 
        { 
            // Exceptions are expected and handled internally by the service for metric collection.
        }
    }
}