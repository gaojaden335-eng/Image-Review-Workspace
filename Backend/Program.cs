using Backend.Data;
using Backend.Middleware;
using Backend.Repositories;
using Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// 减少EF Core的SQL日志噪音（只记录警告和错误）
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

var maxRequestBodySize = builder.Configuration.GetValue<long>(
    "UploadLimits:MaxBatchSizeBytes", 512L * 1024 * 1024);

// Configure Kestrel server limits for file uploads
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = maxRequestBodySize;
    serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
});

// Configure form options for multipart/form-data
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maxRequestBodySize;
    options.ValueLengthLimit = int.MaxValue;
    options.ValueCountLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = false;
    });
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("ModelValidation");

        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .Select(e => new
            {
                Field = e.Key,
                Errors = e.Value!.Errors.Select(err => err.ErrorMessage)
            })
            .ToList();

        var correlationId = context.HttpContext.TraceIdentifier;

        logger.LogWarning("模型验证失败 Path={Path} CorrelationId={CorrelationId} Errors={Errors}",
            context.HttpContext.Request.Path,
            correlationId,
            JsonSerializer.Serialize(errors));

        return new BadRequestObjectResult(new
        {
            message = "请求数据验证失败",
            correlationId,
            errors
        });
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Repository layer
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register Service layer
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IImageProcessingService, ImageProcessingService>();
builder.Services.AddScoped<IImageGroupService, ImageGroupService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IQueueService, QueueService>();
builder.Services.AddScoped<ISelectionService, SelectionService>();
builder.Services.AddScoped<IUserService, UserService>();

// Configure MySQL Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database configuration is missing. Set ConnectionStrings__DefaultConnection.");
}
var serverVersion = new MySqlServerVersion(new Version(8, 0, 35)); // 根据你的MySQL版本调整
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(connectionString, serverVersion, mysqlOptions =>
    {
        mysqlOptions.CommandTimeout(30); // 30秒命令超时
        mysqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
    });
    
    // 开发环境启用详细日志
    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
        if (builder.Configuration.GetValue<bool>("Diagnostics:EnableSensitiveDataLogging"))
        {
            options.EnableSensitiveDataLogging();
        }
    }
});

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];
if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 32)
{
    throw new InvalidOperationException(
        "JWT configuration is missing or too short. Set JwtSettings__SecretKey to at least 32 characters.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (string.IsNullOrEmpty(context.Token) &&
                context.Request.Cookies.TryGetValue("access_token", out var cookieToken))
            {
                context.Token = cookieToken;
            }

            return Task.CompletedTask;
        }
    };
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
    };
});

builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

// Configure CORS
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? Array.Empty<string>();

    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        // Apply migrations
        dbContext.Database.Migrate();
        Console.WriteLine("Database migrated successfully.");

        // 仅在显式配置时创建初始管理员，避免公开默认凭据。
        var defaultAdminUsername = builder.Configuration["DefaultAdmin:Username"];
        var defaultAdminPassword = builder.Configuration["DefaultAdmin:Password"];

        if (!string.IsNullOrWhiteSpace(defaultAdminUsername) &&
            !string.IsNullOrWhiteSpace(defaultAdminPassword) &&
            !dbContext.Users.Any(u => u.Username == defaultAdminUsername))
        {
            var adminUser = new Backend.Models.User
            {
                Username = defaultAdminUsername,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultAdminPassword),
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            };
            dbContext.Users.Add(adminUser);
            dbContext.SaveChanges();
            Console.WriteLine("Initial admin user created.");
        }
        else if (string.IsNullOrWhiteSpace(defaultAdminUsername) ||
                 string.IsNullOrWhiteSpace(defaultAdminPassword))
        {
            Console.WriteLine("Initial admin creation skipped. Configure DefaultAdmin environment variables if needed.");
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(ex, "Database initialization failed. The application will stop.");
        throw;
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

// 🔧【修改这里】调整中间件顺序
// 全局异常处理（必须在最前面）
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// 请求/响应日志（现在只记录，不再处理异常）
app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
