using AlAmalBusiness.Api.Area.CRM.Hubs;
using AlAmalBusiness.Application.Services.Imp;
using AlAmalBusiness.Application.Services.Imp.CRM;
using AlAmalBusiness.Application.Services.Interface;
using AlAmalBusiness.Application.Services.Interface.CRM;
using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.IRepositories;
using AlAmalBusiness.Domain.IRepositories.CRM;
using AlAmalBusiness.Domain.Models;
using AlAmalBusiness.Infrastructure.Repository.Imp;
using AlAmalBusiness.Infrastructure.Repository.Imp.CRM;
using AlAmalBusiness.Infrastructure.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var jwtKeyValue = jwtSettings["Key"];
if (string.IsNullOrWhiteSpace(jwtKeyValue) || jwtKeyValue.Length < 32)
{
    throw new InvalidOperationException(
        "JwtSettings:Key is missing or too short. Set it via 'dotnet user-secrets' locally " +
        "or an environment-provided config value in production — it must never live in a " +
        "tracked appsettings file.");
}
var key = Encoding.UTF8.GetBytes(jwtKeyValue);

// Pooled: contexts are reset and reused across requests instead of built
// from scratch each time — less allocation churn on the 1 GB shared pool.
builder.Services.AddDbContextPool<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<DbInitializer>();

// Repositories (Infrastructure)
builder.Services.AddScoped<IUserRepo, UserRepo>();
builder.Services.AddScoped<IAuthRepo, AuthRepo>();
builder.Services.AddScoped<IRefreshTokenRepo, RefreshTokenRepo>();
builder.Services.AddScoped<IDepartmentRepo, DepartmentRepo>();
builder.Services.AddScoped<ILeadRepo, LeadRepo>();
builder.Services.AddScoped<ILeadHistoryRepo, LeadHistoryRepo>();
builder.Services.AddScoped<ILeadCallRepo, LeadCallRepo>();
builder.Services.AddScoped<IDoctorRepo, DoctorRepo>();
builder.Services.AddScoped<IProcedureRepo, ProcedureRepo>();
builder.Services.AddScoped<IReferalSourceRepo, ReferalSourceRepo>();
builder.Services.AddScoped<IClosedReasonRepo, ClosedReasonRepo>();
// Services (Application)
builder.Services.AddScoped<IUserServices, UserServices>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<ILeadService, LeadService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IProcedureService, ProcedureService>();
builder.Services.AddScoped<IReferalSourceService, ReferalSourceService>();
builder.Services.AddScoped<IClosedReasonService, ClosedReasonService>();
builder.Services.AddScoped<ILeadExcelReportService, LeadExcelReportService>();
builder.Services.AddScoped<ILeadNotifier, SignalRLeadNotifier>();
builder.Services.AddScoped<IFilterCacheRepo, FilterCacheRepo>();
builder.Services.AddScoped<IFilterCacheService, FilterCacheService>();
builder.Services.AddSignalR();
// In-memory IDistributedCache — no Redis on the target (smartasp.net shared)
// hosting. Swapping to AddStackExchangeRedisCache(...) later needs no other
// change, since everything talks to IDistributedCache only.
builder.Services.AddDistributedMemoryCache();
// Admin-dashboard aggregate cache (LeadService) — in-process for the same
// reason; invalidated on every lead write, 60s TTL as a backstop.
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Registered after AddIdentity so JWT stays the default scheme:
// AddIdentity sets the defaults to the Identity cookie schemes, and the last
// configuration wins.
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };

    // SignalR's browser client can't set an Authorization header on the
    // WebSocket handshake — it sends the token as ?access_token=... instead.
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        },
        // Default behavior on a missing/expired/invalid token is an empty 401
        // body — callers (the Next.js frontend included) can't tell "never
        // logged in" from "session expired" from anything else. HandleResponse()
        // suppresses that default so we can write a real JSON body instead.
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            var message = context.AuthenticateFailure is Microsoft.IdentityModel.Tokens.SecurityTokenExpiredException
                ? "Your session has expired. Please log in again."
                : "You need to log in to do that.";
            await context.Response.WriteAsJsonAsync(new { message });
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { message = "You don't have permission to do that." });
        }
    };
});

