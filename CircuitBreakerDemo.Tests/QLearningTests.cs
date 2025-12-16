using Xunit;
using FluentAssertions;
using CircuitBreakerDemo.Core.ReinforcementLearning;
using System;

namespace CircuitBreakerDemo.Tests
{
    public class QLearningTests
    {
        [Theory]
        [InlineData(SimplifiedCircuitState.Closed, 0.1, 0.1, ResponseTimeCategory.Fast, 0 + (0*3) + (0*3*4) + (0*3*4*4))] // State index should be 0
        [InlineData(SimplifiedCircuitState.Open, 0.4, 0.1, ResponseTimeCategory.Fast, 1 + (1*3) + (0*3*4) + (0*3*4*4))] // State index should be 4
        [InlineData(SimplifiedCircuitState.HalfOpen, 0.9, 0.6, ResponseTimeCategory.Slow, 2 + (3*3) + (2*3*4) + (1*3*4*4))] // State index should be 83
        public void GetStateIndex_ShouldCalculateCorrectUniqueIndex(SimplifiedCircuitState cs, double pfr, double bfr, ResponseTimeCategory rtc, int expectedIndex)
        {
            // Act
            int stateIndex = QLearningAgent.GetStateIndex(cs, pfr, bfr, rtc);

            // Assert
            stateIndex.Should().Be(expectedIndex);
        }

        [Fact]
        public void UpdateQValue_ShouldCorrectlyApplyBellmanEquation()
        {
            // Arrange
            // Agent with deterministic parameters for easy calculation
            var agent = new QLearningAgent(learningRate: 0.1, discountFactor: 0.9, explorationRate: 0);
            
            // Initial Q-values are all ~0. Let's set the one we are testing explicitly.
            int prevState = 5;
            int nextState = 10;
            RLAction action = RLAction.OpenFor5s;
            double reward = 10;
            agent.QTable[prevState, (int)action] = 0.5; // Old Q-value
            agent.QTable[nextState, (int)RLAction.KeepClosed] = 2.0; // Max Q-value for next state
            agent.QTable[nextState, (int)RLAction.UsePrimary] = 3.0; // This is the actual max Q

            // Q(s,a) = Q(s,a) + alpha * [reward + gamma * max(Q(s',a')) - Q(s,a)]
            // Expected = 0.5 + 0.1 * [10 + 0.9 * 3.0 - 0.5]
            // Expected = 0.5 + 0.1 * [10 + 2.7 - 0.5]
            // Expected = 0.5 + 0.1 * [12.2]
            // Expected = 0.5 + 1.22 = 1.72
            double expectedNewQValue = 1.72;

            // Act
            agent.UpdateQValue(prevState, action, reward, nextState);

            // Assert
            agent.QTable[prevState, (int)action].Should().BeApproximately(expectedNewQValue, 0.001);
        }

        [Fact]
        public void RLState_AddOutcome_ShouldCorrectlyCalculateFailureRate()
        {
            // Arrange
            var state = new RLState();
            var duration = TimeSpan.FromMilliseconds(100);

            // Act
            // Add 10 outcomes to the primary path: 7 successes, 3 failures
            for (int i = 0; i < 7; i++) state.AddOutcome(true, duration, ServicePath.Primary);
            for (int i = 0; i < 3; i++) state.AddOutcome(false, duration, ServicePath.Primary);
            
            // Add 1 outcome to the backup path
            state.AddOutcome(true, duration, ServicePath.Backup);

            // Assert
            state.PrimaryFailureRate.Should().BeApproximately(0.3, 0.001); // 3 failures / 10 total
            state.BackupFailureRate.Should().BeApproximately(0.0, 0.001); // 0 failures / 1 total
        }
    }
}