# Week 3 Days 3 & 4 — Demo Guide Summary
## OpenAPI, Documentation, TypeScript Clients & Docker

---

## What We Built — The Big Picture

This session completed the "make it shippable" phase. Before today, the API worked but was invisible — no contract, no documentation, no portable runtime, no scheduled work. After today, one command (`docker compose up`) gives any developer a fully working stack with data, and one URL (`/openapi/v1.json`) gives any tool a machine-readable description of every endpoint.

---

## Phase 1 — OpenAPI & Document Transformers

**What it is:** ASP.NET Core's built-in spec generator. `builder.Services.AddOpenApi()` registers a service that inspects your routes, `ActionResult<T>` return types, and attributes at startup and produces a JSON document at `/openapi/v1.json`. It reads your existing code — you don't annotate every method.

**The transformer pattern:**

```csharp
public class ConferenceBookingDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, ...)
    {
        document.Info.Title = "Conference Booking API";
        // ...
    }
}
```

`IOpenApiDocumentTransformer` is a pipeline hook — it runs once on the whole document before it is served. The important lesson here is **DI ordering**: you register the transformer as a scoped service *before* calling `AddOpenApi(options => ...)`. If you flip that order, `AddOpenApi` configures itself before it knows the transformer type exists, and the DI container throws at startup.

**Scalar UI** is a rendering layer over that JSON document. It consumes `/openapi/v1.json` and displays an interactive UI at `/scalar/v1`. It replaced Swagger UI in ASP.NET Core 10 as the default interactive documentation tool.

**Implementation gotcha:** `Microsoft.OpenApi` version 2 removed the `.Models` sub-namespace. `OpenApiDocument` and `OpenApiContact` are now directly in `Microsoft.OpenApi`, not `Microsoft.OpenApi.Models`. This only surfaces when you compile — not when you read documentation written for v1.

---

## Phase 2 — Endpoint Documentation

Two attributes add human-readable text to the generated spec:

- `[EndpointSummary("...")]` — the one-line label in the endpoint list
- `[EndpointDescription("...")]` — the expanded description shown when you open an endpoint

These feed directly into `/openapi/v1.json` under `summary` and `description` fields. Any tool consuming the spec — Postman, generated SDK clients, API gateway documentation — picks these up automatically.

**Why it matters:** Without descriptions, a frontend developer looking at your spec sees `POST /api/v1/bookings` and nothing else. With descriptions, they see what it validates, what auth it requires, and what errors it can return — without asking anyone.

---

## Phase 3 — Response Compression

A middleware that compresses response bodies before sending them. Two configuration concerns:

**1. Where in the pipeline:** `UseResponseCompression()` must be placed *before* any middleware that writes response bodies. In our pipeline, that means before `UseCors`. If you put it after, responses are already written and there is nothing left to compress.

**2. Algorithm priority:** We register Brotli first, then Gzip. The middleware reads the browser's `Accept-Encoding: br, gzip` header and picks the first algorithm both sides support. Brotli typically achieves 15–25% better compression than Gzip on JSON payloads.

```csharp
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
```

`EnableForHttps = true` is safe here because we own both ends of the connection. In a scenario with a shared proxy you do not control, CRIME/BREACH attacks can theoretically extract session tokens by correlating compressed response sizes — but that is not a concern for an API you are hosting directly.

---

## Phase 4 — Health Checks

**The liveness vs readiness distinction** is a Kubernetes concept that maps neatly to two endpoints:

| Endpoint | What it answers | Kubernetes action if it fails |
|---|---|---|
| `/health/live` | Is the process running? | Restart the pod |
| `/health/ready` | Can the process serve traffic? | Remove from load balancer rotation |

These are different questions. A pod can be alive (process is running) but not ready (cannot reach the database). You want Kubernetes to stop routing new requests to it without killing and restarting it — that is what the readiness check enables.

