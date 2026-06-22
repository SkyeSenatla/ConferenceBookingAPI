using Scalar.AspNetCore;
using Serilog;
using API.Middleware;
using API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using API.Data;
using API.Infrastructure;
using API.Infrastructure.OpenApi;
using Microsoft.EntityFrameworkCore;
using Asp.Versioning;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

//════════════════════════════════════════════════════
// Bootstrap Serilog before the host is built.
// This ensures even startup exceptions are logged.
//════════════════════════════════════════════════════
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting up the Conference Booking API...");
    var builder = WebApplication.CreateBuilder(args);

    // Replace the default .NET logger with Serilog
    builder.Host.UseSerilog();

    //════════════════════════════════════════════════════
    // BUILDER — Register services
    //════════════════════════════════════════════════════

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter()));
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion                = new ApiVersion(1);
        options.AssumeDefaultVersionWhenUnspecified = true; // /api/bookings defaults to v1 — nothing breaks
        options.ReportApiVersions                = true;   // adds api-supported-versions header to every response
    }).AddMvc(); // wires versioning into the MVC pipeline so headers and endpoint metadata are applied
    builder.Services.AddScoped<ConferenceBookingDocumentTransformer>();
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer<ConferenceBookingDocumentTransformer>();
    });
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // Day 4 — CORS: tells the browser that requests from the Next.js dev server are permitted.
    // CHANGED: Fixed typo in origin (was "localhost:300") and renamed policy to "NextJsPolicy"
    // to match the frontend it is actually serving.
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("NextJsPolicy", policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:3000",           // Next.js dev server
                    "https://conference.example.com")  // production — replace with real domain
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()          // required — JWT is sent as a credential
                .WithExposedHeaders("X-Total-Count"); // pagination header must be explicitly exposed    
        });
    });

    // Day 4 — CHANGED: Secret is now read from appsettings.Development.json instead of being
    // hardcoded here. In production this would come from Azure Key Vault or AWS Secrets Manager.
    var jwtSecretKey = builder.Configuration["Jwt:SecretKey"]!;

    // Day 4 — Registers the JWT Bearer scheme.
    // Teaches the pipeline to read "Authorization: Bearer <token>" on every request
    // and validate it before any controller action runs.
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,           // Not required — we are the only issuer
                ValidateAudience = false,          // Not required — single API consumer
                ValidateLifetime = true,           // Reject tokens past their expiry
                ValidateIssuerSigningKey = true,   // Verify the signature using our secret
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSecretKey))
            };
        });

    // Day 4 — Required for [Authorize(Roles = "...")] attributes to evaluate correctly.
    builder.Services.AddAuthorization();

    builder.Services.AddRateLimiter(options =>
    {
        // Global limiter applied to every request — 100 per minute across all endpoints.
        // Using GlobalLimiter (rather than MapControllers().RequireRateLimiting()) ensures
        // that endpoint-level [EnableRateLimiting] attributes override correctly: they are
        // the LAST metadata added, so GetMetadata<T> picks them up over the group policy.
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
            RateLimitPartition.GetFixedWindowLimiter("global", _ =>
                new FixedWindowRateLimiterOptions
                {
                    PermitLimit          = 100,
                    Window               = TimeSpan.FromMinutes(1),
                    QueueLimit           = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }));

        // Tighter named policy for search — full-text queries are expensive.
        // Applied via [EnableRateLimiting("search")] on the search action.
        options.AddSlidingWindowLimiter("search", limiter =>
        {
            limiter.Window             = TimeSpan.FromMinutes(1);
            limiter.SegmentsPerWindow  = 6; // checked every 10 seconds
            limiter.PermitLimit        = 20;
            limiter.QueueLimit         = 0;
        });

        options.RejectionStatusCode = 429;
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = 429;
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                context.HttpContext.Response.Headers.RetryAfter =
                    ((int)retryAfter.TotalSeconds).ToString();
            }
            await context.HttpContext.Response.WriteAsync(
                "Too many requests. Please try again later.", cancellationToken);
        };
    });

    // Day 4 — CHANGED: Register AuthService so AuthController can receive it via DI.
    // Previously AuthController built tokens itself with a hardcoded key.
    // Now the controller delegates to this service; token logic lives in one place.
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddDbContext<BookingDbContext>((serviceProvider, options) =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
               .AddInterceptors(serviceProvider.GetRequiredService<SlowQueryInterceptor>()));

    builder.Services
        .AddBookingFeature()
        .AddRoomFeature();

    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
        options.MimeTypes = ResponseCompressionDefaults.MimeTypes
            .Append("application/json");
    });

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<BookingDbContext>(
            name: "database",
            failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
            tags: ["ready"]);

    builder.Services.AddHostedService<BookingArchiveService>();

    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes  = true;
        options.ValidateOnBuild = true;
    });

    //════════════════════════════════════════════════════
    // TRANSITION — Build() seals the DI container.
    // Nothing can be registered after this line.
    //════════════════════════════════════════════════════
    var app = builder.Build();

    // Week 2 — Apply pending migrations and seed the database.
    //
    // WHY create a scope here?
    //   DbContext is registered as Scoped — one instance per HTTP request.
    //   Outside of a request (like here at startup) there is no active scope,
    //   so we create one manually to resolve the DbContext from the DI container.
    //   The 'using' ensures the scope (and the DbContext inside it) is disposed
    //   cleanly when this block exits.
    //
    // MigrateAsync — equivalent to running 'dotnet ef database update' by hand.
    //   It applies any migration files that have not yet been recorded in
    //   __EFMigrationsHistory. Safe to call on every startup: if the schema
    //   is already up-to-date, it is a no-op.
    //   NOTE: This is a development convenience. In production CI/CD pipelines,
    //   migrations are typically applied as a dedicated deployment step before
    //   the new application version is started.
    //
    // SeedAsync — inserts the demo dataset only if the bookings table is empty.
    //   Safe to call on every startup: it returns immediately if data exists.
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        await db.Database.MigrateAsync();
        await SeedData.SeedAsync(db);
    }

    //════════════════════════════════════════════════════
    // PIPELINE — Configure the middleware chain.
    // Order matters. Top to bottom.
    //════════════════════════════════════════════════════

    app.UseSerilogRequestLogging();
    app.UseResponseCompression();

    // Day 4 — CORS must sit before auth so that browser preflight OPTIONS requests are
    // handled before the pipeline attempts to validate a Bearer token. Preflight requests
    // carry no token and would otherwise be rejected before CORS headers are written.
    app.UseCors("NextJsPolicy");
    app.UseRateLimiter();

    // CHANGED: Moved UseExceptionHandler above UseAuthentication so that any exception
    // thrown during authentication or further down the pipeline is caught and formatted
    // as Problem Details. Previously it sat after auth, leaving auth errors unhandled.
    app.UseExceptionHandler();
    app.UseStatusCodePages();

    // Day 4 — UseAuthentication must come before UseAuthorization.
    // "Who are you?" must be answered before "Are you allowed in?"
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapControllers();

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });

    app.Run();


}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start correctly.");
}
finally
{
    Log.CloseAndFlush();
}
public  partial class Program
{
    
}
