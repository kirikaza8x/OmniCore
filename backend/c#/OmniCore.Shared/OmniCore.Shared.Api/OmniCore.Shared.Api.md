# OmniCore.Shared.Api

This layer contains the reusable API foundation for OmniCore services.

## Purpose

The API layer provides the web-facing building blocks required to expose services through consistent endpoints, response formats, authentication flow, and rate limiting.

## Main areas

### Endpoint registration
- Carter-based endpoint setup
- Shared API endpoint conventions

### Results and responses
- Standard API result wrappers
- Custom result helpers and response extensions

### Rate limiting and web policies
- Shared policy definitions
- Fixed-window rate limiting support

### Web and integration helpers
- CORS and authentication/authorization helpers
- Logging hub integration for runtime diagnostics
- Shared exception and extension utilities

## What this layer offers

- A consistent API surface across services
- Reusable response and error formatting
- Protection and operational controls such as rate limiting
- Helpers for quickly wiring modules into the web stack

## Typical usage

Use this layer when building HTTP endpoints, API conventions, or web-oriented integrations for a service module.
