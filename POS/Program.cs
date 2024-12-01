using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using POS.Datas;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext with SQL Server connection string
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add JWT authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

// Add authorization
builder.Services.AddAuthorization();

// Add CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000") 
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Allow cookies or credentials if needed
    });
});

// Add controllers
builder.Services.AddControllers();

// Add Swagger for API documentation with JWT Bearer support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

    // Configure Swagger to accept JWT token
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by your JWT token (e.g., 'Bearer eyJhbGci...')"
    });

    // Apply security requirement to all API operations
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] {}
        }
    });
});

// Add WebSocket services
builder.Services.AddSingleton<WebSocketConnectionManager>();

var app = builder.Build();

// Enable Swagger UI at the root
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    c.RoutePrefix = string.Empty; // Set Swagger UI to be available at the root URL
});

// Use CORS policy
app.UseCors("AllowReactApp"); // Apply CORS policy

// WebSocket middleware
app.UseWebSockets();

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws" && context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var wsManager = app.Services.GetRequiredService<WebSocketConnectionManager>();
        await wsManager.HandleWebSocketConnectionAsync(webSocket);
    }
    else
    {
        await next();
    }
});

// Authentication and Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

app.Run();
