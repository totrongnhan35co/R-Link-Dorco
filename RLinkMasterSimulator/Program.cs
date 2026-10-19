using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RLinkMasterSimulator.Middleware;
using RLinkMasterSimulator.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5130");
// ── JWT ───────────────────────────────────────────────────────────────
string jwtSecret = builder.Configuration["Jwt:Secret"] ?? "RLinkMasterSimulator_JWTSecret_2026!!";
byte[] key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "🖥 R-Link Master Simulator",
        Version = "v1",
        Description = "Giả lập API R-Link Master. Port: 5130."
    });
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "POST /api/rlink/auth/login → lấy AccessToken → nhập vào đây"
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference
          { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// ── Singleton services ────────────────────────────────────────────────
builder.Services.AddSingleton<SimQrStore>();
builder.Services.AddSingleton<SimRLinkConfig>();
builder.Services.AddSingleton<SimMonitorLog>();
builder.Services.AddSingleton<SimLineStore>();
builder.Services.AddSingleton<SimFactoryStore>();
builder.Services.AddSingleton<AccountStore>();
builder.Services.AddSingleton<SimLogStore>();
builder.Services.AddHttpClient();

var app = builder.Build();

// Auto-generate 500 QR khi khởi động
app.Services.GetRequiredService<SimQrStore>().Generate(500);

// ── CORS phải đứng ĐẦU TIÊN — trước Swagger, StaticFiles, Auth ────────
app.UseCors();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "R-Link Master Simulator v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "R-Link Master Simulator";
    c.DefaultModelsExpandDepth(-1);
});

app.UseStaticFiles();

// ── Request logging middleware ────────────────────────────────────────
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();