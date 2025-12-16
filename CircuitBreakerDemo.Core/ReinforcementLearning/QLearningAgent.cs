using System.Text.Json.Serialization;

namespace CircuitBreakerDemo.Core.ReinforcementLearning;

public class QLearningAgent
{
    // Hyperparameters
    private readonly double _learningRate;
    private readonly double _discountFactor;
    private double _explorationRate;

    private readonly Random _random = new();

    // State and Action Space
    private const int NumCircuitStates = 3;       // Closed, Open, HalfOpen
    private const int NumFailureRateBuckets = 4;  // 0-25%, 25-50%, 50-75%, 75-100%
    private const int NumResponseTimeBuckets = 2; // Fast, Slow
    
    // The state space is now larger to include the health of the backup service.
    public const int NumStates = NumCircuitStates * NumFailureRateBuckets * NumFailureRateBuckets * NumResponseTimeBuckets;
    
    // The action space is larger to include path selection.
    public const int NumActions = 7; // 5 CB actions + 2 Path actions

    // --- The Q-Table ---
    [JsonInclude]
    public double[,] QTable { get; private set; }

    public QLearningAgent(double learningRate = 0.1, double discountFactor = 0.9, double explorationRate = 0.3)
    {
        _learningRate = learningRate;
        _discountFactor = discountFactor;
        _explorationRate = explorationRate;

        // Initialize the larger Q-Table
        QTable = new double[NumStates, NumActions];
        for (int i = 0; i < NumStates; i++)
        {
            for (int j = 0; j < NumActions; j++)
            {
                QTable[i, j] = _random.NextDouble() * 0.1;
            }
        }
    }

    /// <summary>
    /// Converts raw environment features, now including both primary and backup service health,
    /// into a single discrete state index for the Q-Table.
    /// </summary>
    public static int GetStateIndex(SimplifiedCircuitState circuitState, double primaryFailureRate, double backupFailureRate, ResponseTimeCategory responseTime)
    {
        // Helper function to discretize failure rates into buckets.
        int bucketize(double rate) => rate switch
        {
            < 0.25 => 0,
            < 0.50 => 1,
            < 0.75 => 2,
            _      => 3
        };

        int primaryFailureBucket = bucketize(primaryFailureRate);
        int backupFailureBucket = bucketize(backupFailureRate);
        
        // This mapping formula flattens the 4D state space into a unique 1D index.
        // It's structured to ensure that each unique combination of features maps to a unique integer.
        int index = (int)circuitState +
                    (primaryFailureBucket * NumCircuitStates) +
                    (backupFailureBucket * NumCircuitStates * NumFailureRateBuckets) +
                    ((int)responseTime * NumCircuitStates * NumFailureRateBuckets * NumFailureRateBuckets);
        
        return index;
    }
    

    /// <summary>
    /// Chooses an action based on the epsilon-greedy strategy.
    /// </summary>
    public RLAction ChooseAction(int stateIndex)
    {
        if (_random.NextDouble() < _explorationRate)
        {
            return (RLAction)_random.Next(NumActions); // Explore (Choose a random action)
        }
        else
        {
            // Exploit (Choose the highest possible Q-value)
            double maxQValue = double.MinValue;
            RLAction bestAction = RLAction.KeepClosed;
            for (int i = 0; i < NumActions; i++)
            {
                if (QTable[stateIndex, i] > maxQValue)
                {
                    maxQValue = QTable[stateIndex, i];
                    bestAction = (RLAction)i;
                }
            }
            return bestAction;
        }
    }

    /// <summary>
    /// Updates the Q-Table using the Bellman equation after an action is taken.
    /// </summary>
    public void UpdateQValue(int prevStateIndex, RLAction action, double reward, int nextStateIndex)
    {
        int actionIndex = (int)action;
        double maxQNextState = 0;
        for (int i = 0; i < NumActions; i++)
        {
            if (QTable[nextStateIndex, i] > maxQNextState)
            {
                maxQNextState = QTable[nextStateIndex, i];
            }
        }
        double oldQValue = QTable[prevStateIndex, actionIndex];
        double newQValue = oldQValue + _learningRate * (reward + _discountFactor * maxQNextState - oldQValue);
        QTable[prevStateIndex, actionIndex] = newQValue;
    }

    /// <summary>
    /// Allows changing the exploration rate, e.g., for "training" vs "trained" modes.
    /// </summary>
    public void SetExplorationRate(double rate)
    {
        _explorationRate = Math.Clamp(rate, 0.0, 1.0);
    }
}