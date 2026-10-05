#syntax=docker/dockerfile:1
# 1. Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy ONLY User microservice, Auth Contracts, and Shared project files (csproj first for restore caching)
COPY --parents backend/c#/OmniCore.Services.User/**/*.csproj ./
COPY --parents backend/c#/OmniCore.Services.Auth/OmniCore.Services.Auth.Contracts/**/*.csproj ./
COPY --parents backend/c#/OmniCore.Shared/**/*.csproj ./

# Restore dependencies (cached unless User, Auth.Contracts, or Shared .csproj files change)
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "backend/c#/OmniCore.Services.User/OmniCore.Services.User.Api/OmniCore.Services.User.Api.csproj"

# Copy source code for User service, Auth.Contracts, and Shared kernel
COPY backend/c#/OmniCore.Services.User/ backend/c#/OmniCore.Services.User/
#COPY backend/c#/OmniCore.Services.Auth/OmniCore.Services.Auth.Contracts/ backend/c#/OmniCore.Services.Auth/OmniCore.Services.Auth.Contracts/
COPY backend/c#/OmniCore.Shared/ backend/c#/OmniCore.Shared/

# Publish directly (skips redundant intermediate `dotnet build` step)
WORKDIR "/src/backend/c#/OmniCore.Services.User/OmniCore.Services.User.Api"
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "OmniCore.Services.User.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# 2. Final Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# curl needed for container healthchecks
RUN --mount=type=cache,target=/var/cache/apt,sharing=locked \
    --mount=type=cache,target=/var/lib/apt,sharing=locked \
    apt-get update && apt-get install -y --no-install-recommends curl

# Run as non-root user
RUN adduser --disabled-password --gecos "" appuser
USER appuser

EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OmniCore.Services.User.Api.dll"]