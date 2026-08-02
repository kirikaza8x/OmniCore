# OmniCore.Shared

This document summarizes the reusable shared foundation for the OmniCore backend solution. It is designed to provide common domain, application, infrastructure, and API building blocks that can be reused across service modules.

## Overview

The shared library is split into four main layers:

- [OmniCore.Shared.Domain](OmniCore.Shared.Domain/OmniCore.Shared.Domain.md): core domain modeling primitives and business-rule abstractions
- [OmniCore.Shared.Application](OmniCore.Shared.Application/OmniCore.Shared.Application.md): application use cases, command/query orchestration, validation, and cross-cutting behaviors
- [OmniCore.Shared.Infrastructure](OmniCore.Shared.Infrastructure/OmniCore.Shared.Infrastructure.md): persistence, caching, messaging, jobs, storage, and platform integrations
- [OmniCore.Shared.Api](OmniCore.Shared.Api/OmniCore.Shared.Api.md): API conventions, endpoint registration, result formatting, rate limiting, and web-layer utilities

## Detailed layer guides

Each layer has its own focused documentation page with deeper guidance:

- [Domain layer guide](OmniCore.Shared.Domain/OmniCore.Shared.Domain.md)
- [Application layer guide](OmniCore.Shared.Application/OmniCore.Shared.Application.md)
- [Infrastructure layer guide](OmniCore.Shared.Infrastructure/OmniCore.Shared.Infrastructure.md)
- [API layer guide](OmniCore.Shared.Api/OmniCore.Shared.Api.md)

## Structure

```text
OmniCore.Shared/
├── OmniCore.Shared.Domain/
│   ├── Abstractions/
│   ├── Contracts/
│   ├── Data/
│   ├── DDD/
│   ├── Pagination/
│   ├── Queries/
│   └── ValueObject/
├── OmniCore.Shared.Application/
│   ├── Abstractions/
│   ├── Behaviors/
│   ├── DTOs/
│   ├── Extensions/
│   └── Helpers/
├── OmniCore.Shared.Infrastructure/
│   ├── Configs/
│   ├── Data/
│   ├── EventBus/
│   ├── Extensions/
│   ├── Inbox/
│   ├── Middleware/
│   ├── Outbox/
│   ├── Quartz/
│   ├── Service/
│   └── Tracing/
└── OmniCore.Shared.Api/
    ├── Endpoints/
    ├── Exceptions/
    ├── Extensions/
    ├── File/
    ├── RateLimiting/
    └── Results/
```

## What OmniCore.Shared can offer

### 1. Domain layer capabilities

The domain layer provides the core building blocks for modeling business domains consistently:

- Domain errors, validation results, and result wrappers
- Common abstractions such as guards and application exceptions
- DDD-style base classes for entities, aggregate roots, auditable entities, and domain events
- Reusable value objects such as address, email, phone number, money, and date ranges
- Shared contracts, pagination, and query abstractions

### 2. Application layer capabilities

The application layer focuses on orchestration and reusable app behavior:

- MediatR-based command/query processing
- Cross-cutting pipeline behaviors for logging, validation, performance, and unit-of-work execution
- DTOs for common application concerns such as current user details, paging, and logging notifications
- Abstractions for authentication, caching, event bus, messaging, notifications, storage, time, tracing, and SignalR
- Extension methods and helper utilities for common app tasks

### 3. Infrastructure layer capabilities

The infrastructure layer connects the domain and application layers to real platform services:

- EF Core and PostgreSQL integration support
- Redis distributed caching and in-memory fallback
- Event bus support through MassTransit and integration event handling
- Inbox/outbox patterns for reliable messaging
- Quartz-based background jobs
- Storage abstractions for S3-compatible services
- SignalR and tracing support for runtime observability
- Shared configuration and service registration helpers

### 4. API layer capabilities

The API layer provides web-facing features for building service endpoints:

- Carter-based endpoint registration
- Common API result types and response wrappers
- Rate limiting policies and configuration
- CORS and authentication/authorization integration helpers
- Shared API extensions and logging hub support

## Typical usage

OmniCore.Shared is intended to be used as the reusable foundation for service-specific modules. In practice, it helps a module:

- define domain entities and value objects
- implement commands and queries using a consistent application layer
- plug into infrastructure concerns such as persistence, messaging, and caching
- expose API endpoints with consistent response and rate-limiting behavior

## Dependency flow

A typical dependency direction is:

```text
Domain -> Application -> Infrastructure -> API
```

This keeps service modules consistent while still allowing them to extend the shared foundation as needed.
