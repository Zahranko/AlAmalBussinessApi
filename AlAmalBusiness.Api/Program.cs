using AlAmalBusiness.Application.Services.Imp;
using AlAmalBusiness.Application.Services.Interface;
using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.IRepositories;
using AlAmalBusiness.Domain.Models;
using AlAmalBusiness.Infrastructure.Repository.Imp;
using AlAmalBusiness.Infrastructure.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<DbInitializer>();

// Repositories (Infrastructure)
builder.Services.AddScoped<IUserRepo, UserRepo>();
builder.Services.AddScoped<IAuthRepo, AuthRepo>();
builder.Services.AddScoped<IDepartmentRepo, DepartmentRepo>();
// Services (Application)
builder.Services.AddScoped<IUserServices, UserServices>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
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
});

var whitelistedIps = builder.Configuration.GetSection("RateLimiterSettings:WhitelistedIps").Get<HashSet<string>>() ?? new HashSet<string>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, token) =>
    {
        await context.HttpContext.Response.WriteAsync(
            "Rate limit exceeded. Please try again later.", token);
    };

    // ==========================================
    // LAYER 1: GLOBAL LIMITER (The Safety Net)
    // ==========================================
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
                PermitLimit = 500,
                QueueLimit = 0,   
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

            return RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: $"user_{userId}",
                factory: partition => new SlidingWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 100,
                    QueueLimit = 6,
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

            return RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: $"guest_ip_{ip}",
                factory: partition => new SlidingWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 30,
                    QueueLimit = 2,
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

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await initializer.SeedRolesAsync();
}
app.UseForwardedHeaders();
if (app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseRouting();
// Add this high up in your Program.cs pipeline, before app.UseRateLimiter()

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
