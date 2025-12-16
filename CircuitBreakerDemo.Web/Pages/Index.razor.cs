using Microsoft.AspNetCore.Components;
using System.Threading;
using CircuitBreakerDemo.Core.Services;
using CircuitBreakerDemo.Core.ReinforcementLearning;
using Polly;
using Polly.CircuitBreaker;
using System.Text;
using Microsoft.JSInterop;



namespace CircuitBreakerDemo.Web.Pages
{

    public partial class Index : ComponentBase, IDisposable
    {
        [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
        [Inject] private SimulationService SimService { get; set; } = null!;
        [Inject] private IMetricsService Metrics { get; set; } = null!;
        [Inject] private UnstableService PrimaryService { get; set; } = null!;
        [Inject] private StaticCircuitBreakerService StaticCB { get; set; } = null!;
        [Inject] private RLCircuitBreakerService RlCB { get; set; } = null!;

        private CancellationTokenSource? _cts;

        protected bool IsSimulationRunning { get; private set; }
        protected double FailureRate { get; set; } = 0.1;

        protected int StaticSuccess { get; private set; }
        protected int StaticFailure { get; private set; }
        protected SimplifiedCircuitState StaticCircuitState { get; private set; }

        protected int RlSuccess { get; private set; }
        protected int RlFailure { get; private set; }
        protected SimplifiedCircuitState RlCircuitState { get; private set; }
        protected ServicePath RlActivePath { get; private set; }
        protected int TotalRequests { get; private set; }

        protected double[,]? RlQTable { get; private set; }
        protected int CurrentStateIndex { get; private set; }

        protected bool IsScenarioRunning { get; private set; }
        protected string ScenarioNarrative { get; private set; } = "";

        public int LastUsedStateIndex { get; private set; }

        protected bool IsInPresentationMode { get; set; }
        protected override void OnInitialized()
        {
            RlQTable = RlCB.QTable;
            PrimaryService.FailureRate = FailureRate;
        }

        private void ToggleSimulation()
        {
            if (IsScenarioRunning && IsSimulationRunning) return;

            IsSimulationRunning = !IsSimulationRunning;
            if (IsSimulationRunning)
            {
                _cts = new CancellationTokenSource();
                _ = Task.Run(() => RunSimulationLoop(_cts.Token));
            }
            else
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;
            }
        }

        private async Task RunSimulationLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await SimService.RunRequestAsync();