```csharp
// Liveness — exclude ALL named checks
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// Readiness — only checks tagged "ready"
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

The `Predicate = _ => false` on liveness is the key insight: we include *no* named checks. If the process can respond to HTTP at all, it is alive. If we included the database check on liveness, a temporary database outage would cause Kubernetes to restart the pod — even though the pod is perfectly healthy and just waiting for the database to recover.

**The package confusion:** `Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore` is for the developer exception page (showing EF migration errors in the browser). The correct package for `AddDbContextCheck<T>()` is `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`. Same namespace prefix, completely different purpose. This only fails at compile time when you call the method and get "method not found".

---

## Phase 5 — Configuration Hierarchy

ASP.NET Core builds a layered configuration object at startup. Each source overrides the previous:

```
1. appsettings.json          ← safe defaults, committed to git
2. appsettings.{env}.json    ← environment overrides, gitignored in dev
3. Environment variables     ← CI/CD, containers, cloud
4. dotnet user-secrets       ← developer-local secrets, OS user profile
5. Command-line arguments    ← one-off overrides
```

**The double-underscore rule:** Environment variables cannot contain colons, so ASP.NET Core maps `__` to `:`. `Jwt__SecretKey` in an environment variable becomes `Jwt:SecretKey` in the configuration object. This is how the Docker Compose `environment:` block reaches the same config key that `appsettings.json` uses.

**User-secrets** store values in `%APPDATA%\Microsoft\UserSecrets\<project-id>\secrets.json` on Windows. They are never in the project directory, never touched by git. Each developer has their own values. The application reads them transparently through the same `builder.Configuration["Jwt:SecretKey"]` call — it does not know or care where the value came from.

---

## Phase 6 — Multi-Stage Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# ... compile and publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
COPY --from=build /app/publish .
```

**Why two stages:** The SDK image is ~700 MB. The ASP.NET runtime image is ~200 MB. A single-stage build would ship the SDK, source code, and build tools into production. Multi-stage discards everything from stage one except the compiled output. The final image has no compiler, no source, no build tools. An attacker who compromises the container has nothing to pivot with.

**Layer caching** is the key optimisation:

```dockerfile
COPY ["API/API.csproj", "API/"]
RUN dotnet restore "API/API.csproj"   # cached unless .csproj changes

COPY API/ API/                        # only source files
RUN dotnet publish ...
```

Docker caches each `RUN` instruction as a layer. If only source files change, the `dotnet restore` layer is served from cache — NuGet does not re-download 30 packages on every build. For a cold build the difference is 30–60 seconds.

---

## Phase 7 — Docker Compose

```yaml
depends_on:
  postgres:
    condition: service_healthy
```

This is the most important line in the entire Compose file. Without `condition: service_healthy`, Compose starts both containers simultaneously. The API process starts, tries to connect to PostgreSQL, fails because the database is still initialising its data directory, and crashes. With the health check condition, the API container does not start until `pg_isready -U postgres -d ConferenceBooking` returns success.

**Service discovery by name:** In the connection string, `Host=postgres` works because Compose puts all services on a shared bridge network. Each service name is registered as a DNS entry on that network. No IP addresses, no environment-specific hostnames — the same Compose file works on every developer's machine.

**Named volumes vs bind mounts:**

```yaml
volumes:
  - postgres_data:/var/lib/postgresql/data
```

A named volume persists across `docker compose down`. `docker compose down -v` is the explicit command to also remove volumes — it is intentionally hard to trigger accidentally, because it is equivalent to dropping the database.

---

## Phase 8 — Background Service

