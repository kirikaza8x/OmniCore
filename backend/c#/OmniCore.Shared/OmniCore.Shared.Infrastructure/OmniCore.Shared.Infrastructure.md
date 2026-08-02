# OmniCore.Shared.Infrastructure

This layer contains the concrete infrastructure implementations used by OmniCore services.

## Purpose

The infrastructure layer bridges the domain and application layers to practical runtime services such as persistence, messaging, caching, jobs, storage, and tracing.

## Main areas

### Configuration and services
- Shared configuration models and registration helpers
- Common services such as current-user resolution, time providers, and caching

### Data and persistence
- EF Core integration support
- PostgreSQL-oriented configuration and data abstractions

### Messaging and reliability
- EventBus implementations
- Inbox and outbox patterns for reliable messaging
- MassTransit-based integration support

### Background jobs
- Quartz job setup and processing support

### Platform integrations
- Redis distributed caching
- Storage abstractions for cloud object storage
- SignalR and tracing support

## What this layer offers

- Concrete implementations of shared abstractions
- Support for distributed systems concerns such as caching, jobs, and messaging
- A common infrastructure baseline for all OmniCore services

## Typical usage

Use this layer when you need to connect a service to databases, caching, messaging, background workers, or cloud storage.
