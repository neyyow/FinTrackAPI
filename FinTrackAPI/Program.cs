using FinTrackAPI.Data;
using FinTrackAPI.Endpoints;
using FinTrackAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Secrets come from User Secrets (development) or environment variables (production),
// never from appsettings.json.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection is not set. Add it with: dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"<value>\"");

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException(
        "Jwt:Key is missing or shorter than 32 characters. Add it with: dotnet user-secrets set \"Jwt:Key\" \"<value>\"");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddEndpointsApiExplorer();

// swagger
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your token}"
    });

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

// cors - only the origins listed in Cors:AllowedOrigins may call this API
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        else
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// jwt
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

builder.Services.AddScoped<JwtService>();

builder.Services.AddHttpClient();

var app = builder.Build();

// Swagger is a full map of the API - keep it out of production.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS has to come before the exception handler below - the CORS header gets
// attached to the response as soon as this middleware runs, so as long as
// this stays first, it stays on the response even if something later throws
// and the exception handler takes over. Without that ordering, an unhandled
// error anywhere in the app comes back to the browser with no CORS header at
// all, and shows up there as a "blocked by CORS policy" error that has
// nothing to do with CORS - which is exactly what happened with the
// /auth/google endpoint.
app.UseCors("AllowReactApp");

// Safety net: turn any unhandled exception into a clean JSON error response
// instead of a raw crash, so a bug in one endpoint never again masquerades
// as a CORS problem in the browser console.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"error\":\"Something went wrong. Please try again.\"}");
    });
});

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapGoogleAuthEndpoints();
app.MapExpenseEndpoints();
app.MapIncomeEndpoints();
app.MapRecurringIncomeEndpoints();
app.MapSummaryEndpoints();
app.MapProfileEndpoints();
app.MapLogsEndpoints();
app.MapBudgetEndpoints();

app.Run();