                await InvokeAsync(() =>
                {
                    StaticSuccess = Metrics.Events.Count(e => e.Source == "StaticCB" && e.Message == "Request Succeeded");
                    StaticFailure = Metrics.Events.Count(e => e.Source == "StaticCB" && e.Message != "Request Succeeded");
                    StaticCircuitState = ConvertToSimplifiedState(StaticCB.State);

                    RlSuccess = Metrics.Events.Count(e => e.Source == "RL_CB" && e.Message == "Request Succeeded");
                    RlFailure = Metrics.Events.Count(e => e.Source == "RL_CB" && e.Message != "Request Succeeded");
                    RlCircuitState = RlCB.State;
                    RlActivePath = RlCB.ActivePath;
                    TotalRequests = StaticSuccess + StaticFailure;

                    var rlState = new RLState();
                    rlState.CircuitState = RlCircuitState;
                    CurrentStateIndex = RlCB.LastUsedStateIndex;

                    StateHasChanged(); // Tells the UI that the state has changed
                });
                try
                {
                    await Task.Delay(250, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private SimplifiedCircuitState ConvertToSimplifiedState(CircuitState pollyState)
        {
            return pollyState switch
            {
                CircuitState.Closed => SimplifiedCircuitState.Closed,
                CircuitState.Open => SimplifiedCircuitState.Open,
                CircuitState.HalfOpen => SimplifiedCircuitState.HalfOpen,
                CircuitState.Isolated => SimplifiedCircuitState.Open,
                _ => SimplifiedCircuitState.Closed
            };
        }

        private void ResetSimulation()
        {
            Metrics.Reset();
            TotalRequests = 0;
            StaticSuccess = 0;
            StaticFailure = 0;
            RlSuccess = 0;
            RlFailure = 0;
            ScenarioNarrative = "";
            StateHasChanged();
        }

        private void OnFailureRateChanged(double value)
        {
            FailureRate = value;
            PrimaryService.FailureRate = value;
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        private async Task RunScenario(int scenarioId)
        {
            if (IsSimulationRunning) return;

            ResetSimulation();
            IsScenarioRunning = true;
            ToggleSimulation();

            switch (scenarioId)
            {
                case 1: await RunScenario_GradualDegradation(); break;
                case 2: await RunScenario_SuddenOutage(); break;
                case 3: await RunScenario_IntermittentFailure(); break;
            }

            ToggleSimulation();
            IsScenarioRunning = false;
            ScenarioNarrative = "Scenario finished.";
            StateHasChanged();
        }

        private async Task UpdateNarrative(string message, int delayMs)
        {
            ScenarioNarrative = message;
            StateHasChanged();
            await Task.Delay(delayMs);
        }

        private async Task SetFailureRate(double rate, int delayMs)
        {
            OnFailureRateChanged(rate);
            StateHasChanged();
            await Task.Delay(delayMs);
        }

        private async Task RunScenario_GradualDegradation()
        {
            await UpdateNarrative("Scenario 1: Starting healthy (10% failure).", 3000);
            await SetFailureRate(0.1, 8000);
            await UpdateNarrative("Service degrading... Failure rate increasing to 40%.", 3000);
            await SetFailureRate(0.4, 8000);
            await UpdateNarrative("Severe degradation... Failure rate at 70%.", 3000);
            await SetFailureRate(0.7, 12000);
            await UpdateNarrative("Critical failure (90%). The RL agent should learn to use the backup path.", 3000);
            await SetFailureRate(0.9, 15000);
        }

        private async Task RunScenario_SuddenOutage()
        {
            await UpdateNarrative("Scenario 2: Starting healthy (10% failure).", 3000);
            await SetFailureRate(0.1, 8000);
            await UpdateNarrative("Sudden outage! Primary service at 95% failure.", 3000);
            await SetFailureRate(0.95, 15000);
            await UpdateNarrative("Service has recovered! Failure rate back to 10%.", 3000);
            await SetFailureRate(0.1, 15000);
        }

        private async Task RunScenario_IntermittentFailure()
        {
            await UpdateNarrative("Scenario 3: Simulating a 'flapping' service.", 3000);
            await SetFailureRate(0.1, 8000);
            await UpdateNarrative("Brief failure spike (70%).", 3000);
            await SetFailureRate(0.7, 8000);
            await UpdateNarrative("Service recovers quickly (10%).", 3000);
            await SetFailureRate(0.1, 8000);
            await UpdateNarrative("Another, longer failure spike (80%).", 3000);
            await SetFailureRate(0.8, 12000);
            await UpdateNarrative("Service stabilizes again (10%).", 3000);
            await SetFailureRate(0.1, 10000);
        }

        private async Task ExportMetrics()
        {
            var sb = new StringBuilder();
            // CSV Header
            sb.AppendLine("Timestamp,Source,Message,DurationMs");

            // CSV Rows
            foreach (var ev in Metrics.Events)
            {
                sb.AppendLine($"{ev.Timestamp:o},{ev.Source},{ev.Message.Replace(",", ";")},{ev.Duration?.TotalMilliseconds}");
            }

            var fileName = $"metrics_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            var fileContent = sb.ToString();

            // Use JS Interop to trigger the download in the browser
            var bytes = Encoding.UTF8.GetBytes(fileContent);
            var base64 = Convert.ToBase64String(bytes);
            await JSRuntime.InvokeVoidAsync("downloadFileFromBase64", fileName, base64);
        }
    }
}