var whitelistedIps = builder.Configuration.GetSection("RateLimiterSettings:WhitelistedIps").Get<HashSet<string>>() ?? new HashSet<string>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, token) =>
    {
        // Sliding-window limiters don't always populate RetryAfter metadata —
        // fall back to the 1-minute window every policy above uses.
        var retryAfterSeconds = 60;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            retryAfterSeconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
        }

        context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            message = "Too many requests. Please wait a moment and try again.",
            retryAfterSeconds
        }, token);
    };

    // ==========================================
    // LAYER 1: GLOBAL LIMITER (The Safety Net)
    // ==========================================
    // Every device on the hospital's LAN shares one public IP behind the
    // office NAT, so this bucket is really "all of Al Amal's concurrent
    // staff traffic," not one person — sized well above LAYER 2's per-user
    // limit accordingly, with a small queue so a brief burst smooths out
    // instead of hard-rejecting.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";

        if (whitelistedIps.Contains(ip))
        {
            return RateLimitPartition.GetNoLimiter(partitionKey: $"whitelist_{ip}");
        }

        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: $"global_ip_{ip}",
            factory: partition => new SlidingWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 2000,
                QueueLimit = 50,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6
            });
    });

    // ==========================================
    // LAYER 2: ENDPOINT POLICY (Business Rules)
    // ==========================================
    options.AddPolicy("PerUserLimit", context =>
    {
        var isAuthenticated = context.User.Identity?.IsAuthenticated == true;

        if (isAuthenticated)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown_user";

            // 100/min (queue 6) was tuned for occasional list-page loads, not
            // a console with pages that poll/refresh on tab focus (the case
            // calendar) plus several parallel count queries per screen (the
            // case queue's per-tab badges) — a single busy staff member
            // could legitimately clear the old limit. 300/min (5/s
            // sustained) with a bigger queue gives real usage headroom while
            // still capping a runaway client.
            return RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: $"user_{userId}",
                factory: partition => new SlidingWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 300,
                    QueueLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6
                });
        }
        else
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous_ip";
            if (whitelistedIps.Contains(ip))
            {
                return RateLimitPartition.GetNoLimiter(partitionKey: $"policy_whitelist_{ip}");
            }

            // Anonymous traffic is almost entirely login attempts, so this
            // stays deliberately tighter than the authenticated bucket
            // above — a modest bump from 30/min for genuine retries
            // (mistyped passwords, page reloads) without loosening
            // brute-force protection.
            return RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: $"guest_ip_{ip}",
                factory: partition => new SlidingWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 60,
                    QueueLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6
                });
        }
    });
});

builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AlAmalBusiness API",
        Version = "v1"
    });

    // Define the JWT Bearer scheme
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token below (without the 'Bearer' prefix)."
    });

    // Require the scheme globally
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddAuthorization(options =>
{
    // Secure-by-default: any endpoint without its own [Authorize]/[AllowAnonymous]
    // requires an authenticated user, rather than defaulting to anonymous.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
// Enums serialize/deserialize as their string name (e.g. "Cash", "Pending")
// everywhere — request bodies, response bodies, everything through
// System.Text.Json. Without this they're raw numbers, which is both opaque
// over the wire and rejects the string values every client naturally sends.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Frontend origin(s) allowed to call this API — never AllowAnyOrigin(), and no
// credentials mode since auth is a bearer JWT (Authorization header / SignalR
// ?access_token=), not a cookie.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await initializer.SeedRolesAsync();
}
app.UseForwardedHeaders();

// Registered unconditionally (not just in Development) — otherwise an
// unhandled exception in production returns a bare empty 500 with nothing
// for the frontend to parse.
app.UseExceptionHandler("/error");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("Frontend");
// Add this high up in your Program.cs pipeline, before app.UseRateLimiter()

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<LeadHub>("/hubs/leads");
app.Map("/error", (HttpContext context) =>
{
    var exceptionFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
    return Results.Problem(
        title: "An unexpected error occurred.",
        detail: app.Environment.IsDevelopment() ? exceptionFeature?.Error?.ToString() : null,
        statusCode: StatusCodes.Status500InternalServerError);
}).AllowAnonymous();
app.Run();
