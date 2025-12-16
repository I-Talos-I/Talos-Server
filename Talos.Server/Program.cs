using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;
using Talos.Server.Data;
using Talos.Server.Middleware;
using Talos.Server.Services;
using Talos.Server.Services.Auth;
using Talos.Server.Models.Entities;

var builder = WebApplication.CreateBuilder(args);

// --------------------
// DbContext
// --------------------
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration.GetConnectionString("Default");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// --------------------
// JWT
// --------------------
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettings);

var key = Encoding.UTF8.GetBytes(
    jwtSettings["Key"] ?? "your-default-secret-key-minimum-32-characters");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// --------------------
// Servicios
// --------------------
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<TemplateService>();
builder.Services.AddScoped<PackageManagerService>();

// Controllers + JSON Options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Talos API", Version = "v1" });
    c.SwaggerDoc("v2", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Talos API", Version = "v2" });
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        if (!apiDesc.TryGetMethodInfo(out var methodInfo))
            return false;

        var versions = methodInfo.DeclaringType?
            .GetCustomAttributes(true)
            .OfType<ApiExplorerSettingsAttribute>()
            .Select(attr => attr.GroupName)
            .ToList();

        if (versions == null || versions.Count == 0)
            return docName == "v1";

        if (docName == "v1")
            return versions.Contains("v1");

        if (docName == "v2")
            return versions.Contains("v1") || versions.Contains("v2");

        return false;
    });
});

// AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Cache (Redis fallback)
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "Talos_";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(
                "https://talos.vandlee.com",
                "http://localhost:3000",
                "http://localhost:5173",
                "http://localhost:4200"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// --------------------
// Build app
// --------------------
var app = builder.Build();

// --------------------
// Seed inicial de Admin y API Keys
// --------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    // Crea admin por defecto si no existe
    await UserSeeder.SeedAsync(db);

    // Crea API Keys iniciales
    ApiKeySeeder.Seed(db);
}

// --------------------
// Middleware
// --------------------
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Talos API v1");
    c.SwaggerEndpoint("/swagger/v2/swagger.json", "Talos API v2");
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ApiKeyMiddleware>();

// --------------------
// Endpoints
// --------------------
app.MapControllers();

app.MapGet("/", () => Results.Ok(new
{
    name = "Talos API",
    status = "Running",
    environment = app.Environment.EnvironmentName
}));

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    timestamp = DateTime.UtcNow
}));

app.Run();
//TODO refactorizar algunas partes del codigo para implementar buenas practicas