RL-Enhanced Circuit Breaker in .NET

This project is a demonstration of a machine learning-enhanced resilience pattern, specifically a dynamic circuit breaker that uses Reinforcement Learning (Q-Learning) to make intelligent decisions. The system is built entirely in .NET 8 and features a Blazor WebAssembly frontend for real-time visualization and control.

The core innovation is a unified learning framework where a single Q-learning agent learns to control both circuit breaker parameters (e.g., timeout durations) and service path selection (e.g., switching between a primary and a backup service).

Project Goal

The goal of this project is to demonstrate that an adaptive, learning-based approach to resilience can significantly outperform traditional, static circuit breakers in dynamic and unpredictable failure scenarios. This is achieved by building a system that requires zero manual configuration and learns optimal strategies through experience.

System Architecture

The application is designed using Clean Architecture principles with a clear separation of concerns across four main layers:

UI Layer (Blazor WASM): An interactive dashboard for real-time visualization of metrics, the Q-Table heatmap, and for controlling the simulation via manual controls or automated scenarios.

Service Layer (.NET): Orchestrates the simulation, contains the implementations for both the static (baseline) and RL (adaptive) circuit breakers, and includes a central MetricsService. It uses the Polly library for the underlying circuit breaker mechanics.

Reinforcement Learning Layer (.NET): The "brain" of the application. It contains a from-scratch implementation of a Q-learning agent, including the Q-Table, state discretization logic, and the learning algorithm (Bellman equation).

Downstream Services Layer (.NET): Simulates the environment with two mock services: a fast but unreliable PrimaryService and a slower but highly stable BackupService.

Technology Stack

Platform: .NET 8

Frontend: Blazor WebAssembly

Resilience Library: Polly

UI Components: Radzen.Blazor

Logging: Serilog

Testing: xUnit, FluentAssertions, Moq

Features

Side-by-Side Comparison: Real-time dashboard comparing the performance of a traditional static circuit breaker against the RL-enhanced version.

Live Q-Table Visualization: A heatmap that visualizes the agent's learned knowledge, showing preferred (green) and avoided (red) actions for every state.

Interactive Control Panel:

Start/Stop the simulation.

Manually adjust the failure rate of the primary service.

Reset the agent's learning and metrics.

Automated Scenarios: One-click buttons to run pre-programmed failure scenarios:

Gradual Degradation: Simulates a service slowly failing over time.

Sudden Outage: Simulates a catastrophic, instantaneous failure and recovery.

Intermittent Failure: Simulates a "flapping" service with unpredictable health.

Metrics Export: Export the raw simulation event log to a CSV file for analysis.

Presentation Mode: Increases font sizes for better visibility during presentations.

How to Run the Application
Prerequisites

.NET 8 SDK

A modern web browser (e.g., Chrome, Firefox, Edge)

Steps

Clone the Repository:

code
Bash
download
content_copy
expand_less
git clone https://github.com/ChinmayLokare/Reinforcement-Learning-Circuit-Breaker.git
cd RLCircuitBreakerDemo

Restore Dependencies:
Open a terminal in the root directory of the solution (/RLCircuitBreakerDemo/) and run:

code
Bash
download
content_copy
expand_less
dotnet restore

Run the Blazor Application:
Navigate to the web project directory and use the dotnet run command.

code
Bash
download
content_copy
expand_less
cd CircuitBreakerDemo.Web
dotnet run

View in Browser:
The terminal will display the URLs where the application is being hosted (e.g., https://localhost:7123). Open this URL in your web browser to see the application.

Project Structure

The solution is organized into three projects:

CircuitBreakerDemo.Core/: A class library containing all the core business logic, services, and the reinforcement learning implementation. It is completely UI-agnostic.

CircuitBreakerDemo.Web/: The Blazor WebAssembly project that contains all UI components, pages, and the main simulation control loop.

CircuitBreakerDemo.Tests/: An xUnit project containing unit tests for the core logic, particularly the Q-learning algorithm and state calculations.
