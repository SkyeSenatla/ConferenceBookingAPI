using Scalar.AspNetCore;

// ════════════════════════════════════════════════════
// PHASE 1 — BUILDER: Register services into the
//           Dependency Injection container
// ════════════════════════════════════════════════════
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();  // Register controller support
builder.Services.AddOpenApi();      // Register built-in OpenAPI document generation

// ════════════════════════════════════════════════════
// TRANSITION — Build() seals the DI container.
// Nothing can be registered after this line.
// ════════════════════════════════════════════════════
var app = builder.Build();

// ════════════════════════════════════════════════════
// PHASE 2 — PIPELINE: Configure the middleware chain.
// Order matters. Every request passes through these
// in sequence, top to bottom.
// ════════════════════════════════════════════════════
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();             // Serves /openapi/v1.json
    app.MapScalarApiReference();  // Serves the Scalar UI at /scalar/v1
}

app.MapControllers();  // Activates attribute routing for all [ApiController] classes

app.Run();  // Starts the Kestrel web server — this line blocks until the process exits
