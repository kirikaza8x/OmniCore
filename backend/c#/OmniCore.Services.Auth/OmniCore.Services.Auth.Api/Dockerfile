#syntax=docker/dockerfile:1
# 1. Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy ONLY Auth microservice and Shared project files
COPY --parents backend/c#/OmniCore.Services.Auth/**/*.csproj ./
COPY --parents backend/c#/OmniCore.Shared/**/*.csproj ./

# Restore dependencies (Cached unless Auth or Shared .csproj files change)
RUN dotnet restore "backend/c#/OmniCore.Services.Auth/OmniCore.Services.Auth.Api/OmniCore.Services.Auth.Api.csproj"

# Copy the rest of the source code and build
COPY . .
WORKDIR "/src/backend/c#/OmniCore.Services.Auth/OmniCore.Services.Auth.Api"
RUN dotnet build "OmniCore.Services.Auth.Api.csproj" -c Release -o /app/build

# 2. Publish Stage
FROM build AS publish
RUN dotnet publish "OmniCore.Services.Auth.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 3. Final Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# curl needed for the container healthcheck (docker-compose.services.yml)
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OmniCore.Services.Auth.Api.dll"]