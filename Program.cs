using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using tradex_backend.Models;
using Microsoft.OpenApi.Models;
using tradex_backend.hubs;
using System.IdentityModel.Tokens.Jwt;
using tradex_backend.services;
using Microsoft.AspNetCore.SignalR;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); 

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); 
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "tradex.db");
    options.UseSqlite($"Data Source=/Users/sasmitjha/tradex/TradeX/backend/tradex.db");
});

builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>();

builder.Services.AddHttpClient();
builder.Services.AddHostedService<OrderMatchingService>();

var key = builder.Configuration["Jwt:Key"] ?? "this is my secret key";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateIssuer = false,
            ValidateAudience = false
        };

        options.Events = new JwtBearerEvents
{
    OnMessageReceived = context =>
    {
        var accessToken = context.Request.Query["access_token"];
        var path = context.HttpContext.Request.Path;

        if (!string.IsNullOrEmpty(accessToken) &&
            (path.StartsWithSegments("/hubs/portfolio") || path.StartsWithSegments("/hubs/orderbook")))
        {
            context.Token = accessToken;
        }

        return Task.CompletedTask;
    }
};

    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "TradeX API", Version = "v1" });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        Description = "Put **_ONLY_** your JWT Bearer token here",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });
});
builder.Services.AddHostedService<PortfolioSnapshotService>();

builder.WebHost.UseUrls("http://0.0.0.0:5001");
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");

app.UseRouting(); 
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<PortfolioHub>("/hubs/portfolio");
app.MapHub<OrderBookHub>("/hubs/orderbook");

app.Run();