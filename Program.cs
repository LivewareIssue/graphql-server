using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server;
using Server.Data;
using Server.Entities;
using Server.GraphQL;
using Server.Services;

var builder = WebApplication
    .CreateBuilder(args)
    .AddAuthentication()
    .AddDatabase()
    .AddServer();

var app = builder.Build();

app
    .UseHttpsRedirection()
    .UseCors("DefaultPolicy")
    .UseRouting()
    .UseAuthentication()
    .UseAuthorization();

app.MapGraphQL();

await ApplyMigrations();
await SeedDatabase();
await LogTestUserToken("testuser@example.com");
await LogTestUserToken("admin@example.com");
await app.RunAsync();

async Task SeedDatabase()
{
    using var scope = app.Services.CreateScope();

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<EntUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    var testUser = await userManager.FindByEmailAsync("testuser@example.com");
    if (testUser is null)
    {
        testUser = new EntUser
        {
            UserName = "TestUser",
            Email = "testuser@example.com"
        };

        var result = await userManager.CreateAsync(testUser, "Password123!");
    }

    var testAdmin = await userManager.FindByEmailAsync("admin@example.com");
    if (testAdmin is null)
    {
        testAdmin = new EntUser
        {
            UserName = "Admin",
            Email = "admin@example.com"
        };

        await userManager.CreateAsync(testAdmin, "Password123!");
    }

    var userRole = await roleManager.FindByNameAsync("User");
    if (userRole is null)
    {
        userRole = new IdentityRole("User");
        await roleManager.CreateAsync(userRole);
    }

    var adminRole = await roleManager.FindByNameAsync("Admin");
    if (adminRole is null)
    {
        adminRole = new IdentityRole("Admin");
        await roleManager.CreateAsync(adminRole);
    }

    if (!await userManager.IsInRoleAsync(testUser, "User"))
    {
        await userManager.AddToRoleAsync(testUser, "User");
    }

    if (!await userManager.IsInRoleAsync(testAdmin, "Admin"))
    {
        await userManager.AddToRoleAsync(testAdmin, "Admin");
    }
}

async Task LogTestUserToken(string email)
{
    using var scope = app.Services.CreateScope();

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<EntUser>>();
    var testUser = await userManager.FindByEmailAsync(email);

    if (testUser is not null)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var authenticationService = scope.ServiceProvider.GetRequiredService<AuthenticationService>();
        logger.LogInformation("User: {user}, Token: {token}", testUser.UserName, await authenticationService.GetTokenAsync(testUser));
    }
}

async Task ApplyMigrations()
{
    using var scope = app.Services.CreateScope();

    using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}

namespace Server
{
    public static class WebApplicationBuilderExtensions
    {
        public static WebApplicationBuilder AddAuthentication(this WebApplicationBuilder builder)
        {
            builder.Configuration.AddUserSecrets<Program>();

            builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
            var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;

            builder.Services
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
                            ValidIssuer = jwtOptions.ValidIssuer,
                            ValidAudience = jwtOptions.ValidAudience,
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = AuthenticationService.GetSecurityKey(jwtOptions),
                        };
                });

            builder.Services
                .AddIdentityCore<EntUser>(options =>
                {
                    options.Password.RequiredLength = 8;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
            
            builder.Services.AddScoped<AuthenticationService>();

            return builder;
        }

        public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder(builder.Configuration.GetConnectionString("DefaultConnection"))
            {
                Password = builder.Configuration["DatabasePassword"]
            };

            builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(
                options => options.UseSqlServer(connectionStringBuilder.ConnectionString));
                
            builder.Services.AddScoped(serviceProvider
                => serviceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());
            
            return builder;
        }

        public static WebApplicationBuilder AddServer(this WebApplicationBuilder builder)
        {
            builder.Services
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

            builder.Services
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
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
            
            return builder;
        }
    }
}