**The singleton-scoped conflict** is the most common mistake with `BackgroundService`. The class is registered as a singleton (lives for the app's lifetime). `DbContext` is scoped (lives for one request). Singletons cannot receive scoped services directly — the DI container throws at startup when `ValidateScopes` is enabled.

The fix is `IServiceScopeFactory`:

```csharp
public class BookingArchiveService(IServiceScopeFactory scopeFactory, ...) : BackgroundService
{
    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        // ... use db, then scope is disposed
    }
}
```

`IServiceScopeFactory` itself is a singleton, so it can be injected directly. You ask it to create a scope on-demand, resolve scoped services from that scope, use them, then dispose the scope when you are done. This is exactly what a request lifetime does — it is just manual instead of automatic.

**The cancellation token on `Task.Delay`** is essential:

```csharp
// Wrong — blocks shutdown for up to 24 hours
await Task.Delay(TimeSpan.FromHours(24));

// Correct — exits immediately when the app stops
await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
```

ASP.NET Core's default grace period is 5 seconds. After that the process is killed forcibly. With the token, the delay throws `OperationCanceledException` immediately when shutdown is signalled and the loop exits cleanly.

---

## Codebase Architecture — Full Picture

After all three weeks, the project has a clear layered structure:

```
HTTP Request
    │
    ▼
Middleware Pipeline
  UseSerilogRequestLogging
  UseResponseCompression
  UseCors
  UseRateLimiter
  UseExceptionHandler
  UseAuthentication     ← "Who are you?"
  UseAuthorization      ← "Are you allowed in?"
    │
    ▼
Controllers  (API/Controllers/)
  BookingsController
  RoomsController
  AuthController
    │
    ▼
Services  (API/Services/)
  BookingService        ← business logic, validation
  RoomService
  AuthService           ← JWT minting
  BookingArchiveService ← background work
    │
    ▼
Repositories  (API/Repositories/)
  BookingRepository     ← data access, pagination, search
  RoomRepository
    │
    ▼
EF Core  (API/Data/)
  BookingDbContext
  SeedData
    │
    ▼
PostgreSQL
```

The dependency direction is always downward. Controllers do not know repositories exist. Repositories do not know controllers exist. Each layer only knows the interface of the layer directly below it — which is what makes each layer independently testable.

---

## Testing Architecture

The test suite has three distinct categories that mirror the testing pyramid:

**Unit tests** (`API.Tests/Unit/`) — NSubstitute substitutes every dependency. Tests run in milliseconds, no infrastructure required. They verify business logic in isolation: does `BookingService.CreateAsync` reject a booking where `EndTime < StartTime`? Does `PatchAsync` skip the conflict check when time fields are not changed?

**Repository tests** (`API.Tests/Repository/`) — A real PostgreSQL container (via TestContainers) with real migrations. No application code involved — just `BookingRepository` against an actual database. They verify database-specific behaviour: check constraints, ordering guarantees, pagination arithmetic. These would be impossible to test correctly with a mock database.

**Integration tests** (`API.Tests/Intergration/`) — The full `WebApplicationFactory<Program>` starts the complete application stack (including middleware, auth, rate limiting) against a real database. They verify that the pieces connect correctly: does the JWT auth pipeline actually reject unauthenticated POST requests? Does the versioning header appear on every response?

**Test isolation lesson:** Tests that share infrastructure (the same PostgreSQL container) must be written to be order-independent. The `CheckConstraint` test created a room, which caused `SeedData.SeedAsync` to return early when its guard checked `Rooms.AnyAsync()`. Changing the guard to `Bookings.AnyAsync()` was the right fix because it reflects what "seeded" actually means — not "there are rooms" but "there are bookings, which can only exist if the full seed ran".

---

## Concepts Reference

| Concept | Where | Why It Matters |
|---|---|---|
| OpenAPI spec | `AddOpenApi()` | Machine-readable contract; one source of truth |
| Document transformer | `IOpenApiDocumentTransformer` | Enrich the spec without touching every endpoint |
| Scalar UI | `MapScalarApiReference()` | Interactive docs without external tooling |
| Endpoint attributes | `[EndpointSummary]`, `[EndpointDescription]` | Per-endpoint documentation from the source |
| Response compression | `UseResponseCompression()` | Smaller payloads; middleware order is critical |
| Liveness check | `/health/live` | Proves the process is running |
| Readiness check | `/health/ready` | Proves the process can serve traffic |
| Configuration hierarchy | appsettings → env vars → user-secrets | One binary, every environment, no hardcoded values |
| Double-underscore mapping | `Jwt__SecretKey` → `Jwt:SecretKey` | How containers inject nested config keys |
| Multi-stage Dockerfile | `sdk:10.0 AS build` → `aspnet:10.0` | Production image has no compiler or source code |
| Docker layer caching | Copy `.csproj` before source | NuGet restore only re-runs when dependencies change |
| Docker Compose service discovery | `Host=postgres` | Services reach each other by name on the bridge network |
| Health check dependency | `condition: service_healthy` | API waits for PostgreSQL to be ready before starting |
| Named volumes | `postgres_data:` | Data survives `docker compose down` |
| Background service | `BackgroundService` | Scheduled work hosted alongside the HTTP pipeline |
| Singleton-scoped conflict | `IServiceScopeFactory` pattern | Singletons cannot hold scoped services; create a scope manually |
| Graceful shutdown | `Task.Delay(..., stoppingToken)` | Service exits cleanly when the application stops |
| TypeScript client generation | `openapi-typescript` | Frontend build breaks when backend contract changes |
| Test ordering independence | Guard on `Bookings.AnyAsync()` | Shared infrastructure tests must not depend on execution order |
