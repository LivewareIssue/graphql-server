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
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                builder.AllowAnyHeader()
                    .WithMethods("GET", "POST")
                    .WithOrigins("http://localhost:8080");
            });
        }
    );

var connectionStringBuilder = new SqlConnectionStringBuilder(builder.Configuration.GetConnectionString("DefaultConnection"))
{
    Password = builder.Configuration["DatabasePassword"]
};

services.AddDbContextFactory<ApplicationDbContext>(options
    => options.UseSqlServer(connectionStringBuilder.ConnectionString));

services
    .AddTransient<IEntPersonService, EntPersonService>();

services
    .AddIdentity<EntPerson, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services
    .AddGraphQLServer()
    .AddAuthorization()
    .AddQueryType<Query>()
    .AddHttpRequestInterceptor((context, executor, builder, ct) =>
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer "))
        {
            var token = authorization["Bearer ".Length..];
            if (!string.IsNullOrWhiteSpace(token))
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var claimsPrincipal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidIssuer = "issuer",
                    ValidAudience = "audience",
                    IssuerSigningKey = jwtPrivateKey
                }, out _);

                context.User = claimsPrincipal;
            }
        }

        return ValueTask.CompletedTask;
    });

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("DefaultPolicy");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapGraphQL();

using var scope = app.Services.CreateScope();
var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
using var dbContext = dbContextFactory.CreateDbContext();

await dbContext.Database.MigrateAsync();

var userManager = scope.ServiceProvider.GetRequiredService<UserManager<EntPerson>>();
var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

if (!await roleManager.RoleExistsAsync("Admin"))
{
    var adminRole = new IdentityRole("Admin");
    await roleManager.CreateAsync(adminRole);
}

var testUser = await userManager.FindByEmailAsync("testuser@example.com");

if (testUser == null)
{
    testUser = new EntPerson
    {
        UserName = "testuser",
        Email = "testuser@example.com",
        EmailConfirmed = true
    };
    await userManager.CreateAsync(testUser, "Password123!");
}

if (!await userManager.IsInRoleAsync(testUser, "Admin"))
{
    await userManager.AddToRoleAsync(testUser, "Admin");
}

var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Test user id: '{id}'", testUser.Id);

var claims = new List<Claim>
{
    new(JwtRegisteredClaimNames.Sub, testUser.Id!),
    new(JwtRegisteredClaimNames.UniqueName, testUser.Id!),
    new(JwtRegisteredClaimNames.Email, testUser.Email!),
    new(JwtRegisteredClaimNames.NameId, testUser.UserName!),
    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
};

var userClaims = await userManager.GetClaimsAsync(testUser);
var userRoles = await userManager.GetRolesAsync(testUser);

claims.AddRange(userClaims);
foreach (var role in userRoles)
{
    claims.Add(new Claim(ClaimTypes.Role, role));
}

var tokenHandler = new JwtSecurityTokenHandler();

var token = new JwtSecurityToken(
    issuer: "issuer",
    audience: "audience",
    claims: claims,
    expires: DateTime.UtcNow.AddHours(1),
    signingCredentials: signingCredentials);

logger.LogInformation("Token: '{token}'", tokenHandler.WriteToken(token));

await app.RunAsync();
