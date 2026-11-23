#syntax=docker/dockerfile:1
# 1. Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy ONLY the Notification microservice, the Contracts of services it consumes (--consumes), and Shared
# (csproj first for restore caching)
COPY --parents backend/c#/OmniCore.Services.Notification/**/*.csproj ./
COPY --parents backend/c#/OmniCore.Services.Auth/OmniCore.Services.Auth.Contracts/**/*.csproj ./
COPY --parents backend/c#/OmniCore.Shared/**/*.csproj ./

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "backend/c#/OmniCore.Services.Notification/OmniCore.Services.Notification.Api/OmniCore.Services.Notification.Api.csproj"

COPY backend/c#/OmniCore.Services.Notification/ backend/c#/OmniCore.Services.Notification/
COPY backend/c#/OmniCore.Services.Auth/OmniCore.Services.Auth.Contracts/ backend/c#/OmniCore.Services.Auth/OmniCore.Services.Auth.Contracts/
COPY backend/c#/OmniCore.Shared/ backend/c#/OmniCore.Shared/

WORKDIR "/src/backend/c#/OmniCore.Services.Notification/OmniCore.Services.Notification.Api"
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "OmniCore.Services.Notification.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# 2. Final Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# curl needed for container healthchecks
RUN --mount=type=cache,target=/var/cache/apt,sharing=locked \
    --mount=type=cache,target=/var/lib/apt,sharing=locked \
    apt-get update && apt-get install -y --no-install-recommends curl

RUN adduser --disabled-password --gecos "" appuser
USER appuser

EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OmniCore.Services.Notification.Api.dll"]
