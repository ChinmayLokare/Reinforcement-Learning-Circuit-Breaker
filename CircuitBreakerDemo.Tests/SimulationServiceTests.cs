using Xunit;
using Moq;
using FluentAssertions;
using CircuitBreakerDemo.Core.Services;
using CircuitBreakerDemo.Core.Interfaces;
using CircuitBreakerDemo.Core.ReinforcementLearning;
using Microsoft.Extensions.Options;
using CircuitBreakerDemo.Core.Configuration;
using System.Threading.Tasks;

namespace CircuitBreakerDemo.Tests
{
    public class SimulationServiceTests
    {
        [Fact]
        public async Task RunRequestAsync_WhenPrimaryServiceFails_ShouldRecordFailuresForAllBreakers()
        {
            // Arrange
            var metricsService = new MetricsService(); // Use the real in-memory service
            var options = Options.Create(new CircuitBreakerSettings());
            
            // Mock the downstream services to always fail
            var mockPrimary = new Mock<UnstableService>();
            mockPrimary.Setup(s => s.MakeRequestAsync()).ThrowsAsync(new System.Exception("Primary Failed"));
            
            var mockBackup = new Mock<BackupService>();
            mockBackup.Setup(s => s.MakeRequestAsync()).ThrowsAsync(new System.Exception("Backup Failed"));

            // Create real services using the mocks and real metrics
            var staticCb = new StaticCircuitBreakerService(options, metricsService);
            var agent = new QLearningAgent();
            var rlCb = new RLCircuitBreakerService(agent, metricsService, mockPrimary.Object, mockBackup.Object);
            
            var simulationService = new SimulationService(staticCb, rlCb, mockPrimary.Object);

            // Act
            await simulationService.RunRequestAsync();

            // Assert
            // One failure should be recorded from the Static CB
            metricsService.Events.Should().ContainSingle(e => e.Source == "StaticCB" && e.Message == "Request Failed");
            // One failure should be recorded from the RL CB
            metricsService.Events.Should().ContainSingle(e => e.Source == "RL_CB" && e.Message == "Request Failed");
        }
    }
}