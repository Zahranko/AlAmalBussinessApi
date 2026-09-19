using AlAmalBusiness.Api.Area.CRM.Hubs;
using AlAmalBusiness.Api.Email;
using AlAmalBusiness.Application.Services.Imp.Appointments;
using AlAmalBusiness.Application.Services.Interface.Appointments;
using AlAmalBusiness.Domain.IRepositories.Appointments;
using AlAmalBusiness.Infrastructure.Repository.Imp.Appointments;
using AlAmalBusiness.Application.Services.Imp.Tickets;
using AlAmalBusiness.Application.Services.Interface.Tickets;
using AlAmalBusiness.Domain.IRepositories.Tickets;
using AlAmalBusiness.Infrastructure.Repository.Imp.Tickets;
using AlAmalBusiness.Application.Services.Imp;
using AlAmalBusiness.Application.Services.Imp.CRM;
using AlAmalBusiness.Application.Services.Imp.Feedback;
using AlAmalBusiness.Application.Services.Interface;
using AlAmalBusiness.Application.Services.Interface.CRM;
using AlAmalBusiness.Application.Services.Interface.Feedback;
using AlAmalBusiness.Application.Services.Imp.Questionnaires;
using AlAmalBusiness.Application.Services.Interface.Questionnaires;
using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.IRepositories;
using AlAmalBusiness.Domain.IRepositories.CRM;
using AlAmalBusiness.Domain.IRepositories.Feedback;
using AlAmalBusiness.Domain.IRepositories.Questionnaires;
using AlAmalBusiness.Domain.Models;
using AlAmalBusiness.Infrastructure.Repository.Imp;
using AlAmalBusiness.Infrastructure.Repository.Imp.CRM;
using AlAmalBusiness.Infrastructure.Repository.Imp.Feedback;
using AlAmalBusiness.Infrastructure.Repository.Imp.Questionnaires;
using AlAmalBusiness.Infrastructure.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Security.Cryptography;
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
builder.Services.AddScoped<IPatientFeedbackRepo, PatientFeedbackRepo>();
builder.Services.AddScoped<IFeedbackHistoryRepo, FeedbackHistoryRepo>();
builder.Services.AddScoped<IQuestionnaireRepo, QuestionnaireRepo>();
builder.Services.AddScoped<IAppointmentRequestRepo, AppointmentRequestRepo>();
builder.Services.AddScoped<IAppointmentHistoryRepo, AppointmentHistoryRepo>();
builder.Services.AddScoped<IAppointmentReferralSourceRepo, AppointmentReferralSourceRepo>();
builder.Services.AddScoped<ITicketRepo, TicketRepo>();
builder.Services.AddScoped<ITicketHistoryRepo, TicketHistoryRepo>();
builder.Services.AddScoped<ITicketCategoryRepo, TicketCategoryRepo>();
builder.Services.AddScoped<ITicketProcedureRepo, TicketProcedureRepo>();
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
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IFeedbackExcelReportService, FeedbackExcelReportService>();
builder.Services.AddScoped<IQuestionnaireService, QuestionnaireService>();
builder.Services.AddScoped<IQuestionnaireExcelReportService, QuestionnaireExcelReportService>();
builder.Services.AddScoped<IQuestionnaireMonthlyReportService, QuestionnaireMonthlyReportService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IAppointmentListService, AppointmentListService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<ITicketListService, TicketListService>();
// Emails last month's questionnaire report to each department's QManagers,
// once per month (QuestionnaireReport section) — see the scheduler.
builder.Services.AddHostedService<AlAmalBusiness.Api.Area.Questionnaires.QuestionnaireMonthlyReportScheduler>();
// Stateless and thread-safe (a static alphabet over the crypto RNG), so one
// instance serves every request.
builder.Services.AddSingleton<IReferenceNumberGenerator, ReferenceNumberGenerator>();
// Outgoing email (Hostinger SMTP). One queue instance shared by every request
// and the background sender that drains it — see ChannelEmailQueue.
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.SectionName));
builder.Services.AddSingleton<ChannelEmailQueue>();
builder.Services.AddSingleton<IEmailQueue>(sp => sp.GetRequiredService<ChannelEmailQueue>());
builder.Services.AddHostedService<SmtpEmailBackgroundService>();
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
builder.Services.AddIdentity<User, IdentityRole>(options =>
    {
        // Account lockout: the per-IP login limiter alone can't stop a slow,
        // distributed guess at one account. AuthRepo counts failures and
        // honours LockoutEnd itself (see LogInAsync), so this also covers
        // accounts imported with LockoutEnabled = false.
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Nothing here takes an upload; the largest legitimate body is a follow-up
// carrying a clinic signature image (capped in FollowUpLeadDTO). The 30 MB
// server default let one request park a 30 MB blob in a LOB column.
const long MaxRequestBodyBytes = 4 * 1024 * 1024;
builder.Services.Configure<IISServerOptions>(options => options.MaxRequestBodySize = MaxRequestBodyBytes);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = MaxRequestBodyBytes);

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
    // The anonymous patient feedback form. Partitioned per client IP because
    // there is no user to partition on, and deliberately far tighter than the
    // guest bucket below: a patient submits one message, then leaves. This is
    // the only write endpoint on the app reachable without a token, so it gets
    // its own ceiling rather than sharing the general anonymous allowance
    // with login attempts.
    options.AddPolicy("PublicFormLimit", context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous_ip";
        if (whitelistedIps.Contains(ip))
        {
            return RateLimitPartition.GetNoLimiter(partitionKey: $"public_whitelist_{ip}");
        }

        // Enough for the dropdown fetch, a mistyped submission or two, and a
        // retry — and nothing like enough to script the table full.
        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: $"public_form_ip_{ip}",
            factory: partition => new SlidingWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6
            });
    });

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
// X-Forwarded-For decides the client IP every rate limiter and the IP
// whitelist key on. The console's server (Hostinger, no fixed address we
// know) proxies staff traffic here and forwards the real client IP, so the
// proxy can't be trusted by address. It is trusted by a shared secret
// instead: the gate middleware below strips X-Forwarded-* from any request
// that doesn't carry ForwardedHeaders:ProxySecret, and only then does
// UseForwardedHeaders run with no address restriction. Without the gate any
// client could claim the whitelisted office IP and skip every limiter.
const string ProxySecretHeader = "X-Proxy-Secret";
var proxySecret = builder.Configuration["ForwardedHeaders:ProxySecret"];
var proxySecretBytes = string.IsNullOrWhiteSpace(proxySecret) || proxySecret.Length < 32
    ? null
    : Encoding.UTF8.GetBytes(proxySecret);
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
    await initializer.SeedTicketProceduresAsync();
}
if (proxySecretBytes == null)
{
    app.Logger.LogWarning(
        "ForwardedHeaders:ProxySecret is not set (or shorter than 32 characters); X-Forwarded-For is ignored on every request.");
}
app.Use(async (context, next) =>
{
    var headers = context.Request.Headers;
    var trusted = proxySecretBytes != null
        && headers.TryGetValue(ProxySecretHeader, out var presented)
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(presented.ToString()), proxySecretBytes);
    headers.Remove(ProxySecretHeader);
    if (!trusted)
    {
        headers.Remove("X-Forwarded-For");
        headers.Remove("X-Forwarded-Proto");
        headers.Remove("X-Forwarded-Host");
    }
    await next();
});
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
    // A body over MaxRequestBodyBytes (or otherwise unreadable) is the
    // client's fault; Kestrel and IIS both report it with the status to use.
    if (exceptionFeature?.Error is BadHttpRequestException badRequest)
    {
        return Results.Problem(
            title: badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge
                ? "The request is too large."
                : "The request could not be read.",
            statusCode: badRequest.StatusCode);
    }
    return Results.Problem(
        title: "An unexpected error occurred.",
        detail: app.Environment.IsDevelopment() ? exceptionFeature?.Error?.ToString() : null,
        statusCode: StatusCodes.Status500InternalServerError);
}).AllowAnonymous();
app.Run();
