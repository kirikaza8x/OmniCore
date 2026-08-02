# OmniCore.Shared.Domain

This layer contains the reusable domain foundation used by OmniCore services.

## Purpose

The domain layer defines the business vocabulary, core abstractions, and reusable value objects that keep service models consistent across the platform.

## Main areas

### Abstractions
- Result, Error, ValidationResult, and related types for consistent domain outcomes
- Guard helpers and application/domain exception types
- Base abstractions for validation and error handling

### DDD primitives
- Entity and AggregateRoot for encapsulation and identity
- AuditableEntity for tracking creation and modification metadata
- DomainEvent for domain-driven event modeling
- Tenant for multi-tenant awareness

### Value objects
- Address
- DateRange
- EmailAddress
- Money
- PhoneNumber
- ValueObject base type

### Supporting domain concepts
- Contracts for shared contracts and interfaces
- Data abstractions
- Pagination and query abstractions

## What this layer offers

- A common language for business models
- Consistent handling of validation and failure states
- A solid foundation for implementing domain entities without repeating boilerplate
- Reusable value objects that reduce duplication across services

## Typical usage

Use this layer when you need to define domain entities, value objects, business rules, or shared domain contracts for a new service module.
