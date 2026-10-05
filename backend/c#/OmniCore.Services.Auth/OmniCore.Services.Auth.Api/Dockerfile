#syntax=docker/dockerfile:1
# 1. Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy ONLY Auth microservice and Shared project files (csproj first for restore caching)
COPY --parents backend/c#/OmniCore.Services.Auth/**/*.csproj ./
COPY --parents backend/c#/OmniCore.Shared/**/*.csproj ./

# Restore dependencies (cached unless Auth or Shared .csproj files change)
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "backend/c#/OmniCore.Services.Auth/OmniCore.Services.Auth.Api/OmniCore.Services.Auth.Api.csproj"

# Copy only the Auth service + Shared source (NOT the whole monorepo)
COPY backend/c#/OmniCore.Services.Auth/ backend/c#/OmniCore.Services.Auth/
COPY backend/c#/OmniCore.Shared/ backend/c#/OmniCore.Shared/

# Publish directly (skips the redundant intermediate `dotnet build` step)
WORKDIR "/src/backend/c#/OmniCore.Services.Auth/OmniCore.Services.Auth.Api"
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "OmniCore.Services.Auth.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# 2. Final Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# curl needed for the container healthcheck (docker-compose.services.yml)
RUN --mount=type=cache,target=/var/cache/apt,sharing=locked \
    --mount=type=cache,target=/var/lib/apt,sharing=locked \
    apt-get update && apt-get install -y --no-install-recommends curl

# Run as non-root
RUN adduser --disabled-password --gecos "" appuser
USER appuser

EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OmniCore.Services.Auth.Api.dll"]