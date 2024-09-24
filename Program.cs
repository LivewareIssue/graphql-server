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

await LogTestUserToken();
await ApplyMigrations();
await app.RunAsync();

async Task LogTestUserToken()
{
    using var scope = app.Services.CreateScope();

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<EntUser>>();
    var testUser = await userManager.FindByEmailAsync("testuser@example.com");

    if (testUser is not null)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var authenticationService = scope.ServiceProvider.GetRequiredService<AuthenticationService>();
        logger.LogInformation("Token: {token}", await authenticationService.GetTokenAsync(testUser));
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