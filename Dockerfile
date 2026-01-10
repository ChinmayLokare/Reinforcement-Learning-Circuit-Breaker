FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["CircuitBreakerDemo.Web/CircuitBreakerDemo.Web.csproj", "CircuitBreakerDemo.Web/"]
COPY ["CircuitBreakerDemo.Core/CircuitBreakerDemo.Core.csproj", "CircuitBreakerDemo.Core/"]
RUN dotnet restore "CircuitBreakerDemo.Web/CircuitBreakerDemo.Web.csproj"
COPY . .
WORKDIR "/src/CircuitBreakerDemo.Web"
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CircuitBreakerDemo.Web.dll"]