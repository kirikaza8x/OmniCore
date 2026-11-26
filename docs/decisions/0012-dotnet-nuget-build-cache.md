# ADR 0012: Isolate NuGet caches for parallel service image builds

- Status: accepted
- Scope: Auth, User, and Notification API Dockerfiles
- Related: local Compose build failure during parallel API image builds

## Context

The three .NET API images are built concurrently by Docker Compose. Their Dockerfiles originally mounted the same default BuildKit cache target, `/root/.nuget/packages`, during `dotnet restore` and `dotnet publish`. A parallel Compose build failed inside NuGet with missing temporary package files (for example, under `microsoft.extensions.logging.abstractions/8.0.2` and `microsoft.codeanalysis.csharp/4.11.0`). `docker compose config --quiet` passed, so this was a build-cache race, not a Compose YAML error.

Removing the cache mounts allowed the images to build and the stack to start, but discarded reusable NuGet package downloads. This is retained as a fallback if isolated cache mounts still fail on a particular builder.

## Decision

Use a dedicated BuildKit NuGet cache ID per service:

- Auth: `nuget-auth`
- User: `nuget-user`
- Notification: `nuget-notification`

Use the same service ID and target for that Dockerfile's `dotnet restore` and `dotnet publish --no-restore` steps. Set `sharing=locked` to serialize concurrent writers when two builds of the same service use that cache. Distinct IDs allow different services to build in parallel without mutating one shared NuGet package directory.

Do not use one generic cache ID across these services. If the isolated mounts reproduce the NuGet corruption on a builder, remove NuGet mounts from the affected Dockerfile as the conservative fallback, then rebuild.

## Dockerfile optimization review

The API Dockerfiles already use a multi-stage SDK/runtime layout, copy project files before application sources to retain restore-layer caching, publish directly in Release mode with `--no-restore` and `UseAppHost=false`, copy only published output to the ASP.NET runtime image, run as a non-root user, and keep the build context small via the repository `.dockerignore`. These are retained.

The runtime stages install `curl` for Compose health checks. Remove apt package lists in the same `RUN` instruction to avoid leaving that index data in the final image layer. Do not switch the runtime base image or remove `curl` without validating compatibility and health checks.

Current service images were approximately 553–563 MB before this cleanup, with `/app/publish` reported at about 154 MB for User. The SDK is already excluded from the final stage. These figures are local observations, not size guarantees; rebuild and compare image history after changes.

## Verification

- `docker compose config --quiet` passed before the Dockerfile changes.
- A full `docker compose up` after removing shared NuGet cache mounts built and started all three APIs; all reported healthy in `docker compose ps`.
- Rebuild all three images after applying the isolated cache IDs and apt-list cleanup. Confirm no NuGet missing-file failures, then check API health using `docker compose ps`.
