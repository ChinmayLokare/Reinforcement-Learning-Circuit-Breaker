namespace CircuitBreakerDemo.Core.ReinforcementLearning;

// Represents the simplified circuit state for our RL model
public enum SimplifiedCircuitState
{
    Closed = 0,
    Open = 1,
    HalfOpen = 2
}

// Represents the buckets for response time
public enum ResponseTimeCategory
{
    Fast = 0,
    Slow = 1
}

// Defines all possible actions the agent can take
public enum RLAction
{
    KeepClosed = 0,
    OpenFor5s = 1,
    OpenFor10s = 2,
    OpenFor20s = 3,
    TryHalfOpen = 4,
    UsePrimary = 5,
    UseBackup = 6
}


public enum ServicePath { Primary, Backup }