# OmniCore

OmniCore is a multi-language, modular platform repository built around a shared backend architecture and supporting infrastructure. It includes a C# backend solution, data and messaging services across multiple languages, frontend placeholders, infrastructure definitions, shared contracts and models, and test scaffolding.

## Repository structure

- `backend/`
  - `c#/OmniCore.Shared/`
    - `OmniCore.Shared.Api/` - ASP.NET Core API project using Carter and EF Core
    - `OmniCore.Shared.Application/` - application layer with MediatR, AutoMapper, FluentValidation, and logging
    - `OmniCore.Shared.Domain/` - domain abstractions and core business models
    - `OmniCore.Shared.Infrastructure/` - infrastructure layer with Entity Framework Core, MassTransit, RabbitMQ, Redis, S3, Quartz, SignalR, and other platform services
  - `backend/python/OmniCore.MLService/` - machine learning service placeholder
  - `backend/rust/OmniCore.StreamProcessor/` - stream processing service placeholder
  - `backend/c++/`, `backend/go/`, `backend/java/` - language service directories available for future expansion

- `frontend/`
  - `frontend/web/` - web frontend placeholder
  - `frontend/mobile/` - mobile frontend placeholder

- `infra/`
  - `infra/docker/` - docker assets and container configuration
  - `infra/k8s/` - Kubernetes deployment manifests and cluster definitions
  - `infra/terraform/` - infrastructure-as-code definitions

- `shared/`
  - `shared/config/` - shared configuration files and templates
  - `shared/contracts/` - API contracts and shared interfaces
  - `shared/models/` - shared data models
  - `shared/utils/` - utilities used by multiple services

- `docs/` - documentation folder (currently empty)
- `tests/`
  - `tests/e2e/` - end-to-end tests placeholder
  - `tests/integration/` - integration tests placeholder

## Key technologies

- .NET 9.0
- ASP.NET Core
- Carter
- Entity Framework Core
- MassTransit with RabbitMQ
- PostgreSQL via Npgsql
- Redis caching
- AWS S3 SDK
- Quartz scheduling
- AutoMapper
- MediatR
- FluentValidation
- Serilog
- Microsoft.CodeAnalysis workspace packages (for code analysis scenarios)

## Backend solution

The main C# solution is located at `backend/c#/OmniCore.slnx` and includes the following projects:

- `OmniCore.Shared.Api` - API and web endpoints
- `OmniCore.Shared.Application` - application logic, CQRS behaviors, and validation
- `OmniCore.Shared.Domain` - domain models and abstractions
- `OmniCore.Shared.Infrastructure` - persistence, messaging, integration, and service wiring

## Getting started

1. Open `backend/c#/OmniCore.slnx` in Visual Studio or Rider.
2. Restore NuGet packages.
3. Build the solution.
4. Configure required infrastructure services:
   - database (PostgreSQL)
   - message broker (RabbitMQ)
   - cache (Redis)
   - object storage (S3-compatible service)
5. Run the API project from the solution.

## Notes

- Several folders currently serve as placeholders for future work such as `frontend/web`, `frontend/mobile`, `backend/python/OmniCore.MLService`, and `backend/rust/OmniCore.StreamProcessor`.
- The solution is designed with a layered architecture separating API, application, domain, and infrastructure concerns.
- `infra/` contains tooling and deployment configurations for containerized and cloud-native delivery.

## Recommended next steps

- Add README or documentation files under `docs/` for service-specific setup and architecture diagrams.
- Populate frontend and service directories with code and build scripts.
- Add automated tests to `tests/e2e/` and `tests/integration/`.
- Define environment configuration and deployment guides for `infra/`.

## Contact

This README was generated based on the current workspace structure of the OmniCore repository.
