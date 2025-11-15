using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MinimalASP.NETWebAPIWithJWTAuthentication.DTOs;
using MinimalASP.NETWebAPIWithJWTAuthentication.Services;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddScoped<JwtTokenGenerator>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapPost("/login", (LoginRequest request, JwtTokenGenerator jwt) =>
{
    if (request.Username == "admin" && request.Password == "123")
    {
        var token = jwt.GenerateToken(request.Username);
        return Results.Ok(new { Token = token });
    }

    return Results.Unauthorized();
});

app.MapGet("/weather", () =>
{
    return new
    {
        Date = DateTime.Now.ToShortDateString(),
        Temperature = "25",
        Status = "Clear Sky"
    };
})
.RequireAuthorization();

app.MapGet("/welcome", (ClaimsPrincipal user) =>
{
    string name = user.Identity?.Name ?? "Unknown";
    return Results.Ok($"Welcome {name} You are authorized");
})
.RequireAuthorization();

app.Run();