using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.Data;
using Server.Entities;
using Server.GraphQL;
using Server.Services;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

var jwtPrivateKey = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(
        builder.Configuration["JwtPrivateKey"]!
    ));

var signingCredentials = new SigningCredentials(jwtPrivateKey, SecurityAlgorithms.HmacSha256);

builder.Configuration.AddJsonFile("appsettings.json",
    optional: true,
    reloadOnChange: false);

services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidIssuer = "issuer",
                ValidAudience = "audience",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = jwtPrivateKey
            };
    });

services
    .AddCors(
        options =>
        {
            options.AddPolicy("DefaultPolicy", builder =>
            {
                builder
                    .AllowAnyHeader()
                    .WithMethods("GET", "POST")
                    .WithOrigins("http://localhost:8080");
            });
        }
    );

var connectionStringBuilder = new SqlConnectionStringBuilder(builder.Configuration.GetConnectionString("DefaultConnection"))
{
    Password = builder.Configuration["DatabasePassword"]
};

services.AddPooledDbContextFactory<ApplicationDbContext>(
    options => options.UseSqlServer(connectionStringBuilder.ConnectionString));
    
services.AddScoped(serviceProvider
    => serviceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

services
    .AddIdentityCore<EntUser>(options =>
    {
        options.Password.RequiredLength = 8;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

services.AddHttpLogging(options =>
{
    options.RequestHeaders.Add("Authorization");
});

builder.Services
    .AddGraphQLServer()
    .AddAuthorization(options =>
    {
        options.AddPolicy("AdminOrCanViewOwnData", policy =>
        {
            policy.RequireAssertion(context =>
            {
                return true;
            });
        });
    })
    .AddQueryType<Query>();

builder.Services.AddScoped<AuthenticationService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("DefaultPolicy");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapGraphQL();

using var scope = app.Services.CreateScope();

using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
await dbContext.Database.MigrateAsync();

var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
var authenticationService = scope.ServiceProvider.GetRequiredService<AuthenticationService>();
var userManager = scope.ServiceProvider.GetRequiredService<UserManager<EntUser>>();

var testUser = await userManager.FindByEmailAsync("testuser@example.com");
if (testUser is not null)
{
    logger.LogInformation("Token: {token}", await authenticationService.GetTokenAsync(testUser));
}

await app.RunAsync();
