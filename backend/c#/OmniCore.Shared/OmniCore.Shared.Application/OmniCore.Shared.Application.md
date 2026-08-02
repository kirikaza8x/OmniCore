# OmniCore.Shared.Application

This layer contains the reusable application orchestration foundation for OmniCore services.

## Purpose

The application layer focuses on use-case execution, request processing, validation, cross-cutting pipeline behavior, and shared application contracts.

## Main areas

### Abstractions
- Authentication abstractions
- Caching abstractions
- Event bus, messaging, and notification abstractions
- Outbox abstractions
- SignalR, storage, time, and tracing abstractions

### Behaviors
- LoggingBehavior
- ValidationBehavior
- UnitOfWorkBehavior
- PerformanceBehavior

### DTOs
- BaseDto
- CurrentUserDto
- DeviceInfoModels
- LogNotificationDto
- PagedRequest

### Helpers and extensions
- Shared helper utilities
- Extension methods for application-level concerns

## What this layer offers

- MediatR-based command and query processing
- Consistent validation and pipeline behavior
- Reusable DTOs for common application data contracts
- A centralized place for application-level abstractions and service registration

## Typical usage

Use this layer when implementing handlers, queries, commands, validators, DTOs, or reusable application services.
